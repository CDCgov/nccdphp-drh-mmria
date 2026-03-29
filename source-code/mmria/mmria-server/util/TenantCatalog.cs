using System;
using System.Collections.Generic;
using System.Linq;
using mmria.common.couchdb;

namespace mmria.server.util;

public sealed class TenantCatalog
{
    private readonly RootRuntimeSettings _rootRuntimeSettings;
    private readonly List<OverridableConfiguration> _overridableConfigurations;
    private readonly List<ConfigurationSet> _configurationSets;

    public TenantCatalog(
        RootRuntimeSettings rootRuntimeSettings,
        List<OverridableConfiguration> overridableConfigurations,
        List<ConfigurationSet> configurationSets)
    {
        _rootRuntimeSettings = rootRuntimeSettings ?? throw new ArgumentNullException(nameof(rootRuntimeSettings));
        _overridableConfigurations = overridableConfigurations ?? throw new ArgumentNullException(nameof(overridableConfigurations));
        _configurationSets = configurationSets ?? throw new ArgumentNullException(nameof(configurationSets));
    }

    public bool IsTenantAvailable(string? hostPrefix)
    {
        if (!_rootRuntimeSettings.IsMultiTenantMode)
        {
            return TryResolveConfiguration(hostPrefix) != null &&
                TryResolveDbConfig(hostPrefix) != null;
        }

        return TryResolveConfiguration(hostPrefix) != null &&
            TryResolveDbConfig(hostPrefix) != null;
    }

    public OverridableConfiguration? TryResolveConfiguration(string? hostPrefix)
    {
        lock (_overridableConfigurations)
        {
            if (_overridableConfigurations.Count == 0)
            {
                return null;
            }

            if (!_rootRuntimeSettings.IsMultiTenantMode)
            {
                return _overridableConfigurations[0];
            }

            string? normalizedHostPrefix = NormalizeHostPrefix(hostPrefix);
            if (string.IsNullOrWhiteSpace(normalizedHostPrefix))
            {
                return null;
            }

            return _overridableConfigurations.FirstOrDefault(configuration =>
                MatchesTenantConfiguration(configuration, normalizedHostPrefix));
        }
    }

    public DBConfigurationDetail? TryResolveDbConfig(string? hostPrefix)
    {
        lock (_configurationSets)
        {
            if (_configurationSets.Count == 0)
            {
                return null;
            }

            if (!_rootRuntimeSettings.IsMultiTenantMode)
            {
                return TryResolveSingleTenantDbConfig(_configurationSets[0]);
            }

            string? normalizedHostPrefix = NormalizeHostPrefix(hostPrefix);
            if (string.IsNullOrWhiteSpace(normalizedHostPrefix))
            {
                return null;
            }

            foreach (var configurationSet in _configurationSets)
            {
                if (configurationSet?.detail_list == null)
                {
                    continue;
                }

                if (string.Equals(configurationSet._id, normalizedHostPrefix, StringComparison.OrdinalIgnoreCase) &&
                    configurationSet.detail_list.TryGetValue(normalizedHostPrefix, out var exactMatch))
                {
                    return exactMatch;
                }

                if (configurationSet.detail_list.TryGetValue(normalizedHostPrefix, out var keyedMatch))
                {
                    return keyedMatch;
                }
            }
        }

        return null;
    }

    public ConfigurationSet? TryResolveConfigurationSet(string? hostPrefix)
    {
        lock (_configurationSets)
        {
            if (_configurationSets.Count == 0)
            {
                return null;
            }

            if (!_rootRuntimeSettings.IsMultiTenantMode)
            {
                return _configurationSets[0];
            }

            string? normalizedHostPrefix = NormalizeHostPrefix(hostPrefix);
            if (string.IsNullOrWhiteSpace(normalizedHostPrefix))
            {
                return null;
            }

            return _configurationSets.FirstOrDefault(configurationSet =>
                string.Equals(configurationSet?._id, normalizedHostPrefix, StringComparison.OrdinalIgnoreCase) ||
                configurationSet?.detail_list?.ContainsKey(normalizedHostPrefix) == true);
        }
    }

    public void UpsertOverridableConfiguration(OverridableConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        lock (_overridableConfigurations)
        {
            if (!_rootRuntimeSettings.IsMultiTenantMode)
            {
                if (_overridableConfigurations.Count == 0)
                {
                    _overridableConfigurations.Add(configuration);
                }
                else
                {
                    _overridableConfigurations[0] = configuration;
                }

                return;
            }

            var tenant = GetTenantNameFromConfiguration(configuration);
            for (var i = 0; i < _overridableConfigurations.Count; i++)
            {
                if (string.Equals(GetTenantNameFromConfiguration(_overridableConfigurations[i]), tenant, StringComparison.OrdinalIgnoreCase))
                {
                    _overridableConfigurations[i] = configuration;
                    return;
                }
            }

            _overridableConfigurations.Add(configuration);
        }
    }

    public void UpsertConfigurationSet(ConfigurationSet configurationSet)
    {
        if (configurationSet == null)
        {
            throw new ArgumentNullException(nameof(configurationSet));
        }

        lock (_configurationSets)
        {
            if (!_rootRuntimeSettings.IsMultiTenantMode)
            {
                if (_configurationSets.Count == 0)
                {
                    _configurationSets.Add(configurationSet);
                }
                else
                {
                    _configurationSets[0] = configurationSet;
                }

                return;
            }

            for (var i = 0; i < _configurationSets.Count; i++)
            {
                if (string.Equals(_configurationSets[i]?._id, configurationSet._id, StringComparison.OrdinalIgnoreCase))
                {
                    _configurationSets[i] = configurationSet;
                    return;
                }
            }

            _configurationSets.Add(configurationSet);
        }
    }

    private DBConfigurationDetail? TryResolveSingleTenantDbConfig(ConfigurationSet configurationSet)
    {
        if (configurationSet?.detail_list == null || configurationSet.detail_list.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_rootRuntimeSettings.SingleTenantName) &&
            configurationSet.detail_list.TryGetValue(_rootRuntimeSettings.SingleTenantName, out var namedMatch))
        {
            return namedMatch;
        }

        return configurationSet.detail_list.Values.FirstOrDefault();
    }

    private string? GetTenantNameFromConfiguration(OverridableConfiguration? configuration)
    {
        if (configuration == null || string.IsNullOrWhiteSpace(configuration._id))
        {
            return null;
        }

        string? sharedConfigId = _rootRuntimeSettings.SharedConfigId;
        if (string.IsNullOrWhiteSpace(sharedConfigId))
        {
            return configuration._id;
        }

        string suffix = $"_{sharedConfigId}";
        if (!configuration._id.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return configuration._id;
        }

        return configuration._id[..^suffix.Length];
    }

    private static bool MatchesTenantConfiguration(OverridableConfiguration? configuration, string hostPrefix)
    {
        if (configuration == null || string.IsNullOrWhiteSpace(configuration._id))
        {
            return false;
        }

        return configuration._id.StartsWith($"{hostPrefix}_", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeHostPrefix(string? hostPrefix)
    {
        return string.IsNullOrWhiteSpace(hostPrefix) ? null : hostPrefix.Trim();
    }
}
