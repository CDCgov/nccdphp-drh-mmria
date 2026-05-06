# IJE Case Field Guide Generation

- Status: Active
- Scope: Instructions for creating QA-facing case field guide `.txt` files from generated MOR/NAT/FET IJE files.
- Last verified: 2026-05-04
- Related paths:
  - `docs/ai/local/<fixture-folder>/`
  - `docs/ai/local/<fixture-folder>/qa-case-field-guides/`
  - `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/IJEGeneration/Generators/TestIJEFileGenerator.cs`
  - `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs`

## Purpose

Use this guide when a QA tester needs readable per-case documentation for a generated IJE fixture set. The output should be one `.txt` file per MOR case, with the source MOR row and the associated NAT/FET rows expanded into field names, positions, lengths, decoded values, and short descriptions.

These guides are intended for manual validation after upload/import. They should help a tester compare source IJE values against the imported case JSON without manually counting fixed-width columns.

## Inputs

Expected fixture folder contents:

- one `.MOR` file
- one `.NAT` file
- one `.FET` file
- optional imported case JSON outputs such as `case-1.txt`, `case-2.txt`, etc.

Example:

```text
docs/ai/local/ije-5-4-2026/
  2025_2026_05_04_TENANT1.MOR
  2025_2026_05_04_TENANT1.NAT
  2025_2026_05_04_TENANT1.FET
  case-1.txt
  case-2.txt
  ...
```

## Output

Create a sibling output folder named:

```text
docs/ai/local/<fixture-folder>/qa-case-field-guides/
```

Create one file per MOR record:

```text
qa-ije-case-01-first-last.txt
qa-ije-case-02-first-last.txt
...
```

Each guide should include:

- title with case number and decedent name
- source folder and generation timestamp
- MOR source file and line number
- NAT source file and association identifier
- FET source file and association identifier
- imported case JSON match, when available
- quick manual checks:
  - association identifier
  - decedent first, middle, and last name
  - date of death
  - date of birth
  - state of death
  - tenant/file suffix
- field tables for MOR, NAT, and FET

Field table format:

```text
Name | Positions | Len | Value | Description
--- | --- | ---: | --- | ---
DOD_YR | 1-4 | 4 | 2025 | Year of death
```

Use 1-based inclusive positions. Show blank fixed-width values as `<blank>`.

## Validation Rules

Decode the IJE files as UTF-8 text before checking record length. The browser upload path and service-side validation operate on decoded string characters, not raw bytes.

This matters when generated data contains non-ASCII characters such as `Ü`. A row may be `4001` bytes on disk but still be a valid `4000` character NAT record after UTF-8 decoding.

Expected decoded record lengths:

| File type | Decoded record length |
| --- | ---: |
| MOR | 5000 |
| NAT | 4000 |
| FET | 6000 |

Normalize records the same way as upload processing:

- accept CRLF, LF, or CR line endings
- preserve spaces inside fixed-width records
- ignore trailing empty records

## Association Rules

Use the same association positions as `MMRIAServicesHelper`:

| Link | Source field | Positions |
| --- | --- | --- |
| MOR case identifier | MOR `SSN` | 191-199 |
| NAT association identifier | NAT mother SSN | 2000-2008 |
| FET association identifier | FET mother SSN | 4039-4047 |

For each MOR record:

1. Read MOR `SSN` at positions `191-199`.
2. Find the NAT row where positions `2000-2008` match.
3. Find the FET row where positions `4039-4047` match.
4. Match imported case JSON, when available, by decedent first name, last name, and date of death.

## Field Source Of Truth

When building or updating field tables, use the current generator source as the source of truth:

```text
../nccdphp-drh-mmria-utilities/mmria-tools/Testing/IJEGeneration/Generators/TestIJEFileGenerator.cs
```

Relevant methods:

- `GenerateMORRecord`
- `GenerateNATRecord`
- `GenerateFETRecord`

Convert each `SetField(sb, zeroBasedStart, length, value)` call into a guide field:

```text
1-based start = zeroBasedStart + 1
1-based end = zeroBasedStart + length
length = length
```

Example:

```csharp
SetField(sb, 190, 9, ssn);
```

becomes:

```text
SSN | 191-199 | 9
```

Prefer the generator comments for field names and descriptions. If a comment and a `SetField` call disagree, document the actual `SetField` positions and add a short note in the description.

## Recommended Workflow

1. Read `docs/ai/ai_context.md`.
2. Locate the fixture folder under `docs/ai/local/`.
3. Confirm there is exactly one MOR, NAT, and FET file for the set.
4. Decode each file as UTF-8.
5. Normalize records using upload-compatible rules.
6. Validate decoded lengths as MOR `5000`, NAT `4000`, FET `6000`.
7. Build MOR/NAT/FET associations by identifier.
8. Parse optional `case-*.txt` files as JSON and match them to MOR records.
9. Write one QA guide per MOR record under `qa-case-field-guides/`.
10. Spot-check at least one guide for:
    - decoded lengths
    - association identifier
    - imported case JSON match
    - readable field table formatting

## Suggested Implementation Notes

Use PowerShell or a small C# utility. Avoid byte-based slicing. Always slice decoded strings by character index.

PowerShell helpers should use this shape:

```powershell
$text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

function Get-FieldValue([string]$record, [int]$start, [int]$length) {
    $value = $record.Substring($start - 1, $length).Trim()
    if ([string]::IsNullOrEmpty($value)) { return '<blank>' }
    return $value
}
```

Do not write raw IJE rows into the QA guides. The guides should expose field values, positions, and descriptions, not the full fixed-width source line.

## QA Warnings To Surface

Call out these issues in the generated guides or in the final validation response:

- missing NAT association for a MOR record
- missing FET association for a MOR record
- wrong decoded record length
- filename suffix mismatch across MOR/NAT/FET
- imported case JSON that does not match any MOR record
- MOR record that does not match any imported case JSON
- non-ASCII characters, only if the tester is validating with byte-oriented external tooling

For MMRIA upload validation, non-ASCII UTF-8 characters are acceptable when decoded record lengths are correct.
