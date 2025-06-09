// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Connectors.Oracle;

/// <summary>
/// Options when creating a <see cref="OracleCollection{TKey, TRecord}"/>.
/// </summary>
public sealed class OracleCollectionOptions : VectorStoreCollectionOptions
{
    internal static readonly OracleCollectionOptions Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleCollectionOptions"/> class.
    /// </summary>
    public OracleCollectionOptions()
    {
    }

    internal OracleCollectionOptions(OracleCollectionOptions? source) : base(source)
    {
        this.Schema = source?.Schema ?? OracleVectorStoreOptions.Defaults.Schema;
    }

    /// <summary>
    /// Gets or sets the database schema.
    /// If not provided, the collection will be created under to the connected schema.
    /// </summary>
    public string? Schema { get; init; }
}
