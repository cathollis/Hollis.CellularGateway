using System.Collections.Concurrent;
using HeboTech.ATLib.Events;
using HeboTech.ATLib.Messaging;
using HeboTech.ATLib.Misc;
using HeboTech.ATLib.Modems;
using HeboTech.ATLib.Numbering;
using HeboTech.ATLib.Parsing;
using RJCP.IO.Ports;

namespace Hollis.CellularGateway.AtClient;

/// <summary>
/// High‑level cellular modem client.  Wraps <see cref="IModem"/> from HeboTech.ATLib
/// and provides a simplified API for SMS send / receive, device information, and
/// storage management.  All SMS operations use PDU mode with automatic GSM‑7 / UCS‑2
/// encoding selection.
/// </summary>
public sealed class AtClient(string portName, int baudRate = 115200) : IAsyncDisposable
{
    private readonly string _portName = portName ?? throw new ArgumentNullException(nameof(portName));
    private SerialPortStream? _serialPort;
    private AtChannel? _atChannel;
    private IModem? _modem;

    // Multipart SMS reassembly.
    private readonly ConcurrentDictionary<(int Reference, int Total), List<SmsDeliver>> _pendingParts = new();

    /// <summary>
    /// Raised when a fully reassembled SMS is received.  For single‑part messages
    /// the event fires immediately; for multipart messages it fires once all parts
    /// have arrived and been concatenated.
    /// </summary>
    public event EventHandler<SmsDeliver>? SmsReceived;

    /// <summary>
    /// Raised for every individual SMS part, before reassembly.
    /// </summary>
    public event EventHandler<SmsDeliver>? SmsPartReceived;

    // ── Lifecycle ────────────────────────────────────────────────────────

    public async Task OpenAsync(CancellationToken ct = default)
    {
        _serialPort = new(_portName, baudRate)
        {
            ReadTimeout = Timeout.Infinite,
            WriteTimeout = Timeout.Infinite,
            RtsEnable = true,
            DtrEnable = true
        };

        await Task.Run(() => _serialPort.Open(), ct);

        _atChannel = AtChannel.Create(_serialPort);
        _atChannel.Open();

        _modem = new QuectelModem(_atChannel);
        _modem.SmsReceived += OnSmsReceived;

        // Basic modem initialisation.
        await _modem.DisableEchoAsync();
        await _modem.SetNewSmsIndicationAsync(2, 2, 0, 0, 1);
    }

    public async Task CloseAsync()
    {
        if (_modem is not null)
        {
            _modem.SmsReceived -= OnSmsReceived;
            _modem.Dispose();
            _modem = null;
        }

        _atChannel?.Dispose();
        _atChannel = null;

        if (_serialPort is not null)
        {
            if (_serialPort.IsOpen)
            {
                await Task.Run(() => _serialPort.Close());
            }

            await _serialPort.DisposeAsync();
        }

        _serialPort = null;
    }

    public async ValueTask DisposeAsync()
    {
        try { await CloseAsync(); } catch { /* best‑effort */ }
        GC.SuppressFinalize(this);
    }

    // ── SMS – Send ───────────────────────────────────────────────────────

    /// <summary>
    /// Sends an SMS in PDU mode.  The library automatically selects GSM‑7 or UCS‑2
    /// encoding and handles multipart segmentation when the message exceeds 160 (GSM‑7)
    /// or 70 (UCS‑2) characters.
    /// </summary>
    public async Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ThrowIfNotOpen();

        var number = PhoneNumberFactory.CreateCommonIsdn(phoneNumber);
        var request = new SmsSubmitRequest(number, message);

