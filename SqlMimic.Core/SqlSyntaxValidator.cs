using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMimic.Core
{

    /// <summary>
    /// Class for validating SQL syntax
    /// </summary>
    public static class SqlSyntaxValidator
    {
        /// <summary>
        /// Checks if the SQL statement syntax is valid
        /// </summary>
        /// <param name="sql">SQL statement to validate</param>
        /// <returns>Syntax validation result</returns>
        public static SqlValidationResult ValidateSyntax(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return new SqlValidationResult
                {
                    IsValid = false,
                    Errors = new[] { "SQL statement is empty" }
                };
            }

            var parser = new TSql150Parser(true); // SQL Server 2019 compatible, allows quoted identifiers

            using (var reader = new StringReader(sql))
            {
                var fragment = parser.Parse(reader, out var errors);

                return new SqlValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors.Select(e => $"Line {e.Line}, Column {e.Column}: {e.Message}").ToArray(),
                    ParsedFragment = fragment
                };
            }
        }

        /// <summary>
        /// Determines the type of SQL statement
        /// </summary>
        /// <param name="sql">SQL statement to identify</param>
        /// <returns>Type of SQL statement</returns>
        public static SqlStatementType GetStatementType(string sql)
        {
            var result = ValidateSyntax(sql);
            if (!result.IsValid || result.ParsedFragment == null)
                return SqlStatementType.Unknown;

            var visitor = new StatementTypeVisitor();
            result.ParsedFragment.Accept(visitor);
            return visitor.StatementType;
        }

        /// <summary>
        /// Extracts table names from SQL statement
        /// </summary>
        /// <param name="sql">SQL statement to analyze</param>
        /// <returns>List of table names</returns>
        public static string[] ExtractTableNames(string sql)
        {
            var result = ValidateSyntax(sql);
            if (!result.IsValid || result.ParsedFragment == null)
                return new string[0];

            var visitor = new TableNameVisitor();
            result.ParsedFragment.Accept(visitor);
            return visitor.TableNames.ToArray();
        }
    }

    /// <summary>
    /// SQL syntax validation result
    /// </summary>
    public class SqlValidationResult
    {
        /// <summary>
        /// Whether the syntax is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error messages
        /// </summary>
        public string[] Errors { get; set; } = new string[0];

        /// <summary>
        /// Parsed SQL fragment
        /// </summary>
        public TSqlFragment ParsedFragment { get; set; }
    }

    /// <summary>
    /// Type of SQL statement
    /// </summary>
    public enum SqlStatementType
    {
        Unknown,
        Select,
        Insert,
        Update,
        Delete,
        Merge
    }

    /// <summary>
    /// Visitor to determine SQL statement type
    /// </summary>
    internal class StatementTypeVisitor : TSqlFragmentVisitor
    {
        public SqlStatementType StatementType { get; private set; } = SqlStatementType.Unknown;

        public override void Visit(SelectStatement node)
        {
            StatementType = SqlStatementType.Select;
        }

        public override void Visit(InsertStatement node)
        {
            StatementType = SqlStatementType.Insert;
        }

        public override void Visit(UpdateStatement node)
        {
            StatementType = SqlStatementType.Update;
        }

        public override void Visit(DeleteStatement node)
        {
            StatementType = SqlStatementType.Delete;
        }

        public override void Visit(MergeStatement node)
        {
            StatementType = SqlStatementType.Merge;
        }
    }

    /// <summary>
    /// Visitor to extract table names
    /// </summary>
    internal class TableNameVisitor : TSqlFragmentVisitor
    {
        public List<string> TableNames { get; } = new List<string>();

        public override void Visit(NamedTableReference node)
        {
            var tableName = node.SchemaObject.BaseIdentifier.Value;
            if (!TableNames.Contains(tableName))
            {
                TableNames.Add(tableName);
            }
        }
    }
}
