using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Notifiers;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.Serialization;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        public EmailSettingsConfig Email { get; set; }

        public class EmailSettingsConfig
        {
            [JsonProperty(Required = Required.Always)]
            public string ToAddress { get; set; }

            public string FromAddress { get; set; }

            public string FromDisplayName { get; set; }

            public string SMTPHost { get; set; }

            public int SMTPPort { get; set; } = 25;

            public string SMTPUserName { get; set; }

            public string SMTPPassword { get; set; }

            public bool SMTPEnableSSL { get; set; }

            public bool PreventDuplicates { get; set; }

            internal void Populate(Settings settings)
            {
                var emailSettings = settings.Email;
                emailSettings.ToAddress = ToAddress;
                emailSettings.FromAddress = FromAddress;
                emailSettings.FromDisplayName = FromDisplayName;
                emailSettings.SMTPHost = SMTPHost;
                emailSettings.SMTPPort = SMTPPort;
                emailSettings.SMTPUserName = SMTPUserName;
                emailSettings.SMTPPassword = SMTPPassword;
                emailSettings.SMTPEnableSSL = SMTPEnableSSL;
                emailSettings.PreventDuplicates = PreventDuplicates;

                if (emailSettings.ToAddress.HasValue())
                {
                    EmailNotifier.Setup(emailSettings);
                }
            }
        }
    }
}