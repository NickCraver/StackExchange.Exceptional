using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// An <see cref="ErrorStore" /> implementation that uses MongoDB as its backing store.
    /// </summary>
    public sealed class MongoDBErrorStore : ErrorStore
    {
        /// <summary>
        /// Name for this error store.
        /// </summary>
        public override string Name => "MongoDB Error Store";

        private readonly string _tableName;
        private readonly int _displayCount;
        private readonly string _connectionString;

        private static bool classMapRegistered;

        /// <summary>
        /// The maximum count of errors to show.
        /// </summary>
        public const int MaximumDisplayCount = 500;

        /// <summary>
        /// Creates a new instance of <see cref="MongoDBErrorStore" /> with the specified connection string.
        /// The default table name is "Exceptions".
        /// </summary>
        /// <param name="connectionString">The database connection string to use.</param>
        /// <param name="applicationName">The application name to use when logging.</param>
        public MongoDBErrorStore(string connectionString, string applicationName)
            : this(new ErrorStoreSettings()
            {
                ApplicationName = applicationName,
                ConnectionString = connectionString
            })
        { }

        /// <summary>
        /// Creates a new instance of <see cref="ErrorStoreSettings"/> with the given configuration.
        /// The default table name is "Exceptions".
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>
        public MongoDBErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            RegisterClassMap();

            _displayCount = Math.Min(settings.Size, MaximumDisplayCount);
            _connectionString = settings.ConnectionString;
            _tableName = settings.TableName ?? "Exceptions";

            if (_connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "A connection string or connection string name must be specified when using a MongoDB error store");
        }

        private static void RegisterClassMap()
        {
            if (classMapRegistered) { return; }
            BsonClassMap.RegisterClassMap<Error>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.GUID);
                foreach (var prop in typeof(Error).GetMembers())
                {
                    if (prop.CustomAttributes.Any(x => x.AttributeType == typeof(JsonIgnoreAttribute)))
                    {
                        cm.UnmapMember(prop);
                    }
                }
            });
            classMapRegistered = true;
        }

        /// <summary>
        /// Protects an error from deletion, by making IsProtected = true in the database.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect.</param>
        /// <returns><c>true</c> if the error was found and protected, <c>false</c> otherwise.</returns>
        protected override async Task<bool> ProtectErrorAsync(Guid guid)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.Eq(x => x.GUID, guid);
            var update = Builders<Error>.Update.Set(x => x.DeletionDate, null).Set(x => x.IsProtected, true);
            var res = await c.UpdateOneAsync(filter, update).ConfigureAwait(false);
            return res.IsAcknowledged;
        }

        /// <summary>
        /// Protects errors from deletion, by making IsProtected = true in the database.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to protect.</param>
        /// <returns><c>true</c> if the errors were found and protected, <c>false</c> otherwise.</returns>
        protected override async Task<bool> ProtectErrorsAsync(IEnumerable<Guid> guids)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.In(x => x.GUID, guids);
            var update = Builders<Error>.Update.Set(x => x.DeletionDate, null).Set(x => x.IsProtected, true);
            var res = await c.UpdateManyAsync(filter, update).ConfigureAwait(false);
            return res.IsAcknowledged;
        }

        /// <summary>
        /// Deletes an error, by setting <see cref="Error.DeletionDate"/> = <see cref="DateTime.UtcNow"/>.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorAsync(Guid guid)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.Eq(x => x.GUID, guid) & Builders<Error>.Filter.Eq(x => x.DeletionDate, null);
            var update = Builders<Error>.Update.Set(x => x.DeletionDate, DateTime.UtcNow);
            var res = await c.UpdateOneAsync(filter, update).ConfigureAwait(false);
            return res.IsAcknowledged;
        }

        /// <summary>
        /// Deletes errors, by setting <see cref="Error.DeletionDate"/> = <see cref="DateTime.UtcNow"/>.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to delete.</param>
        /// <returns><c>true</c> if the errors were found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorsAsync(IEnumerable<Guid> guids)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.In(x => x.GUID, guids) & Builders<Error>.Filter.Eq(x => x.DeletionDate, null);
            var update = Builders<Error>.Update.Set(x => x.DeletionDate, DateTime.UtcNow);
            var res = await c.UpdateManyAsync(filter, update).ConfigureAwait(false);
            return res.IsAcknowledged;
        }

        /// <summary>
        /// Hard deletes an error, actually deletes the document from MongoDB rather than setting <see cref="Error.DeletionDate"/>.
        /// This is used to cleanup when testing the error store when attempting to come out of retry/failover mode after losing connection to MongoDB.
        /// </summary>
        /// <param name="guid">The GUID of the error to hard delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> HardDeleteErrorAsync(Guid guid)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.Eq(x => x.GUID, guid) & Builders<Error>.Filter.Eq(x => x.ApplicationName, ApplicationName);
            var res = await c.DeleteOneAsync(filter).ConfigureAwait(false);
            return res.IsAcknowledged;
        }

        /// <summary>
        /// Deleted all errors in the log, by setting <see cref="Error.DeletionDate"/> = <see cref="DateTime.UtcNow"/>.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        /// <returns><c>true</c> if any errors were deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteAllErrorsAsync(string applicationName = null)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.Eq(x => x.ApplicationName, applicationName ?? ApplicationName) & Builders<Error>.Filter.Eq(x => x.IsProtected, false) & Builders<Error>.Filter.Eq(x => x.DeletionDate, null);
            var update = Builders<Error>.Update.Set(x => x.DeletionDate, DateTime.UtcNow);
            var res = await c.UpdateManyAsync(filter, update).ConfigureAwait(false);
            return res.IsAcknowledged;
        }

        /// <summary>
        /// Logs the error to MongoDB.
        /// If the roll-up conditions are met, then the matching error will have a 
        /// DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new document for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override bool LogError(Error error)
        {
            var c = GetConnection();
            if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
            {
                var filter = Builders<Error>.Filter.Eq(x => x.ApplicationName, error.ApplicationName)
                    & Builders<Error>.Filter.Eq(x => x.ErrorHash, error.ErrorHash)
                    & Builders<Error>.Filter.Eq(x => x.DeletionDate, null)
                    & Builders<Error>.Filter.Gte(x => x.CreationDate, DateTime.UtcNow.Subtract(Settings.RollupPeriod.Value));
                var duplicate = c.Find(filter).FirstOrDefault();
                // if we found an exception that's a duplicate, jump out
                if (duplicate != null)
                {
                    filter = Builders<Error>.Filter.Eq(x => x.GUID, duplicate.GUID);
                    var update = Builders<Error>.Update.Set(x => x.DuplicateCount, duplicate.DuplicateCount + 1);
                    if (duplicate.LastLogDate == null || error.CreationDate > duplicate.LastLogDate)
                    {
                        update = update.Set(x => x.LastLogDate, error.CreationDate);
                    }
                    c.UpdateOne(filter, update);
                    error.GUID = duplicate.GUID;
                    return true;
                }
            }

            c.InsertOne(error);
            return true;
        }

        /// <summary>
        /// Asynchronously logs the error to MongoDB.
        /// If the roll-up conditions are met, then the matching error will have a 
        /// DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new document for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override async Task<bool> LogErrorAsync(Error error)
        {
            var c = GetConnection();
            if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
            {
                var filter = Builders<Error>.Filter.Eq(x => x.ApplicationName, error.ApplicationName)
                    & Builders<Error>.Filter.Eq(x => x.ErrorHash, error.ErrorHash)
                    & Builders<Error>.Filter.Eq(x => x.DeletionDate, null)
                    & Builders<Error>.Filter.Gte(x => x.CreationDate, DateTime.UtcNow.Subtract(Settings.RollupPeriod.Value));
                var duplicate = await c.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
                // if we found an exception that's a duplicate, jump out
                if (duplicate != null)
                {
                    filter = Builders<Error>.Filter.Eq(x => x.GUID, duplicate.GUID);
                    var update = Builders<Error>.Update.Set(x => x.DuplicateCount, duplicate.DuplicateCount + 1);
                    if (duplicate.LastLogDate == null || error.CreationDate > duplicate.LastLogDate)
                    {
                        update = update.Set(x => x.LastLogDate, error.CreationDate);
                    }
                    await c.UpdateOneAsync(filter, update).ConfigureAwait(false);
                    error.GUID = duplicate.GUID;
                    return true;
                }
            }

            await c.InsertOneAsync(error).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Gets the error with the specified GUID from MongoDB.
        /// This can return a deleted error as well, there's no filter based on <see cref="Error.DeletionDate"/>.
        /// </summary>
        /// <param name="guid">The GUID of the error to retrieve.</param>
        /// <returns>The error object if found, <c>null</c> otherwise.</returns>
        protected override async Task<Error> GetErrorAsync(Guid guid)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.Eq(x => x.GUID, guid);
            return await c.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all non-deleted application errors in the database.
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        protected override async Task<List<Error>> GetAllErrorsAsync(string applicationName = null)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.Eq(x => x.ApplicationName, applicationName ?? ApplicationName) & Builders<Error>.Filter.Eq(x => x.DeletionDate, null);
            var errors = await c.Find(filter).SortByDescending(x => x.CreationDate).Limit(_displayCount).ToListAsync().ConfigureAwait(false);

            return errors.ToList();
        }

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if <c>null</c>.
        /// </summary>
        /// <param name="since">The date to get errors since.</param>
        /// <param name="applicationName">The application name to get an error count for.</param>
        protected override async Task<int> GetErrorCountAsync(DateTime? since = null, string applicationName = null)
        {
            var c = GetConnection();
            var filter = Builders<Error>.Filter.Eq(x => x.ApplicationName, applicationName ?? ApplicationName) & Builders<Error>.Filter.Eq(x => x.DeletionDate, null);
            if (since.HasValue)
            {
                filter &= Builders<Error>.Filter.Gt(x => x.CreationDate, since.Value);
            }
            var count = await c.Find(filter).CountDocumentsAsync().ConfigureAwait(false);
            return (int)count;
        }

        private IMongoCollection<Error> GetConnection()
        {
            var databaseName = new MongoUrl(_connectionString).DatabaseName;
            return new MongoClient(_connectionString).GetDatabase(databaseName).GetCollection<Error>(_tableName);
        }
    }
}
