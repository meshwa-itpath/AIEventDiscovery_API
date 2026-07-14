using SmartComponents.LocalEmbeddings;

namespace AIEventDiscovery.Services.Embeddings;

public class LocalEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly LocalEmbedder _embedder;
    private bool _disposed;

    public LocalEmbeddingService()
    {
        // Initializes the embedder. 
        // The first time this runs, it will download the ~90MB all-MiniLM-L6-v2 ONNX model.
        // Subsequent runs will use the cached local file.
        _embedder = new LocalEmbedder();
    }

    public float[] GenerateEmbedding(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Compute the embedding locally on the CPU
        var embedding = _embedder.Embed(text);

        // SmartComponents returns an Embedding<float> struct which we convert to an array
        return embedding.Values.ToArray();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _embedder.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
