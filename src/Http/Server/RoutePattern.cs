namespace codecrafters_http_server.Http.Server;

internal record RoutePattern(string Route)
{
    private const string SeparatorString = "/";

    public string[] RoutePatternSegments => Route.Split(SeparatorString, StringSplitOptions.RemoveEmptyEntries).ToArray();

    public static implicit operator RoutePattern(string route) => new(route);
}