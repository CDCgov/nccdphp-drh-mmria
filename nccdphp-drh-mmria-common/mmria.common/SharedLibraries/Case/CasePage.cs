using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.Case;

public record CasePage(IReadOnlyList<JObject> Documents, string? LastId);
