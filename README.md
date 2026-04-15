# Travel Insurance PDS Q&A (OpenAI · RAG with Embeddings)

A .NET console application that lets you ask natural-language questions about a travel insurance
Product Disclosure Statement (PDS), powered by OpenAI **embeddings** and **streaming chat**.

## How it works (RAG pipeline)

```
┌──────────────┐   chunk + embed   ┌────────────────────┐
│  PDS document │ ────────────────► │  In-memory vector  │
│  (.pdf/.txt) │  text-embedding-  │  index (cosine sim)│
└──────────────┘  3-small (batch)  └────────┬───────────┘
                                            │ top-K chunks
                                   ┌────────▼───────────┐
          User question ──embed──► │  Similarity search │
                                   └────────┬───────────┘
                                            │ grounded context
                                   ┌────────▼───────────┐
                                   │  GPT-4o (streaming)│
                                   │  chat completion   │
                                   └────────────────────┘
```

1. **Chunking** — the PDS is split into overlapping ~375-token chunks (paragraph-aware).
2. **Batch embedding** — all chunks are embedded in batches via `text-embedding-3-small` (one API call per 64 chunks, not one per chunk).
3. **Retrieval** — each user question is embedded; the top-5 chunks by cosine similarity are selected.
4. **Generation** — GPT-4o receives only the retrieved chunks as context and streams a grounded answer.

## Features

- **Embedding-based retrieval (RAG)** — only the most relevant sections of the PDS are sent to the LLM
- **Batch embeddings** — efficient: 64 chunks per API call during indexing
- **Similarity scores** — displayed next to each retrieved chunk count so you can see how relevant the matches are
- **Streaming responses** — answers stream in real-time as OpenAI generates them
- **Conversation memory** — follow-up questions retain context from previous answers
- **PDF & TXT support** — load your own PDS document, or use the built-in sample

## Quick Start

### 1. Configure your OpenAI API key

**Option A — local settings file (recommended):**
Create `appsettings.local.json` in the project root (this file is git-ignored):
```json
{
  "OpenAI": {
    "ApiKey": "sk-..."
  }
}
```

**Option B — environment variable:**
```powershell
$env:OPENAI__APIKEY = "sk-..."
```

**Option C — edit `appsettings.json`** (not recommended for real keys):
Replace `your-openai-api-key-here` with your key.

### 2. Run the application

```bash
dotnet run
```

### 3. Load a PDS and start asking questions

```
Enter the path to your PDS document (.pdf or .txt), or press Enter to use the built-in sample PDS:
> [press Enter for sample, or type a path like C:\Documents\my-pds.pdf]

Building RAG index — embedding chunk 24/24...
RAG index ready — 24 chunks indexed.

Ready! Ask any question about the travel insurance policy.
Type 'clear' to reset conversation history, or 'exit' to quit.

You: What is covered under emergency medical expenses?
[Retrieved 5 chunk(s) — similarity scores: 0.842 0.817 0.804 0.791 0.763]
Assistant: ...
```

## Configuration

`appsettings.json`:

| Key | Default | Description |
|-----|---------|-------------|
| `OpenAI:ApiKey` | _(required)_ | Your OpenAI API key |
| `OpenAI:Model` | `gpt-4o` | Chat model (e.g. `gpt-4o`, `gpt-4o-mini`) |
| `OpenAI:EmbeddingModel` | `text-embedding-3-small` | Embedding model for RAG indexing & retrieval |

## Project Structure

```
OPenAIPDSQandA/
├── Program.cs                  # Entry point, interactive Q&A loop
├── Services/
│   ├── RagService.cs           # Chunking, batch embedding, cosine-similarity retrieval
│   ├── OpenAIService.cs        # Streaming chat completions via OpenAI SDK
│   └── DocumentService.cs      # PDF/TXT loading + built-in sample PDS
├── appsettings.json            # Configuration (model, embedding model, API key placeholder)
├── appsettings.local.json      # Your secrets (git-ignored, create this yourself)
└── OPenAIPDSQandA.csproj
```

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `OpenAI` | Official OpenAI .NET SDK — embeddings + streaming chat |
| `UglyToad.PdfPig` | PDF text extraction |
| `Microsoft.Extensions.Configuration.*` | Layered configuration |

## Supported Commands

| Command | Description |
|---------|-------------|
| _(any question)_ | Ask about the travel insurance policy |
| `clear` | Reset conversation history |
| `exit` | Quit the application |
| `Ctrl+C` | Graceful shutdown |
