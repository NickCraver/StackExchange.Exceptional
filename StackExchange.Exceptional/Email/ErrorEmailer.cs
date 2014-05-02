using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using StackExchange.Exceptional.Extensions;

namespace StackExchange.Exceptional.Email
{
    /// <summary>
    /// Error emailing handler
    /// </summary>
    public class ErrorEmailer
    {
        /// <summary>
        /// Address to send messages to
        /// </summary>
        public static string ToAddress { get; private set; }
        private static MailAddress FromAddress { get; set; }
        private static string Host { get; set; }
        private static int? Port { get; set; }

        private static NetworkCredential Credentials { get; set; }
        private static bool EnableSSL { get; set; } 

        /// <summary>
        /// Whether email functionality is enabled
        /// </summary>
        public static bool Enabled { get; private set; }

        static ErrorEmailer()
        {
            // Override with exceptional specific settings (user may want this mail to go to another place, or only specify mail settings here, etc.)
            var eSettings = Settings.Current.Email;
            Setup(eSettings);
        }

        /// <summary>
        /// Configure the emailer - note that if config isn't valid this sill silently fail
        /// </summary>
        /// <param name="eSettings">Settings to use to configure error emailing</param>
        public static void Setup(IEmailSettings eSettings)
        {
            if (!eSettings.ToAddress.HasValue())
            {
                Trace.WriteLine("Configuration invalid: ToAddress must have a value");
                return; // not enabled
            }

            ToAddress = eSettings.ToAddress;
            if (eSettings.FromAddress.HasValue())
            {
                FromAddress = eSettings.FromDisplayName.HasValue()
                                  ? new MailAddress(eSettings.FromAddress, eSettings.FromDisplayName)
                                  : new MailAddress(eSettings.FromAddress);
            }

            if (eSettings.SMTPUserName.HasValue() && eSettings.SMTPPassword.HasValue())
                Credentials = new NetworkCredential(eSettings.SMTPUserName, eSettings.SMTPPassword);

            if (eSettings.SMTPHost.HasValue()) Host = eSettings.SMTPHost;
            if (eSettings.SMTPPort != 25) Port = eSettings.SMTPPort;
            EnableSSL = eSettings.SMTPEnableSSL;

            Enabled = true;
            
        }

        /// <summary>
        /// If enabled, sends an error email to the configured recipients
        /// </summary>
        /// <param name="error">The error the email is about</param>
        public static void SendMail(Error error)
        {
            if (!Enabled) return;
            try
            {

                using (var message = new MailMessage())
                {
                    message.To.Add(ToAddress);
                    if (FromAddress != null) message.From = FromAddress;

                    message.Subject = ErrorStore.ApplicationName + " error: " + error.Message.Replace(Environment.NewLine, " ");
                    message.Body = GetErrorHtml(error);
                    message.IsBodyHtml = true;

                    using (var client = GetClient())
                    {
                        client.Send(message);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        private static SmtpClient GetClient()
        {
            var client = new SmtpClient();
            if (Credentials != null) client.Credentials = Credentials;
            if (Host.HasValue()) client.Host = Host;
            if (Port.HasValue) client.Port = Port.Value;
            if (EnableSSL) client.EnableSsl = EnableSSL;

            return client;
        }

        private static string GetErrorHtml(Error error)
        {
            var razorView = new ErrorEmail {error = error};
            return razorView.TransformText();
        }
    }
}
