using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Notifiers;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.Serialization;

namespace StackExchange.Exceptional
{
    public partial class ConfigSettings
    {
        /// <summary>
        /// The ErrorStore section of the configuration, optional and will send no email if not present.
        /// </summary>
        public EmailSettingsConfig Email { get; set; }

        /// <summary>
        /// A settings object describing email properties
        /// </summary>
        public class EmailSettingsConfig
        {
            /// <summary>
            /// The address to send email messages to.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public string ToAddress { get; set; }

            /// <summary>
            /// The address to send email messages from.
            /// </summary>
            public string FromAddress { get; set; }

            /// <summary>
            /// The display name to send email messages from.
            /// </summary>
            public string FromDisplayName { get; set; }

            /// <summary>
            /// The SMTP server to send mail through.
            /// </summary>
            public string SMTPHost { get; set; }

            /// <summary>
            /// The port to send mail on (if SMTP server is specified via <see cref="SMTPHost"/>).
            /// Default is 25
            /// </summary>
            public int SMTPPort { get; set; } = 25;

            /// <summary>
            /// The SMTP user name to use, if authentication is needed.
            /// </summary>
            public string SMTPUserName { get; set; }

            /// <summary>
            /// The SMTP password to use, if authentication is needed.
            /// </summary>
            public string SMTPPassword { get; set; }

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
}