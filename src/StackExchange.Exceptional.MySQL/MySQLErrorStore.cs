using System;
using System.Collections.Generic;
using Dapper;
using MySql.Data.MySqlClient;
using StackExchange.Exceptional.Stores;
using StackExchange.Exceptional.Internal;
using System.Threading.Tasks;

namespace StackExchange.Exceptional.MySQL
{
    /// <summary>
    /// An <see cref="ErrorStore" /> implementation that uses MySQL as its backing store.
    /// </summary>
    public sealed class MySQLErrorStore : ErrorStore
    {
        /// <summary>
        /// Name for this error store.
        /// </summary>
        public override string Name => "MySQL Error Store";

        /// <summary>
        /// The maximum count of errors to show.
        /// </summary>
        public const int MaximumDisplayCount = 500;

        /// <summary>
        /// The default maximum count of errors shown at once.
        /// </summary>
        public const int DefaultDisplayCount = 200;

        private readonly string _connectionString;
        private readonly int _displayCount = DefaultDisplayCount;

        /// <summary>
        /// Creates a new instance of <see cref="MySQLErrorStore" /> with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The database connection string to use.</param>
        /// <param name="applicationName">The application name to use when logging.</param>
        /// <param name="displayCount">How many errors to display in the log (for display ONLY, the log is not truncated to this value).</param>
        public MySQLErrorStore(string connectionString, string applicationName, int displayCount = DefaultDisplayCount)
            : this(new ErrorStoreSettings()
            {
                ApplicationName = applicationName,
                ConnectionString = connectionString,
                Size = displayCount
            }) { }

        /// <summary>
        /// Creates a new instance of <see cref="MySQLErrorStore" /> with the given configuration.
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>     
        public MySQLErrorStore(ErrorStoreSettings settings)
            : base(settings)
        {
            _displayCount = Math.Min(settings.Size, MaximumDisplayCount);
            _connectionString = settings.ConnectionString;

            if (_connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "A connection string or connection string name must be specified when using a SQL error store");
        }

