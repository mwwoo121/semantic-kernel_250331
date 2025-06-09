// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData.ProviderServices;
using Oracle.Connectors.Common;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace SemanticKernel.Connectors.Oracle;

internal static class OraclePropertyMapping
{
    //public static OracleVector MapVectorForStorageModel(object? vector)
    //{
    //    OracleVector v;
    //    OracleDbType vType;

    //    switch (vector)
    //    {
    //        case ReadOnlyMemory<float>:
    //        {
    //           v = new OracleVector(vector.ToArray());,
    //    Embedding<float> e => new OracleVector(e.Vector.ToArray()),
    //    float[] a => new OracleVector(a),

    //    ReadOnlyMemory<short> m => new OracleVector(m.ToArray()),
    //    Embedding<short> e => new OracleVector(e.Vector.ToArray()),
    //    float[] a => new OracleVector(a),

    //    BitArray bitArray => bitArray,
    //    SparseVector sparseVector => sparseVector,

    //    null => null,

    //    var value => throw new NotSupportedException($"Mapping for type '{value.GetType().Name}' to a vector is not supported.")
    //};

    public static OracleVector? MapVectorFromDataModelToStorage(object? vector)
    => vector switch
    {
        ReadOnlyMemory<float> m => new OracleVector(m.ToArray()),
        Embedding<float> e => new OracleVector(e.Vector.ToArray()),
        float[] a => new OracleVector(a),

        ReadOnlyMemory<double> m => new OracleVector(m.ToArray()),
        Embedding<double> e => new OracleVector(e.Vector.ToArray()),
        double[] a => new OracleVector(a),

        ReadOnlyMemory<short> m => new OracleVector(m.ToArray()),
        Embedding<short> e => new OracleVector(e.Vector.ToArray()),
        short[] a => new OracleVector(a),

        ReadOnlyMemory<byte> m => new OracleVector(m.ToArray()),
        Embedding<byte> e => new OracleVector(e.Vector.ToArray()),
        byte[] a => new OracleVector(a),

        null => null,

        var value => throw new NotSupportedException($"Mapping for type '{value.GetType().Name}' to a vector is not supported.")
    };

    /// <summary>
    /// Gets the .NET value for the Data Model property from OracleVector .
    /// </summary>
    /// <param name="vectorProperty">The vector property.</param>
    /// <param name="vector">The OracleVector value.</param>
    /// <returns>The Oracle vector type name and the Vector OracleDbType.</returns>
    public static object? MapVectorFromStorageToDataModel(VectorPropertyModel vectorProperty, OracleVector? vector)
    {
        if (vector == null || vector.IsNull)
        {
            return null;
        }

        var unwrappedEmbeddingType = Nullable.GetUnderlyingType(vectorProperty.EmbeddingType) ?? vectorProperty.EmbeddingType;

        object? vectorVal = unwrappedEmbeddingType switch
        {
            Type t when t == typeof(ReadOnlyMemory<short>) => (ReadOnlyMemory<short>)vector.ToInt16Array(),
            Type t when t == typeof(short[]) => vector.ToInt16Array(),
            Type t when t == typeof(Embedding<short>) => new Embedding<short>((ReadOnlyMemory<short>)vector.ToInt16Array()),

            Type t when t == typeof(ReadOnlyMemory<float>) => (ReadOnlyMemory<float>)vector.ToFloatArray(),
            Type t when t == typeof(float[]) => vector.ToFloatArray(),
            Type t when t == typeof(Embedding<float>) => new Embedding<float>((ReadOnlyMemory<float>)vector.ToFloatArray()),

            Type t when t == typeof(ReadOnlyMemory<double>) => (ReadOnlyMemory<double>)vector.ToDoubleArray(),
            Type t when t == typeof(double[]) => vector.ToDoubleArray(),
            Type t when t == typeof(Embedding<double>) => new Embedding<double>((ReadOnlyMemory<double>)vector.ToDoubleArray()),

            Type t when t == typeof(ReadOnlyMemory<byte>) => (ReadOnlyMemory<byte>)vector.ToByteArray(),
            Type t when t == typeof(byte[]) => vector.ToByteArray(),
            Type t when t == typeof(Embedding<byte>) => new Embedding<byte>((ReadOnlyMemory<byte>)vector.ToByteArray()),

