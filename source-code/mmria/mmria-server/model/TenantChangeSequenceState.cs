using System;
using System.Collections.Generic;

namespace mmria.server.model;

/// <summary>
/// Per-tenant change-sequence tracking state used by the Couch _changes-feed
/// reconciliation actors (Process_DB_Synchronization_Set, Synchronize_Deleted_Case_Records).
///
/// Replaces the previous globally-shared <c>Program.Last_Change_Sequence</c> /
/// <c>Program.Change_Sequence_Call_Count</c> / <c>Program.DateOfLastChange_Sequence_Call</c>
/// statics, which were incorrect in multi-tenant mode (one tenant's last_seq overwrote
/// every other tenant's, causing unnecessary full re-syncs and reading the wrong tenant's
/// _changes feed). State is now keyed by tenant via <see cref="KeyFor"/>.
/// </summary>
public sealed class TenantChangeSequenceState
{
    private const int RecentCallTimesRetention = 10;

    private readonly object _gate = new();
    private readonly List<DateTime> _recentCallTimes = new();

    /// <summary>The last <c>last_seq</c> observed from this tenant's _changes feed.</summary>
    public string LastChangeSequence { get; set; }

    /// <summary>Monotonic count of change-sequence polls for this tenant (saturates at int.MaxValue).</summary>
    public int CallCount { get; private set; }

    /// <summary>
    /// Records a poll for diagnostics. Trims the in-memory call-time list so it
    /// cannot grow unbounded.
    /// </summary>
    public void RecordCall()
    {
        lock (_gate)
        {
            if (CallCount < int.MaxValue)
            {
                CallCount++;
            }

            if (_recentCallTimes.Count >= RecentCallTimesRetention)
            {
                _recentCallTimes.Clear();
            }

            _recentCallTimes.Add(DateTime.Now);
        }
    }

    /// <summary>
    /// Stable per-tenant key derived from the tenant's CouchDB connection details.
    /// Combines the URL and DB prefix so single-tenant and multi-tenant deployments
    /// both produce a unique key per tenant database.
    /// </summary>
    public static string KeyFor(mmria.common.couchdb.DBConfigurationDetail db_config)
    {
        if (db_config == null)
        {
            return string.Empty;
        }

        return $"{db_config.url}|{db_config.prefix}";
    }
}
