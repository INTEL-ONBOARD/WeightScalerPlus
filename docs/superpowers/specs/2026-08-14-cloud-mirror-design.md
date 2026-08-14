# Cloud Mirror — Design

**Date:** 2026-08-14
**Status:** Approved for planning
**Branch:** `feature/cloud-mirror`

## Problem

Each branch runs WeightMaster against its own local MySQL. Nothing outside that
branch can see the data. There is no consolidated view, no off-site copy, and no
record of what the app sends to teacoop.lk.

Two requirements:

1. Mirror every table and every row from every branch into one cloud MySQL,
   whenever the branch has internet.
2. Add a table holding the full data of everything posted through the API.

## Governing constraint

This must not affect any currently working feature.

That is not a footnote — it decides the architecture. A weighing station has a
farmer standing at the scale. If a sync defect makes a save fail, the cost is a
queue in the yard, not a stale dashboard.

Every mechanism below is chosen for one property: **it can degrade to doing
nothing, but it cannot break a write.** Where that meant a slower design over a
cleverer one, the slower one won.

## Goals

- One-way replication: local MySQL is the sole source of truth.
- All 12 existing tables mirrored, plus the new API log.
- Many branches into one shared cloud database, keyed so they cannot collide.
- Survives intermittent connectivity; catches up automatically.
- Self-healing: a missed change is eventually corrected without intervention.
- Disable-able at any branch without a redeploy.

## Non-goals

- No writes flowing down from cloud to branch. The cloud copy is read-only to
  everything except the sync.
- No conflict resolution. One-way replication makes conflicts impossible.
- No change to any existing report, docket, query or UI behaviour.
- No backfill of data already sent to teacoop.lk before this ships.

## Decisions taken during design

| Question | Decision | Rationale |
|---|---|---|
| Direction | One-way mirror | No conflicts possible; smallest blast radius |
| Multi-branch | One shared cloud DB, `(branch_id, local_id)` key | Cross-branch reporting in one query; identities cannot collide |
| Sync host | Background thread inside WeightMaster | No second installer per branch |
| Change detection | Triggers + outbox, **plus** read-only differ | Precision from triggers, convergence from the differ |

## Architecture

```
Station 1 / Station 2 (unchanged write paths)
        |
        v
   local MySQL  --trigger-->  sync_outbox
        |                          |
        |                          v
        |                    SyncWorker  --TLS-->  cloud MySQL
        |                          ^                (branch_id, local_id)
        +-----read-only differ-----+

   ApiClient.HttpClient --DelegatingHandler--> api_post_log --> (mirrored)
```

## Table classification

The 12 tables do not behave alike. Treating them alike is what would make this
fragile.

### Operational — triggers, row-level upsert

`transactionData`, `FinaltransactionData`, `GreenLeafPosts`, `PostStatus`,
`TransportBill`, `RunLog`, `UserLoginsLog`, `memDbLog`, `lineDbLog`,
`api_post_log`

Real local data with stable, meaningful identity keys.

### Master — snapshot, no triggers

`UsersData`, `MembersData`, `lineMasterData`

`ReplaceUsersAsync` and `ReplaceMembersAsync` call `RemoveRange` over the whole
table and reinsert it, so these IDs change on every refresh and carry no
meaning. They are also already downloaded *from* teacoop.lk.

Triggers here would be actively harmful: a member refresh at a 20,000-member
branch would fire 40,000 trigger writes and flood the outbox with rows whose IDs
are about to become meaningless.

Instead: hash the table's content each cycle. When the hash changes, replace the
branch's cloud copy inside one transaction.

### Sync-owned — never mirrored

`sync_outbox`, `sync_state`

## Local schema additions

Three new tables. No existing table is altered.

