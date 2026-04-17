using OpenAI.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.ClientModel;

namespace OPenAIPDSQandA.Services;

/// <summary>
/// Retrieval-Augmented Generation service backed by Qdrant vector database.
/// Splits the document into overlapping chunks, generates OpenAI embeddings,
/// stores them in Qdrant, and retrieves the most relevant chunks by cosine similarity.
/// </summary>
public class RagService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly QdrantClient _qdrant;

    private const string CollectionName  = "pds_chunks";
    private const int    ChunkSize       = 1_500;
    private const int    ChunkOverlap    = 250;
    private const int    DefaultTopK     = 5;
    private const int    EmbeddingBatchSize = 64;

    // Qdrant needs to know the vector size up front — text-embedding-3-small produces 1536 floats
    private const uint VectorSize = 1_536;

    public int ChunkCount { get; private set; }

    public RagService(string apiKey, string embeddingModel = "text-embedding-3-small",
                      string qdrantHost = "localhost", int qdrantPort = 6334)
    {
        _embeddingClient = new EmbeddingClient(embeddingModel, new ApiKeyCredential(apiKey));

        // QdrantClient communicates over gRPC (port 6334 by default)
        _qdrant = new QdrantClient(qdrantHost, qdrantPort);
    }

    // -------------------------------------------------------------------------
    // Indexing
    // -------------------------------------------------------------------------

    public async Task BuildIndexAsync(
        string documentText,
        Action<int, int>? onProgress = null,
        CancellationToken ct = default)
    {
        // Re-create the collection fresh each time so old data doesn't bleed in
        var collections = await _qdrant.ListCollectionsAsync(ct);
        if (collections.Any(c => c == CollectionName))
            await _qdrant.DeleteCollectionAsync(CollectionName, cancellationToken: ct);

        await _qdrant.CreateCollectionAsync(
            CollectionName,
            new VectorParams { Size = VectorSize, Distance = Distance.Cosine },
            cancellationToken: ct);

        var chunks = ChunkDocument(documentText);
        int total  = chunks.Count;
        int done   = 0;

        // Embed and upsert in batches
        for (int batchStart = 0; batchStart < total; batchStart += EmbeddingBatchSize)
        {
            ct.ThrowIfCancellationRequested();

            int batchEnd   = Math.Min(batchStart + EmbeddingBatchSize, total);
            var batch      = chunks.GetRange(batchStart, batchEnd - batchStart);
            var embeddings = await EmbedBatchAsync(batch, ct);

            // Build Qdrant points — each point has an ID, a vector, and a payload
            var points = batch.Select((text, i) => new PointStruct
            {
                Id      = (ulong)(batchStart + i),
                Vectors = embeddings[i],
                Payload =
                {
                    // Store the original text and its position so we can sort results later
                    ["text"]  = text,
                    ["index"] = batchStart + i
                }
            }).ToList();

            await _qdrant.UpsertAsync(CollectionName, points, cancellationToken: ct);

            done += batch.Count;
            onProgress?.Invoke(done, total);
        }

        ChunkCount = total;
    }

    // -------------------------------------------------------------------------
    // Retrieval
    // -------------------------------------------------------------------------

    public async Task<string[]> RetrieveAsync(
        string query,
        int topK = DefaultTopK,
        CancellationToken ct = default)
    {
        var results = await RetrieveWithScoresAsync(query, topK, ct);
        return results.Select(r => r.Text).ToArray();
    }

    public async Task<(string Text, float Score)[]> RetrieveWithScoresAsync(
        string query,
        int topK = DefaultTopK,
        CancellationToken ct = default)
    {
        if (ChunkCount == 0)
            return [];

        var queryEmbedding = await EmbedSingleAsync(query, ct);

        // Ask Qdrant for the top-K most similar vectors, including the stored payload
        var hits = await _qdrant.SearchAsync(
            CollectionName,
            queryEmbedding,
            limit: (ulong)topK,
            payloadSelector: new WithPayloadSelector { Enable = true },
            cancellationToken: ct);

        // Sort by original document position (index) so context reads in order
        return hits
            .OrderBy(h => (int)h.Payload["index"].IntegerValue)
            .Select(h => (Text: h.Payload["text"].StringValue, Score: h.Score))
            .ToArray();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<float[]> EmbedSingleAsync(string text, CancellationToken ct)
    {
        var result = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.Value.ToFloats().ToArray();
    }

    private async Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken ct)
    {
        var result = await _embeddingClient.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
        return result.Value
                     .OrderBy(e => e.Index)
                     .Select(e => e.ToFloats().ToArray())
                     .ToList();
    }

    private static List<string> ChunkDocument(string text)
    {
        var chunks = new List<string>();
        int start  = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + ChunkSize, text.Length);

            if (end < text.Length)
            {
                int searchFrom = Math.Max(start + ChunkSize / 2, 0);
                int searchLen  = end - searchFrom;

                int paraBreak = text.LastIndexOf("\n\n", end, searchLen);
                if (paraBreak > searchFrom)
                {
                    end = paraBreak + 2;
                }
                else
                {
                    int lineBreak = text.LastIndexOf('\n', end, Math.Min(searchLen, 200));
                    if (lineBreak > searchFrom)
                        end = lineBreak + 1;
                }
            }

            var chunk = text[start..end].Trim();
            if (chunk.Length > 50)
                chunks.Add(chunk);

            start = Math.Max(start + 1, end - ChunkOverlap);
        }

        return chunks;
    }
}

// DocumentChunk record is no longer needed — data lives in Qdrant payloads
