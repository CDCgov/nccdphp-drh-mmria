# Strongly-Typed Case Generator - AI Context

## Overview

The strongly-typed-case generator is a **metadata-driven code generation tool** that creates type-safe C# classes from MMRIA metadata stored in CouchDB. This tool is critical for maintaining version-specific case document structures.

**Location**: `c:\repos\nccdphp-drh-mmria-utilities\strongly-typed-case\`

**Purpose**: Generates ~4,500 lines of C# code per metadata version to support strongly-typed case document access

---

## When to Run This Tool

**MUST run when:**
- Adding new top-level case properties (e.g., `is_offline`, `offline_lock_type`)
- Creating a new metadata version (e.g., 25.10.14 → 26.01.20)
- Modifying case data structure in CouchDB metadata
- Changing field types or cardinality in metadata

**DO NOT manually edit generated files:**
- ❌ Never edit `case-version/mmria/v{version}/*.cs` files
- ❌ Generated code will be overwritten on next run
- ✅ Always update metadata first, then regenerate

---

## Critical Learning: Adding New Properties

### The Problem We Encountered (Offline Lock Type Feature - Feb 2026)

**What Happened:**
- Manually added `offline_lock_type` property to v251014 generated files
- This was **wrong** - generated files should never be manually edited
- Build errors occurred due to type mismatches
- Changes would be lost on next generator run

**The Correct Process:**

```
1. Update CouchDB Metadata Document
   ↓
2. Run strongly-typed-case Generator
   ↓
3. Copy Generated Files to Server Project
   ↓
4. Update Namespace References in Code
   ↓
5. Build & Test
```

---

## Step-by-Step: Adding New Top-Level Properties

### Step 1: Update CouchDB Metadata

**Location**: `{couchdb_url}/metadata/version_specification-{version}/metadata`

**Example**: Adding `offline_lock_type` field

```json
{
  "_id": "version_specification-26.01.20",
  "_rev": "...",
  "children": [
    {
      "name": "version",
      "type": "string",
      "prompt": "Version"
    },
    // ... other top-level fields ...
    {
      "name": "offline_lock_type",
      "type": "number",
      "prompt": "Offline Lock Type",
      "description": "Type of offline lock: null (not offline), 1 (soft lock - queue), 2 (hard lock - actively offline)"
    },
    // ... forms follow ...
  ]
}
```

**Metadata Type Mapping:**

| Metadata Type | C# Generated Type | Notes |
|---------------|-------------------|-------|
| `"string"` | `string?` | Nullable reference |
| `"textarea"` | `string?` | Multi-line text |
| `"number"` | `double?` | Nullable numeric |
| `"boolean"` | `bool?` | Nullable boolean |
| `"datetime"` | `DateTime?` | Date and time |
| `"date"` | `DateOnly?` | Date only (.NET 6+) |
| `"time"` | `TimeOnly?` | Time only (.NET 6+) |
| `"list"` | `string?` or `List<string>?` | Based on `is_multiselect` |
| `"grid"` | Nested class with `List<>` | Table structure |

**Top-Level vs Form Fields:**
- **Top-level**: Add to metadata root `children` array (before forms)
- **Form fields**: Add to specific form's `children` array

### Step 2: Configure Generator

**File**: `c:\repos\nccdphp-drh-mmria-utilities\strongly-typed-case\Program.cs`

**Update metadata version list:**
```csharp
var metadata_list = new List<string>()
{
    "23.11.08", "24.03.01", "24.06.16", "24.10.01",
    "25.02.13", "25.08.14", "25.10.14", "26.01.20"  // ← Add new version
};
var metadata_index = 7; // ← Update to point to new version
```

**Generator URL** (configured in Program.cs):
```csharp
var metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-{metadata_version}/metadata";
```

### Step 3: Run Generator

```powershell
cd c:\repos\nccdphp-drh-mmria-utilities\strongly-typed-case
dotnet run
```

**Output** (in `output/` directory):
1. `mmria_case.cs` - Main class with properties (~4,400 lines)
2. `mmria_case.convert.cs` - JSON conversion methods
3. `mmria_case.get.s.cs` - Single form getters
4. `mmria_case.set.s.cs` - Single form setters
5. `mmria_case.get.sg.cs` - Single form grid getters
6. `mmria_case.set.sg.cs` - Single form grid setters
7. `mmria_case.get.m.cs` - Multi-form getters
8. `mmria_case.set.m.cs` - Multi-form setters
9. `mmria_case.get.mg.cs` - Multi-form grid getters
10. `mmria_case.set.mg.cs` - Multi-form grid setters

### Step 4: Copy Generated Files

```powershell
# Verify namespace version in generated files
$version = "v260120"  # Format: v{version with dots removed}

# Copy to server project
Copy-Item c:\repos\nccdphp-drh-mmria-utilities\strongly-typed-case\output\*.cs `
          c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\case-version\mmria\$version\
```

**Verify**:
- Check namespace in generated files: `namespace mmria.case_version.v260120;`
- Ensure all 10 files copied successfully
- Confirm new properties appear in `mmria_case.cs`

### Step 5: Update Namespace References

**Files that typically reference case version** (search for `mmria.case_version.v{oldversion}`):

| File | Purpose | Typical Changes |
|------|---------|-----------------|
| `Controllers/api/caseController.cs` | Case CRUD operations | 3 references (Get, DeserializeObject, Case_Data) |
| `SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` | Offline case management | using statement |
| `SharedLibraries/OfflineCase/Model/DocumentChange.cs` | Offline document tracking | 2 properties (OriginalDocument, ModifiedDocument) |
| `SharedLibraries/Case/DAL/CaseDAL.cs` | Case data access | using statement |
| `util/authorization_case.cs` | Case authorization | Method parameter |
| `Controllers/update_year_of_death.cs` | Year migration | DeserializeObject |

**Example**:
```csharp
// Old
using mmria.case_version.v251014;

// New
using mmria.case_version.v260120;
```

### Step 6: Update Runtime Configuration

**Multiple Sources** (depending on deployment):
1. **appsettings.json** (local dev)
2. **CouchDB configuration database** (production)
3. **Environment variables** (containerized)

**Change**:
```json
{
  "metadata_version": "26.01.20"  // was "25.10.14"
}
```

**Where Used**:
- `configuration.GetString("metadata_version", host_prefix)` appears in multiple controllers
- Used for new case creation
- Displayed in UI
- Logged in audit records

### Step 7: Build and Verify

```powershell
cd c:\repos\nccdphp-drh-mmria
dotnet build source-code/mmria/mmria-server/mmria-server.csproj
```

**What to Check**:
- ✅ Build succeeds with no new errors (warnings are pre-existing)
- ✅ New properties accessible: `case.offline_lock_type`
- ✅ Getter methods work: `case.GetS_String("offline_lock_type")`
- ✅ Setter methods work: `case.SetS_Double("offline_lock_type", 1)`
- ✅ JSON conversion works for new properties

---

## Architecture Deep Dive

### How the Generator Works

#### 1. Metadata Fetching (`Program.cs`)

```csharp
using(var metadata_client = new HttpClient())
{
    metadata = await metadata_client.GetFromJsonAsync<mmria.common.metadata.app>(metadata_url);
}
```

- Fetches metadata JSON from CouchDB
- Deserializes into `mmria.common.metadata.app` object
- Metadata tree structure mirrors UI form structure

#### 2. Metadata Parsing (`metadata_mgr.cs`)

**Classification** (see `metadata_mgr.cs` constructor):

```csharp
single_form_value_set = all_list_set.Where(o => 
    o.is_multiform == false && 
    o.is_grid == false && 
    o.Node.is_multiselect == null
).ToList();

multiform_value_set = all_list_set.Where(o => 
    o.is_multiform == true && 
    o.is_grid == false && 
    o.Node.is_multiselect == null
).ToList();
```

**Field Categories**:
- **Single form**: Normal form fields (cardinality ≠ "+" or "*")
- **Multi-form**: Repeating forms (cardinality = "+" or "*")
- **Grid**: Table-like structure (type = "grid")
- **Multi-valued**: Checkbox groups (is_multiselect ≠ null)

#### 3. Code Generation (`metadata_mgr.PassTwo()`)

**Top-Level Class Generation**:

```csharp
source_code_builder.AppendLine("public sealed partial class mmria_case\n{\n\tpublic mmria_case()\n\t{");

// _id and _rev are HARDCODED
source_code_builder.AppendLine("""
    public string _id { get; set; }
    public string _rev { get; set; }
""");

// All other properties come from metadata.children
foreach(var child in value.children)
{
    WriteAttribute(child, "", source_code_builder);
}
```

**Key Insight**: Only `_id` and `_rev` are hardcoded. Everything else comes from metadata!

#### 4. Namespace Versioning

```csharp
var name_space_version = $"v{metadata_version.Replace(".", "")}";
// "26.01.20" → "v260120"

source_code_builder.AppendLine($"""
namespace mmria.case_version.{name_space_version};
""");
```

**Version Format**:
- Metadata: `25.10.14` (dots for readability)
- CouchDB doc ID: `version_specification-25.10.14`
- Namespace: `v251014` (no dots, v prefix)
- Folder: `case-version/mmria/v251014/`

---

## Common Scenarios

### Scenario 1: Adding a New Top-Level String Field

**Example**: Adding `review_status` field

1. **Metadata**:
```json
{
  "name": "review_status",
  "type": "string",
  "prompt": "Review Status",
  "description": "Current review status of the case"
}
```

2. **Generated Code**:
```csharp
public string review_status { get; set; }

// In Convert():
review_status = mmria_case.GetStringField(p_value, "review_status", "review_status");

// In GetS_String():
"review_status" => review_status,

// In SetS_String():
case "review_status":
    review_status = value;
    result = true;
break;
```

3. **No namespace change needed** if adding to existing version

### Scenario 2: Creating a New Metadata Version

**Example**: v260120 → v260301

1. Update metadata in CouchDB: Create new doc `version_specification-26.03.01`
2. Add to `metadata_list`: `"26.03.01"`
3. Update `metadata_index`: `8`
4. Run generator
5. Copy to `case-version/mmria/v260301/`
6. Update namespace refs: `v260120` → `v260301`
7. Update config: `"metadata_version": "26.03.01"`

### Scenario 3: Adding a Number Field

**Example**: Adding `priority_score` (numeric)

1. **Metadata type**: `"number"`
2. **Generated type**: `double?`
3. **Getter method**: `GetS_Double()`
4. **Setter method**: `SetS_Double()`

**Usage**:
```csharp
case.priority_score = 85.5;
var score = case.GetS_Double("priority_score");
case.SetS_Double("priority_score", 90.0);
```

### Scenario 4: Adding a Boolean Field

**Example**: Adding `is_flagged`

1. **Metadata type**: `"boolean"`
2. **Generated type**: `bool?`
3. **Getter method**: `GetS_Boolean()` (if exists, check `mmria_case.get.s.cs`)
4. **Setter method**: `SetS_Boolean()`

---

## Dependencies & Integration

### Generator Dependencies

- **.NET 9 SDK**
- **mmria.common** library (referenced from main project)
- **Internet access** to CouchDB server (for metadata fetch)
- **HttpClient** for metadata retrieval

### Server Project Integration

**The generated classes are used by**:

1. **Controllers**:
   - `caseController.cs` - Case CRUD operations
   - `case_viewController.cs` - Case list/search (indirectly)
   - `update_year_of_death.cs` - Data migration

2. **Managers**:
   - `OfflineCaseManager` - Offline case handling
   - Case validation logic

3. **DAL**:
   - `CaseDAL` - Database operations

4. **Utilities**:
   - `authorization_case.cs` - Permission checks
   - Export tools
   - IJE generator

### CouchDB View Integration

**Important**: Generated classes represent **case documents**, not view responses!

| Component | Purpose | Model |
|-----------|---------|-------|
| Case Document | Full case data in CouchDB | `mmria.case_version.v260120.mmria_case` |
| Case View | Sortable list data | `case_view_sortable_item` (NOT generated) |

**Lesson**: When adding properties to case documents, you may also need to:
1. Update CouchDB views (`case_design_sortable.json`)
2. Update view response models (`case_view_response.cs`)

---

## Troubleshooting

### Issue: "Cannot implicitly convert type 'X' to 'Y'"

**Cause**: Metadata type doesn't match generated C# type

**Fix**:
- Check metadata `type` field
- Ensure generator's type mapping is correct
- Common issue: `number` generates `double?`, not `int?`

**Example**:
```csharp
// Metadata: { "type": "number" }
// Generated: public double? offline_lock_type { get; set; }

// Correct usage:
case.offline_lock_type = 1.0;  // or just: 1

// Wrong usage:
int? value = case.offline_lock_type;  // ❌ Type mismatch
```

### Issue: "Build succeeds but property not accessible"

**Cause**: Generated files not copied or namespace not updated

**Fix**:
1. Verify files exist: `case-version/mmria/v{version}/*.cs`
2. Check namespace in file matches references
3. Rebuild solution

### Issue: "Property exists in metadata but not in generated code"

**Cause**: Top-level vs form-level placement

**Fix**:
- Top-level properties go in metadata root `children`
- Form properties go in form's `children`
- Grid properties go in grid's `children`

### Issue: "Old version still being used"

**Cause**: Configuration not updated

**Fix**:
1. Update `metadata_version` in config
2. Restart application
3. Clear cache if applicable

---

## Best Practices

### DO ✅

- **Always update metadata first** before generating code
- **Use semantic versioning** for metadata versions (YY.MM.DD format)
- **Test generated code** before committing
- **Document metadata changes** in commit messages
- **Keep generator tool up to date** with mmria.common changes
- **Version control metadata** (export CouchDB docs)

### DON'T ❌

- **Never manually edit generated files** (will be overwritten)
- **Don't skip version increments** (26.01.20 → 26.01.21, not 26.01.30)
- **Don't reuse old version namespaces** for new metadata
- **Don't forget to update runtime config** after generation
- **Don't deploy without testing** new case version
- **Don't modify generator output manually** "just this once"

---

## Related Documentation

- **Main AI Context**: [AI_CONTEXT.md](./AI_CONTEXT.md)
- **Utilities Project**: [strongly-typed-case_AI_CONTEXT.md](../../nccdphp-drh-mmria-utilities/ai/strongly-typed-case_AI_CONTEXT.md) (detailed algorithm docs)
- **Offline Mode**: [offline_mode.md](./offline_mode.md)
- **Case Generator**: [mmria-case-generator AI_CONTEXT.md](../../nccdphp-drh-mmria-utilities/mmria-case-generator/docs/AI_CONTEXT.md)

---

## Quick Reference

### File Locations

| What | Where |
|------|-------|
| Generator Project | `c:\repos\nccdphp-drh-mmria-utilities\strongly-typed-case\` |
| Generator Output | `c:\repos\nccdphp-drh-mmria-utilities\strongly-typed-case\output\` |
| Server Location | `c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\case-version\mmria\{version}\` |
| Metadata in CouchDB | `{couchdb}/metadata/version_specification-{version}/metadata` |

### Command Quick Reference

```powershell
# Run Generator
cd c:\repos\nccdphp-drh-mmria-utilities\strongly-typed-case
dotnet run

# Copy Output
Copy-Item output\*.cs c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\case-version\mmria\v260120\

# Build Server
cd c:\repos\nccdphp-drh-mmria
dotnet build source-code/mmria/mmria-server/mmria-server.csproj

# Search for Version References
cd c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server
grep -r "v251014" --include="*.cs"
```

### Version Format Cheat Sheet

| Context | Format | Example |
|---------|--------|---------|
| Human Readable | YY.MM.DD | 26.01.20 |
| CouchDB Doc ID | version_specification-YY.MM.DD | version_specification-26.01.20 |
| C# Namespace | vYYMMDD | v260120 |
| Folder Name | vYYMMDD | v260120/ |
| Config Value | "YY.MM.DD" | "26.01.20" |

---

## Summary

The strongly-typed-case generator is the **single source of truth** for case document structure. Always update metadata first, then regenerate. Never manually edit generated code. Following the proper workflow ensures type safety, maintainability, and consistency across the entire MMRIA application.

**Remember**: Metadata → Generate → Copy → Reference → Config → Build → Test
