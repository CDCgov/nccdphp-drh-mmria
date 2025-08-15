
namespace mmria.ije.generator;

public class GenMortality
{
   Dictionary<string, Func<string>> FieldName;
   GenerationContext Context;
   public GenMortality(GenerationContext _Context)
   {
      Context = _Context;

      FieldName = new()
      {
         { "DOD_YR",gen_DOD_YR},
         { "DSTATE",gen_DSTATE},
         { "FILENO",get_FILENO},
         { "VOID",get_VOID},
         { "AUXNO",get_AUXNO},
         { "BLANK",get_One_BLANK},
         { "GNAME",get_GNAME},
         { "MNAME",get_MNAME},
         { "LNAME",get_LNAME},
         { "SUFF",get_SUFF},
         { "BLANK2", get_53_BLANK},
         { "SSN", get_SSN},
         { "AGETYPE",get_AGETYPE},
         { "AGE", get_AGE},
         {"BLANK3",get_One_BLANK},
         { "DOB_YR", gen_DOB_YR},
         {"DOB_MO", gen_DOB_MO},
         { "DOB_DY", gen_DOB_DY},
         { "BPLACE_CNT", gen_BPLACE_CNT },
         { "BPLACE_ST", gen_BPLACE_ST},
         { "CITYC",  gen_CITYC},
         {"COUNTYC", gen_COUNTYC},
         {"STATEC", gen_STATEC},
         {"COUNTRYC", gen_COUNTRYC},
         {"BLANK4", gen_BLANK},
         {"MARITAL", gen_MARITAL},
         {"BLANK5", gen_BLANK},
         {"DPLACE", gen_DPLACE},
         {"COD", gen_COD},
         {"BLANK6", gen_BLANK},
         {"DOD_MO", gen_DOD_MO},
         {"DOD_DY", gen_DOD_DY},
         {"TOD", gen_TOD},
         {"DEDUC", gen_DEDUC},
         {"BLANK7", gen_BLANK},
         {"DETHNIC1", gen_DETHNIC1},
         {"DETHNIC2", gen_DETHNIC2},
         {"DETHNIC3", gen_DETHNIC3},
         {"DETHNIC4", gen_DETHNIC4},
         {"DETHNIC5", gen_DETHNIC5},
         {"RACE1", gen_RACE1},
         {"RACE2", gen_RACE2},
         {"RACE3", gen_RACE3},
         {"RACE4", gen_RACE4},
         {"RACE5", gen_RACE5},
         {"RACE6", gen_RACE6},
         {"RACE7", gen_RACE7},
         {"RACE8", gen_RACE8},
         {"RACE9", gen_RACE9},
         {"RACE10", gen_RACE10},
         {"RACE11", gen_RACE11},
         {"RACE12", gen_RACE12},
         {"RACE13", gen_RACE13},
         {"RACE14", gen_RACE14},
         {"RACE15", gen_RACE15},
         {"RACE16", gen_RACE16},
         {"RACE17", gen_RACE17},
         {"RACE18", gen_RACE18},
         {"RACE19", gen_RACE19},
         {"RACE20", gen_RACE20},
         {"RACE21", gen_RACE21},
         {"RACE22", gen_RACE22},
         {"RACE23", gen_RACE23},
         {"BLANK8",gen_BLANK},
         {"OCCUP", gen_OCCUP},
         {"OCCUPC", gen_OCCUPC},
         {"INDUST", gen_INDUST},
         {"INDUSTC", gen_INDUSTC},
         {"BLANK9",gen_BLANK},
         {"MANNER", gen_MANNER},
         {"BLANK10",gen_BLANK},
         {"MAN_UC", gen_MAN_UC},
         {"ACME_UC", gen_ACME_UC},
         {"EAC", gen_EAC},
         {"TRX_FLG", gen_TRX_FLG},
         {"RAC", gen_RAC},
         {"AUTOP", gen_AUTOP},
         {"AUTOPF", gen_AUTOPF},
         {"TOBAC", gen_TOBAC},
         {"PREG", gen_PREG},
         {"BLANK11",gen_BLANK},
         {"DOI_MO", gen_DOI_MO},
         {"DOI_DY", gen_DOI_DY},
         {"DOI_YR", gen_DOI_YR},
         {"TOI_HR", gen_TOI_HR},
         {"WORKINJ", gen_WORKINJ},
         {"BLANK12",gen_BLANK},
         {"INACT", gen_INACT},
         {"BLANK13",gen_BLANK},
         {"ARMEDF", gen_ARMEDF},
         {"DINSTI", gen_DINSTI},
         {"ADDRESS_D", gen_ADDRESS_D},
         {"STNUM_D", gen_STNUM_D},
         {"PREDIR_D", gen_PREDIR_D},
         {"STNAME_D", gen_STNAME_D},
         {"STDESIG_D", gen_STDESIG_D},
         {"POSTDIR_D", gen_POSTDIR_D},
         {"CITYTEXT_D", gen_CITYTEXT_D},
         {"STATETEXT_D", gen_STATETEXT_D},
         {"ZIP9_D", gen_ZIP9_D},
         {"COUNTYTEXT_D", gen_COUNTYTEXT_D},
         {"CITYCODE_D", gen_CITYCODE_D},
         {"LONG_D", gen_LONG_D},
         {"LAT_D", gen_LAT_D},
         {"BLANK14",gen_BLANK},
         {"STNUM_R", gen_STNUM_R},
         {"PREDIR_R", gen_PREDIR_R},
         {"STNAME_R", gen_STNAME_R},
         {"STDESIG_R", gen_STDESIG_R},
         {"POSTDIR_R", gen_POSTDIR_R},
         {"UNITNUM_R", gen_UNITNUM_R},
         {"CITYTEXT_R", gen_CITYTEXT_R},
         {"ZIP9_R", gen_ZIP9_R},
         {"COUNTYTEXT_R", gen_COUNTYTEXT_R},
         {"STATETEXT_R ", gen_STATETEXT_R},
         {"COUNTRYTEXT_R", gen_COUNTRYTEXT_R},
         {"ADDRESS_R", gen_ADDRESS_R},
         {"BLANK15",gen_BLANK},
         {"DMIDDLE", gen_DMIDDLE},
         {"BLANK16",gen_BLANK},
         {"POILITRL", gen_POILITRL},
         {"BLANK17",gen_BLANK},
         {"TRANSPRT", gen_TRANSPRT},
         {"COUNTYTEXT_I", gen_COUNTYTEXT_I},
         {"BLANK18",gen_BLANK},
         {"CITYTEXT_I", gen_CITYTEXT_I},
         {"BLANK19",gen_BLANK},
         {"LONG_I", gen_LONG_I},
         {"LAT_I", gen_LAT_I},
         {"BLANK20",gen_BLANK},
         {"REPLACE", gen_REPLACE},
         {"COD1A", gen_COD1A},
         {"INTERVAL1A", gen_INTERVAL1A},
         {"COD1B", gen_COD1B},
         {"INTERVAL1B", gen_INTERVAL1B},
         {"COD1C", gen_COD1C},
         {"INTERVAL1C", gen_INTERVAL1C},
         {"COD1D", gen_COD1D},
         {"INTERVAL1D", gen_INTERVAL1D},
         {"OTHERCONDITION", gen_OTHERCONDITION},
         {"DMAIDEN", gen_DMAIDEN},
         {"DBPLACECITYCODE", gen_DBPLACECITYCODE},
         {"DBPLACECITY", gen_DBPLACECITY},
         {"BLANK21",gen_BLANK},
         {"STINJURY", gen_STINJURY},
         {"BLANK22",gen_BLANK},
         {"DTHCOUNTRY", gen_DTHCOUNTRY},
         {"BLANK23",gen_BLANK},
         {"VRO_STATUS", gen_VRO_STATUS},
         {"BC_DET_MATCH", gen_BC_DET_MATCH},
         {"FDC_DET_MATCH", gen_FDC_DET_MATCH},
         {"BC_PROB_MATCH", gen_BC_PROB_MATCH},
         {"FDC_PROB_MATCH", gen_FDC_PROB_MATCH},
         {"ICD10_MATCH", gen_ICD10_MATCH},
         {"PREGCB_MATCH", gen_PREGCB_MATCH},
         {"LITERALCOD_MATCH", gen_LITERALCOD_MATCH}
         

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

   public string gen_DOD_YR()
   {
      //4		DOD_YR	4 digit year
      return Context.rnd.Next(1980,2030).ToString();
   }
   public string gen_DSTATE()
   {
      return Context.Get(GenerationContext.state_list);
   //2		DSTATE	"NCHS Instruction Manual Part 8A
   /*
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
      */
   }

   string get_FILENO()
   {
      //6		FILENO	Left 0 filled; 000001-999999
      return Context.rnd.Next(1, 999999+ 1).ToString().PadLeft(6, '0');
   }

   string get_VOID()
   {
      //1		VOID	
      /*
"0 =Default; Valid Record
1 = VOID record"
*/
      return Context.Get(GenerationContext.zero_or_one);
   }


   string get_AUXNO()
   {
      //12		AUXNO	000000000001-999999999999; Blank
      if(Context.rnd.NextDouble() > Context.percentage_threshold)
      {
         return Context.rnd.Next(1, 999999999+ 1).ToString().PadLeft(12, '0');
      }
      else
      {
         return "".PadLeft(12, ' ');
      }
   }

   string get_One_BLANK()
   {
      //1		BLANK	BLANK
      return " ";
   }

   string get_GNAME()
   {
      //50		GNAME
      return Context.Get(GenerationContext.first_name).PadLeft(50,' ');
   }	

   string get_MNAME()
   {
      //1		MNAME	
      return Context.Get(GenerationContext.a_z_blank);
   }

   string get_LNAME()
   {
      //50		LNAME	Last name is required
      return Context.Get(GenerationContext.last_name).PadLeft(50,' ');
   }

   string get_SUFF()
   {
      //10		SUFF	
      return Context.Get(GenerationContext.suffix_list).PadLeft(10,' ');
   }

   string get_53_BLANK()
   {
      //53		BLANK	BLANK
      return "".PadLeft(53,' ');
   }

   string get_SSN()
   {
      // 9		SSN	9 digit SSN; blank if unknown or not sharable
      if(Context.rnd.NextDouble() > Context.percentage_threshold)
      {
         return "".PadLeft(9,' ');
      }
      else
      {
         return Context.rnd.NextInt64(000000000, 999999999).ToString();
      }
   }

   string get_AGETYPE()
   {
      /*
      1		AGETYPE	"1 = Years
      2 = Months
      4 = Days
      5 = Hours
      6 = Minutes
      9 = Unknown (not classifiable)"
      */
      return Context.Get(GenerationContext.age_type_list);
   }

   string get_AGE()
   {
      /*
      3		AGE 	"001 - 135, 999
      Codes: If AGETYPE = 1 then 001-135, 999
                                             2 then 001-011, 999
                                             4 then 001-027, 999
                                             5 then 001-023, 999
                                             6 then 001-059, 999
                                             9 then 999"
                                             */
      return Context.rnd.Next(1,136).ToString().PadLeft(3,'0');

   }
//1		BLANK	BLANK

    string gen_DOB_YR()
    {
        //4		DOB_YR	4 digit year; <=year of birth, 9999
    if(Context.rnd.NextDouble() > Context.percentage_threshold)
      {
         return "9999";
      }
      else
      {
        return Context.rnd.Next(1920,2300).ToString();
      }
    }
    string gen_DOB_MO()
    {
         //2		DOB_MO	01-12, 99
      if(Context.rnd.NextDouble() > Context.percentage_threshold)
      {
         return "99";
      }
      else
      {
        return Context.rnd.Next(1,13).ToString().PadLeft(2,'0');
      }
    }
    string gen_DOB_DY()
    {
      //2		DOB_DY	01-31 (based on month), 99
      if(Context.rnd.NextDouble() > Context.percentage_threshold)
      {
         return "99";
      }
      else
      {
        return Context.rnd.Next(1,32).ToString().PadLeft(2,'0');
      }
    }

   string gen_BPLACE_CNT()
   {
      //2		BPLACE_CNT	NCHS Part 8 (from FIPS table 10-4)
      // start:213 length:2
      List<string> list = new()
      {

         "NX",
         "XG",
         "XF",
         "XT",
         "XX",
         "XI",
         "XC",
         "XM",
         "GX",
         "XA",
         "XV",
         "AX",
         "XS",
         "MX",
         "XO",
         "EX",
         "IX",
         "XW",
         "FX",
         "XR",
         "XP",
         "KX"
      };
      return Context.Get(list);
   }

   string gen_BPLACE_ST()
   {
      return Context.Get(GenerationContext.state_list);
      

/*2		BPLACE_ST	"NCHS Instruction Manual Part 8A
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
 */
   }
   string gen_CITYC()
   {
      //5		CITYC	NCHS Instruction Manual Part 8A
      return Context.Get(GenerationContext.a_z_blank).PadLeft(5,' ');
   } 
/*3		COUNTYC	NCHS Instruction Manual Part 8A 
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
2		COUNTRYC	NCHS Instruction Manual Part 8A 
1		BLANK	BLANK
1		MARITAL	"M = Married
A = Married but Separated
W = Widowed
D = Divorced
S = Never Married
U = Not Classifiable"
1		BLANK	BLANK
1		DPLACE	"1 = Inpatient
2 = Emergency Room/Outpatient
3 = Dead on Arrival
4 = Decedent's Home
5 = Hospice Facility
6 = Nursing Home/Long Term Care Facility
7 = Other
9 = Unknown"
3		COD	"NCHS Part 8 (from FIPS table 6-4)
Variable description (""Contents"") edited; same as NCHS ""Facility Name--County"""
1		BLANK	BLANK
2		DOD_MO	01-12, 99
2		DOD_DY	01-31 (based on month), 99
4		TOD	0000-2359, 9999
1		DEDUC	"1 = 8th grade or less
2 = 9th through 12th grade; no diploma
3 = High School Graduate or GED Completed
4 = Some college credit, but no degree
5 = Associate Degree
6 = Bachelor's Degree
7 = Master's Degree
8 = Doctorate Degree or Professional Degree
9 = Unknown"
1		BLANK	BLANK
1		DETHNIC1	"N = No, Not Mexican
H = Yes, Mexican
U = Unknown"
1		DETHNIC2	"N = No, Not Puerto Rican
H = Yes, Puerto Rican
U = Unknown"
1		DETHNIC3	"N = No, Not Cuban
H = Yes, Cuban
U = Unknown"
1		DETHNIC4	"N = No, Not other Hispanic
H = Yes, other Hispanic
U = Unknown"
20		DETHNIC5	Literal; Blank 
1		RACE1	"Y = Yes, box for race checked
N = No, box for race not checked"
1		RACE2	Y, N
1		RACE3	Y, N
1		RACE4	Y, N
1		RACE5	Y, N
1		RACE6	Y, N
1		RACE7	Y, N
1		RACE8	Y, N
1		RACE9	Y, N
1		RACE10	Y, N
1		RACE11	Y, N
1		RACE12	Y, N
1		RACE13	Y, N
1		RACE14	Y, N
1		RACE15	Y, N
30		RACE16	Literal; Blank 
30		RACE17	Literal; Blank 
30		RACE18	Literal; Blank 
30		RACE19	Literal; Blank 
30		RACE20	Literal; Blank
30		RACE21	Literal; Blank 
30		RACE22	Literal; Blank 
30		RACE23	Literal; Blank
49		BLANK	BLANK
40		OCCUP	
3		OCCUPC	
40		INDUST	
3		INDUSTC	Refer to NCHS Instruction Manual Part 19, Industry and Occupation Coding for Death Certificates, 2003. Leave blank if using a coding system other than this.
40		BLANK	BLANK
1		MANNER	Refer to the NCHS Code Structure Descriptions contained in the file layouts for SuperMicar and Transax output formats.  These files are available on the NCHS website at the following address: http://www.cdc.gov/nchs/nvss/vital_certificate_revisions.htm.  Please note that the "Time of Injury Unit" field in position #1075 needs to be completed in conjuction with "Time of injury" in position #989. For Place of Injury (computer generated), record position 704, it will be a numeric code if it is from the Transax file and an alpha code if the field is generated from SuperMicar.
3		BLANK	
5		MAN_UC	
5		ACME_UC	
160		EAC	
1		TRX_FLG	
100		RAC	
1		AUTOP	
1		AUTOPF	
1		TOBAC	
1		PREG	
1		BLANK	
2		DOI_MO	
2		DOI_DY	
4		DOI_YR	
4		TOI_HR	
1		WORKINJ	
30		BLANK	
1		INACT	
56		BLANK	
1		ARMEDF	Y=yes; N=no; U=unknown
30		DINSTI	Facility name literal; if Place of Death (DPLACE)=4 (decedent's home), enter "Home"
50		ADDRESS_D	The item is made up of one long string that includes Street number, Pre Directional, Street name, Street designator, and Post Directional. Jurisdiction should use version of Place of Death address that's used in their system versus reprogramming.
10		STNUM_D	
10		PREDIR_D	
50		STNAME_D	
10		STDESIG_D	
10		POSTDIR_D	
28		CITYTEXT_D	Valid city/town/location literal
28		STATETEXT_D	Valid text for U.S. State or Territory or Canadian Province
9		ZIP9_D	Valid 5+4 digit zip code; 3 space 3 for Canada; unknown portion left blank; do not include the "-"
28		COUNTYTEXT_D	Valid county literal
5		CITYCODE_D	NCHS Part 8 (from FIPS table 55-3) Other part of the 12 digit fips code is contained in earlier part of the record with state and county of death. This is the place or city code
17		LONG_D	As coded by state of occurrence.  Commonly coded with space for a negative sign followed by 3 bytes, a decimal divider, and 6 decimal places.
17		LAT_D	As coded by state of occurrence.  Commonly coded with space for a negative sign followed by 2 bytes, a decimal divider, and 6 decimal places.
101		BLANK	BLANK
10		STNUM_R	
10		PREDIR_R	
28		STNAME_R	
10		STDESIG_R	
10		POSTDIR_R	
7		UNITNUM_R	
28		CITYTEXT_R	Valid city/town/location literal
9		ZIP9_R	Valid 5+4 digit zip code; 3 space 3 for Canada; unknown portion left blank; do not include the "-"
28		COUNTYTEXT_R	Valid county literal
28		STATETEXT_R 	Valid text for U.S. State or Territory or Canadian Province
28		COUNTRYTEXT_R	Valid text for country of residence
50		ADDRESS_R	The item is made up of one long string that includes Street number, Pre Directional, Street name, Street designator, and Post Directional. Jurisdiction should use version of Decedent's Residence address that's used in their system versus reprogramming.
77		BLANK	BLANK
50		DMIDDLE	NCHS only asks for middle initial in start col 77. Free form alpha literal; left justified
251		BLANK	BLANK
50		POILITRL	Literal description; Blank for natural death
250		BLANK	BLANK
30		TRANSPRT	"DR=Driver/Operator
PA=Passenger
PE=Pedestrian
Enter full text if it does not fit above (blank for natural death)"
28		COUNTYTEXT_I	Valid county literal; blank for natural death
3		BLANK	BLANK
28		CITYTEXT_I	Valid town/city literal; blank for natural death.
7		BLANK	BLANK
17		LONG_I	As coded by state of occurrence.  Commonly coded with space for a negative sign followed by 3 bytes, a decimal divider, and 6 decimal places (blank if natural death).
17		LAT_I	As coded by state of occurrence.  Commonly coded with space for a negative sign followed by 2 bytes, a decimal divider, and 6 decimal places (blank if natural death).
2		BLANK	BLANK
1		REPLACE	0=original record; 1=updated record; 2=updated, do not send to NCHS
120		COD1A	Literal information reported on Line a
20		INTERVAL1A	Duration reported on Line a
120		COD1B	Literal information reported on Line b
20		INTERVAL1B	Duration reported on Line b
120		COD1C	Literal information reported on Line c
20		INTERVAL1C	Duration reported on Line c
120		COD1D	Literal information reported on Line d
20		INTERVAL1D	Duration reported on Line d
240		OTHERCONDITION	Literal information reported in Part II
50		DMAIDEN	
5		DBPLACECITYCODE	NCHS Part 8 (from FIPS table 55-3)
28		DBPLACECITY	
845		BLANK	BLANK
28		STINJURY	Valid state, U.S. territory or Canadian province literal, otherwise blank (blank if natural death)
30		BLANK	BLANK
28		DTHCOUNTRY	Valid text for country of death
637		BLANK	BLANK
1		VRO_STATUS	3=N/A (identified via linkage or literal cause of death field)                                                                       9999=blank
1		BC_DET_MATCH	1 = Match,  0 = No Match
1		FDC_DET_MATCH	1 = Match,  0 = No Match
1		BC_PROB_MATCH	1 = Match,  0 = No Match
1		FDC_PROB_MATCH	1 = Match,  0 = No Match
1		ICD10_MATCH	1 = Match,  0 = No Match
1		PREGCB_MATCH	1 = Match,  0 = No Match
1		LITERALCOD_MATCH	1 = Match,  0 = No Match
5000			
= Total Record Length

*/
}
			
			
			
			
			
			
			
			
			
