namespace codecrafters_http_server.Http;

internal record HttpMethod(string Method)
{
    public static HttpMethod Get => new("GET");
    public static HttpMethod Post => new("POST");
    
    public override string ToString() => Method;
}