        /// <summary>
        /// Protects an error from deletion, by making IsProtected = 1 in the database.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect.</param>
        /// <returns><c>true</c> if the error was found and protected, <c>false</c> otherwise.</returns>
        protected override async Task<bool> ProtectErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(@"
Update Exceptions 
   Set IsProtected = 1, DeletionDate = Null
 Where GUID = @guid", new { guid }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Protects errors from deletion, by making IsProtected = 1 in the database.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to protect.</param>
        /// <returns><c>true</c> if the errors were found and protected, <c>false</c> otherwise.</returns>
        protected override async Task<bool> ProtectErrorsAsync(IEnumerable<Guid> guids)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(@"
Update Exceptions 
   Set IsProtected = 1, DeletionDate = Null
 Where GUID In @guids", new { guids }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Deletes an error, by setting DeletionDate = UTC_DATE() in SQL.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(@"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where GUID = @guid 
   And DeletionDate Is Null", new { guid, ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Deletes errors, by setting DeletionDate = UTC_DATE() in SQL.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to delete.</param>
        /// <returns><c>true</c> if the errors were found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorsAsync(IEnumerable<Guid> guids)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(@"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where GUID In @guids
   And DeletionDate Is Null", new { guids }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Hard deletes an error, actually deletes the row from SQL rather than setting <see cref="Error.DeletionDate"/>.
        /// This is used to cleanup when testing the error store when attempting to come out of retry/failover mode after losing connection to SQL.
        /// </summary>
        /// <param name="guid">The GUID of the error to hard delete.</param>
        /// <returns>True if the error was found and deleted, false otherwise.</returns>
        protected override async Task<bool> HardDeleteErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(@"
Delete From Exceptions 
 Where GUID = @guid
   And ApplicationName = @ApplicationName", new { guid, ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Deleted all errors in the log, by setting <see cref="Error.DeletionDate"/> = UTC_DATE() in SQL.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        /// <returns><c>true</c> if any errors were deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteAllErrorsAsync(string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(@"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @ApplicationName", new { ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        private const string _sqlLogUpdate = @"
Update Exceptions 
   Set DuplicateCount = DuplicateCount + @DuplicateCount,
       LastLogDate = (Case When LastLogDate Is Null Or @CreationDate > LastLogDate Then @CreationDate Else LastLogDate End)
 Where ErrorHash = @ErrorHash
   And ApplicationName = @ApplicationName
   And DeletionDate Is Null
   And CreationDate >= @minDate limit 1";

        private const string _sqlLogGUID = @"Select GUID from Exceptions 
 Where ErrorHash = @ErrorHash
   And ApplicationName = @ApplicationName
   And DeletionDate Is Null
   And CreationDate >= @minDate limit 1 ";

        private const string _sqlLogInsert = @"
Insert Into Exceptions (GUID, ApplicationName, Category, MachineName, CreationDate, Type, IsProtected, Host, Url, HTTPMethod, IPAddress, Source, Message, Detail, StatusCode, FullJson, ErrorHash, DuplicateCount, LastLogDate)
Values (@GUID, @ApplicationName, @Category, @MachineName, @CreationDate, @Type, @IsProtected, @Host, @Url, @HTTPMethod, @IPAddress, @Source, @Message, @Detail, @StatusCode, @FullJson, @ErrorHash, @DuplicateCount, @LastLogDate)";

        private DynamicParameters GetUpdateParams(Error error) =>
            new DynamicParameters(new
            {
                error.DuplicateCount,
                error.ErrorHash,
                error.CreationDate,
                ApplicationName = error.ApplicationName.Truncate(50),
                minDate = DateTime.UtcNow.Subtract(Settings.RollupPeriod.Value)
            });

        private object GetInsertParams(Error error) => new
        {
            error.GUID,
            ApplicationName = error.ApplicationName.Truncate(50),
            Category = error.Category.Truncate(100),
            MachineName = error.MachineName.Truncate(50),
            error.CreationDate,
            Type = error.Type.Truncate(100),
            error.IsProtected,
            Host = error.Host.Truncate(100),
            Url = error.Url.Truncate(500),
            HTTPMethod = error.HTTPMethod.Truncate(10),
            error.IPAddress,
            Source = error.Source.Truncate(100),
            Message = error.Message.Truncate(1000),
            error.Detail,
            error.StatusCode,
            error.FullJson,
            error.ErrorHash,
            error.DuplicateCount,
            error.LastLogDate
        };

        /// <summary>
        /// Logs the error to SQL.
        /// If the roll-up conditions are met, then the matching error will have a DuplicateCount += @DuplicateCount (usually 1,
        /// unless in retry) rather than a distinct new row for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override void LogError(Error error)
        {
            using (var c = GetConnection())
            {
                if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
                {
                    var queryParams = GetUpdateParams(error);
                    var count = c.Execute(_sqlLogUpdate, queryParams);
                    // if we found an exception that's a duplicate, jump out
                    if (count > 0)
                    {
                        // MySQL doesn't support OUT parameters, so we need to query for the GUID.
                        error.GUID = c.QueryFirst<Guid>(_sqlLogGUID, queryParams);
                        return;
                    }
                }

                error.FullJson = error.ToJson();

                c.Execute(_sqlLogInsert, GetInsertParams(error));
            }
        }

        /// <summary>
        /// Asynchronously logs the error to SQL.
        /// If the roll-up conditions are met, then the matching error will have a DuplicateCount += @DuplicateCount (usually 1,
        /// unless in retry) rather than a distinct new row for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override async Task LogErrorAsync(Error error)
        {
            using (var c = GetConnection())
            {
                if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
                {
                    var queryParams = GetUpdateParams(error);
                    var count = await c.ExecuteAsync(_sqlLogUpdate, queryParams).ConfigureAwait(false);
                    // if we found an exception that's a duplicate, jump out
                    if (count > 0)
                    {
                        // MySQL doesn't support OUT parameters, so we need to query for the GUID.
                        error.GUID = await c.QueryFirstAsync<Guid>(_sqlLogGUID, queryParams).ConfigureAwait(false);
                        return;
                    }
                }

                error.FullJson = error.ToJson();

                await c.ExecuteAsync(_sqlLogInsert, GetInsertParams(error)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the error with the specified GUID from SQL.
        /// This can return a deleted error as well, there's no filter based on <see cref="Error.DeletionDate"/>.
        /// </summary>
        /// <param name="guid">The GUID of the error to retrieve.</param>
        /// <returns>The error object if found, <c>null</c> otherwise.</returns>
        protected override async Task<Error> GetErrorAsync(Guid guid)
        {
            Error sqlError;
            using (var c = GetConnection())
            {
                sqlError = await c.QueryFirstOrDefaultAsync<Error>(@"
Select * 
  From Exceptions 
 Where GUID = @guid", new { guid }).ConfigureAwait(false);
            }
            if (sqlError == null) return null;

            // everything is in the JSON, but not the columns and we have to deserialize for collections anyway
            // so use that deserialized version and just get the properties that might change on the SQL side and apply them
            Error result = Error.FromJson(sqlError.FullJson);
            result.DuplicateCount = sqlError.DuplicateCount;
            result.DeletionDate = sqlError.DeletionDate;
            result.IsProtected = sqlError.IsProtected;
            result.LastLogDate = sqlError.LastLogDate;
            return result;
        }

        /// <summary>
        /// Retrieves all non-deleted application errors in the database.
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        protected override async Task<List<Error>> GetAllErrorsAsync(string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return (await c.QueryAsync<Error>(@"
Select * 
  From Exceptions 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName
Order By CreationDate Desc limit @max", new { max = _displayCount, ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false)).AsList();
            }
        }

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if <c>null</c>.
        /// </summary>
        /// <param name="since">The date to get errors since.</param>
        /// <param name="applicationName">The application name to get an error count for.</param>
        protected override async Task<int> GetErrorCountAsync(DateTime? since = null, string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return await c.QueryFirstOrDefaultAsync<int>(@"
Select Count(*) 
  From Exceptions 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName" + (since.HasValue ? " And CreationDate > @since" : ""),
                    new { since, ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false);
            }
        }

        private MySqlConnection GetConnection() => new MySqlConnection(_connectionString);
    }
}
