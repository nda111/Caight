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
using Npgsql;

namespace Caight
{
    public class Startup
    {
        private NpgsqlConnection conn = new NpgsqlConnection();

        public Startup(IConfiguration configuration)
        {
            conn.ConnectionString =
                "HOST=54.80.184.43;" +
                "PORT=5432;" +
                "USERNAME=chwnhsjrvwjcmn;" +
                "PASSWORD=27f3f0355524328a1608b7f408b39c45dc08686f29c385b38705a4268964bdfd;" +
                "DATABASE=da7sfef764j2vr";
            conn.Open();

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
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string[] cmdString = Encoding.UTF8.GetString(buffer).Split('\0');

                switch (cmdString[0].ToUpper())
                {
                    case "INSERT":
                        using (var cmd = new NpgsqlCommand($"INSERT INTO {cmdString[1]} VALUES('{cmdString[2]}');") { Connection = conn })
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (var memStream = new MemoryStream(buffer))
                        {
                            var writer = new StreamWriter(memStream);
                            writer.Write("OK\0");
                            writer.Flush();
                        }

                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        break;

                    default:
                        break;
                }

                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            Console.WriteLine("Close");
        }
    }
}
