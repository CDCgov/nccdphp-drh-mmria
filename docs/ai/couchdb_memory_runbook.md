# CouchDB Pod Memory Runbook

> **Audience:** OpenShift / infra operators of the mmria multi-tenant deployment.
> **Scope:** CouchDB pods only (`couchdb-tenant*`, `couchdb-cdc*`, `couchdb-test*`).
> **Goal:** Reduce steady-state working set, eliminate the multi-day "slow doubling" RSS pattern observed on 2-week graphs.
> **No mmria application code changes are required.** Everything in this runbook is CouchDB-side configuration.

---

## 0. Before you start

1. **Take a baseline.** Capture per-pod RSS for at least 24 h before changes, ideally 7 days. Record:
   - `container_memory_working_set_bytes` (the metric in your Prometheus screenshot).
   - `couchdb_open_databases` and `couchdb_database_reads` from `_node/_local/_stats`.
   - On-disk size of `view_index_dir` and `database_dir` per tenant (`du -sh /opt/couchdb/data/shards`).
2. **Roll out to one tenant pod first.** Do not change all 72 pods at once. Pick a low-traffic tenant (e.g. a `*qa` pod), apply the change, watch RSS for 24 h, then promote.
3. **Have a rollback plan.** Each section ends with explicit revert steps.

---

## 1. Compaction & view cleanup (highest impact)

### 1.1 What this fixes

Without compaction, every document update appends a new revision to the `.couch` file and never removes the old one. View design-doc changes leave orphaned `.view` index files. Both stay mmap'd and inflate `container_memory_working_set_bytes` because the kernel keeps recently-touched pages of those files in the page cache.

Expected gain on a 100-case tenant pod after first compaction: **30–60% RSS reduction, sustained.**

### 1.2 Implementation — `local.ini` ConfigMap

Add a `[compactions]` section to each CouchDB pod's `local.ini`. In OpenShift this is typically a `ConfigMap` mounted at `/opt/couchdb/etc/local.d/`.

**File:** `couchdb-local.ini` (mounted at `/opt/couchdb/etc/local.d/10-mmria-compaction.ini`)

```ini
[compactions]
; Trigger automatic compaction when:
;   db_fragmentation > 70%   AND   live data > 100 MB
;   view_fragmentation > 60% AND   live view data > 50 MB
; Only between 23:00 and 04:00 UTC (low-traffic window).
; Stagger pods via the `from`/`to` window per tenant — see section 1.4.
_default = [{db_fragmentation, "70%"}, {view_fragmentation, "60%"}, {from, "23:00"}, {to, "04:00"}, {strict_window, true}]

[couchdb]
; Optional: enable snappy compression on new writes. Reduces on-disk size,
; reduces page-cache pressure. Safe to enable; existing data is rewritten on
; next compaction.
file_compression = snappy

[smoosh]
; CouchDB's automatic compactor daemon. The `_default` channel above feeds it.
; These bounds prevent runaway parallel compactions on a busy pod.
db_channels = upgrade_dbs,ratio_dbs
view_channels = upgrade_views,ratio_views

[smoosh.ratio_dbs]
priority = ratio
min_priority = 2.0
max_jobs = 1

[smoosh.ratio_views]
priority = ratio
min_priority = 2.0
max_jobs = 1
```

### 1.3 Apply to a single pod (canary)

The goal is to drop **one extra `.ini` file** into CouchDB's `local.d/` directory — *without* disturbing any existing files there. CouchDB reads every `*.ini` in `local.d/` in **alphabetical order** at startup; later files override earlier ones. We name ours `10-mmria-compaction.ini` so it loads after the chart-provided defaults but before any operator overrides typically named `90-*.ini` or `99-*.ini`.

#### Step 1 — Pick a canary pod

```bash
# Pick a low-traffic pod. *qa pods are ideal.
oc get pods -n mmria -l app=couchdb | grep tenant1qa
# NAME                            READY   STATUS    RESTARTS   AGE
# couchdb-tenant1qa-0             1/1     Running   0          14d
```

Note the **StatefulSet name** (almost always the pod name minus the `-0` suffix):

