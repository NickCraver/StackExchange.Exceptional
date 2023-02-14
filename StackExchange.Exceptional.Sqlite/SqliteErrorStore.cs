using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Sqlite;

namespace StackExchange.Exceptional.Stores
{

    /// <summary>
    /// An <see cref="ErrorStore"/> implementation that uses Sqlite as its backing store. 
    /// </summary>
    public sealed class SqliteErrorStore : ErrorStore
    {
        /// <summary>
        /// Name for this error store.
        /// </summary>
        public override string Name => "Sqlite Error Store";

        private readonly string _tableName;
        private readonly int _displayCount;
        private readonly string _connectionString;

        /// <summary>
        /// The maximum count of errors to show.
        /// </summary>
        public const int MaximumDisplayCount = 500;

        /// <summary>
        /// Creates a new instance of <see cref="SqliteErrorStore"/> with the specified connection string.
        /// The default table name is "Exceptions".
        /// </summary>
        /// <param name="connectionString">The database connection string to use.</param>
        /// <param name="applicationName">The application name to use when logging.</param>
        public SqliteErrorStore(string connectionString, string applicationName)
            : this(new ErrorStoreSettings() {ApplicationName = applicationName, ConnectionString = connectionString})
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="SqliteErrorStore"/> with the given configuration.
        /// The default table name is "Exceptions".
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>
        public SqliteErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            _displayCount = Math.Min(settings.Size, MaximumDisplayCount);
            _connectionString = settings.ConnectionString;
            _tableName = settings.TableName ?? "Exceptions";

            if (_connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "A connection string or connection string name must be specified when using a SQL error store");

            CreateSchema(_connectionString);

        }

        private string _sqlProtectError;
        private string SqlProtectError => _sqlProtectError ??= $@"
Update {_tableName}
   Set IsProtected = 1, DeletionDate = Null
 Where GUID = @guid";

        /// <summary>
        /// Protects an error from deletion, by making IsProtected = 1 in the database.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect.</param>
        /// <returns><c>true</c> if the error was found and protected, <c>false</c> otherwise.</returns>
        protected override async Task<bool> ProtectErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlProtectError, new { guid = guid.ToString() }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlProtectErrors;
        private string SqlProtectErrors => _sqlProtectErrors ??= $@"
Update {_tableName}
   Set IsProtected = 1, DeletionDate = Null
 Where GUID In @guids";

        /// <summary>
        /// Protects errors from deletion, by making IsProtected = 1 in the database.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to protect.</param>
        /// <returns><c>true</c> if the errors were found and protected, <c>false</c> otherwise.</returns>
        protected override async Task<bool> ProtectErrorsAsync(IEnumerable<Guid> guids)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlProtectErrors, new { guids = guids.Select(s => s.ToString()) }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlDeleteError;
        private string SqlDeleteError => _sqlDeleteError ??= $@"
Update {_tableName} 
   Set DeletionDate = datetime('now') 
 Where GUID = @guid 
   And DeletionDate Is Null";

