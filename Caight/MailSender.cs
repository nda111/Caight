using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Caight
{
    public class MailSender
    {
        public EmailAddress From { get; } = new EmailAddress("caightapp@gmail.com", "Caight");

        public EmailAddress To { get; set; } = null;

        public string Uri { get; set; } = null;

        public MailSender(string to, string certificationUri)
        {
            To = new EmailAddress(to);
            Uri = certificationUri;
        }

        public async Task<Response> SendVerificationMailAsync(string apiKey)
        {
            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<center>");
            htmlBuilder.Append("<h1>Verification</h1>");
            htmlBuilder.Append("<hr />");
            htmlBuilder.Append("<h3>Press <a href='");
            htmlBuilder.Append(Uri);
            htmlBuilder.Append("'><b>HERE</b></a> to verify your email.</h3>");
            htmlBuilder.Append("</center>");

            var client = new SendGridClient(apiKey);
            var msg = MailHelper.CreateSingleEmail(From, To, "Caight Verification", "", htmlBuilder.ToString());
            return await client.SendEmailAsync(msg);
        }

        public async Task<Response> SendResetPasswordMailAsync(string apiKey)
        {
            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<center>");
            htmlBuilder.Append("<h1>Reset password</h1>");
            htmlBuilder.Append("<hr />");
            htmlBuilder.Append("<h3>Press <a href='");
            htmlBuilder.Append(Uri);
            htmlBuilder.Append("'><b>HERE</b></a> to reset your password.</h3>");
            htmlBuilder.Append("</center>");

            var client = new SendGridClient(apiKey);
            var msg = MailHelper.CreateSingleEmail(From, To, "Reset your password", "", htmlBuilder.ToString());
            return await client.SendEmailAsync(msg);
        }
    }
}
