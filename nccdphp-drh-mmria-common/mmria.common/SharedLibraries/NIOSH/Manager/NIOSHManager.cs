using System;
using System.Threading.Tasks;
using mmria.common.niosh;
using mmria.common.SharedLibraries.NIOSH.DAL;

namespace mmria.common.SharedLibraries.NIOSH.Manager;

public sealed class NIOSHManager
{
    private readonly NIOSHDAL _dal;

    public NIOSHManager(NIOSHDAL dal)
    {
        _dal = dal;
    }

    public async Task<NioshResult> GetCodesAsync(string occupation, string industry)
    {
        var result = new NioshResult();
        var hasOccupation = !string.IsNullOrWhiteSpace(occupation);
        var hasIndustry = !string.IsNullOrWhiteSpace(industry);

        if (!hasOccupation && !hasIndustry)
        {
            return result;
        }

        try
        {
            return await _dal.GetCodesAsync(occupation, industry);
        }
        catch (Exception)
        {
            result.is_error = true;
            return result;
        }
    }
}
