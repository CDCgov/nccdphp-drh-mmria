using System;
using System.Collections.Generic;
using mmria.common.couchdb;
using mmria.common.getset;

namespace mmria.services.Utilities;

public static class PagedCaseIdLoader
{
    private const int DefaultPageSize = 1000;

    public static async IAsyncEnumerable<string> GetCaseIdsAsync(
        DBConfigurationDetail dbConfig,
        CouchDbHttpClient couchDbHttpClient,
        int pageSize = DefaultPageSize
    )
    {
        if
        (
            dbConfig == null ||
            couchDbHttpClient == null
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
            string requestString = $"{dbConfig.url}/{dbConfig.prefix}mmrds/_design/sortable/_view/by_date_created?skip={skip}&limit={pageSize}";
            string responseFromServer = await couchDbHttpClient.ExecuteAsync("GET", requestString, null, dbConfig.user_name, dbConfig.user_value);
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
