// GET /api/errors
export async function onRequestGet(context) {
  const { env, request } = context;
  const url = new URL(request.url);

  const category = url.searchParams.get('category');
  const errorCode = url.searchParams.get('errorCode');
  const resolved = url.searchParams.get('resolved');

  let query = 'SELECT * FROM ApiErrors WHERE 1=1';
  const params = [];

  if (category) {
    query += ' AND ErrorCategory = ?';
    params.push(category);
  }
  if (errorCode) {
    query += ' AND ErrorCode LIKE ?';
    params.push(`%${errorCode}%`);
  }
  if (resolved === 'true') {
    query += ' AND IsResolved = 1';
  } else if (resolved === 'false') {
    query += ' AND IsResolved = 0';
  }

  query += ' ORDER BY ErrorTimestamp DESC';

  const { results } = await env.DB.prepare(query).bind(...params).all();

  const data = results.map(mapError);
  return Response.json(data);
}

// POST /api/errors
export async function onRequestPost(context) {
  const { env, request } = context;
  const body = await request.json();

  const id = crypto.randomUUID();
  const now = new Date().toISOString();

  await env.DB.prepare(`
    INSERT INTO ApiErrors (Id, RequestId, ErrorCode, Message, StackTrace, ErrorTimestamp, ErrorCategory, IsResolved, CreatedAt)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
  `).bind(
    id,
    body.requestId,
    body.errorCode || '',
    body.message || null,
    body.stackTrace || null,
    body.errorTimestamp || now,
    body.errorCategory || '',
    body.isResolved ? 1 : 0,
    now
  ).run();

  const { results } = await env.DB.prepare('SELECT * FROM ApiErrors WHERE Id = ?').bind(id).all();
  return new Response(JSON.stringify(mapError(results[0])), { status: 201, headers: { 'Content-Type': 'application/json' } });
}

function mapError(row) {
  return {
    id: row.Id,
    requestId: row.RequestId,
    errorCode: row.ErrorCode,
    message: row.Message,
    stackTrace: row.StackTrace,
    errorTimestamp: row.ErrorTimestamp,
    errorCategory: row.ErrorCategory,
    isResolved: row.IsResolved === 1,
  };
}
