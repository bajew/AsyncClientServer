
using Networking;

Server server = new Server(HeaderType.Int16);

await server.Start(12345);

Console.WriteLine("Exiting...");
Console.ReadLine();