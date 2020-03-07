using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Caight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            FileStream stream;
            string path = "test.txt";
            if (!File.Exists(path))
            {
                stream = File.Create(path);

                var writer = new StreamWriter(stream);
                writer.WriteLine("Hello World!!!");
                writer.Flush();
            }
            else
            {
                stream = File.Open("test.txt", FileMode.Open, FileAccess.Read);
            }

            stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(stream);
            string msg = reader.ReadToEnd();
            stream.Close();

            bool offSwitch = true;
            Task.Run(() =>
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(new IPEndPoint(IPAddress.Any, 10101));
                server.Listen(10);

                while (offSwitch)
                {
                    Socket client = server.Accept();
                    using (var writer = new StreamWriter(new NetworkStream(client)))
                    {
                        writer.WriteLine(msg);
                    }
                }

                server.Close();
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
