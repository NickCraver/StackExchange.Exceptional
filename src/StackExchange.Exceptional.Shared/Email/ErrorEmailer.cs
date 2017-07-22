using StackExchange.Exceptional.Internal;
using System;
using System.Diagnostics;
using System.Net.Mail;

namespace StackExchange.Exceptional.Email
{
    /// <summary>
    /// Error emailing handler
    /// </summary>
    public static class ErrorEmailer
    {
        private static EmailSettings _settings;
        private static EmailSettings Settings => _settings ?? ExceptionalSettings.Current.Email;

        /// <summary>
        /// Whether email functionality is enabled.
        /// </summary>
        public static bool Enabled => Settings.ToAddress.HasValue();

        /// <summary>
        /// Configure the emailer - note that if config isn't valid this will silently fail.
        /// </summary>
        /// <param name="settings">Settings to use to configure error emailing.</param>
        public static void Setup(EmailSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings), "You can't setup without settings, that's just crazy.");
            Trace.WriteLine(settings.ToAddress.HasValue()
                            ? "Email configured, sending to: " + settings.ToAddress
                            : "Configuration invalid: " + nameof(settings.ToAddress) + " must have a value");
        }

        /// <summary>
        /// If enabled, sends an error email to the configured recipients.
        /// </summary>
        /// <param name="error">The error the email is about.</param>
        /// <param name="isDuplicate">Whether this error is a duplicate.</param>
        public static void SendMail(Error error, bool isDuplicate)
        {
            // The following prevents errors that have already been stored from being emailed a second time.
            if (!Enabled || (Settings.PreventDuplicates && isDuplicate)) return;
            try
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(Settings.ToAddress);
                    if (Settings.FromMailAddress != null) message.From = Settings.FromMailAddress;

                    message.Subject = ErrorStore.ApplicationName + " error: " + error.Message.Replace(Environment.NewLine, " ");
                    message.Body = new ErrorEmail(error).Render();
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
            if (Settings.SMTPCredentials != null) client.Credentials = Settings.SMTPCredentials;
            if (Settings.SMTPHost.HasValue()) client.Host = Settings.SMTPHost;
            if (Settings.SMTPPort.HasValue) client.Port = Settings.SMTPPort.Value;
            if (Settings.SMTPEnableSSL) client.EnableSsl = true;

            return client;
        }
    }
}
