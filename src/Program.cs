using System.IO.Compression;
using System.Text;
using codecrafters_http_server.Http;
using codecrafters_http_server.Http.Response;
using codecrafters_http_server.Http.Server;

var endpointBuilder = new EndpointBuilder();

endpointBuilder.Get("", _ => Task.FromResult<IHttpResponse>(new HttpResponse(HttpStatus.Ok)));

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

        using var compressedContentStream = new MemoryStream();
        await using (var compressor = new GZipStream(compressedContentStream, CompressionMode.Compress))
            await compressor.WriteAsync(Encoding.UTF8.GetBytes(content));

        var compressedContent = compressedContentStream.ToArray();

        headers.Add("Content-Length", compressedContent.Length.ToString());

        return new FileHttpResponse(HttpStatus.Ok, compressedContent, headers);
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

    return Task.FromResult<IHttpResponse>(new HttpResponse(HttpStatus.Ok, userAgent, headers));
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

var httpServer = new HttpServer(endpointBuilder);

await httpServer.StartAsync();