```bash
oc get statefulset -n mmria | grep couchdb-tenant1qa
# couchdb-tenant1qa   1/1   14d
```

#### Step 2 — Inspect what's already in `local.d/`

This is critical. If you mount a ConfigMap with `mountPath: /opt/couchdb/etc/local.d` you will **shadow every existing file in that directory**, breaking auth, clustering, and anything else the chart configured. We must mount as a single file via `subPath`.

```bash
oc rsh couchdb-tenant1qa-0 ls -la /opt/couchdb/etc/local.d/
# Typical output:
# -rw-r--r-- 1 couchdb couchdb  234 Apr  9 14:22 10-couchdb.ini
# -rw-r--r-- 1 couchdb couchdb  189 Apr  9 14:22 20-clustering.ini
# (your chart may have different names — record them)
```

Check whether `local.d/` is already a mounted ConfigMap:

```bash
oc get statefulset couchdb-tenant1qa -n mmria \
  -o jsonpath='{.spec.template.spec.volumes[*].name}{"\n"}'
oc get statefulset couchdb-tenant1qa -n mmria \
  -o jsonpath='{.spec.template.spec.containers[0].volumeMounts[*].mountPath}{"\n"}'
```

**If `/opt/couchdb/etc/local.d` is already a mounted ConfigMap** (common for Helm-deployed CouchDB), skip to **Step 3b**. Otherwise use **Step 3a**.

#### Step 3a — Local.d is a plain directory: add ours via `subPath` mount

```bash
# Save the .ini content from section 1.2 to a local file first:
cat > /tmp/10-mmria-compaction.ini <<'EOF'
[compactions]
_default = [{db_fragmentation, "70%"}, {view_fragmentation, "60%"}, {from, "23:00"}, {to, "04:00"}, {strict_window, true}]

[couchdb]
file_compression = snappy

[smoosh]
db_channels = upgrade_dbs,ratio_dbs
view_channels = upgrade_views,ratio_views

[smoosh.ratio_dbs]
priority = ratio
min_priority = 2.0
max_jobs = 1

[smoosh.ratio_views]
priority = ratio
min_priority = 2.0
max_jobs = 1
EOF

# Create the ConfigMap. The key name inside the map IS the filename
# that will appear in /opt/couchdb/etc/local.d/.
oc create configmap mmria-couchdb-compaction \
  --from-file=10-mmria-compaction.ini=/tmp/10-mmria-compaction.ini \
  -n mmria

# Patch the StatefulSet — note the use of subPath so we add ONE file
# instead of replacing the whole directory.
oc patch statefulset couchdb-tenant1qa -n mmria --type='json' -p='[
  {
    "op": "add",
    "path": "/spec/template/spec/volumes/-",
    "value": {
      "name": "mmria-compaction",
      "configMap": {"name": "mmria-couchdb-compaction"}
    }
  },
  {
    "op": "add",
    "path": "/spec/template/spec/containers/0/volumeMounts/-",
    "value": {
      "name": "mmria-compaction",
      "mountPath": "/opt/couchdb/etc/local.d/10-mmria-compaction.ini",
      "subPath": "10-mmria-compaction.ini",
      "readOnly": true
    }
  }
]'
```

#### Step 3b — Local.d is already a mounted ConfigMap: append to that ConfigMap

If your chart already mounts `local.d` as a ConfigMap (e.g. `couchdb-config`), add a key to the existing ConfigMap rather than mounting a second one — they would conflict on the same path otherwise.

```bash
# 1. Find the ConfigMap name your chart uses
oc get statefulset couchdb-tenant1qa -n mmria \
  -o jsonpath='{.spec.template.spec.volumes}' | jq

# 2. Patch it — replace `couchdb-config` with the actual name
oc patch configmap couchdb-config -n mmria --type='merge' -p='
data:
  10-mmria-compaction.ini: |
    [compactions]
    _default = [{db_fragmentation, "70%"}, {view_fragmentation, "60%"}, {from, "23:00"}, {to, "04:00"}, {strict_window, true}]

    [couchdb]
    file_compression = snappy

    [smoosh]
    db_channels = upgrade_dbs,ratio_dbs
    view_channels = upgrade_views,ratio_views

    [smoosh.ratio_dbs]
    priority = ratio
    min_priority = 2.0
    max_jobs = 1

    [smoosh.ratio_views]
    priority = ratio
    min_priority = 2.0
    max_jobs = 1
'
```

