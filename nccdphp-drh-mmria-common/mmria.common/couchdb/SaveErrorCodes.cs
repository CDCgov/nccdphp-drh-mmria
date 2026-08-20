namespace mmria.common.model.couchdb;

/// <summary>
/// Structured error codes returned on <see cref="document_put_response"/> when a
/// case save is rejected by a server-side guard. Callers (server, services, JS clients)
/// must string-match against these constants rather than parsing English error text.
/// </summary>
public static class SaveErrorCodes
{
    /// <summary>Record ID does not match the STATE-YEAR-NNNN pattern.</summary>
    public const string RecordIdFormat = "record_id_format";

    /// <summary>Record ID is already in use in the target jurisdiction database.</summary>
    public const string RecordIdConflict = "record_id_conflict";
}
