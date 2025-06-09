// Copyright (c) Microsoft. All rights reserved.
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Oracle.ManagedDataAccess.Client;
namespace Oracle.Connectors.Common;

internal enum VectorIndexType
{
    HNSW = 1,
    IVF = 2
}

internal abstract class OracleInternalVectorIndexType
{
    /// <summary>
    /// Name of the Vector Index
    /// </summary>
    public string VectorIndexName
    {
        get => this._indexName;

        set
        {
            Verify.NotNullOrWhiteSpace(value);
            this._indexName = value;
        }
    }

    /// <summary>
    /// Vector distance strategy
    /// Default: Cosine
    /// </summary>
    public string DistanceMetric { set; get; } = DistanceFunction.CosineDistance;

    /// <summary>
    /// Target Accuracy
    /// Default: 90
    /// Range: 1 to 100
    /// </summary>
    public int Accuracy
    {
        set
        {
            if (value < MIN_ACCURACY || value > MAX_ACCURACY)
            {
                throw new ArgumentOutOfRangeException(nameof(this.Accuracy), $"{nameof(this.Accuracy)} has a value of {value}.  {nameof(this.Accuracy)} must be between {MIN_ACCURACY} and {MAX_ACCURACY}.");
            }
            this._accuracy = value;
        }
        get => this._accuracy;
    }

    /// <summary>
    /// Degree of parallelism (DOP)
    /// Default: 8)
    /// Range : > 0
    /// </summary>
    public int? Parallelism
    {
        set
        {
            if (value != null && value <= MIN_PARALLEL)
            {
                throw new ArgumentException($"{nameof(this.Parallelism)} has a value of {value}.  {nameof(this.Parallelism)} must be greater than {MIN_PARALLEL}.", nameof(this.Parallelism));
            }
            this._parallelism = value;
        }
        get => this._parallelism;
    }

    internal OracleInternalVectorIndexType(string indexName)
    {
        this._indexName = indexName;
    }

    internal abstract OracleSqlCommandInfo GenerateSQL(string tableName, string vectorColumnName);

    #region Private/Internal Fields
    /// <summary>
    /// Specifies the vector index type: IVF or HNSW
    /// </summary>
    #region Internal const
    internal const int MIN_ACCURACY = 1;
    internal const int MIN_PARALLEL = 0;
    internal const int MAX_ACCURACY = 100;

    internal const int HNSW_MIN_NEIGHBORS = 2;
    internal const int HNSW_MAX_NEIGHBORS = 2048;
    internal const int HNSW_MIN_EFCONSTRUCTION = 1;
    internal const int HNSW_MAX_EFCONSTRUCTION = 65535;

    internal const int IVF_MIN_PARITTION = 0;
    internal const int IVF_MIN_NEIGHBOR_PARITTIONS = 1;
    internal const int IVF_MAX_NEIGHBOR_PARTITIONS = 10000000;
    internal const int IVF_MIN_SAMPLES_PER_PARITTION = 1;
    internal const int IVF_MIN_VECTORS_PER_PARITTION = 0;
    #endregion Internal const

    internal VectorIndexType IndexType { get; set; }
    internal string _indexName = string.Empty;
    private int _accuracy = 90;
    private int? _parallelism = 8;
    #endregion Private/Internal Fields

}

