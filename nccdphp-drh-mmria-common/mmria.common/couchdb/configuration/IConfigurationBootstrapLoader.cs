#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.getset;

namespace mmria.common.couchdb;

/// <summary>
/// Startup seam for loading tenant registry and shared configuration from a backing store.
/// Implemented by <see cref="MultiTenantConfigurationLoader"/> for CouchDB.
/// A SQL-backed implementation can be substituted without changing Program.cs.
/// </summary>
public interface IConfigurationBootstrapLoader
{
    Task<List<OverridableConfiguration>> LoadOverridableConfigurationsAsync(
        string[] tenants,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? sharedConfigId,
        string? configId,
        CouchDbHttpClient httpClient);

    Task<List<OverridableConfiguration>> LoadRequiredOverridableConfigurationsAsync(
        string[] tenants,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? sharedConfigId,
        string? configId,
        CouchDbHttpClient httpClient);

    Task<List<ConfigurationSet>> LoadConfigurationSetsAsync(
        string[] tenants,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? configId,
        CouchDbHttpClient httpClient);

    Task<List<ConfigurationSet>> LoadRequiredConfigurationSetsAsync(
        string[] tenants,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? configId,
        CouchDbHttpClient httpClient);

    Task<OverridableConfiguration?> LoadTenantOverridableConfigurationAsync(
        string tenantName,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        string? sharedConfigId,
        CouchDbHttpClient httpClient);

    Task<ConfigurationSet?> LoadTenantConfigurationSetAsync(
        string tenantName,
        string couchDbTemplateUrl,
        string? timerUserName,
        string? timerPassword,
        CouchDbHttpClient httpClient);
}
