using System.Reflection;

namespace Hollis.CellularGateway.WebApi.Conventions;

/// <summary>
/// Marks a controller as a child resource nested under a parent controller.
/// The convention automatically generates the nested route:
/// <c>{parentRoute}/{parentIdParam}/[controller]</c>
/// </summary>
/// <example>
/// <code>
/// [ChildResourceOf(typeof(SimCardsController))]
/// public class ShortMessagesController : ControllerBase { }
/// // → route: SimCards/{simCardsId:guid}/ShortMessages
///
/// [ChildResourceOf(typeof(SimCardsController), ParentIdParam = "simCardId")]
/// // → route: SimCards/{simCardId:guid}/ShortMessages
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ChildResourceOfAttribute(Type parentControllerType) : Attribute
{
    /// <summary>The parent controller that owns this child resource.</summary>
    public Type ParentControllerType { get; } = parentControllerType
        ?? throw new ArgumentNullException(nameof(parentControllerType));

    /// <summary>
    /// The route parameter name for the parent's ID.
    /// If not set, auto-derived from the parent controller name:
    /// <c>SimCardsController → simCardsId</c>.
    /// </summary>
    public string? ParentIdParam { get; init; }

    /// <summary>
    /// Whether to apply :guid constraint on the parent ID parameter. Defaults to <c>true</c>.
    /// </summary>
    public bool GuidConstraint { get; init; } = true;
}
