namespace mmria.ije.generator;

public class GenFetality
{
   Dictionary<string,Func<string>> FieldName;
   GenerationContext Context;

   public GenFetality(GenerationContext _Context)
   {
      Context = _Context;

      FieldName = new()
      { 
         {"FDOD_YR", gen_FDOD_YR},
         /*"DSTATE",
         "FILENO",
         "VOID",
         "AUXNO",
         "TD",
         "FSEX",
         "FDOD_MO",
         "FDOD_DY",
         "CNTYO",
         "DPLACE",
         "FNPI",
         "BLANK",
         "MDOB_YR",
         "MDOB_MO",
         "MDOB_DY",
         "BLANK",
         "BPLACEC_ST_TER",
         "BPLACEC_CNT",
         "CITYC",
         "COUNTYC",
         "STATEC",
         "BLANK",
         "FDOB_YR",
         "FDOB_MO",
         "BLANK",
         "MARN",
         "BLANK",
         "MEDUC",
         "BLANK",
         "METHNIC1",
         "METHNIC2",
         "METHNIC3",
         "METHNIC4",
         "METHNIC5",
         "MRACE1",
         "MRACE2",
         "MRACE3",
         "MRACE4",
         "MRACE5",
         "MRACE6",
         "MRACE7",
         "MRACE8",
         "MRACE9",
         "MRACE10",
         "MRACE11",
         "MRACE12",
         "MRACE13",
         "MRACE14",
         "MRACE15",
         "MRACE16",
         "MRACE17",
         "MRACE18",
         "MRACE19",
         "MRACE20",
         "MRACE21",
         "MRACE22",
         "MRACE23",
         "BLANK",
         "ATTEND",
         "TRAN",
         "DOFP_MO",
         "DOFP_DY",
         "DOFP_YR",
         "DOLP_MO",
         "DOLP_DY",
         "DOLP_YR",
         "NPREV",
         "BLANK",
         "HFT",
         "HIN",
         "BLANK",
         "PWGT",
         "BLANK",
         "DWGT",
         "BLANK",
         "WIC",
         "PLBL",
         "PLBD",
         "POPO",
         "MLLB",
         "YLLB",
         "MOPO",
         "YOPO",
         "CIGPN",
         "CIGFN",
         "CIGSN",
         "CIGLN",
         "DLMP_YR",
         "DLMP_MO",
         "DLMP_DY",
         "PDIAB",
         "GDIAB",
         "PHYPE",
         "GHYPE",
         "PPB",
         "PPO",
         "BLANK",
         "INFT",
         "PCES",
         "NPCES",
         "BLANK",
         "GON",
         "SYPH",
         "HSV",
         "CHAM",
         "LM",
         "GBS",
         "CMV",
         "B19",
         "TOXO",
         "OTHERI",
         "ATTF",
         "ATTV",
         "PRES",
         "ROUT",
         "TLAB",
         "BLANK",
         "MTR",
         "PLAC",
         "RUT",
         "UHYS",
         "AINT",
         "UOPR",
         "FWG",
         "BLANK",
         "OWGEST",
         "BLANK",
         "PLUR",
         "SORD",
         "BLANK",
         "ANEN",
         "MNSB",
         "CCHD",
         "CDH",
         "OMPH",
         "GAST",
         "LIMB",
         "CL",
         "CP",
         "DOWT",
         "CDIT",
         "HYPO",
         "BLANK",
         "MAGER",
         "FAGER",
         "EHYPE",
         "INFT_DRG",
         "INFT_ART",
         "BLANK",
         "COD18a1",
         "COD18a2",
         "COD18a3",
         "COD18a4",
         "COD18a5",
         "COD18a6",
         "COD18a7",
         "COD18a8",
         "COD18a9",
         "COD18a10",
         "COD18a11",
         "COD18a12",
         "COD18a13",
         "COD18a14",
         "COD18b1",
         "COD18b2",
         "COD18b3",
         "COD18b4",
         "COD18b5",
         "COD18b6",
         "COD18b7",
         "COD18b8",
         "COD18b9",
         "COD18b10",
         "COD18b11",
         "COD18b12",
         "COD18b13",
         "COD18b14",
         "ICOD",
         "OCOD1",
         "OCOD2",
         "OCOD3",
         "OCOD4",
         "OCOD5",
         "OCOD6",
         "OCOD7",
         "HSV1",
         "HIV",
         "BLANK",
         "FETLNAME",
         "BLANK",
         "HOSP_D",
         "BLANK",
         "ADDRESS_D",
         "ZIPCODE_D",
         "CNTY_D",
         "CITY_D",
         "STATE_D",
         "COUNTRY_D",
         "LONG_D",
         "LAT_D",
         "MOMFNAME",
         "MOMMNAME",
         "MOMLNAME",
         "BLANK",
         "MOMFMNME",
         "MOMMMID",
         "MOMMAIDN",
         "BLANK",
         "STNUM",
         "PREDIR",
         "STNAME",
         "STDESIG",
         "POSTDIR",
         "APTNUMB",
         "ADDRESS",
         "ZIPCODE",
         "COUNTYTXT",
         "CITYTXT",
         "STATETXT",
         "CNTRYTXT",
         "LONG",
         "LAT",
         "BLANK",
         "DADLNAME",
         "BLANK",
         "MOM_SSN",
         "BLANK",
         "MAGE_CALC",
         "FAGE_CALC",
         "MOM_OC_T",
         "BLANK",
         "DAD_OC_T",
         "BLANK",
         "MOM_IN_T",
         "BLANK",
         "DAD_IN_T",
         "BLANK",
         "FBPLACD_ST_TER_C",
         "FBPLACE_CNT_C",
         "MBPLACE_ST_TER_TXT",
         "MBPLACE_CNTRY_TXT",
         "FBPLACE_ST_TER_TXT",
         "FBPLACE_CNTRY_TXT",
         "FEDUC",
         "BLANK",
         "FETHNIC1",
         "FETHNIC2",
         "FETHNIC3",
         "FETHNIC4",
         "FETHNIC5",
         "FRACE1",
         "FRACE2",
         "FRACE3",
         "FRACE4",
         "FRACE5",
         "FRACE6",
         "FRACE7",
         "FRACE8",
         "FRACE9",
         "FRACE10",
         "FRACE11",
         "FRACE12",
         "FRACE13",
         "FRACE14",
         "FRACE15",
         "FRACE16",
         "FRACE17",
         "FRACE18",
         "FRACE19",
         "FRACE20",
         "FRACE21",
         "FRACE22",
         "FRACE23",
         "BLANK",
         "HOSPFROM",
         "BLANK",
         "ATTEND_NPI",
         "ATTEND_OTH_TXT",
         "BLANK",
         "REPLACE",
         "BLANK",
         "RECORD_TYPE"
         */
      };
   }

