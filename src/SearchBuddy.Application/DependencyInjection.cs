using Microsoft.Extensions.DependencyInjection;
using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.TextExtraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IIndexingPipeline, IndexingPipeline>();

            return services;
        }
    }
}
