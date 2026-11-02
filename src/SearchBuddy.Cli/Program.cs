using System.IO.Pipes;
using System.Text.Json;
using SearchBuddy.Application.Contracts;

if (args.Length == 0)
{
    Console.WriteLine("--- Command not found --- || Use \"sb --help\" to see what you can do");
    return 1;
}

switch (args[0].ToLowerInvariant())
{
    case "--help":
    case "-h":
        PrintHelp();
        return 0;
    case "--on":
        return await OnRag(!(args.Length > 1 && args[1].Equals("--noupd")));
    case "--off":
        return await OffRag();
    case "--upd":
        return await ForceDataUpdate();
}

if (args[0].StartsWith('-'))
{
    Console.WriteLine("--- Command not found --- || Use \"sb --help\" to see what you can do");
    return 1;
}

return await Search(args[0]);


//pv//
static void PrintHelp()
{
    Console.WriteLine("""
        --- AVALIABLE COMMANDS ---

        sb --help || you are here
        sb --off || disable background process
        sb --on || start background prcoess (will start data update sim.)
        sb --on --noupd || start backround process (without data update)
        sb --upd || force data update in the background
        sb "ask something" || getting rag to work
        """);
}

static async Task<int> OffRag()
{
    var response = JsonSerializer.Deserialize<CommandResponse>(await SendAsync(new PipeRequest(PipeCommands.Off)) ?? "null");

    Console.WriteLine(response?.Message ?? "No response from indexer");
    return response?.Ok == true ? 0 : 1;
}

static async Task<int> OnRag(bool withUpdate = true)
{
    var response = JsonSerializer.Deserialize<CommandResponse>(await SendAsync(new PipeRequest(PipeCommands.On, WithUpdate: withUpdate)) ?? "null");

    Console.WriteLine(response?.Message ?? "No response from indexer");
    return response?.Ok == true ? 0 : 1;
}

static async Task<int> ForceDataUpdate()
{
    var response = JsonSerializer.Deserialize<CommandResponse>(await SendAsync(new PipeRequest(PipeCommands.Update)) ?? "null");

    Console.WriteLine(response?.Message ?? "No response from indexer");
    return response?.Ok == true ? 0 : 1;
}

static async Task<int> Search(string query)
{
    Console.WriteLine("searching...");

    var res = JsonSerializer.Deserialize<SearchResponse>(await SendAsync(new PipeRequest(PipeCommands.Search, query)) ?? "null");
    if (res is null)
    {
        return 1;
    }

    Console.WriteLine();
    Console.WriteLine(res.Answer);
    Console.WriteLine();

    for (int i = 0; i < res.Sources.Count; i++)
    {
        Console.WriteLine($"[{i + 1}] {res.Sources[i].Distance:F3}  {res.Sources[i].Path}");
    }

    return 0;
}

static async Task<string?> SendAsync(PipeRequest request)
{
    await using var pipe = new NamedPipeClientStream(".", "SearchBuddy", PipeDirection.InOut, PipeOptions.Asynchronous);

    try
    {
        await pipe.ConnectAsync(2000);
    }
    catch (TimeoutException)
    {
        Console.Error.WriteLine("INDEXER PROCESS IS OFF");
        return null;
    }

    using var reader = new StreamReader(pipe);
    await using var writer = new StreamWriter(pipe) { AutoFlush = true };

    await writer.WriteLineAsync(JsonSerializer.Serialize(request));
    return await reader.ReadLineAsync();
}