   public override string ToString()
   {
      var result = new System.Text.StringBuilder();
      foreach(var kvp in FieldName)
      {
         result.Append(kvp.Value());
      }

      return result.ToString();
   }

   string gen_FDOD_YR()
   {
      //4		FDOD_YR	4 digit year
      return Context.rnd.Next(1980,2030).ToString();
   }

/*2		DSTATE	"NCHS Instruction Manual Part 8A
For US Territories:
   MP  NORTHERN MARIANAS
   AS  AMERICAN SAMOA
   GU  GUAM
   VI   VIRGIN ISLANDS
   PR  PUERTO RICO
For Canadian Provinces:
   AB  ALBERTA  
   BC  BRITISH COLUMBIA 
   MB  MANITOBA 
   NB  NEW BRUNSWICK  
   NF NEWFOUNDLAND  
   NS  NOVA SCOTIA 
   NT  NORTHWEST TERRITORIES
   NU  NUNAVUT
   ON  ONTARIO
   PE  PRINCE EDWARD ISLAND 
   QC  QUEBEC  
   SK  SASKATCHEWAN
   YT  YUKON"
6		FILENO	Left 0 filled; 000001-999999
1		VOID	"0 = default; valid record
1 = VOID record"
12		AUXNO	000000000001-999999999999; blank
4		TD	Military Time 0000-2359, 9999
1		FSEX	"M = Male
F = Female
U = Unknown"
2		FDOD_MO	01-12
2		FDOD_DY	01-31 (based on month)
3		CNTYO	NCHS Part 8A (from FIPS table 6-4) -- Appendix C
1		DPLACE	"1 = Hospital
2 = Freestanding Birth Center
3 = Home (Intended)
4 = Home (Not Intended)
5 = Home (Unknown if Intended)
6 = Clinic/Doctor's Office
7 = Other
9 = Unknown"
12		FNPI	
4		BLANK	BLANK
4		MDOB_YR	4 digit year; < year of delivery, 9999
2		MDOB_MO	01-12, 99
2		MDOB_DY	01-31 (based on month), 99
1		BLANK	BLANK
2		BPLACEC_ST_TER	"NCHS Instruction Manual Part 8A
   ZZ = UNKNOWN OR BLANK U.S. STATE OR TERRITORY OR UNKNOWN CANADIAN PROVINCE OR UNKNOWN/ UNCLASSIFIABLE COUNTRY
   XX = UNKNOWN STATE WHERE COUNTRY IS KNOWN, BUT NOT U.S. OR CANADA
For U.S. Territories:
   MP  NORTHERN MARIANAS
   AS  AMERICAN SAMOA
   GU  GUAM
   VI   VIRGIN ISLANDS
   PR  PUERTO RICO
For Canadian Provinces:
   AB  ALBERTA  
   BC  BRITISH COLUMBIA 
   MB  MANITOBA 
   NB  NEW BRUNSWICK  
   NF NEWFOUNDLAND  
   NS  NOVA SCOTIA 
   NT  NORTHWEST TERRITORIES
   NU  NUNAVUT
   ON  ONTARIO
   PE  PRINCE EDWARD ISLAND 
   QC  QUEBEC  
   SK  SASKATCHEWAN
   YT  YUKON"
2		BPLACEC_CNT	NCHS Part 8A (from FIPS table 10-4) -- Appendix A
5		CITYC	NCHS Part 8A (from FIPS table 55-3) -- Appendix C
3		COUNTYC	NCHS Part 8A (from FIPS table 6-4) -- Appendix C
2		STATEC	"NCHS Instruction Manual Part 8A
   ZZ = UNKNOWN OR BLANK U.S. STATE OR TERRITORY OR UNKNOWN CANADIAN PROVINCE OR UNKNOWN/ UNCLASSIFIABLE COUNTRY
   XX = UNKNOWN STATE WHERE COUNTRY IS KNOWN, BUT NOT U.S. OR CANADA
For U.S. Territories:
   MP  NORTHERN MARIANAS
   AS  AMERICAN SAMOA
   GU  GUAM
   VI   VIRGIN ISLANDS
   PR  PUERTO RICO
For Canadian Provinces:
   AB  ALBERTA  
   BC  BRITISH COLUMBIA 
   MB  MANITOBA 
   NB  NEW BRUNSWICK  
   NF NEWFOUNDLAND  
   NS  NOVA SCOTIA 
   NT  NORTHWEST TERRITORIES
   NU  NUNAVUT
   ON  ONTARIO
   PE  PRINCE EDWARD ISLAND 
   QC  QUEBEC  
   SK  SASKATCHEWAN
   YT  YUKON"
3		BLANK	BLANK
4		FDOB_YR	4 digit year; < year of delivery, 9999
2		FDOB_MO	01-12, 99
4		BLANK	BLANK
1		MARN	"Y = Yes
N = No
U = Unknown"
1		BLANK	BLANK
1		MEDUC	"1 = 8th grade or less
2 = 9th through 12th grade; no diploma
3 = High School Graduate or GED Completed
4 = Some college credit, but no degree
5 = Associate Degree
6 = Bachelor's Degree
7 = Master's Degree
8 = Doctorate Degree or Professional Degree
9 = Unknown"
1		BLANK	BLANK
1		METHNIC1	"N = No, Not Mexican
H = Yes, Mexican
U = Unknown"
1		METHNIC2	"N = No, Not Puerto Rican
H = Yes, Puerto Rican
U = Unknown"
1		METHNIC3	"N = No, Not Cuban
H = Yes, Cuban
U = Unknown"
1		METHNIC4	"N = No, Not other Hispanic
H = Yes, other Hispanic
U = Unknown"
20		METHNIC5	Literal text, or Blank
1		MRACE1	"Y = Yes, box for race checked
N = No, box for race not checked"
1		MRACE2	Y, N
1		MRACE3	Y, N
1		MRACE4	Y, N
1		MRACE5	Y, N
1		MRACE6	Y, N
1		MRACE7	Y, N
1		MRACE8	Y, N
1		MRACE9	Y, N
1		MRACE10	Y, N
1		MRACE11	Y, N
1		MRACE12	Y, N
1		MRACE13	Y, N
1		MRACE14	Y, N
1		MRACE15	Y, N
30		MRACE16	Literal text, or Blank
30		MRACE17	Literal text, or Blank
30		MRACE18	Literal text, or Blank
30		MRACE19	Literal text, or Blank
30		MRACE20	Literal text, or Blank
30		MRACE21	Literal text, or Blank
30		MRACE22	Literal text, or Blank
30		MRACE23	Literal text, or Blank
48		BLANK	BLANK
1		ATTEND	"1 = MD
2 = DO
3 = CNM/CM
4 = Other midwife
5 = Other (specify)
9 = Unknown"
1		TRAN	"Y = Yes
N = No
U = Unknown"
2		DOFP_MO	01-12, 88, 99
2		DOFP_DY	01-31 (based on month), 88, 99
4		DOFP_YR	"4 digit year; year of delivery or
(year of delivery - 1), 8888, 9999"
2		DOLP_MO	01-12, 88, 99
2		DOLP_DY	01-31 (based on month), 88, 99
4		DOLP_YR	"4 digit year; year of delivery or
(year of delivery - 1), 8888, 9999"
2		NPREV	00-98, 99
1		BLANK	BLANK
1		HFT	1-8, 9
2		HIN	00-11, 99
1		BLANK	BLANK
3		PWGT	050-400, 999
1		BLANK	BLANK
3		DWGT	050-450, 999
1		BLANK	BLANK
1		WIC	"Y = Yes
N = No
U = Unknown"
2		PLBL	00-30, 99
2		PLBD	00-30, 99
2		POPO	00-30, 99
2		MLLB	01-12, 88, 99
4		YLLB	"4 digit year;
(year of mother's birth + 10) through
year of delivery, 8888, 9999"
2		MOPO	01-12, 88, 99
4		YOPO	"4 digit year;
(year of mother's birth + 10) through
year of delivery, 8888, 9999"
2		CIGPN	00-98, 99
2		CIGFN	00-98, 99
2		CIGSN	00-98, 99
2		CIGLN	00-98, 99
4		DLMP_YR	"4 digit year; year of delivery or
(year of delivery - 1) or
(year of delivery - 2), 9999"
2		DLMP_MO	01-12, 99
2		DLMP_DY	01-31 (based on month), 99
1		PDIAB	"Y = Yes
N = No
U = Unknown"
1		GDIAB	Y, N, U
1		PHYPE	Y, N, U
1		GHYPE	Y, N, U
1		PPB	Y, N, U
1		PPO	Y, N, U
1		BLANK	BLANK
1		INFT	Y, N, U
1		PCES	Y, N, U
2		NPCES	00-30, 99
1		BLANK	BLANK
1		GON	"Y = Yes
N = No
U = Unknown"
1		SYPH	Y, N, U
1		HSV	Y, N, U  (BLANK IF DELETED)
1		CHAM	Y, N, U
1		LM	Y, N, U
1		GBS	Y, N, U
1		CMV	Y, N, U
1		B19	Y, N, U
1		TOXO	Y, N, U
1		OTHERI	Y, N, U
1		ATTF	Y, N, U  (BLANK IF DELETED)
1		ATTV	Y, N, U  (BLANK IF DELETED)
1		PRES	"1 = Cephalic
2 = Breech
3 = Other
9 = Unknown"
1		ROUT	"1 = Spontaneous
2 = Forceps
3 = Vacuum
4 = Cesarean
9 = Unknown"
1		TLAB	"Y = Yes
N = No
U = Unknown
X = Not Applicable"
1		BLANK	BLANK
1		MTR	"Y = Yes
N = No
U = Unknown"
1		PLAC	Y, N, U
1		RUT	Y, N, U
1		UHYS	Y, N, U
1		AINT	Y, N, U
1		UOPR	Y, N, U
4		FWG	0000-9999
1		BLANK	BLANK
2		OWGEST	00-98, 99
5		BLANK	BLANK
2		PLUR	01-12, 99
2		SORD	01-12, 99
9		BLANK	BLANK
1		ANEN	"Y = Yes
N = No
U = Unknown"
1		MNSB	Y, N, U
1		CCHD	Y, N, U
1		CDH	Y, N, U
1		OMPH	Y, N, U
1		GAST	Y, N, U
1		LIMB	Y, N, U
1		CL	Y, N, U
1		CP	Y, N, U
1		DOWT	"C = Confirmed
P = Pending
N = No
U = Unknown"
1		CDIT	"C = Confirmed
P = Pending
N = No
U = Unknown"
1		HYPO	"Y = Yes
N = No
U = Unknown"
8		BLANK	BLANK
2		MAGER	00-98, 99
2		FAGER	00-98, 99
1		EHYPE	Y, N, U  (BLANK IF NOT ADDED)
1		INFT_DRG	"Y = Yes (BLANK IF NOT ADDED)
N = No
X = Not Applicable
U = Unknown"
1		INFT_ART	Y, N, X, U  (BLANK IF NOT ADDED)
11		BLANK	BLANK
1		COD18a1	"Y = Yes
N = No"
1		COD18a2	Y, N
1		COD18a3	Y, N
1		COD18a4	Y, N
1		COD18a5	Y, N
1		COD18a6	Y, N
1		COD18a7	Y, N
60		COD18a8	Literal text or Blank
60		COD18a9	Literal text or Blank
60		COD18a10	Literal text or Blank
60		COD18a11	Literal text or Blank
60		COD18a12	Literal text or Blank
60		COD18a13	Literal text or Blank
60		COD18a14	Literal text or Blank
1		COD18b1	"Y = Yes
N = No"
1		COD18b2	Y, N
1		COD18b3	Y, N
1		COD18b4	Y, N
1		COD18b5	Y, N
1		COD18b6	Y, N
1		COD18b7	Y, N
240		COD18b8	Literal text or Blank
240		COD18b9	Literal text or Blank
240		COD18b10	Literal text or Blank
240		COD18b11	Literal text or Blank
240		COD18b12	Literal text or Blank
240		COD18b13	Literal text or Blank
240		COD18b14	Literal text or Blank
5		ICOD	ICD-10 code
5		OCOD1	ICD-10 code, blank
5		OCOD2	ICD-10 code, blank
5		OCOD3	ICD-10 code, blank
5		OCOD4	ICD-10 code, blank
5		OCOD5	ICD-10 code, blank
5		OCOD6	ICD-10 code, blank
5		OCOD7	ICD-10 code, blank
1		HSV1	Y, N, U
1		HIV	Y, N, U
101		BLANK	BLANK
50		FETLNAME	Free form literal
11		BLANK	BLANK
50		HOSP_D	Delivery facility name literal
97		BLANK	BLANK
50		ADDRESS_D	The item is made up of one long string that includes Street number, Pre Directional, Street name, Street designator, and Post Directional. Jurisdiction should use version of Place of Delivery address that's used in their system versus reprogramming.
9		ZIPCODE_D	Valid 5+4 digit zipcode; 3 space 3 for Canada; unknown portion left blank; do not include the "-"
28		CNTY_D	Valid county literal or blank if unknown
28		CITY_D	Valid city/town/place literal or blank if unknown
28		STATE_D	valid state, U.S. territory or Canadian province literal, otherwise blank
28		COUNTRY_D	Valid country literal or blank if unknown
17		LONG_D	As coded by state of occurrence.  Commonly coded with space for a negative sign followed by 3 bytes, a decimal divider, and 6 decimal places
17		LAT_D	As coded by state of occurrence.  Commonly coded with space for a negative sign followed by 2 bytes, a decimal divider, and 6 decimal places
50		MOMFNAME	Free form literal
50		MOMMNAME	Free form literal
50		MOMLNAME	Free form literal
10		BLANK	BLANK
50		MOMFMNME	Free form literal
50		MOMMMID	Free form literal
50		MOMMAIDN	Free form literal
10		BLANK	BLANK
10		STNUM	parsed residence address
10		PREDIR	
50		STNAME	
10		STDESIG	
10		POSTDIR	
7		APTNUMB	
50		ADDRESS	The item is made up of one long string that includes Street number, Pre Directional, Street name, Street designator, and Post Directional. Jurisdiction should use version of Mother's Residence address that's used in their system versus reprogramming.
9		ZIPCODE	Valid 5+4 digit zipcode; 3 space 3 for Canada; unknown portion left blank; do not include the "-"
28		COUNTYTXT	Valid county literal or blank if unknown
28		CITYTXT	Valid city/town/place literal or blank if unknown
28		STATETXT	Valid state, U.S. territory or Canadian province literal, otherwise blank
28		CNTRYTXT	Valid country literal or blank if unknown
17		LONG	As coded by state of occurrence.  Commonly coded with space for a negative sign followed by 3 bytes, a decimal divider, and 6 decimal places
17		LAT	As coded by state of occurrence.  Commonly coded with space for a negative sign followed by 2 bytes, a decimal divider, and 6 decimal places
100		BLANK	BLANK
50		DADLNAME	Free form literal
10		BLANK	BLANK
9		MOM_SSN	9 digit SSN; blank if unknown or not sharable
9		BLANK	BLANK
2		MAGE_CALC	0-98, 99
2		FAGE_CALC	0-98, 99
25		MOM_OC_T	Literal for mother's usual occupation
3		BLANK	BLANK
25		DAD_OC_T	Literal for father's usual occupation
3		BLANK	BLANK
25		MOM_IN_T	Literal for mother's corresponding industry
3		BLANK	BLANK
25		DAD_IN_T	Literal for father's corresponding industry
3		BLANK	BLANK
2		FBPLACD_ST_TER_C	"NCHS Instruction Manual Part 8A
   ZZ = UNKNOWN OR BLANK U.S. STATE OR TERRITORY OR UNKNOWN CANADIAN PROVINCE OR UNKNOWN/ UNCLASSIFIABLE COUNTRY
   XX = UNKNOWN STATE WHERE COUNTRY IS KNOWN, BUT NOT U.S. OR CANADA
For U.S. Territories:
   MP  NORTHERN MARIANAS
   AS  AMERICAN SAMOA
   GU  GUAM
   VI   VIRGIN ISLANDS
   PR  PUERTO RICO
For Canadian Provinces:
   AB  ALBERTA  
   BC  BRITISH COLUMBIA 
   MB  MANITOBA 
   NB  NEW BRUNSWICK  
   NF NEWFOUNDLAND  
   NS  NOVA SCOTIA 
   NT  NORTHWEST TERRITORIES
   NU  NUNAVUT
   ON  ONTARIO
   PE  PRINCE EDWARD ISLAND 
   QC  QUEBEC  
   SK  SASKATCHEWAN
   YT  YUKON"
2		FBPLACE_CNT_C	NCHS Part 8A (from FIPS 10-4 codes)
28		MBPLACE_ST_TER_TXT	Valid state, U.S. territory or Canadian province literal, otherwise blank
28		MBPLACE_CNTRY_TXT	Valid country literal when foreign, otherwise U.S.
28		FBPLACE_ST_TER_TXT	Valid state, U.S. territory or Canadian province literal, otherwise blank
28		FBPLACE_CNTRY_TXT	Valid country literal when foreign, otherwise U.S.
1		FEDUC	"1 = 8th grade or less
2 = 9th through 12th grade; no diploma
3 = High School Graduate or GED Completed
4 = Some college credit, but no degree
5 = Associate Degree
6 = Bachelor's Degree
7 = Master's Degree
8 = Doctorate Degree or Professional Degree
9 = Unknown"
1		BLANK	BLANK
1		FETHNIC1	"N = No, Not Mexican
H = Yes, Mexican
U = Unknown"
1		FETHNIC2	"N = No, Not Puerto Rican
H = Yes, Puerto Rican
U = Unknown"
1		FETHNIC3	"N = No, Not Cuban
H = Yes, Cuban
U = Unknown"
1		FETHNIC4	"N = No, Not other Hispanic
H = Yes, other Hispanic
U = Unknown"
20		FETHNIC5	Literal text, or Blank
1		FRACE1	"Y = Yes, box for race checked
N = No, box for race not checked"
1		FRACE2	Y, N
1		FRACE3	Y, N
1		FRACE4	Y, N
1		FRACE5	Y, N
1		FRACE6	Y, N
1		FRACE7	Y, N
1		FRACE8	Y, N
1		FRACE9	Y, N
1		FRACE10	Y, N
1		FRACE11	Y, N
1		FRACE12	Y, N
1		FRACE13	Y, N
1		FRACE14	Y, N
1		FRACE15	Y, N
30		FRACE16	Literal text, or Blank
30		FRACE17	Literal text, or Blank
30		FRACE18	Literal text, or Blank
30		FRACE19	Literal text, or Blank
30		FRACE20	Literal text, or Blank
30		FRACE21	Literal text, or Blank
30		FRACE22	Literal text, or Blank
30		FRACE23	Literal text, or Blank
194		BLANK	BLANK
50		HOSPFROM	Literal facility name; blank if not transferred
50		BLANK	BLANK
12		ATTEND_NPI	National Provider Index (NPI) number of attendant at delivery
20		ATTEND_OTH_TXT	Alpha character string when "Other" text is specified
167		BLANK	BLANK
1		REPLACE	0=original record; 1=updated record; 2=updated, do not send to NCHS
937		BLANK	BLANK
1		RECORD_TYPE	0 = BC  1=FDC
= Total Record Length			
*/
}