```sql
CREATE TABLE sync_outbox (
  seq        BIGINT AUTO_INCREMENT PRIMARY KEY,
  table_name VARCHAR(64)  NOT NULL,
  row_pk     BIGINT       NOT NULL,
  op         CHAR(1)      NOT NULL,        -- I / U / D
  changed_at DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=InnoDB;

CREATE TABLE sync_state (
  table_name     VARCHAR(64) PRIMARY KEY,
  watermark_id   BIGINT      NOT NULL DEFAULT 0,
  backfill_done  TINYINT(1)  NOT NULL DEFAULT 0,
  backfill_pos   BIGINT      NOT NULL DEFAULT 0,
  last_sweep_at  DATETIME    NULL,
  content_hash   CHAR(64)    NULL          -- master tables only
) ENGINE=InnoDB;

CREATE TABLE api_post_log (
  seq            BIGINT AUTO_INCREMENT PRIMARY KEY,
  occurred_at    DATETIME(3)  NOT NULL,
  method         VARCHAR(8)   NOT NULL,
  endpoint       VARCHAR(255) NOT NULL,
  operation      VARCHAR(64)  NULL,
  source_table   VARCHAR(64)  NULL,
  source_row_pk  BIGINT       NULL,
  request_json   LONGTEXT     NOT NULL,
  status         VARCHAR(16)  NOT NULL,    -- pending / ok / http_error / network_error
  http_status    INT          NULL,
  response_json  LONGTEXT     NULL,
  error_message  TEXT         NULL,
  duration_ms    INT          NULL,
  attempt        INT          NOT NULL DEFAULT 1,
  INDEX ix_api_post_log_occurred (occurred_at),
  INDEX ix_api_post_log_source   (source_table, source_row_pk)
) ENGINE=InnoDB;
```

`sync_outbox` deliberately has no foreign keys and no secondary indexes —
nothing that can lock or fail expensively inside a trigger.

## Trigger design

30 triggers: 10 operational tables × insert / update / delete. All generated
from one template.

```sql
CREATE TRIGGER trg_transactiondata_ai AFTER INSERT ON transactiondata
FOR EACH ROW
BEGIN
  DECLARE CONTINUE HANDLER FOR SQLEXCEPTION BEGIN END;   -- never break the app's write
  INSERT INTO sync_outbox (table_name, row_pk, op)
  VALUES ('transactiondata', NEW.Id, 'I');
END;
```

The `CONTINUE HANDLER` is the safety valve and the reason approach A is
acceptable here. A trigger runs inside the application's write transaction, so
normally a broken trigger breaks the app. With the handler, if `sync_outbox` is
missing, full, renamed or deadlocked, the trigger fails silently and the
weighing still saves.

What is lost in that moment is a *notification*. Recovering exactly that is what
reconciliation is for — which is why the hybrid is more robust than triggers
alone rather than merely more work.

Installation is idempotent at startup: read `information_schema.TRIGGERS`, create
what is missing, and log the result. Never drop and recreate under load.

## API audit capture

Today the outgoing payload goes to `Debug.WriteLine`, which the compiler strips
from Release builds. Production keeps no record of what was sent.

Every teacoop.lk call funnels through a single `HttpClient` inside `ApiClient`,
so a `DelegatingHandler` captures all of them without touching any call site.

```csharp
public ApiClient()
{
    _httpClient = new HttpClient(new ApiAuditHandler(new HttpClientHandler()));
}
```

That constructor line is the only change to any existing file.

Behaviour:

- Writes `status='pending'` before the call, updates with the outcome after. A
  row left at `pending` is direct evidence of a hang — the failure mode fixed in
  `77b9714`, now visible instead of invisible.
- Records the exact bytes on the wire, after every transformation. This makes
  "was `nomal_leaf_weight` netted on this delivery?" a query rather than an
  inference.
- `source_table` + `source_row_pk` link each call to the row it was about.
  Populated via `HttpRequestMessage.Options` at call sites that know it
  (greenleaf first); inferred from the URL otherwise.
- Truncates `response_json` at 64 KB.

Three rules the handler obeys:

1. **Never in the hot path.** It pushes onto a bounded in-memory queue; a
   background writer performs the database work. The HTTP call never awaits a
   database write.
2. **Never throws.** The whole capture is wrapped. If the queue is full, the
   table is missing, or MySQL is down, the call proceeds exactly as today. Audit
   failure degrades to "no record", never to "call failed".
3. **Never stores credentials.** The `Authorization` header is stripped before
   writing, so the API token is not copied into the cloud database.

Volume is roughly 500 calls/day at ~2 KB, about 1 MB/day/branch. Local retains
90 days; cloud retains everything. Pruning is a nightly delete by `occurred_at`.

## Cloud schema

Every mirrored table gains three columns:

- `branch_id VARCHAR(32) NOT NULL` — from `config.json`
- `synced_at DATETIME(3) NOT NULL`
- `deleted_at DATETIME(3) NULL`

Primary key becomes `(branch_id, local_id)`.

