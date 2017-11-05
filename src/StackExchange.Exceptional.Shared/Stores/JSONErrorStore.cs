using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using StackExchange.Exceptional.Internal;
using System.Threading.Tasks;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// An <see cref="ErrorStore"/> implementation that uses JSON files as its backing store. 
    /// </summary>
    public sealed class JSONErrorStore : ErrorStore
    {
        /// <summary>
        /// Name for this error store.
        /// </summary>
        public override string Name => "JSON File Error Store";

        private readonly string _path;
        private readonly int _size = DefaultSize;

        /// <summary>
        /// The maximum count of errors stored before the first is overwritten.
        /// </summary>
        public const int MaximumSize = 500;

        /// <summary>
        /// The default maximum count of errors stored before the first is overwritten.
        /// </summary>        
        public const int DefaultSize = 200;

        /// <summary>
        /// Creates a new instance of <see cref="JSONErrorStore"/> with the specified path string.
        /// </summary>
        /// <param name="path">The folder path to use to store errors.</param>
        /// <param name="size">How many errors to limit the log to, the size+1th error (oldest) will be removed if exceeded.</param>
        public JSONErrorStore(string path, int size = DefaultSize)
            : this(new ErrorStoreSettings
            {
                Path = path,
                Size = size
            })
        { }

        /// <summary>
        /// Creates a new instance of <see cref="JSONErrorStore"/> with the given configuration.
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>    
        public JSONErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            _size = Math.Min(settings.Size, MaximumSize);
            _path = settings.Path.ResolvePath();
        }

        /// <summary>
        /// Protects an error from deletion, by making it read-only.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect</param>
        /// <returns><c>true</c> if the error was found and protected, <c>false</c> otherwise.</returns>
        protected override Task<bool> ProtectErrorAsync(Guid guid)
        {
            if (!TryGetErrorFile(guid, out FileInfo f))
                return Task.FromResult(false);

            f.Attributes |= FileAttributes.ReadOnly;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Deletes an error, by deleting it from the logging folder.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override Task<bool> DeleteErrorAsync(Guid guid)
        {
            if (!TryGetErrorFile(guid, out FileInfo f))
                return Task.FromResult(false);

            if (f.IsReadOnly)
                f.Attributes ^= FileAttributes.ReadOnly;

            f.Delete();
            return Task.FromResult(true);
        }

        /// <summary>
        /// Deleted all errors in the log, by clearing all *.json files in the folder.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        /// <returns><c>true</c> if any errors were deleted, <c>false</c> otherwise.</returns>
        protected override Task<bool> DeleteAllErrorsAsync(string applicationName = null)
        {
            string[] fileList = Directory.GetFiles(_path, "*.json");
            if (fileList.Length == 0)
                return Task.FromResult(false);

            var deleted = 0;
            foreach (var fn in fileList)
            {
                try
                {
                    // ignore protected files
                    var f = new FileInfo(fn);
                    if (f.IsReadOnly) continue;
                    if (applicationName.HasValue())
                    {
                        var e = Get(f.FullName);
                        if (e != null && e.ApplicationName == applicationName)
                        {
                            f.Delete();
                        }
                    }
                    else
                    {
                        f.Delete();
                    }
                    deleted++;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error Deleting file '" + fn + "' from the store: " + ex.Message);
                }
            }
            return Task.FromResult(deleted > 0);
        }

        /// <summary>
        /// Logs the JSON representation of an Error to the file store specified by the page for this store.
        /// If the roll-up conditions are met, then the matching error will have a 
        /// DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new file for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override bool LogError(Error error)
        {
            // will allow fast comparisons of messages to see if we can ignore an incoming exception
            var detailHash = error.ErrorHash?.ToString() ?? "no-stack-trace";

            // before we persist 'error', see if there are any existing errors that it could be a duplicate of
            if (TryFindOriginalError(detailHash, out Error original))
            {
                // just update the existing file after incrementing its "duplicate count"
                original.DuplicateCount = original.DuplicateCount.GetValueOrDefault(0) + error.DuplicateCount;
                // Update the LastLogDate to the latest occurrence
                if (original.LastLogDate == null || error.CreationDate > original.LastLogDate)
                {
                    original.LastLogDate = error.CreationDate;
                }
                error.GUID = original.GUID;

                if (!TryGetErrorFile(original.GUID, out FileInfo f))
                    throw new ArgumentOutOfRangeException("Unable to find a file for error with GUID = " + original.GUID.ToString());

                using (var stream = f.Open(FileMode.Create))
                using (var writer = new StreamWriter(stream))
                {
                    LogError(original, writer);
                }
            }
            else
            {
                string timeStamp = DateTime.UtcNow.ToString("u").Replace(":", "").Replace(" ", "");
                string fileName = $"{_path}/error-{timeStamp}-{detailHash}-{error.GUID.ToString("N")}.json";

                var file = new FileInfo(fileName);
                using (var outstream = file.CreateText())
                {
                    LogError(error, outstream);
                }

                // we added a new file, so clean up old smack over our max errors limit
                RemoveOldErrors();
            }
            return true;
        }

        private static void LogError(Error error, StreamWriter outstream)
        {
            var json = error.ToJson();
            outstream.Write(json); //TODO: consider making this async
            outstream.Flush();
        }

        /// <summary>
        /// Gets the error with the specified GUID from the log/folder.
        /// </summary>
        /// <param name="guid">The GUID of the error to retrieve.</param>
        /// <returns>The error object if found, null otherwise.</returns>
        protected override Task<Error> GetErrorAsync(Guid guid) => Task.FromResult(GetError(guid));

        private Error GetError(Guid guid)
        {
            string[] fileList = Directory.GetFiles(_path, $"*{guid.ToString("N")}.json");

            if (fileList.Length < 1)
                return null;

            return Get(fileList[0]);
        }

        private Error Get(string path)
        {
            if (path.IsNullOrEmpty())
                return null;

            var file = new FileInfo(path);
            if (!file.Exists)
                return null;

            try
            {
                string json;
                using (var fs = file.OpenRead())
                using (var sr = new StreamReader(fs))
                    json = sr.ReadToEnd();

                var result = Error.FromJson(json);
                result.IsProtected = file.IsReadOnly;
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves all of the errors in the log folder.
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        protected override Task<List<Error>> GetAllErrorsAsync(string applicationName = null)
        {
            string[] files = Directory.GetFiles(_path, "*.json");

            if (files.Length < 1) return Task.FromResult(new List<Error>());

            Array.Sort(files);
            Array.Reverse(files);

            var result = files.Select(Get).Where(e => e != null);
            if (applicationName.HasValue())
            {
                result = result.Where(e => e.ApplicationName == applicationName);
            }

            return Task.FromResult(result.ToList());
        }

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if null.
        /// </summary>
        /// <param name="since">The date to get errors since.</param>
        /// <param name="applicationName">The application name to get an error count for.</param>
        protected override Task<int> GetErrorCountAsync(DateTime? since = null, string applicationName = null)
        {
            string[] fileList = Directory.GetFiles(_path, "*.json");

            if (!since.HasValue) return Task.FromResult(fileList.Length);

            var i = 0;
            foreach (var fn in fileList.ToList().OrderByDescending(f => f))
            {
                var error = Get(fn);
                if (error == null)
                    continue;

                if (applicationName.HasValue() && error.ApplicationName != applicationName)
                    continue;

                if (error.CreationDate >= since)
                    i += error.DuplicateCount ?? 1;
                else
                    break; // exit as soon as we passed the date we're looking back to
            }
            return Task.FromResult(i);
        }

        private bool TryGetErrorFile(Guid guid, out FileInfo file)
        {
            string[] fileList = Directory.GetFiles(_path, $"*{guid.ToString("N")}.json");

            if (fileList.Length != 1)
            {
                file = null;
                return false;
            }

            file = new FileInfo(fileList[0]);
            return true;
        }

        /// <summary>
        /// Answers the older exception that 'possibleDuplicate' matches, returning null if no match is found.
        /// </summary>
        /// <param name="messageHash">The hash of the error message (located in the filename).</param>
        /// <param name="original">The original error, if found. <c>null</c> if no matches are found.</param>
        private bool TryFindOriginalError(string messageHash, out Error original)
        {
            if (!Settings.RollupPeriod.HasValue || Settings.RollupPeriod == TimeSpan.Zero)
            {
                original = null;
                return false;
            }

            string[] files = Directory.GetFiles(_path, "*.json");

            if (files.Length > 0)
            {
                var earliestDate = DateTime.UtcNow.Subtract(Settings.RollupPeriod.Value);

                // order by newest
                Array.Sort(files);
                Array.Reverse(files);

                foreach (var filename in files.Where(fn => fn.Contains("error-")))
                {
                    if (File.GetCreationTimeUtc(filename) >= earliestDate)
                    {
                        var match = Regex.Match(filename, @"error[-\d]+Z-(?<hashCode>((?<!\d)-|\d)+)-(?<guid>.+)\.json", RegexOptions.IgnoreCase);
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
                    {
                        break; // no other files are newer, no use checking
                    }
                }
            }

            original = null;
            return false;
        }

        private void RemoveOldErrors()
        {
            string[] files = Directory.GetFiles(_path, "error*.*");

            if (files.Length <= _size) return; // room left below the cap

            Array.Sort(files); // sort by timestamps

            // we'll remove any errors with index less than this upper bound
            int upperBound = files.Length - _size;

            for (int i = 0; i < upperBound && i < files.Length; i++)
            {
                var file = new FileInfo(files[i]);
                // have we protected this error from deletion?
                if ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    // we'll skip this error file and raise our search bounds up one
                    upperBound++;
                }
                else
                {
                    file.Delete();
                }
            }
        }
    }
}
