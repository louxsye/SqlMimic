# SqlMimic.SQLite

[![.NET](https://img.shields.io/badge/.NET-Framework%204.6.2%2B%20%7C%208.0%20%7C%209.0-blue.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)]()

SQLite syntax validation package for SqlMimic. Provides SQLite-specific SQL syntax validation with awareness of SQLite's limitations and unique features.

## Features

✅ **SQLite Syntax Support** - Validates SQLite-specific SQL syntax and limitations  
✅ **SQLite Identifier Quoting** - Supports SQLite's bracket `[identifier]` and double-quote `"identifier"` quoting  
✅ **Limited DDL Validation** - Recognizes SQLite's limited ALTER TABLE support  
✅ **SQLite Data Types** - Validates SQLite's dynamic typing system  
✅ **Statement Type Detection** - Identifies SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, DROP statements  
✅ **Table Name Extraction** - Extracts all referenced table names from queries  
✅ **SQLite Limitations Awareness** - Validates against SQLite-specific restrictions  

## Installation

```bash
dotnet add package SqlMimic.SQLite
```

## Quick Start

```csharp
using SqlMimic.SQLite;

// Create SQLite validator
var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SQLite);

// Validate SQLite syntax
var sql = @"
    SELECT u.[user_id], u.[name], p.[title]
    FROM [users] u
    INNER JOIN [posts] p ON u.[user_id] = p.[user_id]
    WHERE u.[is_active] = 1
    ORDER BY u.[name]";

var result = validator.ValidateSyntax(sql);

if (result.IsValid)
{
    Console.WriteLine("Valid SQLite syntax!");
    
    // Get statement type
    var statementType = validator.GetStatementType(sql);
    Console.WriteLine($"Statement type: {statementType}");
    
    // Extract table names
    var tableNames = validator.ExtractTableNames(sql);
    Console.WriteLine($"Tables: {string.Join(", ", tableNames)}");
}
else
{
    Console.WriteLine("Invalid SQL:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  {error}");
    }
}
```

## SQLite-Specific Features

### SQLite Identifier Quoting
```csharp
// Bracket quoting (SQLite specific)
var sql1 = @"
    SELECT [user_id], [name], [email]
    FROM [users]
    WHERE [is_active] = 1";

// Double quote quoting (SQL standard, supported by SQLite)
var sql2 = @"
    SELECT ""user_id"", ""name"", ""email""
    FROM ""users""
    WHERE ""is_active"" = 1";

var result1 = validator.ValidateSyntax(sql1);
var result2 = validator.ValidateSyntax(sql2);
// Both: Valid
```

### Limited ALTER TABLE Support
```csharp
// Valid SQLite ALTER TABLE operations
var addColumn = "ALTER TABLE [users] ADD COLUMN [last_login] DATETIME";
var renameTable = "ALTER TABLE [users] RENAME TO [user_accounts]";

// These will validate as valid
var result1 = validator.ValidateSyntax(addColumn);
var result2 = validator.ValidateSyntax(renameTable);

// SQLite doesn't support these operations (will be flagged as invalid)
var dropColumn = "ALTER TABLE [users] DROP COLUMN [email]";
var result3 = validator.ValidateSyntax(dropColumn);
// Result: Invalid - SQLite doesn't support DROP COLUMN
```

### UPSERT with ON CONFLICT (SQLite 3.24+)
```csharp
var sql = @"
    INSERT INTO [users] ([user_id], [name], [email])
    VALUES (1, 'John Doe', 'john@example.com')
    ON CONFLICT([user_id])
    DO UPDATE SET
        [name] = excluded.[name],
        [email] = excluded.[email]";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### SQLite Data Types and Affinity
```csharp
var sql = @"
    CREATE TABLE [products] (
        [id] INTEGER PRIMARY KEY AUTOINCREMENT,
        [name] TEXT NOT NULL,
        [description] TEXT,
        [price] REAL,
        [is_available] INTEGER,  -- SQLite doesn't have BOOLEAN
        [metadata] TEXT,         -- Can store JSON as TEXT
        [created_at] TEXT        -- SQLite doesn't have native DATETIME
    )";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

## Advanced Usage

### Common Table Expressions (WITH)
```csharp
var sql = @"
    WITH [recent_orders] AS (
        SELECT [customer_id], [order_date], [amount]
        FROM [orders]
        WHERE [order_date] >= date('now', '-30 days')
    ),
    [customer_totals] AS (
        SELECT [customer_id], SUM([amount]) as [total_amount]
        FROM [recent_orders]
        GROUP BY [customer_id]
    )
    SELECT c.[name], IFNULL(ct.[total_amount], 0) as [recent_total]
    FROM [customers] c
    LEFT JOIN [customer_totals] ct ON c.[customer_id] = ct.[customer_id]";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### Window Functions (SQLite 3.25+)
```csharp
var sql = @"
    SELECT 
        [name],
        [department],
        [salary],
        ROW_NUMBER() OVER (PARTITION BY [department] ORDER BY [salary] DESC) as [dept_rank],
        LAG([salary]) OVER (PARTITION BY [department] ORDER BY [salary] DESC) as [prev_salary]
    FROM [employees]
    WHERE [is_active] = 1";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### JSON Operations (SQLite 3.40+)
```csharp
var sql = @"
    SELECT 
        [name],
        json_extract([metadata], '$.category') as [category],
        json_extract([metadata], '$.attributes.color') as [color]
    FROM [products]
    WHERE json_extract([metadata], '$.featured') = 1";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### SQLite Date/Time Functions
```csharp
var sql = @"
    SELECT 
        [name],
        [created_at],
        date([created_at]) as [created_date],
        datetime('now') as [current_timestamp],
        julianday('now') - julianday([created_at]) as [days_since_created]
    FROM [users]
    WHERE date([created_at]) >= date('now', '-1 year')";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

## SQLite Limitations and Validation

### Operations Not Supported by SQLite
```csharp
// These will be flagged as invalid for SQLite:

// TRUNCATE (use DELETE instead)
var truncate = "TRUNCATE TABLE [users]";
var result1 = validator.ValidateSyntax(truncate);
// Result: Invalid - SQLite doesn't support TRUNCATE

// DROP COLUMN
var dropColumn = "ALTER TABLE [users] DROP COLUMN [email]";
var result2 = validator.ValidateSyntax(dropColumn);
// Result: Invalid - SQLite doesn't support DROP COLUMN

// Multiple ALTER COLUMN operations
var alterColumn = "ALTER TABLE [users] ALTER COLUMN [name] VARCHAR(500)";
var result3 = validator.ValidateSyntax(alterColumn);
// Result: Invalid - SQLite doesn't support ALTER COLUMN
```

### SQLite-Compliant Alternatives
```csharp
// Instead of TRUNCATE, use DELETE
var deleteAll = "DELETE FROM [users]";

// Instead of DROP COLUMN, recreate the table
var recreateTable = @"
    CREATE TABLE [users_new] (
        [user_id] INTEGER PRIMARY KEY,
        [name] TEXT NOT NULL
        -- [email] column removed
    );
    INSERT INTO [users_new] SELECT [user_id], [name] FROM [users];
    DROP TABLE [users];
    ALTER TABLE [users_new] RENAME TO [users]";

var result = validator.ValidateSyntax(deleteAll);
// Result: Valid
```

## Error Handling

```csharp
var invalidSql = "TRUNCATE TABLE [users]"; // Not supported in SQLite

var result = validator.ValidateSyntax(invalidSql);

Console.WriteLine($"Valid: {result.IsValid}");
Console.WriteLine("Errors:");
foreach (var error in result.Errors)
{
    Console.WriteLine($"  {error}");
}

// Output:
// Valid: False
// Errors:
//   SQLite does not support TRUNCATE statement. Use DELETE instead.
```

## Integration with SqlMimic Core

```csharp
using SqlMimic.Core;
using SqlMimic.SQLite;

[Test]
public void TestSQLiteQuery()
{
    // Setup mock database
    var connection = new MimicConnection();
    connection.SetupMockData(
        new[] { "user_id", "name", "email", "created_at" },
        new object[] { 1, "John Doe", "john@example.com", "2023-01-01 10:00:00" }
    );
    
    // Validate SQLite syntax
    var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SQLite);
    var sql = @"
        INSERT INTO [users] ([name], [email], [created_at]) 
        VALUES (@name, @email, datetime('now'))
        ON CONFLICT([email])
        DO UPDATE SET [name] = excluded.[name]";
    
    var validationResult = validator.ValidateSyntax(sql);
    Assert.True(validationResult.IsValid);
    
    var statementType = validator.GetStatementType(sql);
    Assert.Equal(SqlStatementType.Insert, statementType);
}
```

## Best Practices for SQLite

### Data Type Usage
```csharp
// Use SQLite's type affinity system effectively
var sql = @"
    CREATE TABLE [users] (
        [id] INTEGER PRIMARY KEY,      -- INTEGER affinity
        [name] TEXT NOT NULL,          -- TEXT affinity
        [balance] REAL,                -- REAL affinity
        [is_active] INTEGER,           -- Use INTEGER for boolean (0/1)
        [metadata] TEXT,               -- Store JSON as TEXT
        [created_at] TEXT              -- Store dates as TEXT or REAL
    )";
```

### Date Handling
```csharp
// Store dates in ISO 8601 format for best compatibility
var sql = @"
    INSERT INTO [events] ([name], [event_date])
    VALUES ('Meeting', datetime('now'))";
    
// Query with date functions
var dateQuery = @"
    SELECT * FROM [events]
    WHERE date([event_date]) = date('now')";
```

## Compatibility

- **.NET Framework 4.6.2+** - Full support for legacy applications
- **.NET 8.0** - LTS (Long Term Support) version
- **.NET 9.0** - Latest stable version
- **SQLite 3.6+** - Supports SQLite 3.6 and later syntax features
- **Modern SQLite features** - Validates newer features like window functions (3.25+), UPSERT (3.24+), JSON functions (3.40+)
- **Compatible with popular SQLite .NET libraries** - Microsoft.Data.Sqlite, System.Data.SQLite, Entity Framework Core, Dapper

## Dependencies

- `SqlMimic.Core.Abstractions` - Core interfaces

## License

MIT - see LICENSE file for details.