# PCI Scope Boundary Notes

## Payment Data Handling Model
- JobFlow delegates payment processing to third-party processors (Stripe and Square).
- JobFlow does not intentionally store full PAN, CVV, or magnetic-stripe-equivalent card data.
- Payment provider identifiers (for example, provider payment IDs, customer IDs, merchant/account IDs) are persisted for reconciliation and operational workflows.

## In-Scope Components
- API endpoints that create, adjust, refund, or link payment accounts.
- Webhook endpoints that accept payment events from Stripe/Square.
- Configuration and secret-management paths that hold payment API credentials and webhook signing secrets.

## Out-of-Scope Assumptions
- Cardholder data entry, tokenization, and storage are performed by Stripe/Square-hosted components or SDK flows.
- No full card data is logged, persisted, or transmitted through JobFlow-owned systems.

## Required Operational Controls
- Maintain strict least-privilege for payment-related credentials.
- Rotate credentials and webhook secrets on exposure events.
- Keep immutable audit logs for payment-sensitive operations.
- Monitor and alert on unusual payment endpoint and webhook activity.
- Revalidate these assumptions whenever payment workflows change.
