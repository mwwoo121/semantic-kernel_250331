// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;
using Microsoft.SemanticKernel;
using Oracle.Connectors.Common;
using Oracle.ManagedDataAccess.Client;

namespace SemanticKernel.Connectors.Oracle;

internal sealed class OracleSKMetadata : OracleDataModelMetadata
{
    /// <summary>The model for this collection.</summary>
    private readonly CollectionModel _model;

    /// <summary>
    /// Initializes a new instance of the Semantic Kernel Metadata class.
    /// </summary>
    /// <param name="model"> Semantic Kernel data model</param>
    /// <param name="databaseInfo">The database information.</param>
    /// <param name="tableName">The name of the collection.</param>
    /// <param name="schemaName">Schema name.  Null if it is the connected user.</param>
    /// <remarks>
    /// This constructor is internal. It allows internal code to create an instance of this class with a custom client.
    /// </remarks>
    ///
    ///
    [SetsRequiredMembers]
    public OracleSKMetadata(CollectionModel model, OracleDatabaseInfo databaseInfo, string tableName, string? schemaName)
    {
        // Verify.
        Verify.NotNull(databaseInfo);
        Verify.NotNullOrWhiteSpace(tableName);

        this._model = model;

        // Assign.
        this.DatabaseInfo = databaseInfo;

        this.TableName = this.QualifiedTableName = OracleDBMSAssert.EnquoteIdentifier(tableName, this.DatabaseInfo.MaxIdentifierLength, this.DatabaseInfo.DbCharacterSet, true, false);
        this.ApplicationCollectionName = tableName;
        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            this.SchemaName = OracleDBMSAssert.EnquoteIdentifier(schemaName, this.DatabaseInfo.MaxIdentifierLength, this.DatabaseInfo.DbCharacterSet, true, false);
            this.QualifiedTableName = $"{this.SchemaName}.{this.TableName}";
        }

        this._model = model;
        //this._model = new CollectionModelBuilder(OracleConstants.OracleModelBuildingOptions)
        //    .Build(typeof(TRecord), options?.VectorStoreRecordDefinition, options?.EmbeddingGenerator);

        // Primary key columns
        this.AllColumnsByDbObjName = new();
        this.AllColumnsByDataPropertyName = new();
        this.DataPropertyNameToDataStorageNameMappings = new();
        this.DataPropertyNameToDbObjNameMappings = new();
        this.DataStorageNameToDbObjNameMappings = new();
        this.DbObjNameToDataStorageNameMappings = new();
        OracleColumnInfo columnInfo;

        //Processing primary key columns
        this.PrimaryKeyColumnsByDbObjName = new();
        foreach (var keyProperty in this._model.KeyProperties)
        {
            string columnName = OracleDBMSAssert.EnquoteIdentifier(keyProperty.StorageName, this.DatabaseInfo.MaxIdentifierLength, this.DatabaseInfo.DbCharacterSet, true, false);
            var columnTypeName = OraclePropertyMapping.GetOracleTypeName(keyProperty.Type);
            OracleDbType oraDbType = OraclePropertyMapping.GetOracleDbType(keyProperty.Type);

            columnInfo = new()
            {
                Name = columnName,
                ColumnTypeName = columnTypeName.colName,
                ColumnType = OracleColumnType.Key,
                IsNullable = columnTypeName.bIsNullable,
                OraDbType = oraDbType
            };

            this.PrimaryKeyColumnsByDbObjName.Add(columnName, columnInfo);
            this.DataPropertyNameToDataStorageNameMappings.Add(keyProperty.ModelName, keyProperty.StorageName);
            this.DataPropertyNameToDbObjNameMappings.Add(keyProperty.ModelName, columnName);
            this.DataStorageNameToDbObjNameMappings.Add(keyProperty.StorageName, columnName);
            this.DbObjNameToDataStorageNameMappings.Add(columnName, keyProperty.StorageName);
            this.AllColumnsByDbObjName.Add(columnName, columnInfo);
            this.AllColumnsByDataPropertyName.Add(keyProperty.ModelName, columnInfo);
        }

