// Copyright (c) Microsoft. All rights reserved.
using System;

namespace Oracle.Connectors.Common;
internal class OracleSKOrderByTranslator : OracleSqlOrderByTranslator
{
    internal override OracleSqlCommandInfo GenerateSQL(OracleDataModelMetadata metadata, int startParameterIndex = 1) => throw new NotImplementedException();
}
