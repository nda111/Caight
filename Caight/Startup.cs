using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Caight
{
    public class Startup
    {
        private string message = null;
        public Startup(IConfiguration configuration)
        {
            string path = "test.txt";
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Write))
                {
                    var writer = new StreamWriter(stream);
                    writer.WriteLine(0);
                    writer.Flush();
                }
            }

            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
            {
                var reader = new StreamReader(stream);
                int cnt = int.Parse(reader.ReadLine()) + 1;

                stream.Seek(0, SeekOrigin.Begin);
                var writer = new StreamWriter(stream);
                writer.WriteLine(cnt);
                writer.Flush();

                message = cnt + "\0";
            }

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
                        await Response(context, socket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        private async Task Response(HttpContext context, WebSocket socket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = null;// TODO: FIlesystem test

            while (!socket.CloseStatus.HasValue)
            {
                using (var memStream = new MemoryStream(buffer))
                {
                    var writer = new StreamWriter(memStream, Encoding.UTF8);
                    writer.Write(message);
                    writer.Flush();
                }

                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("Send: " + message);

                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            Console.WriteLine("Close");
        }
    }
}
