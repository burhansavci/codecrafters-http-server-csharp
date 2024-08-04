using codecrafters_http_server.Http.Request;
using codecrafters_http_server.Http.Response;

namespace codecrafters_http_server.Http.Server;

internal class EndpointBuilder
{
    private static readonly Dictionary<(RoutePattern route, HttpMethod method), Func<HttpRequest, Task<IHttpResponse>>> Routes = new();

    public EndpointBuilder Get(string route, Func<HttpRequest, Task<IHttpResponse>> handler)
    {
        AddRoute(route, HttpMethod.Get, handler);
        return this;
    }

    public EndpointBuilder Post(string route, Func<HttpRequest, Task<IHttpResponse>> handler)
    {
        AddRoute(route, HttpMethod.Post, handler);
        return this;
    }

    public Dictionary<(RoutePattern route, HttpMethod method), Func<HttpRequest, Task<IHttpResponse>>> Build()
    {
        return Routes;
    }

    public static (RoutePattern route, HttpMethod method)? FindRoutePattern(string requestTarget, HttpMethod method)
    {
        var routePatternToFind = new RoutePattern(requestTarget);

        foreach (var (routePattern, routeMethod) in Routes.Keys)
        {
            if (routeMethod != method)
                continue;

            if (routePattern.IsMatch(routePatternToFind))
                return (routePattern, routeMethod);
        }

        return null;
    }
    
    private static void AddRoute(string route, HttpMethod method, Func<HttpRequest, Task<IHttpResponse>> handler)
    {
        var routePattern = new RoutePattern(route);
        var key = (routePattern, method);

        if (!Routes.TryAdd(key, handler))
        {
            throw new ArgumentException($"A route already exists for {method} {route}");
        }
    }
}