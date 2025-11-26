# MMRIA Command Line Tests

A .NET console application for generating test IJE (Inter-jurisdictional Exchange) files for MMRIA vital records import testing.

## Overview

This utility generates test data files in the standard IJE format used by vital statistics systems:

- **.MOR** - Mortality (Death) records
- **.NAT** - Natality (Birth) records  
- **.FET** - Fetal Death records

The file formats are reverse-engineered from the `BatchItemProcessor` parsing code to ensure compatibility with MMRIA's import functionality.

## Usage

### Basic Usage (Default Settings)

```bash
dotnet run
```

This generates 10 records of each type in `c:\temp\test-ije-files\`

### Custom Output Directory

```bash
dotnet run "c:\my-custom-path"
```

### Custom Record Count

```bash
dotnet run "c:\my-custom-path" 25
```

This generates 25 records of each type.

## Command Line Arguments

```
mmria-cmd-line-tests [output-directory] [record-count]
```

- **output-directory** (optional): Path where files will be generated
  - Default: `c:\temp\test-ije-files`
  
- **record-count** (optional): Number of records per file
  - Default: 10

## Output Files

Files are generated with timestamp-based names:

```
TEST_YYYYMMDDHHmmss.MOR
TEST_YYYYMMDDHHmmss.NAT
TEST_YYYYMMDDHHmmss.FET
```

Each file contains fixed-width format records (5000 characters per record) with:
- Realistic test data (names, dates, addresses, etc.)
- Sequential certificate numbers
- Proper field positioning matching IJE specifications
- All required fields populated

## Building

```bash
dotnet build
```

## Publishing

To create a standalone executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## File Formats

### MOR (Mortality) - 5000 characters per record
Contains death certificate information including:
- Decedent demographics (name, DOB, age, race, ethnicity)
- Death details (date, place, cause of death)
- Residence and death location addresses
- Medical certification details

### NAT (Natality) - ~5000 characters per record
Contains birth certificate information including:
- Infant demographics
- Mother and father information
- Prenatal care details
- Labor and delivery information
- Birth weight and APGAR scores

### FET (Fetal Death) - ~5000 characters per record
Contains fetal death certificate information including:
- Mother and father demographics
- Pregnancy history
- Delivery details
- Congenital anomalies
- Cause of fetal death

## Notes

- All field positions and lengths are based on the IJE format specification
- Test data is synthetic and for testing purposes only
- Files can be used to test MMRIA's vital records import functionality without requiring real vital statistics data
