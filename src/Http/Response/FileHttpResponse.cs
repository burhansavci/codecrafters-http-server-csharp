using System.Text;

namespace codecrafters_http_server.Http.Response;

internal record FileHttpResponse(HttpStatus Status, byte[] Body, Dictionary<string, string>? Headers = null) : IHttpResponse
{
    public byte[] Render()
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

        var response = Encoding.UTF8.GetBytes(sb.ToString());
        var responseWithBody = new byte[response.Length + Body.Length];
        response.CopyTo(responseWithBody, 0);
        Body.CopyTo(responseWithBody, response.Length);

        return responseWithBody;
    }
}