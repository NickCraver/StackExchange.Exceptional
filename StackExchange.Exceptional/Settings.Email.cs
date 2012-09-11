using System.ComponentModel;
using System.Configuration;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    public partial class Settings
    {
            /// <summary>
            /// The Email section of the configuration, optional and will default to a MemoryEmail if not specified
            /// </summary>
            [ConfigurationProperty("Email")]
            public EmailSettings Email
            {
                get { return this["Email"] as EmailSettings; }
            }
        }

        /// <summary>
        /// A settings object describing an error store
        /// </summary>
        public class EmailSettings : ConfigurationElement
        {
            /// <summary>
            /// Email to
            /// </summary>
            [ConfigurationProperty("toaddress")]
            public string Toaddress { get { return this["toaddress"] as string; } }

            /// <summary>
            /// Email to
            /// </summary>
            [ConfigurationProperty("fromaddress")]
            public string Fromaddress { get { return this["fromaddress"] as string; } }

            /// <summary>
            /// outgoing smtp server
            /// </summary>
            [ConfigurationProperty("smtphost")]
            public string SmtpHost { get { return this["smtphost"] as string; } }

            /// <summary>
            /// infourl
            /// </summary>
            [ConfigurationProperty("infourl")]
            public string Infourl { get { return this["infourl"] as string; } }
        }




}