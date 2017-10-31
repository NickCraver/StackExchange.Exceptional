using StackExchange.Exceptional.Internal;
using System;
using System.Diagnostics;
using System.Net.Mail;

namespace StackExchange.Exceptional.Notifiers
{
    /// <summary>
    /// A notifier that emails when an error occurs.
    /// If an error is a duplicate, (the second of the same error in a short period), it will not be sent if
    /// <see cref="EmailSettings.PreventDuplicates"/> is <c>true</c>.
    /// </summary>
    public class EmailNotifier : IErrorNotifier
    {
        /// <summary>
        /// Settings for this email notifier. Changes will take effect in real time.
        /// </summary>
        public EmailSettings Settings { get; }

        /// <summary>
        /// Whether email functionality is enabled.
        /// </summary>
        public bool Enabled => Settings.ToAddress.HasValue();

        /// <summary>
        /// Configures a new email notifier. Note that if configuration isn't valid this will silently fail.
        /// </summary>
        /// <param name="settings">Settings to use to configure error emailing.</param>
        public EmailNotifier(EmailSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Trace.WriteLine(Settings.ToAddress.HasValue()
                            ? "Email configured, sending to: " + Settings.ToAddress
                            : "Configuration invalid: " + nameof(Settings.ToAddress) + " must have a value");
        }

        /// <summary>
        /// If enabled, sends an error email to the configured recipients.
        /// </summary>
        /// <param name="error">The error the email is about.</param>
        public void Notify(Error error)
        {
            // The following prevents errors that have already been stored from being emailed a second time.
            if (!Enabled || (Settings.PreventDuplicates && error.IsDuplicate)) return;
            try
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(Settings.ToAddress);
                    if (Settings.FromMailAddress != null) message.From = Settings.FromMailAddress;

                    message.Subject = error.ApplicationName + " error: " + error.Message.Replace(Environment.NewLine, " ");
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

        private SmtpClient GetClient()
        {
            var client = new SmtpClient();
            if (Settings.SMTPCredentials != null) client.Credentials = Settings.SMTPCredentials;
            if (Settings.SMTPHost.HasValue()) client.Host = Settings.SMTPHost;
            if (Settings.SMTPPort.HasValue) client.Port = Settings.SMTPPort.Value;
            if (Settings.SMTPEnableSSL) client.EnableSsl = true;

            return client;
        }

        /// <summary>
        /// Equals override, actually using <see cref="GetHashCode"/> underneath.
        /// </summary>
        /// <param name="obj"><see cref="EmailNotifier"/> to compare to.</param>
        /// <returns>Whether <paramref name="obj"/> is equal.</returns>
        public override bool Equals(object obj) => obj is EmailNotifier en && GetHashCode() == en.GetHashCode();

        /// <summary>
        /// <see cref="GetHashCode"/> override, actually comparing <see cref="Settings"/>.
        /// </summary>
        /// <returns>The hashcode of this notifier/settings variant.</returns>
        public override int GetHashCode() => Settings.GetHashCode();
    }
}
