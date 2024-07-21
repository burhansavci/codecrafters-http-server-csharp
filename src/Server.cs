using System.Net;
using System.Net.Sockets;
using System.Text;

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
    var buffer = new byte[4096];
    try
    {
        int read = await connection.ReceiveAsync(buffer);

        var request = GetHttpRequest(buffer[..read]);

        HttpResponse response;

        if (request.Route.Length == 0)
        {
            response = new HttpResponse(HttpStatus.Ok, string.Empty);
        }
        else if (request.Route.Contains("echo"))
        {
            var content = request.Route.Last();
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Content-Length", content.Length.ToString() }
            };
            response = new HttpResponse(HttpStatus.Ok, content, headers);
        }
        else if (request.Route.Contains("user-agent"))
        {
            var userAgent = request.Headers.GetValueOrDefault("User-Agent", string.Empty);

            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Content-Length", userAgent.Length.ToString() }
            };

            response = new HttpResponse(HttpStatus.Ok, userAgent, headers);
        }
        else if (request.Route.Contains("files"))
        {
            var filesDirectory = "/tmp/";
            var fileName = request.Route.Last();
            var filePath = Path.Combine(filesDirectory, fileName);

            if (!File.Exists(filePath))
            {
                response = new HttpResponse(HttpStatus.NotFound);
            }
            else
            {
                var content = await File.ReadAllTextAsync(filePath);

                var headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/octet-stream" },
                    { "Content-Length", content.Length.ToString() }
                };

                response = new HttpResponse(HttpStatus.Ok, content, headers);
            }
        }
        else
        {
            response = new HttpResponse(HttpStatus.NotFound);
        }

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

    var routes = requestTarget.Split("/", StringSplitOptions.RemoveEmptyEntries);

    var headers = requestHeaders
        .Select(header => header.Split(": "))
        .ToDictionary(header => header[0], header => header[1]);

    return new HttpRequest(method, requestTarget, routes, httpVersion, headers, requestBody);
}

record HttpRequest(string Method, string RequestTarget, string[] Route, string HttpVersion, Dictionary<string, string> Headers, string Body);

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

record HttpStatus(int Code, string ReasonPhrase)
{
    public static HttpStatus Ok => new(200, "OK");
    public static HttpStatus NotFound => new(404, "Not Found");
    public override string ToString() => $"{Code} {ReasonPhrase}";
}