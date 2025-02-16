using Networking;

Client client = new Client(HeaderType.Int16);

await client.Start("localhost", 12345);


Console.WriteLine("Exiting...");
Console.ReadLine();