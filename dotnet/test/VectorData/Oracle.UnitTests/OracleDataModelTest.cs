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

namespace SemanticKernel.Connectors.Oracle.UnitTests;

public class OracleDataModelTests : OracleConnectorTest
{
    public OracleDataModelTests() : base(new OracleDataSourceBuilder("Data Source = inst1; User ID = scott; Password = tiger").Build())
    {
    }

    //[Theory]
    //[InlineData(true)]
    //[InlineData(false)]
    [Fact]
    public void TestSimpleModel()
    {
        OracleSKMetadata metadata = this.CreateMetadata<PostgresHotel<int>>("Table1");

        //Verify if the metadat is created properly

        //Test simple class
        //Test complicated class with different variety of .NET type in the properties and index/vector index, storageName...etc.
        //Test all the supported data types

    }



    internal void VerifyMetadata(string tableName)
    {
    }
}
