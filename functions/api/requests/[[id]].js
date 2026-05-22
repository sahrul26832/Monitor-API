// GET /api/requests/:id
export async function onRequestGet(context) {
  const { env, params } = context;
  const id = params.id;

  const { results } = await env.DB.prepare('SELECT * FROM ApiRequests WHERE Id = ?').bind(id).all();

  if (results.length === 0) {
    return Response.json({ error: 'Request not found' }, { status: 404 });
  }

  return Response.json(mapRequest(results[0]));
}

// POST /api/requests/:id (handles /resend and /ignore via sub-routes)
// This file handles GET /api/requests/:id only
// Resend and Ignore are in separate files

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
