using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<IIndexingPipeline, IndexingPipeline>();

            services.AddSingleton<IHashGenerator, HashGenerator>();

            return services;
        }
    }
}
