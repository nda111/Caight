using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
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
            bool offSwitch = true;

            Task.Run(() =>
            {
                int i = 0;
                while (offSwitch)
                {
                    System.Diagnostics.Debug.WriteLine(i++);

                    Thread.Sleep(1000);
                }
            });

            FileStream stream;
            string path = "test.txt";
            if (!File.Exists(path))
            {
                stream = File.Create(path);
            }
            else
            {
                stream = File.Open("test.txt", FileMode.Open, FileAccess.ReadWrite);
            }

            stream.Seek(0, SeekOrigin.End);
            var writer = new StreamWriter(stream);
            writer.WriteLine("Hello World!!!");
            writer.Flush();

            stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(stream);
            Console.WriteLine(reader.ReadToEnd());

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
