using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
/// </remarks>
public static class AuthorizationRoleCache
{
    private static readonly TimeSpan s_ttl = TimeSpan.FromSeconds(5);

    private static readonly ConcurrentDictionary<string, CacheEntry<List<mmria.common.model.couchdb.user_role_jurisdiction>>> s_perUserCache
        = new ConcurrentDictionary<string, CacheEntry<List<mmria.common.model.couchdb.user_role_jurisdiction>>>(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<string, CacheEntry<HashSet<(string jurisdiction_id, string user_id, string role_name)>>> s_perTenantCache
        = new ConcurrentDictionary<string, CacheEntry<HashSet<(string jurisdiction_id, string user_id, string role_name)>>>(StringComparer.Ordinal);

    /// <summary>
    /// Returns the cached active role/jurisdiction list for (prefix, userName)
    /// or invokes <paramref name="loader"/> to populate it.
    /// </summary>
    public static List<mmria.common.model.couchdb.user_role_jurisdiction> GetOrLoadActiveUserRoles(
        string prefix,
        string userName,
        Func<List<mmria.common.model.couchdb.user_role_jurisdiction>> loader)
    {
        if (loader == null) throw new ArgumentNullException(nameof(loader));

        var key = $"{prefix ?? string.Empty}|{userName ?? string.Empty}";
        var now = DateTime.UtcNow;

        if (s_perUserCache.TryGetValue(key, out var entry) && entry.ExpiresUtc > now)
        {
            return entry.Value;
        }

        var loaded = loader();
        // Do not cache nulls; let the next call retry.
        if (loaded != null)
        {
            s_perUserCache[key] = new CacheEntry<List<mmria.common.model.couchdb.user_role_jurisdiction>>(loaded, now + s_ttl);
        }
        return loaded;
    }

    /// <summary>
    /// Returns the cached whole-tenant user/role/jurisdiction set or invokes
    /// <paramref name="loader"/> to populate it.
    /// </summary>
    public static HashSet<(string jurisdiction_id, string user_id, string role_name)> GetOrLoadTenantUserRoles(
        string prefix,
        Func<HashSet<(string jurisdiction_id, string user_id, string role_name)>> loader)
    {
        if (loader == null) throw new ArgumentNullException(nameof(loader));

        var key = prefix ?? string.Empty;
        var now = DateTime.UtcNow;

        if (s_perTenantCache.TryGetValue(key, out var entry) && entry.ExpiresUtc > now)
        {
            return entry.Value;
        }

        var loaded = loader();
        if (loaded != null)
        {
            s_perTenantCache[key] = new CacheEntry<HashSet<(string jurisdiction_id, string user_id, string role_name)>>(loaded, now + s_ttl);
        }
        return loaded;
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
