// PATCH /api/errors/:id/resolve
export async function onRequestPatch(context) {
  const { env, params } = context;
  const id = params.id;

  const { results } = await env.DB.prepare('SELECT * FROM ApiErrors WHERE Id = ?').bind(id).all();

  if (results.length === 0) {
    return Response.json({ error: 'Error not found' }, { status: 404 });
  }

  await env.DB.prepare(`
    UPDATE ApiErrors SET IsResolved = 1 WHERE Id = ?
  `).bind(id).run();

  return Response.json({
    success: true,
    message: 'Error marked as resolved',
  });
}
