// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Connectors.Oracle;

internal static class OracleConstants
{
    /// <summary>The name of this database for telemetry purposes.</summary>
    public const string VectorStoreSystemName = "Oracle";

    /// <summary>The default index kind.</summary>
    /// <remarks>Defaults to "Flat", which means no indexing.</remarks>
    public const string DefaultIndexKind = IndexKind.Flat;

    /// <summary>The default distance function.</summary>
    public const string DefaultDistanceFunction = DistanceFunction.CosineDistance;
}
