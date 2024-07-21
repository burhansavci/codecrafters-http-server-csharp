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
    var socket = await listenSocket.AcceptAsync();

    // We got a new connection spawn a task to so that we can echo the contents of the connection
    _ = Task.Run(async () =>
    {
        var buffer = new byte[4096];

        int read = await socket.ReceiveAsync(buffer);

        var message = Encoding.UTF8.GetString(buffer[..read]);
        var httpRequestParts = message.Split("\r\n");
        var requestLine = httpRequestParts[0];
        var requestHeaders = httpRequestParts[1..^2];
        var requestBody = httpRequestParts[^1];

        var requestTarget = requestLine.Split(" ")[1];

        HttpResponse responseMessage;
        var routes = requestTarget.Split("/", StringSplitOptions.RemoveEmptyEntries);
        if (routes.Length == 0)
        {
            responseMessage = new HttpResponse(HttpStatus.Ok, string.Empty);
        }
        else if (routes.Contains("echo"))
        {
            var content = routes.Last();
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Content-Length", content.Length.ToString() }
            };
            responseMessage = new HttpResponse(HttpStatus.Ok, content, headers);
        }
        else if (routes.Contains("user-agent"))
        {
            var userAgent = requestHeaders
                .Select(header => header.Split(": "))
                .First(header => header[0] == "User-Agent")[1];

            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Content-Length", userAgent.Length.ToString() }
            };

            responseMessage = new HttpResponse(HttpStatus.Ok, userAgent, headers);
        }
        else
        {
            responseMessage = new HttpResponse(HttpStatus.NotFound);
        }

        await socket.SendAsync(Encoding.UTF8.GetBytes(responseMessage.ToString()));
        socket.Dispose();
    });
}

record HttpResponse(HttpStatus Status, string? Content = null, Dictionary<string, string>? Headers = null)
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
        sb.Append(Content);

        return sb.ToString();
    }
}

record HttpStatus(int Code, string ReasonPhrase)
{
    public static HttpStatus Ok => new(200, "OK");
    public static HttpStatus NotFound => new(404, "Not Found");
    public override string ToString() => $"{Code} {ReasonPhrase}";
}