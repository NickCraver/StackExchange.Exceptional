using StackExchange.Exceptional.Internal;
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
    }
}