        //Processing data columns
        this.DataColumns = new();
        foreach (var dataProperty in this._model.DataProperties)
        {
            string columnName = OracleDBMSAssert.EnquoteIdentifier(dataProperty.StorageName, this.DatabaseInfo.MaxIdentifierLength, this.DatabaseInfo.DbCharacterSet, true, false);
            var columnTypeName = OraclePropertyMapping.GetOracleTypeName(dataProperty.Type);
            OracleDbType oraDbType = OraclePropertyMapping.GetOracleDbType(dataProperty.Type);

            columnInfo = new()
            {
                Name = columnName,
                ColumnTypeName = columnTypeName.colName,
                IsNullable = columnTypeName.bIsNullable,
                ColumnType = OracleColumnType.Data,
                OraDbType = oraDbType,
                IsIndexed = dataProperty.IsIndexed,
                IsFullTextIndexed = dataProperty.IsFullTextIndexed
            };

            this.DataColumns.Add(columnName, columnInfo);
            this.DataPropertyNameToDataStorageNameMappings.Add(dataProperty.ModelName, dataProperty.StorageName);
            this.DataPropertyNameToDbObjNameMappings.Add(dataProperty.ModelName, columnName);
            this.DataStorageNameToDbObjNameMappings.Add(dataProperty.StorageName, columnName);
            this.DbObjNameToDataStorageNameMappings.Add(columnName, dataProperty.StorageName);
            this.AllColumnsByDbObjName.Add(columnName, columnInfo);
            this.AllColumnsByDataPropertyName.Add(dataProperty.ModelName, columnInfo);
        }