internal class OracleInternalHNSWVectorIndex : OracleInternalVectorIndexType
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="vectorIndexName">Vector index name</param>
    public OracleInternalHNSWVectorIndex(string vectorIndexName) : base(vectorIndexName)
    {
        Verify.NotNullOrWhiteSpace(vectorIndexName);
        this.IndexType = VectorIndexType.HNSW;
    }

    /// <summary>
    /// Maximum number of neighbors a vector can have on any layer
    /// Default: 32
    /// Range: 1 to 2048
    /// </summary>
    public int? Neighbors
    {
        set
        {
            if (value != null &&
                (value < HNSW_MIN_NEIGHBORS || value > HNSW_MAX_NEIGHBORS))
            {
                throw new ArgumentOutOfRangeException(nameof(this.Neighbors), $"{nameof(this.Neighbors)} has a value of {value}.  {nameof(this.Neighbors)} must be between {HNSW_MIN_NEIGHBORS} and {HNSW_MAX_NEIGHBORS}.");
            }
            this._neighbors = value;
        }
        get => this._neighbors;
    }

    /// <summary>
    /// Mximum number of closest vector candidates considered at each step of the search during insertion.
    /// Default: 200
    /// Range: 1 to 65535
    /// </summary>
    public int? EFConstruction
    {
        set
        {
            if (value != null &&
                (value < HNSW_MIN_EFCONSTRUCTION || value > HNSW_MAX_EFCONSTRUCTION))
            {
                throw new ArgumentOutOfRangeException(nameof(this.EFConstruction), $"{nameof(this.EFConstruction)} has a value of {value}.  {nameof(this.EFConstruction)} must be between {HNSW_MIN_EFCONSTRUCTION} and {HNSW_MAX_EFCONSTRUCTION}.");
            }
            this._eFConstruction = value;
        }
        get => this._eFConstruction;
    }

    internal override OracleSqlCommandInfo GenerateSQL(string tableName, string vectorColumnName)
    {
        string sql;

        string internalIndexName = OracleCommandGenerator.ValidateAndConvertToQuotedDbObjectName(this.VectorIndexName, "HNSW vector index name").Item1;


        sql = $"CREATE VECTOR INDEX {internalIndexName} ON {tableName}({vectorColumnName}) GLOBAL ORGANIZATION INMEMORY NEIGHBOR GRAPH WITH DISTANCE {this.DistanceMetric} " +
            $"WITH TARGET ACCURACY {this.Accuracy} PARAMETERS (TYPE HNSW, NEIGHBORS {this.Neighbors}, EFCONSTRUCTION {this.EFConstruction}) ";

        if (this.Parallelism != null)
        {
            sql = $"{sql} PARALLEL {this.Parallelism} ";
        }

        return new()
        {
            SqlText = sql,
        };
    }

    #region Private Fields
    private int? _neighbors = 32;
    private int? _eFConstruction = 200;
    #endregion Private Fields
}

/// <summary>
/// Specifies the partition type of the IVF vector index
/// </summary>
[Flags]
internal enum OracleInternalIVFParitionType
{
    /// <summary>
    /// The target number of centroid partitions that are created by the index.
    /// </summary>
    NeighborPartitions = 0b_0000_0001,  // 1
    /// <summary>
    /// The total number of vectors that are passed to the clustering algorithm
    /// </summary>
    SamplesPerPartition = 0b_0000_0010,  // 2
    /// <summary>
    /// The target minimum number of vectors per partition
    /// </summary>
    MinVectorsPerPartition = 0b_0000_0100 // 4
}

