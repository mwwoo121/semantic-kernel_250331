// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.VectorData.ProviderServices;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Oracle.Connectors.Common;

internal sealed class OracleDbClient : IDisposable
{
    #region static Fields
    internal const string VECTOR_FLEX = "*";
    internal const string VECTOR_INT8 = "INT8";
    internal const string VECTOR_FLOAT32 = "FLOAT32";
    internal const string VECTOR_FLOAT64 = "FLOAT64";
    internal const string VECTOR_BINARY = "BINARY";

    internal const int VECTOR_MIN_DIMENSIONS = 0;
    internal const int VECTOR_FLOAT32_MAX_DIMENSIONS = 65535;
    #endregion static Fields

    #region instant fields
    //Database Source
    private readonly OracleConnectionDispenser _connDispener;
    private readonly OracleDatabaseInfo _databaseInfo;
    private readonly bool _bDisposeDataSource;

    #endregion instant fields

#if NET8_0_OR_GREATER
    internal OracleDbClient(OracleDataSource dataSource, bool bOwnDataSource)
    {
        OracleConnection? conn = null;
        try
        {
            this._bDisposeDataSource = bOwnDataSource;

            //Gets database character set and identifier length of a connection.  This is only done once per OracleDbClient construction
            conn = dataSource.OpenConnection();

            this._databaseInfo = new()
            {
                DbCharacterSet = conn.DatabaseCharset,
                MaxIdentifierLength = conn.DatabaseMaxIdentifierLength,
                DatabaseName = conn.DatabaseName
            };

            this._connDispener = new OracleConnectionDispenser(dataSource);
        }
        finally
        {
            conn?.Dispose();
        }
    }

#endif

#if NET8_0_OR_GREATER
    internal OracleDbClient(string connectionString, bool bDisposeDataSource)
    {
        OracleConnection? conn = null;
        try
        {
            this._bDisposeDataSource = bDisposeDataSource;

            //Gets database character set and identifier length of a connection.  This is only done once per OracleDbClient construction
            OracleDataSource dataSource = new OracleDataSourceBuilder(connectionString).Build();

            //Gets database character set and identifier length of a connection.  This is only done once per OracleDbClient construction
            conn = dataSource.OpenConnection();

            this._databaseInfo = new()
            {
                DbCharacterSet = conn.DatabaseCharset,
                MaxIdentifierLength = conn.DatabaseMaxIdentifierLength,
                DatabaseName = conn.DatabaseName
            };

            this._connDispener = new OracleConnectionDispenser(dataSource);
        }
        finally
        {
            conn?.Dispose();
        }
    }
#else
    internal OracleDbClient(string connectionString)
    {
        OracleConnection? conn = null;
        try
        {
            //Gets database character set and identifier length of a connection.  This is only done once per OracleDbClient construction
            conn = new OracleConnection(connectionString);
            conn.Open();

            this._databaseInfo = new()
            {
                DbCharacterSet = conn.DatabaseCharset,
                MaxIdentifierLength = conn.DatabaseMaxIdentifierLength,
                DatabaseName = conn.DatabaseName
            };
        }
        finally
        {
            conn?.Dispose();
        }

        this._connDispener = new OracleConnectionConnStrDispenser(connectionString);
    }
#endif

#if NET8_0_OR_GREATER
    internal OracleDataSource DataSource => this._connDispener.DataSource;
#endif

    internal string? DatabaseName => this._databaseInfo.DatabaseName;

    internal OracleDatabaseInfo DatabaseInfo => this._databaseInfo;

    internal OracleDbClient Share()
    {
        this._connDispener.Share();

        return this;
    }

