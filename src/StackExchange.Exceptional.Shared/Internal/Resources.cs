using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Security.Cryptography;

namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Internal Exceptional resources, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// The JavaScript bundle.
        /// </summary>
        public static ResourceCache BundleJs = new ResourceCache("Bundle.min.js", "text/javascript");
        /// <summary>
        /// The CSS bundle.
        /// </summary>
        public static ResourceCache BundleCss = new ResourceCache("Bundle.min.css", "text/css");

        /// <summary>
        /// Cache data for a specific resource.
        /// </summary>
        public class ResourceCache
        {
            /// <summary>
            /// The SHA 384 hash for this resource.
            /// </summary>
            public string Sha512 { get; }
            /// <summary>
            /// The full content string for this resource.
            /// </summary>
            public string Content { get; }
            /// <summary>
            /// The MIME type for this resource.
            /// </summary>
            public string MimeType { get; }

            internal ResourceCache(string filename, string mimeType)
            {
                MimeType = mimeType;

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StackExchange.Exceptional.Resources." + filename))
                using (var reader = new StreamReader(stream))
                {
                    Content = reader.ReadToEnd();
                }

                using (var hash = SHA512.Create())
                {
                    Sha512 = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(Content)));
                }
            }
        }
    }
}
