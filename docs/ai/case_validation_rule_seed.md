# Case Validation Rule Seed

- Status: Active seed
- Scope: Initial AI-assisted, human-reviewable case validation rules for the current database metadata version.
- Last verified: 2026-05-07
- Related docs: [Case Validation Context](./case_validation_context.md), [AI Context Index](./AI_CONTEXT.md)

## Purpose

This seed captures the first subject-based validation analysis. It should be treated as reviewable metadata, not a runtime LLM dependency. The app creates editable rule metadata from the current metadata document and these conservative subject patterns.

## Form Status Mapping

Map form status by prompt/subject, not by code name.

Known status source group:

```text
home_record/case_progress_report
```

Status values:

```text
9999 = blank
0 = Not Started
1 = In Progress
2 = Completed
3 = Not Available
4 = Not Applicable
```

Initial form prompt mapping:

- Death Certificate -> Death Certificate
- Birth/Fetal Death Certificate- Parent Section -> Birth/Fetal Death Certificate- Parent Section
- Birth/Fetal Death Certificate- Infant/Fetal Section -> Birth/Fetal Death Certificate- Infant/Fetal Section
- Community Vital Signs -> Community Vital Signs
- Social and Environmental Profile -> Social and Environmental Profile
- Autopsy Report -> Autopsy Report
- Prenatal Care Record -> Prenatal Care Record
- ER Visits and Hospitalizations -> ER Visits and Hospitalizations
- Other Medical Office Visits -> Other Medical Office Visits
- Medical Transport -> Medical Transport
- Mental Health Profile -> Mental Health Profile
- Informant Interviews -> Informant Interviews
- Case Narrative -> Case Narrative
- Committee Decisions -> Committee Decisions

Warnings:

- Status is Not Started, Not Available, or Not Applicable while meaningful data exists.
- Status is Completed while meaningful data count is below the form threshold.

Meaningful data ignores blank/default sentinel values such as `9999`, `9998`, `8888`, `7777`, `6666`, blank strings, empty arrays, and empty objects.

## Metadata-Derived Rules

Generate editable field rules from:

- `min_value`
- `max_value`
- `max_length`
- `regex_pattern`
- list `values`
- field prompt, tags, and ancestry

These rules should be created as warning rules and reviewed in the metadata editor before treating them as program-quality data quality checks.

## Seeded Subject Ranges

The initial seed keeps ranges intentionally broad to avoid noisy clinical false positives.

| Subject pattern | Initial range | Notes |
| --- | ---: | --- |
| Temperature | 80-115 F | Broad human plausibility warning for clinical vital signs. |
| Heart rate / pulse | 20-250 bpm | Broad clinical vital sign plausibility warning. |
| Respiration / respiratory rate | 4-80 breaths/min | Broad clinical vital sign plausibility warning. |
| Systolic blood pressure | 40-300 mmHg | Pair with diastolic connected-field rule. |
| Diastolic blood pressure | 20-200 mmHg | Pair with systolic connected-field rule. |
| Oxygen saturation | 0-100% | Percent cannot be below 0 or above 100. |
| Blood sugar / glucose | 10-1000 mg/dL | Unit-sensitive broad plausibility warning. |
| Apgar score | 0-10 | Intrinsic Apgar score range. |
| Gestational age weeks | 0-45 | Applies to week-based gestational age fields. |
| Gestational age days | 0-6 | Applies when days are paired with weeks. |
| Maternal age / age at death | 10-60 | Broad warning range only. |
| Gravida / parity | 0-25 | Broad warning range only. |
| Birth weight grams | 0-7000 | Use unit-specific review when units are explicit. |
| Birth weight ounces | 0-15 | Applies to ounce remainder fields. |
| BMI | 10-80 | Broad plausibility warning. |
| Height feet/inches | 3-8 feet, 0-11 inches | Applies to adult/maternal height remainder fields. |
| Adult/maternal weight | 50-700 pounds | Unit-sensitive broad plausibility warning. |

## Connected Field Seeds

Initial connected-field rules:

- Death certificate date of birth should not be later than home record date of death.
- Injury date, delivery date, clinical visit event date/time, transport event date/time, and vital sign date/time should not be later than home record date of death.
- Systolic blood pressure should be greater than or equal to paired diastolic blood pressure.
- Gestational age days should be 0-6 when captured alongside gestational age weeks.

Promoted runtime seed candidates:

- hospital/ER arrival date-time should be on or before admission date-time, and admission date-time should be on or before discharge date-time
- autopsy date should be on or after date of death
- informant interview date, committee review date, and case locked date should be on or after date of death
- abstraction begin date should be on or before abstraction complete date
- parent-section delivery date and infant/fetal-section delivery date should match when both are available
- first prenatal visit should be on or before last prenatal visit in the birth/fetal death certificate parent section and prenatal care record
- all systolic/diastolic BP pairs should be detected across naming variants such as `bp_systolic`/`bp_diastolic`, `systolic_bp`/`diastolic_bp`, `systolic_bp`/`diastolic`, and `systolic`/`diastolic`
- "Other" list selections should require the matching specify field

Future candidate review findings:

Needs tolerance or calculated-date policy:

- date of death should be on or after all reported maternal dates of birth when full dates are available
- death certificate age at death and age on death certificate should align with date of birth and date of death when full dates are available
- birth certificate parent-section mother age should align with mother date of birth and delivery date; father age should align with father date of birth and delivery date when enough components exist
- date of last normal menses should be on or before first prenatal visit, first ultrasound, last prenatal visit, estimated delivery date, and delivery date
- first ultrasound should not be before date of last normal menses unless explicitly reviewed as an exception
- prenatal, ER, transport, and other medical visit gestational age weeks/days should be consistent with the event date and pregnancy anchor dates when enough dates are present
- prenatal care total number of visits should be consistent with first/last prenatal visit dates and should not be positive when both visit dates are missing without explanation
- weight gain should be consistent with pre-pregnancy weight and delivery/last-visit weight when all values are available
- BMI should be consistent with height and weight in maternal biometrics, prenatal current pregnancy, ER maternal biometrics, and autopsy maternal biometrics

Needs stronger metadata mapping:

- ER internal transfer date-time should fall within the ER/hospital admission and discharge window when those dates are available
- other medical office visit date should be on or before same-visit vital signs, laboratory, diagnostic imaging, referral, and medication event dates
- medical transport vital sign date-time should align with the date of transport and should not be later than the date of death
- abstraction complete date should be on or before committee review date or case locked date
- birth/fetal weight value should be interpreted with its unit field; ounces should be 0-15 and should only be used as an ounce remainder when the paired value is pounds
- Apgar 10-minute score should not be entered without a 5-minute score unless reviewed as an exception
- multiple gestation, plurality, and birth order should agree when those fields are present across parent and infant/fetal birth certificate sections

Likely noisy until preview impact is reviewed:

- parent-section previous live births should equal now living plus now dead when all three values are present
- prenatal gravida should be greater than or equal to para and abortions, and should be broadly consistent with pregnancy history detail rows
- specify fields should be blank or reviewed when "Other" is not selected
- yes/no source fields should be consistent with dependent grids, such as toxicology performed vs toxicology rows, preexisting conditions vs condition rows, referrals vs referral rows, and pre-delivery hospitalizations vs hospitalization rows

## Review Guidance

AI can propose additional connected-field rules by clustering fields by normalized prompt, tags, and metadata ancestry. A human should review the proposed rules and publish them through the validation metadata editor.

Do not add runtime LLM calls to V1 validation. AI output belongs in docs and metadata seed updates.
