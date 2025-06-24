# SqlMimic.MySQL

[![.NET](https://img.shields.io/badge/.NET-Framework%204.6.2%2B%20%7C%208.0%20%7C%209.0-blue.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)]()

MySQL syntax validation package for SqlMimic. Provides MySQL and MariaDB-specific SQL syntax validation with support for MySQL's unique features and syntax.

## Features

✅ **MySQL/MariaDB Syntax Support** - Validates MySQL and MariaDB-specific SQL syntax  
✅ **ON DUPLICATE KEY UPDATE** - Supports MySQL's ON DUPLICATE KEY UPDATE syntax  
✅ **MySQL Comment Styles** - Handles `#`, `--`, and `/* */` comment styles  
✅ **Backtick Identifiers** - Recognizes MySQL's backtick identifier quoting  
✅ **MySQL Data Types** - Validates MySQL-specific data types and functions  
✅ **Statement Type Detection** - Identifies SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, DROP statements  
✅ **Table Name Extraction** - Extracts all referenced table names from queries  

## Installation

```bash
dotnet add package SqlMimic.MySQL
```

## Quick Start

```csharp
using SqlMimic.MySQL;

// Create MySQL validator
var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.MySQL);

// Validate MySQL syntax
var sql = @"
    SELECT u.`user_id`, u.`name`, p.`title`
    FROM `users` u
    INNER JOIN `posts` p ON u.`user_id` = p.`user_id`
    WHERE u.`is_active` = 1
    ORDER BY u.`name`";

var result = validator.ValidateSyntax(sql);

if (result.IsValid)
{
    Console.WriteLine("Valid MySQL syntax!");
    
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

## MySQL-Specific Features

### ON DUPLICATE KEY UPDATE
```csharp
var sql = @"
    INSERT INTO `users` (`user_id`, `name`, `email`)
    VALUES (1, 'John Doe', 'john@example.com')
    ON DUPLICATE KEY UPDATE
        `name` = VALUES(`name`),
        `email` = VALUES(`email`),
        `updated_at` = NOW()";

var result = validator.ValidateSyntax(sql);
// Result: Valid

var statementType = validator.GetStatementType(sql);
// Result: Insert
```

### INSERT IGNORE
```csharp
var sql = @"
    INSERT IGNORE INTO `users` (`name`, `email`)
    VALUES ('John Doe', 'john@example.com')";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### REPLACE Statement
```csharp
var sql = @"
    REPLACE INTO `users` (`user_id`, `name`, `email`)
    VALUES (1, 'John Doe', 'john@example.com')";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### MySQL Comment Styles
```csharp
var sql = @"
    SELECT 
        `user_id`,    # Primary key
        `name`,       -- User's full name
        `email`       /* User's email address */
    FROM `users`
    WHERE `is_active` = 1";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### Backtick Identifiers
```csharp
var sql = @"
    SELECT 
        `user`.`user_id`,
        `user`.`name`,
        `order`.`total`
    FROM `users` AS `user`
    JOIN `orders` AS `order` ON `user`.`user_id` = `order`.`user_id`";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### MySQL Data Types and Functions
```csharp
var sql = @"
    CREATE TABLE `products` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `name` VARCHAR(255) NOT NULL,
        `description` TEXT,
        `price` DECIMAL(10,2),
        `tags` JSON,
        `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

## Advanced Usage

### Complex UPSERT Operations
```csharp
var sql = @"
    INSERT INTO `user_stats` (`user_id`, `login_count`, `last_login`)
    VALUES (1, 1, NOW())
    ON DUPLICATE KEY UPDATE
        `login_count` = `login_count` + 1,
        `last_login` = NOW()";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### Multi-Row Insert with ON DUPLICATE KEY UPDATE
```csharp
var sql = @"
    INSERT INTO `users` (`user_id`, `name`, `email`)
    VALUES 
        (1, 'John Doe', 'john@example.com'),
        (2, 'Jane Smith', 'jane@example.com'),
        (3, 'Bob Johnson', 'bob@example.com')
    ON DUPLICATE KEY UPDATE
        `name` = VALUES(`name`),
        `email` = VALUES(`email`)";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### Window Functions (MySQL 8.0+)
```csharp
var sql = @"
    SELECT 
        `name`,
        `department`,
        `salary`,
        ROW_NUMBER() OVER (PARTITION BY `department` ORDER BY `salary` DESC) as `dept_rank`,
        LAG(`salary`) OVER (PARTITION BY `department` ORDER BY `salary` DESC) as `prev_salary`
    FROM `employees`
    WHERE `is_active` = 1";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### Common Table Expressions (MySQL 8.0+)
```csharp
var sql = @"
    WITH `recent_orders` AS (
        SELECT `customer_id`, `order_date`, `amount`
        FROM `orders`
        WHERE `order_date` >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)
    ),
    `customer_totals` AS (
        SELECT `customer_id`, SUM(`amount`) as `total_amount`
        FROM `recent_orders`
        GROUP BY `customer_id`
    )
    SELECT c.`name`, IFNULL(ct.`total_amount`, 0) as `recent_total`
    FROM `customers` c
    LEFT JOIN `customer_totals` ct ON c.`customer_id` = ct.`customer_id`";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

### JSON Operations (MySQL 5.7+)
```csharp
var sql = @"
    SELECT 
        `name`,
        JSON_EXTRACT(`metadata`, '$.category') as `category`,
        JSON_UNQUOTE(JSON_EXTRACT(`metadata`, '$.attributes.color')) as `color`
    FROM `products`
    WHERE JSON_EXTRACT(`metadata`, '$.featured') = true";

var result = validator.ValidateSyntax(sql);
// Result: Valid
```

## Error Handling

```csharp
var invalidSql = "SELECT * FORM `users`"; // Typo: FORM instead of FROM

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
using SqlMimic.MySQL;

[Test]
public void TestMySQLQuery()
{
    // Setup mock database
    var connection = new MimicConnection();
    connection.SetupMockData(
        new[] { "user_id", "name", "email", "created_at" },
        new object[] { 1, "John Doe", "john@example.com", DateTime.Now }
    );
    
    // Validate MySQL syntax
    var validator = SqlSyntaxValidatorFactory.CreateValidator(DatabaseType.MySQL);
    var sql = @"
        INSERT INTO `users` (`name`, `email`, `created_at`) 
        VALUES (@name, @email, NOW())
        ON DUPLICATE KEY UPDATE 
            `name` = VALUES(`name`),
            `email` = VALUES(`email`)";
    
    var validationResult = validator.ValidateSyntax(sql);
    Assert.True(validationResult.IsValid);
    
    var statementType = validator.GetStatementType(sql);
    Assert.Equal(SqlStatementType.Insert, statementType);
}
```

## Best Practices

### Identifier Quoting
```csharp
// Recommended: Use backticks for identifiers that might be reserved words
var sql = "SELECT `user`, `order`, `group` FROM `user_data`";

// Alternative: Use standard identifiers when possible
var sql = "SELECT user_id, order_id, group_id FROM user_data";
```

### Comment Usage
```csharp
// MySQL supports multiple comment styles
var sql = @"
    # Hash style comment
    -- SQL standard comment
    /* C-style comment */
    SELECT * FROM users";
```

## Compatibility

- **.NET Framework 4.6.2+** - Full support for legacy applications
- **.NET 8.0** - LTS (Long Term Support) version
- **.NET 9.0** - Latest stable version
- **MySQL 5.6+** - Supports MySQL 5.6 and later syntax features
- **MariaDB 10.0+** - Compatible with MariaDB syntax
- **Compatible with popular MySQL .NET libraries** - MySql.Data, MySqlConnector, Entity Framework Core, Dapper

## Dependencies

- `SqlMimic.Core.Abstractions` - Core interfaces

## License

MIT - see LICENSE file for details.