> **Helm chart users:** the cleanest variant is to add this `.ini` content to your chart values (e.g. `couchdb.couchdbConfig.compactions._default = "..."` if the chart supports it, or via a custom `local.d` values key) and `helm upgrade`. The patch approach above is for emergency / out-of-band canary testing. Do not leave manual patches in place long-term — they will be reverted on the next chart upgrade.

#### Step 4 — Restart the pod

```bash
oc rollout restart statefulset/couchdb-tenant1qa -n mmria
oc rollout status statefulset/couchdb-tenant1qa -n mmria --timeout=120s
```

Wait for `rollout status` to confirm `Running`. If the pod fails to start, **roll back immediately** (Section 1.7) — almost always indicates a typo in the `.ini` content (CouchDB refuses to start on malformed config).

#### Step 5 — Verify the file landed and CouchDB parsed it

```bash
# (a) File is present alongside the existing .ini files
oc rsh couchdb-tenant1qa-0 ls -la /opt/couchdb/etc/local.d/
# Expect 10-mmria-compaction.ini in the listing.

# (b) Content matches what you intended
oc rsh couchdb-tenant1qa-0 cat /opt/couchdb/etc/local.d/10-mmria-compaction.ini

# (c) CouchDB is reporting the new compaction config in its live config endpoint.
#     Get admin creds from your secret first:
ADMIN_PW=$(oc get secret couchdb-admin -n mmria -o jsonpath='{.data.password}' | base64 -d)

oc rsh couchdb-tenant1qa-0 \
  curl -sf "http://admin:${ADMIN_PW}@localhost:5984/_node/_local/_config/compactions"
# Expect a JSON object containing your `_default` rule:
# {"_default":"[{db_fragmentation, \"70%\"}, {view_fragmentation, \"60%\"}, ..."}

# (d) Smoosh daemon picked up the channel config
oc rsh couchdb-tenant1qa-0 \
  curl -sf "http://admin:${ADMIN_PW}@localhost:5984/_node/_local/_config/smoosh"
# Expect db_channels/view_channels listing your ratio_* entries.
```

If any of (a)–(d) fail, see Section 1.7 troubleshooting.

### 1.4 Stagger across all 72 tenants

If all pods compact at exactly 23:00, you create a synchronised CPU/IO storm. Stagger by tenant prefix.

**Option A — fixed offset per tenant (simple).** In each tenant's ConfigMap, replace `from`/`to` with a tenant-specific window:

| Tenant index (0-71) | Window |
|---|---|
| 0–11 | 23:00–00:00 |
| 12–23 | 00:00–01:00 |
| 24–35 | 01:00–02:00 |
| 36–47 | 02:00–03:00 |
| 48–59 | 03:00–04:00 |
| 60–71 | 04:00–05:00 |

**Option B — single shared window, rely on `smoosh.max_jobs = 1`.** Simpler config; CouchDB itself queues compactions one at a time per pod. Since each pod is single-tenant in your topology, this is acceptable as long as `max_jobs` stays low.

For your per-tenant pod topology, **Option B is sufficient.** Each pod only compacts its own ~10 DBs serially.

### 1.5 View cleanup — separate, scheduled

`_view_cleanup` is **not** automatic. It must be triggered explicitly per database, after any design-doc change. The mmria startup rebuild can leave orphaned view index files behind.

Add a Kubernetes `CronJob` per CouchDB pod (or one shared `CronJob` that iterates):

**File:** `couchdb-view-cleanup-cronjob.yaml`

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: couchdb-view-cleanup
  namespace: mmria
