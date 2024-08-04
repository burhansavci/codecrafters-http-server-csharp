using codecrafters_http_server.Http.Request;
using codecrafters_http_server.Http.Response;

namespace codecrafters_http_server.Http.Server;

internal class EndpointBuilder
{
    private static readonly Dictionary<(RoutePattern route, HttpMethod method), Func<HttpRequest, Task<IHttpResponse>>> Routes = new();

    public EndpointBuilder Get(string route, Func<HttpRequest, Task<IHttpResponse>> handler)
    {
        Routes.Add((route, HttpMethod.Get), handler);
        return this;
    }

    public EndpointBuilder Post(string route, Func<HttpRequest, Task<IHttpResponse>> handler)
    {
        Routes.Add((route, HttpMethod.Post), handler);
        return this;
    }

    public Dictionary<(RoutePattern route, HttpMethod method), Func<HttpRequest, Task<IHttpResponse>>> Build()
    {
        return Routes;
    }

    public static (RoutePattern route, HttpMethod method)? FindRoutePattern(string requestTarget, HttpMethod method)
    {
        var routePattern = new RoutePattern(requestTarget);

        var existingRoutePatterns = Routes.Keys.ToArray();

        var matchingRoutePattern = existingRoutePatterns.FirstOrDefault(existingRoutePattern =>
        {
            var existingRoutePatternSegments = existingRoutePattern.route.RoutePatternSegments;
            var routePatternSegments = routePattern.RoutePatternSegments;

            if (existingRoutePatternSegments.Length != routePatternSegments.Length)
                return false;

            if (existingRoutePattern.method != method)
                return false;

            for (var i = 0; i < existingRoutePatternSegments.Length; i++)
            {
                if (existingRoutePatternSegments[i] != routePatternSegments[i] && !existingRoutePatternSegments[i].StartsWith("{") && !existingRoutePatternSegments[i].EndsWith("}"))
                {
                    return false;
                }
            }

            return true;
        });

        return matchingRoutePattern;
    }
}