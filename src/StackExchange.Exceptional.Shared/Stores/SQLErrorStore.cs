using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System;
using Dapper;
using StackExchange.Exceptional.Internal;
using System.Threading.Tasks;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// An <see cref="ErrorStore"/> implementation that uses SQL Server as its backing store. 
    /// </summary>
    public sealed class SQLErrorStore : ErrorStore
    {
        /// <summary>
        /// Name for this error store.
        /// </summary>
        public override string Name => "SQL Error Store";

        private readonly int _displayCount = DefaultDisplayCount;
        private readonly string _connectionString;
        private readonly string _schema;

        /// <summary>
        /// The maximum count of errors to show.
        /// </summary>
        public const int MaximumDisplayCount = 500;

        /// <summary>
        /// The default maximum count of errors shown at once.
        /// </summary>        
        public const int DefaultDisplayCount = 200;

        /// <summary>
        /// Creates a new instance of <see cref="SQLErrorStore"/> with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The database connection string to use.</param>
        /// <param name="schema">The database schema to use.</param>
        /// <param name="displayCount">How many errors to display in the log (for display ONLY, the log is not truncated to this value).</param>
        public SQLErrorStore(string connectionString, string schema, int displayCount = DefaultDisplayCount)
            : this(new ErrorStoreSettings()
            {
                ConnectionString = connectionString,
                Schema = schema,
                Size = displayCount
            })
        { }

        /// <summary>
        /// Creates a new instance of <see cref="SQLErrorStore"/> with the given configuration.
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>        
        public SQLErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            _displayCount = Math.Min(settings.Size, MaximumDisplayCount);
            _schema = settings.Schema;

#if NETSTANDARD2_0
            _connectionString = settings.ConnectionString;
#else
            _connectionString = settings.ConnectionString.IsNullOrEmpty()
                ? GetConnectionStringByName(settings.ConnectionStringName)
                : settings.ConnectionString;
#endif

            if (_connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "A connection string or connection string name must be specified when using a SQL error store");

            if (_schema.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "A table schema must be specified when using a SQL error store");
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
                return await c.ExecuteAsync($@"
Update {GetSchema()}.Exceptions 
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
                return await c.ExecuteAsync($@"
Update {GetSchema()}.Exceptions 
   Set IsProtected = 1, DeletionDate = Null
 Where GUID In @guids", new { guids }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Deletes an error, by setting DeletionDate = GETUTCDATE() in SQL.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync($@"
Update {GetSchema()}.Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where GUID = @guid 
   And DeletionDate Is Null", new { guid, ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Deletes errors, by setting DeletionDate = GETUTCDATE() in SQL.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to delete.</param>
        /// <returns><c>true</c> if the errors were found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorsAsync(IEnumerable<Guid> guids)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync($@"
Update {GetSchema()}.Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where GUID In @guids
   And DeletionDate Is Null", new { guids }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Hard deletes an error, actually deletes the row from SQL rather than setting DeletionDate
        /// This is used to cleanup when testing the error store when attempting to come out of retry/failover mode after losing connection to SQL
        /// </summary>
        /// <param name="guid">The GUID of the error to hard delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> HardDeleteErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync($@"
Delete From {GetSchema()}.Exceptions 
 Where GUID = @guid
   And ApplicationName = @ApplicationName", new { guid, ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        /// <summary>
        /// Deleted all errors in the log, by setting DeletionDate = GETUTCDATE() in SQL.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        /// <returns><c>true</c> if any errors were deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteAllErrorsAsync(string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync($@"
Update {GetSchema()}.Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @ApplicationName", new { ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        private const string _sqlLogUpdate = @"
Update {0}.Exceptions 
   Set DuplicateCount = DuplicateCount + @DuplicateCount,
       LastLogDate = (Case When LastLogDate Is Null Or @CreationDate > LastLogDate Then @CreationDate Else LastLogDate End),
       @newGUID = GUID
 Where Id In (Select Top 1 Id
                From {0}.Exceptions 
               Where ErrorHash = @ErrorHash
                 And ApplicationName = @ApplicationName
                 And DeletionDate Is Null
                 And CreationDate >= @minDate)";

        private const string _sqlLogInsert = @"
Insert Into {0}.Exceptions (GUID, ApplicationName, Category, MachineName, CreationDate, Type, IsProtected, Host, Url, HTTPMethod, IPAddress, Source, Message, Detail, StatusCode, FullJson, ErrorHash, DuplicateCount, LastLogDate)
Values (@GUID, @ApplicationName, @Category, @MachineName, @CreationDate, @Type, @IsProtected, @Host, @Url, @HTTPMethod, @IPAddress, @Source, @Message, @Detail, @StatusCode, @FullJson, @ErrorHash, @DuplicateCount, @LastLogDate)";

        private DynamicParameters GetUpdateParams(Error error)
        {
            var queryParams = new DynamicParameters(new
            {
                error.DuplicateCount,
                error.ErrorHash,
                error.CreationDate,
                ApplicationName = error.ApplicationName.Truncate(50),
                minDate = DateTime.UtcNow.Subtract(Settings.RollupPeriod.Value)
            });
            queryParams.Add("@newGUID", dbType: DbType.Guid, direction: ParameterDirection.Output);
            return queryParams;
        }

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
        /// If the roll-up conditions are met, then the matching error will have a 
        /// DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new row for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override void LogError(Error error)
        {
            using (var c = GetConnection())
            {
                if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
                {
                    var queryParams = GetUpdateParams(error);
                    var sqlLogUpdate = string.Format(_sqlLogUpdate, GetSchema());
                    // if we found an error that's a duplicate, jump out
                    if (c.Execute(sqlLogUpdate, queryParams) > 0)
                    {
                        error.GUID = queryParams.Get<Guid>("@newGUID");
                        return;
                    }
                }

                error.FullJson = error.ToJson();
                var sqlLogInsert = string.Format(_sqlLogInsert, GetSchema());
                c.Execute(sqlLogInsert, GetInsertParams(error));
            }
        }

        /// <summary>
        /// Asynchronously logs the error to SQL.
        /// If the roll-up conditions are met, then the matching error will have a 
        /// DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new row for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override async Task LogErrorAsync(Error error)
        {
            using (var c = GetConnection())
            {
                if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
                {
                    var queryParams = GetUpdateParams(error);
                    var sqlLogUpdate = string.Format(_sqlLogUpdate, GetSchema());
                    // if we found an error that's a duplicate, jump out
                    if (await c.ExecuteAsync(sqlLogUpdate, queryParams).ConfigureAwait(false) > 0)
                    {
                        error.GUID = queryParams.Get<Guid>("@newGUID");
                        return;
                    }
                }

                error.FullJson = error.ToJson();
                var sqlLogInsert = string.Format(_sqlLogInsert, GetSchema());
                await c.ExecuteAsync(sqlLogInsert, GetInsertParams(error)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the error with the specified GUID from SQL.
        /// This can return a deleted error as well, there's no filter based on DeletionDate.
        /// </summary>
        /// <param name="guid">The GUID of the error to retrieve.</param>
        /// <returns>The error object if found, null otherwise.</returns>
        protected override async Task<Error> GetErrorAsync(Guid guid)
        {
            Error sqlError;
            using (var c = GetConnection())
            {
                sqlError = await c.QueryFirstOrDefaultAsync<Error>($@"
Select * 
  From {GetSchema()}.Exceptions 
 Where GUID = @guid", new { guid }).ConfigureAwait(false);
            }
            if (sqlError == null) return null;

            // everything is in the JSON, but not the columns and we have to deserialize for collections anyway
            // so use that deserialized version and just get the properties that might change on the SQL side and apply them
            var result = Error.FromJson(sqlError.FullJson);
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
                return (await c.QueryAsync<Error>($@"
Select Top (@max) * 
  From {GetSchema()}.Exceptions 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName
Order By CreationDate Desc", new { max = _displayCount, ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false)).AsList();
            }
        }

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if null.
        /// </summary>
        /// <param name="since">The date to get errors since.</param>
        /// <param name="applicationName">The application name to get an error count for.</param>
        protected override async Task<int> GetErrorCountAsync(DateTime? since = null, string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return await c.QueryFirstOrDefaultAsync<int>($@"
Select Count(*) 
  From {GetSchema()}.Exceptions 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName" + (since.HasValue ? " And CreationDate > @since" : ""),
                    new { since, ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false);
            }
        }

        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        private string GetSchema() => _schema;
    }
}