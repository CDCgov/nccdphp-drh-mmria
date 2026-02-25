using System;
using System.Security.Claims;
using System.Threading.Tasks;
using mmria.case_version.v260120;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.server.utils;
using Newtonsoft.Json;

namespace mmria.server.SharedLibraries.Manager;

public class CaseManager
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public CaseManager(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<mmria_case> GetCaseAsync(string caseId, DBConfigurationDetail dbConfig, ClaimsPrincipal user)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(caseId))
            {
                string request_string = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");
                string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    request_string,
                    null,
                    dbConfig.user_name,
                    dbConfig.user_value
                );

                var settings = new JsonSerializerSettings
                {
                    Converters = { 
                        new TimeOnlyJsonConverter(), 
                        new DateOnlyJsonConverter() 
                    }
                };

                var result = JsonConvert.DeserializeObject<mmria_case>(responseFromServer, settings);

                if (authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, ResourceRightEnum.ReadCase, result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }
}
