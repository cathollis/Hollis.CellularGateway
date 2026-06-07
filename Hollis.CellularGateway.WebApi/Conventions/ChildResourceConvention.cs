using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Hollis.CellularGateway.WebApi.Conventions;

/// <summary>
/// Automatically rewrites child-controller routes into nested form:
/// <c>{parentRoute}/{parentIdParam}/[controller]</c>.
/// </summary>
public class ChildResourceConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var childController in application.Controllers)
        {
            var childAttr = childController.Attributes
                .OfType<ChildResourceOfAttribute>()
                .FirstOrDefault();
            if (childAttr is null)
                continue;

            var parentController = FindParentController(application, childAttr.ParentControllerType);
            if (parentController is null)
                continue;

            var parentRoute = ExtractParentRoute(parentController);
            var parentIdParam = childAttr.ParentIdParam ?? DeriveParentIdParam(childAttr.ParentControllerType);
            var constraint = childAttr.GuidConstraint ? ":guid" : "";
            var childRoute = $"{parentRoute}/{{{parentIdParam}{constraint}}}/[controller]";

            // Replace the child controller's route
            var selector = childController.Selectors.FirstOrDefault();
            if (selector is null)
            {
                selector = new SelectorModel();
                childController.Selectors.Add(selector);
            }

            selector.AttributeRouteModel = new AttributeRouteModel { Template = childRoute };
        }
    }

    /// <summary>
    /// Finds the parent controller in the application model by type.
    /// </summary>
    private static ControllerModel? FindParentController(ApplicationModel application, Type parentType)
    {
        return application.Controllers
            .FirstOrDefault(c => c.ControllerType == parentType);
    }

    /// <summary>
    /// Extracts the route template from the parent controller's <c>[Route]</c> attribute.
    /// </summary>
    private static string ExtractParentRoute(ControllerModel parentController)
    {
        var routeAttr = parentController.Attributes
            .OfType<RouteAttribute>()
            .FirstOrDefault();

        if (routeAttr is not null)
            return routeAttr.Template;

        // Fallback: use [controller] convention — this is what ASP.NET Core does by default
        var name = parentController.ControllerName; // e.g. "SimCards"
        return name;
    }

    /// <summary>
    /// Derives the parent ID parameter name from the parent controller type:
    /// <c>SimCardsController → simCardsId</c>.
    /// </summary>
    private static string DeriveParentIdParam(Type parentType)
    {
        // SimCardsController → SimCards
        var name = parentType.Name;
        if (name.EndsWith("Controller", StringComparison.Ordinal))
            name = name[..^"Controller".Length];

        // SimCards → simCards
        if (name.Length > 0)
            name = char.ToLowerInvariant(name[0]) + name[1..];

        return $"{name}Id";
    }
}
