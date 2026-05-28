using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using mmria.common.SharedLibraries.MMRIARebuild.Model;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

public static class TenantRebuildCoordinator
{
    internal sealed class ReservationState
    {
        public Guid token { get; init; }
        public string tenant { get; init; }
        public string source { get; init; }
        public string mode { get; init; }
        public string requested_utc { get; init; }
        public string status { get; set; }
    }

    public sealed class TenantRebuildLease : IDisposable
    {
        private readonly ReservationState _state;
        private bool _disposed;

        internal TenantRebuildLease(ReservationState state)
        {
            _state = state;
        }

        public string Tenant => _state.tenant;

        public void UpdateStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return;
            }

            lock (_state)
            {
                _state.status = status.Trim();
            }
        }

        public TenantRebuildReservationSnapshot ToSnapshot()
        {
            lock (_state)
            {
                return CreateSnapshot(_state);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (
                _reservations.TryGetValue(_state.tenant, out var currentState) &&
                currentState.token == _state.token
            )
            {
                _reservations.TryRemove(_state.tenant, out _);
            }
        }
    }

    private static readonly ConcurrentDictionary<string, ReservationState> _reservations =
        new(StringComparer.OrdinalIgnoreCase);

    public static bool TryAcquire(
        string tenant,
        string source,
        string mode,
        string status,
        out TenantRebuildLease lease,
        out TenantRebuildReservationSnapshot existingReservation)
    {
        lease = null;
        existingReservation = null;

        string normalizedTenant = NormalizeTenant(tenant);
        if (string.IsNullOrWhiteSpace(normalizedTenant))
        {
            return false;
        }

        var reservation = new ReservationState
        {
            token = Guid.NewGuid(),
            tenant = normalizedTenant,
            source = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim(),
            mode = string.IsNullOrWhiteSpace(mode) ? "unknown" : mode.Trim(),
            status = string.IsNullOrWhiteSpace(status) ? "queued" : status.Trim(),
            requested_utc = DateTime.UtcNow.ToString("o")
        };

        if (_reservations.TryAdd(normalizedTenant, reservation))
        {
            lease = new TenantRebuildLease(reservation);
            return true;
        }

        if (_reservations.TryGetValue(normalizedTenant, out var existingState))
        {
            lock (existingState)
            {
                existingReservation = CreateSnapshot(existingState);
            }
        }

        return false;
    }

    public static TenantRebuildReservationSnapshot GetReservation(string tenant)
    {
        string normalizedTenant = NormalizeTenant(tenant);
        if (string.IsNullOrWhiteSpace(normalizedTenant))
        {
            return null;
        }

        if (!_reservations.TryGetValue(normalizedTenant, out var state))
        {
            return null;
        }

        lock (state)
        {
            return CreateSnapshot(state);
        }
    }

    public static IReadOnlyList<TenantRebuildReservationSnapshot> GetReservations()
    {
        return _reservations.Values
            .Select(state =>
            {
                lock (state)
                {
                    return CreateSnapshot(state);
                }
            })
            .OrderBy(item => item.tenant, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static void ResetForTests()
    {
        _reservations.Clear();
    }

    private static TenantRebuildReservationSnapshot CreateSnapshot(ReservationState state)
    {
        return new TenantRebuildReservationSnapshot
        {
            tenant = state.tenant,
            source = state.source,
            mode = state.mode,
            status = state.status,
            requested_utc = state.requested_utc
        };
    }

    private static string NormalizeTenant(string tenant)
    {
        return string.IsNullOrWhiteSpace(tenant) ? null : tenant.Trim();
    }
}


