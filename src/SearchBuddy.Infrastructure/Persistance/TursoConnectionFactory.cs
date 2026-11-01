using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.Common;
using Turso;

namespace SearchBuddy.Infrastructure.Persistance
{
    public class TursoConnectionFactory(IOptions<DatabaseSettings> options)
    {
        public async Task<DbConnection> OpenAsync(CancellationToken ct)
        {
            var path = options.Value.Path;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var connection = new TursoConnection($"Data Source={path}");

            await connection.OpenAsync(ct);
            await connection.ExecuteAsync(new CommandDefinition("PRAGMA foreign_keys = ON", cancellationToken: ct));

            return connection;
        }
    }
}
