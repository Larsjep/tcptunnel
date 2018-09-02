using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TunnelReceiver
{
    class Program
    {
        static void Worker()
        {
            while (true)
            {
                var tunnel_listener = new TcpListener(IPAddress.Any, 41111);
                Console.WriteLine("Tunnel listening");
                tunnel_listener.Start();
                var tunnel = tunnel_listener.AcceptTcpClient();
                tunnel.Client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true );
                Console.WriteLine("Tunnel connected");

                var rdp_listener = new TcpListener(IPAddress.Any, 3389);
                Console.WriteLine("Ready for RDP connection");
                rdp_listener.Start();
                try
                {
                    using (var rdp = rdp_listener.AcceptTcpClient())
                    {
                        Console.WriteLine("RDP Connected");
                        rdp.Client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true );
                        tunnel.GetStream().Write(new byte[1] { 0x77 }, 0, 1);

                        Console.WriteLine("Starting data forwarding");
                        var f1 = new Common.Forwarder(tunnel, rdp);
                        var f2 = new Common.Forwarder(rdp, tunnel);
                        ManualResetEvent connection_lost = new ManualResetEvent(false);
                        f1.connection_lost += () => { connection_lost.Set(); };
                        f2.connection_lost += () => { connection_lost.Set(); };
                        connection_lost.WaitOne();
                    }
                }
                catch (System.IO.IOException)
                {
                }
                finally
                {
                    tunnel_listener.Stop();
                    rdp_listener.Stop();
                    tunnel.Close();
                }
            }
        }


        static void Main(string[] args)
        {
            /*            EventWaitHandle stop = new ManualResetEvent(false);

                        (new Thread(() =>
                        {
                            
                        })).Start();
                        */
            Thread worker = new Thread(Worker);
            Console.WriteLine("Stop by pressing ESC");
            worker.Start();

            do
            {
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}
