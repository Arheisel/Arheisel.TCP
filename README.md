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

## Reference
This library also includes the `TCPTools` Static Class. It has matching function for sending and receiving Data.

#### `T[] TCPTools.Concat<T>(this T[] x, T[] y)`

Extension Function

Returns an Array<T> with the `y` array concatenated at the end

#### `T[] TCPTools.Splice<T>(this T[] array, int startIndex, int length)`
Extension Function

Returns an Array<T> with `length` ammount of elements starting at `startIndex`

#### `void TCPTools.Send(TcpClient client, byte[] data)`

Sends a byte array. Received with `byte[] TCPTools.Receive(TcpClient client)`

#### `void TCPTools.SendString(TcpClient client, string str)`

Sends a string. Received with `string TCPTools.ReceiveString(TcpClient client)`

#### `void TCPTools.SendObject<T>(TcpClient client, T obj)`

Sends an instance of object T (via JsonConvert). Received with `T TCPTools.ReceiveObject<T>(TcpClient client)`

#### `void TCPTools.SendACK(TcpClient client)`

Not Recommended. Sends an "ACK" string. Received with `bool TCPTools.ReceiveACK(TcpClient client)`

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
                var obj = TCPTools.ReceiveObject<NetStr>(e.client);
                string smsg;
                if(obj == null)
                {
                    smsg = "NULL";
                }
                else
                    smsg = obj.Str;

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
                var obj = new NetStr(line);

                if (line == "a")//'a' sends null as a test
                    TCPTools.SendObject<NetStr>(client, null);
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
