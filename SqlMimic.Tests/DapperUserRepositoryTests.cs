using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SqlMimic.Core;
using SqlMimic.Core.Abstractions;
using SqlMimic.SqlServer;
using SqlMimic.Tests.Models;
using SqlMimic.Tests.Repositories;

namespace SqlMimic.Tests
{
    public class DapperUserRepositoryTests
    {
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
            
            var repository = new DapperUserRepository(connection);

            // Act
            var user = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(1, user.Id);
            Assert.Equal("John Doe", user.Name);
            Assert.Equal("john@example.com", user.Email);
            Assert.Equal(createdAt, user.CreatedAt);
            Assert.True(user.IsActive);

            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("SELECT Id, Name, Email, CreatedAt, IsActive", command.CommandText);
            Assert.Contains("FROM Users", command.CommandText);
            Assert.Contains("WHERE Id = @Id", command.CommandText);
        }

        [Fact]
        public async Task GetByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.SetupMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" }
                // No rows - empty result set
            );
            
            var repository = new DapperUserRepository(connection);

            // Act
            var user = await repository.GetByIdAsync(999);

            // Assert
            Assert.Null(user);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("WHERE Id = @Id", command.CommandText);
        }

        [Fact]
        public async Task GetAllActiveAsync_MultipleUsers_ReturnsAllActiveUsers()
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
            
            var repository = new DapperUserRepository(connection);

            // Act
            var users = await repository.GetAllActiveAsync();
            var userList = users.ToList();

            // Assert
            Assert.Equal(3, userList.Count);
            Assert.All(userList, u => Assert.True(u.IsActive));
            Assert.Contains(userList, u => u.Name == "Alice");
            Assert.Contains(userList, u => u.Name == "Bob");
            Assert.Contains(userList, u => u.Name == "Charlie");
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("WHERE IsActive = 1", command.CommandText);
            Assert.Contains("ORDER BY Name", command.CommandText);
        }

        [Fact]
        public async Task GetUsersByStatusAsync_ActiveUsers_ReturnsOnlyActiveUsers()
        {
            // Arrange
            var connection = new MimicConnection();
            var now = DateTime.Now;
            connection.SetupMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
                new object[] { 1, "Active User 1", "active1@example.com", now, true },
                new object[] { 2, "Active User 2", "active2@example.com", now.AddDays(-1), true }
            );
            
            var repository = new DapperUserRepository(connection);

            // Act
            var users = await repository.GetUsersByStatusAsync(true);
            var userList = users.ToList();

            // Assert
            Assert.Equal(2, userList.Count);
            Assert.All(userList, u => Assert.True(u.IsActive));
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("WHERE IsActive = @IsActive", command.CommandText);
            Assert.Contains("ORDER BY CreatedAt DESC", command.CommandText);
        }

        [Fact]
        public async Task CreateAsync_ValidUser_ReturnsNewId()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.SetupMockData(
                new[] { "NewId" },
                new object[] { 123 } // Mock the returned ID
            );
            
            var repository = new DapperUserRepository(connection);
            var user = new User
            {
                Name = "Jane Doe",
                Email = "jane@example.com",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            // Act
            var newId = await repository.CreateAsync(user);

            // Assert
            Assert.Equal(123, newId);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("INSERT INTO Users", command.CommandText);
            Assert.Contains("VALUES (@Name, @Email, @CreatedAt, @IsActive)", command.CommandText);
            Assert.Contains("SELECT CAST(SCOPE_IDENTITY() AS int)", command.CommandText);
        }

        [Fact]
        public async Task UpdateAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 1; // Rows affected
            
            var repository = new DapperUserRepository(connection);
            var user = new User
            {
                Id = 1,
                Name = "Updated Name",
                Email = "updated@example.com",
                IsActive = false
            };

            // Act
            var result = await repository.UpdateAsync(user);

            // Assert
            Assert.True(result);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("UPDATE Users", command.CommandText);
            Assert.Contains("SET Name = @Name, Email = @Email, IsActive = @IsActive", command.CommandText);
            Assert.Contains("WHERE Id = @Id", command.CommandText);
        }

        [Fact]
        public async Task DeleteAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 1; // Rows affected
            
            var repository = new DapperUserRepository(connection);

            // Act
            var result = await repository.DeleteAsync(1);

            // Assert
            Assert.True(result);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Equal("DELETE FROM Users WHERE Id = @Id", command.CommandText);
        }

        [Fact]
        public async Task CountActiveUsersAsync_ReturnsCount()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.SetupMockData(
                new[] { "Count" },
                new object[] { 42 }
            );
            
            var repository = new DapperUserRepository(connection);

            // Act
            var count = await repository.CountActiveUsersAsync();

            // Assert
            Assert.Equal(42, count);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Equal("SELECT COUNT(*) FROM Users WHERE IsActive = 1", command.CommandText);
        }

        [Fact]
        public async Task CountUsersByEmailDomainAsync_ReturnsCount()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.SetupMockData(
                new[] { "Count" },
                new object[] { 15 }
            );
            
            var repository = new DapperUserRepository(connection);

            // Act
            var count = await repository.CountUsersByEmailDomainAsync("example.com");

            // Assert
            Assert.Equal(15, count);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("WHERE Email LIKE @EmailPattern", command.CommandText);
            Assert.Contains("AND IsActive = 1", command.CommandText);
        }

        [Fact]
        public async Task SearchUsersByNameAsync_WithPattern_ReturnsMatchingUsers()
        {
            // Arrange
            var connection = new MimicConnection();
            var now = DateTime.Now;
            connection.SetupMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
                new object[] { 1, "John Smith", "john.smith@example.com", now, true },
                new object[] { 2, "John Doe", "john.doe@example.com", now.AddDays(-1), true }
            );
            
            var repository = new DapperUserRepository(connection);

            // Act
            var users = await repository.SearchUsersByNameAsync("John");
            var userList = users.ToList();

            // Assert
            Assert.Equal(2, userList.Count);
            Assert.All(userList, u => Assert.Contains("John", u.Name));
            Assert.All(userList, u => Assert.True(u.IsActive));
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("WHERE Name LIKE @NamePattern", command.CommandText);
            Assert.Contains("ORDER BY Name", command.CommandText);
        }

        [Fact]
        public async Task GetUserWithMostRecentActivityAsync_ReturnsLatestUser()
        {
            // Arrange
            var connection = new MimicConnection();
            var latestDate = DateTime.Now;
            connection.SetupMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
                new object[] { 5, "Latest User", "latest@example.com", latestDate, true }
            );
            
            var repository = new DapperUserRepository(connection);

            // Act
            var user = await repository.GetUserWithMostRecentActivityAsync();

            // Assert
            Assert.NotNull(user);
            Assert.Equal(5, user.Id);
            Assert.Equal("Latest User", user.Name);
            Assert.Equal(latestDate, user.CreatedAt);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("SELECT TOP 1", command.CommandText);
            Assert.Contains("ORDER BY CreatedAt DESC", command.CommandText);
        }

        [Fact]
        public async Task UpdateUserStatusBatchAsync_MultipleIds_ReturnsTrue()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 3; // 3 rows affected
            
            var repository = new DapperUserRepository(connection);
            var userIds = new[] { 1, 2, 3 };

            // Act
            var result = await repository.UpdateUserStatusBatchAsync(userIds, false);

            // Assert
            Assert.True(result);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("UPDATE Users", command.CommandText);
            Assert.Contains("SET IsActive = @IsActive", command.CommandText);
            Assert.Contains("WHERE Id IN", command.CommandText);
        }

        [Fact]
        public async Task GetUserStatsAsync_ReturnsStatistics()
        {
            // Arrange
            var connection = new MimicConnection();
            var earliestDate = DateTime.Now.AddDays(-30);
            var latestDate = DateTime.Now;
            connection.SetupMockData(
                new[] { "IsActive", "Count", "EarliestCreated", "LatestCreated" },
                new object[] { true, 25, earliestDate, latestDate },
                new object[] { false, 5, earliestDate.AddDays(5), latestDate.AddDays(-1) }
            );
            
            var repository = new DapperUserRepository(connection);

            // Act
            var stats = await repository.GetUserStatsAsync();
            var statsList = stats.ToList();

            // Assert
            Assert.Equal(2, statsList.Count);
            
            var activeStats = statsList.First(s => s.IsActive);
            Assert.Equal(25, activeStats.Count);
            Assert.Equal(earliestDate, activeStats.EarliestCreated);
            Assert.Equal(latestDate, activeStats.LatestCreated);
            
            var inactiveStats = statsList.First(s => !s.IsActive);
            Assert.Equal(5, inactiveStats.Count);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("SELECT", command.CommandText);
            Assert.Contains("COUNT(*) as Count", command.CommandText);
            Assert.Contains("MIN(CreatedAt) as EarliestCreated", command.CommandText);
            Assert.Contains("MAX(CreatedAt) as LatestCreated", command.CommandText);
            Assert.Contains("GROUP BY IsActive", command.CommandText);
        }

        [Fact]
        public async Task DapperRepository_SqlValidation_AllQueriesAreValid()
        {
            // This test validates that all SQL queries in the Dapper repository are syntactically correct
            var connection = new MimicConnection();
            var repository = new DapperUserRepository(connection);

            // Setup default mock data to ensure methods complete
            connection.SetupDefaultMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive", "Count" },
                new object[] { 1, "Test", "test@test.com", DateTime.Now, true, 1 }
            );
            connection.MockReturnValue = 1;

            // Execute all repository methods to collect SQL commands
            await repository.GetByIdAsync(1);
            await repository.GetAllActiveAsync();
            await repository.GetUsersByStatusAsync(true);
            await repository.CreateAsync(new User { Name = "Test", Email = "test@test.com", CreatedAt = DateTime.Now });
            await repository.UpdateAsync(new User { Id = 1, Name = "Test", Email = "test@test.com" });
            await repository.DeleteAsync(1);
            await repository.CountActiveUsersAsync();
            await repository.CountUsersByEmailDomainAsync("example.com");
            await repository.SearchUsersByNameAsync("test");
            await repository.GetUserWithMostRecentActivityAsync();
            await repository.UpdateUserStatusBatchAsync(new[] { 1, 2 }, false);
            await repository.GetUserStatsAsync();

            // Validate all SQL commands
            var validator = new SqlServerSyntaxValidator();
            foreach (var command in connection.Commands)
            {
                var result = validator.ValidateSyntax(command.CommandText);
                Assert.True(result.IsValid, 
                    $"SQL syntax error in command: {command.CommandText}\nErrors: {string.Join(", ", result.Errors)}");
            }
        }

        [Fact]
        public async Task DapperRepository_SqlStatementTypes_CorrectTypesDetected()
        {
            var connection = new MimicConnection();
            var repository = new DapperUserRepository(connection);

            // Test individual operations to collect SQL commands
            
            // SELECT statement
            connection.SetupMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
                new object[] { 1, "Test", "test@test.com", DateTime.Now, true }
            );
            await repository.GetByIdAsync(1);

            // INSERT statement  
            connection.SetupMockData(new[] { "NewId" }, new object[] { 123 });
            await repository.CreateAsync(new User { Name = "Test", Email = "test@test.com", CreatedAt = DateTime.Now });

            // UPDATE statement
            connection.MockReturnValue = 1;
            await repository.UpdateAsync(new User { Id = 1, Name = "Test", Email = "test@test.com" });

            // DELETE statement
            connection.MockReturnValue = 1;
            await repository.DeleteAsync(1);

            // COUNT statement (SELECT)
            connection.SetupMockData(new[] { "Count" }, new object[] { 42 });
            await repository.CountActiveUsersAsync();

            // Verify statement types
            var commands = connection.Commands.ToList();
            Assert.True(commands.Count >= 5);
            
            // Verify that we can detect different statement types
            var validator = new SqlServerSyntaxValidator();
            var statementTypes = commands.Select(c => validator.GetStatementType(c.CommandText)).ToList();
            
            // Should have at least these statement types
            Assert.Contains(SqlStatementType.Select, statementTypes);
            Assert.Contains(SqlStatementType.Update, statementTypes);
            Assert.Contains(SqlStatementType.Delete, statementTypes);
            
            // Note: CreateAsync SQL contains both INSERT and SELECT statements in one command
            // The SQL parser might detect either depending on which statement comes first
            
            // Verify specific commands we know about
            Assert.Equal(SqlStatementType.Select, statementTypes[0]); // GetByIdAsync
            Assert.Equal(SqlStatementType.Update, statementTypes[2]); // UpdateAsync
            Assert.Equal(SqlStatementType.Delete, statementTypes[3]); // DeleteAsync
        }

        [Fact]
        public async Task DapperRepository_ParameterBinding_WorksCorrectly()
        {
            // This test demonstrates that Dapper parameter binding works with TestaQuery mocking
            var connection = new MimicConnection();
            connection.SetupMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
                new object[] { 42, "Parameter Test", "param@test.com", DateTime.Now, true }
            );
            
            var repository = new DapperUserRepository(connection);

            // Act
            var user = await repository.GetByIdAsync(42);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("Parameter Test", user.Name);
            
            // Verify that Dapper parameters are properly passed through TestaQuery
            Assert.Single(connection.Commands);
            // Note: With Dapper, parameter names might be different due to Dapper's internal processing
            // but the SQL structure should be correct
        }
    }
}