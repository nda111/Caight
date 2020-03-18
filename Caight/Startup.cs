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
using System.Net.Mail;
using System.Net;

namespace Caight
{
    public class Startup
    {
        private readonly NpgsqlConnection DbConn = new NpgsqlConnection();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            DbConn.ConnectionString = configuration.GetValue<string>("ConnectionString");
            DbConn.Open();
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

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT (certified) FROM account WHERE email='{email}';";

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

                    case RequestId.RegisterEmail:
                        {
                            await conn.ReceiveAsync();
                            string[] args = conn.TextMessage.Split('\0');
                            args[1] = Methods.HashPassword(args[0], args[1], args[2]);
                            string certHash = Methods.CreateCertificationHash(args[0]);

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = 
                                    $"INSERT INTO account (email, pw, name) VALUES('{args[0]}', '{args[1]}', '{args[2]}');" + 
                                    $"INSERT INTO cert_hash (email, hash) VALUES('{args[0]}', '{certHash}');";
                                try
                                {
                                    cmd.ExecuteNonQuery();
                                    string url = $"https://caight.herokuapp.com/certification?h={certHash}";

                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.RegisterOk));

                                    var mail = new CertificationMailSender(args[0], url);
                                    await mail.SendAsync(Configuration.GetValue<string>("MailApiKey"));
                                }
                                catch (NpgsqlException)
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.RegisterNo));
                                }
                            }
                            break;
                        }

                    case RequestId.CertifyEmail:
                        {
                            ResponseId response;

                            await conn.ReceiveAsync();
                            string hash = conn.TextMessage;
                            string email = null;

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT (email) FROM cert_hash WHERE hash='{hash}';";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        response = ResponseId.CertifyOk;
                                        reader.Read();

                                        email = reader.GetString(0);
                                    }
                                    else
                                    {
                                        response = ResponseId.CertifyNo;
                                    }
                                }
                            }

                            if (response == ResponseId.CertifyOk)
                            {
                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText =
                                        $"DELETE FROM cert_hash WHERE email='{email}';" +
                                        $"UPDATE account SET certified=true WHERE email='{email}'";
                                    cmd.ExecuteNonQuery();
                                }

                                await conn.SendTextAsync(email);
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)response));
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
