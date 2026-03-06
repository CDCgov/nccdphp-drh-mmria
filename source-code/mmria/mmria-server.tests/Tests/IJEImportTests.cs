#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.Case.Manager;
using mmria.common.SharedLibraries.CaseView;
using mmria.common.SharedLibraries.MMRIAServices.Helper;
using mmria.common.Testing.IJEGeneration.Models;
using mmria.common.Testing.IJEGeneration.Services;
using mmria.common.couchdb;
using mmria_server.tests.Helpers;
using NUnit.Framework;
using RecordsProcessor_Worker.Services;

namespace mmria_server.tests.Tests;

[TestFixture]
public class IJEImportTests
{
    private const int MorMaxLength = 5001;
    private const int NatMaxLength = 4001;
    private const int FetMaxLength = 6001;

    private TestEnvironment _env = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _env = await TestEnvironment.BootstrapAsync("ije-import");
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        await _env.ResolveConfigurationAsync();
        var cleared = await _env.DbHelper.ClearTestDatabaseAsync();
        Assert.That(cleared, Is.True, "Failed to clear /mmrds before IJE import test.");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _env.CleanupAsync();
    }

    [Test]
    [Category("IJE")]
    public async Task Scenario_A_ImportGeneratedIJE_AndRetrieveViaCaseApis()
    {
        var cfg = _env.Config!;
        var configLoader = cfg.ConfigLoader;

        Assert.That(string.Equals(cfg.HostPrefix, configLoader.TargetTestTenant, StringComparison.OrdinalIgnoreCase), Is.True,
            $"Resolved host prefix '{cfg.HostPrefix}' should match target_test_tenant '{configLoader.TargetTestTenant}'.");

        var importDate = DateTime.UtcNow;
        var generatedFiles = await GenerateIjeFilesAsync(configLoader, importDate);

        var morFile = generatedFiles.Single(f => string.Equals(f.FileType, "MOR", StringComparison.OrdinalIgnoreCase));
        var natFile = generatedFiles.Single(f => string.Equals(f.FileType, "NAT", StringComparison.OrdinalIgnoreCase));
        var fetFile = generatedFiles.Single(f => string.Equals(f.FileType, "FET", StringComparison.OrdinalIgnoreCase));

        Assert.That(morFile.RecordCount, Is.EqualTo(configLoader.IjeNumberToGenerate), "Generated MOR record count should match configured IJE count.");

        InitializeVitalsImportStatics(cfg);

        var batchItemProcessingService = new BatchItemProcessingService(_env.CouchDbClient);
        var importResults = new List<(mmria.common.ije.BatchItemComplete completion, mmria.common.ije.BatchItem batchItem, string expectedResidenceStreet, string expectedResidenceState)>();

        foreach (var morRecord in morFile.Records)
        {
            var cdcUniqueId = GetFixedWidthValue(morRecord, 191, 9);
            var recordId = $"ije-test-{Guid.NewGuid():N}";

            var message = new mmria.common.ije.StartBatchItemMessage
            {
                case_folder = "/",
                cdc_unique_id = cdcUniqueId,
                record_id = recordId,
                ImportDate = importDate,
                ImportFileName = morFile.FileName,
                host_state = cfg.HostPrefix,
                mor = morRecord,
                nat = MMRIAServicesHelper.GetAssociatedNat(natFile.Records.ToArray(), cdcUniqueId),
                fet = MMRIAServicesHelper.GetAssociatedFet(fetFile.Records.ToArray(), cdcUniqueId),
                BatchProcessorPath = "mmria-server.tests/ije-import"
            };

            var expectedResidenceStreet = GetExpectedResidenceStreet(morRecord);
            var expectedResidenceState = GetExpectedResidenceState(morRecord);

            var result = await batchItemProcessingService.Process_Message(message);
            importResults.Add((result.completion, result.batchItem, expectedResidenceStreet, expectedResidenceState));
        }

        Assert.That(importResults, Has.Count.EqualTo(configLoader.IjeNumberToGenerate));

        var failures = importResults.Where(r => !r.completion.success).ToList();
        Assert.That(failures, Is.Empty,
            $"Expected all generated IJE records to import successfully. Failures: {string.Join(" | ", failures.Select(f => f.completion.error_message ?? f.batchItem.StatusDetail ?? "unknown"))}");

        Assert.That(importResults.All(r => r.batchItem.Status == mmria.common.ije.BatchItem.StatusEnum.NewCaseAdded), Is.True,
            $"Expected all imported items to be marked NewCaseAdded. Actual statuses: {string.Join(", ", importResults.Select(r => r.batchItem.Status))}");

        var expectedCaseIds = importResults
            .Select(r => r.batchItem.mmria_id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.That(expectedCaseIds, Has.Count.EqualTo(configLoader.IjeNumberToGenerate), "Each imported IJE record should produce a distinct case id.");

        var principal = await AuthenticateAsDefaultCaseUserAsync(cfg);
        var caseViewManager = new CaseViewManager(cfg.DbConfig, principal, true, false, _env.CouchDbClient);
        var caseManager = new CaseManager(_env.CouchDbClient);

        var caseList = await caseViewManager.execute(
            CancellationToken.None,
            skip: 0,
            take: configLoader.IjeNumberToGenerate + 10,
            sort: "by_date_created",
            search_key: null,
            descending: true,
            case_status: "all",
            field_selection: "all",
            pregnancy_relatedness: "all",
            date_of_death_range: "all",
            date_of_review_range: "all");

        Assert.That(caseList, Is.Not.Null, "Case list result should not be null after IJE import.");
        Assert.That(caseList.rows, Is.Not.Null, "Case list rows should not be null after IJE import.");
        Assert.That(caseList.total_rows, Is.EqualTo(expectedCaseIds.Count), "Expected imported cases to be visible in case list.");

        var actualCaseIds = caseList.rows
            .Select(r => r.id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingCaseIds = expectedCaseIds.Except(actualCaseIds, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.That(missingCaseIds, Is.Empty,
            $"Expected imported case ids to appear in case list. Missing: {string.Join(", ", missingCaseIds)}");

        foreach (var result in importResults)
        {
            var caseId = result.batchItem.mmria_id;
            Assert.That(string.IsNullOrWhiteSpace(caseId), Is.False, "Imported batch item should include mmria_id.");

            var caseDetail = await caseManager.GetCaseAsync(caseId!, cfg.DbConfig, principal);

            Assert.That(caseDetail, Is.Not.Null, $"Imported case {caseId} should be retrievable via CaseManager.GetCaseAsync.");
            Assert.That(caseDetail!._id, Is.EqualTo(caseId));
            Assert.That(caseDetail.home_record, Is.Not.Null, $"Imported case {caseId} should include home_record.");
            Assert.That(caseDetail.home_record.record_id, Is.EqualTo(result.batchItem.mmria_record_id),
                $"Imported case {caseId} should preserve record_id from the start batch item message.");
            Assert.That(caseDetail.home_record.last_name, Is.EqualTo(result.batchItem.LastName?.Trim()),
                $"Imported case {caseId} last name should match IJE batch item summary.");
            Assert.That(caseDetail.home_record.first_name, Is.EqualTo(result.batchItem.FirstName?.Trim()),
                $"Imported case {caseId} first name should match IJE batch item summary.");
            Assert.That(caseDetail.death_certificate.place_of_last_residence.street, Is.EqualTo(result.expectedResidenceStreet),
                $"Imported case {caseId} residence street should match the normalized MOR address.");
            Assert.That(caseDetail.death_certificate.place_of_last_residence.street, Does.Not.Contain("  "),
                $"Imported case {caseId} residence street should not contain repeated spaces.");
            Assert.That(caseDetail.death_certificate.place_of_last_residence.state, Is.EqualTo(result.expectedResidenceState),
                $"Imported case {caseId} residence state should match the MOR state value used for the lookup-backed control.");
        }
    }

    [TestCase("2026_2026_01_18_OH.MOR", "OH")]
    [TestCase("2026_2026_01_18_KY.MOR", "KY")]
    [TestCase("2026_2026_01_18_GA.MOR", "GA")]
    [TestCase("2026_2026_01_18_TENANT1.MOR", "tenant1")]
    [TestCase("2026_2026_01_18_TENANT5.MOR", "tenant5")]
    [TestCase("2026_2026_01_18_TENANT1QA.MOR", "tenant1qa")]
    [TestCase("2026_2026_01_18_TENANT5QA.MOR", "tenant5qa")]
    [Category("IJE")]
    public void Scenario_B_InitializeBatchImport_AcceptsSupportedMorFileFormats(string morFileName, string expectedReportingState)
    {
        var cfg = _env.Config!;
        var message = CreateBatchImportMessage(morFileName);
        var configurationSet = CreateBatchImportConfigurationSet(cfg, expectedReportingState);

        var result = MMRIAServicesHelper.InitializeBatchImport(message, configurationSet, MorMaxLength, NatMaxLength, FetMaxLength);

        Assert.That(string.Equals(result.ReportingState, expectedReportingState, StringComparison.OrdinalIgnoreCase), Is.True,
            $"Expected '{morFileName}' to resolve reporting state '{expectedReportingState}'.");
        Assert.That(result.IsValidFileName, Is.True, $"Expected '{morFileName}' to be treated as a valid MOR file name.");
        Assert.That(result.StatusBuilder.ToString(), Does.Not.Contain("mor file name format incorrect"),
            $"Expected '{morFileName}' to match the MOR file naming rules.");
        Assert.That(result.ItemDbInfo, Is.Not.Null, $"Expected '{morFileName}' to resolve to a configured reporting state.");
    }

    [TestCase("2026_2026_01_18_OHIO.MOR", "OHIO")]
    [TestCase("2026_2026_01_18_KENTUCKY.MOR", "KENTUCKY")]
    [TestCase("2026_2026_01_18_GEORGIA.MOR", "GEORGIA")]
    [Category("IJE")]
    public void Scenario_C_InitializeBatchImport_RejectsFullStateNamesInMorFileFormats(string morFileName, string expectedReportingState)
    {
        var cfg = _env.Config!;
        var message = CreateBatchImportMessage(morFileName);
        var configurationSet = CreateBatchImportConfigurationSet(cfg, "OH", "KY", "GA");

        var result = MMRIAServicesHelper.InitializeBatchImport(message, configurationSet, MorMaxLength, NatMaxLength, FetMaxLength);

        Assert.That(string.Equals(result.ReportingState, expectedReportingState, StringComparison.OrdinalIgnoreCase), Is.True,
            $"Expected '{morFileName}' to resolve reporting state '{expectedReportingState}'.");
        Assert.That(result.IsValidFileName, Is.False, $"Expected '{morFileName}' to be rejected as a MOR file name.");
        Assert.That(result.StatusBuilder.ToString(), Does.Contain("Invalid reporting state"),
            $"Expected '{morFileName}' to be rejected because full state names are not accepted reporting states.");
    }

    private async Task<IReadOnlyList<GeneratedIJEFile>> GenerateIjeFilesAsync(TestConfigurationLoader configLoader, DateTime importDate)
    {
        var service = new IJEGeneratorService();
        var generationResult = await service.GenerateFilesAsync(new IJEGenerationConfig
        {
            RecordsPerFile = configLoader.IjeNumberToGenerate,
            StateCode = configLoader.TargetTestTenant,
            JurisdictionSampling = configLoader.IjeJurisdicationSampling.ToList(),
            YearOfDeathSampling = configLoader.IjeYearOfDeathSampling.ToList(),
            WriteFilesToDisk = false,
            Timestamp = importDate,
            RandomSeed = 99999
        });

        Assert.That(generationResult.Success, Is.True, $"IJE generation should succeed: {generationResult.ErrorMessage}");
        Assert.That(generationResult.GeneratedFiles, Has.Count.EqualTo(3), "Expected MOR, NAT, and FET generated files.");

        return generationResult.GeneratedFiles;
    }

    private void InitializeVitalsImportStatics(TestEnvironmentConfig cfg)
    {
        mmria.services.vitalsimport.Program.couchdb_url = cfg.DbConfig.url;
        mmria.services.vitalsimport.Program.db_prefix = cfg.DbConfig.prefix;
        mmria.services.vitalsimport.Program.timer_user_name = cfg.DbConfig.user_name;
        mmria.services.vitalsimport.Program.timer_value = cfg.DbConfig.user_value;
        mmria.services.vitalsimport.Program.DbConfigSet = BuildServicesConfigurationSet(cfg);
    }

    private async Task<ClaimsPrincipal> AuthenticateAsDefaultCaseUserAsync(TestEnvironmentConfig cfg)
    {
        const string UserName = "user5";
        const string Password = "password";
        const string Issuer = "https://contoso.com";

        var loginResult = await _env.AccountTestHelper.AuthenticateAndCreateSessionAsync(
            UserName,
            Password,
            cfg.DbConfig,
            cfg.Configuration,
            cfg.HostPrefix);

        if (loginResult.IsUnauthorized && loginResult.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            Assert.Inconclusive($"Test user '{UserName}' does not exist in test database.");
        }

        Assert.That(loginResult.IsSuccessful, Is.True, $"User authentication failed: {loginResult.ErrorMessage}");
        Assert.That(loginResult.SessionInfo, Is.Not.Null, "SessionInfo required for case retrieval assertions.");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, UserName, ClaimValueTypes.String, Issuer)
        };

        foreach (var role in loginResult.SessionInfo!.Roles ?? new List<string>())
        {
            claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "SuperSecureLogin"));
    }

    private static ConfigurationSet BuildServicesConfigurationSet(TestEnvironmentConfig cfg)
    {
        var configurationSet = new ConfigurationSet();
        configurationSet.detail_list[cfg.HostPrefix] = cfg.DbConfig;

        if (cfg.Configuration.string_keys.TryGetValue("shared", out var sharedValues))
        {
            foreach (var entry in sharedValues)
            {
                configurationSet.name_value[entry.Key] = entry.Value;
            }
        }

        configurationSet.name_value["metadata_version"] = cfg.MetadataVersion;

        if (!configurationSet.name_value.ContainsKey("geocode_api_key"))
        {
            configurationSet.name_value["geocode_api_key"] = cfg.Configuration.GetString("geocode_api_key", cfg.HostPrefix) ?? string.Empty;
        }

        return configurationSet;
    }

    private static ConfigurationSet CreateBatchImportConfigurationSet(TestEnvironmentConfig cfg, params string[] reportingStates)
    {
        var configurationSet = new ConfigurationSet();

        foreach (var reportingState in reportingStates)
        {
            configurationSet.detail_list[reportingState] = cfg.DbConfig;
            configurationSet.detail_list[reportingState.ToUpperInvariant()] = cfg.DbConfig;
        }

        return configurationSet;
    }

    private static mmria.common.ije.NewIJESet_Message CreateBatchImportMessage(string morFileName)
    {
        return new mmria.common.ije.NewIJESet_Message
        {
            batch_id = $"test-batch-{Guid.NewGuid():N}",
            mor = string.Empty,
            nat = string.Empty,
            fet = string.Empty,
            mor_file_name = morFileName,
            nat_file_name = "test.NAT",
            fet_file_name = "test.FET",
            case_folder = "/"
        };
    }

    private static string GetFixedWidthValue(string record, int startPosition, int length)
    {
        if (string.IsNullOrWhiteSpace(record) || record.Length < startPosition - 1 + length)
        {
            return string.Empty;
        }

        return record.Substring(startPosition - 1, length).Trim();
    }

    private static string GetExpectedResidenceStreet(string morRecord)
    {
        return MMRIAServicesHelper.PLACE_OF_LAST_RESIDENCE_street_Rule(
            GetFixedWidthValue(morRecord, 1485, 10),
            GetFixedWidthValue(morRecord, 1495, 10),
            GetFixedWidthValue(morRecord, 1505, 28),
            GetFixedWidthValue(morRecord, 1533, 10),
            GetFixedWidthValue(morRecord, 1543, 10));
    }

    private static string GetExpectedResidenceState(string morRecord)
    {
        var state = GetFixedWidthValue(morRecord, 225, 2);
        return string.IsNullOrWhiteSpace(state) ? "9999" : state;
    }
}