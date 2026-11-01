using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.LLM;
using SearchBuddy.Application.Storage;
using SearchBuddy.Application.Text;
using SearchBuddy.Infrastructure.LLM;
using SearchBuddy.Infrastructure.Persistance;
using SearchBuddy.Infrastructure.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ITextExtractor, PlainTextExtractor>();
            services.AddSingleton<ITextExtractor, PdfExtractor>();
            services.AddSingleton<ITextExtractor, DocxExtractor>();
            services.AddSingleton<ITextExtractor, PptxExtractor>();

            services.AddSingleton<IDocumentStore, TursoDocumentStore>();
            services.AddSingleton<TursoConnectionFactory>();

            services.AddSingleton<ITextChunker, TextChunker>();
            services.AddSingleton<IEmbedder, Embedder>();

            services.AddSingleton<ILlmGen, OnnxLlmGen>();

            services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

            return services;
        }

        public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
        {
            var factory = services.GetRequiredService<TursoConnectionFactory>();

            await using var connection = await factory.OpenAsync(ct);

            await connection.ExecuteScalarAsync<string>(new CommandDefinition("PRAGMA journal_mode = 'mvcc'", cancellationToken: ct));

            foreach (var statement in TursoSchema.Statements)
            {
                await connection.ExecuteAsync(new CommandDefinition(statement, cancellationToken: ct));
            }
        }
    }
}
