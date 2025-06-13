// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace SemanticKernel.Connectors.Oracle.UnitTests;
public class OracleVectorStoreTests
{
    [Fact]
    public async Task TestBuildCreateVectorStoreAsync()
    {
        OracleVectorStore vs = new OracleVectorStore((new OracleDataSourceBuilder("Data Source = inst1; User ID = scott; Password = tiger")).Build());
    //    OracleSKMetadata metadata = this.CreateMetadata<PostgresHotel<int>>("Table1");

    //    await this.DbClient.CreateTableAsync(metadata, true, default);

    //    await this.VerifyTableCreationAsync(metadata);
    //
    }

    [Fact]
    public async Task TestEmbeddingGenerator()
    {
        string tnsDescriptor = "(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=100.70.104.94)(PORT=5521))(CONNECT_DATA=(SERVICE_NAME=cdb1_pdb1.regress.rdbms.dev.us.oracle.com)))";
        OracleConnection conn = new($"Data Source = {tnsDescriptor}; User ID = ONNXUSER; Password = ONNXPass");
        conn.Open();
        var embeddingGenerator = new OracleOnnxEmbeddingGenerator(conn);

        var embeddings = await embeddingGenerator.GenerateAsync(["Hello, I am Martha", "This is Jiacheng"]);

    }

}
