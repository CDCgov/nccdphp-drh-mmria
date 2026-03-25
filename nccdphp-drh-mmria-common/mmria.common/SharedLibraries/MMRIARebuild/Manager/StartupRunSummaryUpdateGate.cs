using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace mmria.common.SharedLibraries.MMRIARebuild.Manager;

internal static class StartupRunSummaryUpdateGate
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> s_gates =
        new(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeHostPrefix(string hostPrefix)
    {
        return string.IsNullOrWhiteSpace(hostPrefix) ? "shared" : hostPrefix.Trim();
    }

    internal static async Task<Lease> AcquireAsync(string hostPrefix)
    {
        string normalizedHostPrefix = NormalizeHostPrefix(hostPrefix);
        SemaphoreSlim gate = s_gates.GetOrAdd(normalizedHostPrefix, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        return new Lease(gate);
    }

    internal static void ResetForTests()
    {
        foreach (var entry in s_gates)
        {
            entry.Value.Dispose();
        }

        s_gates.Clear();
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
