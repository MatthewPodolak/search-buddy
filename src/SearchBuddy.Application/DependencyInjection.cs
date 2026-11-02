using Microsoft.Extensions.DependencyInjection;
using SearchBuddy.Application.Files;
using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<PathFilter>();

            services.AddSingleton<IIndexingPipeline, IndexingPipeline>();

            services.AddSingleton<IHashGenerator, HashGenerator>();

            services.AddSingleton<IFileEnumerator, FileEnumerator>();
            services.AddSingleton<ScanFilesTrigger>();

            return services;
        }
    }
}
