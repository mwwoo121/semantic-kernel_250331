// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Connectors.Oracle.UnitTests;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