spec:
  # Run weekly Sundays at 05:00 UTC, after the nightly compaction window.
  schedule: "0 5 * * 0"
  concurrencyPolicy: Forbid
  successfulJobsHistoryLimit: 1
  failedJobsHistoryLimit: 3
  jobTemplate:
    spec:
      template:
        spec:
          restartPolicy: OnFailure
          containers:
            - name: cleanup
              image: curlimages/curl:8.7.1
              env:
                - name: COUCHDB_USER
                  valueFrom:
                    secretKeyRef:
                      name: couchdb-admin
                      key: username
                - name: COUCHDB_PASSWORD
                  valueFrom:
                    secretKeyRef:
                      name: couchdb-admin
                      key: password
              command:
                - sh
                - -c
                - |
                  set -e
                  # Iterate every CouchDB tenant pod via its service DNS.
                  # Adjust the loop to match your tenant naming convention.
                  for tenant in $(oc get pods -n mmria -l app=couchdb -o name | sed 's|pod/||'); do
                    echo "[view_cleanup] $tenant"
                    DBS=$(curl -sf "http://${COUCHDB_USER}:${COUCHDB_PASSWORD}@${tenant}:5984/_all_dbs" | tr -d '[]"' | tr ',' '\n')
                    for db in $DBS; do
                      case "$db" in
                        _users|_replicator|_global_changes) continue ;;
                      esac
                      curl -sf -X POST -H "Content-Type: application/json" \
                        "http://${COUCHDB_USER}:${COUCHDB_PASSWORD}@${tenant}:5984/${db}/_view_cleanup" || true
                    done
                  done
```

### 1.6 Verify

```bash
# Check on-disk size before vs. after first compaction window
oc rsh couchdb-tenant1qa-0 du -sh /opt/couchdb/data/shards/

# Check each DB's bloat ratio
oc rsh couchdb-tenant1qa-0 sh -c '
  for db in $(curl -s http://admin:$COUCHDB_PASSWORD@localhost:5984/_all_dbs | tr -d "[]\"" | tr "," "\n"); do
    curl -s "http://admin:$COUCHDB_PASSWORD@localhost:5984/$db" \
      | jq -r "\"\(.db_name)  data=\(.sizes.active)  file=\(.sizes.file)  ratio=\(.sizes.file / .sizes.active)\""
  done
'
# A healthy DB has ratio < 1.5. Anything > 3 is over-fragmented.
```

### 1.7 Rollback & troubleshooting

#### If you used Step 3a (separate ConfigMap, subPath mount)

```bash
# Remove the volume mount and the volume entry by name
oc patch statefulset couchdb-tenant1qa -n mmria --type='json' -p='[
  {"op": "remove", "path": "/spec/template/spec/containers/0/volumeMounts", "value": null}
]' 2>/dev/null

# Easier: use oc set volume to surgically remove just our addition
oc set volume statefulset/couchdb-tenant1qa --remove --name=mmria-compaction -n mmria
oc rollout restart statefulset/couchdb-tenant1qa -n mmria

# Optionally delete the now-unused ConfigMap
oc delete configmap mmria-couchdb-compaction -n mmria
```

#### If you used Step 3b (appended to existing ConfigMap)

```bash
# Remove just our key from the chart's ConfigMap
oc patch configmap couchdb-config -n mmria --type='json' -p='[
  {"op": "remove", "path": "/data/10-mmria-compaction.ini"}
]'
oc rollout restart statefulset/couchdb-tenant1qa -n mmria
```

#### Always

```bash
oc delete cronjob couchdb-view-cleanup -n mmria
```

#### Common failure modes

| Symptom | Cause | Fix |
|---|---|---|
| Pod crashloops with `bad config option` in logs | Typo in `.ini` (most often missing comma in the `_default` list) | Roll back, fix the `.ini`, retry. CouchDB refuses to start on parse errors. |
| `oc rsh ls /opt/couchdb/etc/local.d/` shows our file but `_config/compactions` returns `{}` | File mounted under wrong path or with wrong filename (must end in `.ini`) | Verify `mountPath` and `subPath` exactly match the filename. CouchDB only reads `*.ini`. |
| Pod restarted but our `.ini` is missing entirely | Used `mountPath: /opt/couchdb/etc/local.d` without `subPath` and shadowed the directory | Roll back to Step 3a with `subPath`, or move to Step 3b. |
| Compaction never triggers | Live data below `min_file_size` or in fragmentation `<` threshold | This is normal for low-write tenants. Manually trigger once: `curl -X POST http://admin:$ADMIN_PW@localhost:5984/<db>/_compact -H "Content-Type: application/json"` to confirm the path works. |
| Pod starts but `_config/compactions` 404s | Targeted the wrong node name in the URL | Always use `_node/_local/_config/...` from inside the pod. From outside use the actual node name from `_membership`. |