            _ => throw new NotSupportedException($"Type {vectorProperty.ModelName} is not supported by this store.")
        };

        return vectorVal;
    }

    public static object? MapFromStorageToDataModel(OracleDataReader reader, OracleSKMetadata metadata, PropertyModel property)
    {
        //TODO: We might just need the OracleDataModelMetadata instead of the property name to select the reader accessor method
        //If we cannot use the OracleDataModelMetadata class, then we will have to check Postgress to update the latest Postgress implementation changes accordingly.
        //TODO: We need to check if double-quoted name works for getting index
        int propertyIndex = reader.GetOrdinal(metadata.DataPropertyNameToDataStorageNameMappings[property.StorageName]);

        if (reader.IsDBNull(propertyIndex))
        {
            return null;
        }

        return Nullable.GetUnderlyingType(property.Type) ?? property.Type switch
        {
            Type t when t == typeof(bool) => reader.GetBoolean(propertyIndex),
            Type t when t == typeof(byte) => reader.GetByte(propertyIndex),
            Type t when t == typeof(short) => reader.GetInt16(propertyIndex),
            Type t when t == typeof(int) => reader.GetInt32(propertyIndex),
            Type t when t == typeof(long) => reader.GetInt64(propertyIndex),
            Type t when t == typeof(float) => reader.GetFloat(propertyIndex),
            Type t when t == typeof(double) => reader.GetDouble(propertyIndex),
            Type t when t == typeof(decimal) => reader.GetDecimal(propertyIndex),
            Type t when t == typeof(string) => reader.GetString(propertyIndex),
            Type t when t == typeof(char) => reader.GetString(propertyIndex),
            Type t when t == typeof(char[]) => reader.GetString(propertyIndex),
            Type t when t == typeof(byte[]) => (byte[])reader.GetValue(propertyIndex),
            Type t when t == typeof(DateTime) => reader.GetDateTime(propertyIndex),
            Type t when t == typeof(DateTimeOffset) => reader.GetDateTimeOffset(propertyIndex),
            Type t when t == typeof(TimeSpan) => reader.GetTimeSpan(propertyIndex),
            Type t when t == typeof(Guid) => (byte[])reader.GetValue(propertyIndex),
            _ => reader.GetValue(propertyIndex)
        };
    }

    public static OracleDbType GetOracleDbType(Type propertyType) =>
        (Nullable.GetUnderlyingType(propertyType) ?? propertyType) switch
        {
            Type t when t == typeof(bool) => OracleDbType.Boolean,
            Type t when t == typeof(byte) => OracleDbType.Byte,
            Type t when t == typeof(short) => OracleDbType.Int16,
            Type t when t == typeof(int) => OracleDbType.Int32,
            Type t when t == typeof(long) => OracleDbType.Int64,
            Type t when t == typeof(float) => OracleDbType.BinaryFloat,
            Type t when t == typeof(double) => OracleDbType.BinaryDouble,
            Type t when t == typeof(decimal) => OracleDbType.Decimal,
            Type t when t == typeof(string) => OracleDbType.NVarchar2,
            Type t when t == typeof(char) => OracleDbType.NVarchar2,
            Type t when t == typeof(char[]) => OracleDbType.NVarchar2,
            Type t when t == typeof(byte[]) => OracleDbType.Raw,
            Type t when t == typeof(DateTime) => OracleDbType.TimeStamp,
            Type t when t == typeof(DateTimeOffset) => OracleDbType.TimeStampTZ,
            Type t when t == typeof(TimeSpan) => OracleDbType.IntervalDS,
            Type t when t == typeof(Guid) => OracleDbType.Raw,
            _ => throw new NotSupportedException($"Type {propertyType.Name} is not supported by this store.")
        };

