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
        public EmailSettingsConfig Email
        {
            get { return this["Email"] as EmailSettingsConfig; }
        }
    }


    /// <summary>
    /// Interface for email settings, either direct or from a config
    /// </summary>
    public interface IEmailSettings
    {
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="ToAddress"]/*' />
        string ToAddress { get; }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="FromAddress"]/*' />
        string FromAddress { get; }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="FromDisplayName"]/*' />
        string FromDisplayName { get; }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPHost"]/*' />
        string SMTPHost { get; }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPPort"]/*' />
        /// 
        int SMTPPort { get; }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPUserName"]/*' />
        string SMTPUserName { get; }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPPassword"]/*' />
        string SMTPPassword { get; }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPEnableSSL"]/*' />
        bool SMTPEnableSSL { get; }
    }
    
    /// <summary>
    /// Email settings configuration, for configuring Email sending from code
    /// </summary>
    public class EmailSettings : IEmailSettings
    {
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="ToAddress"]/*' />
        public string ToAddress { get; set; }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="FromAddress"]/*' />
        public string FromAddress { get; set; }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="FromDisplayName"]/*' />
        public string FromDisplayName { get; set; }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPHost"]/*' />
        public string SMTPHost { get; set; }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPPort"]/*' />
        public int SMTPPort { get; set; }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPUserName"]/*' />
        public string SMTPUserName { get; set; }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPPassword"]/*' />
        public string SMTPPassword { get; set; }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPEnableSSL"]/*' />
        public bool SMTPEnableSSL { get; set; }

        /// <summary>
        /// Creates an email settings object defaulting the SMTP port to 25
        /// </summary>
        public EmailSettings()
        {
            SMTPPort = 25;
        }
    }

    /// <summary>
    /// A settings object describing email properties
    /// </summary>
    public class EmailSettingsConfig : ConfigurationElement, IEmailSettings
    {
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="ToAddress"]/*' />
        [ConfigurationProperty("toAddress", IsRequired = true)]
        public string ToAddress
        {
            get { return this["toAddress"] as string; }
        }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="FromAddress"]/*' />
        [ConfigurationProperty("fromAddress")]
        public string FromAddress
        {
            get { return this["fromAddress"] as string; }
        }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="FromDisplayName"]/*' />
        [ConfigurationProperty("fromDisplayName")]
        public string FromDisplayName
        {
            get { return this["fromDisplayName"] as string; }
        }

        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPHost"]/*' />
        [ConfigurationProperty("smtpHost")]
        public string SMTPHost
        {
            get { return this["smtpHost"] as string; }
        }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPPort"]/*' />
        [ConfigurationProperty("smtpPort"), DefaultValue(typeof (int), "25")]
        public int SMTPPort
        {
            get { return (int)this["smtpPort"]; }
        }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPUserName"]/*' />
        [ConfigurationProperty("smtpUserName")]
        public string SMTPUserName
        {
            get { return this["smtpUserName"] as string; }
        }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPPassword"]/*' />
        [ConfigurationProperty("smtpPassword")]
        public string SMTPPassword
        {
            get { return this["smtpPassword"] as string; }
        }
        /// <include file='SharedDocs.xml' path='SharedDocs/IEmailSettings/Member[@name="SMTPEnableSSL"]/*' />
        [ConfigurationProperty("smtpEnableSsl"), DefaultValue(typeof(bool), "false")]
        public bool SMTPEnableSSL
        {
            get { return (bool)this["smtpEnableSsl"]; }
        }
    }
}