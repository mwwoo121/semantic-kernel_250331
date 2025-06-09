// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Oracle.Connectors.Common;
internal static class OracleCommandGenerator
{
    private const string IF_NOT_EXISTS = "IF NOT EXISTS";
    internal static readonly string[] sourceArray = ["d.", "s."];

    //<summary>
    //
    internal static OracleSqlCommandInfo BuildCreateTableCommand(OracleDataModelMetadata tableMetadata, bool bIfNotExist)
    {
        StringBuilder sqlBlr = new($"CREATE TABLE {(bIfNotExist ? IF_NOT_EXISTS : string.Empty)}{tableMetadata.QualifiedTableName} ( ");

        foreach (KeyValuePair<string, OracleColumnInfo> column in tableMetadata.AllColumnsByDbObjName)
        {
            sqlBlr.Append($"{column.Value.Name} {column.Value.ColumnTypeName}, ");
        }

        // Primary key
        if (tableMetadata.PrimaryKeyColumnsByDbObjName.Count > 0)
        {
            sqlBlr.Append("PRIMARY KEY (");
#if NETCOREAPP2_0_OR_GREATER
            sqlBlr.AppendJoin(", ", tableMetadata.PrimaryKeyColumnsByDbObjName.Keys);
#else
            sqlBlr.Append(string.Join(", ", tableMetadata.PrimaryColumns.Keys));
#endif
            sqlBlr.Append(" )");
        }
        else
        {
            sqlBlr.Length -= 2;  // remove the extra comma and space
        }

        sqlBlr.Append(" )");

        return new OracleSqlCommandInfo()
        {
            SqlText = sqlBlr.ToString()
        };
    }

    internal static IEnumerable<OracleSqlCommandInfo> BuildCreateDataIndexCommands(OracleDataModelMetadata tableMetadata, bool bIfNotExist)
    {
        foreach (var column in tableMetadata.DataColumns)
        {
            if (column.Value.IsIndexed)
            {
                yield return new OracleSqlCommandInfo()
                {
                    SqlText = $"CREATE INDEX {(bIfNotExist ? IF_NOT_EXISTS : string.Empty)} {tableMetadata.SchemaName}_{tableMetadata.TableName}_{column.Key}_index ON {tableMetadata.QualifiedTableName} ({column.Key})"
                };
            }
        }
    }

    /* TODO_Jiacheng, I added the GenerateSQL in the vector Index type classes;
     * Would you add the generate SQL implementation in the Generate SQL of the IVF and HNSW index class 
     * My layer can call the GenerateSQL() to generate the SQL when the vector index exists */
    //internal static IEnumerable<OracleSqlCommandInfo> BuildCreateVectorIndexCommands(OracleDataModelMetadata tableMetadata)
    //{
    //    StringBuilder sqlBlr = new();
    //    foreach (var vectorColumnInfo in tableMetadata.VectorColumns.Values)
    //    {
    //        sqlBlr.Clear();

    //        string sqlStr = vectorColumnInfo.IndexType switch
    //        {
    //            OracleInternalHNSWVectorIndex hnswVectorIndex => CreateHNSWIndexSQL(tableMetadata.TableName, vectorColumnInfo.Name, hnswVectorIndex, vectorColumnInfo.DistanceStrategy),
    //            OracleInternalIVFVectorIndex ivfVectorIndex => CreateIVFIndexSQL(tableMetadata.TableName, vectorColumnInfo.Name, ivfVectorIndex, vectorColumnInfo.DistanceStrategy),
    //            _ => ""
    //        };

    //        yield return new OracleSqlCommandInfo()
    //        {
    //            SqlText = sqlStr
    //        };
    //    }
    //}

    internal static OracleSqlCommandInfo BuildDropTableCommand(string qualifiedTableName)
    {
        return new OracleSqlCommandInfo()
        {
            SqlText = $"DROP TABLE IF EXISTS {qualifiedTableName}"
        };
    }

    internal static OracleSqlCommandInfo BuildListTablesCommand(string? schemaName)
    {
        List<OracleParameter>? parameters = null;
        string fromTable = (schemaName == null) ? "USER_TABLES" : "ALL_TABLES WHERE OWNER = :schema";

        if (schemaName == null)
        {
            parameters ??= [];
            parameters.Add(new OracleParameter(":schema", OracleDbType.Varchar2, schemaName, ParameterDirection.Input));
        }

        return new OracleSqlCommandInfo()
        {
            SqlText = $"SELECT TABLE_NAME FROM {fromTable}",
            Parameters = parameters
        };
    }

