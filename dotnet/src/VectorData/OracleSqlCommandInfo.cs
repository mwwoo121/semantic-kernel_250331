// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Oracle.ManagedDataAccess.Client;

namespace Oracle.Connectors.Common;

internal class OracleSqlCommandInfo
{
    internal required string SqlText { get; init; }

    internal List<OracleParameter>? Parameters { get; init; }

    internal int ArrayBindCount { get; init; } = 0;

    /// <summary>
    /// Converts this instance to an <see cref="OracleCommand"/>.
    /// </summary>
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "User input validated before constructing the command string.")]
    public OracleCommand ToOracleCommand(OracleConnection connection, OracleTransaction? transaction = null)
    {
        OracleCommand cmd = connection.CreateCommand();
        if (transaction != null)
        {
            cmd.Transaction = transaction;
        }

        cmd.CommandText = this.SqlText;

        if (this.Parameters != null)
        {
            foreach (var parameter in this.Parameters)
            {
                cmd.Parameters.Add(parameter);
            }
        }
        return cmd;
    }
}
