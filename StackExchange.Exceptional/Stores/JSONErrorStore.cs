using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using StackExchange.Exceptional.Extensions;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// An <see cref="ErrorStore"/> implementation that uses JSON files as its backing store. 
    /// </summary>
    public sealed class JSONErrorStore : ErrorStore
    {
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
        /// Creates a new instance of <see cref="JSONErrorStore"/> with the given configuration.
        /// </summary>        
        public JSONErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            _size = Math.Min(settings.Size, MaximumSize);
            _path = settings.Path.ResolvePath();
        }

        /// <summary>
        /// Creates a new instance of <see cref="JSONErrorStore"/> with the specified path string.
        /// </summary>
        /// <param name="path">The folder path to use to store errors</param>
        /// <param name="size">How many errors to limit the log to, the size+1th error (oldest) will be removed if exceeded</param>
        /// <param name="rollupSeconds">The rollup seconds, defaults to <see cref="ErrorStore.DefaultRollupSeconds"/>, duplicate errors within this time period will be rolled up</param>
        public JSONErrorStore(string path, int size = DefaultSize, int rollupSeconds = DefaultRollupSeconds) : base(rollupSeconds)
        {
            _size = Math.Min(size, MaximumSize);
            _path = path.ResolvePath();
        }

        /// <summary>
        /// Name for this error store
        /// </summary>
        public override string Name { get { return "JSON File Error Store"; } }

        /// <summary>
        /// Protects an error from deletion, by making it ReadOnly
        /// </summary>
        /// <param name="guid">The guid of the error to protect</param>
        /// <returns>True if the error was found and proected, false otherwise</returns>
        protected override bool ProtectError(Guid guid)
        {
            FileInfo f;
            if (!TryGetErrorFile(guid, out f))
                return false;

            f.Attributes |= FileAttributes.ReadOnly;
            return true;
        }

        /// <summary>
        /// Deletes an error, by deleting it from the logging folder
        /// </summary>
        /// <param name="guid">The guid of the error to delete</param>
        /// <returns>True if the error was found and deleted, false otherwise</returns>
        protected override bool DeleteError(Guid guid)
        {
            FileInfo f;
            if (!TryGetErrorFile(guid, out f))
                return false;

            if (f.IsReadOnly) 
                f.Attributes ^= FileAttributes.ReadOnly;

            f.Delete();
            return true;
        }

        /// <summary>
        /// Deleted all errors in the log, by clearing all *.json files in the folder
        /// </summary>
        /// <returns>True if any errors were deleted, false otherwise</returns>
        protected override bool DeleteAllErrors(string applicationName = null)
        {
            string[] fileList = Directory.GetFiles(_path, "*.json");
            if (fileList.Length == 0)
                return false;

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
            return deleted > 0;
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

                FileInfo f;
                if (!TryGetErrorFile(original.GUID, out f))
                    throw new ArgumentOutOfRangeException("Unable to find a file for error with GUID = " + original.GUID);

                using (var stream = f.Open(FileMode.Create))
                using (var writer = new StreamWriter(stream))
                {
                    LogError(original, writer);
                }
            }
            else
            {
                string timeStamp = DateTime.UtcNow.ToString("u").Replace(":", "").Replace(" ", "");
                string fileName = string.Format(@"{0}\error-{1}-{2}-{3}.json", _path, timeStamp, detailHash, error.GUID.ToFileName());

                var file = new FileInfo(fileName);
                using (var outstream = file.CreateText())
                {
                    LogError(error, outstream);
                }

                // we added a new file, so clean up old smack over our max errors limit
                RemoveOldErrors();
            }
        }

        private void LogError(Error error, StreamWriter outstream)
        {
            var json = error.ToJson();
            outstream.Write(json); //TODO: consider making this async
            outstream.Flush();
        }

        /// <summary>
        /// Gets the error with the specified guid from the log/folder
        /// </summary>
        /// <param name="guid">The guid of the error to retrieve</param>
        /// <returns>The error object if found, null otherwise</returns>
        protected override Error GetError(Guid guid)
        {
            string[] fileList = Directory.GetFiles(_path, string.Format("*{0}.json", guid.ToFileName()));
            
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
        /// Retrieves all of the errors in the log folder
        /// </summary>
        protected override int GetAllErrors(List<Error> errors, string applicationName = null)
        {
            string[] files = Directory.GetFiles(_path, "*.json");

            if (files.Length < 1) return 0;

            Array.Sort(files);
            Array.Reverse(files);

            var result = files.Select(Get).Where(e => e != null);
            if (applicationName.HasValue())
            {
                result = result.Where(e => e.ApplicationName == applicationName);
            }

            errors.AddRange(result);

            return files.Length;
        }

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if null
        /// </summary>
        protected override int GetErrorCount(DateTime? since = null, string applicationName = null)
        {
            string[] fileList = Directory.GetFiles(_path, "*.json");

            if (!since.HasValue) return fileList.Length;

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
            return i;
        }
        
        private bool TryGetErrorFile(Guid guid, out FileInfo file)
        {
            string[] fileList = Directory.GetFiles(_path, string.Format("*{0}.json", guid.ToFileName()));

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
        private bool TryFindOriginalError(string messageHash, out Error original)
        {
            if (!RollupThreshold.HasValue || RollupThreshold.Value.Ticks == 0)
            {
                original = null;
                return false;
            }

            string[] files = Directory.GetFiles(_path, "*.json");

            if (files.Length > 0)
            {
                var earliestDate = DateTime.UtcNow.Add(RollupThreshold.Value.Negate());

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
                        break; // no other files are newer, no use checking
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