        var results = await _modem!.SendSmsAsync(request);
        foreach (var result in results)
        {
            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to send SMS: {result.Error}");
            }
        }
    }

    // ── SMS – Receive (forwarded from ATLib) ─────────────────────────────
    private void OnSmsReceived(object? sender, SmsReceivedEventArgs e)
    {
        var sms = e.SmsDeliver;
        SmsPartReceived?.Invoke(this, sms);

        if (sms.TotalNumberOfParts <= 1)
        {
            SmsReceived?.Invoke(this, sms);
            return;
        }

        // Multipart reassembly.
        var key = (sms.MessageReference, sms.TotalNumberOfParts);
        var parts = _pendingParts.GetOrAdd(key, _ => []);
        lock (parts)
        {
            parts.RemoveAll(p => p.PartNumber == sms.PartNumber);
            parts.Add(sms);

            if (parts.Count != sms.TotalNumberOfParts)
                return;

            parts.Sort((a, b) => a.PartNumber.CompareTo(b.PartNumber));
            _pendingParts.TryRemove(key, out _);

            var combined = new SmsDeliver(
                sms.ServiceCenterNumber,
                sms.SenderNumber,
                string.Concat(parts.Select(p => p.Message)),
                sms.Timestamp);

            SmsReceived?.Invoke(this, combined);
        }
    }

    // ── SMS – Storage ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SmsWithIndex>> ListMessagesAsync(
        SmsStatus status = SmsStatus.ALL,
        CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.ListSmssAsync(status);
        return result.Success ? result.Result : [];
    }

    public async Task<Sms?> ReadMessageAsync(int index, CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.ReadSmsAsync(index);
        return result.Success ? result.Result : null;
    }

    public async Task DeleteMessageAsync(int index, CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.DeleteSmsAsync(index);
        if (!result.Success)
            throw new InvalidOperationException($"Failed to delete SMS at index {index}: {result.Error}");
    }

    public async Task DeleteAllMessagesAsync(CancellationToken ct = default)
    {
        var messages = await ListMessagesAsync(SmsStatus.ALL, ct);
        foreach (var msg in messages)
            await DeleteMessageAsync(msg.Index, ct);
    }

    // ── Device info ──────────────────────────────────────────────────────

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        return _atChannel is not null;
    }

    public async Task<ProductIdentificationInformation> GetDeviceInfoAsync(CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.GetProductIdentificationInformationAsync();
        return result.Success
                ? result.Result
                : throw new InvalidOperationException($"Failed to get device info: {result.Error}");
    }

    public async Task<string?> GetImsiAsync(CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.GetImsiAsync();
        return result.Success ? result.Result.ToString() : null;
    }

    public async Task<string?> GetIccidAsync(CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        // ATLib doesn't expose ICCID natively; use a raw Quectel command.
        var result = await _modem!.RawCommandWithResponseAsync("AT+QCCID", "+QCCID:");
        if (result.Success && result.Result.Count > 0)
        {
            foreach (var line in result.Result)
            {
                var m = System.Text.RegularExpressions.Regex.Match(line, @"\+QCCID:\s*(\d+)");
                if (m.Success) return m.Groups[1].Value;
            }
        }

        // Fallback to +CCID.
        result = await _modem!.RawCommandWithResponseAsync("AT+CCID", "+CCID:");
        if (result.Success && result.Result.Count > 0)
        {
            foreach (var line in result.Result)
            {
                var m = System.Text.RegularExpressions.Regex.Match(line, @"\+CCID:\s*(\d+)");
                if (m.Success) return m.Groups[1].Value;
            }
        }

        return null;
    }

    public async Task<int?> GetSignalQualityAsync(CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.GetSignalStrengthAsync();
        return result.Success ? (int)result.Result.Rssi.Value : null;
    }

    public async Task<bool> IsNetworkRegisteredAsync(CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.RawCommandWithResponseAsync("AT+CREG?", "+CREG:");
        if (!result.Success) return false;

        foreach (var line in result.Result)
        {
            var m = System.Text.RegularExpressions.Regex.Match(line, @"\+CREG:\s*\d+\s*,\s*(\d+)");
            if (m.Success)
            {
                int stat = int.Parse(m.Groups[1].Value,
                    System.Globalization.CultureInfo.InvariantCulture);
                return stat is 1 or 5;
            }
        }
        return false;
    }

    // ── PIN ──────────────────────────────────────────────────────────────

    public async Task<bool> IsPinReadyAsync(CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.GetSimStatusAsync();
        return result.Success && result.Result == SimStatus.SIM_READY;
    }

    public async Task<bool> EnterPinAsync(string pin, CancellationToken ct = default)
    {
        ThrowIfNotOpen();
        var result = await _modem!.EnterSimPinAsync(new(pin));
        return result.Success;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void ThrowIfNotOpen()
    {
        if (_modem is null || _atChannel is null)
            throw new InvalidOperationException("The client has not been opened.");
    }
}
