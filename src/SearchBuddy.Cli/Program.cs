using System.IO.Pipes;
using System.Text.Json;
using SearchBuddy.Application.Contracts;

if (args.Length == 0)
{
    Console.WriteLine("Usage -- sb \"question\"");
    return 1;
}

var query = args[0];

await using var pipe = new NamedPipeClientStream(".", "SearchBuddy", PipeDirection.InOut);

try
{
    await pipe.ConnectAsync(2000);
}
catch (TimeoutException)
{
    Console.Error.WriteLine("INDEXER PROCESS IS OFF");
    return 1;
}

using var reader = new StreamReader(pipe);
await using var writer = new StreamWriter(pipe) { AutoFlush = true };

await writer.WriteLineAsync(JsonSerializer.Serialize(new SearchRequest(query, 5)));

Console.WriteLine("searching...");

var response = JsonSerializer.Deserialize<SearchResponse>(await reader.ReadLineAsync() ?? "null");
if (response is null)
{
    return 1;
}

Console.WriteLine();
Console.WriteLine(response.Answer);
Console.WriteLine();

for (int i = 0; i < response.Sources.Count; i++)
{
    Console.WriteLine($"[{i + 1}] {response.Sources[i].Distance:F3}  {response.Sources[i].Path}");
}

return 0;