using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class Client
    {
        MessageProtocol? messageProtocol;
        private readonly HeaderType headerType;
        public ConcurrentQueue<string> MessageQueue = new ConcurrentQueue<string>();
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public Client(HeaderType? headerType = null)
        {
            this.headerType = headerType ?? HeaderType.Int32;
        }

        public async Task Start(string ServerIp, int Port)
        {
            try
            {
                using (TcpClient clientSocket = new TcpClient())
                {
                    await clientSocket.ConnectAsync(ServerIp, Port);
                    messageProtocol = new MessageProtocol(clientSocket.GetStream(), headerType);

                    // Encode the message and send it to the server
                    await messageProtocol.WriteMessageAsync("Hello from client!(1)");
                    await messageProtocol.WriteMessageAsync("Hello from client!(2)");
                    await messageProtocol.WriteMessageAsync("Hello from client!(3)");
                    await messageProtocol.WriteMessageAsync("Hello from client!(4)");

                    _ = StartMessageQueue();
                    Console.WriteLine("Message sent to server");

                    while (MessageQueue.Count < 4)
                    {
                        await Task.Delay(100);
                    }

                    // Read the response from the server
                    while (MessageQueue.TryDequeue(out var message))
                    {
                        Console.WriteLine("Received: " + message);
                    }
                    StopMessageQueue();


                    Console.WriteLine("Connection closed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public Task StartMessageQueue()
        {
            cancellationTokenSource = new CancellationTokenSource();
            return Task.Run(ReadFromNetworkAndWriteToQueue, cancellationTokenSource.Token);
        }

        public void StopMessageQueue()
        {
            cancellationTokenSource?.Cancel();
        }

        private async Task ReadFromNetworkAndWriteToQueue()
        {
            while (!cancellationTokenSource.IsCancellationRequested && messageProtocol != null)
            {
                var msg = await messageProtocol.ReadMessageAsync();
                MessageQueue.Enqueue(msg);
            }
        }
    }
}
