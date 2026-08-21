using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.server.model;

public sealed class FileUploadModel
{
    [MaxFileSize(5 * 1024 * 1024)]
    [AllowedExtensions(new string[] { ".fet" })]
    [Required]
    [Display(Name ="FET File Upload: ")]
    public IFormFile FileUpload_FET { get; set; }

    [MaxFileSize(5 * 1024 * 1024)]
    [AllowedExtensions(new string[] { ".nat" })]
    [Required]
    [Display(Name = "NAT File Upload: ")]
    public IFormFile FileUpload_NAT { get; set; }

    [MaxFileSize(5 * 1024 * 1024)]
    [AllowedExtensions(new string[] { ".mor" })]
    [Required]
    [Display(Name = "MOR File Upload: ")]
    public IFormFile FileUpload_MOR { get; set; }
}


public sealed class NewIJESet_Message
{
    public string mor { get; set; }

    public string nat { get; set; }

    public string fet { get; set; }

    public string mor_file_name { get; set; }

    public string nat_file_name { get; set; }

    public string fet_file_name { get; set; }

    public string case_folder { get; set; }
}

public sealed class NewIJESet_MessageResponse
{
    public string batch_id { get; set; }

    public bool ok { get; set; }

    public string detail { get; set; }

    // Story 38.1: populated when the upload is rejected because a prior
    // Finished/FinishedSynchronized batch already imported one of the same
    // file names. Null/empty when this is not a duplicate rejection.
    public string duplicate_batch_id { get; set; }
    public string matched_file_name { get; set; }
    public DateTime? original_batch_date { get; set; }
    public int? skipped_record_count { get; set; }
}
