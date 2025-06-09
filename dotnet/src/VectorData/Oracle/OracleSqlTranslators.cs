// Copyright (c) Microsoft. All rights reserved.

namespace Oracle.Connectors.Common;
internal abstract class OracleSqlFilterTranslator
{
    internal abstract OracleSqlCommandInfo GenerateSQL(OracleDataModelMetadata metadata, int startParameterIndex);
}
internal abstract class OracleSqlOrderByTranslator
{
    internal abstract OracleSqlCommandInfo GenerateSQL(OracleDataModelMetadata metadatai, int startParameterIndex = 1);
}
