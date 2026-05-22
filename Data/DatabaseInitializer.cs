using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DemoResendInterface.Data;

/// <summary>
/// Database initializer: Creates database and tables if they don't exist.
/// Replaces the Node.js config/database.js logic.
/// </summary>
public class DatabaseInitializer
{
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var connectionString = _config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var appDbName = builder.InitialCatalog;

        // 1) Connect to master first
        builder.InitialCatalog = "master";
        await using var masterConn = new SqlConnection(builder.ConnectionString);

        var retryCount = 0;
        const int maxRetries = 10;

        while (retryCount < maxRetries)
        {
            try
            {
                await masterConn.OpenAsync();
                _logger.LogInformation("Connected to SQL Server (master)");
                break;
            }
            catch (SqlException ex)
            {
                retryCount++;
                _logger.LogWarning("Database connection attempt {Attempt}/{Max} failed: {Message}",
                    retryCount, maxRetries, ex.Message);

                if (retryCount >= maxRetries)
                    throw;

                _logger.LogInformation("Retrying in 5 seconds...");
                await Task.Delay(5000);
            }
        }

        // 2) Create database if not exists
        await masterConn.ExecuteAsync($@"
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @DbName)
            BEGIN
                CREATE DATABASE [{appDbName}];
            END", new { DbName = appDbName });
        _logger.LogInformation("Database [{DbName}] ensured", appDbName);

        await masterConn.CloseAsync();

        // 3) Connect to app database and create tables
        await using var appConn = new SqlConnection(connectionString);
        await appConn.OpenAsync();
        _logger.LogInformation("Connected to [{DbName}]", appDbName);

