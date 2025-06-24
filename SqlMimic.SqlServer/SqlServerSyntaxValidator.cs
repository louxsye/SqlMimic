using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlMimic.Core.Abstractions;

namespace SqlMimic.SqlServer
{
    /// <summary>
    /// SQL Server specific syntax validator
    /// </summary>
    public class SqlServerSyntaxValidator : ISqlSyntaxValidator
    {
        public DatabaseType DatabaseType => DatabaseType.SqlServer;

        public SqlValidationResult ValidateSyntax(string sql)
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

                return new SqlServerValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors.Select(e => $"Line {e.Line}, Column {e.Column}: {e.Message}").ToArray(),
                    ParsedFragment = fragment
                };
            }
        }

        public SqlStatementType GetStatementType(string sql)
        {
            var result = ValidateSyntax(sql) as SqlServerValidationResult;
            if (result == null || !result.IsValid || result.ParsedFragment == null)
                return SqlStatementType.Unknown;

            var visitor = new StatementTypeVisitor();
            result.ParsedFragment.Accept(visitor);
            return visitor.StatementType;
        }

        public string[] ExtractTableNames(string sql)
        {
            var result = ValidateSyntax(sql) as SqlServerValidationResult;
            if (result == null || !result.IsValid || result.ParsedFragment == null)
                return Array.Empty<string>();

            var visitor = new TableNameVisitor();
            result.ParsedFragment.Accept(visitor);
            return visitor.TableNames.ToArray();
        }

        /// <summary>
        /// SQL Server specific validation result
        /// </summary>
        private class SqlServerValidationResult : SqlValidationResult
        {
            public TSqlFragment ParsedFragment { get; set; }
        }

        /// <summary>
        /// Visitor to determine SQL statement type
        /// </summary>
        private class StatementTypeVisitor : TSqlFragmentVisitor
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

            public override void Visit(CreateTableStatement node)
            {
                StatementType = SqlStatementType.CreateTable;
            }

            public override void Visit(AlterTableStatement node)
            {
                StatementType = SqlStatementType.AlterTable;
            }

            public override void Visit(DropTableStatement node)
            {
                StatementType = SqlStatementType.DropTable;
            }

            public override void Visit(CreateIndexStatement node)
            {
                StatementType = SqlStatementType.CreateIndex;
            }

            public override void Visit(DropIndexStatement node)
            {
                StatementType = SqlStatementType.DropIndex;
            }

            public override void Visit(TruncateTableStatement node)
            {
                StatementType = SqlStatementType.Truncate;
            }
        }

        /// <summary>
        /// Visitor to extract table names
        /// </summary>
        private class TableNameVisitor : TSqlFragmentVisitor
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
}