    internal static OracleSqlCommandInfo BuildIsTableExistCommand(string tableName, string? schemaName)
    {
        string sqlStr;
        var parameters = new List<OracleParameter>();

        if (schemaName == null)
        {
            sqlStr = "SELECT TABLE_NAME FROM USER_TABLES WHERE TABLE_NAME = :tableName";
            parameters.Add(new OracleParameter(":tableName", OracleDbType.Varchar2, tableName, ParameterDirection.Input));
        }
        else
        {
            sqlStr = "SELECT TABLE_NAME FROM ALL_TABLES WHERE TABLE_NAME = :tableName AND OWNER = :schema";
            parameters.Add(new OracleParameter(":tableName", OracleDbType.Varchar2, tableName, ParameterDirection.Input));
            parameters.Add(new OracleParameter(":schema", OracleDbType.Varchar2, schemaName, ParameterDirection.Input));
        }

        return new OracleSqlCommandInfo()
        {
            SqlText = sqlStr,
            Parameters = parameters
        };
    }

    internal static OracleSqlCommandInfo BuildIsTableEmptyCommand(string QualifiedTableName)
    {
        return new OracleSqlCommandInfo()
        {
            SqlText = $"SELECT EXISTS (SELECT 1 FROM {QualifiedTableName})",
        };
    }

    internal static OracleSqlCommandInfo BuildGetByKeysCommand(OracleDataModelMetadata metadata, int itemCount, List<(string keyColName, object[] values)> compositeKeys)
    {
        if (compositeKeys == null || compositeKeys.Count == 0)
        {
            throw new ArgumentException("Composite keys cannot be null or empty", nameof(compositeKeys));
        }

        // Check if all arrays have the same length
        if (itemCount == 0 || !compositeKeys.All(key => key.values.Length == itemCount))
        {
            throw new ArgumentException("All value arrays cannot be empty and must have the same length", nameof(compositeKeys));
        }

        StringBuilder sqlBlr = new($"SELECT * FROM {metadata.TableName} WHERE ");
        List<OracleParameter> parameters = new(compositeKeys.Count);

        int index = 0;
        foreach ((var keyColName, var values) in compositeKeys)
        {
            sqlBlr.Append($"${keyColName} = :p{index} AND ");
            parameters.Add(new OracleParameter($"p{index}", metadata.PrimaryKeyColumnsByDbObjName[keyColName].OraDbType, ParameterDirection.Input)
            {
                Value = (itemCount > 1) ? values : values[0],
                ArrayBindStatus = (itemCount > 1) ? new OracleParameterStatus[itemCount] : null,
            });
            index++;
        }

        sqlBlr.Length -= 5;  // remove the extra " AND "

        return new OracleSqlCommandInfo()
        {
            SqlText = sqlBlr.ToString(),
            Parameters = parameters,
            ArrayBindCount = itemCount
        };
    }

    internal static OracleSqlCommandInfo BuildDeleteByKeysCommand(OracleDataModelMetadata metadata, int itemCount, List<(string keyColName, object[] values)> compositeKeys)
    {
        if (compositeKeys == null || compositeKeys.Count == 0)
        {
            throw new ArgumentException("Composite keys cannot be null or empty", nameof(compositeKeys));
        }

        // Check if all arrays have the same length
        if (itemCount == 0 || !compositeKeys.All(key => key.values.Length == itemCount))
        {
            throw new ArgumentException("All value arrays cannot be empty and must have the same length", nameof(compositeKeys));
        }

        StringBuilder sqlBlr = new($"DELETE FROM {metadata.TableName} WHERE ");
        List<OracleParameter> parameters = new(compositeKeys.Count);

        int index = 0;
        foreach ((var keyColName, var values) in compositeKeys)
        {
            sqlBlr.Append(keyColName).Append(" = :p").Append(index).Append(" AND ");
            parameters.Add(new OracleParameter($"p{index}", metadata.PrimaryKeyColumnsByDbObjName[keyColName].OraDbType, ParameterDirection.Input)
            {
                Value = (itemCount > 1) ? values : values[0],
                ArrayBindStatus = (itemCount > 1) ? new OracleParameterStatus[itemCount] : null,
            });
            index++;
        }

        return new OracleSqlCommandInfo()
        {
            SqlText = sqlBlr.ToString(),
            Parameters = parameters,
            ArrayBindCount = itemCount
        };
    }