---

## 2. Erlang allocator tuning (`vm.args`)

### 2.1 What this fixes

CouchDB runs on the BEAM (Erlang VM). BEAM allocates memory through *carriers* — large slabs that hold many small term blocks. The default multi-block carrier strategy is `aobf` (address-order best-fit) which fragments badly under CouchDB's mixed-size workload (small JSON terms + large binary buffers from `_all_docs?include_docs=true` responses).

Symptom: `process_count` is flat, `memory.processes` is flat, but RSS climbs steadily over days. Carriers fill up partially, can't satisfy a new allocation, BEAM allocates a new carrier. Old ones stay reserved.

Expected gain: **20–40% lower steady-state RSS over 1–2 weeks; the slope flattens significantly.**

### 2.2 Implementation — `vm.args` ConfigMap

**File:** `couchdb-vm.args` (mounted at `/opt/couchdb/etc/vm.args`)

> **Important:** `vm.args` does not have an "include" mechanism like `local.d/`. You must replace the whole file. Read your existing `vm.args` first and merge.

```erlang
# Existing CouchDB vm.args content goes here — do NOT lose it.
# Typical defaults:
-name couchdb@127.0.0.1
-setcookie monster
-kernel error_logger silent
-sasl sasl_error_logger false
+K true
+A 16
+Bd -noinput

# ------------------------------------------------------------------
# mmria memory tuning — Apr 2026
# Switch multiblock carrier strategy from address-order best-fit (aobf)
# to best-fit (bf). Better packing for CouchDB's variable-size allocations,
# at a small per-allocation CPU cost (unmeasurable in practice).
+MEas bf

# Reduce binary-allocator largest multiblock carrier size from the default
# 5120 KB to 512 KB. Smaller carriers are returned to the OS sooner when
# emptied, reducing long-term fragmentation of the binary heap.
+MBlmbcs 512

# Optional: enable allocator instrumentation. Useful during the canary
# rollout to capture before/after numbers via `recon_alloc:fragmentation/1`.
# Comment out after validation — instrumentation adds ~1-2% CPU overhead.
# +Mim true
```

### 2.3 Apply to canary pod

```bash
# 1. Read the existing vm.args from the canary pod
oc rsh couchdb-tenant1qa-0 cat /opt/couchdb/etc/vm.args > current-vm.args

# 2. Merge the additions above into your local copy
#    Save as: couchdb-vm.args

# 3. Create ConfigMap (or update existing one if you already have one for vm.args)
oc create configmap couchdb-vm-args \
  --from-file=vm.args=./couchdb-vm.args \
  -n mmria \
  --dry-run=client -o yaml | oc apply -f -

# 4. Patch StatefulSet to mount it (full file replacement)
oc set volume statefulset/couchdb-tenant1qa \
  --add --name=mmria-vm-args \
  --type=configmap --configmap-name=couchdb-vm-args \
  --mount-path=/opt/couchdb/etc/vm.args \
  --sub-path=vm.args \
  -n mmria

# 5. Restart
oc rollout restart statefulset/couchdb-tenant1qa -n mmria

# 6. Verify the new flags are active
oc rsh couchdb-tenant1qa-0 ps auxf | grep beam.smp | grep -- '+MEas bf'
```

### 2.4 Verify the impact (give it 24–72 h)

After 24 h of normal traffic, capture allocator stats:

```bash
oc rsh couchdb-tenant1qa-0 sh -c '
  curl -s "http://admin:$COUCHDB_PASSWORD@localhost:5984/_node/_local/_system" \
    | jq "{
        memory: .memory,
        process_count: .process_count,
        run_queue: .run_queue
      }"
'
```

Compare `memory.binary` and `memory.system` values to your pre-change baseline. The binary allocator is what `+MBlmbcs` directly affects.

If RSS does **not** improve within 72 h, revert. Erlang allocator tuning is workload-dependent; these defaults work for most CouchDB deployments but not all.