    internal async Task<bool> IsTableExistAsync(string qualifiedTableName, CancellationToken cancellationToken)
    {
        OracleSqlCommandInfo cmdInfo = new() { SqlText = $"SELECT 1 FROM {qualifiedTableName}" };

         using (OracleConnection conn = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            using (OracleCommand cmd = cmdInfo.ToOracleCommand(conn))
            {
                try
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OracleException oraEx)
                {
                    if (oraEx.Number == 942)
                    {
                        return false;
                    }

                    throw;
                }

                return true;
            }
        }
    }

    internal async Task<bool> IsTableExistAsync(string tableName, string? schemaName, CancellationToken cancellationToken)
    {
        OracleSqlCommandInfo cmdInfo = OracleCommandGenerator.BuildIsTableExistCommand(tableName, schemaName);
        using (OracleConnection conn = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            //TODO_Martha:  Checking with Jiacheng on the SQL
            using (OracleCommand cmd = cmdInfo.ToOracleCommand(conn))
            {
                try
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OracleException oraEx)
                {
                    if (oraEx.Number == 942)
                    {
                        return false;
                    }

                    throw;
                }

                return true;
            }
        }
    }

    internal async IAsyncEnumerable<string> GetTablesAsync(string? schemaName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        OracleSqlCommandInfo cmdInfo = OracleCommandGenerator.BuildListTablesCommand(schemaName);
        using (OracleConnection conn = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            using (OracleCommand cmd = cmdInfo.ToOracleCommand(conn))
            {
                using (OracleDataReader dataReader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        yield return dataReader.GetString(0);
                    }
                }
            }
        }
    }

    private async Task CreateVectorIndexAsync(OracleConnection conn, string internalTableName, string embeddingColName, OracleInternalVectorIndexType vectorIndex, CancellationToken cancellationToken)
    {
        using (OracleCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = vectorIndex.GenerateSQL(internalTableName, embeddingColName).SqlText;

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task CreateTableAsync(OracleDataModelMetadata metadata, bool bIfNotExists, CancellationToken cancellationToken)
    {
        OracleSqlCommandInfo cmdInfo = OracleCommandGenerator.BuildCreateTableCommand(metadata, bIfNotExists);

        using (OracleConnection conn = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            await this.ExecuteNonQueryAsync(cmdInfo, cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task DeleteTableAsync(string qualifiedTableName, CancellationToken cancellationToken)
    {
        OracleSqlCommandInfo sqlCmdInfo = OracleCommandGenerator.BuildDropTableCommand(qualifiedTableName);
        await this.ExecuteNonQueryAsync(sqlCmdInfo, cancellationToken).ConfigureAwait(false);
    }

    internal async Task AddAsync(
      OracleDataModelMetadata metadata,
      List<Dictionary<string, object?>> rows,
      CancellationToken cancellationToken,
      bool bUpsert = true)
    {
        int itemCount = rows.Count;
        if (itemCount <= 0)
        {
            return;
        }

        OracleSqlCommandInfo sqlCmdInfo = OracleCommandGenerator.BuildUpsertCommand(metadata, rows, bUpsert);
        await this.ExecuteNonQueryAsync(sqlCmdInfo, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<bool> DeleteAsync(
        OracleDataModelMetadata metadata,
        int itemCount,
        List<(string, object[])> keys,
        CancellationToken cancellationToken)
    {
        if (keys.Count == 0) { return false; }

        OracleSqlCommandInfo sqlCmdInfo = OracleCommandGenerator.BuildDeleteByKeysCommand(metadata, itemCount, keys);
        await this.ExecuteNonQueryAsync(sqlCmdInfo, cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<Dictionary<string, object?>?> GetAsync(
    OracleDataModelMetadata metadata,
    int itemCount,
    (string, object[]) key,
    bool bIncludeVectors = false,
    CancellationToken cancellationToken = default)
    {
        OracleSqlCommandInfo sqlCmdInfo = OracleCommandGenerator.BuildGetByKeysCommand(
            metadata, bIncludeVectors, itemCount, [key]);

        using (OracleConnection connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            using (OracleDataReader reader = await sqlCmdInfo.ToOracleCommand(connection).ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return this.GetRow(metadata, reader, bIncludeVectors);
                }
            }
            return null;
        }
    }

    public async IAsyncEnumerable<Dictionary<string, object?>> GetBatchAsync(
        OracleDataModelMetadata metadata,
        int itemCount,
        List<(string, object[])> keys,
        bool bIncludeVectors = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        OracleSqlCommandInfo sqlCmdInfo = OracleCommandGenerator.BuildGetByKeysCommand(
            metadata, bIncludeVectors, itemCount, keys);

        using (OracleConnection connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            using (OracleDataReader reader = await sqlCmdInfo.ToOracleCommand(connection).ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return (this.GetRow(metadata, reader, bIncludeVectors));
                }
            }
        }
    }

    internal async IAsyncEnumerable<Dictionary<string, object?>> GetByFilterAsync(OracleDataModelMetadata metadata,
        int top, int skip, bool bIncludeVector, OracleSqlFilterTranslator? filter, OracleSqlOrderByTranslator? orderBy, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        OracleSqlCommandInfo sqlCmdInfo = OracleCommandGenerator.BuildGetByFilterCommand(metadata, top, skip, bIncludeVector, filter, orderBy);

        using (OracleConnection connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            using (OracleDataReader reader = await sqlCmdInfo.ToOracleCommand(connection).ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return this.GetRow(metadata, reader, bIncludeVector);
                }
            }
        }
    }


    internal async IAsyncEnumerable<(Dictionary<string, object?> Row, double Distance)> SearchAsync(OracleDataModelMetadata metadata, string vectorColName, string distanceStrategy, bool bASC,
        OracleVector vector, int top, int skip, bool bIncludeVector, OracleSqlFilterTranslator? filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        OracleSqlCommandInfo sqlCmdInfo = OracleCommandGenerator.BuildSearchCommand(metadata, vectorColName, distanceStrategy, vector, bASC, top, skip, bIncludeVector, filter);

        using (OracleConnection connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            using (OracleDataReader reader = await sqlCmdInfo.ToOracleCommand(connection).ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var distance = reader.GetDouble(reader.GetOrdinal(vectorColName));
                    yield return (Row: this.GetRow(metadata, reader, bIncludeVector), Distance:distance);
                }
            }
        }
    }

    public void Dispose()
    {
        this._connDispener.Dispose();
    }

    #region Helper methods
    internal async Task<bool> IsEmptyAsync(string internalTableName, CancellationToken cancellationToken)
    {
        using (OracleConnection conn = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT EXISTS (SELECT 1 FROM {internalTableName})";

                bool bExist = false;
                bExist = (bool)await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                return !bExist;
            }
        }
    }

    private async Task ExecuteNonQueryAsync(OracleSqlCommandInfo commandInfo, CancellationToken cancellationToken)
    {
        using (OracleConnection connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
        {
            using (OracleCommand cmd = commandInfo.ToOracleCommand(connection))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static object? GetColumnValue(OracleDataReader reader, string columnName, int ordinal, OracleDbType oraDbType)
    {
        try
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            switch (oraDbType)
            {
                case OracleDbType.BinaryDouble:
                case OracleDbType.Double:
                    return reader.GetDouble(ordinal);
                case OracleDbType.BinaryFloat:
                case OracleDbType.Single:
                    return reader.GetFloat(ordinal);
                case OracleDbType.Blob:
                case OracleDbType.Raw:
                case OracleDbType.LongRaw:
                    return reader.GetValue(ordinal);
                case OracleDbType.Boolean:
                    return reader.GetBoolean(ordinal);
                case OracleDbType.Byte:
                    return reader.GetByte(ordinal);
                case OracleDbType.Char:
                case OracleDbType.Clob:
                case OracleDbType.Json:
                case OracleDbType.Long:
                case OracleDbType.NChar:
                case OracleDbType.NClob:
                case OracleDbType.NVarchar2:
                case OracleDbType.Varchar2:
                case OracleDbType.IntervalYM: //TODO_Martha: Need to check if the default mapping for IntervalYM is GetString()
                    return reader.GetString(ordinal);
                case OracleDbType.Date:
                case OracleDbType.TimeStamp:
                case OracleDbType.TimeStampLTZ:
                    return reader.GetDateTime(ordinal);
                case OracleDbType.Decimal:
                    return reader.GetDecimal(ordinal);
                case OracleDbType.Int16:
                    return reader.GetInt16(ordinal);
                case OracleDbType.Int32:
                    return reader.GetInt32(ordinal);
                case OracleDbType.Int64:
                    return reader.GetInt64(ordinal);
                case OracleDbType.IntervalDS:
                    return reader.GetTimeSpan(ordinal);
                case OracleDbType.TimeStampTZ:
                    return reader.GetDateTimeOffset(ordinal);
                case OracleDbType.Vector:
                case OracleDbType.Vector_Float32:
                case OracleDbType.Vector_Binary:
                case OracleDbType.Vector_Float64:
                case OracleDbType.Vector_Int8:
                    return reader.GetOracleVector(ordinal);
                default:
                    return reader.GetValue(ordinal);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read from DataReader '{columnName}' of type '{oraDbType}'.", ex);
        }
    }

    private async Task<OracleConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await this._connDispener.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    }

    private Dictionary<string, object?> GetRow(
    OracleDataModelMetadata metadata,
    OracleDataReader reader,
    bool includeVectors = false
)
    {
        var row = new Dictionary<string, object?>();

        foreach (KeyValuePair<string, OracleColumnInfo> column in metadata.AllColumnsByDbObjName)
        {
            int ordinal = reader.GetOrdinal(column.Key);
            row.Add(column.Key, GetColumnValue(reader, column.Key, ordinal, column.Value.OraDbType));
        }

        return row;
    }
    #endregion Helper methods

}
