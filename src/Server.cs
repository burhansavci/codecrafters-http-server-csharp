using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

var endpointBuilder = new EndpointBuilder();

endpointBuilder.Get("", _ => Task.FromResult(new HttpResponse(HttpStatus.Ok, string.Empty)));

endpointBuilder.Get("echo/{str}", async request =>
{
    var content = request.Route.Last();

    var headers = new Dictionary<string, string>
    {
        { "Content-Type", "text/plain" },
    };

    var encodingTypes = request.Headers.GetValueOrDefault("Accept-Encoding", string.Empty).Split(", ", StringSplitOptions.RemoveEmptyEntries);

    if (encodingTypes.Contains("gzip"))
    {
        headers.Add("Content-Encoding", "gzip");

        var buffer = new byte[4 * 1024];
        using var compressedContentStream = new MemoryStream();
        await using (var compressor = new GZipStream(compressedContentStream, CompressionLevel.Fastest, true))
            await compressor.WriteAsync(buffer);

        content = BitConverter.ToString(compressedContentStream.ToArray());
    }

    headers.Add("Content-Length", content.Length.ToString());

    return new HttpResponse(HttpStatus.Ok, content, headers);
});

endpointBuilder.Get("user-agent", request =>
{
    var userAgent = request.Headers.GetValueOrDefault("User-Agent", string.Empty);

    var headers = new Dictionary<string, string>
    {
        { "Content-Type", "text/plain" },
        { "Content-Length", userAgent.Length.ToString() }
    };

    return Task.FromResult(new HttpResponse(HttpStatus.Ok, userAgent, headers));
});

endpointBuilder.Get("files/{fileName}", async request =>
{
    var filesDirectory = args.Last();
    var fileName = request.Route.Last();
    var filePath = Path.Combine(filesDirectory, fileName);

    if (!File.Exists(filePath))
    {
        return new HttpResponse(HttpStatus.NotFound);
    }

    var content = await File.ReadAllTextAsync(filePath);

    var headers = new Dictionary<string, string>
    {
        { "Content-Type", "application/octet-stream" },
        { "Content-Length", content.Length.ToString() }
    };

    return new HttpResponse(HttpStatus.Ok, content, headers);
});

endpointBuilder.Post("files/{fileName}", async request =>
{
    var body = request.Body;

    var filesDirectory = args.Last();
    var fileName = request.Route.Last();
    var filePath = Path.Combine(filesDirectory, fileName);

    await File.WriteAllTextAsync(filePath, body);
    return new HttpResponse(HttpStatus.Created);
});

var routes = EndpointBuilder.Build();

using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 4221));

Console.WriteLine($"Listening on {listenSocket.LocalEndPoint}");

listenSocket.Listen();
while (true)
{
    // Wait for a new connection to arrive
    var connection = await listenSocket.AcceptAsync();

    // We got a new connection spawn a task to so that we can echo the contents of the connection
    _ = Task.Run(async () => await HandleConnectionAsync(connection));
}

async Task HandleConnectionAsync(Socket connection)
{
    var buffer = new byte[4 * 1024];
    try
    {
        int read = await connection.ReceiveAsync(buffer);

        var request = GetHttpRequest(buffer[..read]);

        var routePattern = EndpointBuilder.FindRoutePattern(request.RequestTarget, request.Method);

        var requestHandler = routePattern is not null
            ? routes.GetValueOrDefault(routePattern.Value, _ => Task.FromResult(new HttpResponse(HttpStatus.NotFound)))
            : _ => Task.FromResult(new HttpResponse(HttpStatus.NotFound));

        var response = await requestHandler(request);

        await connection.SendAsync(Encoding.UTF8.GetBytes(response.ToString()));
    }
    finally
    {
        connection.Dispose();
    }
}

HttpRequest GetHttpRequest(byte[] messageBuffer)
{
    var message = Encoding.UTF8.GetString(messageBuffer);

    var httpRequestParts = message.Split("\r\n");
    var requestLine = httpRequestParts[0];
    var requestHeaders = httpRequestParts[1..^2];
    var requestBody = httpRequestParts[^1];

    var requestLineParts = requestLine.Split(" ");
    var method = requestLineParts[0];
    var requestTarget = requestLineParts[1];
    var httpVersion = requestLineParts[2];

    var routeSections = requestTarget.Split("/", StringSplitOptions.RemoveEmptyEntries);

    var headers = requestHeaders
        .Select(header => header.Split(": "))
        .ToDictionary(header => header[0], header => header[1]);

    return new HttpRequest(new HttpMethod(method), requestTarget, routeSections, httpVersion, headers, requestBody);
}

record HttpRequest(HttpMethod Method, string RequestTarget, string[] Route, string HttpVersion, Dictionary<string, string> Headers, string Body);

record HttpResponse(HttpStatus Status, string? Body = null, Dictionary<string, string>? Headers = null)
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("HTTP/1.1 ");
        sb.Append(Status);
        sb.Append("\r\n");

        if (Headers != null)
        {
            foreach (var (key, value) in Headers)
            {
                sb.Append(key);
                sb.Append(": ");
                sb.Append(value);
                sb.Append("\r\n");
            }
        }

        sb.Append("\r\n");
        sb.Append(Body);

        return sb.ToString();
    }
}

record HttpMethod(string Method)
{
    public static HttpMethod Get => new("GET");
    public static HttpMethod Post => new("POST");
    public override string ToString() => Method;
}

record HttpStatus(int Code, string ReasonPhrase)
{
    public static HttpStatus Ok => new(200, "OK");
    public static HttpStatus Created => new(201, "Created");
    public static HttpStatus NotFound => new(404, "Not Found");

    public override string ToString() => $"{Code} {ReasonPhrase}";
}

record RoutePattern(string Route)
{
    private const string SeparatorString = "/";

    public string[] RoutePatternSegments => Route.Split(SeparatorString, StringSplitOptions.RemoveEmptyEntries).ToArray();

    public static implicit operator RoutePattern(string route) => new(route);
}


class EndpointBuilder
{
    private static readonly Dictionary<(RoutePattern route, HttpMethod method), Func<HttpRequest, Task<HttpResponse>>> Routes = new();

    public EndpointBuilder Get(string route, Func<HttpRequest, Task<HttpResponse>> handler)
    {
        Routes.Add((route, HttpMethod.Get), handler);
        return this;
    }

    public EndpointBuilder Post(string route, Func<HttpRequest, Task<HttpResponse>> handler)
    {
        Routes.Add((route, HttpMethod.Post), handler);
        return this;
    }

    public static Dictionary<(RoutePattern route, HttpMethod method), Func<HttpRequest, Task<HttpResponse>>> Build()
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