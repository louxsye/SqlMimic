using System;
using System.Linq;
using Xunit;
using SqlMimic.Core.Abstractions;
using SqlMimic.SqlServer;

namespace SqlMimic.Tests
{
    public class SqlSyntaxValidatorTests
    {
        private readonly ISqlSyntaxValidator _validator;

        public SqlSyntaxValidatorTests()
        {
            _validator = new SqlServerSyntaxValidator();
        }
        [Fact]
        public void ValidateSyntax_ValidSelectStatement_ReturnsValid()
        {
            // Arrange
            var sql = @"
SELECT 
    Foo
FROM 
    Bar WITH(NOLOCK)
WHERE 
    StartDate <= @baseDate
AND 
    @baseDate < EndDate";

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateSyntax_ValidInsertStatement_ReturnsValid()
        {
            // Arrange
            var sql = @"
INSERT INTO Bar(
    Foo
)
VALUES(
    @entryDate
)";

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateSyntax_ValidUpdateStatement_ReturnsValid()
        {
            // Arrange
            var sql = @"
UPDATE Users 
SET 
    Name = @name,
    UpdatedAt = GETDATE()
WHERE 
    Id = @id";

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateSyntax_ValidDeleteStatement_ReturnsValid()
        {
            // Arrange
            var sql = "DELETE FROM Users WHERE Id = @id AND Status = 'Inactive'";

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateSyntax_ValidMergeStatement_ReturnsValid()
        {
            // Arrange
            var sql = @"
MERGE INTO Users AS target
USING(
    SELECT
        @id AS Id,
        @name AS Name,
        @email AS Email
) AS source ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT(Id, Name, Email)
    VALUES(source.Id, source.Name, source.Email);";

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateSyntax_IncompleteSqlStatement_ReturnsInvalid()
        {
            // Arrange
            var sql = "SELECT * FROM"; // Incomplete SQL

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Line", result.Errors[0]);
            Assert.Contains("Column", result.Errors[0]);
        }

        [Fact]
        public void ValidateSyntax_EmptyString_ReturnsInvalid()
        {
            // Arrange
            var sql = "";

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("SQL statement is empty", result.Errors[0]);
        }

        [Fact]
        public void ValidateSyntax_NullString_ReturnsInvalid()
        {
            // Arrange
            string? sql = null;

            // Act
            var result = _validator.ValidateSyntax(sql!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("SQL statement is empty", result.Errors[0]);
        }

        [Fact]
        public void ValidateSyntax_WhitespaceOnlyString_ReturnsInvalid()
        {
            // Arrange
            var sql = "   \t\n  ";

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("SQL statement is empty", result.Errors[0]);
        }

        [Fact]
        public void ValidateSyntax_SyntaxErrorWithLineAndColumn_ReturnsDetailedError()
        {
            // Arrange
            var sql = @"
SELECT Name, 
FROM Users"; // No column after comma

            // Act
            var result = _validator.ValidateSyntax(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Line", result.Errors[0]);
            Assert.Contains("Column", result.Errors[0]);
        }

        [Fact]
        public void GetStatementType_SelectStatement_ReturnsSelect()
        {
            // Arrange
            var sql = "SELECT * FROM Users";

            // Act
            var statementType = _validator.GetStatementType(sql);

            // Assert
            Assert.Equal(SqlStatementType.Select, statementType);
        }

        [Fact]
        public void GetStatementType_InsertStatement_ReturnsInsert()
        {
            // Arrange
            var sql = "INSERT INTO Users (Name) VALUES ('Test')";

            // Act
            var statementType = _validator.GetStatementType(sql);

            // Assert
            Assert.Equal(SqlStatementType.Insert, statementType);
        }

        [Fact]
        public void GetStatementType_UpdateStatement_ReturnsUpdate()
        {
            // Arrange
            var sql = "UPDATE Users SET Name = 'Test' WHERE Id = 1";

            // Act
            var statementType = _validator.GetStatementType(sql);

            // Assert
            Assert.Equal(SqlStatementType.Update, statementType);
        }

        [Fact]
        public void GetStatementType_DeleteStatement_ReturnsDelete()
        {
            // Arrange
            var sql = "DELETE FROM Users WHERE Id = 1";

            // Act
            var statementType = _validator.GetStatementType(sql);

            // Assert
            Assert.Equal(SqlStatementType.Delete, statementType);
        }

        [Fact]
        public void GetStatementType_MergeStatement_ReturnsMerge()
        {
            // Arrange
            var sql = @"
MERGE Users AS target
USING (SELECT 1 AS Id, 'Test' AS Name) AS source
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET Name = source.Name;";

            // Act
            var statementType = _validator.GetStatementType(sql);

            // Assert
            Assert.Equal(SqlStatementType.Merge, statementType);
        }

        [Fact]
        public void GetStatementType_InvalidSql_ReturnsUnknown()
        {
            // Arrange
            var sql = "This is not valid SQL";

            // Act
            var statementType = _validator.GetStatementType(sql);

            // Assert
            Assert.Equal(SqlStatementType.Unknown, statementType);
        }

        [Fact]
        public void GetStatementType_EmptySql_ReturnsUnknown()
        {
            // Arrange
            var sql = "";

            // Act
            var statementType = _validator.GetStatementType(sql);

            // Assert
            Assert.Equal(SqlStatementType.Unknown, statementType);
        }

        [Fact]
        public void ExtractTableNames_SingleTable_ReturnsCorrectTableName()
        {
            // Arrange
            var sql = "SELECT * FROM SalesTax";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Single(tableNames);
            Assert.Contains("SalesTax", tableNames);
        }

        [Fact]
        public void ExtractTableNames_MultipleTables_ReturnsAllTableNames()
        {
            // Arrange
            var sql = @"
SELECT u.Name, p.Title 
FROM Users u 
INNER JOIN Posts p ON u.Id = p.UserId";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Equal(2, tableNames.Length);
            Assert.Contains("Users", tableNames);
            Assert.Contains("Posts", tableNames);
        }

        [Fact]
        public void ExtractTableNames_TableWithNolock_ReturnsTableName()
        {
            // Arrange
            var sql = "SELECT * FROM SalesTax WITH(NOLOCK)";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Single(tableNames);
            Assert.Contains("SalesTax", tableNames);
        }

        [Fact]
        public void ExtractTableNames_DuplicateTables_ReturnsUniqueTables()
        {
            // Arrange
            var sql = @"
SELECT * FROM Users u1
INNER JOIN Users u2 ON u1.ParentId = u2.Id";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Single(tableNames);
            Assert.Contains("Users", tableNames);
        }

        [Fact]
        public void ExtractTableNames_ComplexQuery_ReturnsAllTables()
        {
            // Arrange
            var sql = @"
WITH CTE AS (
    SELECT Id FROM Orders
)
SELECT u.Name, p.ProductName, o.OrderDate
FROM Users u
INNER JOIN Orders o ON u.Id = o.UserId
INNER JOIN OrderItems oi ON o.Id = oi.OrderId
INNER JOIN Products p ON oi.ProductId = p.Id
WHERE o.Id IN (SELECT Id FROM CTE)";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Equal(5, tableNames.Length);
            Assert.Contains("Orders", tableNames);
            Assert.Contains("Users", tableNames);
            Assert.Contains("OrderItems", tableNames);
            Assert.Contains("Products", tableNames);
        }

        [Fact]
        public void ExtractTableNames_InvalidSql_ReturnsEmptyArray()
        {
            // Arrange
            var sql = "This is not valid SQL";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Empty(tableNames);
        }

        [Fact]
        public void ExtractTableNames_EmptySql_ReturnsEmptyArray()
        {
            // Arrange
            var sql = "";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Empty(tableNames);
        }

        [Fact]
        public void ExtractTableNames_InsertStatement_ReturnsTableName()
        {
            // Arrange
            var sql = "INSERT INTO Products (Name, Price) VALUES ('Test', 100)";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Single(tableNames);
            Assert.Contains("Products", tableNames);
        }

        [Fact]
        public void ExtractTableNames_UpdateStatement_ReturnsTableName()
        {
            // Arrange
            var sql = "UPDATE Inventory SET Quantity = 10 WHERE ProductId = 1";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Single(tableNames);
            Assert.Contains("Inventory", tableNames);
        }

        [Fact]
        public void ExtractTableNames_DeleteStatement_ReturnsTableName()
        {
            // Arrange
            var sql = "DELETE FROM Orders WHERE CreatedAt < '2023-01-01'";

            // Act
            var tableNames = _validator.ExtractTableNames(sql);

            // Assert
            Assert.Single(tableNames);
            Assert.Contains("Orders", tableNames);
        }
    }
}