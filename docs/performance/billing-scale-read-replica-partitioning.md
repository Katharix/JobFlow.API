# Billing Scale Plan: Partitioning and Read Replicas

This document covers the next-stage infrastructure plan once API-level query and indexing optimizations are no longer enough.

## Trigger Points

Adopt this plan when one or more of the following are true:

- Payment/billing read endpoints sustain high p95 latency under normal load.
- PaymentHistory row count crosses tens of millions.
- Reporting and operational reads compete with write throughput.
- CPU and IO pressure remain high after query/index tuning.

## Read Replica Strategy

1. Keep writes on primary.
2. Route read-heavy endpoints to replicas:
   - billing/payment history pages
   - disputes lists
   - support-hub financial reporting
3. Keep write-critical paths on primary:
   - checkout
   - refund/capture updates
   - webhook ingestion
4. Add connection-level routing by workload:
   - primary DbContext for writes
   - read-only DbContext for query endpoints
5. Guardrails:
   - mark replica queries as eventually consistent
   - use short-term cache for summary endpoints
   - include fallback-to-primary on replica outage

## Partitioning Strategy (PaymentHistory, later Invoice)

Recommended: date-based monthly partitioning with optional organization sharding key.

1. Partition PaymentHistory by PaidAt month.
2. Keep local indexes aligned with partition key.
3. Add global covering index for EntityId + PaidAt if needed by cross-partition reads.
4. Archive old partitions to colder storage after retention window.
5. Automate partition create/merge/switch operations.

If data skew is extreme by tenant, evaluate composite partitioning:

- primary partition by PaidAt
- secondary strategy by Organization/Entity bucket

## Operational Rollout

1. Stage replica routing behind feature flag.
2. Measure consistency lag and endpoint correctness.
3. Roll out read endpoints incrementally.
4. Introduce partitioning in maintenance windows.
5. Backfill historical rows and validate query plans.
6. Monitor:
   - p50/p95/p99 latency
   - deadlocks/timeouts
   - replica lag
   - storage growth by partition

## Non-Goals

- Do not route webhook idempotency or state-transition writes to replicas.
- Do not partition tables until query/index/caching improvements are exhausted.
