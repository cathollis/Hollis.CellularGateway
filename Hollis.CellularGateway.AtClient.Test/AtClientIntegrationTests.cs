using Hollis.CellularGateway.AtClient;

namespace Hollis.CellularGateway.AtClient.Test;

/// <summary>
/// 共享的 Modem 连接夹具：整个测试类只打开一次串口，所有测试复用同一连接。
/// </summary>
public class ModemFixture : IAsyncLifetime
{
    public AtClient? Client { get; private set; }

    public async Task InitializeAsync()
    {
        var portName = Environment.GetEnvironmentVariable("AT_PORT_NAME");
        if (string.IsNullOrWhiteSpace(portName))
            return;

        Client = new(portName, baudRate: 115200);
        await Client.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        if (Client is not null)
        {
            try { await Client.DisposeAsync(); } catch { /* best-effort */ }
        }
    }
}

/// <summary>
/// 集成测试：通过物理串口连接真实 4G/5G 模组，测试 AT 命令功能。
/// 
/// 使用方式：
///   设置环境变量 AT_PORT_NAME 为你的串口号（如 COM3），然后运行测试。
///   如果未设置环境变量，所有测试会自动跳过。
///   
///   Linux:   export AT_PORT_NAME=/dev/ttyUSB2
///   Windows: $env:AT_PORT_NAME = "COM3"
/// </summary>
public class AtClientIntegrationTests(ModemFixture fixture) : IClassFixture<ModemFixture>
{
    private AtClient Client => fixture.Client!;

    private bool SkipIfNotConfigured()
    {
        if (fixture.Client is not null) return false;
        // AT_PORT_NAME 未设置 → 测试体不执行，显示为通过
        return true;
    }

    // ── 基础连接 ──────────────────────────────────────────────────────

    [Fact]
    public async Task Ping_ShouldReturnTrue()
    {
        if (SkipIfNotConfigured()) return;
        Assert.True(await Client.PingAsync());
    }

    // ── 设备信息 ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetDeviceInfo_ShouldReturnValidInfo()
    {
        if (SkipIfNotConfigured()) return;
        var info = await Client.GetDeviceInfoAsync();

        Assert.NotNull(info.Information);
    }

    [Fact]
    public async Task GetImsi_ShouldReturnValidImsi()
    {
        if (SkipIfNotConfigured()) return;
        var imsi = await Client.GetImsiAsync();

        Assert.NotNull(imsi);
        Assert.NotEmpty(imsi);
        Assert.Matches(@"^\d{14,15}$", imsi);
    }

    [Fact]
    public async Task GetIccid_ShouldReturnValidIccid()
    {
        if (SkipIfNotConfigured())
        {
            return;
        }
        var iccid = await Client.GetIccidAsync();

        Assert.NotNull(iccid);
        Assert.NotEmpty(iccid);
        Assert.Matches(@"(\d+)", iccid);
    }

    // ── SIM 状态 ──────────────────────────────────────────────────────

    [Fact]
    public async Task IsPinReady_ShouldReturnTrue()
    {
        if (SkipIfNotConfigured()) return;
        Assert.True(await Client.IsPinReadyAsync());
    }

    // ── 网络状态 ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSignalQuality_ShouldReturnValidRssi()
    {
        if (SkipIfNotConfigured()) return;
        var rssi = await Client.GetSignalQualityAsync();

        Assert.NotNull(rssi);
        Assert.InRange(rssi.Value, 0, 99);
    }

    [Fact]
    public async Task IsNetworkRegistered_ShouldBeTrue()
    {
        if (SkipIfNotConfigured()) return;
        Assert.True(await Client.IsNetworkRegisteredAsync());
    }

    // ── SMS 发送 ──────────────────────────────────────────────────────

    [Fact]
    public async Task SendSms_English_ShouldSucceed()
    {
        if (SkipIfNotConfigured()) return;
        var targetNumber = Environment.GetEnvironmentVariable("AT_TEST_PHONE");
        if (string.IsNullOrWhiteSpace(targetNumber)) return;

        await Client.SendSmsAsync(targetNumber, $"Hello {DateTimeOffset.Now:HH:mm:ss}");
    }

    [Fact]
    public async Task SendSms_Chinese_ShouldSucceed()
    {
        if (SkipIfNotConfigured()) return;
        var targetNumber = Environment.GetEnvironmentVariable("AT_TEST_PHONE");
        if (string.IsNullOrWhiteSpace(targetNumber)) return;

        await Client.SendSmsAsync(targetNumber, $"好 {DateTimeOffset.Now:HH:mm:ss}");
    }

    // ── SMS 存储管理 ──────────────────────────────────────────────────

    [Fact]
    public async Task ListMessages_ShouldReturnMessages()
    {
        if (SkipIfNotConfigured()) return;
        var messages = await Client.ListMessagesAsync();
        Assert.NotNull(messages);
    }

    [Fact]
    public async Task ReadMessage_InvalidIndex_ShouldReturnNull()
    {
        if (SkipIfNotConfigured()) return;
        var msg = await Client.ReadMessageAsync(999999);
        Assert.Null(msg);
    }
}
