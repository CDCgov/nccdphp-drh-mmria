using System;
using System.Collections.Generic;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.Case;

namespace mmria.services.Utilities;

public static class PagedCaseIdLoader
{
    private const int DefaultPageSize = 1000;

    public static async IAsyncEnumerable<string> GetCaseIdsAsync(
        DBConfigurationDetail dbConfig,
        ICaseRepository caseRepository,
        int pageSize = DefaultPageSize
    )
    {
        if
        (
            dbConfig == null ||
            caseRepository == null
        )
        {
            yield break;
        }

        if (pageSize <= 0)
        {
            pageSize = DefaultPageSize;
        }

        var skip = 0;

        while (true)
        {
            string responseFromServer = await caseRepository.GetCasesByDateCreatedPagedAsync(skip, pageSize, dbConfig);
            var caseViewResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            if
            (
                caseViewResponse?.rows == null ||
                caseViewResponse.rows.Count == 0
            )
            {
                yield break;
            }

            foreach (var caseViewItem in caseViewResponse.rows)
            {
                if (!string.IsNullOrWhiteSpace(caseViewItem?.id))
                {
                    yield return caseViewItem.id;
                }
            }

            if (caseViewResponse.rows.Count < pageSize)
            {
                yield break;
            }

            skip += caseViewResponse.rows.Count;
        }
    }

    public static IEnumerable<string> GetRequestedCaseIds(IEnumerable<string> caseIds)
    {
        if (caseIds == null)
        {
            yield break;
        }

        var seenCaseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var caseId in caseIds)
        {
            if
            (
                string.IsNullOrWhiteSpace(caseId) ||
                !seenCaseIds.Add(caseId)
            )
            {
                continue;
            }

            yield return caseId;
        }
    }
}
