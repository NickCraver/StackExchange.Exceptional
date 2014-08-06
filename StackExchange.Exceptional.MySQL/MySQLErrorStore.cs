using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using StackExchange.Exceptional.Dapper;
using StackExchange.Exceptional.Extensions;
using StackExchange.Exceptional.Stores;

namespace StackExchange.Exceptional.MySQL
{
    /// <summary>
    ///     An <see cref="ErrorStore" /> implementation that uses MySQL as its backing store.
    /// </summary>
    public sealed class MySQLErrorStore : ErrorStore
    {
        /// <summary>
        ///     The maximum count of errors to show.
        /// </summary>
        public const int MaximumDisplayCount = 500;

        /// <summary>
        ///     The default maximum count of errors shown at once.
        /// </summary>
        public const int DefaultDisplayCount = 200;

        private readonly string _connectionString;
        private readonly int _displayCount = DefaultDisplayCount;

        /// <summary>
        ///     Creates a new instance of <see cref="SQLErrorStore" /> with the given configuration.
        /// </summary>
        public MySQLErrorStore(ErrorStoreSettings settings)
            : base(settings)
        {
            _displayCount = Math.Min(settings.Size, MaximumDisplayCount);

            _connectionString = settings.ConnectionString.IsNullOrEmpty()
                ? GetConnectionStringByName(settings.ConnectionStringName)
                : settings.ConnectionString;

            if (_connectionString.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException("settings", "A connection string or connection string name must be specified when using a SQL error store");
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SQLErrorStore" /> with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The database connection string to use</param>
        /// <param name="displayCount">
        ///     How many errors to display in the log (for display ONLY, the log is not truncated to this
        ///     value)
        /// </param>
        /// <param name="rollupSeconds">
        ///     The rollup seconds, defaults to <see cref="ErrorStore.DefaultRollupSeconds" />, duplicate
        ///     errors within this time period will be rolled up
        /// </param>
        public MySQLErrorStore(string connectionString, int displayCount = DefaultDisplayCount, int rollupSeconds = DefaultRollupSeconds)
            : base(rollupSeconds)
        {
            _displayCount = Math.Min(displayCount, MaximumDisplayCount);

            if (connectionString.IsNullOrEmpty()) throw new ArgumentOutOfRangeException("connectionString", "Connection string must be specified when using a SQL error store");
            _connectionString = connectionString;
        }

        /// <summary>
        ///     Name for this error store
        /// </summary>
        public override string Name
        {
            get { return "SQL Error Store"; }
        }

        /// <summary>
        ///     Protects an error from deletion, by making IsProtected = 1 in the database
        /// </summary>
        /// <param name="guid">The guid of the error to protect</param>
        /// <returns>True if the error was found and protected, false otherwise</returns>
        protected override bool ProtectError(Guid guid)
        {
            using (MySqlConnection c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set IsProtected = 1, DeletionDate = Null
 Where GUID = @guid", new {guid}) > 0;
            }
        }

        /// <summary>
        ///     Protects errors from deletion, by making IsProtected = 1 in the database
        /// </summary>
        /// <param name="guids">The guids of the error to protect</param>
        /// <returns>True if the errors were found and protected, false otherwise</returns>
        protected override bool ProtectErrors(IEnumerable<Guid> guids)
        {
            using (MySqlConnection c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set IsProtected = 1, DeletionDate = Null
 Where GUID In @guids", new {guids}) > 0;
            }
        }

        /// <summary>
        ///     Deletes an error, by setting DeletionDate = UTC_DATE() in SQL
        /// </summary>
        /// <param name="guid">The guid of the error to delete</param>
        /// <returns>True if the error was found and deleted, false otherwise</returns>
        protected override bool DeleteError(Guid guid)
        {
            using (MySqlConnection c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where GUID = @guid 
   And DeletionDate Is Null", new {guid, ApplicationName}) > 0;
            }
        }

        /// <summary>
        ///     Deletes errors, by setting DeletionDate = UTC_DATE() in SQL
        /// </summary>
        /// <param name="guids">The guids of the error to delete</param>
        /// <returns>True if the errors were found and deleted, false otherwise</returns>
        protected override bool DeleteErrors(IEnumerable<Guid> guids)
        {
            using (MySqlConnection c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where GUID In @guids
   And DeletionDate Is Null", new {guids}) > 0;
            }
        }

        /// <summary>
        ///     Hard deletes an error, actually deletes the row from SQL rather than setting DeletionDate
        ///     This is used to cleanup when testing the error store when attempting to come out of retry/failover mode after
        ///     losing connection to SQL
        /// </summary>
        /// <param name="guid">The guid of the error to hard delete</param>
        /// <returns>True if the error was found and deleted, false otherwise</returns>
        protected override bool HardDeleteError(Guid guid)
        {
            using (MySqlConnection c = GetConnection())
            {
                return c.Execute(@"
Delete From Exceptions 
 Where GUID = @guid
   And ApplicationName = @ApplicationName", new {guid, ApplicationName}) > 0;
            }
        }

        /// <summary>
        ///     Deleted all errors in the log, by setting DeletionDate = UTC_DATE() in SQL
        /// </summary>
        /// <returns>True if any errors were deleted, false otherwise</returns>
        protected override bool DeleteAllErrors(string applicationName = null)
        {
            using (MySqlConnection c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @ApplicationName", new {ApplicationName = applicationName.IsNullOrEmptyReturn(ApplicationName)}) > 0;
            }
        }

        /// <summary>
        ///     Logs the error to SQL
        ///     If the rollup conditions are met, then the matching error will have a DuplicateCount += @DuplicateCount (usually 1,
        ///     unless in retry) rather than a distinct new row for the error
        /// </summary>
        /// <param name="error">The error to log</param>
        protected override void LogError(Error error)
        {
            using (var c = GetConnection())
            {
                if (RollupThreshold.HasValue && error.ErrorHash.HasValue)
                {
                    // Look for an existing exception 
                    IEnumerable<Error> existingException = c.Query<Error>(@"Select * from Exceptions Where ErrorHash = @ErrorHash
                                                     And ApplicationName = @ApplicationName
                                                     And DeletionDate Is Null
                                                     And CreationDate >= @minDate limit 1 ",
                        new
                        {
                            error.ErrorHash,
                            ApplicationName = error.ApplicationName.Truncate(50),
                            minDate = DateTime.UtcNow.Add(RollupThreshold.Value.Negate())
                        });


                    if (existingException.Any())
                    {
                        // Update the count and move on
                        error.GUID = existingException.First().GUID;
                        c.Execute("Update Exceptions set DuplicateCount = DuplicateCount + @DuplicateCount where ID = @id", new {error.DuplicateCount, id = existingException.First().Id});
                    }
                    else
                    {
                        error.FullJson = error.ToJson();

                        c.Execute(@"
Insert Into Exceptions (GUID, ApplicationName, MachineName, CreationDate, Type, IsProtected, Host, Url, HTTPMethod, IPAddress, Source, Message, Detail, StatusCode, `SQL`, FullJson, ErrorHash, DuplicateCount)
Values (@GUID, @ApplicationName, @MachineName, @CreationDate, @Type, @IsProtected, @Host, @Url, @HTTPMethod, @IPAddress, @Source, @Message, @Detail, @StatusCode, @SQL, @FullJson, @ErrorHash, @DuplicateCount)",
                            new
                            {
                                error.GUID,
                                ApplicationName = error.ApplicationName.Truncate(50),
                                MachineName = error.MachineName.Truncate(50),
                                error.CreationDate,
                                Type = error.Type.Truncate(100),
                                error.IsProtected,
                                Host = error.Host.Truncate(100),
                                Url = error.Url.Truncate(500),
                                HTTPMethod = error.HTTPMethod.Truncate(10), // this feels silly, but you never know when someone will up and go crazy with HTTP 1.2!
                                error.IPAddress,
                                Source = error.Source.Truncate(100),
                                Message = error.Message.Truncate(1000),
                                error.Detail,
                                error.StatusCode,
                                error.SQL,
                                error.FullJson,
                                error.ErrorHash,
                                error.DuplicateCount
                            });
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the error with the specified guid from SQL
        ///     This can return a deleted error as well, there's no filter based on DeletionDate
        /// </summary>
        /// <param name="guid">The guid of the error to retrieve</param>
        /// <returns>The error object if found, null otherwise</returns>
        protected override
            Error GetError(Guid guid)
        {
            Error sqlError;
            using (MySqlConnection c = GetConnection())
            {
                sqlError = c.Query<Error>(@"
Select * 
  From Exceptions 
 Where GUID = @guid", new {guid}).FirstOrDefault(); // a guid won't collide, but the AppName is for security
            }
            if (sqlError == null) return null;

            // everything is in the JSON, but not the columns and we have to deserialize for collections anyway
            // so use that deserialized version and just get the properties that might change on the SQL side and apply them
            Error result = Error.FromJson(sqlError.FullJson);
            result.DuplicateCount = sqlError.DuplicateCount;
            result.DeletionDate = sqlError.DeletionDate;
            return result;
        }

        /// <summary>
        ///     Retrieves all non-deleted application errors in the database
        /// </summary>
        protected override int GetAllErrors(List<Error> errors, string applicationName = null)
        {
            using (MySqlConnection c = GetConnection())
            {
                errors.AddRange(c.Query<Error>(@"
Select * 
  From Exceptions 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName
Order By CreationDate Desc limit @max", new {max = _displayCount, ApplicationName = applicationName.IsNullOrEmptyReturn(ApplicationName)}));
            }

            return errors.Count;
        }

        /// <summary>
        ///     Retrieves a count of application errors since the specified date, or all time if null
        /// </summary>
        protected override int GetErrorCount(DateTime? since = null, string applicationName = null)
        {
            using (MySqlConnection c = GetConnection())
            {
                return c.Query<int>(@"
Select Count(*) 
  From Exceptions 
 Where DeletionDate Is Null
   And ApplicationName = @ApplicationName" + (since.HasValue ? " And CreationDate > @since" : ""),
                    new {since, ApplicationName = applicationName.IsNullOrEmptyReturn(ApplicationName)}).FirstOrDefault();
            }
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}