internal class OracleInternalIVFVectorIndex : OracleInternalVectorIndexType
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="vectorIndexName">Vector index name</param>
    public OracleInternalIVFVectorIndex(string vectorIndexName) : base(vectorIndexName)
    {
        Verify.NotNullOrWhiteSpace(vectorIndexName);
        this.IndexType = VectorIndexType.IVF;
        this._includedColumns = new List<string>();
    }

    /// <summary>
    /// The partition type for the IVFVectorIndex.
    /// Default: Neighbor Partition.
    /// </summary>
    public OracleInternalIVFParitionType PartitionTypes { set; get; } = OracleInternalIVFParitionType.NeighborPartitions;

    /// <summary>
    /// Included columns in vector indexes facilitate faster searches with attribute filters by incorporating non-vector columns within a Neighbor Partition Vector Index.
    /// Default: null.
    /// </summary>
    public List<string> IncludedColumns
    {
        get
        {
            if (this._includedColumns == null)
            {
                this._includedColumns = new List<string>();
            }

            return this._includedColumns;
        }
    }

    /// <summary>
    /// The target number of centroid partitions for Neighbor Partitions type.
    /// Default: 32
    /// Range: 1 to 10000000
    /// </summary>
    public int? NeighborPartitions
    {
        set
        {
            if (value != null && (value < IVF_MIN_NEIGHBOR_PARITTIONS || value > IVF_MAX_NEIGHBOR_PARTITIONS))
            {
                throw new ArgumentOutOfRangeException(nameof(this.NeighborPartitions), $"{nameof(this.NeighborPartitions)} has a value of {value}.  {nameof(this.NeighborPartitions)} must be between {IVF_MIN_VECTORS_PER_PARITTION} and {IVF_MAX_NEIGHBOR_PARTITIONS}.");
            }

            this._neighborPartitions = value;
        }
        get => this._neighborPartitions;
    }

    /// <summary>
    /// Specifies the total number of vectors that are passed to the clustering algorithm for the Samples Per Partitions type.
    /// Default: null
    /// Range:Miminum value = 1
    /// </summary>
    public int? SamplesPerPartition
    {
        set
        {
            if (value != null && value < IVF_MIN_SAMPLES_PER_PARITTION)
            {
                throw new ArgumentException($"{nameof(this.SamplesPerPartition)} has a value of {value}.  {nameof(this.SamplesPerPartition)} must be greater than {IVF_MIN_SAMPLES_PER_PARITTION}.", nameof(this.SamplesPerPartition));
            }

            this._samplesPerPartition = value;
        }
        get => this._samplesPerPartition;
    }

    /// <summary>
    /// Specifies the target minimum number of vectors per partition.
    /// Default: null
    /// Range: Miminum value = 0
    /// </summary>
    public int? MinVectorsPerPartition
    {
        set
        {
            if (value != null && value < IVF_MIN_VECTORS_PER_PARITTION)
            {
                throw new ArgumentException($"{nameof(this.MinVectorsPerPartition)} has a value of {value}.  {nameof(this.MinVectorsPerPartition)} must be greater than {IVF_MIN_VECTORS_PER_PARITTION}.", nameof(this.MinVectorsPerPartition));
            }
            this._minVectorsPerPartition = value;
        }
        get => this._minVectorsPerPartition;
    }

    /// <summary>
    /// The IVF vector index is a global or local IVF vector index
    /// Default: false
    /// </summary>
    public bool? IsLocal { set; get; } = false;

    internal override OracleSqlCommandInfo GenerateSQL(string tableName, string vectorColumnName)
    {
        string? parameterOptions = null;

        if ((this.PartitionTypes & OracleInternalIVFParitionType.NeighborPartitions) != 0)
        {
            parameterOptions = $"NEIGHBOR PARTITIONS {this.NeighborPartitions} ";
        }

        if ((this.PartitionTypes & OracleInternalIVFParitionType.SamplesPerPartition) != 0)
        {
            if (this.SamplesPerPartition != null)
            {
                parameterOptions = string.IsNullOrEmpty(parameterOptions)
                    ? $"SAMPLES_PER_PARTITION {this.SamplesPerPartition} "
                    : $"| SAMPLES_PER_PARTITION {this.SamplesPerPartition} ";
            }
        }

        if ((this.PartitionTypes & OracleInternalIVFParitionType.MinVectorsPerPartition) != 0)
        {
            if (this.MinVectorsPerPartition != null)
            {
                parameterOptions = string.IsNullOrEmpty(parameterOptions)
                    ? $"MIN_VECTORS_PER_PARTITION {this.MinVectorsPerPartition} "
                    : $"| MIN_VECTORS_PER_PARTITION {this.MinVectorsPerPartition} ";
            }
        }

        string internalIndexName = OracleCommandGenerator.ValidateAndConvertToQuotedDbObjectName(this.VectorIndexName, "IVF vector index name").Item1;
        string colName = vectorColumnName;
        string vectorDistance = this.DistanceMetric;

        StringBuilder includedColumnsClause = new();
        bool bHasIncludedColumns = false;
        if (this.IncludedColumns != null && this.IncludedColumns.Count > 0)
        {
            string quotedColName;
            foreach (string includedCol in this.IncludedColumns)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(includedCol, "OracleIVFVectorIndex.IncludedColumns");
                quotedColName = OracleDBMSAssert.EnquoteIdentifier(includedCol, true, false);

                ArgumentException.ThrowIfNullOrWhiteSpace(quotedColName, "OracleIVFVectorIndex.IncludedColumns");

                if (bHasIncludedColumns)
                {
                    includedColumnsClause.Append($", {quotedColName}");
                }
                else
                {
                    includedColumnsClause.Append($"INCLUDE({quotedColName}");
                    bHasIncludedColumns = true;
                }
            }

            if (bHasIncludedColumns)
            {
                includedColumnsClause.Append(')');
            }
        }

        string sql;
        if (!bHasIncludedColumns)
        {
            sql = $"CREATE VECTOR INDEX {internalIndexName} ON {tableName}({colName}) ORGANIZATION NEIGHBOR PARTITIONS " +
                $"WITH DISTANCE {vectorDistance} " +
                $"WITH TARGET ACCURACY {this.Accuracy} ";
        }
        else
        {
            sql = $"CREATE VECTOR INDEX {internalIndexName} ON {tableName}({colName}) {includedColumnsClause} ORGANIZATION NEIGHBOR PARTITIONS " +
                $"WITH DISTANCE {vectorDistance} " +
                $"WITH TARGET ACCURACY {this.Accuracy} ";
        }

        if (!string.IsNullOrEmpty(parameterOptions))
        {
            sql = $"{sql} PARAMETERS (TYPE IVF, {parameterOptions}) ";
        }

        if (this.Parallelism != null)
        {
            sql = $"{sql} PARALLEL {this.Parallelism} ";
        }

        if (this.IsLocal == true)
        {
            sql = $"{sql} LOCAL ";
        }

        return new()
        {
            SqlText = sql,
        };
    }

    #region Private Fields
    private List<string> _includedColumns;
    private int? _neighborPartitions = 32;
    private int? _samplesPerPartition;
    private int? _minVectorsPerPartition;
    #endregion Private Fields
}
