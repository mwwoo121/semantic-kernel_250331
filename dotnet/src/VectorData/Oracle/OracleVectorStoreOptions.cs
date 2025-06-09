// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.AI;

namespace SemanticKernel.Connectors.Oracle;

/// <summary>
/// Options for creating a <see cref="OracleVectorStore"/>.
/// </summary>
public sealed class OracleVectorStoreOptions
{
    internal static readonly OracleVectorStoreOptions Defaults = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleVectorStoreOptions"/> class.
    /// </summary>
    public OracleVectorStoreOptions()
    {
    }

    internal OracleVectorStoreOptions(OracleVectorStoreOptions? source)
    {
        this.Schema = source?.Schema;
        this.EmbeddingGenerator = source?.EmbeddingGenerator;
    }

    /// <summary>
    /// Gets or sets the database schema.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the default embedding generator to use when generating vectors embeddings with this vector store.
    /// </summary>
    public IEmbeddingGenerator? EmbeddingGenerator { get; set; }
}
