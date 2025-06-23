using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SqlMimic.Core;
using SqlMimic.Tests.Models;
using SqlMimic.Tests.Repositories;

namespace SqlMimic.Tests
{
    /// <summary>
    /// Demonstrates the differences between ADO.NET and Dapper approaches with SqlMimic
    /// </summary>
    public class RepositoryComparisonTests
    {
        [Fact]
        public async Task ADO_NET_vs_Dapper_BothWorkWithSqlMimic()
        {
            // Arrange - Setup mock data for both repositories
            var adoConnection = new MimicConnection();
            var dapperConnection = new MimicConnection();
            
            var testUser = new User
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            // Setup identical mock data for both connections
            var columnNames = new[] { "Id", "Name", "Email", "CreatedAt", "IsActive" };
            var rowData = new object[] { testUser.Id, testUser.Name, testUser.Email, testUser.CreatedAt, testUser.IsActive };
            
            adoConnection.SetupMockData(columnNames, rowData);
            dapperConnection.SetupMockData(columnNames, rowData);

            var adoRepository = new UserRepository(adoConnection);
            var dapperRepository = new DapperUserRepository(dapperConnection);

            // Act - Execute the same operation on both repositories
            var adoUser = await adoRepository.GetByIdAsync(1);
            var dapperUser = await dapperRepository.GetByIdAsync(1);

            // Assert - Both should return equivalent results
            Assert.NotNull(adoUser);
            Assert.NotNull(dapperUser);
            
            Assert.Equal(adoUser.Id, dapperUser.Id);
            Assert.Equal(adoUser.Name, dapperUser.Name);
            Assert.Equal(adoUser.Email, dapperUser.Email);
            Assert.Equal(adoUser.IsActive, dapperUser.IsActive);

            // Verify both captured SQL commands
            Assert.Single(adoConnection.Commands);
            Assert.Single(dapperConnection.Commands);
            
            // Both should execute similar SQL
            var adoSql = adoConnection.Commands[0].CommandText;
            var dapperSql = dapperConnection.Commands[0].CommandText;
            
            Assert.Contains("SELECT", adoSql);
            Assert.Contains("SELECT", dapperSql);
            Assert.Contains("FROM Users", adoSql);
            Assert.Contains("FROM Users", dapperSql);
            Assert.Contains("WHERE Id =", adoSql);
            Assert.Contains("WHERE Id =", dapperSql);
        }

        [Fact]
        public async Task Dapper_AdvancedFeatures_WorkWithSqlMimic()
        {
            // Arrange
            var connection = new MimicConnection();
            connection.SetupMockData(
                new[] { "IsActive", "Count", "EarliestCreated", "LatestCreated" },
                new object[] { true, 25, DateTime.Now.AddDays(-30), DateTime.Now },
                new object[] { false, 5, DateTime.Now.AddDays(-25), DateTime.Now.AddDays(-1) }
            );
            
            var repository = new DapperUserRepository(connection);

            // Act - Use Dapper's advanced features
            var stats = await repository.GetUserStatsAsync();
            var statsList = stats.ToList();

            // Assert - Verify complex query results
            Assert.Equal(2, statsList.Count);
            
            var activeStats = statsList.First(s => s.IsActive);
            Assert.Equal(25, activeStats.Count);
            
            var inactiveStats = statsList.First(s => !s.IsActive);
            Assert.Equal(5, inactiveStats.Count);

            // Verify complex SQL was captured
            Assert.Single(connection.Commands);
            var command = connection.Commands[0];
            Assert.Contains("COUNT(*) as Count", command.CommandText);
            Assert.Contains("MIN(CreatedAt) as EarliestCreated", command.CommandText);
            Assert.Contains("MAX(CreatedAt) as LatestCreated", command.CommandText);
            Assert.Contains("GROUP BY IsActive", command.CommandText);
        }

        [Fact]
        public async Task SqlMimic_ParameterCapture_WorksWithBothApproaches()
        {
            // Arrange
            var adoConnection = new MimicConnection();
            var dapperConnection = new MimicConnection();
            
            adoConnection.MockReturnValue = 1;
            dapperConnection.MockReturnValue = 1;
            
            var adoRepository = new UserRepository(adoConnection);
            var dapperRepository = new DapperUserRepository(dapperConnection);

            // Act - Execute operations with parameters
            await adoRepository.DeleteAsync(42);
            await dapperRepository.DeleteAsync(42);

            // Assert - Verify parameter capture works for both
            var adoCommand = adoConnection.Commands[0];
            var dapperCommand = dapperConnection.Commands[0];
            
            // Both should have captured the ID parameter
            Assert.Single(adoCommand.Parameters);
            Assert.Single(dapperCommand.Parameters);
            
            // Note: Parameter names might differ between ADO.NET and Dapper
            // but both should capture the value 42
            var adoParam = adoCommand.Parameters.Cast<System.Data.Common.DbParameter>().First();
            var dapperParam = dapperCommand.Parameters.Cast<System.Data.Common.DbParameter>().First();
            
            Assert.Equal(42, adoParam.Value);
            Assert.Equal(42, dapperParam.Value);
        }
    }
}