    /// <summary>
    /// Maps a .NET type to a PostgreSQL type name.
    /// </summary>
    /// <param name="propertyType">The .NET type.</param>
    /// <returns>Tuple of the the PostgreSQL type name and whether it can be NULL</returns>
    public static (string colName, bool bIsNullable) GetOracleTypeName(Type propertyType)
    {
        static bool TryGetBaseType(Type type, [NotNullWhen(true)] out string? typeName)
        {
            typeName = type switch
            {
                Type t when t == typeof(bool) => "BOOLEAN",
                Type t when t == typeof(byte) => "NUMBER(3)",
                Type t when t == typeof(short) => "NUMBER(5)",
                Type t when t == typeof(int) => "NUMBER(10)",
                Type t when t == typeof(decimal) => "NUMBER(18,2)",
                Type t when t == typeof(long) => "NUMBER(19)",
                Type t when t == typeof(float) => "BINARY_FLOAT",
                Type t when t == typeof(double) => "BINARY_DOUBLE",
                Type t when t == typeof(string) => "NVARCHAR2(2000)",
                Type t when t == typeof(char) => "NVARCHAR2(1)",
                Type t when t == typeof(char[]) => "NVARCHAR2(2000)",
                Type t when t == typeof(byte[]) => "RAW(2000)",
                Type t when t == typeof(DateTime) => "TIMESTAMP(7)",
                Type t when t == typeof(DateTimeOffset) => "TIMESTAMP(7) WITH TIME ZONE",
                Type t when t == typeof(TimeSpan) => "INTERVAL DAY(8) TO SECOND(5)",
                Type t when t == typeof(Guid) => "RAW(16)",
                _ => null
            };

            return typeName is not null;
        }

        // TODO: Handle NRTs properly via NullabilityInfoContext

        if (TryGetBaseType(propertyType, out string? typeName))
        {
            return (typeName, !propertyType.IsValueType);
        }

        // Handle nullable types (e.g. Nullable<int>)
        if (Nullable.GetUnderlyingType(propertyType) is Type unwrappedType
            && TryGetBaseType(unwrappedType, out string? underlyingtypeName))
        {
            return (underlyingtypeName, true);
        }

        throw new NotSupportedException($"Type {propertyType.Name} is not supported by this store.");
    }

    /// <summary>
    /// Gets the Oracle vector type name based on the dimensions of the vector property.
    /// </summary>
    /// <param name="vectorProperty">The vector property.</param>
    /// <returns>The Oracle vector type name and the Vector OracleDbType.</returns>
    public static (string colName, OracleDbType oraDbType, bool bIsNullable) GetOracleVectorTypeInfo(VectorPropertyModel vectorProperty)
    {
        if (vectorProperty.Dimensions <= CommonConstants.VECTOR_MIN_DIMENSIONS || vectorProperty.Dimensions > CommonConstants.VECTOR_FLOAT32_MAX_DIMENSIONS)
        {
            throw new ArgumentException($"Dimension has a value of {vectorProperty.Dimensions}.  Dimension must be between {CommonConstants.VECTOR_MIN_DIMENSIONS} and {CommonConstants.VECTOR_FLOAT32_MAX_DIMENSIONS}.");
        }

        var unwrappedEmbeddingType = Nullable.GetUnderlyingType(vectorProperty.EmbeddingType) ?? vectorProperty.EmbeddingType;

        var vectorCol = unwrappedEmbeddingType switch
        {
            Type t when t == typeof(ReadOnlyMemory<short>) || t == typeof(short[]) || t == typeof(Embedding<short>)
                => ($"VECTOR({vectorProperty.Dimensions}, {CommonConstants.VECTOR_INT8})", OracleDbType.Vector_Int8, unwrappedEmbeddingType != vectorProperty.EmbeddingType),
            Type t when t == typeof(ReadOnlyMemory<float>) || t == typeof(float[]) || t == typeof(Embedding<float>)
                => ($"VECTOR({vectorProperty.Dimensions}, {CommonConstants.VECTOR_FLOAT32})", OracleDbType.Vector_Float32, unwrappedEmbeddingType != vectorProperty.EmbeddingType),
            Type t when t == typeof(ReadOnlyMemory<double>) || t == typeof(double[]) || t == typeof(Embedding<double>)
                => ($"VECTOR({vectorProperty.Dimensions}, {CommonConstants.VECTOR_FLOAT64})", OracleDbType.Vector_Float64, unwrappedEmbeddingType != vectorProperty.EmbeddingType),
            Type t when t == typeof(ReadOnlyMemory<byte>) || t == typeof(byte[]) || t == typeof(Embedding<byte>)
                => ($"VECTOR({vectorProperty.Dimensions}, {CommonConstants.VECTOR_BINARY})", OracleDbType.Vector_Binary, unwrappedEmbeddingType != vectorProperty.EmbeddingType),
            _ => throw new NotSupportedException($"Type {vectorProperty.ModelName} is not supported by this store.")
        };

        return vectorCol;
    }
}
