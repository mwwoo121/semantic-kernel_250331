using System;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;
using Oracle.ManagedDataAccess.Client;
using SemanticKernel.Connectors.Oracle;
using Oracle.Connectors.Common;
using Xunit;
using Xunit.Abstractions;
using System.Threading;
using System.Data;
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
    internal void TestBuildCreateTableCommand()
    {
        OracleSKMetadata metadata = this.CreateMetadata<PostgresHotel<int>>("Table1");


        OracleSqlCommandInfo sqlCmdInfo = OracleCommandGenerator.BuildCreateTableCommand(metadata, true);

        //Verify generated SQL
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
