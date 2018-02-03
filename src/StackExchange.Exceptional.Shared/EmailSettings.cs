using StackExchange.Exceptional.Internal;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Email settings configuration, for configuring Email sending from code.
    /// </summary>
    public class EmailSettings
    {
        private string _fromAddress, _fromDisplayName,
                       _SMTPUserName, _SMTPPassword;

        internal MailAddress FromMailAddress { get; private set; }
        internal NetworkCredential SMTPCredentials { get; private set; }

        private void SetMailAddress()
        {
            try
            {
                // Because MailAddress.TryParse() isn't a thing, and an invalid address will throw.
                FromMailAddress = _fromDisplayName.HasValue()
                                  ? new MailAddress(_fromAddress, _fromDisplayName)
                                  : new MailAddress(_fromAddress);
            }
            catch
            {
                FromMailAddress = null;
            }
        }

        private void SetCredentials() =>
            SMTPCredentials = _SMTPUserName.HasValue() && _SMTPPassword.HasValue()
                              ? new NetworkCredential(_SMTPUserName, _SMTPPassword)
                              : null;

        /// <summary>
        /// The address to send email messages to.
        /// </summary>
        public string ToAddress { get; set; }
        /// <summary>
        /// The address to send email messages from.
        /// </summary>
        public string FromAddress
        {
            get => _fromAddress;
            set
            {
                _fromAddress = value;
                SetMailAddress();
            }
        }
        /// <summary>
        /// The display name to send email messages from.
        /// </summary>
        public string FromDisplayName
        {
            get => _fromDisplayName;
            set
            {
                _fromDisplayName = value;
                SetMailAddress();
            }
        }
        /// <summary>
        /// The SMTP server to send mail through.
        /// </summary>
        public string SMTPHost { get; set; }
        /// <summary>
        /// The port to send mail on (if SMTP server is specified via <see cref="SMTPHost"/>).
        /// </summary>
        public int? SMTPPort { get; set; }
        /// <summary>
        /// The SMTP user name to use, if authentication is needed.
        /// </summary>
        public string SMTPUserName
        {
            get => _SMTPUserName;
            set
            {
                _SMTPUserName = value;
                SetCredentials();
            }
        }
        /// <summary>
        /// The SMTP password to use, if authentication is needed.
        /// </summary>
        public string SMTPPassword
        {
            get => _SMTPPassword;
            set
            {
                _SMTPPassword = value;
                SetCredentials();
            }
        }
        /// <summary>
        /// Whether to use SSL when sending via SMTP.
        /// </summary>
        public bool SMTPEnableSSL { get; set; }
        /// <summary>
        /// Flags whether or not emails are sent for duplicate errors.
        /// </summary>
        public bool PreventDuplicates { get; set; }

        /// <summary>
        /// Equals override.
        /// </summary>
        /// <param name="obj"><see cref="EmailSettings"/> to compare to.</param>
        /// <returns>Whether <paramref name="obj"/> is equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is EmailSettings settings
                   && _fromAddress == settings._fromAddress
                   && _fromDisplayName == settings._fromDisplayName
                   && _SMTPUserName == settings._SMTPUserName
                   && _SMTPPassword == settings._SMTPPassword
                   && ToAddress == settings.ToAddress
                   && FromAddress == settings.FromAddress
                   && FromDisplayName == settings.FromDisplayName
                   && SMTPHost == settings.SMTPHost
                   && EqualityComparer<int?>.Default.Equals(SMTPPort, settings.SMTPPort)
                   && SMTPEnableSSL == settings.SMTPEnableSSL
                   && PreventDuplicates == settings.PreventDuplicates;
        }

        /// <summary>
        /// <see cref="GetHashCode"/> override.
        /// </summary>
        /// <returns>The hashcode of this settings variant.</returns>
        public override int GetHashCode()
        {
            var hashCode = 1406920336;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(_fromAddress);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(_fromDisplayName);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(_SMTPUserName);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(_SMTPPassword);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(ToAddress);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(FromAddress);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(FromDisplayName);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(SMTPHost);
            hashCode = (hashCode * -1521134295) + EqualityComparer<int?>.Default.GetHashCode(SMTPPort);
            hashCode = (hashCode * -1521134295) + SMTPEnableSSL.GetHashCode();
            hashCode = (hashCode * -1521134295) + PreventDuplicates.GetHashCode();
            return hashCode;
        }
    }
}
