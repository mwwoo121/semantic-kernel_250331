// Copyright (c) Microsoft. All rights reserved.
using System;
using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData.ProviderServices;
using Oracle.Connectors.Common;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace SemanticKernel.Connectors.Oracle;

/// <summary>
/// A mapper class that handles the conversion between data models and storage models for Postgres vector store.
/// </summary>
/// <typeparam name="TRecord">The type of the data model record.</typeparam>
internal sealed class OracleMapper<TRecord>(CollectionModel model, OracleSKMetadata metadata)
    where TRecord : notnull
{
    public Dictionary<string, object?> MapFromDataToStorageModel(TRecord dataModel, int recordIndex, IReadOnlyList<Embedding>?[]? generatedEmbeddings)
    {
        var properties = new Dictionary<string, object?>();

        foreach (var property in model.KeyProperties)
        {
            properties.Add(metadata.DataStorageNameToDbObjNameMappings[property.StorageName], property.GetValueAsObject(dataModel));
        };

        foreach (var property in model.DataProperties)
        {
            properties.Add(metadata.DataStorageNameToDbObjNameMappings[property.StorageName], property.GetValueAsObject(dataModel));
        }

        for (var i = 0; i < model.VectorProperties.Count; i++)
        {
            var property = model.VectorProperties[i];

            string columnName = metadata.DataStorageNameToDbObjNameMappings[property.StorageName];
            OracleVectorColumnInfo vectorColumn = metadata.VectorColumnsByDbObjName[columnName];

            properties.Add(columnName, OraclePropertyMapping.MapVectorFromDataModelToStorage(property.GetValueAsObject(dataModel!)!));

            //try
            //{
            //    switch (vectorColumn.OraDbType)
            //    {
            //        case OracleDbType.Vector_Float32:
            //        case OracleDbType.Vector:
            //        {
            //            vector = new OracleVector((float[])property.GetValueAsObject(dataModel!)!);
            //            properties.Add(columnName, OraclePropertyMapping.MapVectorForStorageModel(vector));
            //            break;
            //        }
            //        case OracleDbType.Vector_Float64:
            //        {
            //            vector = new OracleVector((double[])property.GetValueAsObject(dataModel!)!);
            //            properties.Add(columnName, OraclePropertyMapping.MapVectorForStorageModel(vector));
            //            break;
            //        }
            //        case OracleDbType.Vector_Int8:
            //        {
            //            vector = new OracleVector((short[])property.GetValueAsObject(dataModel!)!);
            //            properties.Add(columnName, OraclePropertyMapping.MapVectorForStorageModel(vector));
            //            break;
            //        }
            //        case OracleDbType.Vector_Binary:
            //        {
            //            vector = new OracleVector((byte[])property.GetValueAsObject(dataModel!)!);
            //            properties.Add(columnName, OraclePropertyMapping.MapVectorForStorageModel(vector));
            //            break;
            //        }
            //    }
            //}
            //finally { vector?.Dispose(); }

            //properties.Add(
            //    property.StorageName,
            //    OracleVectorStoreRecordPropertyMapping.MapVectorForStorageModel(
            //        generatedEmbeddings?[i] is IReadOnlyList<Embedding> e
            //            ? e[recordIndex] switch
            //            {
            //                Embedding<float> fe => fe.Vector,
            //                _ => throw new UnreachableException()
            //            }
            //            : (ReadOnlyMemory<float>?)property.GetValueAsObject(dataModel!)!));
        }

        return properties;
    }

    public TRecord MapFromStorageToDataModel(Dictionary<string, object?> storageModel, bool bIncludeVectors)
    {
        var record = model.CreateRecord<TRecord>()!;

        foreach (var keyProperty in model.KeyProperties)
        {
            keyProperty.SetValueAsObject(record, storageModel[metadata.DataStorageNameToDbObjNameMappings[keyProperty.StorageName]]);
            //TODO: Postgress needs to convert the value to the type.  Not sure why it needs.
            //var keyPropertyValue = Convert.ChangeType(storageModel[keyProperty.StorageName], keyProperty.Type);
            //keyProperty.SetValueAsObject(record, keyPropertyValue)
        }

        foreach (var dataProperty in model.DataProperties)
        {
            dataProperty.SetValueAsObject(record, storageModel[metadata.DataStorageNameToDbObjNameMappings[dataProperty.StorageName]]);
        }

        if (bIncludeVectors)
        {
            foreach (var vectorProperty in model.VectorProperties)
            {
                string columnName = metadata.DataStorageNameToDbObjNameMappings[vectorProperty.ModelName];
                OracleVectorColumnInfo vectorColumn = metadata.VectorColumnsByDbObjName[columnName];
                OracleVector vector = (OracleVector)storageModel[metadata.DataStorageNameToDbObjNameMappings[vectorProperty.ModelName]]!;

                vectorProperty.SetValueAsObject(record, OraclePropertyMapping.MapVectorFromStorageToDataModel(vectorProperty, vector));
            }
        }

        return record;
    }
    private static void PopulateValue(OracleDataModelMetadata metadata, OracleDataReader reader, PropertyModel property, object record)
    {
        try
        {
            var ordinal = reader.GetOrdinal(metadata.DataPropertyNameToDataStorageNameMappings[property.StorageName]);

            if (reader.IsDBNull(ordinal))
            {
                property.SetValueAsObject(record, null);
                return;
            }

            OracleDbType oraDbType = metadata.AllColumnsByDbObjName[metadata.DataPropertyNameToDataStorageNameMappings[property.StorageName]].OraDbType;
            switch (oraDbType)
            {
                case OracleDbType.BinaryDouble:
                case OracleDbType.Double:
                    property.SetValueAsObject(record, reader.GetDouble(ordinal));
                    break;
                case OracleDbType.BinaryFloat:
                case OracleDbType.Single:
                    property.SetValueAsObject(record, reader.GetFloat(ordinal));
                    break;
                case OracleDbType.Blob:
                case OracleDbType.Raw:
                case OracleDbType.LongRaw:
                    property.SetValueAsObject(record, reader.GetValue(ordinal));
                    break;
                case OracleDbType.Boolean:
                    property.SetValueAsObject(record, reader.GetBoolean(ordinal));
                    break;
                case OracleDbType.Byte:
                    property.SetValueAsObject(record, reader.GetByte(ordinal));
                    break;
                case OracleDbType.Char:
                case OracleDbType.Clob:
                case OracleDbType.Json:
                case OracleDbType.Long:
                case OracleDbType.NChar:
                case OracleDbType.NClob:
                case OracleDbType.NVarchar2:
                case OracleDbType.Varchar2:
                case OracleDbType.IntervalYM: //TODO_Martha: Need to check if the default mapping for IntervalYM is GetString()
                    property.SetValueAsObject(record, reader.GetString(ordinal));
                    break;
                case OracleDbType.Date:
                case OracleDbType.TimeStamp:
                case OracleDbType.TimeStampLTZ:
                    property.SetValueAsObject(record, reader.GetDateTime(ordinal));
                    break;
                case OracleDbType.Decimal:
                    property.SetValueAsObject(record, reader.GetDecimal(ordinal));
                    break;
                case OracleDbType.Int16:
                    property.SetValueAsObject(record, reader.GetInt16(ordinal));
                    break;
                case OracleDbType.Int32:
                    property.SetValueAsObject(record, reader.GetInt32(ordinal));
                    break;
                case OracleDbType.Int64:
                    property.SetValueAsObject(record, reader.GetInt64(ordinal));
                    break;
                case OracleDbType.IntervalDS:
                    property.SetValueAsObject(record, reader.GetTimeSpan(ordinal));
                    break;
                case OracleDbType.TimeStampTZ:
                    property.SetValueAsObject(record, reader.GetDateTimeOffset(ordinal));
                    break;
                case OracleDbType.Vector:
                case OracleDbType.Vector_Float32:
                case OracleDbType.Vector_Binary:
                case OracleDbType.Vector_Float64:
                case OracleDbType.Vector_Int8:
                    OracleVector vector = reader.GetOracleVector(ordinal);
                    OraclePropertyMapping.MapVectorFromStorageToDataModel((VectorPropertyModel)property, vector);
                    break;
                default:
                    property.SetValueAsObject(record, reader.GetValue(ordinal));
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read property '{property.ModelName}' of type '{property.Type.Name}'.", ex);
        }
    }

    public TRecord MapFromStorageToDataModel(OracleDataModelMetadata metadata, OracleDataReader reader, bool bIncludeVectors)
    {
        var record = model.CreateRecord<TRecord>()!;

        foreach (var keyProperty in model.KeyProperties)
        {
            PopulateValue(metadata, reader, keyProperty, record);
        }

        foreach (var dataProperty in model.DataProperties)
        {
            PopulateValue(metadata, reader, dataProperty, record);
        }

        if (bIncludeVectors)
        {
            foreach (var vectorProperty in model.VectorProperties)
            {
                PopulateValue(metadata, reader, vectorProperty, record);
            }
        }

        return record;
    }
}
