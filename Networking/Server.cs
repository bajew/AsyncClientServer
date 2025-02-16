using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class Server
    {
        MessageProtocol? messageProtocol;
        private readonly HeaderType headerType;

        public Server(HeaderType? headerType = null)
        {
            this.headerType = headerType ?? HeaderType.Int32;
        }
        public async Task Start(int port)
        {
            TcpListener serverSocket = new TcpListener(IPAddress.Any, port);
            serverSocket.Start();

            Console.WriteLine("Server started. Listening for connections...");

            try
            {
                while (true)
                {
                    TcpClient clientSocket = await serverSocket.AcceptTcpClientAsync();
                    _ = HandleConnectionAsync(clientSocket); // Start a new task to handle the connection
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        private async Task HandleConnectionAsync(TcpClient clientSocket)
        {

            try
            {
                messageProtocol = new MessageProtocol(clientSocket.GetStream(), headerType);
                while (true)
                {
                    string receivedMessage = await messageProtocol.ReadMessageAsync();
                    if (receivedMessage == null) break; // Connection closed by client

                    Console.WriteLine("Received: " + receivedMessage);

                    // Send a response back to the client
                    await messageProtocol.WriteMessageAsync(receivedMessage);
                }

                Console.WriteLine("Client disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                clientSocket.Close();
            }
        }



    }
}
