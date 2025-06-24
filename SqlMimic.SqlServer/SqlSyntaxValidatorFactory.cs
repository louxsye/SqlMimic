using System;
using SqlMimic.Core.Abstractions;

namespace SqlMimic.SqlServer
{
    /// <summary>
    /// Factory for creating SQL Server syntax validator
    /// </summary>
    public static class SqlSyntaxValidatorFactory
    {
        /// <summary>
        /// Creates a SQL Server syntax validator
        /// </summary>
        /// <returns>SQL Server syntax validator instance</returns>
        public static ISqlSyntaxValidator CreateValidator()
        {
            return new SqlServerSyntaxValidator();
        }
    }
}