namespace codecrafters_http_server.Http.Response;

internal interface IHttpResponse
{
    byte[] Render();
}