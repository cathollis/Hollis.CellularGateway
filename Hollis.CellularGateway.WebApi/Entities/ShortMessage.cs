using System.ComponentModel.DataAnnotations;
using Hollis.CellularGateway.WebApi.Consts;
using Hollis.CellularGateway.WebApi.Models.ShortMessage;

namespace Hollis.CellularGateway.WebApi.Entities;

public class ShortMessage : EntityBase
{
    [StringLength(ShortMessageConst.ContentMaxLength)]
    public required string Content { get; set; }

    public DateTimeOffset? TransmissionTime { get; set; }

    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.Now;

    public ShortMessageModel.TransmissionDirectionEnum TransmissionDirection { get; set; }

    [StringLength(ShortMessageConst.TargetPhoneNumberMaxLength)]
    public string TargetPhoneNumber { get; set; } = string.Empty;

    public Guid SimCardId { get; set; }

    public virtual SimCard SimCard { get; set; } = null!;
}
