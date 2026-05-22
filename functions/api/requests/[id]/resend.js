// POST /api/requests/:id/resend
export async function onRequestPost(context) {
  const { env, params } = context;
  const id = params.id;

  const { results } = await env.DB.prepare('SELECT * FROM ApiRequests WHERE Id = ?').bind(id).all();

  if (results.length === 0) {
    return Response.json({ error: 'Request not found' }, { status: 404 });
  }

  const original = results[0];

  // Attempt to resend the request
  try {
    const headers = {};
    if (original.Headers) {
      const parsed = JSON.parse(original.Headers);
      Object.assign(headers, parsed);
    }

    const fetchOptions = {
      method: original.HttpMethod,
      headers,
    };

    if (original.Body && !['GET', 'HEAD'].includes(original.HttpMethod.toUpperCase())) {
      fetchOptions.body = original.Body;
    }

    const startTime = Date.now();
    const response = await fetch(original.Url, fetchOptions);
    const responseTime = Date.now() - startTime;

    const newStatus = response.ok ? 'SUCCESS' : 'ERROR';

    await env.DB.prepare(`
      UPDATE ApiRequests SET Status = ?, ResponseStatusCode = ?, ResponseTime = ? WHERE Id = ?
    `).bind(newStatus, response.status, responseTime, id).run();

    return Response.json({
      success: response.ok,
      message: response.ok
        ? `Resend สำเร็จ: ${response.status} (${responseTime}ms)`
        : `Resend ไม่สำเร็จ: ${response.status} (${responseTime}ms)`,
      statusCode: response.status,
      responseTime,
    });
  } catch (err) {
    await env.DB.prepare(`
      UPDATE ApiRequests SET Status = 'ERROR' WHERE Id = ?
    `).bind(id).run();

    return Response.json({
      success: false,
      message: `Resend ไม่สำเร็จ: ${err.message}`,
    }, { status: 500 });
  }
}
