using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using StackExchange.Exceptional.Internal;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// An <see cref="ErrorStore" /> implementation that uses PostgreSQL as its backing store.
    /// </summary>
    public sealed class PostgreSqlErrorStore : ErrorStore
    {
        /// <summary>
        /// Name for this error store.
        /// </summary>
        public override string Name => "PostgreSQL Error Store";

        private readonly string _tableName;
        private readonly int _displayCount;
        private readonly string _connectionString;

        /// <summary>
        /// The maximum count of errors to show.
        /// </summary>
        public const int MaximumDisplayCount = 500;

        /// <summary>
        /// Creates a new instance of <see cref="PostgreSqlErrorStore" /> with the specified connection string.
        /// The default table name is public."Errors".
        /// </summary>
        /// <param name="connectionString">The database connection string to use.</param>
        /// <param name="applicationName">The application name to use when logging.</param>
        public PostgreSqlErrorStore(string connectionString, string applicationName)
            : this(new ErrorStoreSettings()
            {
                ApplicationName = applicationName,
                ConnectionString = connectionString
            })
        { }

        /// <summary>
        /// Creates a new instance of <see cref="PostgreSqlErrorStore"/> with the given configuration.
        /// The default table name is public."Errors".
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>
        public PostgreSqlErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            _displayCount = Math.Min(settings.Size, MaximumDisplayCount);
            _connectionString = settings.ConnectionString;
            _tableName = settings.TableName ?? @"public.""Errors""";

            if (_connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "A connection string or connection string name must be specified when using a PostgreSQL error store");
        }

        private string _sqlProtectError;
        private string SqlProtectError => _sqlProtectError ?? (_sqlProtectError = $@"
UPDATE {_tableName}
   SET ""IsProtected"" = true, ""DeletionDate"" = NULL
 WHERE ""GUID"" = :guid");

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
UPDATE {_tableName}
   SET ""IsProtected"" = true, 
       ""DeletionDate"" = NULL
 WHERE ""GUID"" = ANY(:guids)");

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
UPDATE {_tableName} 
   SET ""DeletionDate"" = now() at time zone 'utc'
 WHERE ""GUID"" = :guid 
   AND ""DeletionDate"" IS NULL");

        /// <summary>
        /// Deletes an error, by setting DeletionDate = now() at time zone 'utc' in SQL.
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
UPDATE {_tableName}
   SET ""DeletionDate"" = now() at time zone 'utc' 
 WHERE ""GUID"" = ANY(:guids)
   AND ""DeletionDate"" IS NULL");

        /// <summary>
        /// Deletes errors, by setting DeletionDate = now() at time zone 'utc' in SQL.
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
DELETE FROM {_tableName} 
 WHERE ""GUID"" = :guid
   AND ""ApplicationName"" = :ApplicationName");

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
UPDATE {_tableName}
   SET ""DeletionDate"" = now() at time zone 'utc'
 WHERE ""DeletionDate"" IS NULL 
   AND ""IsProtected"" = false
   AND ""ApplicationName"" = :ApplicationName");

        /// <summary>
        /// Deleted all errors in the log, by setting <see cref="Error.DeletionDate"/> = now() at time zone 'utc' in SQL.
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
UPDATE {_tableName} 
   SET ""DuplicateCount"" = ""DuplicateCount"" + :DuplicateCount,
       ""LastLogDate"" = (CASE WHEN ""LastLogDate"" IS NULL OR @CreationDate > ""LastLogDate"" THEN @CreationDate ELSE ""LastLogDate"" END)
 WHERE ""Id"" IN (SELECT ""Id""
                    FROM {_tableName} 
                   WHERE ""ErrorHash"" = :ErrorHash
                     AND ""ApplicationName"" = :ApplicationName
                     AND ""DeletionDate"" IS NULL
                     AND ""CreationDate"" >= :MinDate 
                   LIMIT 1)
RETURNING ""GUID"";");

        private string _sqlLogInsert;
        private string SqlLogInsert => _sqlLogInsert ?? (_sqlLogInsert = $@"
INSERT INTO {_tableName} (""GUID"", ""ApplicationName"", ""Category"", ""MachineName"", ""CreationDate"", ""Type"", ""IsProtected"", ""Host"", ""Url"", ""HTTPMethod"", ""IPAddress"", ""Source"", ""Message"", ""Detail"", ""StatusCode"", ""FullJson"", ""ErrorHash"", ""DuplicateCount"", ""LastLogDate"")
VALUES (:GUID, :ApplicationName, :Category, :MachineName, :CreationDate, :Type, :IsProtected, :Host, :Url, :HTTPMethod, :IPAddress, :Source, :Message, :Detail, :StatusCode, :FullJson, :ErrorHash, :DuplicateCount, :LastLogDate)");

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
                    var guid = c.QueryFirstOrDefault<Guid>(SqlLogUpdate, queryParams);
                    // if we found an exception that's a duplicate, jump out
                    if (guid != default(Guid))
                    {
                        error.GUID = guid;
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
                    var guid = await c.QueryFirstOrDefaultAsync<Guid>(SqlLogUpdate, queryParams).ConfigureAwait(false);
                    // if we found an exception that's a duplicate, jump out
                    if (guid != default(Guid))
                    {
                        error.GUID = guid;
                        return true;
                    }
                }

                error.FullJson = error.ToJson();

                return (await c.ExecuteAsync(SqlLogInsert, GetInsertParams(error)).ConfigureAwait(false)) > 0;
            }
        }

        private string _sqlGetError;
        private string SqlGetError => _sqlGetError ?? (_sqlGetError = $@"
SELECT * 
  FROM {_tableName} 
 WHERE ""GUID"" = :guid");

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
SELECT * 
  FROM {_tableName} 
 WHERE ""DeletionDate"" IS NULL
   AND ""ApplicationName"" = :ApplicationName
 ORDER BY ""CreationDate"" DESC 
 LIMIT :max");

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
SELECT COUNT(*) 
  FROM {_tableName} 
 WHERE ""DeletionDate"" IS NULL
   AND ""ApplicationName"" = :ApplicationName");

        private string _sqlGetErrorCountWithSince;
        private string SqlGetErrorCountWithSince => _sqlGetErrorCountWithSince ?? (_sqlGetErrorCountWithSince = $@"
SELECT COUNT(*) 
  FROM {_tableName} 
 WHERE ""DeletionDate"" IS NULL
   AND ""ApplicationName"" = :ApplicationName
   AND ""CreationDate"" > :since");

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

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);
    }
}
