using System;
using System.Text;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.niosh;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.NIOSH.DAL;

public sealed class NIOSHDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public NIOSHDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NioshResult> GetCodesAsync(string occupation, string industry)
    {
        var builder = new StringBuilder();
        builder.Append("https://wwwn.cdc.gov/nioccs/IOCode?n=3");

        if (!string.IsNullOrWhiteSpace(occupation))
        {
            builder.Append($"&o={Uri.EscapeDataString(occupation)}");
        }

        if (!string.IsNullOrWhiteSpace(industry))
        {
            builder.Append($"&i={Uri.EscapeDataString(industry)}");
        }

        string response = await _httpClient.ExecuteAsync("GET", builder.ToString(), null, null, null);
        return JsonConvert.DeserializeObject<NioshResult>(response) ?? new NioshResult();
    }
}
