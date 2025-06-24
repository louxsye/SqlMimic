using SqlMimic.Core.Abstractions;
using SqlMimic.SqlServer;
using SqlMimic.PostgreSQL;
using SqlMimic.MySQL;
using SqlMimic.SQLite;

namespace SqlMimic.Tests.Integration
{
    public class DatabaseValidatorTests
    {
        [Theory]
        [InlineData(typeof(SqlServerSyntaxValidator), DatabaseType.SqlServer)]
        [InlineData(typeof(PostgreSQLSyntaxValidator), DatabaseType.PostgreSQL)]
        [InlineData(typeof(MySQLSyntaxValidator), DatabaseType.MySQL)]
        [InlineData(typeof(SQLiteSyntaxValidator), DatabaseType.SQLite)]
        public void Validator_DatabaseType_ReturnsCorrectType(Type validatorType, DatabaseType expectedType)
        {
            // Arrange & Act
            var validator = (ISqlSyntaxValidator)Activator.CreateInstance(validatorType)!;
            
            // Assert
            Assert.Equal(expectedType, validator.DatabaseType);
        }

        [Theory]
        [InlineData("SELECT * FROM users")]
        [InlineData("INSERT INTO users (name, email) VALUES ('John', 'john@example.com')")]
        [InlineData("UPDATE users SET name = 'Jane' WHERE id = 1")]
        [InlineData("DELETE FROM users WHERE id = 1")]
        public void AllValidators_ValidSQL_ReturnsValid(string sql)
        {
            // Arrange
            var validators = new ISqlSyntaxValidator[]
            {
                new SqlServerSyntaxValidator(),
                new PostgreSQLSyntaxValidator(),
                new MySQLSyntaxValidator(),
                new SQLiteSyntaxValidator()
            };

            foreach (var validator in validators)
            {
                // Act
                var result = validator.ValidateSyntax(sql);

                // Assert
                Assert.True(result.IsValid, 
                    $"SQL validation failed for {validator.DatabaseType}: {string.Join(", ", result.Errors)}");
            }
        }

        [Theory]
        [InlineData("SELECT * FROM users", SqlStatementType.Select)]
        [InlineData("INSERT INTO users (name) VALUES ('John')", SqlStatementType.Insert)]
        [InlineData("UPDATE users SET name = 'Jane'", SqlStatementType.Update)]
        [InlineData("DELETE FROM users WHERE id = 1", SqlStatementType.Delete)]
        public void AllValidators_StatementTypes_DetectedCorrectly(string sql, SqlStatementType expectedType)
        {
            // Arrange
            var validators = new ISqlSyntaxValidator[]
            {
                new SqlServerSyntaxValidator(),
                new PostgreSQLSyntaxValidator(),
                new MySQLSyntaxValidator(),
                new SQLiteSyntaxValidator()
            };

            foreach (var validator in validators)
            {
                // Act
                var statementType = validator.GetStatementType(sql);

                // Assert
                Assert.Equal(expectedType, statementType);
            }
        }

        [Theory]
        [InlineData("SELECT * FROM users", "users")]
        [InlineData("INSERT INTO customers (name) VALUES ('John')", "customers")]
        [InlineData("UPDATE products SET price = 100", "products")]
        [InlineData("DELETE FROM orders WHERE id = 1", "orders")]
        public void AllValidators_ExtractTableNames_WorksCorrectly(string sql, string expectedTable)
        {
            // Arrange
            var validators = new ISqlSyntaxValidator[]
            {
                new SqlServerSyntaxValidator(),
                new PostgreSQLSyntaxValidator(),
                new MySQLSyntaxValidator(),
                new SQLiteSyntaxValidator()
            };

            foreach (var validator in validators)
            {
                // Act
                var tableNames = validator.ExtractTableNames(sql);

                // Assert
                Assert.Contains(expectedTable, tableNames);
            }
        }

        [Fact]
        public void SqlServer_SpecificFeatures_WorkCorrectly()
        {
            // Arrange
            var validator = new SqlServerSyntaxValidator();
            
            // Test SQL Server specific syntax
            var withNoLockSql = "SELECT * FROM users WITH(NOLOCK)";
            var result = validator.ValidateSyntax(withNoLockSql);
            
            // Assert
            Assert.True(result.IsValid, 
                $"SQL Server specific syntax failed: {string.Join(", ", result.Errors)}");
        }

        [Fact]
        public void MySQL_OnDuplicateKeyUpdate_IsSupported()
        {
            // Arrange
            var validator = new MySQLSyntaxValidator();
            var sql = "INSERT INTO users (name, email) VALUES ('John', 'john@test.com') ON DUPLICATE KEY UPDATE name = VALUES(name)";

            // Act
            var result = validator.ValidateSyntax(sql);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void PostgreSQL_ReturningClause_IsSupported()
        {
            // Arrange
            var validator = new PostgreSQLSyntaxValidator();
            var sql = "INSERT INTO users (name) VALUES ('John') RETURNING id";

            // Act
            var result = validator.ValidateSyntax(sql);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void SQLite_TruncateStatement_IsNotSupported()
        {
            // Arrange
            var validator = new SQLiteSyntaxValidator();
            var sql = "TRUNCATE TABLE users";

            // Act
            var result = validator.ValidateSyntax(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Any(e => e.StartsWith("TRUNCATE is not supported in SQLite")));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void AllValidators_EmptySQL_ReturnsInvalid(string? sql)
        {
            // Arrange
            var validators = new ISqlSyntaxValidator[]
            {
                new SqlServerSyntaxValidator(),
                new PostgreSQLSyntaxValidator(),
                new MySQLSyntaxValidator(),
                new SQLiteSyntaxValidator()
            };

            foreach (var validator in validators)
            {
                // Act
                var result = validator.ValidateSyntax(sql ?? "");

                // Assert
                Assert.False(result.IsValid);
                Assert.Contains("SQL statement is empty", result.Errors);
            }
        }

        [Theory]
        [InlineData("SELECT * FROM 'unclosed")]
        [InlineData("SELECT * FROM (unclosed")]
        public void AllValidators_InvalidSQL_ReturnsInvalid(string sql)
        {
            // Arrange
            var validators = new ISqlSyntaxValidator[]
            {
                new SqlServerSyntaxValidator(),
                new PostgreSQLSyntaxValidator(),
                new MySQLSyntaxValidator(),
                new SQLiteSyntaxValidator()
            };

            foreach (var validator in validators)
            {
                // Act
                var result = validator.ValidateSyntax(sql);

                // Assert
                Assert.False(result.IsValid, 
                    $"Expected validation to fail for {validator.DatabaseType} but it passed");
            }
        }
    }
}