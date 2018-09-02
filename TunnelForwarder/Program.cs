using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tcpforward
{
    class Program
    {
        static void Worker(string tunnel_host, string rdp_server)
        {
            while (true)
            {
                try {
                    using (var tunnel = new TcpClient())
                    {
                        tunnel.Connect(tunnel_host, 44444);
                        tunnel.Client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true );
                        Console.WriteLine("Tunnel Connected, waiting for start signal");
                        byte[] start_buffer = new byte[1];
                        tunnel.GetStream().Read(start_buffer, 0, 1);
                        if (start_buffer[0] != 0x77)
                        {
                            Console.WriteLine(string.Format("Got invalid start character {0} :( !!!", start_buffer[0]));
                            break;
                        }

                        using (var localrdp = new TcpClient())
                        {
                            localrdp.Connect(rdp_server, 3389);
                            Console.WriteLine("Rdp connected");
                            var f1 = new Common.Forwarder(tunnel, localrdp);
                            var f2 = new Common.Forwarder(localrdp, tunnel);

                            ManualResetEvent connection_lost = new ManualResetEvent(false);
                            f1.connection_lost += () => { connection_lost.Set(); };
                            f2.connection_lost += () => { connection_lost.Set(); };
                            connection_lost.WaitOne();
                        }
                    }
                }
                catch (System.IO.IOException)
                {
                }
            }
        }
        static void Main(string[] args)
        {
            Thread worker = new Thread(() => { Worker(args[0], args[1]); });
            Console.WriteLine("Stop by pressing ESC");
            worker.Start();
            do
            {
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}
