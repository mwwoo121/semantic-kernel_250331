using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;
using Oracle.Connectors.Common;
using Oracle.ManagedDataAccess.Client;
using SemanticKernel.Connectors.Oracle;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SemanticKernel.Connectors.Oracle.UnitTests;

public class OracleCommandGeneratorTests : OracleConnectorTest
{
    public OracleCommandGeneratorTests() : base(new OracleDataSourceBuilder("Data Source = inst1; User ID = scott; Password = tiger").Build())
    {
    }
    //[Theory]
    //[InlineData(true)]
    //[InlineData(false)]
    [Fact]
    public async Task TestBuildCreateTableCommandAsync()
    {
        OracleSKMetadata metadata = this.CreateMetadata<PostgresHotel<int>>("Table1");

        await this.DbClient.CreateTableAsync(metadata, true, default);

        await this.VerifyTableCreationAsync(metadata);
    }

    [Fact]
    public async Task TestBuildCreateAndDropTableCommandAsync()
    {
        OracleSKMetadata metadata = this.CreateMetadata<PostgresHotel<int>>("Table1");

        await this.DbClient.CreateTableAsync(metadata, true, default);
        //Verify table is created

        bool bExists = await this.DbClient.IsTableExistAsync(metadata.QualifiedTableName, default);
        //Verify bExists = true

        await this.DbClient.DeleteTableAsync(metadata.QualifiedTableName, default);

        bExists = await this.DbClient.IsTableExistAsync(metadata.QualifiedTableName, default);
        //Verify bExists = true
    }


    [Fact]
    public async Task TestBuildCreateAndDropTableInDiffUserAsync()
    {
        OracleSKMetadata metadata = this.CreateMetadata<PostgresHotel<int>>("Table1", "HR");

        await this.DbClient.CreateTableAsync(metadata, true, default);
        //Verify table is created

        bool bExists = await this.DbClient.IsTableExistAsync(metadata.QualifiedTableName, default);
        //Verify bExists = true

        await this.DbClient.DeleteTableAsync(metadata.QualifiedTableName, default);

        bExists = await this.DbClient.IsTableExistAsync(metadata.QualifiedTableName, default);
        //Verify bExists = true
    }

    [Fact]
    public async Task TestUpsertCommand()
    {
        //Use testing team database for generating embedding as the schema is not setup for Vector data type yet.
        string tnsDescriptor = "(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=100.70.104.94)(PORT=5521))(CONNECT_DATA=(SERVICE_NAME=cdb1_pdb1.regress.rdbms.dev.us.oracle.com)))";
        string connStrEmbeddingGenerator = $"Data Source = {tnsDescriptor}; User ID = ONNXUSER; Password = ONNXPass";
        OracleConnection connEmbeddingGenerator = new(connStrEmbeddingGenerator);
        connEmbeddingGenerator.Open();
        OracleOnnxEmbeddingGenerator embeddingGenerator = new OracleOnnxEmbeddingGenerator(connEmbeddingGenerator);

        //Database that supports Vector data type
        string connStr = $"Data Source = inst1; User ID = scott; Password = tiger";
        OracleDataSource ds = new OracleDataSourceBuilder(connStr).Build();

        // Construct the vector store and get the collection.
        var vectorStoreOptions = new OracleVectorStoreOptions()
        {
            EmbeddingGenerator = embeddingGenerator
        };

        OracleVectorStore vectorStore = new(ds, false, vectorStoreOptions);
        var collection = vectorStore.GetCollection<string, SimpleClass>("table1");

        // Create the collection if it doesn't exist.
        await collection.EnsureCollectionExistsAsync();

        // Create glossary entries and generate embeddings for them.
        var simpleEntry = new SimpleClass
        {
            Key = "1",
            Category = "Software",
            Definition = "Application Programming Interface. A set of rules and specifications that allow software components to communicate and exchange data."
        };

        simpleEntry.DefinitionEmbedding = (await embeddingGenerator.GenerateAsync(simpleEntry.Definition)).Vector;

        await collection.UpsertAsync(simpleEntry);

        //TODO_Jiacheng: Below is the generated SQL.  Please check.
        //MERGE INTO "table1" d USING(SELECT :"Key" AS "Key" , :"Category" AS "Category" , :"Definition" AS "Definition" , :"DefinitionEmbedding" AS "DefinitionEmbedding"  FROM DUAL ) s ON(d."Key" = s."Key" ) WHEN MATCHED THEN UPDATE SET d."Key" = s."Key", d."Category" = s."Category", d."Definition" = s."Definition", d."DefinitionEmbedding" = s."DefinitionEmbedding" WHEN NOT MATCHED THEN INSERT(d."Key", d."Category", d."Definition", d."DefinitionEmbedding") VALUES(s."Key", s."Category", s."Definition", s."DefinitionEmbedding")

        // Retrieve an item from the collection and write it to the console.
        var record = await collection.GetAsync("4");
        Console.WriteLine(record!.Definition);
    }


    internal async Task VerifyTableCreationAsync(OracleSKMetadata metadata)
    {
        using (OracleConnection conn = await this.OpenConnectionAsync())
        {
            using (OracleCommand cmd = new($"SELECT * FROM {metadata.TableName}", conn))
            {
                OracleDataReader reader = cmd.ExecuteReader();
                DataTable schemaTable = reader.GetSchemaTable();

                reader.Read();
                foreach (string colName in metadata.AllColumnsByDbObjName.Keys)
                {
                    int ordinal = reader.GetOrdinal(colName);

                    Console.WriteLine($"{colName} is of ordinal {ordinal}");

                    object val1 = reader.GetValue(ordinal);
                    object val2 = reader.GetValue(colName);

                    Console.WriteLine($"{colName} of ordinal {ordinal} has value {val1}, {val2}");
                }

                //Check Schema table or check using SQL*plus 
            }
        }
    }
}
