// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SemanticKernel.Connectors.Oracle.UnitTests;
internal class OracleOnnxEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private OracleConnection _conn;
    private readonly EmbeddingGeneratorMetadata _metadata;

    public OracleOnnxEmbeddingGenerator(OracleConnection conn)
    {
        this._conn = conn;
        this._metadata = new EmbeddingGeneratorMetadata("Oracle ONNX");
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
    IEnumerable<string> values,
    EmbeddingGenerationOptions? options = null,
    CancellationToken cancellationToken = default)
    {
        string sql = "SELECT VECTOR_EMBEDDING(ALL_MINILM_L12_V2 USING :1 as DATA)";

        List<ReadOnlyMemory<float>> listOfFloats = new();
        using (OracleCommand cmd = this._conn.CreateCommand())
        {
            cmd.CommandText = sql;

            OracleParameter parameter = new("1", OracleDbType.Varchar2, System.Data.ParameterDirection.Input);
            cmd.Parameters.Add(parameter);

            //Try one value first.
            foreach (string data in values)
            {
                parameter.Value = data;
                float[] floats = null;
                using (OracleDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (reader.Read())
                    {
                        floats = reader.GetFloatArray(0);
                        listOfFloats.Add(new ReadOnlyMemory<float>(floats));
                    }
                }
            }

            return new GeneratedEmbeddings<Embedding<float>>(listOfFloats.Select(e => new Embedding<float>(e)));
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        Verify.NotNull(serviceType);

        return
            serviceKey is null ? null :
            serviceType.IsInstanceOfType(this) ? this :
            serviceType == typeof(EmbeddingGeneratorMetadata) ? this._metadata :
            null;
    }

    public void Dispose()
    {
        this._conn?.Dispose();
    }

}