| Decision | Choice | Reason |
|---|---|---|
| Write mode | `INSERT … ON DUPLICATE KEY UPDATE` | Idempotent; retry after a dropped connection is always safe |
| Deletes | Soft — set `deleted_at` | A mirror must not lose history; a wrongly-detected delete stays recoverable |
| DDL | Applied once by script | The app never runs DDL on the cloud |
| Schema mismatch | Refuse to sync, log loudly | Better stale than corrupt |
| DB user grants | `SELECT, INSERT, UPDATE` on the schema, plus `DELETE` on the three master tables only | No `DROP`; a compromised branch cannot destroy the mirror |
| Transport | `SslMode=Required`, server enforces `require_secure_transport=ON` | Credentials and farmer data never cross the internet in clear |
| Table-name case | Server initialised with `lower_case_table_names=1` | Branches run MySQL on Windows, which folds table names to lower case. A case-sensitive Linux server would fail to match them |

### Correction to the original grant plan

The design originally said "no `DELETE`". That is wrong for the three master
tables. They are replaced wholesale on every member or user refresh, and their
identity keys change each time, so soft-deleting instead of removing would
accumulate dead rows without bound — tens of thousands per refresh.

`DELETE` is therefore granted on `usersdata`, `membersdata` and `linemasterdata`
and nowhere else. MySQL requires a table to exist before a table-level grant can
be issued, so these three grants are applied **after** the mirror schema is
created, not at user-creation time.

Plus one status table, written by every branch each cycle:

```sql
CREATE TABLE sync_branch_status (
  branch_id       VARCHAR(32) PRIMARY KEY,
  branch_name     VARCHAR(128),
  app_version     VARCHAR(32),
  last_contact_at DATETIME(3),
  last_success_at DATETIME(3),
  pending_count   INT,
  last_error      TEXT
) ENGINE=InnoDB;
```

This is what makes stragglers visible without visiting a branch.

## Sync worker

A background task started once the app's database is ready. Wakes every 30–60
seconds. There is no "are we online?" probe — attempting and failing cheaply is
the check.

1. **Claim.** `GET_LOCK('wm_sync', 0)`. Both stations run the app against the
   same database, so without this they would drain the same outbox twice.
   Whoever takes the lock syncs; the other skips the cycle.
2. **Read.** Up to 500 outbox entries in `seq` order, grouped by table.
3. **Resolve.** Fetch the *current* state of each row, not the historical change.
   Ten edits to one row collapse into one upsert — self-collapsing and naturally
   idempotent.
4. **Push.** One transaction per batch. Success commits; any failure rolls back
   and the outbox entries remain for the next cycle.
5. **Clear.** Delete those outbox rows, advance `sync_state`, release the lock.

On failure: exponential backoff capped at five minutes. All errors go to the
existing `log.json` via `Logger`. No modal dialogs ever reach an operator.

### Backfill

The outbox only knows about changes made after the triggers exist. Existing
history needs a one-time upload: a throttled, resumable pass in ascending ID
order, recording position in `sync_state.backfill_pos` so a closed app or a
dropped line resumes rather than restarts. Rate-limited so it cannot saturate a
rural connection or compete with live weighing.

## Reconciliation

Triggers fail silently by design and did not exist before installation. The
differ is what guarantees convergence. Three tiers, cheapest first, all
read-only against existing tables.

| Tier | Frequency | Mechanism | Catches |
|---|---|---|---|
| 1 | Every cycle | `WHERE Id > watermark_id` | Inserts a trigger missed |
| 2 | Every cycle | Re-hash the newest 5,000 rows by primary key | Edits to live data |
| 3 | Nightly | Block hashes over the full table | Late edits deep in history; deletions |

Tier 2 defines "recent" by **primary key descending, not by date**. Only some
tables carry a date column, and where one exists it is a `VARCHAR` rather than a
`DATE` (for example `transactionData.date`, `GreenLeafPosts.leaf_handover_date`).
Ordering by the auto-increment key works identically for every operational
table, needs no schema knowledge, and uses the primary key index.

Tier 3 hashes rows in blocks of 1,000 primary keys, compares block hashes with
the cloud, and drills into only the blocks that disagree. On 63,000 rows that
compares 63 block hashes rather than reading 63,000 rows.

### Hash definition

Both sides must compute an identical value, so the hash is defined once and
pinned:

- Over the **mirrored business columns only** — never `branch_id`, `synced_at`
  or `deleted_at`, which exist solely on the cloud side.
- Column order is the local table's ordinal order, captured at build time so it
  cannot drift.
- `MD5(CONCAT_WS('\x1f', <cols>))` per row, with `NULL` normalised to a sentinel
  so `NULL` and empty string do not collide.
