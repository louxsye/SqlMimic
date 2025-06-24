# SqlMimic.PostgreSQL

[![.NET](https://img.shields.io/badge/.NET-Framework%204.6.2%2B%20%7C%208.0%20%7C%209.0-blue.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)]()

PostgreSQL syntax validation package for SqlMimic. Provides PostgreSQL-specific SQL syntax validation with support for PostgreSQL's unique features and syntax.

## Features

✅ **PostgreSQL Syntax Support** - Validates PostgreSQL-specific SQL syntax  
✅ **RETURNING Clause** - Supports PostgreSQL's RETURNING clause in INSERT/UPDATE/DELETE  
✅ **UPSERT (ON CONFLICT)** - Validates PostgreSQL's ON CONFLICT syntax  
✅ **PostgreSQL Data Types** - Recognizes PostgreSQL-specific data types  
✅ **Statement Type Detection** - Identifies SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, DROP statements  
✅ **Table Name Extraction** - Extracts all referenced table names from queries  

## Installation

```bash
dotnet add package SqlMimic.PostgreSQL
```

## Quick Start

```csharp
using SqlMimic.PostgreSQL;

// Create PostgreSQL validator
var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.PostgreSQL);

// Validate PostgreSQL syntax
var sql = @"
    SELECT u.user_id, u.name, p.title
    FROM users u
    INNER JOIN posts p ON u.user_id = p.user_id
    WHERE u.is_active = true
    ORDER BY u.name";

var result = validator.ValidateSyntax(sql);

if (result.IsValid)
{
    Console.WriteLine("Valid PostgreSQL syntax!");
    
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

## PostgreSQL-Specific Features

### RETURNING Clause
```csharp
var sql = @"
    INSERT INTO users (name, email, created_at)
    VALUES ('John Doe', 'john@example.com', NOW())
    RETURNING user_id, name, created_at";

var result = validator.ValidateSyntax(sql);
// Result: Valid

var statementType = validator.GetStatementType(sql);
// Result: Insert
```

### UPSERT with ON CONFLICT
```csharp
var sql = @"
    INSERT INTO users (user_id, name, email)
    VALUES (1, 'John Doe', 'john@example.com')
    ON CONFLICT (user_id)
    DO UPDATE SET
        name = EXCLUDED.name,
        email = EXCLUDED.email,
        updated_at = NOW()";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### UPDATE with RETURNING
```csharp
var sql = @"
    UPDATE users 
    SET email = 'newemail@example.com', updated_at = NOW()
    WHERE user_id = 1
    RETURNING user_id, name, email, updated_at";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### DELETE with RETURNING
```csharp
var sql = @"
    DELETE FROM users 
    WHERE is_active = false 
    RETURNING user_id, name";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### PostgreSQL Data Types
```csharp
var sql = @"
    CREATE TABLE products (
        id SERIAL PRIMARY KEY,
        name VARCHAR(255) NOT NULL,
        description TEXT,
        price NUMERIC(10,2),
        tags TEXT[],
        metadata JSONB,
        created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
    )";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### Common Table Expressions (WITH)
```csharp
var sql = @"
    WITH recent_orders AS (
        SELECT customer_id, order_date, amount
        FROM orders
        WHERE order_date >= CURRENT_DATE - INTERVAL '30 days'
    ),
    customer_totals AS (
        SELECT customer_id, SUM(amount) as total_amount
        FROM recent_orders
        GROUP BY customer_id
    )
    SELECT c.name, COALESCE(ct.total_amount, 0) as recent_total
    FROM customers c
    LEFT JOIN customer_totals ct ON c.customer_id = ct.customer_id";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

## Advanced Usage

### Complex Query Validation
```csharp
var complexSql = @"
    SELECT 
        u.name,
        CASE 
            WHEN u.subscription_type = 'premium' THEN 'Gold'
            WHEN u.subscription_type = 'standard' THEN 'Silver'
            ELSE 'Bronze'
        END as customer_tier,
        (
            SELECT COUNT(*) 
            FROM orders o 
            WHERE o.customer_id = u.user_id 
            AND o.order_date >= CURRENT_DATE - INTERVAL '1 year'
        ) as orders_last_year
    FROM users u
    WHERE u.is_active = true
    ORDER BY u.name
    LIMIT 100 OFFSET 20";

var result = validator.ValidateSyntax(sql);
var tableNames = validator.ExtractTableNames(complexSql);
// Returns: ["users", "orders"]
```

### Window Functions
```csharp
var sql = @"
    SELECT 
        name,
        department,
        salary,
        ROW_NUMBER() OVER (PARTITION BY department ORDER BY salary DESC) as dept_rank,
        LAG(salary) OVER (PARTITION BY department ORDER BY salary DESC) as prev_salary
    FROM employees
    WHERE is_active = true";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### JSON Operations
```csharp
var sql = @"
    SELECT 
        name,
        metadata->>'category' as category,
        metadata->'attributes'->>'color' as color
    FROM products
    WHERE metadata @> '{\"featured\": true}'";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

## Error Handling

```csharp
var invalidSql = "SELECT * FORM users"; // Typo: FORM instead of FROM

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
//   Invalid syntax: FORM is not a valid keyword
```

## Integration with SqlMimic Core

```csharp
using SqlMimic.Core;
using SqlMimic.PostgreSQL;

[Test]
public void TestPostgreSQLQuery()
{
    // Setup mock database
    var connection = new MimicConnection();
    connection.SetupMockData(
        new[] { "user_id", "name", "email", "created_at" },
        new object[] { 1, "John Doe", "john@example.com", DateTime.Now }
    );
    
    // Validate PostgreSQL syntax
    var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.PostgreSQL);
    var sql = @"
        INSERT INTO users (name, email, created_at) 
        VALUES (@name, @email, NOW()) 
        RETURNING user_id, name";
    
    var validationResult = validator.ValidateSyntax(sql);
    Assert.True(validationResult.IsValid);
    
    var statementType = validator.GetStatementType(sql);
    Assert.Equal(SqlStatementType.Insert, statementType);
}
```

## Compatibility

- **.NET Framework 4.6.2+** - Full support for legacy applications
- **.NET 8.0** - LTS (Long Term Support) version
- **.NET 9.0** - Latest stable version
- **PostgreSQL 9.0+** - Supports PostgreSQL 9.0 and later syntax features
- **Compatible with popular PostgreSQL .NET libraries** - Npgsql, Entity Framework Core, Dapper

## Dependencies

- `SqlMimic.Core.Abstractions` - Core interfaces

## License

MIT - see LICENSE file for details.