using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using mmria.common.Testing.CaseGeneration.Models;
using mmria.common.Testing.CaseGeneration.Generators.ValueGenerators;
using mmria.common.metadata;

namespace mmria.common.Testing.CaseGeneration.Generators
{
    public class CaseDataGenerator
    {
        private readonly MetadataManager _metadataManager;
        private readonly GenerationConfig _config;
        private readonly Faker _faker;
        private readonly Random _random;
        private readonly StringValueGenerator _stringGenerator;
        private readonly NumberValueGenerator _numberGenerator;
        private readonly DateValueGenerator _dateGenerator;
        private readonly BooleanValueGenerator _boolGenerator;
        private readonly ToxicologyValueGenerator _toxicologyGenerator;
        private readonly ToxicologyClassifier _toxicologyClassifier;
        private readonly SubstanceGridGenerator _substanceGridGenerator;
        private readonly DataRelationshipCoordinator _relationshipCoordinator;
        private readonly EnhancedListValueGenerator _enhancedListGenerator;

        public CaseDataGenerator(MetadataManager metadataManager, GenerationConfig config)
        {
            _metadataManager = metadataManager;
            _config = config;
            _random = config.RandomSeed.HasValue ? new Random(config.RandomSeed.Value) : new Random();
            _faker = new Faker("en_US") { Random = new Bogus.Randomizer(_random.Next()) };
            
            _stringGenerator = new StringValueGenerator(_faker, config.Strategy, _random);
            _numberGenerator = new NumberValueGenerator(_faker, config.Strategy, _random);
            _dateGenerator = new DateValueGenerator(_faker, config.Strategy, _random);
            _boolGenerator = new BooleanValueGenerator(_faker, config.Strategy, _random, metadataManager);
            _toxicologyClassifier = new ToxicologyClassifier();
            _toxicologyGenerator = new ToxicologyValueGenerator(_faker, config.Strategy, _random, _toxicologyClassifier, metadataManager);
            _substanceGridGenerator = new SubstanceGridGenerator(_faker, config.Strategy, _random, metadataManager);
            _relationshipCoordinator = new DataRelationshipCoordinator(_toxicologyClassifier, _random);
            _enhancedListGenerator = new EnhancedListValueGenerator(_faker, config.Strategy, _random, 
                config.DemographicWeights ?? new DemographicWeights());
        }

        public Dictionary<string, object?> GenerateCase(int caseNumber)
        {
            var recordId = $"{_config.Jurisdiction}-{caseNumber:D6}";
            var caseData = new Dictionary<string, object?>
            {
                ["_id"] = Guid.NewGuid().ToString(),
                ["date_created"] = DateTime.UtcNow.ToString("o"),
                ["date_last_updated"] = DateTime.UtcNow.ToString("o"),
                ["created_by"] = _config.CreatedBy,
                ["last_updated_by"] = _config.LastUpdatedBy,
                ["jurisdiction_id"] = _config.Jurisdiction,
                ["agency_case_id"] = "",
                ["case_status"] = "in-progress",
                ["version"] = _config.MetadataVersion,
                ["data_migration_history"] = new List<object>(),
                ["host_state"] = _config.GetHostState(),
                ["addquarter"] = _config.GetAddQuarter(),
                ["cmpquarter"] = "",
                ["date_last_checked_out"] = "",
                ["last_checked_out_by"] = "",
                ["is_offline"] = "false",
                ["offline_date"] = "",
                ["offline_by"] = ""
            };

            // Generate form data based on metadata
            var forms = _metadataManager.GetForms();
            foreach (var form in forms)
            {
                var formData = GenerateFormData(form);
                if (formData != null)
                {
                    caseData[form.Node.name] = formData;
                }
            }

            // Post-process to add calculated fields
            PostProcessHomeRecord(caseData, recordId);
            PostProcessBirthCertificateParent(caseData);
            PostProcessCVS(caseData);
            PostProcessTAMU(caseData);
            PostProcessDateOfBirth(caseData);
            PostProcessUSAddresses(caseData);
            
            // Phase 1: Add new forms and enhanced data
            PostProcessAutopsyForm(caseData);
            PostProcessPrenatalRecordsForm(caseData);
            PostProcessSocialEnvironmentalProfileForm(caseData);
            PostProcessEducationField(caseData);
            PostProcessContributingFactors(caseData);
            
            // Phase 2B: Coordinate field relationships for realistic correlations
            _relationshipCoordinator.CoordinateCase(caseData);

            return caseData;
        }

        private object? GenerateFormData(MetadataNode formNode)
        {
            if (formNode.IsMultiform)
            {
                // Generate multiple instances
                var instances = new List<Dictionary<string, object?>>();
                var instanceCount = _random.Next(
                    _config.Strategy.MultiformInstancesMin,
                    _config.Strategy.MultiformInstancesMax + 1
                );

                for (int i = 0; i < instanceCount; i++)
                {
                    var instance = new Dictionary<string, object?>();
                    if (formNode.Node.children != null)
                    {
                        foreach (var child in formNode.Node.children)
                        {
                            var childPath = $"{formNode.Path}/{child.name}";
                            if (_metadataManager.NodeDictionary.TryGetValue(childPath, out var childNode))
                            {
                                var value = GenerateNodeData(childNode);
                                instance[child.name] = value;
                            }
                        }
                    }
                    instances.Add(instance);
                }
                return instances;
            }
            else
            {
                // Generate single instance
                var formData = new Dictionary<string, object?>();
                if (formNode.Node.children != null)
                {
                    foreach (var child in formNode.Node.children)
                    {
                        var childPath = $"{formNode.Path}/{child.name}";
                        if (_metadataManager.NodeDictionary.TryGetValue(childPath, out var childNode))
                        {
                            var value = GenerateNodeData(childNode);
                            formData[child.name] = value;
                        }
                    }
                }
                return formData;
            }
        }

        private object? GenerateNodeData(MetadataNode metadataNode)
        {
            var node = metadataNode.Node;
            var isRequired = node.is_required.HasValue && node.is_required.Value;

            // Normalize type to lowercase to handle metadata inconsistencies (e.g., "List" vs "list")
            var nodeType = node.type?.ToLowerInvariant() ?? "";

