using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.Case;

public record CaseChangeFeedResult(string LastSeq, IReadOnlyList<CaseChangeEntry> Changes);
public record CaseChangeEntry(string Id, string Seq, bool Deleted, JObject? Doc);
