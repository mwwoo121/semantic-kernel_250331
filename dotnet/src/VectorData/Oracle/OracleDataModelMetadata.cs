// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Oracle.ManagedDataAccess.Client;

namespace Oracle.Connectors.Common;

internal class OracleDatabaseInfo
{
    //Database Character set
    internal required OracleDatabaseCharset DbCharacterSet { get; init; }

    //Maximum database identifier length
    internal required int MaxIdentifierLength { get; init; }

    internal string? DatabaseName { get; init; }
}

internal enum OracleColumnType
{
    Key,
    Data,
    Vector
}

//This class will be expanded as needed.
internal class OracleColumnInfo
{
    internal required string Name { get; init; }
    internal required string ColumnTypeName { get; init; }
    internal required OracleColumnType ColumnType { get; init; }
    internal required OracleDbType OraDbType { get; init; }
    internal bool IsNullable { get; init; } = true;
    internal bool IsIndexed { get; init; }
    internal bool IsFullTextIndexed { get; init; }
}

internal class OracleVectorColumnInfo : OracleColumnInfo
{
    internal required int Dimensions { get; init; }
    internal required string DistanceStrategy { get; init; }
    internal IEmbeddingGenerator? EmbeddingGenerator { get; init; }
    internal OracleInternalVectorIndexType? VectorIndex { get; set; }
}

internal abstract class OracleDataModelMetadata
{
    internal required string ApplicationCollectionName { get; init; }

    internal required string TableName { get; init; }

    internal required string? SchemaName { get; init; }

    internal required string QualifiedTableName { get; init; }

    internal required Dictionary<string, OracleColumnInfo> AllColumnsByDbObjName { get; init; }

    internal required Dictionary<string, OracleColumnInfo> AllColumnsByDataPropertyName { get; init; }

    internal required Dictionary<string, OracleColumnInfo> PrimaryKeyColumnsByDbObjName { get; init; }

    internal required Dictionary<string, OracleVectorColumnInfo> VectorColumnsByDbObjName { get; init; }

    internal required Dictionary<string, OracleColumnInfo> DataColumns { get; init; }

    //Mappings between the Data Model Property name to Data Model Storage name
    internal required Dictionary<string, string> DataPropertyNameToDataStorageNameMappings { get; init; }

    //Mappings between the Data Model Property name to Oracle Database Object name
    internal required Dictionary<string, string> DataPropertyNameToDbObjNameMappings { get; init; }

    //Mappings between the Data Model Storage name to Oracle Database Object name
    internal required Dictionary<string, string> DataStorageNameToDbObjNameMappings { get; init; }

    //Mappings between the Oracle Database object name to Data Model Storage name
    internal required Dictionary<string, string> DbObjNameToDataStorageNameMappings { get; init; }

    internal required OracleDatabaseInfo DatabaseInfo { get; init; }
}
