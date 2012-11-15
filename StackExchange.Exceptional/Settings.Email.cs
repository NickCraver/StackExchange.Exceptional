using System.ComponentModel;
using System.Configuration;

namespace StackExchange.Exceptional
{
    public partial class Settings
    {
        /// <summary>
        /// The ErrorStore section of the configuration, optional and will default to a MemoryErrorStore if not specified
        /// </summary>
        [ConfigurationProperty("Email")]
        public EmailSettings Email
        {
            get { return this["Email"] as EmailSettings; }
        }
    }

    /// <summary>
    /// A settings object describing email properties
    /// </summary>
    public class EmailSettings : ConfigurationElement
    {
        /// <summary>
        /// The address to send email messages to
        /// </summary>
        [ConfigurationProperty("toAddress", IsRequired = true)]
        public string ToAddress
        {
            get { return this["toAddress"] as string; }
        }

        /// <summary>
        /// The address to send email messages from
        /// </summary>
        [ConfigurationProperty("fromAddress")]
        public string FromAddress
        {
            get { return this["fromAddress"] as string; }
        }

        /// <summary>
        /// The display name to send email messages from
        /// </summary>
        [ConfigurationProperty("fromDisplayName")]
        public string FromDisplayName
        {
            get { return this["fromDisplayName"] as string; }
        }

        /// <summary>
        /// The SMTP server to send mail through
        /// </summary>
        [ConfigurationProperty("smtpHost")]
        public string SMTPHost
        {
            get { return this["smtpHost"] as string; }
        }
        /// <summary>
        /// The port to send mail on (if SMTP server is specified via smtpHost="serverName")
        /// </summary>
        [ConfigurationProperty("smtpPort"), DefaultValue(typeof (int), "25")]
        public int SMTPPort
        {
            get { return (int)this["smtpPort"]; }
        }
        /// <summary>
        /// The SMTP username to use, if authentication is needed
        /// </summary>
        [ConfigurationProperty("smtpUserName")]
        public string SMTPUserName
        {
            get { return this["smtpUserName"] as string; }
        }
        /// <summary>
        /// The SMTP password to use, if authentication is needed
        /// </summary>
        [ConfigurationProperty("smtpPassword")]
        public string SMTPPassword
        {
            get { return this["smtpPassword"] as string; }
        }
        /// <summary>
        /// Whether to use SSL when sending via SMTP
        /// </summary>
        [ConfigurationProperty("smtpEnableSsl"), DefaultValue(typeof(bool), "false")]
        public bool SMTPEnableSSL
        {
            get { return (bool)this["smtpEnableSsl"]; }
        }
    }
}