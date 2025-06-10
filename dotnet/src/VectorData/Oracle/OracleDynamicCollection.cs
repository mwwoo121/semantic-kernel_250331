// Copyright (c) Microsoft. All rights reserved.

#if!READY
using System;
using System.Collections.Generic;
using Oracle.Connectors.Common;
using Oracle.ManagedDataAccess.Client;

namespace SemanticKernel.Connectors.Oracle;
/// <summary>
/// Represents a collection of vector store records in a Postgres database, mapped to a dynamic <c>Dictionary&lt;string, object?&gt;</c>.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class OracleDynamicCollection : OracleCollection<object, Dictionary<string, object?>>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleDynamicCollection"/> class.
    /// </summary>
    /// <param name="dataSource">The data source to use for connecting to the database.</param>
    /// <param name="name">The name of the collection.</param>
    /// <param name="ownsDataSource">A value indicating whether the data source should be disposed when the collection is disposed.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    public OracleDynamicCollection(OracleDataSource dataSource, string name, bool ownsDataSource, OracleCollectionOptions options)
        : this(() => new OracleDbClient(dataSource, ownsDataSource), name, options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresCollection{TKey, TRecord}"/> class.
    /// </summary>
    /// <param name="connectionString">Postgres database connection string.</param>
    /// <param name="name">The name of the collection.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    public OracleDynamicCollection(string connectionString, string name, OracleCollectionOptions options)
        : this(() => new OracleDbClient(OracleUtils.CreateDataSource(connectionString), bDisposeDataSource: true), name, options)
    {
    }

    internal OracleDynamicCollection(Func<OracleDbClient> clientFactory, string name, OracleCollectionOptions options)
        : base(
            clientFactory,
            name,
            static options => new OracleModelBuilder().BuildDynamic(
                options.Definition ?? throw new ArgumentException("Definition is required for dynamic collections"),
                options.EmbeddingGenerator),
            options)
    {
    }
}
#endif
