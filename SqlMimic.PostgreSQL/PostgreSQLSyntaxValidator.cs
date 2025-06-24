using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlMimic.Core.Abstractions;

namespace SqlMimic.PostgreSQL
{
    /// <summary>
    /// PostgreSQL specific syntax validator using regex-based parsing
    /// </summary>
    public class PostgreSQLSyntaxValidator : ISqlSyntaxValidator
    {
        public DatabaseType DatabaseType => DatabaseType.PostgreSQL;

        private static readonly Dictionary<string, SqlStatementType> StatementTypePatterns = new Dictionary<string, SqlStatementType>
        {
            { @"^\s*SELECT\s+", SqlStatementType.Select },
            { @"^\s*INSERT\s+INTO\s+", SqlStatementType.Insert },
            { @"^\s*UPDATE\s+", SqlStatementType.Update },
            { @"^\s*DELETE\s+FROM\s+", SqlStatementType.Delete },
            { @"^\s*MERGE\s+", SqlStatementType.Merge },
            { @"^\s*CREATE\s+TABLE\s+", SqlStatementType.CreateTable },
            { @"^\s*ALTER\s+TABLE\s+", SqlStatementType.AlterTable },
            { @"^\s*DROP\s+TABLE\s+", SqlStatementType.DropTable },
            { @"^\s*CREATE\s+INDEX\s+", SqlStatementType.CreateIndex },
            { @"^\s*DROP\s+INDEX\s+", SqlStatementType.DropIndex },
            { @"^\s*TRUNCATE\s+", SqlStatementType.Truncate }
        };

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

            var errors = new List<string>();

            // Basic syntax checks
            var trimmedSql = sql.Trim();
            
            // Check for unclosed quotes
            if (!IsQuotesBalanced(trimmedSql))
            {
                errors.Add("Unclosed quotes detected");
            }

            // Check for unclosed parentheses
            if (!IsParenthesesBalanced(trimmedSql))
            {
                errors.Add("Unclosed parentheses detected");
            }

            // Check for basic statement structure
            var statementType = GetStatementType(sql);
            if (statementType == SqlStatementType.Unknown)
            {
                // Check if it's a valid PostgreSQL statement that we don't categorize
                if (!IsValidPostgreSQLStatement(trimmedSql))
                {
                    errors.Add("Unknown or invalid SQL statement");
                }
            }

            // PostgreSQL specific checks
            ValidatePostgreSQLSpecific(trimmedSql, errors);

            return new SqlValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray()
            };
        }

        public SqlStatementType GetStatementType(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return SqlStatementType.Unknown;

            var trimmedSql = sql.Trim();
            
            foreach (var pattern in StatementTypePatterns)
            {
                if (Regex.IsMatch(trimmedSql, pattern.Key, RegexOptions.IgnoreCase))
                {
                    return pattern.Value;
                }
            }

            return SqlStatementType.Unknown;
        }

        public string[] ExtractTableNames(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return Array.Empty<string>();

            var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var trimmedSql = sql.Trim();

            // Patterns to extract table names from different statement types
            var patterns = new[]
            {
                @"FROM\s+([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)?)",
                @"JOIN\s+([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)?)",
                @"INTO\s+([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)?)",
                @"UPDATE\s+([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)?)",
                @"TABLE\s+([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)?)"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(trimmedSql, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var tableName = match.Groups[1].Value;
                        // Remove schema prefix if present
                        var parts = tableName.Split('.');
                        tableNames.Add(parts.Length > 1 ? parts[1] : parts[0]);
                    }
                }
            }

            return tableNames.ToArray();
        }

        private bool IsQuotesBalanced(string sql)
        {
            var singleQuoteCount = 0;
            var doubleQuoteCount = 0;
            var inEscape = false;

            for (int i = 0; i < sql.Length; i++)
            {
                if (inEscape)
                {
                    inEscape = false;
                    continue;
                }

                if (sql[i] == '\\')
                {
                    inEscape = true;
                    continue;
                }

                if (sql[i] == '\'')
                {
                    // Check for escaped single quote
                    if (i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i++; // Skip the next quote
                        continue;
                    }
                    singleQuoteCount++;
                }
                else if (sql[i] == '"')
                {
                    doubleQuoteCount++;
                }
            }

            return singleQuoteCount % 2 == 0 && doubleQuoteCount % 2 == 0;
        }

        private bool IsParenthesesBalanced(string sql)
        {
            var count = 0;
            var inSingleQuote = false;
            var inDoubleQuote = false;

            for (int i = 0; i < sql.Length; i++)
            {
                if (sql[i] == '\'' && !inDoubleQuote)
                {
                    if (i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i++; // Skip escaped quote
                        continue;
                    }
                    inSingleQuote = !inSingleQuote;
                }
                else if (sql[i] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                }
                else if (!inSingleQuote && !inDoubleQuote)
                {
                    if (sql[i] == '(')
                        count++;
                    else if (sql[i] == ')')
                        count--;

                    if (count < 0)
                        return false;
                }
            }

            return count == 0;
        }

        private bool IsValidPostgreSQLStatement(string sql)
        {
            // Common PostgreSQL statements
            var validPatterns = new[]
            {
                @"^\s*SET\s+",
                @"^\s*SHOW\s+",
                @"^\s*BEGIN\s*",
                @"^\s*COMMIT\s*",
                @"^\s*ROLLBACK\s*",
                @"^\s*VACUUM\s+",
                @"^\s*ANALYZE\s+",
                @"^\s*EXPLAIN\s+",
                @"^\s*COPY\s+",
                @"^\s*GRANT\s+",
                @"^\s*REVOKE\s+",
                @"^\s*CREATE\s+(FUNCTION|PROCEDURE|TRIGGER|VIEW|SEQUENCE|TYPE|SCHEMA|DATABASE|USER|ROLE)\s+",
                @"^\s*DROP\s+(FUNCTION|PROCEDURE|TRIGGER|VIEW|SEQUENCE|TYPE|SCHEMA|DATABASE|USER|ROLE)\s+",
                @"^\s*ALTER\s+(FUNCTION|PROCEDURE|TRIGGER|VIEW|SEQUENCE|TYPE|SCHEMA|DATABASE|USER|ROLE)\s+"
            };

            return validPatterns.Any(pattern => Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase));
        }

        private void ValidatePostgreSQLSpecific(string sql, List<string> errors)
        {
            // Check for PostgreSQL specific syntax issues
            
            // Double colon casting
            if (Regex.IsMatch(sql, @"::[a-zA-Z_]+\s*\("))
            {
                // This might be a function call, not a cast
                errors.Add("Invalid cast syntax - possible function call after :: operator");
            }

            // Array syntax validation
            if (sql.Contains("ARRAY[") && !sql.Contains("]"))
            {
                errors.Add("Unclosed ARRAY constructor");
            }

            // RETURNING clause validation
            if (Regex.IsMatch(sql, @"\sRETURNING\s+", RegexOptions.IgnoreCase))
            {
                // RETURNING should only be used with INSERT, UPDATE, DELETE
                var statementType = GetStatementType(sql);
                if (statementType != SqlStatementType.Insert && 
                    statementType != SqlStatementType.Update && 
                    statementType != SqlStatementType.Delete)
                {
                    errors.Add("RETURNING clause can only be used with INSERT, UPDATE, or DELETE statements");
                }
            }
        }
    }
}