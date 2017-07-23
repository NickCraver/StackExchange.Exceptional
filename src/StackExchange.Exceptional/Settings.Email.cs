using System.ComponentModel;
using System.Configuration;

namespace StackExchange.Exceptional
{
    internal partial class Settings
    {
        /// <summary>
        /// The ErrorStore section of the configuration, optional and will send no email if not present.
        /// </summary>
        [ConfigurationProperty("Email")]
        public EmailSettingsConfig Email => this["Email"] as EmailSettingsConfig;

        /// <summary>
        /// A settings object describing email properties
        /// </summary>
        public class EmailSettingsConfig : ConfigurationElement
        {
            /// <summary>
            /// The address to send email messages to.
            /// </summary>
            [ConfigurationProperty("toAddress", IsRequired = true)]
            public string ToAddress => this["toAddress"] as string;

            /// <summary>
            /// The address to send email messages from.
            /// </summary>
            [ConfigurationProperty("fromAddress")]
            public string FromAddress => this["fromAddress"] as string;

            /// <summary>
            /// The display name to send email messages from.
            /// </summary>
            [ConfigurationProperty("fromDisplayName")]
            public string FromDisplayName => this["fromDisplayName"] as string;

            /// <summary>
            /// The SMTP server to send mail through.
            /// </summary>
            [ConfigurationProperty("smtpHost")]
            public string SMTPHost => this["smtpHost"] as string;

            /// <summary>
            /// The port to send mail on (if SMTP server is specified via <see cref="SMTPHost"/>).
            /// Default is 25
            /// </summary>
            [ConfigurationProperty("smtpPort"), DefaultValue(typeof(int), "25")]
            public int SMTPPort => (int)this["smtpPort"];

            /// <summary>
            /// The SMTP user name to use, if authentication is needed.
            /// </summary>
            [ConfigurationProperty("smtpUserName")]
            public string SMTPUserName => this["smtpUserName"] as string;

            /// <summary>
            /// The SMTP password to use, if authentication is needed.
            /// </summary>
            [ConfigurationProperty("smtpPassword")]
            public string SMTPPassword => this["smtpPassword"] as string;

            /// <summary>
            /// Whether to use SSL when sending via SMTP.
            /// </summary>
            [ConfigurationProperty("smtpEnableSsl"), DefaultValue(typeof(bool), "false")]
            public bool SMTPEnableSSL => (bool)this["smtpEnableSsl"];

            /// <summary>
            /// Flags whether or not emails are sent for duplicate errors.
            /// </summary>
            [ConfigurationProperty("preventDuplicates"), DefaultValue(typeof(bool), "false")]
            public bool PreventDuplicates => (bool)this["preventDuplicates"];

            /// <summary>
            /// Runs after deserialization, to populate <see cref="ExceptionalSettings.Email"/>.
            /// </summary>
            protected override void PostDeserialize()
            {
                base.PostDeserialize();

                var s = ExceptionalSettings.Current.Email;
                s.ToAddress = ToAddress;
                s.FromAddress = FromAddress;
                s.FromDisplayName = FromDisplayName;
                s.SMTPHost = SMTPHost;
                s.SMTPPort = SMTPPort;
                s.SMTPUserName = SMTPUserName;
                s.SMTPPassword = SMTPPassword;
                s.SMTPEnableSSL = SMTPEnableSSL;
                s.PreventDuplicates = PreventDuplicates;
            }
        }
    }
}