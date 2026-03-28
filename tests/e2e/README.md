# JobFlow API E2E (Playwright)

This folder provides black-box API automation using Playwright's request client.

## Why Playwright for API

- Reuses one runner across UI and API automation.
- Fast HTTP smoke tests and contract checks.
- Good reporting and CI ergonomics.

## Setup

1. Start JobFlow API locally.
2. `cd tests/e2e`
3. `npm install`
4. Configure env (see `.env.example`):
   - PowerShell: `$env:API_BASE_URL = "https://localhost:5099"`
   - PowerShell: `$env:JOBFLOW_API_BEARER_TOKEN = "<seeded-jwt>"`
   - PowerShell: `$env:JOBFLOW_ORGANIZATION_ID = "<org-guid>"`
5. `npm run test:e2e`

## Current coverage

- Swagger UI availability.
- OpenAPI document availability.
- Unknown route behavior.
- End-to-end business lifecycle:
   - Create client
   - Create estimate
   - Upsert job
   - Create and fetch invoice

## Seeded fixtures

- `fixtures.seed.example.json` documents required seed assumptions.
- `JOBFLOW_API_BEARER_TOKEN` should be a token whose claims include `organizationId`.
- Business-flow tests auto-skip when token is not set.

## Next workflow scenarios to automate

1. Firebase login handshake (`/api/auth/login-with-firebase`) with a seeded test identity.
2. Onboarding checklist fetch for a test organization.
3. Client create/list/update lifecycle.
4. Estimate create -> revise -> accept path.
5. Job create/schedule/assignment path.
6. Invoice issue/payment record path.
7. Subscription-gated endpoint behavior by plan.
