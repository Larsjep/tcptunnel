using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class Forwarder
    {
        private Thread reader_thread;
        public event Action connection_lost = delegate { };
        public Forwarder(TcpClient input, TcpClient output)
        {
            reader_thread = new Thread(() => { do_read(input, output); });
            reader_thread.Start();
        }

        private void do_read(TcpClient input, TcpClient output)
        {
            string input_ep = input.Client.RemoteEndPoint.ToString();
            string output_ep = output.Client.RemoteEndPoint.ToString();
            try
            {
                var ns_input = input.GetStream();
                var ns_output = output.GetStream();
                const int buf_size = 1000000;
                var buffer = new byte[buf_size];
                Console.WriteLine(string.Format("Reading from {0}", input.Client.RemoteEndPoint));
                while (true)
                {
                    var read_count = ns_input.Read(buffer, 0, buf_size);
                    if (read_count == 0)
                    {
                        connection_lost();
                        return;
                    }
                    // Console.WriteLine(string.Format("Got Data {0}, from {1}, to {2}", read_count, input.Client.RemoteEndPoint, output.Client.RemoteEndPoint));
                    ns_output.Write(buffer, 0, read_count);
                }
            } catch (System.IO.IOException)
            {
                connection_lost();
            }
            Console.WriteLine(string.Format("Connection lost between {0} and {1}", input_ep, output_ep));
        }
    }
}