### 2.5 Rollback

```bash
oc set volume statefulset/couchdb-tenant1qa --remove --name=mmria-vm-args -n mmria
oc rollout restart statefulset/couchdb-tenant1qa -n mmria
```

---

## 3. `max_dbs_open` — **skip for current per-tenant topology**

For your current deployment (one CouchDB pod per tenant, ~10 DBs per pod), the default `max_dbs_open = 500` is far above what each pod ever opens. Tuning this would have no measurable effect.

**Apply this section only if you consolidate to shared CouchDB pods** (multiple tenants per CouchDB instance). In that case:

```ini
[couchdb]
; Cap to roughly 2x your expected hot DB count. Excess DBs get LRU-evicted,
; freeing their cache pages and process memory.
max_dbs_open = 100
```

---

## 4. Rollout sequence (recommended)

| Day | Action |
|---|---|
| Day 0 | Capture 7-day baseline metrics. Pick canary pod (e.g. `couchdb-tenant1qa`). |
| Day 7 | Apply Section 1 (compaction + view cleanup) to canary only. |
| Day 9 | Inspect canary: on-disk size, RSS slope, fragmentation ratios. If green → continue. |
| Day 9 | Apply Section 2 (`vm.args` tuning) to same canary pod. |
| Day 12 | Inspect canary: RSS over 72 h. Look for flattened slope. |
| Day 12 | If both green: roll out Section 1 + 2 to one tenant per day for 72 days. **Do not big-bang.** |
| Day 12+72 | All tenants migrated. Continue baseline monitoring; expect RSS doubling pattern to be gone. |

---

## 5. Monitoring queries (Prometheus)

Add these to your dashboard alongside the existing `container_memory_working_set_bytes` panel:

```promql
# RSS slope per pod over last 7 days — should be near zero after rollout
deriv(container_memory_working_set_bytes{namespace="mmria", pod=~"couchdb-.*"}[7d])

# Fragmentation ratio — needs the couchdb_prometheus_exporter sidecar
couchdb_database_data_size_bytes / couchdb_database_disk_size_bytes

# Compaction activity
rate(couchdb_couchdb_database_writes_total[5m])
```

If `couchdb_prometheus_exporter` isn't deployed, scrape `_node/_local/_stats` and `_node/_local/_system` directly via a `ServiceMonitor` + `prometheus-couchdb-exporter` sidecar. That's a separate ticket but worth doing — without per-DB metrics you can only see container-level RSS.

---

## 6. Expected combined outcome

For a representative 100-case tenant CouchDB pod that today shows **200–300 MB RSS climbing to 400–600 MB over 2 weeks**:

| Phase | Fresh-start RSS | RSS at 2 weeks |
|---|---|---|
| Today (no changes) | ~200–300 MB | ~400–600 MB |
| After compaction (Section 1) | ~120–180 MB | ~250–400 MB (still climbing from binary frag) |
| After Section 1 + Section 2 | ~120–180 MB | ~150–220 MB (slope flat) |

Numbers are estimates based on typical CouchDB-on-Erlang behaviour. Your actual mileage will depend on per-tenant write rate and view query patterns. Treat the canary's 72 h numbers as the source of truth before promoting.

---

## 7. What this runbook does NOT address

- mmria-server pod memory growth — covered in [performance_risk_review.md](performance_risk_review.md).
- Replication checkpoint accumulation in `_replicator` — investigate separately if `_replicator` DB grows past ~50 MB.
- Cluster-level coordination if you ever move from per-tenant pods to a CouchDB cluster — different tuning model entirely.

---

## 8. References

- [Apache CouchDB 3 Configuration: `[compactions]`](https://docs.couchdb.org/en/stable/config/compaction.html)
- [Apache CouchDB 3 Configuration: `[smoosh]`](https://docs.couchdb.org/en/stable/maintenance/compaction.html#automatic-compaction)
- [Erlang Run-Time System Application (ERTS) `erl` flags — `+MEas`, `+MBlmbcs`](https://www.erlang.org/doc/man/erts_alloc.html)
- [recon_alloc — runtime allocator inspection](https://ferd.github.io/recon/recon_alloc.html)
