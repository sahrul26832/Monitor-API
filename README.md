# Monitor API — Cloudflare Pages

API Gateway Monitor Dashboard deployed on Cloudflare Pages with D1 database.

## Setup

### 1. Install dependencies

```bash
npm install
```

### 2. Create D1 database

```bash
npx wrangler d1 create monitor-db
```

Copy the `database_id` from the output and paste it into `wrangler.toml`.

### 3. Initialize database (local)

```bash
npm run db:init
```

### 4. Run locally

```bash
npm run dev
```

### 5. Deploy

```bash
# Initialize remote database
npm run db:init:remote

# Deploy to Cloudflare Pages
npm run deploy
```

## Project Structure

```
monitor-api/
├── public/              ← Static frontend (served by Pages)
│   ├── index.html
│   ├── app.js
│   └── styles.css
├── functions/           ← Pages Functions (API backend)
│   └── api/
│       ├── requests.js          GET/POST /api/requests
│       ├── requests/
│       │   ├── [[id]].js        GET /api/requests/:id
│       │   └── [id]/
│       │       ├── resend.js    POST /api/requests/:id/resend
│       │       └── ignore.js    PATCH /api/requests/:id/ignore
│       ├── errors.js            GET/POST /api/errors
│       └── errors/
│           └── [id]/
│               └── resolve.js   PATCH /api/errors/:id/resolve
├── schema.sql           ← D1 database schema + seed data
├── wrangler.toml        ← Cloudflare config
└── package.json
```

## Cloudflare Pages Build Settings

When connecting via Cloudflare Dashboard:

- **Build command:** (leave empty — no build needed)
- **Build output directory:** `public`
- **D1 binding:** Add `DB` binding to your Pages project settings
