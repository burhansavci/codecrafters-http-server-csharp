using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_http_server.Http.Request;
using codecrafters_http_server.Http.Response;

namespace codecrafters_http_server.Http.Server;

internal class HttpServer(EndpointBuilder endpointBuilder)
{
    private Dictionary<(RoutePattern route, HttpMethod method), Func<HttpRequest, Task<IHttpResponse>>> _routes = null!;

    public async Task StartAsync()
    {
        _routes = endpointBuilder.Build();

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
    }

    private async Task HandleConnectionAsync(Socket connection)
    {
        var buffer = new byte[4 * 1024];
        try
        {
            while (connection.Connected)
            {
                var read = await connection.ReceiveAsync(buffer);

                if (read <= 0) break;

                var request = GetHttpRequest(buffer[..read]);

                var routePattern = EndpointBuilder.FindRoutePattern(request.RequestTarget, request.Method);

                var requestHandler = _routes.GetValueOrDefault(routePattern.GetValueOrDefault(), _ => Task.FromResult<IHttpResponse>(new HttpResponse(HttpStatus.NotFound)));

                var response = await requestHandler(request);

                var shouldCloseConnection = ShouldCloseConnection(request);

                if (shouldCloseConnection && response is HttpResponse httpResponse)
                    httpResponse.Headers["Connection"] = "close";

                await connection.SendAsync(response.Render());

                if (shouldCloseConnection)
                    break;
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    private static HttpRequest GetHttpRequest(byte[] messageBuffer)
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

    private static bool ShouldCloseConnection(HttpRequest request) => request.Headers.TryGetValue("Connection", out var connectionHeader)
                                                                      && connectionHeader.Equals("close", StringComparison.OrdinalIgnoreCase);
}