- Block hash is `MD5(GROUP_CONCAT(row_hash ORDER BY local_id))`.

Both sides run MySQL, so the same expression evaluates identically on each.

A row present in the cloud but absent locally is marked `deleted_at`, never
removed.

## Configuration

`config.json` gains:

```json
{
  "sync_enabled": false,
  "sync_interval_seconds": 45,
  "sync_batch_size": 500,
  "api_log_retention_days": 90
}
```

Cloud credentials live in a new `cloudsync.json` beside `dbconfig.txt`, excluded
from git, following the existing configuration pattern.

**`sync_enabled` ships `false`.** It disables the worker, the handler and the
trigger installer without a redeploy. One config edit and a restart returns any
branch to exactly today's behaviour.

## Failure modes

| Condition | Effect on the app | Recovery |
|---|---|---|
| Trigger fails | None — write commits | Reconciliation picks the row up |
| Outbox table missing | None | Trigger no-ops; tier 1/3 recover |
| Cloud unreachable | None | Backoff; outbox drains on restore |
| Cloud schema mismatch | None | Worker refuses to sync, logs loudly |
| Audit queue full | None | Oldest entries dropped; call unaffected |
| Both stations sync at once | None | Advisory lock serialises them |
| App killed mid-batch | None | Batch was transactional; outbox intact |

## Testing

Each of these runs before any branch goes live.

| Test | Method | Pass condition |
|---|---|---|
| Trigger cannot block a save | Drop `sync_outbox`, save weighings at both stations | Every save succeeds |
| Audit cannot block an API call | Point writer at an unreachable DB, post green leaf | POST behaves as today |
| Write latency unchanged | Time 1,000 inserts before and after | No material regression |
| Cloud outage harmless | Firewall the cloud host for an hour of live use | App unaffected; drains on restore |
| Mirror faithful | Row counts and block hashes, local vs cloud | Exact match after sweep |
| Concurrent stations | Both PCs against one database | No duplicate work |
| Reports unchanged | Daily and line reports before and after | Byte-identical output |
| Backfill resumable | Kill app mid-backfill, restart | Resumes at `backfill_pos` |

## Rollout

1. **Dark.** Release with `sync_enabled: false` everywhere. Proves the build is
   harmless before it does anything.
2. **Pilot.** Enable at one branch. Run the backfill. Compare row counts and
   block hashes daily for a week.
3. **Expand.** A few branches at a time, watching `sync_branch_status`.
4. **Steady state.** All branches, with a dashboard over `sync_branch_status`.

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Trigger overhead on high-volume inserts | Medium | Outbox has no indexes beyond its PK; measured before rollout |
| Backfill saturates a slow line | Medium | Rate-limited, resumable, pausable |
| Cloud disk growth from `api_post_log` | Low | ~1 MB/day/branch; responses truncated |
| Schema drift between app and cloud | Medium | Worker verifies on startup and refuses to sync |
| Branch on an old build never syncs | Low | Visible immediately in `sync_branch_status` |
| Clock skew between branches | Low | Ordering is by local `seq`, never wall-clock |

## Files

**New:**

- `Services/Sync/SyncWorker.cs` — the loop
- `Services/Sync/OutboxDrainer.cs` — batch, resolve, upsert
- `Services/Sync/Reconciler.cs` — the three tiers
- `Services/Sync/MasterSnapshotSync.cs` — hash-and-replace for master tables
- `Services/Sync/TriggerInstaller.cs` — idempotent DDL install and verify
- `Services/Sync/CloudConnection.cs` — connection, TLS, schema verification
- `Services/Api/ApiAuditHandler.cs` — the `DelegatingHandler`
- `Services/Api/ApiAuditWriter.cs` — the background queue writer
- `Models/SyncOutboxEntry.cs`, `Models/SyncState.cs`, `Models/ApiPostLog.cs`
- `sql/cloud-schema.sql` — one-time cloud DDL

**Changed:**

- `Config/ApiClient.cs` — one constructor line
- `Config/dbConfig.cs` — three new `DbSet`s
- `Config/AppConfig.cs` — new settings
- app startup — start the worker when enabled

No existing model, service, query, report or UI path is modified.

## Required before implementation

1. Cloud MySQL host, and confirmation it is a fresh empty schema.
2. Rotation of the API token committed in plaintext at `Config/ApiClient.cs`
   lines 52 and 89. The repository is public and the token is in git history, so
   removing the line is not sufficient. Independent of this work, and more urgent
   than it.
