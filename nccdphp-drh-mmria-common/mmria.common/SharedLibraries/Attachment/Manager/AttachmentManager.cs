using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.Attachment.DAL;
using mmria.common.SharedLibraries.Attachment.Model;
using mmria.common.couchdb;

namespace mmria.common.SharedLibraries.Attachment.Manager;

public sealed class AttachmentManager
{
    private readonly AttachmentDAL _dal;

    public AttachmentManager(AttachmentDAL dal)
    {
        _dal = dal;
    }

    public string GetInvalidPdfFileName(IEnumerable<string> fileNames)
    {
        foreach (var file_name in fileNames)
        {
            if (!file_name.EndsWith(".pdf"))
            {
                return file_name;
            }
        }

        return null;
    }

    public async Task<CentralUploadResolutionResult> ResolveCentralUploadAsync(
        string[] file_name_list,
        DBConfigurationDetail db_config)
    {
        var result = new CentralUploadResolutionResult();
        var valid_file_format = new Regex("\\d\\d\\d\\d\\d\\d\\d\\d.pdf", RegexOptions.IgnoreCase);

        for (var i = 0; i < file_name_list.Length; i++)
        {
            var file_name = file_name_list[i];

            if (!valid_file_format.IsMatch(file_name))
            {
                result.ResultMessages.Add($"Invalid File name: {file_name}");
                result.IsRejectBatch = true;
            }
            else
            {
                var search_text = $"{file_name.Substring(0, 2)}-{file_name.Substring(2, 2)}-{file_name.Substring(4, 4)}";
                var search_result = await _dal.GetPmssCaseViewByNumberAsync(search_text, db_config);

                if (search_result.rows.Count == 1)
                {
                    result.PmssNoToId.Add(file_name, search_result.rows[0].id);
                    result.IsValidList.Add(true);
                }
                else
                {
                    result.ResultMessages.Add($"row_count:{search_result.rows.Count} Invalid File name: {file_name}");
                    result.IsValidList.Add(false);
                    result.IsRejectBatch = true;
                }
            }
        }

        return result;
    }
}
