using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace mmria.cmd.line.tests
{
    /// <summary>
    /// Generates test IJE files (.MOR, .NAT, .FET) based on the format specifications
    /// reverse-engineered from the BatchItemProcessor parsing code.
    /// </summary>
    public class TestIJEFileGenerator
    {
        private Random _random = new Random();

        /// <summary>
        /// Generates a test .MOR (Mortality) file with sample data
        /// Total record length: 5000 characters
        /// </summary>
        public void GenerateMORFile(string filePath, int recordCount = 5)
        {
            var records = new List<string>();

            for (int i = 0; i < recordCount; i++)
            {
                records.Add(GenerateMORRecord(i + 1));
            }

            File.WriteAllLines(filePath, records);
            Console.WriteLine($"Generated {recordCount} MOR records in: {filePath}");
        }

        /// <summary>
        /// Generates a test .NAT (Natality/Birth) file with sample data
        /// Total record length varies by year, typically ~5000 characters
        /// </summary>
        public void GenerateNATFile(string filePath, int recordCount = 5)
        {
            var records = new List<string>();

            for (int i = 0; i < recordCount; i++)
            {
                records.Add(GenerateNATRecord(i + 1));
            }

            File.WriteAllLines(filePath, records);
            Console.WriteLine($"Generated {recordCount} NAT records in: {filePath}");
        }

        /// <summary>
        /// Generates a test .FET (Fetal Death) file with sample data
        /// Total record length: ~5000 characters
        /// </summary>
        public void GenerateFETFile(string filePath, int recordCount = 5)
        {
            var records = new List<string>();

            for (int i = 0; i < recordCount; i++)
            {
                records.Add(GenerateFETRecord(i + 1));
            }

            File.WriteAllLines(filePath, records);
            Console.WriteLine($"Generated {recordCount} FET records in: {filePath}");
        }

        private string GenerateMORRecord(int sequenceNumber)
        {
            var sb = new StringBuilder(new string(' ', 5000));

            // DOD_YR (1-4): Year of death
            SetField(sb, 0, 4, "2023");

            // DSTATE (5-6): State of death
            SetField(sb, 4, 2, "MI");

            // FILENO (7-12): Certificate number
            SetField(sb, 6, 6, sequenceNumber.ToString("D6"));

            // AUXNO (14-25): Auxiliary number
            SetField(sb, 13, 12, sequenceNumber.ToString("D12"));

            // GNAME (27-76): Given/First name
            SetField(sb, 26, 50, "JANE");

            // LNAME (78-127): Last name
            SetField(sb, 77, 50, $"TESTCASE{sequenceNumber:D4}");

            // SSN (191-199): Social Security Number
            SetField(sb, 190, 9, "123456789");

            // AGETYPE (200): Age type (1=years, 2=months, 4=days, 5=hours, 6=minutes, 9=not stated)
            SetField(sb, 199, 1, "1");

            // AGE (201-203): Age
            SetField(sb, 200, 3, "035");

            // DOB_YR (205-208): Year of birth
            SetField(sb, 204, 4, "1988");

            // DOB_MO (209-210): Month of birth
            SetField(sb, 208, 2, "06");

            // DOB_DY (211-212): Day of birth
            SetField(sb, 210, 2, "15");

            // BPLACE_CNT (213-214): Country of birth
            SetField(sb, 212, 2, "US");

            // BPLACE_ST (215-216): State of birth
            SetField(sb, 214, 2, "MI");

            // CITYC (217-221): City code
            SetField(sb, 216, 5, "12345");

            // COUNTYC (222-224): County code
            SetField(sb, 221, 3, "163");

            // STATEC (225-226): State code
            SetField(sb, 224, 2, "26");

            // COUNTRYC (227-228): Country code
            SetField(sb, 226, 2, "US");

            // MARITAL (230): Marital status
            SetField(sb, 229, 1, "M");

            // DPLACE (232): Place of death
            SetField(sb, 231, 1, "1");

            // COD (233-235): County of death
            SetField(sb, 232, 3, "163");

            // DOD_MO (237-238): Month of death
            SetField(sb, 236, 2, "03");

            // DOD_DY (239-240): Day of death
            SetField(sb, 238, 2, "15");

            // TOD (241-244): Time of death (HHMM)
            SetField(sb, 240, 4, "1430");

            // DEDUC (245): Decedent's education
            SetField(sb, 244, 1, "4");

            // DETHNIC1-5: Decedent's ethnicity
            SetField(sb, 246, 1, "N");
            SetField(sb, 247, 1, "N");
            SetField(sb, 248, 1, "N");
            SetField(sb, 249, 1, "N");
            SetField(sb, 250, 20, "");

            // RACE1-15: Decedent's race (1=White, 2=Black, etc.)
            SetField(sb, 270, 1, "Y");
            for (int i = 271; i < 285; i++)
            {
                SetField(sb, i, 1, "N");
            }

            // RACE16-23: Race text fields
            for (int i = 0; i < 8; i++)
            {
                SetField(sb, 285 + (i * 30), 30, "");
            }

            // OCCUP (575-614): Occupation
            SetField(sb, 574, 40, "SOFTWARE ENGINEER");

            // INDUST (618-657): Industry
            SetField(sb, 617, 40, "COMPUTER PROGRAMMING");

            // MANNER (701): Manner of death
            SetField(sb, 700, 1, "N");

            // AUTOP (976): Autopsy performed
            SetField(sb, 975, 1, "Y");

            // AUTOPF (977): Autopsy findings available
            SetField(sb, 976, 1, "Y");

            // TOBAC (978): Tobacco use contributed
            SetField(sb, 977, 1, "N");

            // PREG (979): Pregnancy status
            SetField(sb, 978, 1, "1");

            // ARMEDF (1081): Armed Forces
            SetField(sb, 1080, 1, "N");

            // Death address fields
            // STNUM_D (1162-1171): Street number
            SetField(sb, 1161, 10, "123");

            // STNAME_D (1182-1231): Street name
            SetField(sb, 1181, 50, "MAIN STREET");

            // CITYTEXT_D (1252-1279): City
            SetField(sb, 1251, 28, "DETROIT");

            // STATETEXT_D (1280-1307): State
            SetField(sb, 1279, 28, "MICHIGAN");

            // ZIP9_D (1308-1316): ZIP code
            SetField(sb, 1307, 9, "482011234");

            // COUNTYTEXT_D (1317-1344): County
            SetField(sb, 1316, 28, "WAYNE");

            // Residence address fields
            // STNUM_R (1485-1494): Street number
            SetField(sb, 1484, 10, "456");

            // STNAME_R (1505-1532): Street name
            SetField(sb, 1504, 28, "ELM AVENUE");

            // CITYTEXT_R (1560-1587): City
            SetField(sb, 1559, 28, "ANN ARBOR");

            // ZIP9_R (1588-1596): ZIP code
            SetField(sb, 1587, 9, "481031234");

            // COUNTYTEXT_R (1597-1624): County
            SetField(sb, 1596, 28, "WASHTENAW");

            // DMIDDLE (1808-1857): Middle name
            SetField(sb, 1807, 50, "MARIE");

            // DMAIDEN (3342-3391): Maiden name
            SetField(sb, 3341, 50, "SMITH");

            // COD1A (2542-2661): Cause of death Part I Line A
            SetField(sb, 2541, 120, "ACUTE RESPIRATORY FAILURE");

            // INTERVAL1A (2662-2681): Interval Line A
            SetField(sb, 2661, 20, "2 DAYS");

            // COD1B (2682-2801): Cause of death Part I Line B
            SetField(sb, 2681, 120, "PNEUMONIA");

            // INTERVAL1B (2802-2821): Interval Line B
            SetField(sb, 2801, 20, "1 WEEK");

            // COD1C (2822-2941): Cause of death Part I Line C
            SetField(sb, 2821, 120, "");

            // COD1D (2962-3081): Cause of death Part I Line D
            SetField(sb, 2961, 120, "");

            // OTHERCONDITION (3102-3341): Other significant conditions
            SetField(sb, 3101, 240, "HYPERTENSION");

            // DBPLACECITY (3397-3424): Birthplace city
            SetField(sb, 3396, 28, "LANSING");

            // VRO_STATUS (4993): VRO status
            SetField(sb, 4992, 1, "0");

            return sb.ToString();
        }

        private string GenerateNATRecord(int sequenceNumber)
        {
            var sb = new StringBuilder(new string(' ', 4000));

            // IDOB_YR (1-4): Infant year of birth
            SetField(sb, 0, 4, "2023");

            // BSTATE (5-6): State of birth
            SetField(sb, 4, 2, "MI");

            // FILENO (7-12): Certificate number
            SetField(sb, 6, 6, sequenceNumber.ToString("D6"));

            // AUXNO (14-25): Auxiliary number
            SetField(sb, 13, 12, sequenceNumber.ToString("D12"));

            // TB (26-29): Time of birth (HHMM)
            SetField(sb, 25, 4, "0830");

            // IDOB_MO (31-32): Infant month of birth
            SetField(sb, 30, 2, "07");

            // IDOB_DY (33-34): Infant day of birth
            SetField(sb, 32, 2, "20");

            // BPLACE (38): Place of birth
            SetField(sb, 37, 1, "1");

            // FNPI (39-50): Facility NPI
            SetField(sb, 38, 12, "1234567890");

            // MDOB_YR (55-58): Mother year of birth
            SetField(sb, 54, 4, "1995");

            // MDOB_MO (59-60): Mother month of birth
            SetField(sb, 58, 2, "03");

            // MDOB_DY (61-62): Mother day of birth
            SetField(sb, 60, 2, "10");

            // BPLACEC_ST_TER (64-65): Mother's birthplace state
            SetField(sb, 63, 2, "MI");

            // BPLACEC_CNT (66-67): Mother's birthplace country
            SetField(sb, 65, 2, "US");

            // STATEC (76-77): County code
            SetField(sb, 75, 2, "26");

            // FDOB_YR (81-84): Father year of birth
            SetField(sb, 80, 4, "1993");

            // FDOB_MO (85-86): Father month of birth
            SetField(sb, 84, 2, "05");

            // MARN (91): Parents married
            SetField(sb, 90, 1, "Y");

            // ACKN (92): Paternity acknowledged
            SetField(sb, 91, 1, "Y");

            // MEDUC (93): Mother's education
            SetField(sb, 92, 1, "4");

            // METHNIC1-5: Mother's ethnicity
            SetField(sb, 94, 1, "N");
            SetField(sb, 95, 1, "N");
            SetField(sb, 96, 1, "N");
            SetField(sb, 97, 1, "N");
            SetField(sb, 98, 20, "");

            // MRACE1-15: Mother's race
            SetField(sb, 118, 1, "Y");
            for (int i = 119; i < 133; i++)
            {
                SetField(sb, i, 1, "N");
            }

            // MRACE16-23: Mother's race text
            for (int i = 0; i < 8; i++)
            {
                SetField(sb, 133 + (i * 30), 30, "");
            }

            // FEDUC (422): Father's education
            SetField(sb, 421, 1, "5");

            // FETHNIC1-5: Father's ethnicity
            SetField(sb, 423, 1, "N");
            SetField(sb, 424, 1, "N");
            SetField(sb, 425, 1, "N");
            SetField(sb, 426, 1, "N");
            SetField(sb, 427, 20, "");

            // FRACE1-15: Father's race
            SetField(sb, 447, 1, "Y");
            for (int i = 448; i < 462; i++)
            {
                SetField(sb, i, 1, "N");
            }

            // FRACE16-23: Father's race text
            for (int i = 0; i < 8; i++)
            {
                SetField(sb, 462 + (i * 30), 30, "");
            }

            // ATTEND (751): Attendant at birth
            SetField(sb, 750, 1, "1");

            // TRAN (752): Transfer
            SetField(sb, 751, 1, "N");

            // DOFP_MO (753-754): Date of first prenatal visit - month
            SetField(sb, 752, 2, "02");

            // DOFP_DY (755-756): Date of first prenatal visit - day
            SetField(sb, 754, 2, "15");

            // DOFP_YR (757-760): Date of first prenatal visit - year
            SetField(sb, 756, 4, "2023");

            // DOLP_MO (761-762): Date of last prenatal visit - month
            SetField(sb, 760, 2, "07");

            // DOLP_DY (763-764): Date of last prenatal visit - day
            SetField(sb, 762, 2, "15");

            // DOLP_YR (765-768): Date of last prenatal visit - year
            SetField(sb, 764, 4, "2023");

            // NPREV (769-770): Number of previous births now living
            SetField(sb, 768, 2, "01");

            // HFT (772): Mother's height - feet
            SetField(sb, 771, 1, "5");

            // HIN (773-774): Mother's height - inches
            SetField(sb, 772, 2, "06");

            // PWGT (776-778): Mother's prepregnancy weight
            SetField(sb, 775, 3, "140");

            // DWGT (780-782): Mother's delivery weight
            SetField(sb, 779, 3, "165");

            // WIC (784): WIC
            SetField(sb, 783, 1, "Y");

            // PLBL (785-786): Number of prenatal visits
            SetField(sb, 784, 2, "12");

            // PLBD (787-788): Plurality - number born alive
            SetField(sb, 786, 2, "01");

            // POPO (789-790): Plurality - number born dead
            SetField(sb, 788, 2, "00");

            // CIGPN (803-804): Cigarettes before pregnancy
            SetField(sb, 802, 2, "00");

            // CIGFN (805-806): Cigarettes first trimester
            SetField(sb, 804, 2, "00");

            // CIGSN (807-808): Cigarettes second trimester
            SetField(sb, 806, 2, "00");

            // CIGLN (809-810): Cigarettes third trimester
            SetField(sb, 808, 2, "00");

            // PAY (811): Payment source
            SetField(sb, 810, 1, "1");

            // DLMP_YR (812-815): Date of last menses - year
            SetField(sb, 811, 4, "2022");

            // DLMP_MO (816-817): Date of last menses - month
            SetField(sb, 815, 2, "10");

            // DLMP_DY (818-819): Date of last menses - day
            SetField(sb, 817, 2, "15");

            // Medical risk factors
            SetField(sb, 819, 1, "N"); // PDIAB
            SetField(sb, 820, 1, "N"); // GDIAB
            SetField(sb, 821, 1, "N"); // PHYPE
            SetField(sb, 822, 1, "N"); // GHYPE
            SetField(sb, 823, 1, "N"); // PPB
            SetField(sb, 824, 1, "N"); // PPO

            // NPCES (829-830): Number of previous cesareans
            SetField(sb, 828, 2, "00");

            // Infections
            SetField(sb, 831, 1, "N"); // GON
            SetField(sb, 832, 1, "N"); // SYPH
            SetField(sb, 833, 1, "N"); // HSV

            // Obstetric procedures
            SetField(sb, 853, 1, "N"); // ATTF
            SetField(sb, 854, 1, "Y"); // ATTV
            SetField(sb, 855, 1, "1"); // PRES - Cephalic
            SetField(sb, 856, 1, "1"); // ROUT - Vaginal

            // BWG (865-868): Birth weight in grams
            SetField(sb, 864, 4, "3250");

            // OWGEST (870-871): Obstetric estimate of gestation
            SetField(sb, 869, 2, "39");

            // APGAR5 (873-874): 5-minute Apgar score
            SetField(sb, 872, 2, "09");

            // APGAR10 (875-876): 10-minute Apgar score
            SetField(sb, 874, 2, "99");

            // PLUR (877-878): Plurality
            SetField(sb, 876, 2, "01");

            // SORD (879-880): Set order
            SetField(sb, 878, 2, "01");

            // ITRAN (909): Infant transferred
            SetField(sb, 908, 1, "N");

            // ILIV (910): Infant living
            SetField(sb, 909, 1, "Y");

            // BFED (911): Breastfed
            SetField(sb, 910, 1, "Y");

            // MAGER (920-921): Mother's age
            SetField(sb, 919, 2, "28");

            // FAGER (922-923): Father's age
            SetField(sb, 921, 2, "30");

            // Mother's SSN (2000-2008): Must match MOR SSN for linking
            SetField(sb, 1999, 9, "123456789");

            return sb.ToString();
        }

        private string GenerateFETRecord(int sequenceNumber)
        {
            var sb = new StringBuilder(new string(' ', 6000));

            // FDOD_YR (1-4): Year of death
            SetField(sb, 0, 4, "2023");

            // DSTATE (5-6): State
            SetField(sb, 4, 2, "MI");

            // FILENO (7-12): Certificate number
            SetField(sb, 6, 6, sequenceNumber.ToString("D6"));

            // AUXNO (14-25): Auxiliary number
            SetField(sb, 13, 12, sequenceNumber.ToString("D12"));

            // TD (26-29): Time of delivery (HHMM)
            SetField(sb, 25, 4, "1145");

            // FSEX (30): Sex of fetus
            SetField(sb, 29, 1, "M");

            // FDOD_MO (31-32): Month of death
            SetField(sb, 30, 2, "05");

            // FDOD_DY (33-34): Day of death
            SetField(sb, 32, 2, "25");

            // DPLACE (38): Place of delivery
            SetField(sb, 37, 1, "1");

            // FNPI (39-50): Facility NPI
            SetField(sb, 38, 12, "9876543210");

            // MDOB_YR (55-58): Mother year of birth
            SetField(sb, 54, 4, "1990");

            // MDOB_MO (59-60): Mother month of birth
            SetField(sb, 58, 2, "08");

            // MDOB_DY (61-62): Mother day of birth
            SetField(sb, 60, 2, "20");

            // BPLACEC_ST_TER (64-65): Mother's birthplace state
            SetField(sb, 63, 2, "MI");

            // BPLACEC_CNT (66-67): Mother's birthplace country
            SetField(sb, 65, 2, "US");

            // STATEC (76-77): County code
            SetField(sb, 75, 2, "26");

            // FDOB_YR (81-84): Father year of birth
            SetField(sb, 80, 4, "1988");

            // FDOB_MO (85-86): Father month of birth
            SetField(sb, 84, 2, "04");

            // MARN (91): Parents married
            SetField(sb, 90, 1, "Y");

            // MEDUC (93): Mother's education
            SetField(sb, 92, 1, "3");

            // METHNIC1-5: Mother's ethnicity
            SetField(sb, 94, 1, "N");
            SetField(sb, 95, 1, "N");
            SetField(sb, 96, 1, "N");
            SetField(sb, 97, 1, "N");
            SetField(sb, 98, 20, "");

            // MRACE1-15: Mother's race
            SetField(sb, 118, 1, "Y");
            for (int i = 119; i < 133; i++)
            {
                SetField(sb, i, 1, "N");
            }

            // MRACE16-23: Mother's race text
            for (int i = 0; i < 8; i++)
            {
                SetField(sb, 133 + (i * 30), 30, "");
            }

            // ATTEND (422): Attendant
            SetField(sb, 421, 1, "1");

            // TRAN (423): Transfer
            SetField(sb, 422, 1, "N");

            // DOFP_MO (424-425): Date of first prenatal visit - month
            SetField(sb, 423, 2, "01");

            // DOFP_DY (426-427): Date of first prenatal visit - day
            SetField(sb, 425, 2, "10");

            // DOFP_YR (428-431): Date of first prenatal visit - year
            SetField(sb, 427, 4, "2023");

            // DOLP_MO (432-433): Date of last prenatal visit - month
            SetField(sb, 431, 2, "05");

            // DOLP_DY (434-435): Date of last prenatal visit - day
            SetField(sb, 433, 2, "20");

            // DOLP_YR (436-439): Date of last prenatal visit - year
            SetField(sb, 435, 4, "2023");

            // NPREV (440-441): Number of previous births
            SetField(sb, 439, 2, "02");

            // HFT (443): Mother's height - feet
            SetField(sb, 442, 1, "5");

            // HIN (444-445): Mother's height - inches
            SetField(sb, 443, 2, "04");

            // PWGT (447-449): Mother's prepregnancy weight
            SetField(sb, 446, 3, "135");

            // DWGT (451-453): Mother's delivery weight
            SetField(sb, 450, 3, "155");

            // WIC (455): WIC
            SetField(sb, 454, 1, "N");

            // PLBL (456-457): Number born alive
            SetField(sb, 455, 2, "00");

            // PLBD (458-459): Number born dead
            SetField(sb, 457, 2, "01");

            // POPO (460-461): Number of other pregnancy outcomes
            SetField(sb, 459, 2, "00");

            // CIGPN (474-475): Cigarettes before pregnancy
            SetField(sb, 473, 2, "05");

            // CIGFN (476-477): Cigarettes first trimester
            SetField(sb, 475, 2, "02");

            // CIGSN (478-479): Cigarettes second trimester
            SetField(sb, 477, 2, "00");

            // CIGLN (480-481): Cigarettes third trimester
            SetField(sb, 479, 2, "00");

            // DLMP_YR (482-485): Date of last menses - year
            SetField(sb, 481, 4, "2022");

            // DLMP_MO (486-487): Date of last menses - month
            SetField(sb, 485, 2, "09");

            // DLMP_DY (488-489): Date of last menses - day
            SetField(sb, 487, 2, "01");

            // Risk factors
            SetField(sb, 489, 1, "N"); // PDIAB
            SetField(sb, 490, 1, "N"); // GDIAB
            SetField(sb, 491, 1, "N"); // PHYPE
            SetField(sb, 492, 1, "N"); // GHYPE
            SetField(sb, 493, 1, "N"); // PPB
            SetField(sb, 494, 1, "N"); // PPO

            // NPCES (499-500): Number of previous cesareans
            SetField(sb, 498, 2, "00");

            // ATTF (512): Fetal presentation attendant
            SetField(sb, 511, 1, "N");

            // ATTV (513): Vertex presentation attendant
            SetField(sb, 512, 1, "Y");

            // PRES (514): Presentation
            SetField(sb, 513, 1, "1");

            // ROUT (515): Route of delivery
            SetField(sb, 514, 1, "1");

            // FWG (524-527): Fetal weight in grams
            SetField(sb, 523, 4, "1850");

            // OWGEST (529-530): Obstetric estimate of gestation
            SetField(sb, 528, 2, "26");

            // PLUR (536-537): Plurality
            SetField(sb, 535, 2, "01");

            // SORD (538-539): Set order
            SetField(sb, 537, 2, "01");

            // Congenital anomalies
            SetField(sb, 548, 1, "N"); // ANEN
            SetField(sb, 549, 1, "N"); // MNSB
            SetField(sb, 550, 1, "N"); // CCHD

            // MAGER (569-570): Mother's age
            SetField(sb, 568, 2, "33");

            // FAGER (571-572): Father's age
            SetField(sb, 570, 2, "35");

            // FEDUC (4289): Father's education
            SetField(sb, 4288, 1, "4");

            // Mother's SSN (4039-4047): Must match MOR SSN for linking
            SetField(sb, 4038, 9, "123456789");

            // MOMFNAME (3257-3306): Mother's first name
            SetField(sb, 3256, 50, "MARY");

            // MOMLNAME (3357-3406): Mother's last name
            SetField(sb, 3356, 50, $"FETTEST{sequenceNumber:D4}");

            // MOMMAIDN (3517-3566): Mother's maiden name
            SetField(sb, 3516, 50, "JOHNSON");

            // HOSP_D (2905-2954): Hospital of delivery
            SetField(sb, 2904, 50, "MERCY HOSPITAL");

            // ADDRESS_D (3052-3101): Address of delivery
            SetField(sb, 3051, 50, "789 HOSPITAL DRIVE");

            // ZIPCODE_D (3102-3110): ZIP code of delivery
            SetField(sb, 3101, 9, "481201234");

            // CITY_D (3139-3166): City of delivery
            SetField(sb, 3138, 28, "DETROIT");

            // CNTY_D (3111-3138): County of delivery
            SetField(sb, 3110, 28, "WAYNE");

            return sb.ToString();
        }

        private void SetField(StringBuilder sb, int startIndex, int length, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "";
            }

            // Pad or truncate to fit the field length
            value = value.PadRight(length).Substring(0, length);

            for (int i = 0; i < length; i++)
            {
                sb[startIndex + i] = value[i];
            }
        }

        /// <summary>
        /// Generates all three types of test files
        /// </summary>
        public void GenerateAllTestFiles(string outputDirectory, int recordsPerFile = 5, string stateCode = "LOCALHOST")
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var now = DateTime.Now;
            string timestamp = $"{now.Year}_{now:yyyy_MM_dd}_{stateCode.ToUpper()}";

            string morPath = Path.Combine(outputDirectory, $"{timestamp}.MOR");
            string natPath = Path.Combine(outputDirectory, $"{timestamp}.NAT");
            string fetPath = Path.Combine(outputDirectory, $"{timestamp}.FET");

            GenerateMORFile(morPath, recordsPerFile);
            GenerateNATFile(natPath, recordsPerFile);
            GenerateFETFile(fetPath, recordsPerFile);

            Console.WriteLine($"\nAll test files generated in: {outputDirectory}");
        }
    }
}