    internal static OracleSqlCommandInfo BuildUpsertCommand(OracleDataModelMetadata metadata, List<Dictionary<string, object?>> rows, bool bUpsert = true)
    {
        //TODO_Jiacheng:  Please also handle the case when bUpsert = false.
        //When bUpsert = true, it is insert and update/merge
        //When bUpsert = false, it is only insert.
        if (rows == null || rows.Count == 0)
        {
            throw new ArgumentException("Rows cannot be null or empty", nameof(rows));
        }

        int itemCount = rows.Count;

        List<OracleParameter> parameters = new(metadata.AllColumnsByDbObjName.Count);

        StringBuilder sqlBlr = new($"MERGE INTO {metadata.QualifiedTableName} d USING ( SELECT ");  // d as in destination

        foreach ((var colName, var colInfo) in metadata.AllColumnsByDbObjName)
        {
            sqlBlr.AppendFormat(":{0} AS {0} , ", colName);
            parameters.Add(new OracleParameter($"{colName}", colInfo.OraDbType, ParameterDirection.Input));
        }
        sqlBlr.Length -= 2;             // Remove the extra ", "

        sqlBlr.Append(" FROM DUAL ) s ON ( ");  // s as in source
        sqlBlr.AppendJoin(" AND ", metadata.PrimaryKeyColumnsByDbObjName.Keys.Select(keyColName => $"d.{keyColName} = s.{keyColName}"));
        sqlBlr.Append(" ) WHEN MATCHED THEN UPDATE SET ");
        sqlBlr.AppendJoin(", ", metadata.AllColumnsByDbObjName.Keys.Select(colName => $"d.{colName} = s.{colName}"));
        sqlBlr.Append(" WHEN NOT MATCHED THEN INSERT ( ");
        sqlBlr.AppendJoin(", ", metadata.AllColumnsByDbObjName.Keys.Select(colName => $"d.{colName}"));
        sqlBlr.Append(" ) VAULES ( ");
        sqlBlr.AppendJoin(", ", metadata.AllColumnsByDbObjName.Keys.Select(colName => $"s.{colName}"));
        sqlBlr.Append(" )");

        foreach (var parameter in parameters)
        {
            if (itemCount > 1)  // Array binding
            {
                parameter.ArrayBindStatus = new OracleParameterStatus[itemCount];
                parameter.Value = rows.Select((row, index) =>
                {
                    var rowValue = row[parameter.ParameterName];
                    parameter.ArrayBindStatus[index] = rowValue is null ? OracleParameterStatus.NullInsert : OracleParameterStatus.Success;
                    return rowValue;
                }).ToArray();
            }
            else  // Normal binding
            {
                parameter.Value = rows[0][parameter.ParameterName];
            }
        }

        return new OracleSqlCommandInfo()
        {
            SqlText = sqlBlr.ToString(),
            Parameters = parameters,
            ArrayBindCount = itemCount,
        };
    }

    internal static OracleSqlCommandInfo BuildGetByFilterCommand(OracleDataModelMetadata metadata, int top, int skip, bool bIncludedVector, OracleSqlFilterTranslator? filterTranslator, OracleSqlOrderByTranslator? orderByTransalator)
    {
        List<OracleParameter>? parameters = null;

        int parameterIndex = 0;
        StringBuilder sqlBlr = new("SELECT ");
        if (bIncludedVector)
        {
            sqlBlr.AppendJoin(", ", metadata.AllColumnsByDbObjName.Keys);
        }
        else
        {
            sqlBlr.AppendJoin(", ", metadata.PrimaryKeyColumnsByDbObjName.Keys);
            sqlBlr.Append(", ");
            sqlBlr.AppendJoin(", ", metadata.DataColumns.Keys);
        }

        sqlBlr.Append($" FROM {metadata.QualifiedTableName} ");

        if (filterTranslator is not null)
        {
            OracleSqlCommandInfo filterCmdInfo = filterTranslator.GenerateSQL(metadata, parameterIndex);
            sqlBlr.Append(filterCmdInfo.SqlText);
            parameters = filterCmdInfo.Parameters;
        }

        if (orderByTransalator is not null)
        {
            OracleSqlCommandInfo orderCmdInfo = orderByTransalator.GenerateSQL(metadata);
            sqlBlr.AppendLine(orderCmdInfo.SqlText);
        }

        sqlBlr.Append($"OFFSET {skip} LIMIT {top}");

        return new OracleSqlCommandInfo()
        {
            SqlText = sqlBlr.ToString(),
            Parameters = parameters,
            ArrayBindCount = (parameters is not null) ? (parameters[0].Value.GetType().IsArray) ? ((Array)parameters[0].Value).Length : 0 : 0,
        };
    }

