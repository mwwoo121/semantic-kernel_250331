// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Connectors.Oracle.UnitTests;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// <summary>
/// Sample model class that represents a glossary entry.
/// </summary>
/// <remarks>
/// Note that each property is decorated with an attribute that specifies how the property should be treated by the vector store.
/// This allows us to create a collection in the vector store and upsert and retrieve instances of this class without any further configuration.
/// </remarks>
/// This class is a copied from dotnet\samples\GettingStartedWithVectorStore
public sealed class Glossary
{
    [VectorStoreKey]
    public string Key { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public string Category { get; set; }

    [VectorStoreData]
    public string Term { get; set; }

    [VectorStoreData]
    public string Definition { get; set; }

    [VectorStoreVector(Dimensions: 384)]
    public ReadOnlyMemory<float> DefinitionEmbedding { get; set; }
}

public sealed class SimpleClass
{
    [VectorStoreKey]
    public string Key { get; set; }

    [VectorStoreData]
    public string Category { get; set; }

    [VectorStoreData]
    public string Definition { get; set; }

    [VectorStoreVector(Dimensions: 384)]
    public ReadOnlyMemory<float> DefinitionEmbedding { get; set; }

}

public record PostgresHotel<T>()
{
    /// <summary>The key of the record.</summary>
    [VectorStoreKey]
    public T HotelId { get; init; }

    /// <summary>A string metadata field.</summary>
    [VectorStoreData()]
    public string? HotelName { get; set; }

    /// <summary>An int metadata field.</summary>
    [VectorStoreData()]
    public int HotelCode { get; set; }

    /// <summary>A  float metadata field.</summary>
    [VectorStoreData()]
    public float? HotelRating { get; set; }

    /// <summary>A bool metadata field.</summary>
    [VectorStoreData(StorageName = "parkingIsIncluded")]
    public bool ParkingIncluded { get; set; }

    /// <summary>A data field.</summary>
    [VectorStoreData]
    public string Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>A vector field.</summary>
    [VectorStoreVector(4, DistanceFunction = IndexKind.Hnsw, IndexKind = DistanceFunction.ManhattanDistance)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}

public class SqliteHotel<TKey>()
{
    /// <summary>The key of the record.</summary>
    [VectorStoreKey]
    public TKey? HotelId { get; init; }

    /// <summary>A string metadata field.</summary>
    [VectorStoreData(IsIndexed = true)]
    public string? HotelName { get; set; }

    /// <summary>An int metadata field.</summary>
    [VectorStoreData]
    public int HotelCode { get; set; }

    /// <summary>A float metadata field.</summary>
    [VectorStoreData]
    public float? HotelRating { get; set; }

    /// <summary>A bool metadata field.</summary>
    [VectorStoreData(StorageName = "parkingIsIncluded")]
    public bool ParkingIncluded { get; set; }

    /// <summary>A data field.</summary>
    [VectorStoreData]
    public string? Description { get; set; }

    /// <summary>A vector field.</summary>
    [VectorStoreVector(Dimensions: 4, DistanceFunction = DistanceFunction.EuclideanDistance)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}
