// ============================================================
// TRAVEL INSURANCE PDS Q&A — Beginner-friendly entry point
// ============================================================
// This program lets you ask questions about a travel insurance
// PDF or text file. It uses OpenAI to understand your question
// and find the right answer from the document.
//
// How it works (3 steps):
//   1. Load your document (PDF or TXT)
//   2. Build a "search index" using OpenAI embeddings
//   3. Answer your questions in a chat loop
// ============================================================

using Microsoft.Extensions.Configuration;
using OPenAIPDSQandA.Services;

// ── Load settings from appsettings.json and environment variables ──
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.local.json", optional: true)  // local secrets, not in git
    .AddEnvironmentVariables()
    .Build();

// Read the OpenAI settings. If not set, use sensible defaults.
string apiKey       = config["OpenAI:ApiKey"] ?? "";
string model        = config["OpenAI:Model"] ?? "gpt-4o";
string embedModel   = config["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
string systemPrompt = string.Join("\n", config.GetSection("OpenAI:SystemPrompt")
                          .GetChildren()
                          .Select(c => c.Value ?? ""));

// Read Qdrant connection settings
string qdrantHost = config["Qdrant:Host"] ?? "localhost";
int    qdrantPort = int.TryParse(config["Qdrant:Port"], out var p) ? p : 6334;

// ── Check the API key before doing anything else ──
if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "your-openai-api-key-here")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: OpenAI API key is not configured.");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Set your API key using ONE of the following methods:");
    Console.WriteLine("  1. Edit appsettings.json and replace 'your-openai-api-key-here'");
    Console.WriteLine("  2. Create appsettings.local.json with your key (this file is git-ignored)");
    Console.WriteLine("  3. Set environment variable: OPENAI__APIKEY=sk-...");
    Console.WriteLine();
    return;
}

// ── Create the three services we need ──
// DocumentService  — reads PDF or TXT files
// RagService       — splits the document into chunks and finds relevant ones
// OpenAIService    — sends questions + context to GPT and streams the answer
var documentService = new DocumentService();
var ragService      = new RagService(apiKey, embedModel, qdrantHost, qdrantPort);
var openAIService   = new OpenAIService(apiKey, model, systemPrompt);

// This list stores the conversation so the assistant remembers previous messages
var conversationHistory = new List<ConversationTurn>();

// ── Print a welcome banner ──
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   Travel Insurance PDS Q&A  (RAG · powered by OpenAI)   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// STEP 1 — Load the document
// ─────────────────────────────────────────────────────────────
Console.WriteLine("Enter the path to your PDS document (.pdf or .txt),");
Console.Write("or press Enter to use the built-in sample PDS: ");

string userPath  = Console.ReadLine()?.Trim() ?? "";
string pdsText;

if (string.IsNullOrEmpty(userPath))
{
    // No path entered — use the built-in sample insurance document
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Using built-in sample travel insurance PDS (Wanderlust Insurance).");
    Console.ResetColor();
    pdsText = documentService.GetSamplePds();
}
else if (File.Exists(userPath))
{
    // A valid file path was entered — load it
    try
    {
        Console.Write($"Loading '{Path.GetFileName(userPath)}'...");
        pdsText = await documentService.LoadDocumentAsync(userPath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" Done.");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($" Failed: {ex.Message}");
        Console.ResetColor();
        return;
    }
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"File not found: {userPath}");
    Console.ResetColor();
    return;
}

// ─────────────────────────────────────────────────────────────
// STEP 2 — Build the RAG (search) index
//
// The document is split into small chunks, and each chunk is
// converted into a list of numbers (an "embedding") that
// captures its meaning. Later, we compare your question's
// embedding against these to find the most relevant chunks.
// ─────────────────────────────────────────────────────────────
Console.WriteLine();
Console.Write("Building RAG index");

// CancellationTokenSource lets the user press Ctrl+C to stop at any point
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;   // prevent the process from closing immediately
    cts.Cancel();      // signal all async operations to stop
};

try
{
    await ragService.BuildIndexAsync(
        pdsText,
        onProgress: (done, total) =>
        {
            // \r moves back to the start of the line so we overwrite the previous message
            Console.Write($"\rBuilding RAG index — embedding chunk {done}/{total}...");
        },
        ct: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nCancelled.");
    return;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\nFailed to build index: {ex.Message}");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\rRAG index ready — {ragService.ChunkCount} chunks indexed.          ");
Console.ResetColor();
Console.WriteLine();
Console.WriteLine("Ready! Ask any question about the travel insurance policy.");
Console.WriteLine("Type 'clear' to reset conversation history, or 'exit' to quit.");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────
// STEP 3 — Interactive Q&A loop
//
// Keep asking for questions until the user types 'exit'
// or presses Ctrl+C.
// ─────────────────────────────────────────────────────────────
while (!cts.Token.IsCancellationRequested)
{
    // Prompt the user for their question
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("You: ");
    Console.ResetColor();

    string question = Console.ReadLine()?.Trim() ?? "";

    // Skip blank input
    if (string.IsNullOrEmpty(question))
        continue;

    // Allow the user to quit
    if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    // Allow the user to reset the conversation memory
    if (question.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        conversationHistory.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Conversation history cleared.");
        Console.ResetColor();
        Console.WriteLine();
        continue;
    }

    Console.WriteLine();

    try
    {
        // Find the document chunks most relevant to this question
        var results = await ragService.RetrieveWithScoresAsync(question, ct: cts.Token);

        // Show how many chunks were found and how similar each one is (0 = unrelated, 1 = identical)
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[Retrieved {results.Length} chunk(s) — similarity scores:");
        foreach (var result in results)
            Console.Write($" {result.Score:F3}");
        Console.WriteLine("]");
        Console.ResetColor();

        // Extract just the text from each result to pass to GPT
        string[] chunks = results.Select(r => r.Text).ToArray();

        // Send the question + relevant chunks to OpenAI and stream the answer to the console
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Assistant: ");
        Console.ResetColor();

        await openAIService.AskWithRagAsync(chunks, question, conversationHistory, Console.Out, cts.Token);
        Console.WriteLine();
        Console.WriteLine();
    }
    catch (OperationCanceledException)
    {
        // User pressed Ctrl+C mid-answer — exit cleanly
        Console.WriteLine();
        break;
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\nError: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine();
    }
}

Console.WriteLine("Goodbye!");
