namespace codecrafters_http_server.Http.Request;

internal record HttpRequest(HttpMethod Method, string RequestTarget, string[] Route, string HttpVersion, Dictionary<string, string> Headers, string Body);