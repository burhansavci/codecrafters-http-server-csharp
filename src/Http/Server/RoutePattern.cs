namespace codecrafters_http_server.Http.Server;

internal record RoutePattern(string Route)
{
    private const string SeparatorString = "/";
    private const string RouteParameterStart = "{";
    private const string RouteParameterEnd = "}";

    public string[] RoutePatternSegments => Route.Split(SeparatorString, StringSplitOptions.RemoveEmptyEntries).ToArray();

    public static implicit operator RoutePattern(string route) => new(route);

    public bool IsMatch(RoutePattern target)
    {
        var targetSegments = target.RoutePatternSegments;

        if (RoutePatternSegments.Length != targetSegments.Length)
            return false;

        for (var i = 0; i < RoutePatternSegments.Length; i++)
        {
            if (IsParameterSegment(RoutePatternSegments[i]))
                continue;

            if (!RoutePatternSegments[i].Equals(targetSegments[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static bool IsParameterSegment(string segment)
    {
        return segment.StartsWith(RouteParameterStart) && segment.EndsWith(RouteParameterEnd);
    }
}