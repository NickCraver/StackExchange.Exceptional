using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using StackExchange.Exceptional.Extensions;

namespace StackExchange.Exceptional.AzureStore
{
    /// <summary>
    /// An <see cref="ErrorStore"/> implementation that uses Azure Blob storage to store errors as JSON files. 
    /// </summary>
    public class AzureErrorStore : ErrorStore
    {
        private readonly int _size = DefaultSize;
        private readonly string _connectionString;
        public const string IsProtectedKey = "IsProtected";

        /// <summary>
        /// The maximum count of errors stored before the first is overwritten.
        /// </summary>
        public const int MaximumSize = 500;

        /// <summary>
        /// The default maximum count of errors stored before the first is overwritten.
        /// </summary>        
        public const int DefaultSize = 200;

        /// <summary>
        /// Creates a new instance of <see cref="AzureErrorStore"/> with the given configuration.
        /// </summary>        
        public AzureErrorStore(ErrorStoreSettings settings)
            : base(settings)
        {
            _size = Math.Min(settings.Size, MaximumSize);
            _connectionString = settings.ConnectionString.IsNullOrEmpty()
             ? getConnectionStringByName(settings.ConnectionStringName)
             : settings.ConnectionString;

            if (_connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException("settings", "A connection string or connection string name must be specified when using a SQL error store");

            Initialize();
        }

        /// <summary>
        /// Initializes blob container
        /// </summary>
        protected void Initialize()
        {
            var blobContainer = GetBlobContainer();
            blobContainer.CreateIfNotExists();
            blobContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }

        private CloudBlobContainer GetBlobContainer()
        {
            var blobClient = GetStorageAccount().CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("mp-exceptional-errors");
            return blobContainer;
        }

        private CloudStorageAccount GetStorageAccount()
        {
            return CloudStorageAccount.Parse(_connectionString);
        }

        static string getConnectionStringByName(string connectionStringName)
        {
            if (connectionStringName.IsNullOrEmpty()) return null;

            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (connectionString == null)
                throw new ArgumentOutOfRangeException("connectionStringName", "A connection string was not found for the connection string name provided");
            return connectionString.ConnectionString;
        }

        /// <summary>
        /// Creates a new instance of <see cref="AzureErrorStore"/> with the specified path string.
        /// </summary>
        /// <param name="connectionString">The folder path to use to store errors</param>
        /// <param name="size">How many errors to limit the log to, the size+1th error (oldest) will be removed if exceeded</param>
        /// <param name="rollupSeconds">The rollup seconds, defaults to <see cref="ErrorStore.DefaultRollupSeconds"/>, duplicate errors within this time period will be rolled up</param>
        public AzureErrorStore(string connectionString, int size = DefaultSize, int rollupSeconds = DefaultRollupSeconds)
            : base(rollupSeconds)
        {
            _size = Math.Min(size, MaximumSize);
            if (connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException("connectionString", "Connection string must be specified when using a SQL error store");

            _connectionString = connectionString;
        }

        /// <summary>
        /// Name for this error store
        /// </summary>
        public override string Name { get { return "Azure Blob Storage Error Store"; } }

        /// <summary>
        /// Protects an error from deletion, by making it ReadOnly
        /// </summary>
        /// <param name="guid">The guid of the error to protect</param>
        /// <returns>True if the error was found and proected, false otherwise</returns>
        protected override bool ProtectError(Guid guid)
        {
            var blob = TryGetBlob(guid);

            if (blob == null)
            {
                return false;
            }

            try
            {
                blob.Metadata[IsProtectedKey] = true.ToString();
                blob.SetMetadata();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private ICloudBlob TryGetBlob(Guid guid)
        {
            var blobItems = GetBlobContainer().ListBlobs("error-" + guid.ToFileName()).ToArray();

            if (blobItems.Length == 0)
            {
                return null;
            }

            try
            {
                return GetBlobContainer().GetBlobReferenceFromServer(blobItems[0].Uri.ToString());
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes an error, by deleting it from the logging folder
        /// </summary>
        /// <param name="guid">The guid of the error to delete</param>
        /// <returns>True if the error was found and deleted, false otherwise</returns>
        protected override bool DeleteError(Guid guid)
        {
            var blob = TryGetBlob(guid);

            if (blob == null)
            {
                return false;
            }

            blob.Delete();

            return true;
        }

        /// <summary>
        /// Deleted all errors in the log, by clearing all *.json files in the folder
        /// </summary>
        /// <returns>True if any errors were deleted, false otherwise</returns>
        protected override bool DeleteAllErrors()
        {
            try
            {
                CloudBlobContainer blobContainer = GetBlobContainer();
                foreach (var blobItem in blobContainer.ListBlobs())
                {
                    var blob = blobContainer.GetBlobReferenceFromServer(blobItem.Uri.ToString());
                    if (!isProtected(blob))
                    {
                        blob.Delete();
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs the JSON representation of an Error to the file store specified by the page for this store
        /// If the rollup conditions are met, then the matching error will have a DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new row for the error
        /// </summary>
        /// <param name="error">The error to log</param>
        protected override void LogError(Error error)
        {
            // will allow fast comparisons of messages to see if we can ignore an incoming exception
            var detailHash = error.ErrorHash.HasValue ? error.ErrorHash.ToString() : "no-stack-trace";
            Error original;

            // before we persist 'error', see if there are any existing errors that it could be a duplicate of
            if (TryFindOriginalError(detailHash, out original))
            {
                // just update the existing file after incrementing its "duplicate count"
                original.DuplicateCount = original.DuplicateCount.GetValueOrDefault(0) + error.DuplicateCount;
                error.GUID = original.GUID;

                ICloudBlob blob = TryGetBlob(original.GUID);
                if (blob == null)
                    throw new ArgumentOutOfRangeException("Unable to find a file for error with GUID = " + original.GUID);

                WriteToBlob(error, blob);
            }
            else
            {
                string timeStamp = DateTime.UtcNow.ToString("u").Replace(":", "").Replace(" ", "").Replace("-", "");
                string blobName = string.Format(@"error-{0}-{1}-{2}.json", error.GUID.ToFileName(), timeStamp, detailHash);

                CloudBlockBlob blockBlob = GetBlobContainer().GetBlockBlobReference(blobName);
                WriteToBlob(error, blockBlob);

                // we added a new file, so clean up old smack over our max errors limit
                RemoveOldErrors();
            }
        }

        private static void WriteToBlob(Error error, ICloudBlob blob)
        {
            byte[] bytes = Encoding.Default.GetBytes(error.ToJson());
            blob.UploadFromByteArray(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Gets the error with the specified guid from the log/folder
        /// </summary>
        /// <param name="guid">The guid of the error to retrieve</param>
        /// <returns>The error object if found, null otherwise</returns>
        protected override Error GetError(Guid guid)
        {
            IListBlobItem[] fileList = GetBlobContainer().ListBlobs("error-" + guid.ToFileName()).ToArray();

            if (fileList.Length < 1)
                return null;

            return Get(fileList[0]);
        }

        private Error Get(IListBlobItem blobItem)
        {
            if (blobItem == null)
                return null;
            try
            {
                ICloudBlob blob = GetBlobContainer().GetBlobReferenceFromServer(blobItem.Uri.ToString());

                using (var stream = new MemoryStream())
                {
                    blob.DownloadToStream(stream);
                    stream.Position = 0;
                    using (var streamReader = new StreamReader(stream))
                    {
                        string json = streamReader.ReadToEnd();
                        var result = Error.FromJson(json);
                        result.IsProtected = isProtected(blob);
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves all of the errors in the log folder
        /// </summary>
        protected override int GetAllErrors(List<Error> errors)
        {
            IListBlobItem[] blobItems = GetBlobContainer().ListBlobs().ToArray();

            if (blobItems.Length < 1) return 0;

            blobItems = SortBlobItems(blobItems);

            errors.AddRange(blobItems.Select(Get).Where(e => e != null));

            return blobItems.Length;
        }

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if null
        /// </summary>
        protected override int GetErrorCount(DateTime? since = null)
        {
            IListBlobItem[] blobItems = GetBlobContainer().ListBlobs().ToArray();

            if (!since.HasValue) return blobItems.Length;

            var i = 0;
            foreach (var fn in SortBlobItems(blobItems))
            {
                var error = Get(fn);
                if (error == null)
                    continue;

                if (error.CreationDate >= since)
                    i += error.DuplicateCount ?? 1;
                else
                    break; // exit as soon as we passed the date we're looking back to
            }
            return i;
        }

        //private bool TryGetErrorFile(Guid guid, out FileInfo file)
        //{
        //    string[] fileList = Directory.GetFiles(_path, string.Format("*{0}.json", guid.ToFileName()));

        //    if (fileList.Length != 1)
        //    {
        //        file = null;
        //        return false;
        //    }

        //    file = new FileInfo(fileList[0]);
        //    return true;
        //}

        /// <summary>
        /// Answers the older exception that 'possibleDuplicate' matches, returning null if no match is found.
        /// </summary>
        private bool TryFindOriginalError(string messageHash, out Error original)
        {
            if (!RollupThreshold.HasValue || RollupThreshold.Value.Ticks == 0)
            {
                original = null;
                return false;
            }

            CloudBlobContainer blobContainer = GetBlobContainer();
            IListBlobItem[] blobItems = blobContainer.ListBlobs().ToArray();

            if (blobItems.Length > 0)
            {
                var earliestDate = DateTime.UtcNow.Add(RollupThreshold.Value.Negate());

                // order by newest                
                blobItems = SortBlobItems(blobItems);

                foreach (var blobItem in blobItems.Where(fn => fn.Uri.ToString().Contains("error-")))
                {
                    string blobName = blobItem.Uri.ToString();
                    ICloudBlob blob = blobContainer.GetBlobReferenceFromServer(blobName);
                    if (blob.Properties.LastModified >= earliestDate)
                    {
                        //                        string blobName = string.Format(@"error-{0}-{1}-{2}.json", error.GUID.ToFileName(), timeStamp, detailHash);
                        var match = Regex.Match(blobName, @"error-(?<guid>.+)-(?<timeStamp>.+)-(?<hashCode>((?<!\d)-|\d)+)\.json", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            var existingHash = match.Groups["hashCode"].Value;
                            if (messageHash.Equals(existingHash))
                            {
                                original = GetError(match.Groups["guid"].Value.ToGuid());
                                return true;
                            }
                        }
                    }
                    else
                        break; // no other files are newer, no use checking
                }
            }

            original = null;
            return false;
        }

        private static IListBlobItem[] SortBlobItems(IListBlobItem[] blobItems)
        {
            return blobItems
                .OrderByDescending(x => x.Uri.ToString().Substring(39)).
                ToArray();
        }

        private void RemoveOldErrors()
        {
            CloudBlobContainer blobContainer = GetBlobContainer();
            IListBlobItem[] blobItems = blobContainer.ListBlobs().ToArray();

            if (blobItems.Length <= _size) return; // room left below the cap

            Array.Sort(blobItems); // sort by timestamps

            // we'll remove any errors with index less than this upper bound
            int upperBound = blobItems.Length - _size;

            for (int i = 0; i < upperBound && i < blobItems.Length; i++)
            {
                var blob = blobContainer.GetBlobReferenceFromServer(blobItems[i].Uri.ToString());
                // have we protected this error from deletion?
                if (isProtected(blob))
                {
                    // we'll skip this error file and raise our search bounds up one
                    upperBound++;
                }
                else
                {
                    blob.Delete();
                }
            }
        }

        private static bool isProtected(ICloudBlob blob)
        {
            if (blob.Metadata.ContainsKey(IsProtectedKey))
            {
                return bool.Parse(blob.Metadata[IsProtectedKey]);
            }

            return false;
        }
    }

}
