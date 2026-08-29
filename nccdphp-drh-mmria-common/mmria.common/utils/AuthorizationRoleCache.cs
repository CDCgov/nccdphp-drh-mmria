using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using mmria.common.SharedLibraries.Jurisdiction;
using mmria.common.SharedLibraries.Jurisdiction.Model;

namespace mmria.common.utils;

/// <summary>
/// Short-TTL in-process cache for per-tenant user-role-jurisdiction lookups.
/// Used by authorization helpers that are called on every request to avoid
/// hitting CouchDB's <c>jurisdiction/_design/sortable/_view/by_user_id</c>
/// (and the sync-over-async wait that goes with it) on every authorize check.
/// </summary>
/// <remarks>
/// <para>
/// TTL is intentionally short (5 seconds) so that role/jurisdiction edits
/// made through the UI take effect within one cache window. Cache entries are
/// keyed by <c>(db_config.prefix, userName)</c> for the per-user variant and
/// by <c>db_config.prefix</c> for the whole-tenant variant.
/// </para>
/// <para>
/// Storage is a <see cref="ConcurrentDictionary{TKey, TValue}"/> with lazy
/// expiry on read — no background eviction thread. A single stale entry is
/// at most O(role-count-per-user), which is small.
/// </para>
/// <para>
/// Active-role filtering for the per-user cache is performed here, keeping
/// that logic centralised rather than duplicated across authorization helper files.
/// </para>
/// </remarks>
public static class AuthorizationRoleCache
{
    private static readonly TimeSpan s_ttl = TimeSpan.FromSeconds(5);

    private static readonly ConcurrentDictionary<string, CacheEntry<IReadOnlyList<JurisdictionRoleEntry>>> s_perUserCache
        = new ConcurrentDictionary<string, CacheEntry<IReadOnlyList<JurisdictionRoleEntry>>>(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<string, CacheEntry<HashSet<(string jurisdiction_id, string user_id, string role_name)>>> s_perTenantCache
        = new ConcurrentDictionary<string, CacheEntry<HashSet<(string jurisdiction_id, string user_id, string role_name)>>>(StringComparer.Ordinal);

    /// <summary>
    /// Returns the cached active role/jurisdiction list for (prefix, userName)
    /// or uses <paramref name="reader"/> to populate it on a cache miss.
    /// Active-role filtering, user-id match guard, and null role_name guard are applied on a miss.
    /// </summary>
    public static IReadOnlyList<JurisdictionRoleEntry> GetOrLoadActiveUserRoles(
        string? prefix,
        string? userName,
        IJurisdictionAuthorizationReader reader,
        mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        var key = $"{prefix ?? string.Empty}|{userName ?? string.Empty}";
        var now = DateTime.UtcNow;

        if (s_perUserCache.TryGetValue(key, out var entry) && entry.ExpiresUtc > now)
        {
            return entry.Value;
        }

        var rawRoles = reader.GetRolesByUserIdAsync(userName, dbConfig).GetAwaiter().GetResult();
        var activeRoles = FilterActiveRoles(rawRoles, userName, now);

        s_perUserCache[key] = new CacheEntry<IReadOnlyList<JurisdictionRoleEntry>>(activeRoles, now + s_ttl);
        return activeRoles;
    }

    /// <summary>
    /// Returns the cached whole-tenant user/role/jurisdiction set or uses
    /// <paramref name="reader"/> to populate it on a cache miss.
    /// No active-role filtering is applied — all role documents are included.
    /// </summary>
    public static HashSet<(string jurisdiction_id, string user_id, string role_name)> GetOrLoadTenantUserRoles(
        string? prefix,
        IJurisdictionAuthorizationReader reader,
        mmria.common.couchdb.DBConfigurationDetail dbConfig)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        var key = prefix ?? string.Empty;
        var now = DateTime.UtcNow;

        if (s_perTenantCache.TryGetValue(key, out var entry) && entry.ExpiresUtc > now)
        {
            return entry.Value;
        }

        var rawRoles = reader.GetRolesByUserIdAsync(null, dbConfig).GetAwaiter().GetResult();

        var result = new HashSet<(string jurisdiction_id, string user_id, string role_name)>();
        foreach (var r in rawRoles)
        {
            if (r?.user_id != null)
            {
                result.Add((r.jurisdiction_id!, r.user_id!, r.role_name!));
            }
        }

        s_perTenantCache[key] = new CacheEntry<HashSet<(string jurisdiction_id, string user_id, string role_name)>>(result, now + s_ttl);
        return result;
    }

    // ── Active-role filtering ─────────────────────────────────────────────────

    private static IReadOnlyList<JurisdictionRoleEntry> FilterActiveRoles(
        IReadOnlyList<JurisdictionRoleEntry> rawRoles,
        string? userName,
        DateTime now)
    {
        var result = new List<JurisdictionRoleEntry>();
        foreach (var e in rawRoles)
        {
            if (e == null)
                continue;

            // Safety: the by_user_id view should return only rows for the requested user
            // when a key filter is applied, but guard against unexpected data.
            if (userName != null && e.user_id != userName)
                continue;

            if (!IsActiveEntry(e, now))
                continue;

            // Guard against jurisdiction documents with a missing role_name — such rows
            // would later be passed to new Claim(ClaimTypes.Role, role, ...) which throws
            // ArgumentNullException and 500s the entire sign-in flow.
            if (string.IsNullOrWhiteSpace(e.role_name))
            {
                System.Console.WriteLine(
                    $"Skipping jurisdiction role with null/empty role_name. " +
                    $"user={userName}; jurisdiction_id={e.jurisdiction_id}; doc_id={e._id}");
                continue;
            }

            result.Add(e);
        }
        return result;
    }

    private static bool IsActiveEntry(JurisdictionRoleEntry value, DateTime nowDate)
    {
        if (value == null ||
            value.is_active == null ||
            value.effective_start_date == null ||
            !value.is_active.HasValue ||
            !value.effective_start_date.HasValue)
        {
            return false;
        }

        var effectiveEndDate = value.effective_end_date.HasValue
            ? value.effective_end_date.Value
            : nowDate;

        return value.is_active.Value &&
            value.effective_start_date.Value <= nowDate &&
            nowDate <= effectiveEndDate;
    }

    private readonly struct CacheEntry<T>
    {
        public CacheEntry(T value, DateTime expiresUtc)
        {
            Value = value;
            ExpiresUtc = expiresUtc;
        }

        public T Value { get; }
        public DateTime ExpiresUtc { get; }
    }
}
