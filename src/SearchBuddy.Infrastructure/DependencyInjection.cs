using Microsoft.Extensions.DependencyInjection;
using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.TextExtraction;
using SearchBuddy.Infrastructure.TextExtractors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<ITextExtractor, PlainTextExtractor>();
            services.AddSingleton<ITextExtractor, PdfExtractor>();
            services.AddSingleton<ITextExtractor, DocxExtractor>();
            services.AddSingleton<ITextExtractor, PptxExtractor>();

            return services;
        }
    }
}
