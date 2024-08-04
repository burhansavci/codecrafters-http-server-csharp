namespace codecrafters_http_server.Http;

internal record HttpStatus(int Code, string ReasonPhrase)
{
    public static HttpStatus Ok => new(200, "OK");
    public static HttpStatus Created => new(201, "Created");
    public static HttpStatus NotFound => new(404, "Not Found");

    public override string ToString() => $"{Code} {ReasonPhrase}";
}