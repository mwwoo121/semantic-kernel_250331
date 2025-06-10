using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;
using Oracle.ManagedDataAccess.Client;
using SemanticKernel.Connectors.Oracle;
using Oracle.Connectors.Common;
using Xunit;
using Xunit.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace SemanticKernel.Connectors.Oracle.UnitTests;

public class OracleFilterGeneratorTests : OracleConnectorTest
{
    public OracleFilterGeneratorTests() : base(new OracleDataSourceBuilder("Data Source = inst1; User ID = scott; Password = tiger").Build())
    {
    }

    //[Theory]
    //[InlineData(true)]
    //[InlineData(false)]
    [Fact]
    public void TestGenerateFilter()
    {
        OracleSKMetadata metadata = this.CreateMetadata<SqliteHotel<string>>("Table1");

        Expression<Func<SqliteHotel<string>, bool>> exp = r => r.HotelId == "8" && r.HotelName == "Hotel1" && r.ParkingIncluded == false;

        OracleLambdaFilterTranslator filter = new(metadata.Model, exp, null);

        OracleSqlCommandInfo filterInfo = filter.GenerateSQL(metadata, 1);

        this.VerifyFilter(filterInfo);
    }

    internal void VerifyFilter(OracleSqlCommandInfo filterInfo)
    {
        //Verify if the SQL is generated properly for the filter 
        Console.WriteLine(filterInfo.SqlText);
        Console.WriteLine(filterInfo.Parameters?.Count);
    }
}
