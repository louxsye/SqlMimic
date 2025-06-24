using System;

namespace SqlMimic.Core.Abstractions
{
    /// <summary>
    /// Interface for SQL syntax validation
    /// </summary>
    public interface ISqlSyntaxValidator
    {
        /// <summary>
        /// Gets the database type this validator supports
        /// </summary>
        DatabaseType DatabaseType { get; }

        /// <summary>
        /// Checks if the SQL statement syntax is valid
        /// </summary>
        /// <param name="sql">SQL statement to validate</param>
        /// <returns>Syntax validation result</returns>
        SqlValidationResult ValidateSyntax(string sql);

        /// <summary>
        /// Determines the type of SQL statement
        /// </summary>
        /// <param name="sql">SQL statement to identify</param>
        /// <returns>Type of SQL statement</returns>
        SqlStatementType GetStatementType(string sql);

        /// <summary>
        /// Extracts table names from SQL statement
        /// </summary>
        /// <param name="sql">SQL statement to analyze</param>
        /// <returns>List of table names</returns>
        string[] ExtractTableNames(string sql);
    }
}