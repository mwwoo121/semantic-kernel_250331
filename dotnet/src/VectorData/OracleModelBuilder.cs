// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData.ProviderServices;

namespace SemanticKernel.Connectors.Oracle;

internal class OracleModelBuilder() : CollectionModelBuilder(OracleModelBuilder.ModelBuildingOptions)
{
    internal const string SupportedVectorTypes = "ReadOnlyMemory<float>, Embedding<float>, float[], " +
        "ReadOnlyMemory<double>, Embedding<double>, double[], " +
        "ReadOnlyMemory<short>, Embedding<short>, short[], " +
        "ReadOnlyMemory<byte>, Embedding<byte>, byte[], " +
        "BitArray";

    public static readonly CollectionModelBuildingOptions ModelBuildingOptions = new()
    {
        RequiresAtLeastOneVector = false,
        SupportsMultipleKeys = true,
        SupportsMultipleVectors = true,
    };

    protected override bool IsKeyPropertyTypeValid(Type type, [NotNullWhen(false)] out string? supportedTypes)
    {
        supportedTypes = "short, int, long, string, Guid";

        return type == typeof(short)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(string)
            || type == typeof(Guid);
    }

    protected override bool IsDataPropertyTypeValid(Type type, [NotNullWhen(false)] out string? supportedTypes)
    {
        supportedTypes = "bool, short, int, long, float, double, decimal, string, DateTime, DateTimeOffset, TimeSpan, char, char[], byte[], Guid";

        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
        {
            type = underlyingType;
        }

        return type == typeof(bool) ||
            type == typeof(short) ||
            type == typeof(int) ||
            type == typeof(long) ||
            type == typeof(float) ||
            type == typeof(double) ||
            type == typeof(decimal) ||
            type == typeof(string) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) ||
            type == typeof(char) ||
            type == typeof(char[]) ||
            type == typeof(byte[]) ||
            type == typeof(Guid);
    }

    protected override bool IsVectorPropertyTypeValid(Type type, [NotNullWhen(false)] out string? supportedTypes)
        => IsVectorPropertyTypeValidCore(type, out supportedTypes);

    internal static bool IsVectorPropertyTypeValidCore(Type type, [NotNullWhen(false)] out string? supportedTypes)
    {
        supportedTypes = SupportedVectorTypes;

        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
        {
            type = underlyingType;
        }

        return type == typeof(ReadOnlyMemory<float>) ||
            type == typeof(Embedding<float>) ||
            type == typeof(float[]) ||
            type == typeof(ReadOnlyMemory<double>) ||
            type == typeof(Embedding<double>) ||
            type == typeof(double[]) ||
            type == typeof(ReadOnlyMemory<short>) ||
            type == typeof(Embedding<short>) ||
            type == typeof(short[]) ||
            type == typeof(ReadOnlyMemory<byte>) ||
            type == typeof(Embedding<byte>) ||
            type == typeof(byte[]);
    }

    /// <inheritdoc />
    protected override Type? ResolveEmbeddingType(
        VectorPropertyModel vectorProperty,
        IEmbeddingGenerator embeddingGenerator,
        Type? userRequestedEmbeddingType)
        => vectorProperty.ResolveEmbeddingType<Embedding<float>>(embeddingGenerator, userRequestedEmbeddingType);
};
