using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.case_version.v260615;

namespace mmria.common.SharedLibraries.Case;

public interface ICaseRepository
{
    Task<mmria_case> GetCaseAsync(string caseId, DBConfigurationDetail dbConfig);
    Task<string> GetCaseDocumentJsonAsync(string caseId, DBConfigurationDetail dbConfig);
    Task<document_put_response> UpdateCaseAsync(string caseId, mmria_case caseDoc, DBConfigurationDetail dbConfig);
    Task<string> PutCaseDocumentJsonAsync(string caseId, string caseDocumentJson, DBConfigurationDetail dbConfig);
    Task<string> DeleteCaseAsync(string caseId, string revision, DBConfigurationDetail dbConfig);
    Task<string> GetCaseAtRevisionAsync(string caseId, string revision, DBConfigurationDetail dbConfig);
    Task<string> GetCaseRevisionsAsync(string caseId, DBConfigurationDetail dbConfig);
    Task<string> GetCaseRevisionsRawAsync(string caseId, DBConfigurationDetail dbConfig);
    Task<string> GetCasesByDateLastUpdatedViewJsonAsync(DBConfigurationDetail dbConfig);
    Task<string> GetCasesByDateLastUpdatedViewJsonAsync(DBConfigurationDetail dbConfig, int limit);
    Task<string> GetCasesByDateCreatedViewJsonAsync(DBConfigurationDetail dbConfig);
    Task<string> GetCasesByJurisdictionIdViewJsonAsync(DBConfigurationDetail dbConfig);
    Task<string> GetCasesByLastNameViewJsonAsync(DBConfigurationDetail dbConfig);
    Task<string> GetCasesByPmssNumberViewJsonAsync(DBConfigurationDetail dbConfig);
    Task<string> GetCaseRecordIdListViewJsonAsync(DBConfigurationDetail dbConfig);
    Task<string> GetCasesByIdViewJsonAsync(string caseId, DBConfigurationDetail dbConfig);
    Task<string> GetSoftLockedCaseIdForUserInAnotherTabAsync(string userName, string currentTabId, DBConfigurationDetail dbConfig);
    Task<bool> RecordIdExistsAsync(string recordId, DBConfigurationDetail dbInfo);
    Task<(int StatusCode, string Body)> GetCaseDocumentWithStatusAsync(string caseId, DBConfigurationDetail dbConfig);
    Task<string> GetAllCaseDocsAsync(bool includeDocs, DBConfigurationDetail dbConfig);
    Task<string> GetCasesByDateCreatedPagedAsync(int skip, int pageSize, DBConfigurationDetail dbConfig);
}
