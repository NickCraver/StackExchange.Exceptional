using Microsoft.Extensions.Configuration;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Notifiers;
using System;
using System.ComponentModel;
using System.Configuration;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        public EmailSettingsConfig Email => new EmailSettingsConfig(_current.GetSection("Email"));


        public class EmailSettingsConfig 
        {
            private readonly IConfiguration _current;

            public string ToAddress => _current["toAddress"] as string;

            public string FromAddress => _current["fromAddress"] as string;
            public string FromDisplayName => _current["fromDisplayName"] as string;

            public string SMTPHost => _current["smtpHost"] as string;

            public int SMTPPort => Convert.ToInt32(_current["smtpPort"]);

            public string SMTPUserName => _current["smtpUserName"] as string;

            public string SMTPPassword => _current["smtpPassword"] as string;

            public bool SMTPEnableSSL => Convert.ToBoolean(_current["smtpEnableSsl"]);

            public bool PreventDuplicates => Convert.ToBoolean(_current["preventDuplicates"]);

            public EmailSettingsConfig(IConfiguration current)
            {
                _current = current;
            }
        }
    }
}