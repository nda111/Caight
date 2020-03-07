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

            FileStream stream = File.Create("test.txt");
            stream.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            stream.Close();

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
