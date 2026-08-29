using System.Collections.Generic;
using System.Threading.Tasks;

namespace mmria.server;

// SQL migration seam: covers startup database initialization only. Design-doc and index operations are on IDeIdentifiedRepository/IReportRepository (Story 24.2).
public interface IDatabaseLifecycleService
{
    Task Setup(
        bool triggerStartupRebuild = true,
        List<string> configuredStartupTenants = null,
        string summaryHostPrefix = null);
}
