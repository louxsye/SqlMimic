# SqlMimic.Core.Abstractions

[![.NET](https://img.shields.io/badge/.NET-Framework%204.6.2%2B%20%7C%208.0%20%7C%209.0-blue.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)]()

Core abstractions and interfaces for SqlMimic's multi-database SQL syntax validation system. This package provides the foundational interfaces and types for building database-specific SQL validators.

## Features

✅ **Database-Agnostic Interfaces** - Core contracts for SQL syntax validation  
✅ **Extensible Architecture** - Build custom validators for any database  
✅ **Factory Pattern Support** - Centralized validator creation  
✅ **Standardized Results** - Consistent validation result types  
✅ **Multi-Database Support** - Built-in support for SQL Server, PostgreSQL, MySQL, SQLite  

## Installation

```bash
dotnet add package SqlMimic.Core.Abstractions
```

This package is automatically included when you install any database-specific SqlMimic validation package.

## Core Interfaces

### ISqlSyntaxValidator

The main interface for SQL syntax validation:

```csharp
public interface ISqlSyntaxValidator
{
    DatabaseType DatabaseType { get; }
    SqlValidationResult ValidateSyntax(string sql);
    SqlStatementType GetStatementType(string sql);
    string[] ExtractTableNames(string sql);
}
```

### Usage Example

```csharp
using SqlMimic.Core.Abstractions;

// Use with any database-specific implementation
ISqlSyntaxValidator validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SqlServer);

var sql = "SELECT * FROM Users WHERE IsActive = 1";

// Validate syntax
var result = validator.ValidateSyntax(sql);
if (result.IsValid)
{
    // Get statement type
    var statementType = validator.GetStatementType(sql);
    Console.WriteLine($"Statement type: {statementType}");
    
    // Extract table names
    var tables = validator.ExtractTableNames(sql);
    Console.WriteLine($"Tables: {string.Join(", ", tables)}");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

## Core Types

### DatabaseType Enumeration

```csharp
public enum DatabaseType
{
    SqlServer,
    PostgreSQL,
    MySQL,
    SQLite
}
```

### SqlStatementType Enumeration

```csharp
public enum SqlStatementType
{
    Unknown,
    Select,
    Insert,
    Update,
    Delete,
    Create,
    Alter,
    Drop,
    Merge,
    Truncate,
    Batch
}
```

### SqlValidationResult Class

```csharp
public class SqlValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public object ParsedFragment { get; set; } // Database-specific parsed result
}
```

## Factory Pattern

### SqlSyntaxValidatorFactory

Central factory for creating database-specific validators:

```csharp
public static class SqlSyntaxValidatorFactory
{
    public static ISqlSyntaxValidator CreateValidator(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SqlServer => new SqlServerSyntaxValidator(),
            DatabaseType.PostgreSQL => new PostgreSQLSyntaxValidator(),
            DatabaseType.MySQL => new MySQLSyntaxValidator(),
            DatabaseType.SQLite => new SQLiteSyntaxValidator(),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
        };
    }
}
```

## Building Custom Validators

You can implement `ISqlSyntaxValidator` to create validators for additional databases:

```csharp
using SqlMimic.Core.Abstractions;

public class OracleSyntaxValidator : ISqlSyntaxValidator
{
    public DatabaseType DatabaseType => DatabaseType.Oracle; // Custom enum value
    
    public SqlValidationResult ValidateSyntax(string sql)
    {
        // Implement Oracle-specific validation logic
        var result = new SqlValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };
        
        // Your validation logic here...
        
        return result;
    }
    
    public SqlStatementType GetStatementType(string sql)
    {
        // Implement Oracle-specific statement type detection
        // ...
        return SqlStatementType.Unknown;
    }
    
    public string[] ExtractTableNames(string sql)
    {
        // Implement Oracle-specific table name extraction
        // ...
        return new string[0];
    }
}
```

## Advanced Usage

### Dependency Injection

Register validators in your DI container:

```csharp
// In Startup.cs or Program.cs
services.AddScoped<ISqlSyntaxValidator>(provider => 
    SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SqlServer));

// Or register multiple validators
services.AddScoped<ISqlSyntaxValidator>(provider => 
    SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.PostgreSQL));
```

### Multi-Database Validation

Validate the same SQL against multiple databases:

```csharp
var sql = "SELECT * FROM Users";

var databases = new[] 
{
    DatabaseType.SqlServer,
    DatabaseType.PostgreSQL,
    DatabaseType.MySQL,
    DatabaseType.SQLite
};

foreach (var dbType in databases)
{
    var validator = SqlSyntaxValidatorFactory.CreateValidator(dbType);
    var result = validator.ValidateSyntax(sql);
    
    Console.WriteLine($"{dbType}: {(result.IsValid ? "Valid" : "Invalid")}");
    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"  - {error}");
        }
    }
}
```

### Async Validation Pattern

While the core interface is synchronous, you can wrap it for async scenarios:

```csharp
public async Task<SqlValidationResult> ValidateAsync(string sql, DatabaseType databaseType)
{
    return await Task.Run(() =>
    {
        var validator = SqlSyntaxValidatorFactory.CreateValidator(databaseType);
        return validator.ValidateSyntax(sql);
    });
}
```

## Extension Points

### Custom Result Types

Extend `SqlValidationResult` for database-specific information:

```csharp
public class PostgreSQLValidationResult : SqlValidationResult
{
    public List<string> Warnings { get; set; } = new List<string>();
    public string PostgreSQLVersion { get; set; }
}
```

### Validation Rules

Implement custom validation rules:

```csharp
public interface IValidationRule
{
    ValidationRuleResult Validate(string sql);
}

public class SecurityValidationRule : IValidationRule
{
    public ValidationRuleResult Validate(string sql)
    {
        // Check for potential SQL injection patterns
        // Check for dangerous statements in production
        // etc.
        return new ValidationRuleResult();
    }
}
```

## Testing Support

The abstractions package includes utilities for testing:

```csharp
[Test]
public void TestCustomValidator()
{
    // Arrange
    var validator = new MyCustomValidator();
    var sql = "SELECT * FROM MyTable";
    
    // Act
    var result = validator.ValidateSyntax(sql);
    
    // Assert
    Assert.True(result.IsValid);
    Assert.Equal(SqlStatementType.Select, validator.GetStatementType(sql));
    Assert.Contains("MyTable", validator.ExtractTableNames(sql));
}
```

## Database Package Dependencies

This package is included automatically when you install any of these database-specific packages:

- `SqlMimic.SqlServer` - SQL Server validation using Microsoft.SqlServer.TransactSql.ScriptDom
- `SqlMimic.PostgreSQL` - PostgreSQL-specific syntax validation
- `SqlMimic.MySQL` - MySQL/MariaDB syntax validation  
- `SqlMimic.SQLite` - SQLite syntax validation with limitation awareness

## Compatibility

- **.NET Framework 4.6.2+** - Full support for legacy applications
- **.NET 8.0** - LTS (Long Term Support) version
- **.NET 9.0** - Latest stable version
- **No external dependencies** - Pure .NET Standard library
- **Thread-safe** - All implementations should be thread-safe for concurrent validation

## Contributing

To add support for a new database:

1. Implement `ISqlSyntaxValidator`
2. Add the new database type to `DatabaseType` enum
3. Update `SqlSyntaxValidatorFactory.CreateValidator()`
4. Add comprehensive tests
5. Submit a pull request

## License

MIT - see LICENSE file for details.