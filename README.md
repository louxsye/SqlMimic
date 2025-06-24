# SqlMimic

[![Tests](https://img.shields.io/badge/tests-passing-brightgreen.svg)]()
[![.NET](https://img.shields.io/badge/.NET-Framework%204.6.2%2B%20%7C%208.0%20%7C%209.0-blue.svg)]()
[![License](https://img.shields.io/badge/license-Apache%202.0-green.svg)]()

**SqlMimic** is a comprehensive .NET library for mocking SQL database operations and validating SQL syntax in unit tests. It provides in-memory implementations of `DbConnection`, `DbCommand`, and `DbDataReader`, plus multi-database SQL syntax validation capabilities.

## Features

### Database Mocking
✅ **Database Connection Mocking** - Mock `DbConnection` for testing  
✅ **Command Execution Tracking** - Track all executed SQL commands and parameters  
✅ **Multi-row/Multi-column Support** - Return complex result sets from queries  
✅ **Transaction Support** - Mock database transactions  
✅ **Dapper Compatible** - Full compatibility with Dapper ORM  

### Multi-Database SQL Syntax Validation
✅ **SQL Server** - Full syntax validation using Microsoft SQL Server parser  
✅ **PostgreSQL** - PostgreSQL-specific syntax validation  
✅ **MySQL** - MySQL/MariaDB syntax validation  
✅ **SQLite** - SQLite syntax validation  
✅ **Statement Type Detection** - Identify SELECT, INSERT, UPDATE, DELETE, MERGE statements  
✅ **Table Name Extraction** - Extract table names from SQL queries  

## Installation

Choose the packages you need:

```bash
# Core package - Database mocking capabilities
dotnet add package SqlMimic

# SQL syntax validation packages (choose your database)
dotnet add package SqlMimic.SqlServer      # For SQL Server
dotnet add package SqlMimic.PostgreSQL    # For PostgreSQL
dotnet add package SqlMimic.MySQL         # For MySQL/MariaDB  
dotnet add package SqlMimic.SQLite        # For SQLite

# Or add the abstractions package for interface-based development
dotnet add package SqlMimic.Core.Abstractions
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

### 6. Multi-Database SQL Syntax Validation

```csharp
using SqlMimic.SqlServer;
using SqlMimic.PostgreSQL;
using SqlMimic.MySQL;
using SqlMimic.SQLite;

[Fact]
public void ValidateSyntax_SqlServer_ValidSelectStatement()
{
    // Arrange
    var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SqlServer);
    var sql = @"
    SELECT Id, Name, Email 
    FROM Users 
    WHERE IsActive = @isActive 
    ORDER BY Name";

    // Act
    var result = validator.ValidateSyntax(sql);

    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
}

[Fact]
public void ValidateSyntax_PostgreSQL_WithReturningClause()
{
    // Arrange
    var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.PostgreSQL);
    var sql = @"
    INSERT INTO Users (Name, Email) 
    VALUES ('John', 'john@example.com') 
    RETURNING Id, Name";

    // Act
    var result = validator.ValidateSyntax(sql);

    // Assert
    Assert.True(result.IsValid);
}

[Fact]
public void ValidateSyntax_MySQL_OnDuplicateKeyUpdate()
{
    // Arrange
    var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.MySQL);
    var sql = @"
    INSERT INTO Users (Id, Name, Email) 
    VALUES (1, 'John', 'john@example.com') 
    ON DUPLICATE KEY UPDATE 
    Name = VALUES(Name), Email = VALUES(Email)";

    // Act
    var result = validator.ValidateSyntax(sql);

    // Assert
    Assert.True(result.IsValid);
}

[Fact]
public void ValidateSyntax_SQLite_LimitedAlterTable()
{
    // Arrange
    var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SQLite);
    var sql = "ALTER TABLE Users ADD COLUMN IsActive INTEGER DEFAULT 1";

    // Act
    var result = validator.ValidateSyntax(sql);

    // Assert
    Assert.True(result.IsValid);
}

[Fact]
public void GetStatementType_AcrossDatabases()
{
    // Arrange
    var validators = new[]
    {
        SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SqlServer),
        SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.PostgreSQL),
        SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.MySQL),
        SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SQLite)
    };
    var sql = "SELECT * FROM Users";

    // Act & Assert
    foreach (var validator in validators)
    {
        var statementType = validator.GetStatementType(sql);
        Assert.Equal(SqlStatementType.Select, statementType);
    }
}
```

### Legacy SqlSyntaxValidator (Backward Compatibility)

```csharp
[Fact]
public void LegacyValidator_StillWorks()
{
    // This uses SQL Server validator by default
    var result = SqlSyntaxValidator.ValidateSyntax("SELECT * FROM Users");
    Assert.True(result.IsValid);
    
    var statementType = SqlSyntaxValidator.GetStatementType("INSERT INTO Users VALUES (1, 'Test')");
    Assert.Equal(SqlStatementType.Insert, statementType);
    
    var tableNames = SqlSyntaxValidator.ExtractTableNames("SELECT * FROM Users u JOIN Posts p ON u.Id = p.UserId");
    Assert.Contains("Users", tableNames);
    Assert.Contains("Posts", tableNames);
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

### ISqlSyntaxValidator & SqlSyntaxValidatorFactory

Multi-database SQL syntax validation system.

**ISqlSyntaxValidator Interface:**
- `ValidateSyntax(string sql)` - Validate SQL syntax and return detailed results
- `GetStatementType(string sql)` - Identify the type of SQL statement  
- `ExtractTableNames(string sql)` - Extract all table names referenced in the query
- `DatabaseType` - Get the database type this validator supports

**SqlSyntaxValidatorFactory Methods:**
- `CreateValidator(DatabaseType databaseType)` - Create validator for specific database
- Available database types: `SqlServer`, `PostgreSQL`, `MySQL`, `SQLite`

**Legacy SqlSyntaxValidator (Backward Compatibility):**
- Static methods that use SQL Server validator by default
- `ValidateSyntax(string sql)`, `GetStatementType(string sql)`, `ExtractTableNames(string sql)`

## Framework Compatibility

- **.NET Framework 4.6.2+** - Full support for legacy applications
- **.NET 8.0** - LTS (Long Term Support) version
- **.NET 9.0** - Latest stable version
- **Dapper** - Full compatibility with Dapper ORM
- **Entity Framework Core** - Compatible with raw SQL queries
- **Any test framework** - MSTest, NUnit, xUnit, etc.

## License

Apache-2.0 - see LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.