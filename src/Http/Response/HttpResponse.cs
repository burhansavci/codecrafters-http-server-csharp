using System.Text;

namespace codecrafters_http_server.Http.Response;

internal record HttpResponse(HttpStatus Status, string? Body = null, Dictionary<string, string>? Headers = null) : IHttpResponse
{
    public Dictionary<string, string> Headers { get; } = Headers ?? new Dictionary<string, string>();

    public byte[] Render()
    {
        return Encoding.UTF8.GetBytes(ToString());
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("HTTP/1.1 ");
        sb.Append(Status);
        sb.Append("\r\n");

        foreach (var (key, value) in Headers)
        {
            sb.Append(key);
            sb.Append(": ");
            sb.Append(value);
            sb.Append("\r\n");
        }

        sb.Append("\r\n");
        sb.Append(Body);

        return sb.ToString();
    }
}