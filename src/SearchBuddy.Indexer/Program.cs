using SearchBuddy.Application;
using SearchBuddy.Application.Files;
using SearchBuddy.Application.Settings;
using SearchBuddy.Indexer;
using SearchBuddy.Indexer.FileWatcher;
using SearchBuddy.Indexer.Indexer;
using SearchBuddy.Indexer.Search;
using SearchBuddy.Infrastructure;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(o => o.ServiceName = "SearchBuddyIndexer");

builder.Configuration.AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SearchBuddy", "watchsettings.json"),
    optional: true, reloadOnChange: true);

builder.Services.AddSingleton(new IndexerState(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SearchBuddy", "state")));

builder.Services.Configure<WatchSettings>(builder.Configuration.GetSection(WatchSettings.SectionName));
builder.Services.PostConfigure<WatchSettings>(s =>
{
    if (s.Extensions.Length == 0)
    {
        s.Extensions = [".txt", ".pdf", ".docx", ".pptx", ".cs", ".md"];
    }

    if(s.ExcludedDirectoryNames.Length == 0)
    {
        s.ExcludedDirectoryNames =
        [
            "Windows", "Program Files", "Program Files (x86)", "ProgramData",
            "$Recycle.Bin", "System Volume Information", "AppData",
            "node_modules", ".git", ".vs", "bin", "obj"
        ];
    }
});

builder.Services.AddSerilog(lc => lc
    .MinimumLevel.Information()
    .WriteTo.File(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SearchBuddy"), "logs", "indexer-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

builder.Services.AddSingleton<ChangeQueue>();
builder.Services.AddSingleton<DeleteQueue>();

builder.Services.AddHostedService<ChangeListenerWorker>();
builder.Services.AddHostedService<DeleteWorker>();
builder.Services.AddHostedService<IndexingWorker>();
builder.Services.AddHostedService<SearchPipeWorker>();
builder.Services.AddHostedService<ScanWorker>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();

await host.Services.InitializeDatabaseAsync();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopped.Register(() =>
{
    if (Environment.ExitCode != 0) { Environment.Exit(Environment.ExitCode); }
});

if (host.Services.GetRequiredService<IndexerState>().IsEnabled)
{
    host.Services.GetRequiredService<ScanFilesTrigger>().Request();
}

host.Run();
