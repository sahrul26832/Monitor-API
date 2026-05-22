// GET /api/requests
export async function onRequestGet(context) {
  const { env, request } = context;
  const url = new URL(request.url);

  const method = url.searchParams.get('method');
  const status = url.searchParams.get('status');
  const urlFilter = url.searchParams.get('url');
  const dateFrom = url.searchParams.get('dateFrom');
  const dateTo = url.searchParams.get('dateTo');

  let query = 'SELECT * FROM ApiRequests WHERE 1=1';
  const params = [];

  if (method) {
    query += ' AND HttpMethod = ?';
    params.push(method);
  }
  if (status) {
    query += ' AND Status = ?';
    params.push(status);
  }
  if (urlFilter) {
    query += ' AND Url LIKE ?';
    params.push(`%${urlFilter}%`);
  }
  if (dateFrom) {
    query += ' AND RequestTimestamp >= ?';
    params.push(dateFrom);
  }
  if (dateTo) {
    query += ' AND RequestTimestamp <= ?';
    params.push(dateTo + 'T23:59:59');
  }

  query += ' ORDER BY RequestTimestamp DESC';

  const { results } = await env.DB.prepare(query).bind(...params).all();

  const data = results.map(mapRequest);
  return Response.json(data);
}

// POST /api/requests
export async function onRequestPost(context) {
  const { env, request } = context;
  const body = await request.json();

  const id = crypto.randomUUID();
  const now = new Date().toISOString();

  await env.DB.prepare(`
    INSERT INTO ApiRequests (Id, ApplicationName, AppName, Url, HttpMethod, Headers, Body, ClientIpAddress, RequestTimestamp, Status, ResponseStatusCode, ResponseTime, CreatedAt)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
  `).bind(
    id,
    body.applicationName || '',
    body.appName || null,
    body.url || '',
    body.httpMethod || 'GET',
    body.headers ? (typeof body.headers === 'string' ? body.headers : JSON.stringify(body.headers)) : null,
    body.body || null,
    body.clientIpAddress || null,
    body.requestTimestamp || now,
    body.status || 'PENDING',
    body.responseStatusCode || null,
    body.responseTime || 0,
    now
  ).run();

  const { results } = await env.DB.prepare('SELECT * FROM ApiRequests WHERE Id = ?').bind(id).all();
  return new Response(JSON.stringify(mapRequest(results[0])), { status: 201, headers: { 'Content-Type': 'application/json' } });
}

function mapRequest(row) {
  return {
    id: row.Id,
    applicationName: row.ApplicationName,
    appName: row.AppName,
    url: row.Url,
    httpMethod: row.HttpMethod,
    headers: tryParseJson(row.Headers),
    body: row.Body,
    clientIpAddress: row.ClientIpAddress,
    requestTimestamp: row.RequestTimestamp,
    status: row.Status,
    responseStatusCode: row.ResponseStatusCode,
    responseTime: row.ResponseTime,
  };
}

function tryParseJson(str) {
  if (!str) return null;
  try { return JSON.parse(str); } catch { return str; }
}
