using HeboTech.ATLib.Messaging;
using HeboTech.ATLib.Modems;
using HeboTech.ATLib.Modems.Generic;
using HeboTech.ATLib.Parsing;

namespace Hollis.CellularGateway.AtClient;

/// <summary>
/// <see cref="IModem"/> implementation for Quectel cellular modules.
/// Overrides <see cref="SendSmsAsync"/> to fix the CMGS length calculation
/// inherited from <see cref="ModemBase"/> (the generic base includes the SMSC‑length
/// octet in the PDU but fails to subtract it from the <c>AT+CMGS</c> length).
/// </summary>
public sealed class QuectelModem(IAtChannel channel) : ModemBase(channel), IModem
{
    /// <inheritdoc />
    public override Task<IEnumerable<ModemResponse<SmsReference>>> SendSmsAsync(
        SmsSubmitRequest request)
    {
        // Use includeEmptySmscLength=false so the PDU contains only the TPDU.
        // This avoids the off‑by‑one bug in ModemBase where it prepends "00"
        // but calculates AT+CMGS length from the full PDU including that octet.
        return SendSmsAsync(request, false);
    }
}
