using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// Needed because long strings can not be passed.
    /// </summary>
    /// <remarks>
    /// https://github.com/StackExchange/Dapper/issues/142
    /// </remarks>
    internal class OracleClobParameter : SqlMapper.ICustomQueryParameter
    {
        private readonly string value;

        public OracleClobParameter(string value)
        {
            this.value = value;
        }

        public void AddParameter(IDbCommand command, string name)
        {
            // accesing the connection in open state.            
            var connection = command.Connection as OracleConnection;

            connection.StateChange += (object sender, StateChangeEventArgs e) =>
            {
                if (e.CurrentState != ConnectionState.Open)
                    return;

                var clob = new OracleClob(connection);

                // It should be Unicode oracle throws an exception when
                // the length is not even.
                var bytes = System.Text.Encoding.Unicode.GetBytes(value);
                var length = System.Text.Encoding.Unicode.GetByteCount(value);

                int pos = 0;
                int chunkSize = 1024; // Oracle does not allow large chunks.

                while (pos < length)
                {
                    chunkSize = chunkSize > (length - pos) ? chunkSize = length - pos : chunkSize;
                    clob.Write(bytes, pos, chunkSize);
                    pos += chunkSize;
                }

                var param = new OracleParameter(name, OracleDbType.Clob);
                param.Value = clob;

                command.Parameters.Add(param);
            };
        }
    }
}
