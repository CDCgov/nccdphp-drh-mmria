# MMRIA Database Migration: CouchDB → SQL
### Leadership Brief

---

## The Bottom Line

MMRIA currently runs on **145 separate infrastructure components** in production. A migration to a SQL database would reduce that to **3**. This is not a technology preference — it is an operational and financial necessity.

---

## Where We Are Today

MMRIA serves 72 jurisdictions (states, territories, and tribal organizations). Each jurisdiction runs its own isolated set of infrastructure:

| Component | Count | Notes |
|---|---|---|
| Application server pods | 72 | One per jurisdiction |
| Database pods | 72 | One per jurisdiction |
| Shared services pod | 1 | MMRIA services |
| **Total** | **145 pods** | All managed, monitored, updated individually |

Every release, patch, or configuration change must be applied across all 72 jurisdictions. A configuration file stored in the database means there are 72 copies of that configuration — each of which must be kept in sync.

---

## The Near Term: Multi-Tenant Mode

Work is underway to run multiple jurisdictions on a single application pod (multi-tenant mode). This is meaningful progress and reduces application pods from 72 to 1. However, the 72 database pods remain.

| Component | Count |
|---|---|
| Application server pods | 1 (multi-tenant) |
| Database pods | 72 |
| Shared services pod | 1 |
| **Total** | **74 pods** |

This is an improvement, but the database infrastructure — and the operational complexity it carries — is still largely intact.

---

## The Goal: SQL Migration

Migrating from CouchDB to SQL resolves the infrastructure problem at its root.

| Component | Count |
|---|---|
| Application server | 1 |
| SQL database | 1 |
| MMRIA services | 1 |
| **Total** | **3** |

Data separation between jurisdictions is maintained within the single database using standard, well-understood SQL patterns (per-row client keys, schema separation, or cloud-native partitioning — the right approach can be chosen based on security and compliance requirements).

---

## Why This Matters Beyond Infrastructure

### 1. Release Complexity
Today, each release requires updating configuration and metadata stored across 72 separate databases. This is one of the most error-prone and time-consuming parts of every deployment. SQL consolidates this into a single, version-controlled location.

### 2. Application Complexity
The codebase carries years of accumulated complexity from supporting both single-tenant and multi-tenant modes, as well as the evolution from a centralized to a decentralized architecture and back. This makes the application difficult to maintain and especially difficult for new team members to learn. SQL migration is an opportunity to remove that complexity at its source.

### 3. Resource Consumption at Scale
CouchDB is memory-intensive by nature — it loads data into memory aggressively and does not release it. Each jurisdiction's database instance consumes approximately **800 MB of memory at idle**, and that figure grows over time even when no users are active. Across 72 database pods, this means MMRIA is consuming over **57 GB of memory at a minimum, around the clock, regardless of user activity**. A single SQL database instance serving all jurisdictions would eliminate this compounding idle cost.

### 4. Technology Supportability
CouchDB is a capable database, but it is a niche technology with a narrower talent pool and tooling ecosystem than SQL. SQL is the most widely supported database technology in the industry. This improves hiring options, vendor support, and long-term maintainability.

### 5. Data Integrity
SQL databases provide mature, well-tested tools for enforcing data integrity — constraints, transactions, foreign keys. These reduce the risk of data inconsistency and make auditing and reporting more reliable.

### 6. Developer Onboarding
The current architecture — multiple databases, spread configuration, single-tenant/multi-tenant toggle code — creates a steep learning curve for new developers. A simpler, SQL-backed architecture is easier to understand, test, and extend.

### 7. Development and Testing Environment Costs
The production architecture does not exist in isolation. Development, QA, and Integration environments each require their own set of pods to support the application — and while those environments do not mirror production one-to-one, they still carry a significantly larger footprint than necessary. That excess has a direct impact on cloud resource costs and environment setup time. The future state model of one application, one database, and one services component would make standing up or tearing down any environment fast and inexpensive.

---

## Summary

| Dimension | Today | After SQL Migration |
|---|---|---|
| Infrastructure pods | 145 | 3 |
| Release surface area | 72 databases to update | 1 |
| Configuration management | Distributed across 72 DBs | Centralized |
| Technology familiarity | Niche (CouchDB) | Universal (SQL) |
| Idle memory consumption | ~57 GB minimum (72 × ~800 MB), growing | Fraction of that, consolidated |
| Developer onboarding difficulty | High | Moderate |
| Dev/QA/INT environment footprint | Excess pods across all environments | 3 components per environment |

The SQL migration is not a rewrite of MMRIA's business logic. It is a focused infrastructure and data-layer change that reduces operational cost, simplifies deployments, and puts the application on a more sustainable foundation for the long term.

---

*Prepared by: MMRIA Engineering Team*  
*Date: August 2026*
