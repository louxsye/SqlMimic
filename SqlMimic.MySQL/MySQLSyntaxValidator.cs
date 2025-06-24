using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlMimic.Core.Abstractions;

namespace SqlMimic.MySQL
{
    /// <summary>
    /// MySQL specific syntax validator using regex-based parsing
    /// </summary>
    public class MySQLSyntaxValidator : ISqlSyntaxValidator
    {
        public DatabaseType DatabaseType => DatabaseType.MySQL;

        private static readonly Dictionary<string, SqlStatementType> StatementTypePatterns = new Dictionary<string, SqlStatementType>
        {
            { @"^\s*SELECT\s+", SqlStatementType.Select },
            { @"^\s*INSERT\s+", SqlStatementType.Insert },
            { @"^\s*UPDATE\s+", SqlStatementType.Update },
            { @"^\s*DELETE\s+", SqlStatementType.Delete },
            { @"^\s*REPLACE\s+", SqlStatementType.Insert }, // MySQL specific
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
            var trimmedSql = sql.Trim();

            // Check for unclosed quotes (considering MySQL escape sequences)
            if (!IsMySQLQuotesBalanced(trimmedSql))
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
                if (!IsValidMySQLStatement(trimmedSql))
                {
                    errors.Add("Unknown or invalid SQL statement");
                }
            }

            // MySQL specific syntax validation
            ValidateMySQLSpecific(trimmedSql, errors);

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
            trimmedSql = RemoveMySQLComments(trimmedSql);

            // Patterns to extract table names
            var patterns = new[]
            {
                @"FROM\s+`?([a-zA-Z_][a-zA-Z0-9_]*)`?(?:\s+AS\s+\w+)?",
                @"JOIN\s+`?([a-zA-Z_][a-zA-Z0-9_]*)`?(?:\s+AS\s+\w+)?",
                @"INTO\s+`?([a-zA-Z_][a-zA-Z0-9_]*)`?",
                @"UPDATE\s+`?([a-zA-Z_][a-zA-Z0-9_]*)`?",
                @"TABLE\s+`?([a-zA-Z_][a-zA-Z0-9_]*)`?"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(trimmedSql, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var tableName = match.Groups[1].Value;
                        tableNames.Add(tableName);
                    }
                }
            }

            return tableNames.ToArray();
        }

        private bool IsMySQLQuotesBalanced(string sql)
        {
            var singleQuoteCount = 0;
            var doubleQuoteCount = 0;
            var backtickCount = 0;
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

                switch (sql[i])
                {
                    case '\'':
                        // MySQL allows '' for escaping
                        if (i + 1 < sql.Length && sql[i + 1] == '\'')
                        {
                            i++;
                            continue;
                        }
                        singleQuoteCount++;
                        break;
                    case '"':
                        doubleQuoteCount++;
                        break;
                    case '`':
                        backtickCount++;
                        break;
                }
            }

            return singleQuoteCount % 2 == 0 && doubleQuoteCount % 2 == 0 && backtickCount % 2 == 0;
        }

        private bool IsParenthesesBalanced(string sql)
        {
            var count = 0;
            var inSingleQuote = false;
            var inDoubleQuote = false;
            var inBacktick = false;

            for (int i = 0; i < sql.Length; i++)
            {
                if (!inDoubleQuote && !inBacktick)
                {
                    if (sql[i] == '\'' && (i == 0 || sql[i - 1] != '\\'))
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == '\'')
                        {
                            i++;
                            continue;
                        }
                        inSingleQuote = !inSingleQuote;
                    }
                }

                if (!inSingleQuote && !inBacktick && sql[i] == '"' && (i == 0 || sql[i - 1] != '\\'))
                {
                    inDoubleQuote = !inDoubleQuote;
                }

                if (!inSingleQuote && !inDoubleQuote && sql[i] == '`')
                {
                    inBacktick = !inBacktick;
                }

                if (!inSingleQuote && !inDoubleQuote && !inBacktick)
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

        private bool IsValidMySQLStatement(string sql)
        {
            var validPatterns = new[]
            {
                @"^\s*SET\s+",
                @"^\s*SHOW\s+",
                @"^\s*DESCRIBE\s+",
                @"^\s*DESC\s+",
                @"^\s*EXPLAIN\s+",
                @"^\s*USE\s+",
                @"^\s*START\s+TRANSACTION",
                @"^\s*BEGIN\s*",
                @"^\s*COMMIT\s*",
                @"^\s*ROLLBACK\s*",
                @"^\s*LOCK\s+TABLES",
                @"^\s*UNLOCK\s+TABLES",
                @"^\s*GRANT\s+",
                @"^\s*REVOKE\s+",
                @"^\s*FLUSH\s+",
                @"^\s*CREATE\s+(DATABASE|USER|VIEW|PROCEDURE|FUNCTION|TRIGGER|EVENT)\s+",
                @"^\s*DROP\s+(DATABASE|USER|VIEW|PROCEDURE|FUNCTION|TRIGGER|EVENT)\s+",
                @"^\s*ALTER\s+(DATABASE|USER|VIEW|PROCEDURE|FUNCTION|EVENT)\s+",
                @"^\s*CALL\s+",
                @"^\s*LOAD\s+DATA\s+",
                @"^\s*HANDLER\s+"
            };

            return validPatterns.Any(pattern => Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase));
        }

        private void ValidateMySQLSpecific(string sql, List<string> errors)
        {
            // Check for MySQL specific syntax issues

            // LIMIT without ORDER BY warning (not an error, but could be problematic)
            if (Regex.IsMatch(sql, @"\sLIMIT\s+\d+", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(sql, @"\sORDER\s+BY\s+", RegexOptions.IgnoreCase))
            {
                // This is actually valid in MySQL, so we don't add an error
            }

            // Check for ON DUPLICATE KEY UPDATE in non-INSERT statements
            if (Regex.IsMatch(sql, @"\sON\s+DUPLICATE\s+KEY\s+UPDATE\s+", RegexOptions.IgnoreCase))
            {
                var statementType = GetStatementType(sql);
                if (statementType != SqlStatementType.Insert)
                {
                    errors.Add("ON DUPLICATE KEY UPDATE can only be used with INSERT statements");
                }
            }

            // Check for backtick balance (MySQL identifier quotes)
            var backtickCount = sql.Count(c => c == '`');
            if (backtickCount % 2 != 0)
            {
                errors.Add("Unclosed backtick (`) identifier quote");
            }

            // Check for MySQL comment syntax
            if (sql.Contains("/*") && !sql.Contains("*/"))
            {
                errors.Add("Unclosed multi-line comment");
            }

            // Validate AUTO_INCREMENT usage
            if (Regex.IsMatch(sql, @"\sAUTO_INCREMENT\s*=", RegexOptions.IgnoreCase))
            {
                var statementType = GetStatementType(sql);
                if (statementType != SqlStatementType.CreateTable && statementType != SqlStatementType.AlterTable)
                {
                    errors.Add("AUTO_INCREMENT can only be used in CREATE TABLE or ALTER TABLE statements");
                }
            }
        }

        private string RemoveMySQLComments(string sql)
        {
            // Remove -- comments
            sql = Regex.Replace(sql, @"--[^\r\n]*", "");
            
            // Remove # comments (MySQL specific)
            sql = Regex.Replace(sql, @"#[^\r\n]*", "");
            
            // Remove /* */ comments
            sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);
            
            return sql;
        }
    }
}