        /// <summary>
        /// Deletes an error, by setting DeletionDate = datetime('now') in SQL.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteErrorAsync(Guid guid)
        {
            using (var c = GetConnection())
            {
                return await c.ExecuteAsync(SqlDeleteError, new { guid = guid.ToString(), ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlDeleteErrors;
        private string SqlDeleteErrors => _sqlDeleteErrors ??= $@"
Update {_tableName} 
   Set DeletionDate = datetime('now') 
 Where GUID In @guids
   And DeletionDate Is Null";

        /// <summary>
        /// Deletes errors, by setting DeletionDate = datetime('now') in SQL.
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
        private string SqlHardDeleteErrors => _sqlHardDeleteErrors ??= $@"
Delete From {_tableName} 
 Where GUID = @guid
   And ApplicationName = @ApplicationName";

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
                return await c.ExecuteAsync(SqlHardDeleteErrors, new { guid = guid.ToString(), ApplicationName }).ConfigureAwait(false) > 0;
            }
        }

        private string _sqlDeleteAllErrors;
        private string SqlDeleteAllErrors => _sqlDeleteAllErrors ??= $@"
Update {_tableName}
   Set DeletionDate = datetime('now') 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @ApplicationName";

        /// <summary>
        /// Deleted all errors in the log, by setting <see cref="Error.DeletionDate"/> = datetime('now') in SQL.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        /// <returns><c>true</c> if any errors were deleted, <c>false</c> otherwise.</returns>
        protected override async Task<bool> DeleteAllErrorsAsync(string applicationName = null)
        {
            using var c = GetConnection();
            return await c.ExecuteAsync(SqlDeleteAllErrors, new { ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false) > 0;
        }

        private string _sqlLogUpdate;
        private string SqlLogUpdate => _sqlLogUpdate ??= 
            $@"Update {_tableName} 
               Set DuplicateCount = DuplicateCount + @DuplicateCount,
               LastLogDate = (Case When LastLogDate Is Null Or @CreationDate > LastLogDate Then @CreationDate Else LastLogDate End)
               Where Id = (Select Id
               From {_tableName} 
               Where ErrorHash = @ErrorHash
                 And ApplicationName = @ApplicationName
                 And DeletionDate Is Null
                 And CreationDate >= @minDate
                 LIMIT 1)";

        private string _sqlLogGetGuid;

        private string SqlLogGetGuid => _sqlLogGetGuid ??=
            @"Select Cast(Id as text) Id
            From {_tableName} 
               Where ErrorHash = @ErrorHash
                 And ApplicationName = @ApplicationName
                 And DeletionDate Is Null
                 And CreationDate >= @minDate
                 LIMIT 1";

        private string _sqlLogInsert;
        private string SqlLogInsert => _sqlLogInsert ??= $@"
Insert Into {_tableName} (GUID, ApplicationName, Category, MachineName, CreationDate, Type, IsProtected, Host, Url, HTTPMethod, IPAddress, Source, Message, Detail, StatusCode, FullJson, ErrorHash, DuplicateCount, LastLogDate)
Values (@GUID, @ApplicationName, @Category, @MachineName, @CreationDate, @Type, @IsProtected, @Host, @Url, @HTTPMethod, @IPAddress, @Source, @Message, @Detail, @StatusCode, @FullJson, @ErrorHash, @DuplicateCount, @LastLogDate)";

        private object GetUpdateParams(Error error) => new
        {
            DuplicateCount = error.DuplicateCount,
            ErrorHash = error.ErrorHash,
            CreationDate = error.CreationDate,
            ApplicationName = error.ApplicationName.Truncate(50),
            minDate = DateTime.UtcNow.Subtract(Settings.RollupPeriod.Value)
        };
        

        private object GetInsertParams(Error error) => new
        {
            GUID =error.GUID.ToString(),
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
            using var c = GetConnection();
            if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
            {
                var queryParams = GetUpdateParams(error);
                // if we found an error that's a duplicate, jump out
                if (c.Execute(SqlLogUpdate, queryParams) > 0)
                {
                    
                    var guidStr = c.QueryFirstOrDefault<string>(SqlLogGetGuid, queryParams);

                    if (Guid.TryParse(guidStr, out var guid))
                    {
                        error.GUID = guid;
                        return true;
                    }
                }
            }

            error.FullJson = error.ToJson();
            return c.Execute(SqlLogInsert, GetInsertParams(error)) > 0;
        }

        /// <summary>
        /// Asynchronously logs the error to SQL.
        /// If the roll-up conditions are met, then the matching error will have a 
        /// DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new row for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override async Task<bool> LogErrorAsync(Error error)
        {
            using var c = GetConnection();
            if (Settings.RollupPeriod.HasValue && error.ErrorHash.HasValue)
            {
                var queryParams = GetUpdateParams(error);
                // if we found an error that's a duplicate, jump out
                if (await c.ExecuteAsync(SqlLogUpdate, queryParams).ConfigureAwait(false) > 0)
                {

                    var guidStr = await c.QueryFirstOrDefaultAsync<string>(SqlLogGetGuid, queryParams).ConfigureAwait(false);

                    if (Guid.TryParse(guidStr, out var guid))
                    {
                        error.GUID = guid;
                        return true;
                    }
                }

               
            }

            error.FullJson = error.ToJson();
            return (await c.ExecuteAsync(SqlLogInsert, GetInsertParams(error)).ConfigureAwait(false)) > 0;
        }

        private string _sqlGetError;
        private string SqlGetError => _sqlGetError ??= $@"
Select * 
  From {_tableName} 
 Where GUID = @guid";

        /// <summary>
        /// Gets the error with the specified GUID from SQL.
        /// This can return a deleted error as well, there's no filter based on DeletionDate.
        /// </summary>
        /// <param name="guid">The GUID of the error to retrieve.</param>
        /// <returns>The error object if found, null otherwise.</returns>
        protected override async Task<Error> GetErrorAsync(Guid guid)
        {
            ErrorMapSqlite error;
            using (var c = GetConnection())
            {
                error = await c.QueryFirstOrDefaultAsync<ErrorMapSqlite>(SqlGetError, new { guid = guid.ToString() }).ConfigureAwait(false);

            }
            if (error == null) return null;

            var sqlError = new Error
            {
                ApplicationName = error.ApplicationName,
                Category = error.Category,
                CreationDate = error.CreationDate,
                Detail = error.Detail,
                DeletionDate = error.DeletionDate,
                DuplicateCount = error.DuplicateCount,
                ErrorHash = error.ErrorHash,
                FullJson = error.FullJson,
                GUID = Guid.Parse(error.GUID),
                FullUrl = error.FullUrl,
                HTTPMethod = error.HTTPMethod,
                Host = error.Host,
                Id = error.Id,
                IsDuplicate = error.IsDuplicate,
                IPAddress = error.IPAddress,
                IsProtected = error.IsProtected,
                Source = error.Source,
                LastLogDate = error.LastLogDate,
                MachineName = error.MachineName,
                Message = error.Message,
                StatusCode = error.StatusCode,
                Type = error.Type,
                UrlPath = error.UrlPath

            };
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
        private string SqlGetAllErrors => _sqlGetAllErrors ??= $@"
                        Select  * 
                          From {_tableName} 
                         Where DeletionDate Is Null
                           And ApplicationName = @ApplicationName
                        Order By CreationDate Desc
                        LIMIT @max";

        /// <summary>
        /// Retrieves all non-deleted application errors in the database.
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        protected override async Task<List<Error>> GetAllErrorsAsync(string applicationName = null)
        {
            using (var c = GetConnection())
            {
                return (await c.QueryAsync<ErrorMapSqlite>(SqlGetAllErrors, new { max = _displayCount, ApplicationName = applicationName ?? ApplicationName }).ConfigureAwait(false)).Select(s => new Error
                {
                    ApplicationName = s.ApplicationName,
                    Category = s.Category,
                    CreationDate = s.CreationDate,
                    Detail = s.Detail,
                    DeletionDate = s.DeletionDate,
                    DuplicateCount = s.DuplicateCount,
                    ErrorHash = s.ErrorHash,
                    FullJson = s.FullJson,
                    GUID = Guid.Parse(s.GUID),
                    FullUrl = s.FullUrl,
                    HTTPMethod = s.HTTPMethod,
                    Host = s.Host,
                    Id = s.Id,
                    IsDuplicate = s.IsDuplicate,
                    IPAddress = s.IPAddress,
                    IsProtected = s.IsProtected,
                    Source = s.Source,
                    LastLogDate = s.LastLogDate,
                    MachineName = s.MachineName,
                    Message = s.Message,
                    StatusCode = s.StatusCode,
                    Type = s.Type,
                    UrlPath = s.UrlPath

                }).AsList();
            }
        }

        private string _sqlGetErrorCount;
        private string SqlGetErrorCount => _sqlGetErrorCount ??= $@"
Select Count(*) 
  From {_tableName} 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName";

        private string _sqlGetErrorCountWithSince;
        private string SqlGetErrorCountWithSince => _sqlGetErrorCountWithSince ??= $@"
Select Count(*) 
  From {_tableName} 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName
   And CreationDate > @since";

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

        private DbConnection GetConnection() => new SqliteConnection(_connectionString);

        private List<string> _tableCreationScripts;

        /// <summary>
        /// The table creation scripts for this database storage.
        /// Generated by the <see cref="GetTableCreationScripts"/> implemented by the provider.
        /// </summary>
        public List<string> TableCreationScripts => _tableCreationScripts ??= GetTableCreationScripts().ToList();

        /// <summary>
        /// Creates the database schema from scratch, for initial spinup.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        public void CreateSchema(string connectionString)
        {
            var cnn = GetConnection();

            // We need some tiny mods to allow SQLite support 
            foreach (var sql in TableCreationScripts)
            {
                cnn.Execute(sql);
            }
        }

        /// <summary>
        /// SQL statements to create the SQLite tables.
        /// </summary>
        private IEnumerable<string> GetTableCreationScripts()
        {
            yield return $@"CREATE TABLE IF NOT EXISTS {_tableName}
                         (
                            Id  					integer not null primary key,
                            GUID 					nvarchar(36) not null,
                            ApplicationName			nvarchar(50) not null,
                            MachineName				nvarchar(50) not null,
                            CreationDate 			datetime not null,
                            Type 					nvarchar(100) not null,
                            IsProtected 			boolean not null default 0,
                            Host 					nvarchar(100) null,
                            Url 					nvarchar(500) null,
                            HTTPMethod 				nvarchar(10) null,
                            IPAddress 				varchar(40) null,
                            Source 					nvarchar(100) null,
                            Message 				nvarchar(1000) null,
                            Detail 					nvarchar null,	
                            StatusCode 				int null,
                            DeletionDate 			datetime null,
                            FullJson 				nvarchar null,
                            ErrorHash 				int null,
                            DuplicateCount 			int not null default 1,
                            LastLogDate datetime 	null,
                            Category nvarchar(100) 	null    
	                    );";

            yield return $@"CREATE UNIQUE INDEX IF NOT EXISTS IX_{_tableName}_ErrorHash_ApplicationName_CreationDate_DeletionDate ON {_tableName} 
                        (
		                    ErrorHash	ASC,
		                    ApplicationName	ASC,
		                    CreationDate	DESC,
		                    DeletionDate	ASC
	                    );";

            yield return $@"CREATE UNIQUE INDEX IF NOT EXISTS IX_{_tableName}_GUID_ApplicationName_DeletionDate_CreationDate ON {_tableName} 
                        (
		                    GUID	ASC,
		                    ApplicationName	ASC,
		                    DeletionDate	ASC,
		                    CreationDate	DESC
	                    );";

            yield return $@"CREATE UNIQUE INDEX IF NOT EXISTS IX_{_tableName}_ApplicationName_DeletionDate_CreationDate_Filtered ON {_tableName} 
                         (
                            ApplicationName	ASC,
                            CreationDate	DESC,
                            DeletionDate	ASC
                         )
                         Where DeletionDate is null;";

        }
    }

}
