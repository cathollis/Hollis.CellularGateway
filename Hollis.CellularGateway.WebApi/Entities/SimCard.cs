using System.ComponentModel.DataAnnotations;
using Hollis.CellularGateway.WebApi.Consts;

namespace Hollis.CellularGateway.WebApi.Entities;

public class SimCard : EntityBase
{
    [StringLength(SimCardConst.DisplayNameMaxLength)]
    public required string DisplayName { get; set; }

    [StringLength(SimCardConst.DescriptionMaxLength)]
    public string? Description { get; set; }

    [StringLength(SimCardConst.InternationalMobileSubscriberIdMaxLength)]
    public required string InternationalMobileSubscriberId { get; set; }
}
