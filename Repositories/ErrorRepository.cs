using System.Data;
using System.Text;
using Dapper;
using DemoResendInterface.Models.Entities;
using DemoResendInterface.Repositories.Interfaces;

namespace DemoResendInterface.Repositories;

/// <summary>
/// Repository: ApiErrors — Database access only, no business logic.
/// Uses parameterized queries via Dapper.
/// </summary>
public class ErrorRepository : IErrorRepository
{
    private readonly IDbConnection _db;

    public ErrorRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ApiError>> FindAllAsync(
        string? category, string? errorCode, string? resolved)
    {
        var sql = new StringBuilder("SELECT * FROM ApiErrors WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(category))
        {
            sql.Append(" AND ErrorCategory = @Category");
            parameters.Add("Category", category);
        }
        if (!string.IsNullOrEmpty(errorCode))
        {
            sql.Append(" AND ErrorCode LIKE @ErrorCode");
            parameters.Add("ErrorCode", $"%{errorCode}%");
        }
        if (!string.IsNullOrEmpty(resolved))
        {
            sql.Append(" AND IsResolved = @Resolved");
            parameters.Add("Resolved", resolved == "true" ? 1 : 0);
        }

        sql.Append(" ORDER BY ErrorTimestamp DESC");

        return await _db.QueryAsync<ApiError>(sql.ToString(), parameters);
    }

    public async Task CreateAsync(ApiError entity)
    {
        const string sql = @"
            INSERT INTO ApiErrors 
                (Id, RequestId, ErrorCode, Message, StackTrace, 
                 ErrorTimestamp, ErrorCategory, IsResolved)
            VALUES 
                (@Id, @RequestId, @ErrorCode, @Message, @StackTrace, 
                 @ErrorTimestamp, @ErrorCategory, @IsResolved)";

        await _db.ExecuteAsync(sql, entity);
    }

    public async Task MarkResolvedAsync(Guid id)
    {
        const string sql = "UPDATE ApiErrors SET IsResolved = 1 WHERE Id = @Id";
        await _db.ExecuteAsync(sql, new { Id = id });
    }
}
