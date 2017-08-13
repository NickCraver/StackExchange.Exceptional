using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Notifiers;
using System.Configuration;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        [ConfigurationProperty("Email")]
        public EmailSettingsConfig Email => this["Email"] as EmailSettingsConfig;
        
        public class EmailSettingsConfig : ExceptionalElement
        {
            [ConfigurationProperty("toAddress", IsRequired = true)]
            public string ToAddress => Get("toAddress");
            [ConfigurationProperty("fromAddress")]
            public string FromAddress => Get("fromAddress");
            [ConfigurationProperty("fromDisplayName")]
            public string FromDisplayName => Get("fromDisplayName");
            [ConfigurationProperty("smtpHost")]
            public string SMTPHost => Get("smtpHost");
            [ConfigurationProperty("smtpPort")]
            public int? SMTPPort => GetInt("smtpPort");
            [ConfigurationProperty("smtpUserName")]
            public string SMTPUserName => Get("smtpUserName");
            [ConfigurationProperty("smtpPassword")]
            public string SMTPPassword => Get("smtpPassword");
            [ConfigurationProperty("smtpEnableSsl")]
            public bool? SMTPEnableSSL => GetBool("smtpEnableSsl");
            [ConfigurationProperty("preventDuplicates")]
            public bool? PreventDuplicates => GetBool("preventDuplicates");
            
            internal void Populate(Settings settings)
            {
                var s = settings.Email;
                if (ToAddress.HasValue()) s.ToAddress = ToAddress;
                if (FromAddress.HasValue()) s.FromAddress = FromAddress;
                if (FromDisplayName.HasValue()) s.FromDisplayName = FromDisplayName;
                if (SMTPHost.HasValue()) s.SMTPHost = SMTPHost;
                if (SMTPPort.HasValue) s.SMTPPort = SMTPPort;
                if (SMTPUserName.HasValue()) s.SMTPUserName = SMTPUserName;
                if (SMTPPassword.HasValue()) s.SMTPPassword = SMTPPassword;
                if (SMTPEnableSSL.HasValue) s.SMTPEnableSSL = SMTPEnableSSL.Value;
                if (PreventDuplicates.HasValue) s.PreventDuplicates = PreventDuplicates.Value;

                if (s.ToAddress.HasValue())
                {
                    EmailNotifier.Setup(s);
                }
            }
        }
    }
}