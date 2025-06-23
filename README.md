# SqlMimic

[![Tests](https://img.shields.io/badge/tests-passing-brightgreen.svg)]()
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)]()

**SqlMimic** is a lightweight .NET library for mocking SQL database operations in unit tests. It provides in-memory implementations of `DbConnection`, `DbCommand`, and `DbDataReader` that allow you to test your data access code without requiring an actual database connection.

## Features

✅ **Database Connection Mocking** - Mock `DbConnection` for testing  
✅ **Command Execution Tracking** - Track all executed SQL commands and parameters  
✅ **Multi-row/Multi-column Support** - Return complex result sets from queries  
✅ **Transaction Support** - Mock database transactions  
✅ **Dapper Compatible** - Full compatibility with Dapper ORM  
✅ **SQL Syntax Validation** - Validate SQL syntax using Microsoft SQL Server parser  
✅ **Statement Type Detection** - Identify SELECT, INSERT, UPDATE, DELETE, MERGE statements  
✅ **Table Name Extraction** - Extract table names from SQL queries

## Installation

```bash
# Package will be available on NuGet
dotnet add package SqlMimic
```

## Quick Start

### Basic Usage with ADO.NET

```csharp
using SqlMimic.Core;
using Xunit;

[Fact]
public async Task GetUser_ReturnsCorrectUser()
{
    // Arrange
    var connection = new MimicConnection();
    connection.SetupMockData(
        new[] { "Id", "Name", "Email", "IsActive" },
        new object[] { 1, "John Doe", "john@example.com", true }
    );
    
    var repository = new UserRepository(connection);

    // Act
    var user = await repository.GetByIdAsync(1);

    // Assert
    Assert.NotNull(user);
    Assert.Equal("John Doe", user.Name);
    Assert.Equal("john@example.com", user.Email);
    
    // Verify SQL execution
    Assert.Single(connection.Commands);
    Assert.Contains("SELECT", connection.Commands[0].CommandText);
}
```

### Dapper Integration

```csharp
using Dapper;
using SqlMimic.Core;
using Xunit;

[Fact]
public async Task DapperQuery_ReturnsMultipleUsers()
{
    // Arrange
    var connection = new MimicConnection();
    connection.SetupMockData(
        new[] { "Id", "Name", "Email", "IsActive" },
        new object[] { 1, "Alice", "alice@example.com", true },
        new object[] { 2, "Bob", "bob@example.com", true },
        new object[] { 3, "Charlie", "charlie@example.com", false }
    );

    // Act
    var users = await connection.QueryAsync<User>(
        "SELECT Id, Name, Email, IsActive FROM Users WHERE IsActive = @isActive",
        new { isActive = true }
    );

    // Assert
    Assert.Equal(3, users.Count());
    Assert.All(users, u => Assert.True(u.IsActive));
}
```

## Use Cases & Examples

### 1. Testing Repository Methods

**Testing a Get operation:**

```csharp
[Fact]
public async Task GetByIdAsync_ValidId_ReturnsUser()
{
    // Arrange
    var connection = new MimicConnection();
    var createdAt = DateTime.Now;
    connection.SetupMockData(
        new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
        new object[] { 1, "John Doe", "john@example.com", createdAt, true }
    );
    
    var repository = new UserRepository(connection);

    // Act
    var user = await repository.GetByIdAsync(1);

    // Assert
    Assert.NotNull(user);
    Assert.Equal(1, user.Id);
    Assert.Equal("John Doe", user.Name);
    Assert.Equal(createdAt, user.CreatedAt);

    // Verify SQL and parameters
    Assert.Single(connection.Commands);
    var command = connection.Commands[0];
    Assert.Contains("WHERE Id = @id", command.CommandText);
    Assert.Equal(1, command.Parameters.Cast<DbParameter>().First().Value);
}
```

**Testing when no data is found:**

```csharp
[Fact]
public async Task GetByIdAsync_InvalidId_ReturnsNull()
{
    // Arrange
    var connection = new MimicConnection();
    connection.SetupMockData(
        new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" }
        // No rows - empty result set
    );
    
    var repository = new UserRepository(connection);

    // Act
    var user = await repository.GetByIdAsync(999);

    // Assert
    Assert.Null(user);
}
```

### 2. Testing Insert/Update Operations

```csharp
[Fact]
public async Task CreateAsync_ValidUser_ReturnsNewId()
{
    // Arrange
    var connection = new MimicConnection();
    connection.MockReturnValue = 123; // Simulated new ID
    
    var repository = new UserRepository(connection);
    var user = new User
    {
        Name = "Jane Doe",
        Email = "jane@example.com",
        IsActive = true
    };

    // Act
    var newId = await repository.CreateAsync(user);

    // Assert
    Assert.Equal(123, newId);
    
    // Verify SQL execution
    var command = connection.Commands[0];
    Assert.Contains("INSERT INTO Users", command.CommandText);
    Assert.Contains("@name", command.CommandText);
    Assert.Contains("@email", command.CommandText);
}
```

### 3. Testing Multiple Results

