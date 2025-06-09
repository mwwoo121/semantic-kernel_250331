// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Oracle.Connectors.Common;
internal interface IOracleConnectionDispenser : IDisposable
{
    public Task<OracleConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
    public OracleConnection OpenConnection();
    public void Share() { }
}

#if NET8_0_OR_GREATER
internal class OracleConnectionDispenser : IOracleConnectionDispenser
{
    internal OracleDataSource _dataSource;
    protected bool _bDisposeDataSource;
    private int _referenceCount = 1;

    internal OracleConnectionDispenser(OracleDataSource ds, bool bDisposeDataSource = true)
    {
        this._dataSource = ds;
        this._bDisposeDataSource = bDisposeDataSource;
    }

#if NET8_0_OR_GREATER
    internal OracleDataSource DataSource => this._dataSource;
#endif

    public async Task<OracleConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    }

    public OracleConnection OpenConnection()
    {
        return this._dataSource.OpenConnection();
    }

    public void Share()
    {
        if (this._bDisposeDataSource)
        {
            Interlocked.Increment(ref this._referenceCount);
        }

        //return this;
    }

    public void Dispose()
    {
        if (this._bDisposeDataSource)
        {
            // An instance of OracleDbClient can be shared between a single store and multiple collections.
            // The reference count is used to track how many collections are using this instance.
            // When the number gets to zero, the DataSource is getting disposed.
            if (Interlocked.Decrement(ref this._referenceCount) == 0)
            {
                this._dataSource.Dispose();
            }
        }
    }
}
#endif

#if !NET8_0_OR_GREATER
internal class OracleConnectionDispenser : IOracleConnectionDispenser
{
    internal string _connectionString;

    internal OracleConnectionDispenser(string connStr)
    {
        _connectionString = connStr;
    }

    public async Task<OracleConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        OracleConnection conn = new(this._connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }

    public OracleConnection OpenConnection()
    {
        OracleConnection conn = new(this._connectionString);
        conn.Open();
        return conn;
    }

    public void Share() { }


    public void Dispose()
    {
    }
}
#endif
