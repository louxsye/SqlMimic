using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SqlMimic.Core;
using SqlMimic.Tests.Models;
using SqlMimic.Tests.Repositories;

namespace SqlMimic.Tests
{
    public class UserRepositoryTests
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
            
            var repository = new UserRepository(connection);

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
            Assert.Contains("WHERE Id = @id", command.CommandText);
            
            // Verify parameters
            Assert.Single(command.Parameters);
            var parameter = (DbParameter)command.Parameters[0]!;
            Assert.Equal("@id", parameter.ParameterName);
            Assert.Equal(1, parameter.Value);
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
            
            var repository = new UserRepository(connection);

            // Act
            var user = await repository.GetByIdAsync(999);

            // Assert
            Assert.Null(user);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("WHERE Id = @id", command.CommandText);
        }

        [Fact]
        public async Task CreateAsync_ValidUser_ReturnsNewId()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 123; // New ID
            
            var repository = new UserRepository(connection);
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
            Assert.Equal(1, newId);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("INSERT INTO Users", command.CommandText);
            Assert.Contains("Name, Email, CreatedAt, IsActive", command.CommandText);
            Assert.Contains("@name, @email, @createdAt, @isActive", command.CommandText);
            
            // Verify parameters
            Assert.Equal(4, command.Parameters.Count);
            var nameParam = command.Parameters.Cast<DbParameter>().FirstOrDefault(p => p.ParameterName == "@name");
            Assert.NotNull(nameParam);
            Assert.Equal("Jane Doe", nameParam.Value);
        }

        [Fact]
        public async Task UpdateAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 1; // Rows affected
            
            var repository = new UserRepository(connection);
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
            Assert.Contains("SET Name = @name, Email = @email, IsActive = @isActive", command.CommandText);
            Assert.Contains("WHERE Id = @id", command.CommandText);
            
            // Verify parameters
            Assert.Equal(4, command.Parameters.Count);
            var idParam = command.Parameters.Cast<DbParameter>().FirstOrDefault(p => p.ParameterName == "@id");
            Assert.NotNull(idParam);
            Assert.Equal(1, idParam.Value);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingUser_ReturnsFalse()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 0; // No rows affected
            
            var repository = new UserRepository(connection);
            var user = new User
            {
                Id = 999,
                Name = "Non Existing",
                Email = "none@example.com",
                IsActive = false
            };

            // Act
            var result = await repository.UpdateAsync(user);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 1; // Rows affected
            
            var repository = new UserRepository(connection);

            // Act
            var result = await repository.DeleteAsync(1);

            // Assert
            Assert.True(result);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Equal("DELETE FROM Users WHERE Id = @id", command.CommandText);
            
            // Verify parameters
            Assert.Single(command.Parameters);
            var parameter = (DbParameter)command.Parameters[0]!;
            Assert.Equal("@id", parameter.ParameterName);
            Assert.Equal(1, parameter.Value);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingUser_ReturnsFalse()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 0; // No rows affected
            
            var repository = new UserRepository(connection);

            // Act
            var result = await repository.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CountActiveUsersAsync_ReturnsCount()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.MockReturnValue = 42;
            
            var repository = new UserRepository(connection);

            // Act
            var count = await repository.CountActiveUsersAsync();

            // Assert
            Assert.Equal(42, count);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Equal("SELECT COUNT(*) FROM Users WHERE IsActive = 1", command.CommandText);
            Assert.Empty(command.Parameters);
        }

        [Fact]
        public async Task GetAllActiveAsync_MultipleUsers_ReturnsAllActiveUsers()
        {
            // Arrange
            var connection = new MimicConnection();
            var now = DateTime.Now;
            connection.SetupMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
                new object[] { 1, "User 1", "user1@example.com", now, true },
                new object[] { 2, "User 2", "user2@example.com", now, true }
            );
            
            var repository = new UserRepository(connection);

            // Act
            var users = await repository.GetAllActiveAsync();
            var userList = users.ToList();

            // Assert
            Assert.Equal(2, userList.Count);
            Assert.Equal("User 1", userList[0].Name);
            Assert.Equal("User 2", userList[1].Name);
            Assert.Equal("user1@example.com", userList[0].Email);
            Assert.Equal("user2@example.com", userList[1].Email);
            
            // Verify SQL execution
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("SELECT Id, Name, Email, CreatedAt, IsActive", command.CommandText);
            Assert.Contains("FROM Users", command.CommandText);
            Assert.Contains("WHERE IsActive = 1", command.CommandText);
            Assert.Contains("ORDER BY Name", command.CommandText);
        }

        [Fact]
        public async Task Repository_SqlValidation_AllQueriesAreValid()
        {
            // This test validates that all SQL queries in the repository are syntactically correct
            var connection = new MimicConnection();
            var repository = new UserRepository(connection);

            // Setup default mock data to ensure methods complete
            connection.SetupDefaultMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" },
                new object[] { 1, "Test", "test@test.com", DateTime.Now, true }
            );
            connection.MockReturnValue = 1;

            // Execute all repository methods to collect SQL commands
            await repository.GetByIdAsync(1);
            await repository.GetAllActiveAsync();
            await repository.CreateAsync(new User { Name = "Test", Email = "test@test.com", CreatedAt = DateTime.Now });
            await repository.UpdateAsync(new User { Id = 1, Name = "Test", Email = "test@test.com" });
            await repository.DeleteAsync(1);
            await repository.CountActiveUsersAsync();

            // Validate all SQL commands
            foreach (var command in connection.Commands)
            {
                var result = SqlSyntaxValidator.ValidateSyntax(command.CommandText);
                Assert.True(result.IsValid, 
                    $"SQL syntax error in command: {command.CommandText}\nErrors: {string.Join(", ", result.Errors)}");
            }
        }

        [Fact]
        public void Repository_SqlStatementTypes_CorrectTypesDetected()
        {
            var connection = new MimicConnection();
            var repository = new UserRepository(connection);

            // Setup default mock data
            connection.SetupDefaultMockData(
                new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" }
            );
            connection.MockReturnValue = 1;

            // Collect SQL commands (synchronously for simplicity)
            Task.Run(async () =>
            {
                await repository.GetByIdAsync(1);
                await repository.CreateAsync(new User { Name = "Test", Email = "test@test.com", CreatedAt = DateTime.Now });
                await repository.UpdateAsync(new User { Id = 1, Name = "Test", Email = "test@test.com" });
                await repository.DeleteAsync(1);
                await repository.CountActiveUsersAsync();
            }).Wait();

            // Verify statement types
            var commands = connection.Commands.ToList();
            Assert.Equal(SqlStatementType.Select, SqlSyntaxValidator.GetStatementType(commands[0].CommandText));
            Assert.Equal(SqlStatementType.Insert, SqlSyntaxValidator.GetStatementType(commands[1].CommandText));
            Assert.Equal(SqlStatementType.Update, SqlSyntaxValidator.GetStatementType(commands[2].CommandText));
            Assert.Equal(SqlStatementType.Delete, SqlSyntaxValidator.GetStatementType(commands[3].CommandText));
            Assert.Equal(SqlStatementType.Select, SqlSyntaxValidator.GetStatementType(commands[4].CommandText));
        }
    }
}