        await InitializeTablesAsync(appConn);
    }

    private async Task InitializeTablesAsync(IDbConnection db)
    {
        // Create ApiRequests table
        await db.ExecuteAsync(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ApiRequests' AND xtype='U')
            BEGIN
                CREATE TABLE ApiRequests (
                    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    ApplicationName NVARCHAR(100) NOT NULL,
                    AppName NVARCHAR(100),
                    Url NVARCHAR(500) NOT NULL,
                    HttpMethod VARCHAR(10) NOT NULL,
                    Headers NVARCHAR(MAX),
                    Body NVARCHAR(MAX),
                    ClientIpAddress VARCHAR(45),
                    RequestTimestamp DATETIME2 NOT NULL,
                    Status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
                    ResponseStatusCode INT NULL,
                    ResponseTime INT DEFAULT 0,
                    CreatedAt DATETIME2 DEFAULT GETDATE()
                );

                CREATE INDEX IX_ApiRequests_Status ON ApiRequests(Status);
                CREATE INDEX IX_ApiRequests_HttpMethod ON ApiRequests(HttpMethod);
                CREATE INDEX IX_ApiRequests_RequestTimestamp ON ApiRequests(RequestTimestamp DESC);
            END
            ELSE
            BEGIN
                IF COL_LENGTH('ApiRequests', 'AppName') IS NULL
                BEGIN
                    ALTER TABLE ApiRequests ADD AppName NVARCHAR(100);
                END
            END");

        // Create ApiErrors table
        await db.ExecuteAsync(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ApiErrors' AND xtype='U')
            BEGIN
                CREATE TABLE ApiErrors (
                    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    RequestId UNIQUEIDENTIFIER NOT NULL,
                    ErrorCode VARCHAR(50) NOT NULL,
                    Message NVARCHAR(500),
                    StackTrace NVARCHAR(MAX),
                    ErrorTimestamp DATETIME2 NOT NULL,
                    ErrorCategory VARCHAR(30) NOT NULL,
                    IsResolved BIT DEFAULT 0,
                    CreatedAt DATETIME2 DEFAULT GETDATE(),
                    CONSTRAINT FK_ApiErrors_ApiRequests FOREIGN KEY (RequestId) REFERENCES ApiRequests(Id)
                );

                CREATE INDEX IX_ApiErrors_ErrorCategory ON ApiErrors(ErrorCategory);
                CREATE INDEX IX_ApiErrors_IsResolved ON ApiErrors(IsResolved);
                CREATE INDEX IX_ApiErrors_RequestId ON ApiErrors(RequestId);
            END");

        // Seed data if tables are empty
        await SeedDataAsync(db);

        _logger.LogInformation("Database tables initialized");
    }

    private async Task SeedDataAsync(IDbConnection db)
    {
        var requestCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ApiRequests");
        if (requestCount > 0) return;

        _logger.LogInformation("Seeding initial data...");

        await db.ExecuteAsync(@"
            INSERT INTO ApiRequests (Id, ApplicationName, AppName, Url, HttpMethod, Headers, Body, ClientIpAddress, RequestTimestamp, Status, ResponseStatusCode, ResponseTime) VALUES
            ('a1b2c3d4-e5f6-7890-abcd-ef1234567890','User Service', 'alice', '/api/users','GET','{""Accept"":""application/json"",""Authorization"":""Bearer eyJ...""}',NULL,'192.168.1.10','2024-01-15T08:30:00','SUCCESS',200,120),
            ('b2c3d4e5-f6a7-8901-bcde-f12345678901','User Service', 'bob', '/api/users/42','GET','{""Accept"":""application/json"",""Authorization"":""Bearer eyJ...""}',NULL,'10.0.0.5','2024-01-15T09:15:22','SUCCESS',200,85),
            ('c3d4e5f6-a7b8-9012-cdef-123456789012','Order Service', 'charlie', '/api/orders','GET','{""Accept"":""application/json"",""Authorization"":""Bearer eyJ...""}',NULL,'172.16.0.100','2024-01-14T14:45:10','SUCCESS',200,230),
            ('e5f6a7b8-c9d0-1234-efab-345678901234','User Service', 'david', '/api/users/999','GET','{""Accept"":""application/json"",""Authorization"":""Bearer eyJ...""}',NULL,'10.10.10.1','2024-01-13T16:30:45','ERROR',404,55),
            ('a7b8c9d0-e1f2-3456-abcd-567890123456','Order Service', 'eve', '/api/orders/128','GET','{""Accept"":""application/json"",""Authorization"":""Bearer eyJ...""}',NULL,'10.0.1.15','2024-01-12T09:50:18','ERROR',500,3200),
            ('c9d0e1f2-a3b4-5678-cdef-789012345678','User Service', NULL, '/api/users','GET','{""Accept"":""application/json""}',NULL,'192.168.3.75','2024-01-11T15:20:33','ERROR',503,5000),
            ('e1f2a3b4-c5d6-7890-efab-901234567890','User Service', 'admin', '/api/users','POST','{""Content-Type"":""application/json"",""Authorization"":""Bearer eyJ...""}','{""name"":""John Doe"",""email"":""john@example.com"",""role"":""admin""}','192.168.1.10','2024-01-15T10:00:00','SUCCESS',201,180),
            ('b4c5d6e7-f8a9-0123-bcde-234567890123','Auth Service', NULL, '/api/auth/login','POST','{""Content-Type"":""application/json""}','{""username"":""hacker"",""password"":""********""}','203.0.113.50','2024-01-13T22:15:30','ERROR',401,60),
            ('d6e7f8a9-b0c1-2345-defa-456789012345','Payment Service', 'alice', '/api/payments','POST','{""Content-Type"":""application/json"",""Authorization"":""Bearer eyJ...""}','{""orderId"":128,""amount"":299.99,""currency"":""USD"",""method"":""credit_card""}','10.0.0.5','2024-01-12T18:30:00','ERROR',502,4500),
            ('f8a9b0c1-d2e3-4567-fabc-678901234567','Order Service', 'bob', '/api/orders','POST','{""Content-Type"":""application/json"",""Authorization"":""Bearer eyJ...""}','{""productId"":7,""quantity"":1,""shippingAddress"":""456 Oak Ave""}','172.16.2.30','2024-01-11T20:05:10','ERROR',400,70),
            ('e3f4a5b6-c7d8-9012-efab-123456789012','Order Service', 'charlie', '/api/orders/128','PUT','{""Content-Type"":""application/json"",""Authorization"":""Bearer eyJ...""}','{""status"":""shipped"",""trackingNumber"":""TRK-98765""}','172.16.0.100','2024-01-13T17:20:15','ERROR',403,80),
            ('f4a5b6c7-d8e9-0123-fabc-234567890123','User Service', 'admin', '/api/users/100','PUT','{""Content-Type"":""application/json"",""Authorization"":""Bearer eyJ...""}','{""name"":""Alice Wonder"",""email"":""alice@example.com""}','192.168.2.50','2024-01-12T10:05:45','ERROR',500,2800),
            ('d0e1f2a3-b4c5-6789-defa-890123456789','Product Service', NULL, '/api/products','GET','{""Accept"":""application/json"",""X-Request-ID"":""req-001""}',NULL,'10.20.30.40','2024-01-11T12:10:15','PENDING',NULL,0),
            ('44444444-4444-4444-4444-444444444444','Order Service', 'david', '/api/orders','POST','{""Content-Type"":""application/json"",""Authorization"":""Bearer eyJ...""}','{""productId"":22,""quantity"":5,""shippingAddress"":""789 Pine Rd""}','192.168.6.30','2024-01-14T17:45:15','ERROR',500,4200),
            ('55556666-7777-8888-9999-000011112222','Order Service', 'eve', '/api/orders/450','PUT','{""Content-Type"":""application/json"",""Authorization"":""Bearer eyJ...""}','{""status"":""processing"",""estimatedDelivery"":""2024-01-20""}','10.0.7.80','2024-01-07T09:50:45','ERROR',503,5000)");

        await db.ExecuteAsync(@"
            INSERT INTO ApiErrors (Id, RequestId, ErrorCode, Message, StackTrace, ErrorTimestamp, ErrorCategory, IsResolved) VALUES
            ('e0010001-aaaa-bbbb-cccc-ddddeeee0001','e5f6a7b8-c9d0-1234-efab-345678901234','404','User not found: No user exists with ID 999','NotFoundError: User not found\n    at UserService.findById','2024-01-13T16:30:45.120','CLIENT_ERROR',1),
            ('e0010002-aaaa-bbbb-cccc-ddddeeee0002','b4c5d6e7-f8a9-0123-bcde-234567890123','401','Authentication failed: Invalid credentials for user hacker','UnauthorizedError: Authentication failed\n    at AuthService.validateCredentials','2024-01-13T22:15:30.085','CLIENT_ERROR',0),
            ('e0010003-aaaa-bbbb-cccc-ddddeeee0003','f8a9b0c1-d2e3-4567-fabc-678901234567','400','Validation error: productId must be positive integer','ValidationError: Request body validation failed\n    at OrderValidator.validate','2024-01-11T20:05:10.200','CLIENT_ERROR',1),
            ('e0010004-aaaa-bbbb-cccc-ddddeeee0004','e3f4a5b6-c7d8-9012-efab-123456789012','403','Forbidden: Insufficient permissions to update order #128','ForbiddenError: Insufficient permissions\n    at AuthorizationMiddleware.checkPermission','2024-01-13T17:20:15.340','CLIENT_ERROR',0),
            ('e0010010-aaaa-bbbb-cccc-ddddeeee0010','a7b8c9d0-e1f2-3456-abcd-567890123456','500','Internal Server Error: Database query failed for order #128','DatabaseError: Connection pool exhausted\n    at Pool.acquire\n    at OrderRepository.findById','2024-01-12T09:50:18.500','SERVER_ERROR',0),
            ('e0010011-aaaa-bbbb-cccc-ddddeeee0011','f4a5b6c7-d8e9-0123-fabc-234567890123','500','Internal Server Error: Null reference in user update handler','TypeError: Cannot read properties of null\n    at UserService.updateUser','2024-01-12T10:05:45.800','SERVER_ERROR',1),
            ('e0010013-aaaa-bbbb-cccc-ddddeeee0013','44444444-4444-4444-4444-444444444444','500','Internal Server Error: Inventory lock timeout','Error: Inventory lock acquisition timeout after 5000ms\n    at InventoryService.acquireLock','2024-01-14T17:45:15.900','SERVER_ERROR',1),
            ('e0010016-aaaa-bbbb-cccc-ddddeeee0016','c9d0e1f2-a3b4-5678-cdef-789012345678','TIMEOUT','Request timeout: GET /api/users exceeded 5000ms','TimeoutError: Request timed out after 5000ms\n    at Timeout._onTimeout','2024-01-11T15:20:38.000','TIMEOUT_ERROR',0),
            ('e0010017-aaaa-bbbb-cccc-ddddeeee0017','55556666-7777-8888-9999-000011112222','TIMEOUT','Request timeout: PUT /api/orders/450 upstream did not respond','TimeoutError: Upstream service response timeout\n    at Timeout._onTimeout','2024-01-07T09:50:50.700','TIMEOUT_ERROR',1),
            ('e0010018-aaaa-bbbb-cccc-ddddeeee0018','d6e7f8a9-b0c1-2345-defa-456789012345','CONNECTION_REFUSED','Connection refused: Payment service at 10.0.5.100:8443 unreachable','Error: connect ECONNREFUSED 10.0.5.100:8443\n    at TCPConnectWrap.afterConnect','2024-01-12T18:30:04.500','CONNECTION_ERROR',0),
            ('e0010020-aaaa-bbbb-cccc-ddddeeee0020','d6e7f8a9-b0c1-2345-defa-456789012345','GATEWAY_INTERNAL','Gateway internal error: Rate limiter config failed','GatewayError: Failed to initialize rate limiter\n    at RateLimiter.init','2024-01-12T18:30:02.100','GATEWAY_ERROR',0),
            ('e0010021-aaaa-bbbb-cccc-ddddeeee0021','c9d0e1f2-a3b4-5678-cdef-789012345678','GATEWAY_ROUTING','Gateway routing error: Failed to resolve upstream for /api/users','GatewayError: Service registry lookup failed\n    at ServiceRegistry.resolve','2024-01-11T15:20:35.500','GATEWAY_ERROR',1)");

        _logger.LogInformation("Seed data inserted");
    }
}
