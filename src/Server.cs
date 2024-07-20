using System.Net;
using System.Net.Sockets;
using System.Text;

var server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Console.WriteLine("Server started");

var socket = server.AcceptSocket(); // wait for client
Console.WriteLine("Client connected");

var buffer = new byte[4096];

int read = socket.Receive(buffer);

var message = Encoding.UTF8.GetString(buffer[..read]);
var requestLine = message.Split("\r\n")[0];
var requestTarget = requestLine.Split(" ")[1];

var content = requestTarget.Split("/").Last();
var headers = new Dictionary<string, string>
{
    { "Content-Type", "text/plain" },
    { "Content-Length", content.Length.ToString() }
};
var responseMessage = new HttpResponse(HttpStatus.Ok, content, headers).ToString();

socket.Send(Encoding.UTF8.GetBytes(responseMessage));

record HttpResponse(HttpStatus Status, string Content, Dictionary<string, string> Headers)
{
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
        sb.Append(Content);

        return sb.ToString();
    }
}


record HttpStatus(int Code, string ReasonPhrase)
{
    public static HttpStatus Ok => new(200, "OK");
    public override string ToString() => $"{Code} {ReasonPhrase}";
}