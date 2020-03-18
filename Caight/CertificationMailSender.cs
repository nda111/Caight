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
    public class CertificationMailSender
    {
        public EmailAddress From { get; } = new EmailAddress("caightapp@gmail.com", "Caight");

        public EmailAddress To { get; set; } = null;

        public string Uri { get; set; } = null;

        public CertificationMailSender(string to, string certificationUri)
        {
            To = new EmailAddress(to);
            Uri = certificationUri;
        }

        public async Task<Response> SendAsync(string apiKey)
        {
            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<h2>Certification</h2>");
            htmlBuilder.Append("<hr />");
            htmlBuilder.Append("<p>Press <a href='");
            htmlBuilder.Append(Uri);
            htmlBuilder.Append("'>HERE</a> to certify your email.");

            var client = new SendGridClient(apiKey);
            var msg = MailHelper.CreateSingleEmail(From, To, "Caight certifiaction mail", "", htmlBuilder.ToString());
            return await client.SendEmailAsync(msg);
        }
    }
}
