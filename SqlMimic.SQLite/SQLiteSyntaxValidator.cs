using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlMimic.Core.Abstractions;

namespace SqlMimic.SQLite
{
    /// <summary>
    /// SQLite specific syntax validator using regex-based parsing
    /// </summary>
    public class SQLiteSyntaxValidator : ISqlSyntaxValidator
    {
        public DatabaseType DatabaseType => DatabaseType.SQLite;

        private static readonly Dictionary<string, SqlStatementType> StatementTypePatterns = new Dictionary<string, SqlStatementType>
        {
            { @"^\s*SELECT\s+", SqlStatementType.Select },
            { @"^\s*INSERT\s+", SqlStatementType.Insert },
            { @"^\s*UPDATE\s+", SqlStatementType.Update },
            { @"^\s*DELETE\s+FROM\s+", SqlStatementType.Delete },
            { @"^\s*REPLACE\s+", SqlStatementType.Insert }, // SQLite REPLACE
            { @"^\s*CREATE\s+TABLE\s+", SqlStatementType.CreateTable },
            { @"^\s*ALTER\s+TABLE\s+", SqlStatementType.AlterTable },
            { @"^\s*DROP\s+TABLE\s+", SqlStatementType.DropTable },
            { @"^\s*CREATE\s+INDEX\s+", SqlStatementType.CreateIndex },
            { @"^\s*DROP\s+INDEX\s+", SqlStatementType.DropIndex },
            { @"^\s*CREATE\s+UNIQUE\s+INDEX\s+", SqlStatementType.CreateIndex }
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
                if (!IsValidSQLiteStatement(trimmedSql))
                {
                    errors.Add("Unknown or invalid SQL statement");
                }
            }

            // SQLite specific validation
            ValidateSQLiteSpecific(trimmedSql, errors);

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

            // Remove comments
            trimmedSql = RemoveSQLiteComments(trimmedSql);

            // Patterns to extract table names - SQLite supports multiple quote styles
            var patterns = new[]
            {
                @"FROM\s+[`""\[]?([a-zA-Z_][a-zA-Z0-9_]*)[`""\]]?",
                @"JOIN\s+[`""\[]?([a-zA-Z_][a-zA-Z0-9_]*)[`""\]]?",
                @"INTO\s+[`""\[]?([a-zA-Z_][a-zA-Z0-9_]*)[`""\]]?",
                @"UPDATE\s+[`""\[]?([a-zA-Z_][a-zA-Z0-9_]*)[`""\]]?",
                @"TABLE\s+[`""\[]?([a-zA-Z_][a-zA-Z0-9_]*)[`""\]]?"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(trimmedSql, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var tableName = match.Groups[1].Value;
                        // SQLite system tables start with sqlite_
                        if (!tableName.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
                        {
                            tableNames.Add(tableName);
                        }
                    }
                }
            }

            return tableNames.ToArray();
        }

        private bool IsQuotesBalanced(string sql)
        {
            var singleQuoteCount = 0;
            var doubleQuoteCount = 0;

            for (int i = 0; i < sql.Length; i++)
            {
                if (sql[i] == '\'')
                {
                    // Check for escaped single quote (SQLite uses '' for escaping)
                    if (i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i++; // Skip the next quote
                        continue;
                    }
                    singleQuoteCount++;
                }
                else if (sql[i] == '"')
                {
                    // SQLite can use double quotes for identifiers
                    doubleQuoteCount++;
                }
            }

            // Also check for square brackets (SQLite supports [identifier] syntax)
            var openBrackets = sql.Count(c => c == '[');
            var closeBrackets = sql.Count(c => c == ']');

            return singleQuoteCount % 2 == 0 && 
                   doubleQuoteCount % 2 == 0 && 
                   openBrackets == closeBrackets;
        }

