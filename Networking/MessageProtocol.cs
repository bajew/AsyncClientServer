using System;
using System.IO;
using System.Net;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Collections.Concurrent;

namespace Networking;

public enum HeaderType
{
    Int16,
    Int32,
}

public class MessageProtocol
{

    private readonly int HeaderLength;
    private readonly NetworkStream networkStream;
    private readonly HeaderType? headerType;


    private int ReadSize(BinaryReader reader)
    {
        return headerType switch
        {
            HeaderType.Int16 => IPAddress.NetworkToHostOrder(reader.ReadInt16()),
            HeaderType.Int32 => IPAddress.NetworkToHostOrder(reader.ReadInt32()),
            _ => reader.ReadInt32()
        };
    }
    private void WriteSize(BinaryWriter writer, int size)
    {
        switch (headerType)
        {
            case HeaderType.Int16:
                writer.Write(IPAddress.HostToNetworkOrder((short)size));
                break;
            case HeaderType.Int32:
                writer.Write(IPAddress.HostToNetworkOrder(size));
                break;
        };
    }
    public MessageProtocol(NetworkStream networkStream, HeaderType? type = null)
    {
        HeaderLength = type switch
        {
            HeaderType.Int16 => 2,
            HeaderType.Int32 => 4,
            _ => 4
        };
        this.networkStream = networkStream;
        this.headerType = type;
    }

    public async Task WriteMessageAsync(string message)
    {
        var bytes = EncodeMessage(message);
        await networkStream.WriteAsync(bytes, 0, bytes.Length);
    }

    // Encodes the message into a byte array where the length includes the 4 bytes of the header
    public byte[] EncodeMessage(string message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        // Convert the message to bytes
        byte[] bodyBytes = System.Text.Encoding.ASCII.GetBytes(message);

        int totalLength = HeaderLength + bodyBytes.Length; // Total length including header

        // Create a buffer for the header and the body
        byte[] messageBytes = new byte[totalLength];

        using (MemoryStream ms = new MemoryStream(messageBytes))
        {
            BinaryWriter writer = new BinaryWriter(ms);
            WriteSize(writer, totalLength);
        }

        // Copy the body bytes after the header
        Array.Copy(bodyBytes, 0, messageBytes, HeaderLength, bodyBytes.Length);

        return messageBytes;
    }



    public async Task<string> ReadMessageAsync()
    {
        byte[] headerBytes = new byte[HeaderLength];
        int bytesRead = 0;

        while (bytesRead < HeaderLength)
        {
            int n = await networkStream.ReadAsync(headerBytes, bytesRead, HeaderLength - bytesRead);
            if (n == 0) return null;
            bytesRead += n;
        }

        using (MemoryStream ms = new MemoryStream(headerBytes))
        {
            BinaryReader reader = new BinaryReader(ms);

            int totalLength = ReadSize(reader);

            if (totalLength < HeaderLength)
                throw new ArgumentException("Invalid message length.");

            byte[] messageBytes = new byte[totalLength];
            Array.Copy(headerBytes, messageBytes, HeaderLength);

            bytesRead = 0;


            while (bytesRead < totalLength - HeaderLength)
            {
                int n = await networkStream.ReadAsync(messageBytes, bytesRead + HeaderLength, totalLength - HeaderLength - bytesRead);
                if (n == 0) return null;
                bytesRead += n;
            }

            return DecodeMessage(messageBytes);
        }


    }
    public string DecodeMessage(byte[] messageBytes)
    {
        if (messageBytes == null || messageBytes.Length < HeaderLength)
            throw new ArgumentException("Invalid message format.");

        using (MemoryStream ms = new MemoryStream(messageBytes))
        {
            BinaryReader reader = new BinaryReader(ms);
            int totalLength = ReadSize(reader);

            if (messageBytes.Length != totalLength)
                throw new ArgumentException("Message length mismatch.");

            int bodyLength = totalLength - HeaderLength; // Subtract header length
            byte[] bodyBytes = reader.ReadBytes(bodyLength); // Read the body
            return System.Text.Encoding.ASCII.GetString(bodyBytes);
        }
    }


}
