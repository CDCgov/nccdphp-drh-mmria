using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using mmria.common.getset;
using mmria.common.Testing.CaseGeneration.Models;
using mmria.common.Testing.CaseGeneration.Writers;
using mmria.common.Testing.CaseGeneration.Utilities;
using mmria.common.Testing.CaseGeneration.Generators;
using mmria.common.Testing.CaseGeneration.Validators;

namespace mmria.common.Testing.CaseGeneration.Services
{
    /// <summary>
    /// Public API for MMRIA case generation.
    /// This service handles the complete workflow: metadata fetching, case generation, validation, and saving.
    /// </summary>
    public class CaseGeneratorService
    {
        private readonly CouchDbHttpClient? _couchDbHttpClient;

        /// <summary>
        /// Initialize the service with optional custom HTTP client for CouchDB operations.
        /// If no client is provided, a default one will be created.
        /// </summary>
        public CaseGeneratorService(CouchDbHttpClient? couchDbHttpClient = null)
        {
            _couchDbHttpClient = couchDbHttpClient;
        }

        /// <summary>
        /// Execute the complete case generation workflow with the provided configuration.
        /// </summary>
        /// <param name="config">Generation configuration (can be loaded from JSON file)</param>
        /// <returns>Generation result with generated cases and optional validation/save results</returns>
        public async Task<GenerationResult> GenerateCasesAsync(GenerationConfig config)
        {
            var result = new GenerationResult();

            try
            {
                // Setup HTTP client if not provided
                var httpClient = _couchDbHttpClient ?? CreateDefaultHttpClient();

                // Step 1: Fetch metadata
                var metadataManager = new MetadataManager(httpClient);
                await metadataManager.FetchMetadataAsync(config.GetResolvedMetadataUrl());

                // Step 2: Initialize jurisdiction rules
                var random = config.RandomSeed.HasValue 
                    ? new Random(config.RandomSeed.Value) 
                    : new Random();
                var jurisdictionEngine = new JurisdictionRuleEngine(config.Jurisdiction, random);

                // Step 3: Generate cases
                var generator = new CaseDataGenerator(metadataManager, config);
                var cases = generator.GenerateCases();
                result.GeneratedCases = cases;

                // Step 4: Validate cases if requested
                if (config.ValidateBeforeSave)
                {
                    var allNodes = new Dictionary<string, mmria.common.metadata.node>();
                    
                    if (metadataManager.Metadata?.children != null)
                    {
                        foreach (var form in metadataManager.Metadata.children)
                        {
                            CollectNodes(form, allNodes);
                        }
                    }
                    
                    var validator = new CaseValidator(allNodes, jurisdictionEngine);
                    result.ValidationReport = validator.ValidateBatch(cases);
                }

                // Step 5: Save to JSON files if output directory specified
                if (!string.IsNullOrEmpty(config.OutputDirectory))
                {
                    var jsonWriter = new JsonCaseWriter(config);
                    jsonWriter.WriteCasesToFiles(cases);
                    result.OutputDirectory = config.OutputDirectory;
                }

                // Step 6: Save to CouchDB if configured
                if (config.SaveToCouchDb && !string.IsNullOrEmpty(config.CouchDbUrl))
                {
                    var couchWriter = new CouchDbWriter(config, httpClient);
                    
                    // Test connection first
                    var (testSuccess, testError) = await couchWriter.TestConnectionAsync();
                    if (!testSuccess)
                    {
                        throw new Exception($"CouchDB connection failed: {testError}");
                    }

                    // Save cases
                    result.CouchDbResult = await couchWriter.SaveCasesBatchAsync(cases);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Create a default HTTP client for CouchDB operations if none is provided.
        /// This is used internally when the service is instantiated without a custom client.
        /// </summary>
        private static CouchDbHttpClient CreateDefaultHttpClient()
        {
            var services = new ServiceCollection();
            services.AddHttpClient("CouchDb");
            services.AddScoped<CouchDbHttpClient>();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<CouchDbHttpClient>();
        }

        /// <summary>
        /// Recursively collect all metadata nodes from the tree structure.
        /// Used for validation to ensure all fields have metadata definitions.
        /// </summary>
        private static void CollectNodes(mmria.common.metadata.node node, Dictionary<string, mmria.common.metadata.node> allNodes)
        {
            if (!allNodes.ContainsKey(node.name ?? ""))
                allNodes[node.name ?? ""] = node;

            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    CollectNodes(child, allNodes);
                }
            }
        }
    }
}

