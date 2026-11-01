using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SearchBuddy.Infrastructure.Persistance
{
    internal class TursoParams(object values) : SqlMapper.IDynamicParameters
    {
        public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            foreach (var prop in values.GetType().GetProperties())
            {
                var p = command.CreateParameter();
                p.ParameterName = "@" + prop.Name;
                p.Value = prop.GetValue(values) ?? DBNull.Value;
                command.Parameters.Add(p);
            }
        }
    }
}