        private bool IsParenthesesBalanced(string sql)
        {
            var count = 0;
            var inSingleQuote = false;
            var inDoubleQuote = false;
            var inBracket = false;

            for (int i = 0; i < sql.Length; i++)
            {
                if (sql[i] == '\'' && !inDoubleQuote && !inBracket)
                {
                    if (i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i++; // Skip escaped quote
                        continue;
                    }
                    inSingleQuote = !inSingleQuote;
                }
                else if (sql[i] == '"' && !inSingleQuote && !inBracket)
                {
                    inDoubleQuote = !inDoubleQuote;
                }
                else if (sql[i] == '[' && !inSingleQuote && !inDoubleQuote)
                {
                    inBracket = true;
                }
                else if (sql[i] == ']' && !inSingleQuote && !inDoubleQuote)
                {
                    inBracket = false;
                }
                else if (!inSingleQuote && !inDoubleQuote && !inBracket)
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

        private bool IsValidSQLiteStatement(string sql)
        {
            var validPatterns = new[]
            {
                @"^\s*PRAGMA\s+",
                @"^\s*ATTACH\s+",
                @"^\s*DETACH\s+",
                @"^\s*BEGIN\s*",
                @"^\s*COMMIT\s*",
                @"^\s*ROLLBACK\s*",
                @"^\s*SAVEPOINT\s+",
                @"^\s*RELEASE\s+",
                @"^\s*VACUUM\s*",
                @"^\s*ANALYZE\s*",
                @"^\s*EXPLAIN\s+",
                @"^\s*CREATE\s+(VIEW|TRIGGER|VIRTUAL\s+TABLE)\s+",
                @"^\s*DROP\s+(VIEW|TRIGGER)\s+",
                @"^\s*ALTER\s+TABLE\s+.*\s+(RENAME|ADD)\s+",
                @"^\s*REINDEX\s*",
                @"^\s*WITH\s+"  // Common Table Expressions
            };

            return validPatterns.Any(pattern => Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase));
        }

        private void ValidateSQLiteSpecific(string sql, List<string> errors)
        {
            // SQLite specific validations

            // Check for TRUNCATE (not supported in SQLite)
            if (Regex.IsMatch(sql, @"^\s*TRUNCATE\s+", RegexOptions.IgnoreCase))
            {
                errors.Add("TRUNCATE is not supported in SQLite. Use DELETE FROM table_name instead.");
            }

            // Check for RIGHT/FULL OUTER JOIN (not supported in SQLite < 3.39.0)
            if (Regex.IsMatch(sql, @"\s(RIGHT|FULL)\s+OUTER\s+JOIN\s", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(sql, @"\s(RIGHT|FULL)\s+JOIN\s", RegexOptions.IgnoreCase))
            {
                errors.Add("RIGHT and FULL OUTER JOINs are not supported in older SQLite versions");
            }

            // Check for ALTER TABLE limitations
            if (Regex.IsMatch(sql, @"ALTER\s+TABLE", RegexOptions.IgnoreCase))
            {
                // SQLite has limited ALTER TABLE support
                if (!Regex.IsMatch(sql, @"ALTER\s+TABLE\s+.*\s+(RENAME|ADD\s+COLUMN|RENAME\s+COLUMN|DROP\s+COLUMN)", RegexOptions.IgnoreCase))
                {
                    errors.Add("SQLite only supports limited ALTER TABLE operations: RENAME TO, ADD COLUMN, RENAME COLUMN, DROP COLUMN");
                }
            }

            // Check for unsupported data types
            var unsupportedTypes = new[] { "DATETIME2", "MONEY", "SMALLMONEY", "HIERARCHYID", "GEOGRAPHY", "GEOMETRY" };
            foreach (var type in unsupportedTypes)
            {
                if (Regex.IsMatch(sql, $@"\s{type}(\s|\(|,|$)", RegexOptions.IgnoreCase))
                {
                    errors.Add($"{type} data type is not supported in SQLite");
                }
            }

            // Validate AUTOINCREMENT usage
            if (Regex.IsMatch(sql, @"\sAUTOINCREMENT\s", RegexOptions.IgnoreCase))
            {
                // AUTOINCREMENT must be used with INTEGER PRIMARY KEY
                if (!Regex.IsMatch(sql, @"INTEGER\s+PRIMARY\s+KEY\s+AUTOINCREMENT", RegexOptions.IgnoreCase))
                {
                    errors.Add("AUTOINCREMENT can only be used with INTEGER PRIMARY KEY columns");
                }
            }

            // Check for IF EXISTS/IF NOT EXISTS in wrong contexts
            if (Regex.IsMatch(sql, @"CREATE\s+TABLE\s+.*\s+IF\s+EXISTS", RegexOptions.IgnoreCase))
            {
                errors.Add("Use 'CREATE TABLE IF NOT EXISTS' instead of 'CREATE TABLE IF EXISTS'");
            }
        }

        private string RemoveSQLiteComments(string sql)
        {
            // Remove -- comments
            sql = Regex.Replace(sql, @"--[^\r\n]*", "");

            // Remove /* */ comments
            sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);

            return sql;
        }
    }
}