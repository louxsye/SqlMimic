# SqlMimic.SqlServer

[![.NET](https://img.shields.io/badge/.NET-Framework%204.6.2%2B%20%7C%208.0%20%7C%209.0-blue.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)]()

SQL Server syntax validation package for SqlMimic. Provides the most accurate SQL Server syntax validation using Microsoft's official `Microsoft.SqlServer.TransactSql.ScriptDom` parser.

## Features

✅ **Accurate SQL Server Parsing** - Uses Microsoft.SqlServer.TransactSql.ScriptDom for precise validation  
✅ **T-SQL Support** - Full support for T-SQL syntax including stored procedures, functions, and advanced features  
✅ **Statement Type Detection** - Identifies SELECT, INSERT, UPDATE, DELETE, MERGE, CREATE, ALTER, DROP statements  
✅ **Table Name Extraction** - Extracts all referenced table names from complex queries  
✅ **Error Reporting** - Detailed error messages with line and column information  

## Installation

```bash
dotnet add package SqlMimic.SqlServer
```

## Quick Start

```csharp
using SqlMimic.SqlServer;

// Create SQL Server validator
var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SqlServer);

// Validate T-SQL syntax
var sql = @"
    SELECT u.UserId, u.Name, p.Title
    FROM Users u
    INNER JOIN Posts p ON u.UserId = p.UserId
    WHERE u.IsActive = 1
    ORDER BY u.Name";

var result = validator.ValidateSyntax(sql);

if (result.IsValid)
{
    Console.WriteLine("Valid SQL Server syntax!");
    
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

## T-SQL Specific Features

### Window Functions
```csharp
var sql = @"
    SELECT 
        Name,
        ROW_NUMBER() OVER (ORDER BY CreatedDate) as RowNum,
        RANK() OVER (PARTITION BY Department ORDER BY Salary DESC) as SalaryRank
    FROM Employees";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### Common Table Expressions (CTEs)
```csharp
var sql = @"
    WITH RecentOrders AS (
        SELECT CustomerId, OrderDate, Amount
        FROM Orders
        WHERE OrderDate >= DATEADD(day, -30, GETDATE())
    )
    SELECT c.CustomerName, COUNT(ro.CustomerId) as RecentOrderCount
    FROM Customers c
    LEFT JOIN RecentOrders ro ON c.CustomerId = ro.CustomerId
    GROUP BY c.CustomerName";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### MERGE Statements
```csharp
var sql = @"
    MERGE Users AS target
    USING UserUpdates AS source ON target.UserId = source.UserId
    WHEN MATCHED THEN
        UPDATE SET Name = source.Name, Email = source.Email
    WHEN NOT MATCHED THEN
        INSERT (UserId, Name, Email) VALUES (source.UserId, source.Name, source.Email)
    WHEN NOT MATCHED BY SOURCE THEN
        DELETE;";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### Stored Procedures
```csharp
var sql = @"
    CREATE PROCEDURE GetUserById
        @UserId INT
    AS
    BEGIN
        SET NOCOUNT ON;
        
        SELECT UserId, Name, Email, CreatedDate
        FROM Users
        WHERE UserId = @UserId AND IsActive = 1;
    END";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

## Advanced Usage

### Batch Statement Validation
```csharp
var batchSql = @"
    INSERT INTO Users (Name, Email) VALUES ('John', 'john@example.com');
    INSERT INTO Users (Name, Email) VALUES ('Jane', 'jane@example.com');
    SELECT COUNT(*) FROM Users;";

var result = validator.ValidateSyntax(batchSql);
var statementType = validator.GetStatementType(batchSql);
// Returns: Batch (for multiple statements)
```

### Complex Query Analysis
```csharp
var complexSql = @"
    SELECT 
        u.Name,
        (SELECT COUNT(*) FROM Orders o WHERE o.UserId = u.UserId) as OrderCount,
        CASE 
            WHEN u.SubscriptionType = 'Premium' THEN 'Gold'
            WHEN u.SubscriptionType = 'Standard' THEN 'Silver'
            ELSE 'Bronze'
        END as CustomerTier
    FROM Users u
    CROSS APPLY (
        SELECT TOP 1 OrderDate 
        FROM Orders o2 
        WHERE o2.UserId = u.UserId 
        ORDER BY OrderDate DESC
    ) AS LastOrder
    WHERE u.IsActive = 1";

var tableNames = validator.ExtractTableNames(complexSql);
// Returns: ["Users", "Orders"]
```

## Error Handling

The SQL Server validator provides detailed error information:

```csharp
var invalidSql = "SELECT * FORM Users"; // Typo: FORM instead of FROM

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
//   Incorrect syntax near 'FORM'.
```

## Integration with SqlMimic Core

You can use this validator with SqlMimic's database mocking capabilities:

```csharp
using SqlMimic.Core;
using SqlMimic.SqlServer;

[Test]
public void TestRepositoryWithValidation()
{
    // Setup mock database
    var connection = new MimicConnection();
    connection.SetupMockData(
        new[] { "UserId", "Name", "Email" },
        new object[] { 1, "John Doe", "john@example.com" }
    );
    
    // Validate SQL before execution
    var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.SqlServer);
    var sql = "SELECT UserId, Name, Email FROM Users WHERE UserId = @userId";
    
    var validationResult = validator.ValidateSyntax(sql);
    Assert.True(validationResult.IsValid);
    
    // Execute with mock
    var users = connection.Query<User>(sql, new { userId = 1 });
    Assert.Single(users);
}
```

## Compatibility

- **.NET Framework 4.6.2+** - Full support for legacy applications
- **.NET 8.0** - LTS (Long Term Support) version  
- **.NET 9.0** - Latest stable version
- **SQL Server 2012+** - Supports SQL Server 2012 and later syntax
- **Azure SQL Database** - Full compatibility with Azure SQL Database
- **SQL Server Express/LocalDB** - Works with all SQL Server editions

## Dependencies

- `Microsoft.SqlServer.TransactSql.ScriptDom` - Microsoft's official T-SQL parser
- `SqlMimic.Core.Abstractions` - Core interfaces

## License

MIT - see LICENSE file for details.