        //Processing vector columns
        this.VectorColumnsByDbObjName = new();
        foreach (var vectorProperty in this._model.VectorProperties)
        {
            string columnName = OracleDBMSAssert.EnquoteIdentifier(vectorProperty.StorageName, this.DatabaseInfo.MaxIdentifierLength, this.DatabaseInfo.DbCharacterSet, true, false);
            var columnTypeInfo = OraclePropertyMapping.GetOracleVectorTypeInfo(vectorProperty);

            OracleVectorColumnInfo vectorColumn = new()
            {
                Name = columnName,
                ColumnTypeName = columnTypeInfo.colName,
                ColumnType = OracleColumnType.Vector,
                IsNullable = columnTypeInfo.bIsNullable,
                OraDbType = columnTypeInfo.oraDbType,
                Dimensions = vectorProperty.Dimensions,
                DistanceStrategy = vectorProperty.DistanceFunction switch
                {
                    DistanceFunction.CosineDistance => CommonConstants.DISTANCE_STRATEGY_COSINE,
                    DistanceFunction.CosineSimilarity => CommonConstants.DISTANCE_STRATEGY_COSINE,
                    DistanceFunction.EuclideanDistance => CommonConstants.DISTANCE_STRATEGY_EUCLIDEAN,
                    DistanceFunction.EuclideanSquaredDistance => CommonConstants.DISTANCE_STRATEGY_EUCLIDEAN_SQUARED,
                    DistanceFunction.ManhattanDistance => CommonConstants.DISTANCE_STRATEGY_MANHATTAN,
                    DistanceFunction.DotProductSimilarity => CommonConstants.DISTANCE_STRATEGY_INNERPRODUCT,
                    DistanceFunction.HammingDistance => CommonConstants.DISTANCE_STRATEGY_HAMMING,
                    null or "" => CommonConstants.DISTANCE_STRATEGY_COSINE,
                    _ => vectorProperty.DistanceFunction //return the distance function so that the database might be able to support it in later releases.
                },
                EmbeddingGenerator = vectorProperty.EmbeddingGenerator,
            };

            switch (vectorProperty.IndexKind)
            {
                case IndexKind.Hnsw:
                {
                    vectorColumn.VectorIndex = new OracleInternalHNSWVectorIndex(
                        OracleDBMSAssert.EnquoteIdentifier($"{tableName}_{vectorProperty.StorageName}_index", this.DatabaseInfo.MaxIdentifierLength, this.DatabaseInfo.DbCharacterSet, true, false));
                    vectorColumn.VectorIndex.DistanceMetric = vectorColumn.DistanceStrategy;
                    break;
                }

                case IndexKind.IvfFlat:
                {
                    vectorColumn.VectorIndex = new OracleInternalIVFVectorIndex(
                        OracleDBMSAssert.EnquoteIdentifier($"{tableName}_{vectorProperty.StorageName}_index", this.DatabaseInfo.MaxIdentifierLength, this.DatabaseInfo.DbCharacterSet, true, false));
                    vectorColumn.VectorIndex.DistanceMetric = vectorColumn.DistanceStrategy;
                    break;
                }

            }

            this.VectorColumnsByDbObjName.Add(columnName, vectorColumn);
            this.DataPropertyNameToDataStorageNameMappings.Add(vectorProperty.ModelName, vectorProperty.StorageName);
            this.DataPropertyNameToDbObjNameMappings.Add(vectorProperty.ModelName, columnName);
            this.DataStorageNameToDbObjNameMappings.Add(vectorProperty.StorageName, columnName);
            this.DbObjNameToDataStorageNameMappings.Add(columnName, vectorProperty.StorageName);
            this.AllColumnsByDbObjName.Add(columnName, vectorColumn);
            this.AllColumnsByDataPropertyName.Add(vectorProperty.ModelName, vectorColumn);
        }
    }

    internal CollectionModel Model => this._model;

    //Splits string into schemaName, tablename,dbLink,if  sections do not exist in the string they will be output as null.
    //Ex. oe.employees@DBLINK, schemaName = "oe", tableName = "employees", dbLink = "DBLINK".
    //Ex. employees, schemaName = NULL, tableName = employees, dbLink = NULL;
    private static void SplitTableString(string inStr, out string? schema_Name, out string? table_Name, out string? dbLink)
    {
        schema_Name = null;

        table_Name = null;
        dbLink = null;
        int lastSplitPos = 0;

        char doubleQuote = '\"';

        if (inStr.Contains(doubleQuote, StringComparison.Ordinal))
        {
            bool withinDoubleQuotes = false;
            bool atDiscovered = false;
            List<string> splitByDot = new();

            for (int currentPos = 0; currentPos < inStr.Length; currentPos++)
            {
                char currentChar = inStr[currentPos];
#pragma warning disable CA1508, CA1514
                if (currentChar.Equals(doubleQuote))
                {
                    withinDoubleQuotes = !withinDoubleQuotes;
                    continue;
                }

                if (withinDoubleQuotes == false)
                {
                    if (currentChar.Equals('.') && !atDiscovered)
                    {
                        string splitString = inStr.Substring(lastSplitPos, currentPos - lastSplitPos);

                        splitByDot.Add(splitString);
                        lastSplitPos = currentPos + 1;
                    }

                    //Assumes that the DBLink is valid after the @ symbol
                    if (currentChar.Equals('@') && !atDiscovered)
                    {
                        atDiscovered = true;
                        string splitString = inStr.Substring(lastSplitPos, currentPos - lastSplitPos);
                        splitByDot.Add(splitString);
                        dbLink = inStr.Substring(currentPos + 1, inStr.Length - (currentPos + 1));
                        //After encountering a @ symbol there is no more need to split by "." anymore.
                        lastSplitPos = inStr.Length;
                    }
                }
            }

            //Checks and grabs rest of the string 
            if (lastSplitPos < inStr.Length)
            {
                string tempString = inStr.Substring(lastSplitPos, inStr.Length - lastSplitPos);
                splitByDot.Add(tempString);
            }
#pragma warning restore CA1508, CA1514

            if (splitByDot.Count == 1)
            {
                table_Name = splitByDot[0];
            }
            else if (splitByDot.Count == 2)
            {
                schema_Name = splitByDot[0];
                table_Name = splitByDot[1];
            }
        }
        else
        {
            char atSymbol = '@';

            if (inStr.Contains(atSymbol, StringComparison.Ordinal))
            {
                string[] splitByAt = inStr.Split('@', 2);
                dbLink = splitByAt[1];

                string[] splitByDot = splitByAt[0].Split('.');

                if (splitByDot.Length == 1)
                {
                    table_Name = splitByDot[0];
                }
                else if (splitByDot.Length == 2)
                {
                    table_Name = splitByDot[1];
                    schema_Name = splitByDot[0];
                }
            }
            else
            {
                string[] splitByDot = inStr.Split('.');

                if (splitByDot.Length == 1)
                {
                    table_Name = splitByDot[0];
                }
                else if (splitByDot.Length == 2)
                {
                    table_Name = splitByDot[1];
                    schema_Name = splitByDot[0];
                }
            }
        }
    }

}
