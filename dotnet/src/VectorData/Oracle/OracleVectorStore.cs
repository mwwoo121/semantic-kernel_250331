// Copyright (c) Microsoft. All rights reserved.
#if !READY
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;
using Oracle.Connectors.Common;
using Oracle.ManagedDataAccess.Client;

namespace SemanticKernel.Connectors.Oracle;


/// <summary>
/// Represents a vector store implementation using PostgreSQL.
/// </summary>
public sealed class OracleVectorStore : VectorStore
{
    private readonly OracleDbClient _client;

    /// <summary>Metadata about vector store.</summary>
    private readonly VectorStoreMetadata _metadata;

    /// <summary>A general purpose definition that can be used to construct a collection when needing to proxy schema agnostic operations.</summary>
    private static readonly VectorStoreCollectionDefinition s_generalPurposeDefinition = new() { Properties = [new VectorStoreKeyProperty("Key", typeof(string))] };

    /// <summary>The database schema.</summary>
    private readonly string? _schema;

    private readonly IEmbeddingGenerator? _embeddingGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleVectorStore"/> class.
    /// </summary>
    /// <param name="dataSource">Oracle  data source.</param>
    /// <param name="bDisposeDataSource">A value indicating whether <paramref name="dataSource"/> is disposed when this instance of <see cref="OracleVectorStore"/> is disposed.</param>
    /// <param name="options">Optional configuration options for this class</param>
    public OracleVectorStore(OracleDataSource dataSource, bool bDisposeDataSource, OracleVectorStoreOptions? options = default)
    {
        Verify.NotNull(dataSource);

        options ??= OracleVectorStoreOptions.Defaults;
        this._schema = options.Schema;
        this._embeddingGenerator = options?.EmbeddingGenerator;
        this._client = new OracleDbClient(dataSource, bDisposeDataSource);

        this._metadata = new()
        {
            VectorStoreSystemName = OracleConstants.VectorStoreSystemName,
            VectorStoreName = this._client.DatabaseName
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleVectorStore"/> class.
    /// </summary>
    /// <param name="connectionString">Oracle database connection string.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    public OracleVectorStore(string connectionString, OracleVectorStoreOptions? options = default)
        :this(OracleUtils.CreateDataSource(connectionString), bDisposeDataSource: true, options)
    {
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        this._client.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override IAsyncEnumerable<string> ListCollectionNamesAsync(CancellationToken cancellationToken = default)
    {
        return OracleUtils.WrapAsyncEnumerableAsync(
            this._client.GetTablesAsync(this._schema, cancellationToken),
            "ListCollectionNames",
            this._metadata
        );
    }

#pragma warning disable IDE0090 // Use 'new(...)'
    /// <inheritdoc />
    [RequiresDynamicCode("This overload of GetCollection() is incompatible with NativeAOT. For dynamic mapping via Dictionary<string, object?>, call GetDynamicCollection() instead.")]
    [RequiresUnreferencedCode("This overload of GetCollecttion() is incompatible with trimming. For dynamic mapping via Dictionary<string, object?>, call GetDynamicCollection() instead.")]
#if NET8_0_OR_GREATER
    public override OracleCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreCollectionDefinition? definition = null)
#else
    public override VectorStoreCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreCollectionDefinition? definition = null)
#endif
        => typeof(TRecord) == typeof(Dictionary<string, object?>)
            ? throw new ArgumentException(VectorDataStrings.GetCollectionWithDictionaryNotSupported)
            : new OracleCollection<TKey, TRecord>(
                () => this._client.Share(),
                name,
                new()
                {
                    Schema = this._schema,
                    Definition = definition,
                    EmbeddingGenerator = this._embeddingGenerator,
                }
            );

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    public override OracleDynamicCollection GetDynamicCollection(string name, VectorStoreCollectionDefinition definition)
#else
    public override VectorStoreCollection<object, Dictionary<string, object?>> GetDynamicCollection(string name, VectorStoreCollectionDefinition definition)
#endif
        => new OracleDynamicCollection(
            () => this._client.Share(),
            name,
            new()
            {
                Schema = this._schema,
                Definition = definition,
                EmbeddingGenerator = this._embeddingGenerator,
            }
        );
#pragma warning restore IDE0090 // Use 'new(...)'

    /// <inheritdoc />
    public override Task<bool> CollectionExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var collection = this.GetDynamicCollection(name, s_generalPurposeDefinition);
        return collection.CollectionExistsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override Task EnsureCollectionDeletedAsync(string name, CancellationToken cancellationToken = default)
    {
        var collection = this.GetDynamicCollection(name, s_generalPurposeDefinition);
        return collection.EnsureCollectionDeletedAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        Verify.NotNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(VectorStoreMetadata) ? this._metadata :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }
}

#endif