            // **Business Rule**: Always generate feminine values for sex/gender fields
            // death_certificate/demographics/sex and birth_fetal_death_certificate_infant_fetal_section/biometrics_and_demographics/gender
            // Value "2" = Female
            if (nodeType == "list" && (metadataNode.Path == "death_certificate/demographics/sex" || 
                                         metadataNode.Path == "birth_fetal_death_certificate_infant_fetal_section/biometrics_and_demographics/gender"))
            {
                return "2"; // Female
            }

            // **Business Rule**: Always generate feminine names for first_name and middle_name
            if (nodeType == "string" && (node.name.ToLower() == "first_name" || node.name.ToLower() == "middle_name"))
            {
                return node.name.ToLower() == "first_name" ? _faker.Name.FirstName(Bogus.DataSets.Name.Gender.Female) 
                                                              : _faker.Name.FirstName(Bogus.DataSets.Name.Gender.Female);
            }

            return nodeType switch
            {
                "string" => _stringGenerator.Generate(node.name, isRequired),
                "number" => _numberGenerator.Generate(node.name, isRequired),
                "date" => _dateGenerator.GenerateDateString(node.name, isRequired),
                "datetime" => _dateGenerator.GenerateDateTimeString(node.name, isRequired),
                "time" => _dateGenerator.GenerateTimeString(node.name, isRequired),
                "boolean" => _boolGenerator.Generate(node.name, isRequired),
                "list" => GenerateListValue(metadataNode, isRequired),
                "grid" => GenerateGridData(metadataNode),
                "textarea" => _stringGenerator.Generate(node.name, isRequired),
                "group" => GenerateGroupData(metadataNode),
                _ => "" // Unknown types default to empty string per MMRIA convention
            };
        }

