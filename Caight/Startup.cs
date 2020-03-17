using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Odbc;
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
        private NpgsqlConnection dbConn = new NpgsqlConnection();

        public Startup(IConfiguration configuration)
        {
            NpgsqlConnectionStringBuilder connBuilder = new NpgsqlConnectionStringBuilder()
            {
                {
                    "Server",
                    "ec2-54-80-184-43.compute-1.amazonaws.com"
                },
                {
                    "Port",
                    "5432"
                },
                {
                    "Database",
                    "da7sfef764j2vr"
                },
                {
                    "Uid",
                    "chwnhsjrvwjcmn"
                },
                {
                    "Pwd",
                    "27f3f0355524328a1608b7f408b39c45dc08686f29c385b38705a4268964bdfd"
                },
                {
                    "SSL Mode",
                    "Prefer"
                },
                {
                    "Trust Server Certificate",
                    "true"
                },
            };
            dbConn.ConnectionString = connBuilder.ToString();
            dbConn.Open();

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
            WebSocketConnection conn = new WebSocketConnection(socket);

            while (!conn.WebSocket.CloseStatus.HasValue)
            {
                await conn.ReceiveAsync();
                RequestId request = (RequestId)Methods.ByteArrayToInt(conn.BinaryMessage);
                switch (request)
                {
                    case RequestId.EvaluateEmail:
                        {
                            await conn.ReceiveAsync();
                            string email = conn.TextMessage;

                            using (var cmd = dbConn.CreateCommand())
                            {
                                cmd.CommandText = $"select (certified) from account where email='{email}';";

                                ResponseId response;
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        reader.Read();
                                        if (reader.GetBoolean(0))
                                        {
                                            response = ResponseId.CertifiedEmail;
                                        }
                                        else
                                        {
                                            response = ResponseId.RegisteredEmail;
                                        }
                                    }
                                    else
                                    {
                                        response = ResponseId.UnknownEmail;
                                    }
                                }

                                await conn.SendBinaryAsync(Methods.IntToByteArray((int)response));
                            }


                            break;
                        }

                    case RequestId.Unknown:
                    default:
                        break;
                }
            }

            Console.WriteLine("Close");
        }
    }
}
