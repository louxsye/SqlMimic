using System.Data;
using Dapper;
using SqlMimic.Tests.Models;

namespace SqlMimic.Tests.Repositories
{
    /// <summary>
    /// Dapper-based implementation of User repository
    /// </summary>
    public class DapperUserRepository
    {
        private readonly IDbConnection _connection;

        public DapperUserRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT Id, Name, Email, CreatedAt, IsActive 
                FROM Users 
                WHERE Id = @Id";

            return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<IEnumerable<User>> GetAllActiveAsync()
        {
            const string sql = @"
                SELECT Id, Name, Email, CreatedAt, IsActive 
                FROM Users 
                WHERE IsActive = 1
                ORDER BY Name";

            return await _connection.QueryAsync<User>(sql);
        }

        public async Task<IEnumerable<User>> GetUsersByStatusAsync(bool isActive)
        {
            const string sql = @"
                SELECT Id, Name, Email, CreatedAt, IsActive 
                FROM Users 
                WHERE IsActive = @IsActive
                ORDER BY CreatedAt DESC";

            return await _connection.QueryAsync<User>(sql, new { IsActive = isActive });
        }

        public async Task<int> CreateAsync(User user)
        {
            const string sql = @"
                INSERT INTO Users (Name, Email, CreatedAt, IsActive) 
                VALUES (@Name, @Email, @CreatedAt, @IsActive);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await _connection.QuerySingleAsync<int>(sql, user);
        }

        public async Task<bool> UpdateAsync(User user)
        {
            const string sql = @"
                UPDATE Users 
                SET Name = @Name, Email = @Email, IsActive = @IsActive
                WHERE Id = @Id";

            var rowsAffected = await _connection.ExecuteAsync(sql, user);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Users WHERE Id = @Id";
            
            var rowsAffected = await _connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<int> CountActiveUsersAsync()
        {
            const string sql = "SELECT COUNT(*) FROM Users WHERE IsActive = 1";
            
            return await _connection.QuerySingleAsync<int>(sql);
        }

        public async Task<int> CountUsersByEmailDomainAsync(string domain)
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM Users 
                WHERE Email LIKE @EmailPattern AND IsActive = 1";

            return await _connection.QuerySingleAsync<int>(sql, new { EmailPattern = $"%@{domain}" });
        }

        public async Task<IEnumerable<User>> SearchUsersByNameAsync(string namePattern)
        {
            const string sql = @"
                SELECT Id, Name, Email, CreatedAt, IsActive 
                FROM Users 
                WHERE Name LIKE @NamePattern AND IsActive = 1
                ORDER BY Name";

            return await _connection.QueryAsync<User>(sql, new { NamePattern = $"%{namePattern}%" });
        }

        public async Task<User?> GetUserWithMostRecentActivityAsync()
        {
            const string sql = @"
                SELECT TOP 1 Id, Name, Email, CreatedAt, IsActive 
                FROM Users 
                WHERE IsActive = 1
                ORDER BY CreatedAt DESC";

            return await _connection.QueryFirstOrDefaultAsync<User>(sql);
        }

        public async Task<bool> UpdateUserStatusBatchAsync(int[] userIds, bool isActive)
        {
            const string sql = @"
                UPDATE Users 
                SET IsActive = @IsActive
                WHERE Id IN @UserIds";

            var rowsAffected = await _connection.ExecuteAsync(sql, new { IsActive = isActive, UserIds = userIds });
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<UserStats>> GetUserStatsAsync()
        {
            const string sql = @"
                SELECT 
                    IsActive,
                    COUNT(*) as Count,
                    MIN(CreatedAt) as EarliestCreated,
                    MAX(CreatedAt) as LatestCreated
                FROM Users 
                GROUP BY IsActive";

            return await _connection.QueryAsync<UserStats>(sql);
        }
    }

    /// <summary>
    /// Statistics model for user data
    /// </summary>
    public class UserStats
    {
        public bool IsActive { get; set; }
        public int Count { get; set; }
        public DateTime EarliestCreated { get; set; }
        public DateTime LatestCreated { get; set; }
    }
}