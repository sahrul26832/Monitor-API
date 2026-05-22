using System.Data;
using System.Text;
using Dapper;
using DemoResendInterface.Models.Entities;
using DemoResendInterface.Repositories.Interfaces;

namespace DemoResendInterface.Repositories;

/// <summary>
/// Repository: ApiRequests — Database access only, no business logic.
/// Uses parameterized queries via Dapper.
/// </summary>
public class RequestRepository : IRequestRepository
{
    private readonly IDbConnection _db;

    public RequestRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ApiRequest>> FindAllAsync(
        string? method, string? status, string? url, string? dateFrom, string? dateTo)
    {
        var sql = new StringBuilder("SELECT * FROM ApiRequests WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(method))
        {
            sql.Append(" AND HttpMethod = @Method");
            parameters.Add("Method", method);
        }
        if (!string.IsNullOrEmpty(status))
        {
            sql.Append(" AND Status = @Status");
            parameters.Add("Status", status);
        }
        if (!string.IsNullOrEmpty(url))
        {
            sql.Append(" AND Url LIKE @Url");
            parameters.Add("Url", $"%{url}%");
        }
        if (!string.IsNullOrEmpty(dateFrom))
        {
            sql.Append(" AND CAST(RequestTimestamp AS DATE) >= @DateFrom");
            parameters.Add("DateFrom", dateFrom);
        }
        if (!string.IsNullOrEmpty(dateTo))
        {
            sql.Append(" AND CAST(RequestTimestamp AS DATE) <= @DateTo");
            parameters.Add("DateTo", dateTo);
        }

        sql.Append(" ORDER BY RequestTimestamp DESC");

        return await _db.QueryAsync<ApiRequest>(sql.ToString(), parameters);
    }

    public async Task<ApiRequest?> FindByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM ApiRequests WHERE Id = @Id";
        return await _db.QueryFirstOrDefaultAsync<ApiRequest>(sql, new { Id = id });
    }

    public async Task CreateAsync(ApiRequest entity)
    {
        const string sql = @"
            INSERT INTO ApiRequests 
                (Id, ApplicationName, AppName, Url, HttpMethod, Headers, Body, 
                 ClientIpAddress, RequestTimestamp, Status, ResponseStatusCode, ResponseTime)
            VALUES 
                (@Id, @ApplicationName, @AppName, @Url, @HttpMethod, @Headers, @Body, 
                 @ClientIpAddress, @RequestTimestamp, @Status, @ResponseStatusCode, @ResponseTime)";

        await _db.ExecuteAsync(sql, entity);
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        const string sql = "UPDATE ApiRequests SET Status = @Status WHERE Id = @Id";
        await _db.ExecuteAsync(sql, new { Id = id, Status = status });
    }
}
