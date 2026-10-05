using ArticleService.Models;

namespace ArticleService.Controllers;

/// <summary>
/// Makes "{region:region}" in a route match only real region names (case-insensitive),
/// so an unknown region like "atlantis" gives 404 Not Found instead of an error.
/// </summary>
public class RegionRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        var value = values[routeKey]?.ToString();
        return Enum.GetNames<Region>().Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}
