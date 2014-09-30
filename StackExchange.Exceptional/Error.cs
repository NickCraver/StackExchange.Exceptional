using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using StackExchange.Exceptional.Extensions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Represents a logical application error (as opposed to the actual exception it may be representing).
    /// </summary>
    [Serializable]
    public class Error
    {
        internal const string CollectionErrorKey = "CollectionFetchError";

		/// <summary>
		/// The convert from json
		/// </summary>
		public static Func<string, Error> ConvertFromJson { get; set; }

		/// <summary>
		/// The convert to json
		/// </summary>
	    public static Func<object, string> ConvertToJson { get; set; }

        /// <summary>
        /// Filters on form values *not * to log, because they contain sensitive data
        /// </summary>
        public static ConcurrentDictionary<string, string> FormLogFilters { get; private set; }

        /// <summary>
        /// Filters on form values *not * to log, because they contain sensitive data
        /// </summary>
        public static ConcurrentDictionary<string, string> CookieLogFilters { get; private set; }

        static Error()
        {
			ConvertFromJson = json =>
	        {
				var serializer = new JavaScriptSerializer();
				return serializer.Deserialize<Error>(json);
	        };
			ConvertToJson = error =>
			{
				var serializer = new JavaScriptSerializer();
				return serializer.Serialize(error);
			};

            CookieLogFilters = new ConcurrentDictionary<string, string>();
            Settings.Current.LogFilters.CookieFilters.All.ForEach(flf => CookieLogFilters[flf.Name] = flf.ReplaceWith ?? "");

            FormLogFilters = new ConcurrentDictionary<string, string>();
            Settings.Current.LogFilters.FormFilters.All.ForEach(flf => FormLogFilters[flf.Name] = flf.ReplaceWith ?? "");
        }

        /// <summary>
        /// The Id on this error, strictly for primary keying on persistent stores
        /// </summary>
        [ScriptIgnore]
        public long Id { get; set; }

        /// <summary>
        /// Unique identifier for this error, gernated on the server it came from
        /// </summary>
        public Guid GUID { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        public Error() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class from a given <see cref="Exception"/> instance.
        /// </summary>
        public Error(Exception e): this(e, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class
        /// from a given <see cref="Exception"/> instance and 
        /// <see cref="HttpContext"/> instance representing the HTTP 
        /// context during the exception.
        /// </summary>
        public Error(Exception e, HttpContext context, string applicationName = null)
        {
            if (e == null) throw new ArgumentNullException("e");

            Exception = e;
            var baseException = e;

            // if it's not a .Net core exception, usually more information is being added
            // so use the wrapper for the message, type, etc.
            // if it's a .Net core exception type, drill down and get the innermost exception
            if (IsBuiltInException(e))
                baseException = e.GetBaseException();

            GUID = Guid.NewGuid();
            ApplicationName = applicationName ?? ErrorStore.ApplicationName;
            MachineName = Environment.MachineName;
            Type = baseException.GetType().FullName;
            Message = baseException.Message;
            Source = baseException.Source;
            Detail = e.ToString();
            CreationDate = DateTime.UtcNow;
            DuplicateCount = 1;
            
            var httpException = e as HttpException;
            if (httpException != null)
            {
                StatusCode = httpException.GetHttpCode();
            }

            SetContextProperties(context);

            ErrorHash = GetHash();
        }

        /// <summary>
        /// Sets Error properties pulled from HttpContext, if present
        /// </summary>
        /// <param name="context">The HttpContext related to the request</param>
        private void SetContextProperties(HttpContext context)
        {
            if (context == null) return;

            var request = context.Request;

            Func<Func<HttpRequest, NameValueCollection>, NameValueCollection> tryGetCollection = getter =>
                {
                    try
                    {
                        return new NameValueCollection(getter(request));
                    }
                    catch (HttpRequestValidationException e)
                    {
                        Trace.WriteLine("Error parsing collection: " + e.Message);
                        return new NameValueCollection {{CollectionErrorKey, e.Message}};
                    }
                };

            ServerVariables = tryGetCollection(r => r.ServerVariables);
            QueryString = tryGetCollection(r => r.QueryString);
            Form = tryGetCollection(r => r.Form);
            
            // Filter form variables for sensitive information
            if (FormLogFilters.Count > 0)
            {
                foreach (var k in FormLogFilters.Keys)
                {
                    if (Form[k] != null)
                        Form[k] = FormLogFilters[k];
                }
            }

            try
            {
                Cookies = new NameValueCollection(request.Cookies.Count);
                for (var i = 0; i < request.Cookies.Count; i++)
                {
                    var name = request.Cookies[i].Name;
                    string val;
                    CookieLogFilters.TryGetValue(name, out val);
                    Cookies.Add(name, val ?? request.Cookies[i].Value);
                }
            }
            catch (HttpRequestValidationException e)
            {
                Trace.WriteLine("Error parsing cookie collection: " + e.Message);
            }

            RequestHeaders = new NameValueCollection(request.Headers.Count);
            foreach(var header in request.Headers.AllKeys)
            {
                // Cookies are handled above, no need to repeat
                if (string.Compare(header, "Cookie", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                if (request.Headers[header] != null)
                    RequestHeaders[header] = request.Headers[header];
            }
        }

        /// <summary>
        /// returns if the type of the exception is built into .Net core
        /// </summary>
        /// <param name="e">The exception to check</param>
        /// <returns>True if the exception is a type from within the CLR, false if it's a user/third party type</returns>
        private bool IsBuiltInException(Exception e)
        {
            return e.GetType().Module.ScopeName == "CommonLanguageRuntimeLibrary";
        }

        /// <summary>
        /// Gets a unique-enough hash of this error.  Stored as a quick comparison mehanism to rollup duplicate errors.
        /// </summary>
        /// <returns>"Unique" hash for this error</returns>
        public int? GetHash()
        {
            if (!Detail.HasValue()) return null;

            var result = Detail.GetHashCode();
            if (RollupPerServer && MachineName.HasValue())
                result = (result * 397)^ MachineName.GetHashCode();

            return result;
        }

        /// <summary>
        /// Reflects if the error is protected from deletion
        /// </summary>
        public bool IsProtected { get; set; }

        /// <summary>
        /// Gets the <see cref="Exception"/> instance used to create this error
        /// </summary>
        [ScriptIgnore]
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets the name of the application that threw this exception
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets the hostname of where the exception occured
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Get the type of error
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets the source of this error
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets the exception message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets the detail/stack trace of this error
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// The hash that describes this error
        /// </summary>
        public int? ErrorHash { get; set; }

        /// <summary>
        /// Gets the time in UTC that the error occured
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets the HTTP Status code associated with the request
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Gets the server variables collection for the request
        /// </summary>
        [ScriptIgnore]
        public NameValueCollection ServerVariables { get; set; }
        
        /// <summary>
        /// Gets the query string collection for the request
        /// </summary>
        [ScriptIgnore]
        public NameValueCollection QueryString { get; set; }
        
        /// <summary>
        /// Gets the form collection for the request
        /// </summary>
        [ScriptIgnore]
        public NameValueCollection Form { get; set; }
        
        /// <summary>
        /// Gets a collection representing the client cookies of the request
        /// </summary>
        [ScriptIgnore]
        public NameValueCollection Cookies { get; set; }

        /// <summary>
        /// Gets a collection representing the headers sent with the request
        /// </summary>
        [ScriptIgnore]
        public NameValueCollection RequestHeaders { get; set; }

        /// <summary>
        /// Gets a collection of custom data added at log time
        /// </summary>
        public Dictionary<string, string> CustomData { get; set; }
        
        /// <summary>
        /// The number of newer Errors that have been discarded because they match this Error and fall within the configured 
        /// "IgnoreSimilarExceptionsThreshold" TimeSpan value.
        /// </summary>
        public int? DuplicateCount { get; set; }

        /// <summary>
        /// Gets the SQL command text assocaited with this error
        /// </summary>
        public string SQL { get; set; }
        
        /// <summary>
        /// Date this error was deleted (for stores that support deletion and retention, e.g. SQL)
        /// </summary>
        public DateTime? DeletionDate { get; set; }

        /// <summary>
        /// The URL host of the request causing this error
        /// </summary>
        public string Host { get { return _host ?? (_host = ServerVariables == null ? "" : ServerVariables["HTTP_HOST"]); } set { _host = value; } }
        private string _host;

        /// <summary>
        /// The URL path of the request causing this error
        /// </summary>
        public string Url { get { return _url ?? (_url = ServerVariables == null ? "" : ServerVariables["URL"]); } set { _url = value; } }
        private string _url;

        /// <summary>
        /// The HTTP Method causing this error, e.g. GET or POST
        /// </summary>
        public string HTTPMethod { get { return _httpMethod ?? (_httpMethod = ServerVariables == null ? "" : ServerVariables["REQUEST_METHOD"]); } set { _httpMethod = value; } }
        private string _httpMethod;

        /// <summary>
        /// The IPAddress of the request causing this error
        /// </summary>
        public string IPAddress { get { return _ipAddress ?? (_ipAddress = ServerVariables == null ? "" : ServerVariables.GetRemoteIP()); } set { _ipAddress = value; } }
        private string _ipAddress;
        
        /// <summary>
        /// Json populated from database stored, deserialized after if needed
        /// </summary>
        [ScriptIgnore]
        public string FullJson { get; set; }

        /// <summary>
        /// Whether to roll up errors per-server. E.g. should an identical error happening on 2 separate servers be a DuplicateCount++, or 2 separate errors.
        /// </summary>
        [ScriptIgnore]
        public bool RollupPerServer { get; set; }

        /// <summary>
        /// Returns the value of the <see cref="Message"/> property.
        /// </summary>
        public override string ToString()
        {
            return Message;
        }
        
        /// <summary>
        /// Create a copy of the error and collections so if it's modified in memory logging is not affected
        /// </summary>
        /// <returns>A clone of this error</returns>
        public Error Clone()
        {
            var copy = (Error) MemberwiseClone();
            if (ServerVariables != null) copy.ServerVariables = new NameValueCollection(ServerVariables);
            if (QueryString != null) copy.QueryString = new NameValueCollection(QueryString);
            if (Form != null) copy.Form = new NameValueCollection(Form);
            if (Cookies != null) copy.Cookies = new NameValueCollection(Cookies);
            if (RequestHeaders != null) copy.RequestHeaders = new NameValueCollection(RequestHeaders);
            if (CustomData != null) copy.CustomData = new Dictionary<string, string>(CustomData);
            return copy;
        }

        /// <summary>
        /// Caribles strictly for JSON serialziation, to maintain non-dictonary behavior
        /// </summary>
        public List<NameValuePair> ServerVariablesSerializable
        {
            get { return GetPairs(ServerVariables); }
            set { ServerVariables = GetNameValueCollection(value); }
        }
        /// <summary>
        /// Caribles strictly for JSON serialziation, to maintain non-dictonary behavior
        /// </summary>
        public List<NameValuePair> QueryStringSerializable
        {
            get { return GetPairs(QueryString); }
            set { QueryString = GetNameValueCollection(value); }
        }
        /// <summary>
        /// Caribles strictly for JSON serialziation, to maintain non-dictonary behavior
        /// </summary>
        public List<NameValuePair> FormSerializable
        {
            get { return GetPairs(Form); }
            set { Form = GetNameValueCollection(value); }
        }
        /// <summary>
        /// Caribles strictly for JSON serialziation, to maintain non-dictonary behavior
        /// </summary>
        public List<NameValuePair> CookiesSerializable
        {
            get { return GetPairs(Cookies); }
            set { Cookies = GetNameValueCollection(value); }
        }

        /// <summary>
        /// Caribles strictly for JSON serialziation, to maintain non-dictonary behavior
        /// </summary>
        public List<NameValuePair> RequestHeadersSerializable
        {
            get { return GetPairs(RequestHeaders); }
            set { RequestHeaders = GetNameValueCollection(value); }
        }

        /// <summary>
        /// Only for deserializing errors pre-spelling fix properly
        /// </summary>
        [ScriptIgnore]
        public List<NameValuePair> ServerVariablesSerialzable
        {
            set { ServerVariables = GetNameValueCollection(value); }
        }
        /// <summary>
        /// Only for deserializing errors pre-spelling fix properly
        /// </summary>
        [ScriptIgnore]
        public List<NameValuePair> QueryStringSerialzable
        {
            set { QueryString = GetNameValueCollection(value); }
        }
        /// <summary>
        /// Only for deserializing errors pre-spelling fix properly
        /// </summary>
        [ScriptIgnore]
        public List<NameValuePair> FormSerialzable
        {
            set { Form = GetNameValueCollection(value); }
        }
        /// <summary>
        /// Only for deserializing errors pre-spelling fix properly
        /// </summary>
        [ScriptIgnore]
        public List<NameValuePair> CookiesSerialzable
        {
            set { Cookies = GetNameValueCollection(value); }
        }
        /// <summary>
        /// Only for deserializing errors pre-spelling fix properly
        /// </summary>
        [ScriptIgnore]
        public List<NameValuePair> RequestHeadersSerialzable
        {
            set { RequestHeaders = GetNameValueCollection(value); }
        }

        /// <summary>
        /// Gets a JSON representation for this error
        /// </summary>
        public string ToJson()
        {
	        return ConvertToJson(this);
        }

        /// <summary>
        /// Gets a JSON representation for this error suitable for cross-domain 
        /// </summary>
        /// <returns></returns>
        public string ToDetailedJson()
        {
            return ConvertToJson(new
                                            {
                                                GUID,
                                                ApplicationName,
                                                CreationDate = CreationDate.ToEpochTime(),
                                                CustomData,
                                                DeletionDate = DeletionDate.ToEpochTime(),
                                                Detail,
                                                DuplicateCount,
                                                ErrorHash,
                                                HTTPMethod,
                                                Host,
                                                IPAddress,
                                                IsProtected,
                                                MachineName,
                                                Message,
                                                SQL,
                                                Source,
                                                StatusCode,
                                                Type,
                                                Url,
                                                QueryString = ServerVariables != null ? ServerVariables["QUERY_STRING"] : null,
                                                ServerVariables = ServerVariablesSerializable.ToJsonDictionary(),
                                                CookieVariables = CookiesSerializable.ToJsonDictionary(),
                                                RequestHeaders = RequestHeadersSerializable.ToJsonDictionary(),
                                                QueryStringVariables = QueryStringSerializable.ToJsonDictionary(),
                                                FormVariables = FormSerializable.ToJsonDictionary()
                                            });
        }

        /// <summary>
        /// Deserializes provided JSON into an Error object
        /// </summary>
        /// <param name="json">JSON representing an Error</param>
        /// <returns>The Error object</returns>
        public static Error FromJson(string json)
        {
	        return ConvertFromJson(json);
        }

        /// <summary>
        /// Serialization class in place of the NameValueCollection pairs
        /// </summary>
        /// <remarks>This exists because things like a querystring can havle multiple values, they are not a dictionary</remarks>
        public class NameValuePair
        {
            /// <summary>
            /// The name for this variable
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// The value for this variable
            /// </summary>
            public string Value { get; set; }
        }

        private List<NameValuePair> GetPairs(NameValueCollection nvc)
        {
            var result = new List<NameValuePair>();
            if (nvc == null) return null;

            for (int i = 0; i < nvc.Count; i++)
            {
                result.Add(new NameValuePair {Name = nvc.GetKey(i), Value = nvc.Get(i)});
            }
            return result;
        }

        private NameValueCollection GetNameValueCollection(List<NameValuePair> pairs)
        {
            var result = new NameValueCollection();
            if (pairs == null) return null;

            foreach(var p in pairs)
            {
                result.Add(p.Name, p.Value);
            }
            return result;
        }
    }
}