    internal static OracleSqlCommandInfo BuildSearchCommand(OracleDataModelMetadata metadata, string vectorColumnName, string vectorDistance,
        OracleVector vector, bool bVectorDistanceAsc, int top, int skip, bool bIncludedVector, OracleSqlFilterTranslator? filterTranslator)
    {
        List<OracleParameter>? parameters = new();

        StringBuilder sqlBlr = new("SELECT ");
        if (bIncludedVector)
        {
            sqlBlr.AppendJoin(", ", metadata.AllColumnsByDbObjName.Keys);
        }
        else
        {
            sqlBlr.AppendJoin(", ", metadata.PrimaryKeyColumnsByDbObjName.Keys);
            sqlBlr.Append(", ");
            sqlBlr.AppendJoin(", ", metadata.DataColumns.Keys);
        }
        sqlBlr.Append($" FROM {metadata.QualifiedTableName} ");

        if (filterTranslator is not null)
        {
            OracleSqlCommandInfo filterCmdInfo = filterTranslator.GenerateSQL(metadata, 1);
            sqlBlr.Append(filterCmdInfo.SqlText);
            if (filterCmdInfo.Parameters is not null)
            {
                parameters.AddRange(filterCmdInfo.Parameters);
            }
        }

        sqlBlr.Append(" ORDER BY VECTOR_DISTANCE ( ").Append(vectorColumnName).Append(" , :").Append(vectorColumnName)
            .Append(" , ").Append(metadata.VectorColumnsByDbObjName[vectorColumnName].DistanceStrategy);
        parameters.Add(new OracleParameter($":{vectorColumnName}", metadata.VectorColumnsByDbObjName[vectorColumnName].OraDbType, vector, ParameterDirection.Input));

        sqlBlr.Append(" OFFSET ").Append(skip).Append(" FETCH FIRST ").Append(top).Append(" ROWS ONLY");

        return new OracleSqlCommandInfo()
        {
            SqlText = sqlBlr.ToString(),
            Parameters = parameters
        };
    }

    internal static (string, bool, string?) ValidateAndConvertToQuotedDbObjectName(string inputName, string context)
    {
        bool bHasSchema = false;

        SplitTableString(inputName, out string? schemaName, out string? tableName, out string? dbLinkName);

        try
        {
            if (!string.IsNullOrWhiteSpace(schemaName))
            {
                schemaName = OracleDBMSAssert.EnquoteIdentifier(schemaName, true, false);
            }

            if (!string.IsNullOrWhiteSpace(tableName))
            {
                tableName = OracleDBMSAssert.EnquoteIdentifier(tableName, true, false);
            }

            if (!string.IsNullOrWhiteSpace(dbLinkName))
            {
                dbLinkName = OracleDBMSAssert.EnquoteIdentifier(dbLinkName, true, false);
            }
        }
        catch (OracleException oraEx)
        {
            throw new ArgumentException($"The {context} is invalid.  Provide a valid {context}", inputName, oraEx);
        }

        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException($"The {context} is invalid, either an empty string or consisting of white spaces only.  Provide a valid {context}", inputName);
        }

        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            bHasSchema = true;
        }

        if (!string.IsNullOrWhiteSpace(dbLinkName))
        {
            tableName = $"{tableName}@{dbLinkName}";
        }

        return (tableName, bHasSchema, schemaName);
    }

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
                        string splitString = inStr[lastSplitPos..currentPos];

                        splitByDot.Add(splitString);
                        lastSplitPos = currentPos + 1;
                    }

                    //Assumes that the DBLink is valid after the @ symbol
                    if (currentChar.Equals('@') && !atDiscovered)
                    {
                        atDiscovered = true;
                        string splitString = inStr[lastSplitPos..currentPos];
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
                string tempString = inStr[lastSplitPos..];
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

    //Splits string into schemaName,tableName,and columnName. If these aspects do not exist in the string, they will be output as NULL.
    private static void SplitColumnString(string inStr, out string? schemaName, out string? tableName, out string? columnName)
    {
        columnName = tableName = schemaName = null;
        int lastSplitPos = 0;
        char doubleQuote = '\"';

        if (inStr.Contains(doubleQuote, StringComparison.Ordinal))
        {
#pragma warning disable CA1508, CA1514
            bool withinDoubleQuotes = false;
            List<string> splitByDot = new();

            for (int currentPos = 0; currentPos < inStr.Length; currentPos++)
            {
                char currentChar = inStr[currentPos];

                if (currentChar.Equals(doubleQuote))
                {
                    withinDoubleQuotes = !withinDoubleQuotes;
                    continue;
                }

                if (withinDoubleQuotes == false)
                {
                    if (currentChar.Equals('.'))
                    {
                        string splitString = inStr.Substring(lastSplitPos, currentPos - lastSplitPos);

                        splitByDot.Add(splitString);
                        lastSplitPos = currentPos + 1;
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
                columnName = splitByDot[0];
            }
            else if (splitByDot.Count == 2)
            {
                tableName = splitByDot[0];
                columnName = splitByDot[1];
            }
            else if (splitByDot.Count == 3)
            {
                schemaName = splitByDot[0];
                tableName = splitByDot[1];
                columnName = splitByDot[2];
            }
        }
        else
        {
            string[] splitArray = inStr.Split('.');

            if (splitArray.Length == 1)
            {
                columnName = splitArray[0];
            }
            else if (splitArray.Length == 2)
            {
                columnName = splitArray[1];
                tableName = splitArray[0];
            }
            else if (splitArray.Length == 3)
            {
                columnName = splitArray[2];
                tableName = splitArray[1];
                schemaName = splitArray[0];
            }
        }
    }
}
