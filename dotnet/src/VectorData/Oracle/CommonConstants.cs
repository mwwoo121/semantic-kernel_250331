// Copyright (c) Microsoft. All rights reserved.

using System;


namespace Oracle.Connectors.Common;
internal static class CommonConstants
{
    #region static Fields
    internal const string VECTOR_STR = "VECTOR";
    internal const string VECTOR_FLEX = "*";
    internal const string VECTOR_INT8 = "INT8";
    internal const string VECTOR_FLOAT32 = "FLOAT32";
    internal const string VECTOR_FLOAT64 = "FLOAT64";
    internal const string VECTOR_BINARY = "BINARY";

    internal const int VECTOR_MIN_DIMENSIONS = 0;
    internal const int VECTOR_FLOAT32_MAX_DIMENSIONS = 65535;

    //Vector Distance Strategies
    internal const string DISTANCE_FUNCTION = "VECTOR_DISTANCE";
    internal const string DISTANCE_STRATEGY_COSINE = "COSINE";
    internal const string DISTANCE_STRATEGY_EUCLIDEAN = "EUCLIDEAN ";
    internal const string DISTANCE_STRATEGY_EUCLIDEAN_SQUARED = "EUCLIDEAN_SQUARED";
    internal const string DISTANCE_STRATEGY_MANHATTAN = "MANHATTAN";
    internal const string DISTANCE_STRATEGY_INNERPRODUCT = "DOT";
    internal const string DISTANCE_STRATEGY_HAMMING = "HAMMING";
    internal const string DISTANCE_STRATEGY_JACCARD = "JACCARD";

    #endregion static Fields

}
