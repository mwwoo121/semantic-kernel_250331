// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.Connectors.Common;
using Oracle.ManagedDataAccess.Client;
using Xunit.Abstractions;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;

using System.Threading;

namespace SemanticKernel.Connectors.Oracle.UnitTests;

public partial class OracleConnectorTest
{
    static internal string ConnectionString = "Data Source = inst1; User ID = scott; Password = tiger";
    private readonly OracleDataSource _dataSource;
    protected OracleConnectorTest(OracleDataSource ds)
    {
        this._dataSource = ds;

        using (OracleConnection conn = ds.OpenConnection())
        {
            this.DatabaseInfo = new()
            {
                DbCharacterSet = conn.DatabaseCharset,
                MaxIdentifierLength = conn.DatabaseMaxIdentifierLength
            };
        }

        this.DbClient = new OracleDbClient(this._dataSource, false);
    }

    internal OracleVectorStore VectorStore { get; init; }

    internal OracleDbClient DbClient { get; init; }

    internal OracleDatabaseInfo DatabaseInfo { get; init; }

    public async Task<OracleConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await this._dataSource.OpenConnectionAsync(cancellationToken);
    }

    internal OracleSKMetadata CreateMetadata<TModel>(string tableName, string? schemaName = null)
    {
        OracleCollectionOptions options = new() { Schema = schemaName };

        CollectionModel model = new OracleModelBuilder().Build(typeof(TModel), options.Definition, options.EmbeddingGenerator);

        OracleSKMetadata metadata = new(model, this.DatabaseInfo, tableName, options.Schema);
        return metadata;
    }
}
