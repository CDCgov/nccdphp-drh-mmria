using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.CaseValidation.Model;

public sealed class CaseValidationRuleDocument
{
    public string _id { get; set; }
    public string _rev { get; set; }
    public string data_type { get; set; } = "case-validation-rules";
    public string metadata_version { get; set; }
    public bool enabled { get; set; } = true;
    public string date_created { get; set; }
    public string created_by { get; set; }
    public string date_last_updated { get; set; }
    public string last_updated_by { get; set; }
    public List<CaseValidationFieldRule> field_rules { get; set; } = new();
    public List<CaseValidationConnectedFieldRule> connected_field_rules { get; set; } = new();
    public List<CaseValidationFormStatusRule> form_status_rules { get; set; } = new();
}

public sealed class CaseValidationFieldRule
{
    public string id { get; set; }
    public bool enabled { get; set; } = true;
    public string category { get; set; } = "range";
    public string rule_type { get; set; }
    public string severity { get; set; } = "warning";
    public string validation_level { get; set; } = "metadata";
    public string review_status { get; set; } = "generated";
    public string source { get; set; }
    public string rationale { get; set; }
    public string unit { get; set; }
    public string form_path { get; set; }
    public string form_prompt { get; set; }
    public string field_path { get; set; }
    public string metadata_path { get; set; }
    public string prompt { get; set; }
    public string subject { get; set; }
    public string data_type { get; set; }
    public string field_type { get; set; }
    public double? min_value { get; set; }
    public double? max_value { get; set; }
    public int? max_length { get; set; }
    public string regex_pattern { get; set; }
    public List<string> allowed_values { get; set; } = new();
    public string message { get; set; }
    public bool editable { get; set; } = true;
}

public sealed class CaseValidationConnectedFieldRule
{
    public string id { get; set; }
    public bool enabled { get; set; } = true;
    public string category { get; set; } = "connected-field";
    public string rule_type { get; set; }
    public string severity { get; set; } = "warning";
    public string validation_level { get; set; } = "connected";
    public string review_status { get; set; } = "generated";
    public string source { get; set; }
    public string rationale { get; set; }
    public string form_path { get; set; }
    public string form_prompt { get; set; }
    public string field_path { get; set; }
    public string related_field_path { get; set; }
    public string metadata_path { get; set; }
    public string prompt { get; set; }
    public string related_prompt { get; set; }
    public string subject { get; set; }
    public string comparison { get; set; }
    public double? max_difference { get; set; }
    public string message { get; set; }
}

public sealed class CaseValidationFormStatusRule
{
    public string id { get; set; }
    public bool enabled { get; set; } = true;
    public string category { get; set; } = "form-status";
    public string severity { get; set; } = "warning";
    public string validation_level { get; set; } = "form-completeness";
    public string review_status { get; set; } = "generated";
    public string source { get; set; }
    public string rationale { get; set; }
    public string form_path { get; set; }
    public string form_prompt { get; set; }
    public string status_field_path { get; set; }
    public string status_field_prompt { get; set; }
    public int completed_min_meaningful_fields { get; set; } = 2;
    public int data_present_min_meaningful_fields { get; set; } = 1;
    public string message { get; set; }
}

public sealed class CaseValidationFlattenedField
{
    public string form_path { get; set; }
    public string form_prompt { get; set; }
    public string field_path { get; set; }
    public string metadata_path { get; set; }
    public string dictionary_path { get; set; }
    public string object_path { get; set; }
    public string prompt { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string data_type { get; set; }
    public string cardinality { get; set; }
    public string subject { get; set; }
    public string path_reference { get; set; }
    public bool is_multiform { get; set; }
    public bool is_grid { get; set; }
    public bool is_scalar { get; set; }
    public bool can_quick_edit { get; set; }
    public bool is_required { get; set; }
    public bool is_read_only { get; set; }
    public bool is_hidden { get; set; }
    public string min_value { get; set; }
    public string max_value { get; set; }
    public string max_length { get; set; }
    public string regex_pattern { get; set; }
    public string validation_description { get; set; }
    public string[] tags { get; set; } = Array.Empty<string>();
    public List<CaseValidationListValue> values { get; set; } = new();
    public List<string> ancestry { get; set; } = new();
}

public sealed class CaseValidationListValue
{
    public string value { get; set; }
    public string display { get; set; }
}

public sealed class CaseValidationFinding
{
    public string id { get; set; }
    public string category { get; set; }
    public string severity { get; set; } = "warning";
    public string form_path { get; set; }
    public string form_prompt { get; set; }
    public string field_path { get; set; }
    public string metadata_path { get; set; }
    public string prompt { get; set; }
    public string subject { get; set; }
    public string value { get; set; }
    public string expected { get; set; }
    public string message { get; set; }
    public bool is_finding { get; set; } = true;
    public string validation_level { get; set; }
    public string review_status { get; set; }
    public string rule_id { get; set; }
    public bool can_quick_edit { get; set; }
    public int? form_index { get; set; }
    public int? grid_index { get; set; }
}

public sealed class CaseValidationEvaluationResult
{
    public string metadata_version { get; set; }
    public List<CaseValidationFinding> findings { get; set; } = new();
    public List<CaseValidationFinding> checks { get; set; } = new();
    public List<CaseValidationFlattenedField> fields { get; set; } = new();
}

public sealed class CaseValidationFieldUpdateRequest
{
    public string case_id { get; set; }
    public string field_path { get; set; }
    public string metadata_path { get; set; }
    public JToken value { get; set; }
    public int? form_index { get; set; }
    public int? grid_index { get; set; }
    public string tab_id { get; set; }
}

public sealed class CaseValidationFieldUpdateResult
{
    public bool ok { get; set; }
    public string id { get; set; }
    public string rev { get; set; }
    public string error_description { get; set; }
    public string serialized_case { get; set; }
}
