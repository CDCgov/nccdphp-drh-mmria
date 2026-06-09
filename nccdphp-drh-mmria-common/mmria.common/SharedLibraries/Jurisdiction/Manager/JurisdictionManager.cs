using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Jurisdiction.DAL;
using mmria.common.utils;

namespace mmria.common.SharedLibraries.Jurisdiction.Manager;

public sealed class JurisdictionManager
{
    private readonly JurisdictionDAL _dal;

    public JurisdictionManager(JurisdictionDAL dal)
    {
        _dal = dal;
    }

    public async Task<jurisdiction_tree> GetJurisdictionTreeAsync(DBConfigurationDetail dbConfig)
    {
        return await _dal.GetJurisdictionTreeAsync(dbConfig);
    }

    public async Task<document_put_response> SaveJurisdictionTreeAsync(
        jurisdiction_tree jurisdictionTree,
        string currentUserName,
        DBConfigurationDetail dbConfig)
    {
        var result = new document_put_response();
        var existingTree = await GetCurrentJurisdictionTreeAsync(dbConfig);
        var resolvedRevision = CouchDbRevisionHelper.ResolveServerOwnedRevision(
            jurisdictionTree?._rev,
            existingTree?._rev);

        var sanitizedJurisdictionTree = CreateSanitizedJurisdictionTree(
            jurisdictionTree,
            currentUserName,
            resolvedRevision);

        if (sanitizedJurisdictionTree == null)
        {
            return result;
        }

        return await _dal.SaveJurisdictionTreeAsync(sanitizedJurisdictionTree, dbConfig);
    }

    private async Task<jurisdiction_tree> GetCurrentJurisdictionTreeAsync(DBConfigurationDetail dbConfig)
    {
        try
        {
            return await _dal.GetJurisdictionTreeAsync(dbConfig);
        }
        catch
        {
            return null;
        }
    }

    private static jurisdiction_tree CreateSanitizedJurisdictionTree(
        jurisdiction_tree request,
        string currentUserName,
        string resolvedRevision)
    {
        if (request == null)
        {
            return null;
        }

        return new jurisdiction_tree
        {
            _rev = resolvedRevision,
            date_created = request.date_created == default ? DateTime.UtcNow : request.date_created,
            created_by = string.IsNullOrWhiteSpace(request.created_by) ? currentUserName : SanitizeSingleLineText(request.created_by, 256),
            date_last_updated = DateTime.UtcNow,
            last_updated_by = SanitizeSingleLineText(currentUserName, 256),
            children = request.children?
                .Where(child => child != null)
                .Select(child => CreateSanitizedJurisdiction(child, currentUserName))
                .Where(child => child != null)
                .ToArray() ?? Array.Empty<jurisdiction>()
        };
    }

    private static jurisdiction CreateSanitizedJurisdiction(
        jurisdiction request,
        string currentUserName)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.id))
        {
            return null;
        }

        return new jurisdiction
        {
            id = SanitizeSingleLineText(request.id, 256),
            name = SanitizeSingleLineText(request.name, 256),
            date_created = request.date_created == default ? DateTime.UtcNow : request.date_created,
            created_by = string.IsNullOrWhiteSpace(request.created_by) ? currentUserName : SanitizeSingleLineText(request.created_by, 256),
            date_last_updated = DateTime.UtcNow,
            last_updated_by = SanitizeSingleLineText(currentUserName, 256),
            is_active = request.is_active,
            is_enabled = request.is_enabled,
            parent_id = SanitizeSingleLineText(request.parent_id, 256),
            children = request.children?
                .Where(child => child != null)
                .Select(child => CreateSanitizedJurisdiction(child, currentUserName))
                .Where(child => child != null)
                .ToList() ?? new List<jurisdiction>()
        };
    }

    private static string SanitizeSingleLineText(string value, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }
}
