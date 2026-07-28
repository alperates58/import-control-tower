# Open Questions & Decisions Log

## Phase 01 Decisions (Resolved)

1. **32 Permission Catalog & Matrix Structure**:
   - Resolved: Exact 32 permissions across 12 functional groups implemented in `PermissionsCatalog`. Matrix explicitly assigns permissions to 7 system roles. `Management` role is not granted `financial.view` by default.

2. **Refresh Token Cookie & CSRF Security**:
   - Resolved: Production uses `__Host-ict_refresh_token` with `Secure=true`, `HttpOnly=true`, `SameSite=Strict`, `Path=/`, and no `Domain`. CSRF protected endpoints enforce `ALLOWED_ORIGINS` exact matching and `X-ICT-CSRF-Protection: 1` header.

3. **Financial Privacy Guard (`FinancialModuleEnabled`)**:
   - Resolved: Default setting `FinancialModuleEnabled=false` stored in `system_settings`. When false, financial permissions are disabled system-wide for all users including `SystemAdmin` and `Finance`.

4. **Migration & Seeding Locking**:
   - Resolved: Connection-scoped PostgreSQL Advisory Lock (`pg_try_advisory_lock(987654321)`) in a `try-finally` block prevents multi-instance race conditions during startup.

5. **Last SystemAdmin Protection**:
   - Resolved: System prevents disabling or demoting the last active `SystemAdmin` or deleting built-in system roles (`HTTP 409 Conflict`).

---

## Phase 02 Decisions (Resolved)

1. **PostgreSQL Concurrency Token (`xmin`)**:
   - Resolved: Mapped as EF Core shadow property `.Property<uint>("xmin").IsRowVersion()`. Physical `xmin` column DDL creation is completely omitted from migrations, using PostgreSQL's system column.

2. **OpenXML Security & Streaming Architecture**:
   - Resolved: OpenXML v3.2.0 scanning blocks formulas (`FORMULA_NOT_ALLOWED`), external links, embedded OLE objects, and zip bombs (`ratio > 100` on entries > 500KB). ExcelDataReader v3.7.0 reads forward-only without `AsDataSet()`.

3. **Identifier Zero-Preservation & Dates**:
   - Resolved: Text cell leading zeroes preserved (`000123`). Numeric cell zero loss triggers warning. Ambiguous dates checked against ISO-8601 Turkish format. SAS Date placed exclusively at PO Line level (`purchase_order_lines.sas_date`).

4. **Advisory Lock & Idempotent Confirmation**:
   - Resolved: SHA-256 hash derived 2-key advisory lock (`pg_try_advisory_xact_lock`) prevents concurrent processing. `import_confirmation_requests` table with `UNIQUE(import_batch_id, idempotency_key)` guarantees safe retries.

5. **Financial Data Isolation**:
   - Resolved: Phase 02 database schema, DTOs, controllers, audit logs, and frontend views contain exactly 0 financial fields.

---

## Phase 03 Preparation Questions (Upcoming)

1. **Import Case & Shipment Management**:
   - Defining import case lifecycle (Booking, ETD, ETA, Customs Clearance, Delivery).
   - Container tracking and document attachments.
