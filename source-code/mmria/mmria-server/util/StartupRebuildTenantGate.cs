using System;
using System.Threading;
using System.Threading.Tasks;

namespace mmria.server.util;

internal static class StartupRebuildTenantGate
{
    private static readonly object s_sync = new();
    private static SemaphoreSlim s_gate = new(1, 1);
    private static int s_max_concurrent_tenants = 1;

    internal static int CurrentCapacity
    {
        get
        {
            lock (s_sync)
            {
                return s_max_concurrent_tenants;
            }
        }
    }

    internal static void EnsureInitialized(int maxConcurrentTenants)
    {
        maxConcurrentTenants = Math.Max(1, maxConcurrentTenants);

        lock (s_sync)
        {
            if (s_gate == null)
            {
                s_gate = new SemaphoreSlim(maxConcurrentTenants, maxConcurrentTenants);
                s_max_concurrent_tenants = maxConcurrentTenants;
                return;
            }

            if (s_max_concurrent_tenants == maxConcurrentTenants)
            {
                return;
            }

            if (s_gate.CurrentCount == s_max_concurrent_tenants)
            {
                s_gate.Dispose();
                s_gate = new SemaphoreSlim(maxConcurrentTenants, maxConcurrentTenants);
                s_max_concurrent_tenants = maxConcurrentTenants;
            }
        }
    }

    internal static async Task<Lease> AcquireAsync(int maxConcurrentTenants)
    {
        EnsureInitialized(maxConcurrentTenants);

        SemaphoreSlim gate;
        lock (s_sync)
        {
            gate = s_gate;
        }

        await gate.WaitAsync();
        return new Lease(gate);
    }

    internal static void ResetForTests(int maxConcurrentTenants = 1)
    {
        maxConcurrentTenants = Math.Max(1, maxConcurrentTenants);

        lock (s_sync)
        {
            s_gate?.Dispose();
            s_gate = new SemaphoreSlim(maxConcurrentTenants, maxConcurrentTenants);
            s_max_concurrent_tenants = maxConcurrentTenants;
        }
    }

    internal sealed class Lease : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private bool _disposed;

        internal Lease(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _gate.Release();
        }
    }
}
