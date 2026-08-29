using System;

namespace mmria.common.SharedLibraries.Case.Manager;

/// <summary>
/// Thrown by <see cref="CaseManager.GenerateUniqueRecordIdAsync"/> when the configured
/// number of collision-retry attempts is exhausted without finding a free 4-digit suffix
/// for the given <see cref="StatePrefix"/> / <see cref="Year"/> segment.
/// </summary>
public sealed class RecordIdGenerationExhaustedException : Exception
{
    public string StatePrefix { get; }
    public string Year { get; }
    public int Attempts { get; }

    public RecordIdGenerationExhaustedException(string statePrefix, string year, int attempts)
        : base($"Unable to generate a unique record ID for {statePrefix}-{year} after {attempts} attempts.")
    {
        StatePrefix = statePrefix;
        Year = year;
        Attempts = attempts;
    }
}
