using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;

namespace Arheisel.TCP
{
    public class TCPServer
    {
        private TcpListener server;
        private Thread listenerThread;

        public event EventHandler<ClientConnectedArgs> ClientConnected;

        public TCPServer(IPAddress ip, int port)
        {
            server = new TcpListener(ip, port);
            listenerThread = new Thread(new ThreadStart(RunServer));
        }

        public void Start()
        {
            listenerThread.Start();
        }

        public void Stop()
        {
            try
            {
                listenerThread.Abort();
            }
            catch { }
        }

        private void RunServer()
        {
            server.Start();
            while (true)
            {
                var client = server.AcceptTcpClient();
                Task.Run(() => HandleClient(client));
            }
        }

        private void HandleClient(TcpClient client)
        {
            client.NoDelay = true;
            client.Client.NoDelay = true;
            ClientConnected?.Invoke(this, new ClientConnectedArgs(client));
        }
    }

    public class ClientConnectedArgs : EventArgs
    {
        public TcpClient Client { get; }

        public ClientConnectedArgs(TcpClient _client)
        {
            Client = _client;
        }
    }

    public static class TCPTools
    {
        public static T[] Concat<T>(this T[] x, T[] y)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");
            int oldLen = x.Length;
            Array.Resize<T>(ref x, x.Length + y.Length);
            Array.Copy(y, 0, x, oldLen, y.Length);
            return x;
        }

        public static T[] Splice<T>(this T[] array, int startIndex, int length)
        {
            var ret = new T[length];
            Array.Copy(array, startIndex, ret, 0, length);
            return ret;
        }


        public static void Send(TcpClient client, byte[] data)
        {
            if (data.Length > 4096)
                SendMultipart(client, data);
            else
                Send(client.GetStream(), data);
        }

        private static void Send(NetworkStream ns, byte[] data, bool multipart = false)
        {
            var header = new byte[] { 252, (byte)(multipart ? 1:0) }; //Sync Byte

            var len = BitConverter.GetBytes(Convert.ToInt16(data.Length));
            if (!BitConverter.IsLittleEndian) //Make sure is Little Endian
            {
                Array.Reverse(len);
            }
            header = header.Concat(len);
            data = header.Concat(data);

            ns.Write(data, 0, data.Length);
        }

        public static byte[] Receive(TcpClient client)
        {
            var ns = client.GetStream();
            DelayTimeout(ns);
            if(ns.ReadByte() == 252)
            {
                DelayTimeout(ns);
                var multipart = ns.ReadByte() == 1;
                DelayTimeout(client, 2);
                var lenArray = new byte[2];
                ns.Read(lenArray, 0, 2);
                if (!BitConverter.IsLittleEndian) Array.Reverse(lenArray);
                var len = BitConverter.ToInt16(lenArray, 0);
                var buffer = new byte[len];
                DelayTimeout(client, len);
                ns.Read(buffer, 0, len);
                if (multipart)
                {
                    if (!BitConverter.IsLittleEndian) Array.Reverse(buffer);
                    return RecvMultipart(client, BitConverter.ToInt32(buffer, 0));
                }
                else
                    return buffer;
            }
            else
            {
                //discard all data
                var buffer = new byte[4096];
                while (ns.DataAvailable)
                {
                    ns.Read(buffer, 0, buffer.Length);
                }
                throw new Exception("Data out of Sync, recv buffer flushed");
            }
        }

        private static void DelayTimeout(NetworkStream ns)
        {
            var time = DateTime.Now;
            var diff = new TimeSpan(0, 0, 10);
            while (!ns.DataAvailable)
            {
                if ((DateTime.Now - time) > diff)
                {
                    throw new Exception("E_TCP_TIMEOUT: La conexion TCP excedio el tiempo de espera.");
                }
                Thread.Sleep(20);
            }
        }

        private static void DelayTimeout(TcpClient client, int length)
        {
            var time = DateTime.Now;
            var diff = new TimeSpan(0, 0, 10);
            while (client.Available < length)
            {
                if ((DateTime.Now - time) > diff)
                {
                    throw new Exception("E_TCP_TIMEOUT: La conexion TCP excedio el tiempo de espera.");
                }
                Thread.Sleep(20);
            }
        }

        private static void SendMultipart(TcpClient client, byte[] data)
        {
            var ns = client.GetStream();
            var len = BitConverter.GetBytes(data.Length);
            if (!BitConverter.IsLittleEndian) Array.Reverse(len);
            Send(ns, len, true);

            for(int i = 0; i < data.Length; i += 4000)
            {
                if(data.Length - i > 4000)
                {
                    var arr = data.Splice(i, 4000);
                    Send(ns, arr);
                }
                else
                {
                    var arr = data.Splice(i, data.Length - i);
                    Send(ns, arr);
                }
            }
        }

        private static byte[] RecvMultipart(TcpClient client, int length)
        {
            var buffer = new byte[0];
            int bytesReceived = 0;
            while(bytesReceived < length)
            {
                var data = Receive(client);
                bytesReceived += data.Length;
                buffer = buffer.Concat(data);
            }
            return buffer;
        }

        public static void SendString(TcpClient client, string str)
        {
            Send(client, Encoding.UTF8.GetBytes(str));
        }

        public static string ReceiveString(TcpClient client)
        {
            return Encoding.UTF8.GetString(Receive(client));
        }

        public static void SendObject<T>(TcpClient client, T obj)
        {
            var str = JsonConvert.SerializeObject(obj);
            SendString(client, str);
        }

        public static T ReceiveObject<T>(TcpClient client)
        {
            var str = ReceiveString(client);
            return JsonConvert.DeserializeObject<T>(str);
        }

        public static void SendACK(TcpClient client)
        {
            SendString(client, "ACK");
        }

        public static bool ReceiveACK(TcpClient client)
        {
            return ReceiveString(client) == "ACK";
        }
    }
}
