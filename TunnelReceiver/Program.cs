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
        static readonly int keepalive_interval_minutes = 5;
        static void Worker()
        {
            while (true)
            {
                var tunnel_listener = new TcpListener(IPAddress.Any, 41111);
                Console.WriteLine("Tunnel listening");
                tunnel_listener.Start();
                var tunnel = tunnel_listener.AcceptTcpClient();
                Console.WriteLine("Tunnel connected");

                var rdp_listener = new TcpListener(IPAddress.Any, 3389);
                Console.WriteLine("Ready for RDP connection");
                rdp_listener.Start();
                var tunnel_stream = tunnel.GetStream();

                var keep_alive_timer = new Timer((object obj) =>
                    {
                        tunnel_stream.Write(new byte[1] { Common.Constants.keealive_signal }, 0, 1);
                    },
                    null,
                    1000 * 60 * keepalive_interval_minutes,
                    1000 * 60 * keepalive_interval_minutes
                    );
                try
                {
                    using (var rdp = rdp_listener.AcceptTcpClient())
                    {
                        Console.WriteLine("RDP Connected");
                        // Stop the keepalive timer
                        EventWaitHandle timer_stopped = new AutoResetEvent(false);
                        keep_alive_timer.Dispose(timer_stopped);
                        timer_stopped.WaitOne();
                        tunnel_stream.Write(new byte[1] { Common.Constants.start_signal }, 0, 1);

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
            Thread worker = new Thread(Worker);
            Console.WriteLine("Stop by pressing ESC");
            worker.Start();

            do
            {
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}
