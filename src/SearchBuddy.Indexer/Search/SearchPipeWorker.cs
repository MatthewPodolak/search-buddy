using System.IO.Pipes;
using System.Text.Json;
using SearchBuddy.Application.Contracts;
using SearchBuddy.Application.Storage;
using System.Security.AccessControl;
using System.Security.Principal;
using SearchBuddy.Application.LLM;
using SearchBuddy.Application.Files;

namespace SearchBuddy.Indexer.Search
{
    public class SearchPipeWorker(IEmbedder embedder, IDocumentStore store, ILlmGen llm, ScanFilesTrigger scanTrigger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var security = new PipeSecurity();

                security.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));

                security.AddAccessRule(new PipeAccessRule(
                    WindowsIdentity.GetCurrent().User!,
                    PipeAccessRights.FullControl, AccessControlType.Allow));

                await using var pipe = NamedPipeServerStreamAcl.Create("SearchBuddy", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,  0, 0, security);
                await pipe.WaitForConnectionAsync(stoppingToken);

                using var reader = new StreamReader(pipe);
                await using var writer = new StreamWriter(pipe) { AutoFlush = true };

                try
                {
                    var request = JsonSerializer.Deserialize<PipeRequest>(await reader.ReadLineAsync(stoppingToken) ?? "null");
                    if (request is null) continue;

                    switch (request?.Command)
                    {
                        case PipeCommands.Search when !string.IsNullOrWhiteSpace(request.Query):

                            var vector = await embedder.EmbeddAsync(request.Query, true, stoppingToken);

                            var hits = await store.SearchAsync(vector, stoppingToken);

                            var answer = await llm.AskAsync(request.Query, hits, stoppingToken);

                            await writer.WriteLineAsync(JsonSerializer.Serialize(new SearchResponse(answer, hits)));

                            break;
                        case PipeCommands.Update:
                            scanTrigger.Request();
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new CommandResponse(true, "Data update scheduled")));
                            break;
                        default:
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new CommandResponse(false, "Unknown command")));
                            break;
                    }    
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Console.Error.WriteLine(ex);
                    try { await writer.WriteLineAsync(JsonSerializer.Serialize(new SearchResponse("INDEXER ISSUE", []))); } catch { }
                }
            }
        }
    }
}