        private object? GenerateGroupData(MetadataNode groupNode)
        {
            var groupData = new Dictionary<string, object?>();
            if (groupNode.Node.children != null)
            {
                // Check if this is a date group (has month, day, year children)
                var isDateGroup = IsDateGroup(groupNode);
                
                if (isDateGroup)
                {
                    // Generate date components using DateValueGenerator
                    var dateComponents = GenerateDateComponents(groupNode.Node.name);
                    foreach (var kvp in dateComponents)
                    {
                        groupData[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // Process children normally
                    foreach (var child in groupNode.Node.children)
                    {
                        var childPath = $"{groupNode.Path}/{child.name}";
                        if (_metadataManager.NodeDictionary.TryGetValue(childPath, out var childNode))
                        {
                            var value = GenerateNodeData(childNode);
                            groupData[child.name] = value;
                        }
                    }
                }
            }
            return groupData; // Always return dictionary, even if empty
        }

        private object? GenerateListValue(MetadataNode metadataNode, bool isRequired)
        {
            var node = metadataNode.Node;
            
            // Special handling for date component fields (month, day, year)
            // These should never be "9999" - they should be actual values
            if (IsDateComponentField(node.name))
            {
                return GenerateDateComponentValue(node.name);
            }
            
            // Check is_multiselect first to determine proper default
            var isMultiSelect = metadataNode.IsMultiSelect || 
                              node.control_style == "multi-select" || 
                              node.control_style == "checkbox";
            
            var isNumericList = metadataNode.ListItemDataType == "number" || 
                              metadataNode.ListItemDataType == "integer";
            
            // If no values available, return proper default per MMRIA convention
            if (metadataNode.ValueToDisplay.Count == 0)
            {
                return isMultiSelect ? (isNumericList ? (object)new List<double>() : new List<string>()) : "9999";
            }

            if (isMultiSelect)
            {
                // For multi-select, only return empty array 10% of the time for non-required fields
                // This ensures better test data coverage while still allowing some empty cases
                if (!isRequired && _random.Next(10) == 0)
                {
                    return isNumericList ? new List<double>() : new List<string>();
                }

                // Generate 2-4 random selections for better test coverage
                // Use minimum of 2 to ensure multi-select fields have multiple values
                var minSelections = Math.Min(2, metadataNode.ValueToDisplay.Count);
                var maxSelections = Math.Min(4, metadataNode.ValueToDisplay.Count);
                var count = _random.Next(minSelections, maxSelections + 1);
                var selectedValues = metadataNode.ValueToDisplay.Keys
                    .OrderBy(x => _random.Next())
                    .Take(count)
                    .ToList();

                // Convert to numeric list if needed
                if (isNumericList)
                {
                    var numericValues = new List<double>();
                    foreach (var val in selectedValues)
                    {
                        if (double.TryParse(val, out var numVal))
                        {
                            numericValues.Add(numVal);
                        }
                    }
                    return numericValues;
                }
                
                return selectedValues;
            }
            else
            {
                // Single selection
                var values = metadataNode.ValueToDisplay.Keys.ToList();
                return values[_random.Next(values.Count)];
            }
        }

        private List<Dictionary<string, object?>>? GenerateGridData(MetadataNode gridNode)
        {
            var rowCount = _random.Next(
                _config.Strategy.GridRowsMin,
                _config.Strategy.GridRowsMax + 1
            );

            if (rowCount == 0) return null;

            var rows = new List<Dictionary<string, object?>>();
            for (int i = 0; i < rowCount; i++)
            {
                var row = new Dictionary<string, object?>();
                if (gridNode.Node.children != null)
                {
                    foreach (var child in gridNode.Node.children)
                    {
                        var childPath = $"{gridNode.Path}/{child.name}";
                        if (_metadataManager.NodeDictionary.TryGetValue(childPath, out var childNode))
                        {
                            var value = GenerateNodeData(childNode);
                            // Always add property to grid row, even if null
                            // This prevents undefined errors in JavaScript sorting
                            row[child.name] = value;
                        }
                    }
                }
                rows.Add(row);
            }

            return rows;
        }

        public List<Dictionary<string, object?>> GenerateCases()
        {
            Console.WriteLine($"\nGenerating {_config.CaseCount} cases with '{_config.Strategy.Name}' strategy...");
            
            var cases = new List<Dictionary<string, object?>>();
            for (int i = 1; i <= _config.CaseCount; i++)
            {
                var caseData = GenerateCase(i);
                cases.Add(caseData);
                
                if (i % 10 == 0 || i == _config.CaseCount)
                {
                    Console.WriteLine($"  Generated {i}/{_config.CaseCount} cases");
                }
            }

            Console.WriteLine($"✓ Generated {cases.Count} cases");
            
            return cases;
        }

        private bool IsDateGroup(MetadataNode groupNode)
        {
            // A date group has month, day, and year children
            if (groupNode.Node.children == null) return false;
            
            var childNames = groupNode.Node.children.Select(c => c.name.ToLower()).ToHashSet();
            return childNames.Contains("month") && childNames.Contains("year");
        }

        private bool IsDateComponentField(string fieldName)
        {
            var lower = fieldName.ToLower();
            return lower == "month" || lower == "day" || lower == "year";
        }

        private Dictionary<string, object?> GenerateDateComponents(string parentFieldName)
        {
            var components = new Dictionary<string, object?>();
            var date = _dateGenerator.GenerateDate(parentFieldName, false);
            
            if (date.HasValue && date.Value != DateTime.MinValue)
            {
                components["month"] = date.Value.Month.ToString();
                components["day"] = date.Value.Day.ToString();
                components["year"] = date.Value.Year.ToString();
            }
            else
            {
                // Empty date - use MMRIA convention of "9999" for unspecified
                components["month"] = "9999";
                components["day"] = "9999";
                components["year"] = "9999";
            }
            
            return components;
        }

        private string GenerateDateComponentValue(string componentName)
        {
            var lower = componentName.ToLower();
            
            // Generate a random date for context
            var date = _dateGenerator.GenerateDate(componentName, false);
            
            if (!date.HasValue || date.Value == DateTime.MinValue)
            {
                return "9999"; // Unspecified
            }
            
            return lower switch
            {
                "month" => date.Value.Month.ToString(),
                "day" => date.Value.Day.ToString(),
                "year" => date.Value.Year.ToString(),
                _ => "9999"
            };
        }

        private void PostProcessHomeRecord(Dictionary<string, object?> caseData, string recordId)
        {
            // Add record_id and date_of_death to home_record
            if (caseData.TryGetValue("home_record", out var homeRecordObj) && homeRecordObj is Dictionary<string, object?> homeRecord)
            {
                // Add record_id
                homeRecord["record_id"] = recordId;

                // Add jurisdiction_id (different from top-level jurisdiction_id)
                homeRecord["jurisdiction_id"] = _config.JurisdictionId;

                // Generate a random date of death in the past 5 years
                var startDate = DateTime.UtcNow.AddYears(-5);
                var endDate = DateTime.UtcNow;
                var range = (endDate - startDate).Days;
                var randomDays = _random.Next(range);
                var dateOfDeath = startDate.AddDays(randomDays);

                homeRecord["date_of_death"] = new Dictionary<string, object?>
                {
                    ["month"] = dateOfDeath.Month.ToString(),
                    ["day"] = dateOfDeath.Day.ToString(),
                    ["year"] = dateOfDeath.Year.ToString()
                };

                // Add case_status
                // Randomize overall_case_status to 1, 2, or 3
                // 1 = Abstracting (Incomplete), 2 = Abstraction Complete, 3 = Ready for Review
                homeRecord["case_status"] = new Dictionary<string, object?>
                {
                    ["overall_case_status"] = _random.Next(3) + 1, // Randomize to 1, 2, or 3
                    ["abstraction_begin_date"] = "",
                    ["abstraction_complete_date"] = "",
                    ["projected_review_date"] = "",
                    ["committee_review_date"] = "" // Clear - only set after committee review (status 4+)
                };

                // Add case_progress_report with randomized 0/1 values for each field
                // These fields track whether specific sections/documents have been completed
                homeRecord["case_progress_report"] = new Dictionary<string, object?>
                {
                    ["death_certificate"] = _random.Next(2), // 0 or 1
                    ["autopsy_report"] = _random.Next(2),
                    ["prenatal"] = _random.Next(2),
                    ["er_visits_and_hospitalizations"] = _random.Next(2),
                    ["other_medical_office_visits"] = _random.Next(2),
                    ["medical_transport"] = _random.Next(2),
                    ["social_and_environmental_profile"] = _random.Next(2),
                    ["mental_health_profile"] = _random.Next(2),
                    ["informant_interviews"] = _random.Next(2),
                    ["birth_certificate_infant_fetal_section"] = _random.Next(2),
                    ["birth_certificate_parent_section"] = _random.Next(2),
                    ["committee_review"] = _random.Next(2),
                    ["case_narrative"] = _random.Next(2),
                    ["informant_interviews_blank_status"] = 9999
                };

                // Add automated_vitals_group - fields set by CDC STEVE-NAPHSIS vitals import API
                // Set to empty/default values since these are populated by external API
                homeRecord["automated_vitals_group"] = new Dictionary<string, object?>
                {
                    ["vital_report"] = "",
                    ["vro_status"] = "9999", // Default blank value
                    ["import_date"] = "",
                    ["bc_det_match"] = 9999, // Default blank value for vital_yes_no_not_applicable lookup
                    ["fdc_det_match"] = 9999,
                    ["bc_prob_match"] = 9999,
                    ["fdc_prob_match"] = 9999,
                    ["icd10_match"] = 9999,
                    ["pregcb_match"] = 9999,
                    ["literalcod_match"] = 9999,
                    ["hr_cdc_other"] = 9999
                };
            }
        }

        private void PostProcessCVS(Dictionary<string, object?> caseData)
        {
            // Process CVS form to clear API-populated fields in cvs_grid
            if (caseData.TryGetValue("cvs", out var cvsFormObj) && cvsFormObj is Dictionary<string, object?> cvsForm)
            {
                // Clear cvs_grid if it exists - these fields are populated by CVS API
                if (cvsForm.TryGetValue("cvs_grid", out var gridObj) && gridObj is List<Dictionary<string, object?>> grid)
                {
                    foreach (var row in grid)
                    {
                        // Set all CVS API request fields to empty
                        // These are populated by the Community Vital Signs API
                        if (row.ContainsKey("cvs_api_request_url")) row["cvs_api_request_url"] = "";
                        if (row.ContainsKey("cvs_api_request_date_time")) row["cvs_api_request_date_time"] = "";
                        if (row.ContainsKey("cvs_api_request_c_geoid")) row["cvs_api_request_c_geoid"] = "";
                        if (row.ContainsKey("cvs_api_request_t_geoid")) row["cvs_api_request_t_geoid"] = "";
                        if (row.ContainsKey("cvs_api_request_year")) row["cvs_api_request_year"] = "";
                        if (row.ContainsKey("cvs_api_request_result_message")) row["cvs_api_request_result_message"] = "";
                        
                        // Set all CVS metric fields to empty
                        // These are populated by the Community Vital Signs API
                        var cvsMetricFields = new[] {
                            "cvs_mdrate_county", "cvs_pctnoins_fem_tract", "cvs_pctnovehicle_county", 
                            "cvs_pctnovehicle_tract", "cvs_pctmove_tract", "cvs_pctsphh_tract",
                            "cvs_pctovercrowdhh_tract", "cvs_pctowner_occ_tract", "cvs_pct_less_well_tract",
                            "cvs_ndi_raw_tract", "cvs_pctpov_tract", "cvs_ice_income_all_tract",
                            "cvs_pctobese_county", "cvs_fi_county", "cvs_cnmrate_county", "cvs_obgynrate_county",
                            "cvs_rtteenbirth_county", "cvs_rtstd_county", "cvs_rtdrugodmortality_county",
                            "cvs_rtsocassoc_county", "cvs_pcthouse_distress_county", "cvs_rtviolentcr_icpsr_county",
                            "cvs_isolation_county", "cvs_pctrural", "cvs_racialized_pov", "cvs_mhproviderrate",
                            "cvs_rtmhpract_county"
                        };
                        
                        foreach (var field in cvsMetricFields)
                        {
                            if (row.ContainsKey(field)) row[field] = "";
                        }
                    }
                }
            }
        }

        private void PostProcessTAMU(Dictionary<string, object?> caseData)
        {
            // Clear all TAMU (Texas A&M Geocoding) fields - these are populated by external geocoding API
            // See docs/ai/TAMU_Geocoding_Context.md for complete documentation

            // Helper method to clear geocode fields in a dictionary
            void ClearGeocodeFields(Dictionary<string, object?> dict)
            {
                if (dict.ContainsKey("latitude")) dict["latitude"] = "";
                if (dict.ContainsKey("longitude")) dict["longitude"] = "";
                if (dict.ContainsKey("feature_matching_result_type")) dict["feature_matching_result_type"] = "";
                if (dict.ContainsKey("feature_matching_geography_type")) dict["feature_matching_geography_type"] = "";
                if (dict.ContainsKey("naaccr_gis_coordinate_quality_code")) dict["naaccr_gis_coordinate_quality_code"] = "";
                if (dict.ContainsKey("naaccr_gis_coordinate_quality_type")) dict["naaccr_gis_coordinate_quality_type"] = "";
                if (dict.ContainsKey("naaccr_census_tract_certainty_code")) dict["naaccr_census_tract_certainty_code"] = "";
                if (dict.ContainsKey("naaccr_census_tract_certainty_type")) dict["naaccr_census_tract_certainty_type"] = "";
                if (dict.ContainsKey("census_state_fips")) dict["census_state_fips"] = "";
                if (dict.ContainsKey("census_county_fips")) dict["census_county_fips"] = "";
                if (dict.ContainsKey("census_tract_fips")) dict["census_tract_fips"] = "";
                if (dict.ContainsKey("census_cbsa_fips")) dict["census_cbsa_fips"] = "";
                if (dict.ContainsKey("census_cbsa_micro")) dict["census_cbsa_micro"] = "";
                if (dict.ContainsKey("census_met_div_fips")) dict["census_met_div_fips"] = "";
                if (dict.ContainsKey("urban_status")) dict["urban_status"] = "";
                if (dict.ContainsKey("state_county_fips")) dict["state_county_fips"] = "";
            }

            // 1. Death Certificate - Place of Last Residence (16 fields)
            if (caseData.TryGetValue("death_certificate", out var dcObj) && dcObj is Dictionary<string, object?> dc)
            {
                if (dc.TryGetValue("place_of_last_residence", out var plrObj) && plrObj is Dictionary<string, object?> plr)
                {
                    ClearGeocodeFields(plr);
                }

                // 2. Death Certificate - Address of Injury (16 fields)
                if (dc.TryGetValue("address_of_injury", out var aoiObj) && aoiObj is Dictionary<string, object?> aoi)
                {
                    ClearGeocodeFields(aoi);
                }

                // 3. Death Certificate - Address of Death (16 fields)
                if (dc.TryGetValue("address_of_death", out var aodObj) && aodObj is Dictionary<string, object?> aod)
                {
                    ClearGeocodeFields(aod);
                }
            }

            // 4. Birth Certificate - Facility of Delivery Location (16 fields)
            if (caseData.TryGetValue("birth_fetal_death_certificate_parent", out var bfdcpObj) && bfdcpObj is Dictionary<string, object?> bfdcp)
            {
                if (bfdcp.TryGetValue("facility_of_delivery_location", out var fodlObj) && fodlObj is Dictionary<string, object?> fodl)
                {
                    ClearGeocodeFields(fodl);
                }

                // 5. Birth Certificate - Location of Residence (16 fields)
                if (bfdcp.TryGetValue("location_of_residence", out var lorObj) && lorObj is Dictionary<string, object?> lor)
                {
                    ClearGeocodeFields(lor);
                }
            }

            // 6. Prenatal - Location of Primary Prenatal Care Facility (16 fields)
            if (caseData.TryGetValue("prenatal", out var prenatalObj) && prenatalObj is Dictionary<string, object?> prenatal)
            {
                if (prenatal.TryGetValue("location_of_primary_prenatal_care_facility", out var loppcfObj) && loppcfObj is Dictionary<string, object?> loppcf)
                {
                    ClearGeocodeFields(loppcf);
                }
            }

            // 7. ER Visit/Hospital Medical Records Grid (16 fields per item)
            if (caseData.TryGetValue("er_visit_and_hospital_medical_records", out var erhObj) && erhObj is List<Dictionary<string, object?>> erhList)
            {
                foreach (var erhItem in erhList)
                {
                    if (erhItem.TryGetValue("name_and_location_facility", out var nalfObj) && nalfObj is Dictionary<string, object?> nalf)
                    {
                        ClearGeocodeFields(nalf);
                    }
                }
            }

            // 8. Other Medical Office Visits Grid (16 fields per item)
            if (caseData.TryGetValue("other_medical_office_visits", out var omovObj) && omovObj is List<Dictionary<string, object?>> omovList)
            {
                foreach (var omovItem in omovList)
                {
                    if (omovItem.TryGetValue("location_of_medical_care_facility", out var lomcfObj) && lomcfObj is Dictionary<string, object?> lomcf)
                    {
                        ClearGeocodeFields(lomcf);
                    }
                }
            }

            // 9 & 10. Medical Transport Grid - Origin and Destination Addresses (16 fields per item, 2 addresses per item)
            if (caseData.TryGetValue("medical_transport", out var mtObj) && mtObj is List<Dictionary<string, object?>> mtList)
            {
                foreach (var mtItem in mtList)
                {
                    // Origin address
                    if (mtItem.TryGetValue("origin_information", out var originObj) && originObj is Dictionary<string, object?> origin)
                    {
                        if (origin.TryGetValue("address", out var originAddrObj) && originAddrObj is Dictionary<string, object?> originAddr)
                        {
                            ClearGeocodeFields(originAddr);
                        }
                    }

                    // Destination address
                    if (mtItem.TryGetValue("destination_information", out var destObj) && destObj is Dictionary<string, object?> dest)
                    {
                        if (dest.TryGetValue("address", out var destAddrObj) && destAddrObj is Dictionary<string, object?> destAddr)
                        {
                            ClearGeocodeFields(destAddr);
                        }
                    }
                }
            }
        }

        private void PostProcessDateOfBirth(Dictionary<string, object?> caseData)
        {
            // Ensure realistic date of birth values for parents and sync death certificate DOB with mother's DOB
            var dateOfDeath = DateTime.UtcNow.AddYears(-2); // Default to 2 years ago
            
            // Get date of death from home_record if available
            if (caseData.TryGetValue("home_record", out var homeRecordObj) && homeRecordObj is Dictionary<string, object?> homeRecord)
            {
                if (homeRecord.TryGetValue("date_of_death", out var dodObj) && dodObj is Dictionary<string, object?> dod)
                {
                    if (dod.TryGetValue("year", out var yearObj) && int.TryParse(yearObj?.ToString(), out var year) &&
                        dod.TryGetValue("month", out var monthObj) && int.TryParse(monthObj?.ToString(), out var month) &&
                        dod.TryGetValue("day", out var dayObj) && int.TryParse(dayObj?.ToString(), out var day))
                    {
                        try
                        {
                            dateOfDeath = new DateTime(year, month, day);
                        }
                        catch
                        {
                            // If invalid date, use default
                        }
                    }
                }
            }

            // Process birth_fetal_death_certificate_parent to set realistic parent dates of birth
            if (caseData.TryGetValue("birth_fetal_death_certificate_parent", out var parentFormObj) && parentFormObj is Dictionary<string, object?> parentForm)
            {
                // Mother's demographics - generate realistic DOB (18-45 years before death)
                if (parentForm.TryGetValue("demographic_of_mother", out var motherDemoObj) && motherDemoObj is Dictionary<string, object?> motherDemo)
                {
                    var motherAge = _random.Next(18, 46); // Realistic childbearing age
                    var motherDOB = dateOfDeath.AddYears(-motherAge);
                    
                    if (!motherDemo.ContainsKey("date_of_birth") || motherDemo["date_of_birth"] == null)
                    {
                        motherDemo["date_of_birth"] = new Dictionary<string, object?>();
                    }
                    
                    if (motherDemo["date_of_birth"] is Dictionary<string, object?> motherDOBDict)
                    {
                        motherDOBDict["month"] = motherDOB.Month;
                        motherDOBDict["day"] = motherDOB.Day;
                        motherDOBDict["year"] = motherDOB.Year;
                    }
                    
                    // Update age to match
                    motherDemo["age"] = motherAge;
                    
                    // **Key Relationship**: Sync death certificate DOB with mother's DOB
                    // The deceased is the mother, so her DOB on death cert should match
                    if (caseData.TryGetValue("death_certificate", out var dcFormObj) && dcFormObj is Dictionary<string, object?> dcForm)
                    {
                        if (dcForm.TryGetValue("demographics", out var dcDemoObj) && dcDemoObj is Dictionary<string, object?> dcDemo)
                        {
                            if (!dcDemo.ContainsKey("date_of_birth") || dcDemo["date_of_birth"] == null)
                            {
                                dcDemo["date_of_birth"] = new Dictionary<string, object?>();
                            }
                            
                            if (dcDemo["date_of_birth"] is Dictionary<string, object?> dcDOBDict)
                            {
                                dcDOBDict["month"] = motherDOB.Month;
                                dcDOBDict["day"] = motherDOB.Day;
                                dcDOBDict["year"] = motherDOB.Year;
                            }
                            
                            // Update age to match
                            dcDemo["age"] = motherAge;
                        }
                    }
                }
                
                // Father's demographics - generate realistic DOB (18-65 years before death)
                if (parentForm.TryGetValue("demographic_of_father", out var fatherDemoObj) && fatherDemoObj is Dictionary<string, object?> fatherDemo)
                {
                    var fatherAge = _random.Next(18, 66); // Realistic fatherhood age range
                    var fatherDOB = dateOfDeath.AddYears(-fatherAge);
                    
                    if (!fatherDemo.ContainsKey("date_of_birth") || fatherDemo["date_of_birth"] == null)
                    {
                        fatherDemo["date_of_birth"] = new Dictionary<string, object?>();
                    }
                    
                    if (fatherDemo["date_of_birth"] is Dictionary<string, object?> fatherDOBDict)
                    {
                        fatherDOBDict["month"] = fatherDOB.Month;
                        // Father DOB often doesn't have day in metadata
                        if (fatherDOBDict.ContainsKey("day"))
                        {
                            fatherDOBDict["day"] = fatherDOB.Day;
                        }
                        fatherDOBDict["year"] = fatherDOB.Year;
                    }
                    
                    // Update age to match
                    fatherDemo["age"] = fatherAge;
                }
            }
        }

        private void PostProcessUSAddresses(Dictionary<string, object?> caseData)
        {
            // Ensure all country fields use US or US territories only
            // US Territories: Puerto Rico (PR), Guam (GU), US Virgin Islands (VI), 
            // American Samoa (AS), Northern Mariana Islands (MP)
            
            var usAndTerritories = new[] { "US", "PR", "GU", "VI", "AS", "MP" };
            
            void SetUSCountry(Dictionary<string, object?> dict, string fieldName)
            {
                if (dict.ContainsKey(fieldName))
                {
                    // 90% US, 10% territories
                    dict[fieldName] = _random.Next(100) < 90 ? "US" : usAndTerritories[_random.Next(1, usAndTerritories.Length)];
                }
            }
            
            // Death Certificate
            if (caseData.TryGetValue("death_certificate", out var dcObj) && dcObj is Dictionary<string, object?> dc)
            {
                if (dc.TryGetValue("demographics", out var demoObj) && demoObj is Dictionary<string, object?> demo)
                {
                    SetUSCountry(demo, "country_of_birth");
                }
                
                if (dc.TryGetValue("place_of_last_residence", out var plrObj) && plrObj is Dictionary<string, object?> plr)
                {
                    SetUSCountry(plr, "country_of_last_residence");
                }
            }
            
            // Birth Certificate Parent
            if (caseData.TryGetValue("birth_fetal_death_certificate_parent", out var bfdcpObj) && bfdcpObj is Dictionary<string, object?> bfdcp)
            {
                if (bfdcp.TryGetValue("demographic_of_mother", out var motherObj) && motherObj is Dictionary<string, object?> mother)
                {
                    SetUSCountry(mother, "country_of_birth");
                }
                
                if (bfdcp.TryGetValue("demographic_of_father", out var fatherObj) && fatherObj is Dictionary<string, object?> father)
                {
                    SetUSCountry(father, "father_country_of_birth");
                }
                
                if (bfdcp.TryGetValue("location_of_residence", out var lorObj) && lorObj is Dictionary<string, object?> lor)
                {
                    SetUSCountry(lor, "country_of_last_residence");
                }
            }
            
            // Birth Certificate Infant/Fetal Section (multi-form)
            if (caseData.TryGetValue("birth_certificate_infant_fetal_section", out var bcifsObj) && bcifsObj is List<Dictionary<string, object?>> bcifsList)
            {
                foreach (var bcifs in bcifsList)
                {
                    if (bcifs.TryGetValue("causes_of_death", out var codGroupObj) && codGroupObj is Dictionary<string, object?> codGroup)
                    {
                        SetUSCountry(codGroup, "country_of_birth");
                    }
                }
            }
            
            // ER Visit and Hospital Medical Records (grid)
            if (caseData.TryGetValue("er_visit_and_hospital_medical_records", out var erhObj) && erhObj is List<Dictionary<string, object?>> erhList)
            {
                foreach (var erhItem in erhList)
                {
                    if (erhItem.TryGetValue("name_and_location_facility", out var nalfObj) && nalfObj is Dictionary<string, object?> nalf)
                    {
                        SetUSCountry(nalf, "country");
                    }
                }
            }
            
            // Other Medical Office Visits (grid)
            if (caseData.TryGetValue("other_medical_office_visits", out var omovObj) && omovObj is List<Dictionary<string, object?>> omovList)
            {
                foreach (var omovItem in omovList)
                {
                    if (omovItem.TryGetValue("location_of_medical_care_facility", out var lomcfObj) && lomcfObj is Dictionary<string, object?> lomcf)
                    {
                        SetUSCountry(lomcf, "country");
                    }
                }
            }
            
            // Medical Transport (grid) - Origin and Destination
            if (caseData.TryGetValue("medical_transport", out var mtObj) && mtObj is List<Dictionary<string, object?>> mtList)
            {
                foreach (var mtItem in mtList)
                {
                    if (mtItem.TryGetValue("origin_information", out var originObj) && originObj is Dictionary<string, object?> origin)
                    {
                        if (origin.TryGetValue("address", out var originAddrObj) && originAddrObj is Dictionary<string, object?> originAddr)
                        {
                            SetUSCountry(originAddr, "country");
                        }
                    }
                    
                    if (mtItem.TryGetValue("destination_information", out var destObj) && destObj is Dictionary<string, object?> dest)
                    {
                        if (dest.TryGetValue("address", out var destAddrObj) && destAddrObj is Dictionary<string, object?> destAddr)
                        {
                            SetUSCountry(destAddr, "country");
                        }
                    }
                }
            }
        }

        private void PostProcessBirthCertificateParent(Dictionary<string, object?> caseData)
        {
            // Apply BMI and weight gain calculations to birth certificate parent form
            if (caseData.TryGetValue("birth_fetal_death_certificate_parent", out var parentForm) && parentForm is Dictionary<string, object?> parentDict)
            {
                if (parentDict.TryGetValue("maternal_biometrics", out var biometricsObj) && biometricsObj is Dictionary<string, object?> biometrics)
                {
                    // Calculate BMI if height and weight at delivery are present
                    if (biometrics.TryGetValue("height_feet", out var hftObj) && 
                        biometrics.TryGetValue("height_inches", out var hinObj) &&
                        biometrics.TryGetValue("weight_at_delivery", out var dwgtObj))
                    {
                        var bmi = CalculateBMI(hftObj, hinObj, dwgtObj);
                        if (bmi.HasValue)
                        {
                            biometrics["bmi"] = bmi.Value;
                        }
                    }

                    // Calculate weight gain if both weights are present
                    if (biometrics.TryGetValue("weight_at_delivery", out var delWgtObj) &&
                        biometrics.TryGetValue("pre_pregnancy_weight", out var ppWgtObj))
                    {
                        var weightGain = CalculateWeightGain(delWgtObj, ppWgtObj);
                        if (weightGain.HasValue)
                        {
                            biometrics["weight_gain"] = weightGain.Value;
                        }
                    }
                }
            }
        }

        private static double? CalculateBMI(object? heightFeetObj, object? heightInchesObj, object? weightPoundsObj)
        {
            // Parse values
            if (!TryParseDouble(heightFeetObj, out var heightFeet) ||
                !TryParseDouble(heightInchesObj, out var heightInches) ||
                !TryParseDouble(weightPoundsObj, out var weightPounds))
            {
                return null;
            }

            // Calculate total height in inches
            var totalHeightInches = (heightFeet * 12) + heightInches;

            // Validate ranges (24-108 inches, 50-800 pounds)
            if (totalHeightInches < 24 || totalHeightInches > 108 || 
                weightPounds < 50 || weightPounds > 800)
            {
                return null;
            }

            // Convert to metric
            var heightMeters = totalHeightInches / 39.3700787;
            var weightKg = weightPounds / 2.20462;

            // Calculate BMI: kg / m²
            var bmi = weightKg / Math.Pow(heightMeters, 2);
            return Math.Round(bmi, 1);
        }

        private static double? CalculateWeightGain(object? weightDeliveryObj, object? weightPrePregnancyObj)
        {
            // Parse values
            if (!TryParseDouble(weightDeliveryObj, out var weightDelivery) ||
                !TryParseDouble(weightPrePregnancyObj, out var weightPrePregnancy))
            {
                return null;
            }

            // Validate ranges (50-800 pounds)
            if (weightDelivery < 50 || weightDelivery > 800 ||
                weightPrePregnancy < 50 || weightPrePregnancy > 800)
            {
                return null;
            }

            // Calculate weight gain
            var weightGain = weightDelivery - weightPrePregnancy;
            return Math.Round(weightGain, 1);
        }

        private static bool TryParseDouble(object? value, out double result)
        {
            result = 0;
            if (value == null) return false;

            if (value is double d)
            {
                result = d;
                return true;
            }

            if (value is int i)
            {
                result = i;
                return true;
            }

            if (value is string s && double.TryParse(s, out var parsed))
            {
                result = parsed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Normalize multi-select field values to ensure proper array format
        /// Handles special codes: 9999 (unspecified) -> [], 7777/8888 -> [7777.0]/[8888.0]
        /// </summary>
        private object? NormalizeMultiSelectValue(object? value, bool isNumericList)
        {
            if (value == null) return isNumericList ? new List<double>() : new List<string>();

            // Already a list - return as-is if correct type
            if (value is List<double> numList) return numList;
            if (value is List<string> strList && !isNumericList) return strList;
            if (value is List<string> strListNum && isNumericList)
            {
                var converted = new List<double>();
                foreach (var str in strListNum)
                {
                    if (double.TryParse(str, out var num))
                        converted.Add(num);
                }
                return converted;
            }

            // Handle string values
            if (value is string strValue)
            {
                // Special codes for "unspecified" should be empty arrays
                if (strValue == "9999" || strValue == "")
                {
                    return isNumericList ? new List<double>() : new List<string>();
                }

                // Special codes like 7777 (None) or 8888 (Not applicable) should be single-item arrays
                if (isNumericList && double.TryParse(strValue, out var numVal))
                {
                    return new List<double> { numVal };
                }

                return new List<string> { strValue };
            }

            // Handle numeric values
            if (value is int intValue)
            {
                if (intValue == 9999)
                    return isNumericList ? new List<double>() : new List<string>();
                return isNumericList ? new List<double> { intValue } : new List<string> { intValue.ToString() };
            }

            if (value is double dblValue)
            {
                if (dblValue == 9999.0)
                    return isNumericList ? new List<double>() : new List<string>();
                return isNumericList ? new List<double> { dblValue } : new List<string> { dblValue.ToString() };
            }

            // Fallback: return empty array
            return isNumericList ? new List<double>() : new List<string>();
        }

        /// <summary>
        /// Phase 1: Generate Autopsy form with toxicology results
        /// </summary>
        private void PostProcessAutopsyForm(Dictionary<string, object?> caseData)
        {
            // Update or create autopsy_report form with realistic toxicology data
            if (!caseData.ContainsKey("autopsy_report"))
            {
                caseData["autopsy_report"] = new Dictionary<string, object?>();
            }

            if (caseData["autopsy_report"] is Dictionary<string, object?> autopsyReport)
            {
                autopsyReport["_id"] = "autopsy_report";
                // Generate autopsy_performed as yes/no code using metadata lookup instead of boolean
                var performedAutopsy = _config.Strategy.CompletenessPercentage >= 70 || _random.NextDouble() < 0.8;
                autopsyReport["autopsy_performed"] = _boolGenerator.GenerateYesNo("autopsy_performed", performedAutopsy);
                autopsyReport["findings"] = _faker.Lorem.Paragraphs(1) ?? "";
                
                // Generate toxicology results with realistic concentration and unit_of_measure
                var toxicologyResults = _toxicologyGenerator.GenerateToxicologyResults();
                
                // Map from our format to the actual MMRIA format
                var mappedToxicology = new List<Dictionary<string, object?>>();
                foreach (var result in toxicologyResults)
                {
                    var mapped = new Dictionary<string, object?>
                    {
                        ["substance"] = result["substance"],
                        ["concentration"] = result["concentration"],
                        ["unit_of_measure"] = result["unit_of_measure"],
                        ["level"] = result["level"],
                        ["result"] = result.ContainsKey("result") ? result["result"] : "Positive",
                        ["substance_other"] = "",
                        ["comment"] = _faker.Lorem.Sentence()
                    };
                    mappedToxicology.Add(mapped);
                }
                
                autopsyReport["toxicology"] = mappedToxicology;
                autopsyReport["was_drug_toxicology_positive"] = mappedToxicology.Count > 0 ? "1" : "2";
                autopsyReport["cause_of_death"] = _faker.PickRandom(new[] { "Overdose", "Drug toxicity", "Poisoning" });
                autopsyReport["manner_of_death"] = _faker.PickRandom(new[] { "Accident", "Undetermined", "Suicide" });
            }
        }

        /// <summary>
        /// Phase 1: Generate Prenatal Records form with substance use evidence
        /// </summary>
        private void PostProcessPrenatalRecordsForm(Dictionary<string, object?> caseData)
        {
            var substanceUseEvidence = _boolGenerator.GenerateFourState("substance_use_evidence", false);
            
            var prenatalForm = new Dictionary<string, object?>
            {
                ["_id"] = "prenatal_records",
                ["prenatal_care_received"] = _boolGenerator.GenerateYesNo("prenatal_care_received", false),
                ["substance_use_evidence"] = substanceUseEvidence,
                ["prenatal_visits"] = _random.Next(0, 15),
                ["gestational_age_at_first_visit"] = _random.Next(4, 36),
                ["first_trimester_visit"] = _boolGenerator.GenerateYesNo("first_trimester_visit", false),
                ["last_visit_date"] = _dateGenerator.GenerateDateString("last_visit_date", false),
                // Populate substance grid if evidence = "Yes" (code "1")
                ["substance_use_grid"] = IsYesCode(substanceUseEvidence) ? _substanceGridGenerator.GeneratePrenatalSubstanceGrid() : null
            };

            caseData["prenatal_records"] = prenatalForm;
        }

        /// <summary>
        /// Phase 1: Generate Social & Environmental Profile form with substance use history
        /// </summary>
        private void PostProcessSocialEnvironmentalProfileForm(Dictionary<string, object?> caseData)
        {
            var substanceUseHistory = _boolGenerator.GenerateFourState("substance_use_history", false);
            
            var socialForm = new Dictionary<string, object?>
            {
                ["_id"] = "social_environmental_profile",
                ["substance_use_history"] = substanceUseHistory,
                ["substance_type"] = IsYesCode(substanceUseHistory) ? _faker.PickRandom(new[] { "Alcohol", "Drugs", "Both" }) : null,
                ["housing_status"] = _faker.PickRandom(new[] { "Stable", "Unstable", "Homeless", "Unknown" }),
                ["employment_status"] = _faker.PickRandom(new[] { "Employed", "Unemployed", "Unknown" }),
                ["insurance_type"] = _faker.PickRandom(new[] { "Private", "Medicaid", "Medicare", "Uninsured", "Unknown" }),
                ["marital_status"] = _faker.PickRandom(new[] { "Married", "Single", "Divorced", "Unknown" }),
                ["living_environment"] = _faker.PickRandom(new[] { "Urban", "Rural", "Suburban", "Unknown" }),
                // Populate substance grid if history = "Yes" (code "1")
                ["if_yes_specify_substances"] = IsYesCode(substanceUseHistory) ? _substanceGridGenerator.GenerateSocialSubstanceGrid() : null
            };

            caseData["social_environmental_profile"] = socialForm;
        }

        /// <summary>
        /// Phase 1: Add education field to Death Certificate demographics
        /// </summary>
        private void PostProcessEducationField(Dictionary<string, object?> caseData)
        {
            if (caseData.TryGetValue("death_certificate", out var dcObj) && dcObj is Dictionary<string, object?> deathCert)
            {
                if (deathCert.TryGetValue("demographics", out var demoObj) && demoObj is Dictionary<string, object?> demographics)
                {
                    // Only add if not already present
                    if (!demographics.ContainsKey("education"))
                    {
                        var educationLevels = new[]
                        {
                            "High school diploma equivalent or less",
                            "Completed some college",
                            "Associate or bachelor degree",
                            "Completed advanced degree"
                        };

                        // Weighted distribution: ~70% populated, ~30% blank/unknown
                        if (_config.Strategy.CompletenessPercentage >= 70 || _random.NextDouble() < 0.7)
                        {
                            demographics["education"] = _faker.PickRandom(educationLevels);
                        }
                        else
                        {
                            demographics["education"] = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Phase 1: Add contributing factor fields to Committee Review
        /// Contributing factors: Preventability, Obesity, Mental Health, Substance Use Disorder, Suicide, Homicide
        /// </summary>
        private void PostProcessContributingFactors(Dictionary<string, object?> caseData)
        {
            if (caseData.TryGetValue("committee_review", out var crObj) && crObj is Dictionary<string, object?> committeeReview)
            {
                // Only add if not already present (to avoid overwriting metadata-generated values)
                if (!committeeReview.ContainsKey("was_this_death_preventable"))
                    committeeReview["was_this_death_preventable"] = _boolGenerator.GenerateFourState("was_this_death_preventable", false);

                if (!committeeReview.ContainsKey("did_obesity_contribute_to_the_death"))
                    committeeReview["did_obesity_contribute_to_the_death"] = _boolGenerator.GenerateFourState("did_obesity_contribute_to_the_death", false);

                if (!committeeReview.ContainsKey("did_mental_health_conditions_contribute_to_the_death"))
                    committeeReview["did_mental_health_conditions_contribute_to_the_death"] = _boolGenerator.GenerateFourState("did_mental_health_conditions_contribute_to_the_death", false);

                if (!committeeReview.ContainsKey("did_substance_use_disorder_contribute_to_the_death"))
                    committeeReview["did_substance_use_disorder_contribute_to_the_death"] = _boolGenerator.GenerateFourState("did_substance_use_disorder_contribute_to_the_death", false);

                if (!committeeReview.ContainsKey("was_this_death_a_sucide"))
                    committeeReview["was_this_death_a_sucide"] = _boolGenerator.GenerateFourState("was_this_death_a_sucide", false);

                // Homicide is nested under homicide_relatedness
                if (!committeeReview.ContainsKey("homicide_relatedness"))
                {
                    committeeReview["homicide_relatedness"] = new Dictionary<string, object?>
                    {
                        ["was_this_death_a_homicide"] = _boolGenerator.GenerateFourState("was_this_death_a_homicide", false)
                    };
                }
                else if (committeeReview["homicide_relatedness"] is Dictionary<string, object?> homicideDict)
                {
                    if (!homicideDict.ContainsKey("was_this_death_a_homicide"))
                        homicideDict["was_this_death_a_homicide"] = _boolGenerator.GenerateFourState("was_this_death_a_homicide", false);
                }
            }
        }

        /// <summary>
        /// Helper method to check if a value represents "Yes" from boolean generator.
        /// Handles both numeric codes (1, "1", 1.0) and legacy string format ("Yes").
        /// </summary>
        private bool IsYesCode(object? value)
        {
            if (value == null)
                return false;

            // Check for numeric code "1" (Yes)
            if (value is string strValue)
            {
                return strValue == "1" || strValue.Equals("Yes", StringComparison.OrdinalIgnoreCase);
            }

            // Check for numeric 1 or 1.0
            if (value is int intValue)
                return intValue == 1;

            if (value is double doubleValue)
                return Math.Abs(doubleValue - 1.0) < 0.001;

            return false;
        }
    }
}