```csharp
[Fact]
public async Task GetAllActiveAsync_ReturnsMultipleUsers()
{
    // Arrange
    var connection = new MimicConnection();
    var now = DateTime.Now;
    connection.SetupMockData(
        new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
        new object[] { 1, "Alice", "alice@example.com", now, true },
        new object[] { 2, "Bob", "bob@example.com", now.AddDays(-1), true },
        new object[] { 3, "Charlie", "charlie@example.com", now.AddDays(-2), true }
    );
    
    var repository = new UserRepository(connection);

    // Act
    var users = await repository.GetAllActiveAsync();
    var userList = users.ToList();

    // Assert
    Assert.Equal(3, userList.Count);
    Assert.All(userList, u => Assert.True(u.IsActive));
    Assert.Contains(userList, u => u.Name == "Alice");
    Assert.Contains(userList, u => u.Name == "Bob");
    Assert.Contains(userList, u => u.Name == "Charlie");
}
```

### 4. Testing Transaction Behavior

```csharp
[Fact]
public async Task TransactionTest_CommitBehavior()
{
    // Arrange
    var connection = new MimicConnection();
    var repository = new UserRepository(connection);

    // Act
    using var transaction = connection.BeginTransaction();
    var user = new User { Name = "Test User", Email = "test@example.com" };
    await repository.CreateAsync(user);
    transaction.Commit();

    // Assert
    Assert.Single(connection.Transactions);
    var txn = connection.Transactions[0];
    Assert.True(txn.IsCommitted);
    Assert.False(txn.IsRolledBack);
    
    // Verify command was part of transaction
    var command = connection.Commands[0];
    Assert.NotNull(command.UsedTransaction);
}
```

### 5. Testing with Sequential Return Values

```csharp
[Fact]
public async Task SequentialOperations_DifferentResults()
{
    // Arrange
    var connection = new MimicConnection();
    
    // Setup different results for sequential calls
    connection.SetupSequentialReturnValues(1, 2, 3);
    var repository = new UserRepository(connection);

    // Act
    var id1 = await repository.GetUserCountAsync();
    var id2 = await repository.GetUserCountAsync();
    var id3 = await repository.GetUserCountAsync();

    // Assert
    Assert.Equal(1, id1);
    Assert.Equal(2, id2);
    Assert.Equal(3, id3);
    Assert.Equal(3, connection.Commands.Count);
}
```

### 6. SQL Syntax Validation

```csharp
[Fact]
public void ValidateSyntax_ValidSelectStatement_ReturnsValid()
{
    // Arrange
    var sql = @"
    SELECT Id, Name, Email 
    FROM Users 
    WHERE IsActive = @isActive 
    ORDER BY Name";

    // Act
    var result = SqlSyntaxValidator.ValidateSyntax(sql);

    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
    Assert.NotNull(result.ParsedFragment);
}

[Fact]
public void GetStatementType_SelectQuery_ReturnsSelect()
{
    // Arrange
    var sql = "SELECT * FROM Users";

    // Act
    var statementType = SqlSyntaxValidator.GetStatementType(sql);

    // Assert
    Assert.Equal(SqlStatementType.Select, statementType);
}

[Fact]
public void ExtractTableNames_ComplexQuery_ReturnsAllTables()
{
    // Arrange
    var sql = @"
    SELECT u.Name, p.Title 
    FROM Users u 
    JOIN Posts p ON u.Id = p.UserId 
    WHERE u.IsActive = 1";

    // Act
    var tableNames = SqlSyntaxValidator.ExtractTableNames(sql);

    // Assert
    Assert.Contains("Users", tableNames);
    Assert.Contains("Posts", tableNames);
    Assert.Equal(2, tableNames.Length);
}
```

## API Reference

### MimicConnection

The main mock connection class that implements `DbConnection`.

**Key Methods:**
- `SetupMockData(string[] columnNames, params object[][] rows)` - Setup multi-row/column data
- `SetupDefaultMockData(string[] columnNames, params object[][] rows)` - Default data for all commands
- `SetupSequentialReturnValues(params object[] values)` - Different return values for sequential calls
- `SetupSequentialHasRowsValues(params bool[] values)` - Different HasRows values for sequential calls

**Properties:**
- `Commands` - Read-only list of executed commands
- `Transactions` - Read-only list of transactions
- `MockReturnValue` - Single return value for simple scenarios
- `MockHasRows` - Whether queries should return rows

### MimicCommand

Mock implementation of `DbCommand` that tracks execution.

**Static Methods:**
- `ClearExecutedCommands()` - Clear execution history
- `ExecutedCommands` - Static list of all executed commands

### MimicDataReader

Mock implementation of `DbDataReader` supporting both single values and multi-row/column data.

**Constructors:**
- `MimicDataReader(object? returnValue, bool hasRows)` - Single value mode
- `MimicDataReader(string[] columnNames, List<object[]> data)` - Multi-row mode

### SqlSyntaxValidator

Utility class for SQL syntax validation and analysis.

**Methods:**
- `ValidateSyntax(string sql)` - Validate SQL syntax and return detailed results
- `GetStatementType(string sql)` - Identify the type of SQL statement
- `ExtractTableNames(string sql)` - Extract all table names referenced in the query

## Framework Compatibility

- **.NET Framework 4.6.2** (Legacy support)
- **.NET 8.0** (LTS)
- **.NET 9.0** (Latest)
- **Dapper** - Full compatibility with Dapper ORM
- **Entity Framework Core** - Compatible with raw SQL queries
- **Xunit** - Used in examples, but works with any test framework

## License

Apache-2.0 - see LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.