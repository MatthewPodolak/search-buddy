using SearchBuddy.Application;
using SearchBuddy.Indexer;
using SearchBuddy.Indexer.FileWatcher;
using SearchBuddy.Indexer.Indexer;
using SearchBuddy.Infrastructure;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Indexer", "watchsettings.json"),
    optional: true, reloadOnChange: true);

builder.Services.Configure<WatchSettings>(builder.Configuration.GetSection(WatchSettings.SectionName));
builder.Services.PostConfigure<WatchSettings>(s =>
{
    if (s.Extensions.Length == 0)
    {
        s.Extensions = [".txt", ".pdf", ".docx", ".pptx", ".cs", ".md"];
    }
});

builder.Services.AddSerilog(lc => lc
    .MinimumLevel.Information()
    .WriteTo.File(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Indexer"), "logs", "indexer-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

builder.Services.AddSingleton<ChangeQueue>();

builder.Services.AddHostedService<ChangeListenerWorker>();
builder.Services.AddHostedService<IndexingWorker>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var host = builder.Build();
host.Run();
