# Arheisel.TCP
Full TCP Server and Tool Library supporting Byte, String and Object transfers

# How to use
For the server side you just need to create a new instance of the Arheisel.TCP.TCPServer class

```csharp
webServer = new TCPServer(IPAddress.Loopback, Port);
webServer.ClientConnected += Server_ClientConnected;
webServer.Start();
```
The event includes the a `TCPClient` object
```csharp
private void WebServer_ClientConnected(object sender, ClientConnectedArgs e)
{
  //Your code here
}
```

The library also includes the `TCPTools` Class. It has matching function for sending and receiving Data.

## Example
(More of a program I made to test the library)

Server Code:
```csharp
class Program
{
    static void Main(string[] args)
    {
        var server = new TCPServer(IPAddress.Loopback, 9994);
        server.ClientConnected += Server_ClientConnected;
        server.Start();

        Console.WriteLine("Server Started");
        while (true) Console.ReadKey(true);
    }

    private static void Server_ClientConnected(object sender, ClientConnectedArgs e)
    {
        Console.WriteLine("Client Connected");

        var ns = e.client.GetStream();

        TCPTools.SendString(e.client, "Hello from " + Environment.MachineName);

        Console.WriteLine("Receiving File...");
        var file = TCPTools.Receive(e.client);
        ByteArrayToFile("pic.jpg", file);
        Console.WriteLine("Done.");

        while (e.client.Connected)  //while the client is connected, we look for incoming messages
        {
            if (ns.DataAvailable)
            {
                //var smsg = TCPTools.ReceiveString(e.client);
                var obj = TCPTools.ReceiveObject<List<NetStr>>(e.client);
                string smsg;
                if(obj == null)
                {
                    smsg = "NULL";
                }
                else
                    smsg = obj[0].Str;

                Console.WriteLine("Received: \"{0}\"", smsg); //now , we write the message as string
            }
            Thread.Sleep(20);
        }

        ns.Close();
        e.client.Close();
        Console.WriteLine("Client Disconnected");
    }

    private static bool ByteArrayToFile(string fileName, byte[] byteArray)
    {
        try
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(byteArray, 0, byteArray.Length);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception caught in process: {0}", ex);
            return false;
        }
    }
}

public class NetStr
{
    public string Str;

    public NetStr(string str)
    {
        Str = str;
    }
}
```
    
Client Code:
```csharp
class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Create a TcpClient.
            // Note, for this client to work you need to have a TcpServer 
            // connected to the same address as specified by the server, port
            // combination.
            TcpClient client = new TcpClient("localhost", 9994);
            NetworkStream stream = client.GetStream();
            // Receive the TcpServer.hello.

            Console.WriteLine("Received: {0}", TCPTools.ReceiveString(client));

            Console.WriteLine("Sending file...");
            var file = File.ReadAllBytes("pic.jpg");
            TCPTools.Send(client, file);
            Console.WriteLine("Done.");

            while (true)
            {
                var line = Console.ReadLine();

                if (line == "close") break;
                // Translate the passed message into ASCII and store it as a Byte array.
                var obj = new NetStr(line);
                var list = new List<NetStr>
                {
                    obj
                };
                //TCPTools.SendString(client, line);
                if (line == "a")
                    TCPTools.SendObject<List<NetStr>>(client, null);
                else
                    TCPTools.SendObject(client, list);
            }

            // Close everything.
            stream.Close();
            client.Close();
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("ArgumentNullException: {0}", e);
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
    }
}
```
