using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// We need a special Mapping for Oracle.
    /// </summary>
    public class OracleGuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid guid)
        {
            parameter.Value = guid.ToString("N");
        }

        public override Guid Parse(object value)
        {
            return new Guid((string)value);
        }
    }
}
