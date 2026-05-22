-- ===== Create Tables =====

CREATE TABLE IF NOT EXISTS ApiRequests (
    Id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6)))),
    ApplicationName TEXT NOT NULL,
    AppName TEXT,
    Url TEXT NOT NULL,
    HttpMethod TEXT NOT NULL,
    Headers TEXT,
    Body TEXT,
    ClientIpAddress TEXT,
    RequestTimestamp TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'PENDING',
    ResponseStatusCode INTEGER,
    ResponseTime INTEGER DEFAULT 0,
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS IX_ApiRequests_Status ON ApiRequests(Status);
CREATE INDEX IF NOT EXISTS IX_ApiRequests_HttpMethod ON ApiRequests(HttpMethod);
CREATE INDEX IF NOT EXISTS IX_ApiRequests_RequestTimestamp ON ApiRequests(RequestTimestamp);

CREATE TABLE IF NOT EXISTS ApiErrors (
    Id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6)))),
    RequestId TEXT NOT NULL,
    ErrorCode TEXT NOT NULL,
    Message TEXT,
    StackTrace TEXT,
    ErrorTimestamp TEXT NOT NULL,
    ErrorCategory TEXT NOT NULL,
    IsResolved INTEGER DEFAULT 0,
    CreatedAt TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (RequestId) REFERENCES ApiRequests(Id)
);

CREATE INDEX IF NOT EXISTS IX_ApiErrors_ErrorCategory ON ApiErrors(ErrorCategory);
CREATE INDEX IF NOT EXISTS IX_ApiErrors_IsResolved ON ApiErrors(IsResolved);
CREATE INDEX IF NOT EXISTS IX_ApiErrors_RequestId ON ApiErrors(RequestId);

-- ===== Seed Data =====

INSERT OR IGNORE INTO ApiRequests (Id, ApplicationName, AppName, Url, HttpMethod, Headers, Body, ClientIpAddress, RequestTimestamp, Status, ResponseStatusCode, ResponseTime) VALUES
('a1b2c3d4-e5f6-7890-abcd-ef1234567890','User Service','alice','/api/users','GET','{"Accept":"application/json","Authorization":"Bearer eyJ..."}',NULL,'192.168.1.10','2024-01-15T08:30:00','SUCCESS',200,120),
('b2c3d4e5-f6a7-8901-bcde-f12345678901','User Service','bob','/api/users/42','GET','{"Accept":"application/json","Authorization":"Bearer eyJ..."}',NULL,'10.0.0.5','2024-01-15T09:15:22','SUCCESS',200,85),
('c3d4e5f6-a7b8-9012-cdef-123456789012','Order Service','charlie','/api/orders','GET','{"Accept":"application/json","Authorization":"Bearer eyJ..."}',NULL,'172.16.0.100','2024-01-14T14:45:10','SUCCESS',200,230),
('e5f6a7b8-c9d0-1234-efab-345678901234','User Service','david','/api/users/999','GET','{"Accept":"application/json","Authorization":"Bearer eyJ..."}',NULL,'10.10.10.1','2024-01-13T16:30:45','ERROR',404,55),
('a7b8c9d0-e1f2-3456-abcd-567890123456','Order Service','eve','/api/orders/128','GET','{"Accept":"application/json","Authorization":"Bearer eyJ..."}',NULL,'10.0.1.15','2024-01-12T09:50:18','ERROR',500,3200),
('c9d0e1f2-a3b4-5678-cdef-789012345678','User Service',NULL,'/api/users','GET','{"Accept":"application/json"}',NULL,'192.168.3.75','2024-01-11T15:20:33','ERROR',503,5000),
('e1f2a3b4-c5d6-7890-efab-901234567890','User Service','admin','/api/users','POST','{"Content-Type":"application/json","Authorization":"Bearer eyJ..."}','{"name":"John Doe","email":"john@example.com","role":"admin"}','192.168.1.10','2024-01-15T10:00:00','SUCCESS',201,180),
('b4c5d6e7-f8a9-0123-bcde-234567890123','Auth Service',NULL,'/api/auth/login','POST','{"Content-Type":"application/json"}','{"username":"hacker","password":"********"}','203.0.113.50','2024-01-13T22:15:30','ERROR',401,60),
('d6e7f8a9-b0c1-2345-defa-456789012345','Payment Service','alice','/api/payments','POST','{"Content-Type":"application/json","Authorization":"Bearer eyJ..."}','{"orderId":128,"amount":299.99,"currency":"USD","method":"credit_card"}','10.0.0.5','2024-01-12T18:30:00','ERROR',502,4500),
('f8a9b0c1-d2e3-4567-fabc-678901234567','Order Service','bob','/api/orders','POST','{"Content-Type":"application/json","Authorization":"Bearer eyJ..."}','{"productId":7,"quantity":1,"shippingAddress":"456 Oak Ave"}','172.16.2.30','2024-01-11T20:05:10','ERROR',400,70),
('e3f4a5b6-c7d8-9012-efab-123456789012','Order Service','charlie','/api/orders/128','PUT','{"Content-Type":"application/json","Authorization":"Bearer eyJ..."}','{"status":"shipped","trackingNumber":"TRK-98765"}','172.16.0.100','2024-01-13T17:20:15','ERROR',403,80),
('f4a5b6c7-d8e9-0123-fabc-234567890123','User Service','admin','/api/users/100','PUT','{"Content-Type":"application/json","Authorization":"Bearer eyJ..."}','{"name":"Alice Wonder","email":"alice@example.com"}','192.168.2.50','2024-01-12T10:05:45','ERROR',500,2800),
('d0e1f2a3-b4c5-6789-defa-890123456789','Product Service',NULL,'/api/products','GET','{"Accept":"application/json","X-Request-ID":"req-001"}',NULL,'10.20.30.40','2024-01-11T12:10:15','PENDING',NULL,0),
('44444444-4444-4444-4444-444444444444','Order Service','david','/api/orders','POST','{"Content-Type":"application/json","Authorization":"Bearer eyJ..."}','{"productId":22,"quantity":5,"shippingAddress":"789 Pine Rd"}','192.168.6.30','2024-01-14T17:45:15','ERROR',500,4200),
('55556666-7777-8888-9999-000011112222','Order Service','eve','/api/orders/450','PUT','{"Content-Type":"application/json","Authorization":"Bearer eyJ..."}','{"status":"processing","estimatedDelivery":"2024-01-20"}','10.0.7.80','2024-01-07T09:50:45','ERROR',503,5000);

INSERT OR IGNORE INTO ApiErrors (Id, RequestId, ErrorCode, Message, StackTrace, ErrorTimestamp, ErrorCategory, IsResolved) VALUES
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
('e0010021-aaaa-bbbb-cccc-ddddeeee0021','c9d0e1f2-a3b4-5678-cdef-789012345678','GATEWAY_ROUTING','Gateway routing error: Failed to resolve upstream for /api/users','GatewayError: Service registry lookup failed\n    at ServiceRegistry.resolve','2024-01-11T15:20:35.500','GATEWAY_ERROR',1);
