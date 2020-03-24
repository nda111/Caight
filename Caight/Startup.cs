using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
using Newtonsoft.Json.Linq;

namespace Caight
{
    public class Startup
    {
        private readonly NpgsqlConnection DbConn = new NpgsqlConnection();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            DbConn.ConnectionString = Configuration.GetValue<string>("ConnectionString");
            DbConn.Open();

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

                            using var cmd = DbConn.CreateCommand();
                            cmd.CommandText = $"SELECT verified FROM account WHERE email='{email}';";

                            ResponseId response;
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    if (reader.GetBoolean(0))
                                    {
                                        response = ResponseId.VerifiedEmail;
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
                            break;
                        }

                    case RequestId.RegisterEmail:
                        {
                            await conn.ReceiveAsync();
                            string[] args = conn.TextMessage.Split('\0');
                            args[1] = Methods.HashPassword(args[1]);
                            string verifyingHash = Methods.CreateAuthenticationToken(args[0]);

                            using var cmd = DbConn.CreateCommand();
                            cmd.CommandText =
                                $"INSERT INTO account (email, pw, name) VALUES('{args[0]}', '{args[1]}', '{args[2]}');" +
                                $"INSERT INTO verifying_hash (email, hash) VALUES('{args[0]}', '{verifyingHash}');";
                            try
                            {
                                cmd.ExecuteNonQuery();
                                string url = $"https://caight.herokuapp.com/verification?h={verifyingHash}";

                                var mail = new MailSender(args[0], url);
                                await mail.SendVerificationMailAsync(Configuration.GetValue<string>("MailApiKey"));

                                await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.RegisterOk));
                            }
                            catch (NpgsqlException)
                            {
                                await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.RegisterNo));
                            }
                            break;
                        }

                    case RequestId.VerifyEmailWebOnly:
                        {
                            ResponseId response;

                            await conn.ReceiveAsync();
                            string hash = conn.TextMessage;
                            string email = null;

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM verifying_hash WHERE hash='{hash}';";
                                using var reader = cmd.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    response = ResponseId.VerifyOkWebOnly;
                                    reader.Read();

                                    email = reader.GetString(0);
                                }
                                else
                                {
                                    response = ResponseId.VerifyNoWebOnly;
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)response));
                            if (response == ResponseId.VerifyOkWebOnly)
                            {
                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText =
                                        $"DELETE FROM verifying_hash WHERE email='{email}';" +
                                        $"UPDATE account SET verified=true WHERE email='{email}';";
                                    cmd.ExecuteNonQuery();
                                }

                                await conn.SendTextAsync(email);
                            }
                            break;
                        }

                    case RequestId.SignIn:
                        {
                            ResponseId response;

                            await conn.ReceiveAsync();
                            string[] args = conn.TextMessage.Split('\0');
                            string email = args[0];
                            string passwd = args[1];

                            long id = 0;
                            string name = null;
                            string token = null;

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT pw, accnt_id, name FROM account WHERE email='{email}';";
                                using var reader = cmd.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    string dbPass = reader.GetString(0);
                                    passwd = Methods.HashPassword(passwd);

                                    if (passwd == dbPass)
                                    {
                                        response = ResponseId.SignInOk;

                                        id = reader.GetInt64(1);
                                        token = Methods.CreateAuthenticationToken(email);
                                        name = reader.GetString(2);
                                    }
                                    else
                                    {
                                        response = ResponseId.SignInWrongPassword;
                                    }
                                }
                                else
                                {
                                    response = ResponseId.SignInError;
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)response));

                            if (response == ResponseId.SignInOk)
                            {
                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText = $"UPDATE account SET auth_token='{token}' WHERE email='{email}';";
                                    cmd.ExecuteNonQuery();
                                }

                                await conn.SendBinaryAsync(Methods.LongToByteArray(id));
                                await conn.SendTextAsync(token);
                                await conn.SendTextAsync(name);
                            }
                            break;
                        }

                    case RequestId.NewGroup:
                        {
                            await conn.ReceiveAsync();
                            long accountId = Methods.ByteArrayToLong(conn.BinaryMessage);

                            await conn.ReceiveAsync();
                            string token = conn.TextMessage;

                            await conn.ReceiveAsync();
                            string[] groupValue = conn.TextMessage.Split('\0');

                            ResponseId response = ResponseId.Unknown;
                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE accnt_id={accountId} AND auth_token='{token}';";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        reader.Read();
                                        email = reader.GetString(0);
                                    }
                                    else
                                    {
                                        response = ResponseId.AddEntityNo;
                                    }
                                }
                            }

                            if (response == ResponseId.Unknown)
                            {
                                try
                                {
                                    using (var cmd = DbConn.CreateCommand())
                                    {
                                        cmd.CommandText = $"INSERT INTO managing_group (owner_email, name, pw) VALUES ('{email}', '{groupValue[0]}', '{Methods.HashPassword(groupValue[1])}');";
                                        cmd.ExecuteNonQuery();
                                    }

                                    int groupId = -1;
                                    using (var cmd = DbConn.CreateCommand())
                                    {
                                        cmd.CommandText = $"SELECT max(id) FROM managing_group;";
                                        using (var reader = cmd.ExecuteReader())
                                        {
                                            reader.Read();
                                            groupId = reader.GetInt32(0);
                                        }
                                    }

                                    using (var cmd = DbConn.CreateCommand())
                                    {
                                        cmd.CommandText = $"INSERT INTO participate (group_id, account_email) VALUES({groupId}, '{email}');";
                                        cmd.ExecuteNonQuery();
                                    }

                                    response = ResponseId.AddEntityOk;
                                }
                                catch (Exception)
                                {
                                    response = ResponseId.AddEntityError;
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)response));

                            break;
                        }

                    case RequestId.NewCat:
                        {
                            await conn.ReceiveAsync();
                            long accountId = Methods.ByteArrayToLong(conn.BinaryMessage);

                            await conn.ReceiveAsync();
                            string token = conn.TextMessage;

                            await conn.ReceiveAsync();
                            string[] catValue = conn.TextMessage.Split('\0');

                            int groupId = int.Parse(catValue[0]);
                            string pw = Methods.HashPassword(catValue[1]);
                            int color = unchecked((int)long.Parse(catValue[2]));
                            string name = catValue[3];
                            long birthday = long.Parse(catValue[4]);
                            short gender = short.Parse(catValue[5]);
                            int species = int.Parse(catValue[6]);
                            long today = long.Parse(catValue[7]);
                            float weight = float.Parse(catValue[8]);

                            ResponseId response = ResponseId.Unknown;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE accnt_id={accountId} AND auth_token='{token}';";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        response = ResponseId.AddEntityNo;
                                    }
                                }
                            }

                            if (response == ResponseId.Unknown)
                            {
                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText = $"SELECT pw FROM managing_group WHERE id={groupId};";
                                    using (var reader = cmd.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            reader.Read();
                                            string groupPasswd = reader.GetString(0);

                                            if (groupPasswd != pw)
                                            {
                                                response = ResponseId.AddEntityNotPw;
                                            }
                                        }
                                        else
                                        {
                                            response = ResponseId.AddEntityError;
                                        }
                                    }
                                }
                            }

                            if (response == ResponseId.Unknown)
                            {
                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText = $"INSERT INTO cat (color, name, birth, gender, species) VALUES({color}, '{name}', {birthday}, {gender}, {species});";
                                    cmd.ExecuteNonQuery();
                                }

                                int catId = -1;
                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText = $"SELECT max(id) FROM cat;";
                                    using var reader = cmd.ExecuteReader();
                                    reader.Read();
                                    catId = reader.GetInt32(0);
                                }

                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText =
                                        $"INSERT INTO managed (group_id, cat_id) VALUES({groupId}, {catId});" +
                                        $"INSERT INTO weighs (cat_id, measured, weight) VALUES({catId}, {today}, {weight});";
                                    cmd.ExecuteNonQuery();
                                }

                                response = ResponseId.AddEntityOk;
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)response));
                            break;
                        }

                    case RequestId.DownloadEntity:
                        {
                            await conn.ReceiveAsync();
                            long accountId = Methods.ByteArrayToLong(conn.BinaryMessage);

                            await conn.ReceiveAsync();
                            string token = conn.TextMessage;

                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE accnt_id={accountId} AND auth_token='{token}';";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        reader.Read();
                                        email = reader.GetString(0);
                                    }
                                    else
                                    {
                                        await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.DownloadRejected));
                                        break;
                                    }
                                }
                            }

                            var groups = new List<CatGroup>();
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT id, name, owner_email, locked FROM managing_group WHERE id IN (SELECT group_id FROM participate WHERE account_email='{email}');";
                                using var reader = cmd.ExecuteReader();

                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        int id = reader.GetInt32(0);
                                        string name = reader.GetString(1);
                                        string owner = reader.GetString(2);
                                        bool locked = reader.GetBoolean(3);

                                        groups.Add(new CatGroup(id, name, owner, locked));
                                    }
                                }
                                else
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.EndOfEntity));
                                    break;
                                }
                            }

                            var entries = new Dictionary<CatGroup, List<Cat>>();
                            foreach (var group in groups)
                            {
                                int groupId = group.Id;

                                List<Cat> catList = new List<Cat>();
                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText = $"SELECT id, name, birth, gender, species, color FROM cat WHERE id IN (SELECT cat_id FROM managed WHERE group_id={groupId}) ORDER BY id;";
                                    using var reader = cmd.ExecuteReader();

                                    while (reader.Read())
                                    {
                                        int id = reader.GetInt32(0);
                                        string name = reader.GetString(1);
                                        long birth = reader.GetInt64(2);
                                        short gender = reader.GetInt16(3);
                                        int species = reader.GetInt32(4);
                                        int color = reader.GetInt32(5);

                                        catList.Add(new Cat(id, color, name, birth, gender, species, null));
                                    }
                                }

                                foreach (var cat in catList)
                                {
                                    int catId = cat.Id;
                                    var weights = new SortedDictionary<long, float>();
                                    using (var cmd = DbConn.CreateCommand())
                                    {
                                        cmd.CommandText = $"SELECT measured, weight FROM weighs WHERE cat_id={catId};";
                                        using var reader = cmd.ExecuteReader();

                                        while (reader.Read())
                                        {
                                            long when = reader.GetInt64(0);
                                            float weight = reader.GetFloat(1);

                                            weights.Add(when, weight);
                                        }
                                    }

                                    cat.Weights = weights;
                                }

                                entries.Add(group, catList);
                            }

                            foreach (var entry in entries)
                            {
                                string groupJson = entry.Key.ToJsonObject().ToString();
                                await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.EntityGroup));
                                await conn.SendTextAsync(groupJson);

                                foreach (var cat in entry.Value)
                                {
                                    string catJson = cat.ToJsonObject().ToString();
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.EntityCat));
                                    await conn.SendTextAsync(catJson);
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.EndOfEntity));
                            break;
                        }

                    case RequestId.ChangeName:
                        {
                            await conn.ReceiveAsync();
                            long accountId = Methods.ByteArrayToLong(conn.BinaryMessage);

                            await conn.ReceiveAsync();
                            string token = conn.TextMessage;

                            await conn.ReceiveAsync();
                            string newName = conn.TextMessage;

                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE accnt_id={accountId} AND auth_token='{token}';";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        reader.Read();
                                        email = reader.GetString(0);
                                    }
                                    else
                                    {
                                        await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ChangeNameNo));
                                        break;
                                    }
                                }
                            }

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"UPDATE account SET name='{newName}' WHERE email='{email}';";

                                try
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                catch
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ChangeNameNo));
                                    break;
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ChangeNameOk));
                            break;
                        }

                    case RequestId.Logout:
                        {
                            await conn.ReceiveAsync();
                            long accountId = Methods.ByteArrayToLong(conn.BinaryMessage);

                            await conn.ReceiveAsync();
                            string token = conn.TextMessage;

                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE accnt_id={accountId} AND auth_token='{token}';";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        reader.Read();
                                        email = reader.GetString(0);
                                    }
                                    else
                                    {
                                        await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.LogoutNo));
                                        break;
                                    }
                                }
                            }

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"UPDATE account SET auth_token=null WHERE email='{email}';";

                                try
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                catch
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.LogoutNo));
                                    break;
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.LogoutOk));
                            break;
                        }

                    case RequestId.RequestResetPasswordUri:
                        {
                            await conn.ReceiveAsync();
                            string email = conn.TextMessage;

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE email='{email}';";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ResetPasswordUriError));
                                        break;
                                    }
                                }
                            }

                            long expireDue = DateTime.Now.AddMinutes(10).Ticks;
                            string hash = Methods.CreateAuthenticationToken(email + expireDue);
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText =
                                    $"DELETE FROM reset_password WHERE email='{email}';" +
                                    $"INSERT INTO reset_password (email, expire_due, hash) VALUES ('{email}', {expireDue}, '{hash}');";
                                try
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                catch
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ResetPasswordUriError));
                                    break;
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ResetPasswordUriCreated));

                            string url = $"https://caight.herokuapp.com/resetpassword/{hash}";
                            var mail = new MailSender(email, url);
                            await mail.SendResetPasswordMailAsync(Configuration.GetValue<string>("MailApiKey"));
                            break;
                        }

                    case RequestId.ResetPasswordWebOnly:
                        {
                            await conn.ReceiveAsync();
                            string hash = conn.TextMessage;

                            ResponseId response;
                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email, expire_due, used FROM reset_password WHERE hash='{hash}';";

                                using var reader = cmd.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    reader.Read();

                                    bool used = reader.GetBoolean(2);
                                    long now = DateTime.Now.Ticks;
                                    long expireDue = reader.GetInt64(1);

                                    if (now >= expireDue)
                                    {
                                        response = ResponseId.ResetPasswordPageExpiredWebOnly;
                                    }
                                    else if (used)
                                    {
                                        response = ResponseId.ResetPasswordPageUsedWebOnly;
                                    }
                                    else
                                    {
                                        email = reader.GetString(0);
                                        response = ResponseId.ResetPasswordPageOkWebOnly;
                                    }
                                }
                                else
                                {
                                    response = ResponseId.ResetPasswordPageNoWebOnly;
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)response));
                            if (response == ResponseId.ResetPasswordPageOkWebOnly)
                            {
                                await conn.SendTextAsync(email);
                            }
                            break;
                        }

                    case RequestId.ResetPasswordConfirmWebOnly:
                        {
                            await conn.ReceiveAsync();
                            string hash = conn.TextMessage;

                            await conn.ReceiveAsync();
                            string password = conn.TextMessage;

                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM reset_password WHERE hash='{hash}';";

                                using var reader = cmd.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    email = reader.GetString(0);
                                }
                                else
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ResetPasswordConfirmNoWebOnly));
                                    break;
                                }
                            }

                            const string PasswordPattern = "((?=.*[a-z])(?=.*[0-9])(?=.*[!@#$%^&*])(?=.*[A-Z]).{8,})";
                            var regex = new Regex(PasswordPattern);
                            if (regex.Match(password).Success)
                            {
                                password = Methods.HashPassword(password);
                                using (var cmd = DbConn.CreateCommand())
                                {
                                    cmd.CommandText =
                                        $"UPDATE account SET pw='{password}' WHERE email='{email}';" +
                                        $"UPDATE reset_password SET used=true WHERE hash='{hash}';";

                                    try
                                    {
                                        cmd.ExecuteNonQuery();
                                        await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ResetPasswordConfirmOkWebOnly));
                                        break;
                                    }
                                    catch
                                    {
                                        await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ResetPasswordConfirmErrorWebOnly));
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.ResetPasswordConfirmNoWebOnly));
                                break;
                            }
                        }

                    case RequestId.DeleteAccount:
                        {
                            await conn.ReceiveAsync();
                            long accountId = Methods.ByteArrayToLong(conn.BinaryMessage);

                            await conn.ReceiveAsync();
                            string token = conn.TextMessage;

                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE accnt_id={accountId} AND auth_token='{token}' AND NOT IN (SELECT owner_email FROM managing_group);";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        reader.Read();
                                        email = reader.GetString(0);
                                    }
                                    else
                                    {
                                        await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.DeleteAccountNo));
                                        break;
                                    }
                                }
                            }

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = 
                                    $"DELETE FROM account WHERE email='{email}';" + 
                                    $"DELETE FROM participate WHERE account_email='{email}'";

                                try
                                {
                                    cmd.ExecuteNonQuery();

                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.DeleteAccountOk));
                                    break;
                                }
                                catch
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.DeleteAccountNo));
                                    break;
                                }
                            }
                        }

                    case RequestId.JoinGroup:
                        {
                            await conn.ReceiveAsync();
                            long accountId = Methods.ByteArrayToLong(conn.BinaryMessage);

                            await conn.ReceiveAsync();
                            string token = conn.TextMessage;

                            await conn.ReceiveAsync();
                            string[] groupValue = conn.TextMessage.Split('\0');

                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE accnt_id={accountId} AND auth_token='{token}';";
                                using var reader = cmd.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    email = reader.GetString(0);
                                }
                                else
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.JoinGroupError));
                                    break;
                                }
                            }

                            int groupId;
                            int.TryParse(groupValue[0], out groupId);
                            string password = Methods.HashPassword(groupValue[1]);
                            bool joinable = false;
                            bool passwordMatches = false;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT pw, locked FROM managing_group WHERE id={groupId};";

                                using var reader = cmd.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    passwordMatches = string.Equals(password, reader.GetString(0));
                                    joinable = !reader.GetBoolean(1);
                                }
                                else
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.JoinGroupNotExists));
                                    break;
                                }
                            }

                            if (!joinable)
                            {
                                await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.JoinGroupRejected));
                                break;
                            }

                            if (!passwordMatches)
                            {
                                await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.JoinGroupWrongPassword));
                                break;
                            }

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"INSERT INTO participate (group_id, account_email) VALUES ({groupId}, '{email}');";
                                try
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                catch
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.JoinGroupError));
                                    break;
                                }
                            }

                            await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.JoinGroupOk));
                            break;
                        }

                    case RequestId.DownloadMember:
                        {
                            await conn.ReceiveAsync();
                            int groupId = Methods.ByteArrayToInt(conn.BinaryMessage);

                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT name, email FROM account WHERE email IN (SELECT account_email FROM participate WHERE group_id={groupId});";
                                using var reader = cmd.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        StringBuilder builder = new StringBuilder();
                                        builder.Append(reader.GetString(0));
                                        builder.Append('\0');
                                        builder.Append(reader.GetString(1));

                                        await conn.SendTextAsync(builder.ToString());
                                    }

                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.EndOfMember));
                                    break;
                                }
                                else
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.DownloadMemberError));
                                    break;
                                }
                            }
                        }

                    case RequestId.UpdateGroup:
                        {
                            await conn.ReceiveAsync();
                            long accountId = Methods.ByteArrayToLong(conn.BinaryMessage);

                            await conn.ReceiveAsync();
                            string token = conn.TextMessage;

                            await conn.ReceiveAsync();
                            string jsonString = conn.TextMessage;

                            string email = null;
                            using (var cmd = DbConn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT email FROM account WHERE accnt_id={accountId} AND auth_token='{token}';";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        reader.Read();
                                        email = reader.GetString(0);
                                    }
                                    else
                                    {
                                        await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.UpdateGroupError));
                                        break;
                                    }
                                }
                            }

                            JObject json = JObject.Parse(jsonString);
                            int id = json.GetValue("id").ToObject<int>();
                            var updateList = new List<string>();
                            JToken temp;
                            if (json.TryGetValue("name", out temp))
                            {
                                updateList.Add($"name='{temp.ToObject<string>()}'");
                            }
                            if (json.TryGetValue("password", out temp))
                            {
                                updateList.Add($"pw='{temp.ToObject<string>()}'");
                            }
                            if (json.TryGetValue("locked", out temp))
                            {
                                updateList.Add($"locked={temp.ToObject<bool>()}");
                            }
                            if (json.TryGetValue("manager", out temp))
                            {
                                updateList.Add($"manager='{temp.ToObject<string>()}'");
                            }

                            using (var cmd = DbConn.CreateCommand())
                            {
                                try
                                {
                                    cmd.CommandText = $"UPDATE managing_group SET {string.Join(',', updateList.ToArray())} WHERE id={id};";
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.UpdateGroupOk));
                                }
                                catch
                                {
                                    await conn.SendBinaryAsync(Methods.IntToByteArray((int)ResponseId.UpdateGroupError));
                                    break;
                                }

                                throw new Exception(cmd.CommandText);
                                break;
                            }
                        }

                    case RequestId.Unknown:
                    default:
                        break;
                }

                await conn.ReceiveAsync();
            }
        }

        private int HexStringToInt32(string str)
        {
            try
            {
                return Convert.ToInt32(str, 16);
            }
            catch (FormatException)
            {
                return -1;
            }
        }
    }
}
