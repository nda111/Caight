using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.WebSockets;

namespace Caight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool offSwitch = true;
            Task.Run(async () =>
            {
                Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Any, 10101));

                socket.Listen(10);
                while (offSwitch)
                {
                    using (Socket client = await socket.AcceptAsync())
                    {
                        using (var writer = new StreamWriter(new NetworkStream(client)))
                        {
                            writer.WriteLine("Hello World!!!");
                        }

                        client.Close();
                    }
                }
            });

            CreateHostBuilder(args).Build().Run();
            offSwitch = false;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
