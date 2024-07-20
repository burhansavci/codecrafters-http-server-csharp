using System.Net;
using System.Net.Sockets;
using System.Text;

const string httpOkMessage = @"HTTP/1.1 200 OK\r\n\r\n";
const string httpNotFoundMessage = @"HTTP/1.1 404 Not Found\r\n\r\n";

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

var responseMessage = string.IsNullOrWhiteSpace(requestTarget) ? httpOkMessage : httpNotFoundMessage;

socket.Send(Encoding.UTF8.GetBytes(responseMessage));