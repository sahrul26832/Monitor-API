// PATCH /api/requests/:id/ignore
export async function onRequestPatch(context) {
  const { env, params } = context;
  const id = params.id;

  const { results } = await env.DB.prepare('SELECT * FROM ApiRequests WHERE Id = ?').bind(id).all();

  if (results.length === 0) {
    return Response.json({ error: 'Request not found' }, { status: 404 });
  }

  await env.DB.prepare(`
    UPDATE ApiRequests SET Status = 'IGNORED' WHERE Id = ?
  `).bind(id).run();

  return Response.json({
    success: true,
    message: 'Request marked as IGNORED',
  });
}
