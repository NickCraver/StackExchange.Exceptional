using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using StackExchange.Exceptional.Internal;

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

        private readonly string _tableName;
        private readonly int _displayCount;
        private readonly string _connectionString;

        /// <summary>
        /// The maximum count of errors to show.
        /// </summary>
        public const int MaximumDisplayCount = 500;

        /// <summary>
        /// Creates a new instance of <see cref="SQLErrorStore"/> with the specified connection string.
        /// The default table name is "Exceptions".
        /// </summary>
        /// <param name="connectionString">The database connection string to use.</param>
        /// <param name="applicationName">The application name to use when logging.</param>
        public SQLErrorStore(string connectionString, string applicationName)
            : this(new ErrorStoreSettings()
            {
                ApplicationName = applicationName,
                ConnectionString = connectionString
            })
        { }

        /// <summary>
        /// Creates a new instance of <see cref="SQLErrorStore"/> with the given configuration.
        /// The default table name is "Exceptions".
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>
        public SQLErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            _displayCount = Math.Min(settings.Size, MaximumDisplayCount);
            _connectionString = settings.ConnectionString;
            _tableName = settings.TableName ?? "Exceptions";

            if (_connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "A connection string or connection string name must be specified when using a SQL error store");
        }

        private string _sqlProtectError;
        private string SqlProtectError => _sqlProtectError ?? (_sqlProtectError = $@"
Update {_tableName}
   Set IsProtected = 1, DeletionDate = Null
 Where GUID = @guid");

        /// <summary>
        /// Protects an error from deletion, by making IsProtected = 1 in the database.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect.</param>
        /// <returns><c>true</c> if the error was found and protected, <c>false</c> otherwise.</returns>
        protected override async Task<bool> ProtectErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlProtectError, new { guid }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlProtectErrors;
        private string SqlProtectErrors => _sqlProtectErrors ?? (_sqlProtectErrors = $@"
Update {_tableName}
   Set IsProtected = 1, DeletionDate = Null
 Where GUID In @guids");

        /// <summary>
        /// Protects errors from deletion, by making IsProtected = 1 in the database.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to protect.</param>
        /// <returns><c>true</c> if the errors were found and protected, <c>false</c> otherwise.</returns>
        protected override async Task<bool> ProtectErrorsAsync(IEnumerable<Guid> guids)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlProtectErrors, new { guids }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlDeleteError;
        private string SqlDeleteError => _sqlDeleteError ?? (_sqlDeleteError = $@"
Update {_tableName} 
   Set DeletionDate = GETUTCDATE() 
 Where GUID = @guid 
   And DeletionDate Is Null");

        /// <summary>
        /// Deletes an error, by setting DeletionDate = GETUTCDATE() in SQL.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlDeleteError, new { guid, ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlDeleteErrors;
        private string SqlDeleteErrors => _sqlDeleteErrors ?? (_sqlDeleteErrors = $@"
Update {_tableName} 
   Set DeletionDate = GETUTCDATE() 
 Where GUID In @guids
   And DeletionDate Is Null");

        /// <summary>
        /// Deletes errors, by setting DeletionDate = GETUTCDATE() in SQL.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to delete.</param>
        /// <returns><c>true</c> if the errors were found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorsAsync(IEnumerable<Guid> guids)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlDeleteErrors, new { guids }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlHardDeleteErrors;
        private string SqlHardDeleteErrors => _sqlHardDeleteErrors ?? (_sqlHardDeleteErrors = $@"
Delete From {_tableName} 
 Where GUID = @guid
   And ApplicationName = @ApplicationName");

        /// <summary>
        /// Hard deletes an error, actually deletes the row from SQL rather than setting <see cref="Error.DeletionDate"/>.
        /// This is used to cleanup when testing the error store when attempting to come out of retry/failover mode after losing connection to SQL.
        /// </summary>
        /// <param name="guid">The GUID of the error to hard delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> HardDeleteErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlHardDeleteErrors, new { guid, ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlDeleteAllErrors;
        private string SqlDeleteAllErrors => _sqlDeleteAllErrors ?? (_sqlDeleteAllErrors = $@"
Update {_tableName}
   Set DeletionDate = GETUTCDATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @ApplicationName");

        /// <summary>
        /// Deleted all errors in the log, by setting <see cref="Error.DeletionDate"/> = GETUTCDATE() in SQL.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        /// <returns><c>true</c> if any errors were deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteAllErrorsAsync(string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlDeleteAllErrors, new { ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlLogUpdate;
        private string SqlLogUpdate => _sqlLogUpdate ?? (_sqlLogUpdate = $@"
Update {_tableName} 
   Set DuplicateCount = DuplicateCount + @DuplicateCount,
       LastLogDate = (Case When LastLogDate Is Null Or @CreationDate > LastLogDate Then @CreationDate Else LastLogDate End),
       @newGUID = GUID
 Where Id In (Select Top 1 Id
                From {_tableName} 
               Where ErrorHash = @ErrorHash
                 And ApplicationName = @ApplicationName
                 And DeletionDate Is Null
                 And CreationDate >= @minDate)");

        private string _sqlLogInsert;
        private string SqlLogInsert => _sqlLogInsert ?? (_sqlLogInsert = $@"
Insert Into {_tableName} (GUID, ApplicationName, Category, MachineName, CreationDate, Type, IsProtected, Host, Url, HTTPMethod, IPAddress, Source, Message, Detail, StatusCode, FullJson, ErrorHash, DuplicateCount, LastLogDate)
Values (@GUID, @ApplicationName, @Category, @MachineName, @CreationDate, @Type, @IsProtected, @Host, @Url, @HTTPMethod, @IPAddress, @Source, @Message, @Detail, @StatusCode, @FullJson, @ErrorHash, @DuplicateCount, @LastLogDate)");

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
            Url = error.UrlPath.Truncate(500),
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
        protected override bool LogError(Error error)
        {
            using (var c = GetConnection())
            {
                if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
                {
                    var queryParams = GetUpdateParams(error);
                    // if we found an error that's a duplicate, jump out
                    if (c.Execute(SqlLogUpdate, queryParams) > 0)
                    {
                        error.GUID = queryParams.Get<Guid>("@newGUID");
                        return true;
                    }
                }

                error.FullJson = error.ToJson();
                return c.Execute(SqlLogInsert, GetInsertParams(error)) > 0;
            }
        }

        /// <summary>
        /// Asynchronously logs the error to SQL.
        /// If the roll-up conditions are met, then the matching error will have a 
        /// DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new row for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override async Task<bool> LogErrorAsync(Error error)
        {
            using (var c = GetConnection())
            {
                if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
                {
                    var queryParams = GetUpdateParams(error);
                    // if we found an error that's a duplicate, jump out
                    if (await c.ExecuteAsync(SqlLogUpdate, queryParams).ConfigureAwait(false) > 0)
                    {
                        error.GUID = queryParams.Get<Guid>("@newGUID");
                        return true;
                    }
                }

                error.FullJson = error.ToJson();
                return (await c.ExecuteAsync(SqlLogInsert, GetInsertParams(error)).ConfigureAwait(false)) > 0;
            }
        }

        private string _sqlGetError;
        private string SqlGetError => _sqlGetError ?? (_sqlGetError = $@"
Select * 
  From {_tableName} 
 Where GUID = @guid");

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
                sqlError = await c.QueryFirstOrDefaultAsync<Error>(SqlGetError, new { guid }).ConfigureAwait(false);
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

        private string _sqlGetAllErrors;
        private string SqlGetAllErrors => _sqlGetAllErrors ?? (_sqlGetAllErrors = $@"
Select Top {{=max}} * 
  From {_tableName} 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName
Order By CreationDate Desc");

        /// <summary>
        /// Retrieves all non-deleted application errors in the database.
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        protected override async Task<List<Error>> GetAllErrorsAsync(string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return (await c.QueryAsync<Error>(SqlGetAllErrors, new { max = _displayCount, ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false)).AsList();
            }
        }

        private string _sqlGetErrorCount;
        private string SqlGetErrorCount => _sqlGetErrorCount ?? (_sqlGetErrorCount = $@"
Select Count(*) 
  From {_tableName} 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName");

        private string _sqlGetErrorCountWithSince;
        private string SqlGetErrorCountWithSince => _sqlGetErrorCountWithSince ?? (_sqlGetErrorCountWithSince = $@"
Select Count(*) 
  From {_tableName} 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName
   And CreationDate > @since");

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if <c>null</c>.
        /// </summary>
        /// <param name="since">The date to get errors since.</param>
        /// <param name="applicationName">The application name to get an error count for.</param>
        protected override async Task<int> GetErrorCountAsync(DateTime? since = null, string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return await c.QueryFirstOrDefaultAsync<int>(
                    since.HasValue ? SqlGetErrorCountWithSince : SqlGetErrorCount,
                    new { since, ApplicationName = applicationName ?? ApplicationName }
                ).ConfigureAwait(false);
            }
        }

        private SqlConnection GetConnection() => new SqlConnection(_connectionString);
    }
}
