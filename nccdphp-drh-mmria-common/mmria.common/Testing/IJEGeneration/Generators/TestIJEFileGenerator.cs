using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using mmria.common.Testing.IJEGeneration.Models;

namespace mmria.common.Testing.IJEGeneration.Generators
{
    /// <summary>
    /// Generates test IJE files (.MOR, .NAT, .FET) based on the format specifications
    /// reverse-engineered from the BatchItemProcessor parsing code.
    /// </summary>
    public class TestIJEFileGenerator
    {
        private Random _random;

        private static readonly HashSet<string> DirectionTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "N", "S", "E", "W", "NE", "NW", "SE", "SW"
        };

        private static readonly HashSet<string> StreetDesignators = new(StringComparer.OrdinalIgnoreCase)
        {
            "ALY", "AVE", "AV", "BLVD", "CIR", "CT", "DR", "HWY", "LN", "PKWY", "PL", "PLZ", "RD", "SQ", "ST", "TER", "TRL", "WAY"
        };

        private record Address(string Street, string AptOrSuite, string City, string County, string Zip5, string StateText)
        {
            public string Zip9 => Zip5.PadRight(9).Substring(0, 9);
        }

        private record ParsedStreet(string StreetNumber, string PreDirection, string StreetName, string StreetDesignator, string PostDirection);

        private static readonly Dictionary<string, List<Address>> StatePublicAddresses = new()
        {
            { "MI", new List<Address>
                {
                    new Address("1100 W MICHIGAN AVE", "", "ANN ARBOR", "WASHTENAW", "48109", "MICHIGAN"),
                    new Address("2 WOODWARD AVE", "", "DETROIT", "WAYNE", "48226", "MICHIGAN"),
                    new Address("303 MONROE AVE NW", "", "GRAND RAPIDS", "KENT", "49503", "MICHIGAN"),
                    new Address("100 MUSEUM DR", "", "KALAMAZOO", "KALAMAZOO", "49007", "MICHIGAN"),
                    new Address("702 W KALAMAZOO ST", "", "LANSING", "INGHAM", "48915", "MICHIGAN")
                }
            },
            { "CA", new List<Address>
                {
                    new Address("1 DR CARLTON B GOODLETT PL", "", "SAN FRANCISCO", "SAN FRANCISCO", "94102", "CALIFORNIA"),
                    new Address("500 S GRAND AVE", "", "LOS ANGELES", "LOS ANGELES", "90071", "CALIFORNIA"),
                    new Address("2000 K ST", "", "SACRAMENTO", "SACRAMENTO", "95814", "CALIFORNIA"),
                    new Address("100 AQUARIUM WAY", "", "LONG BEACH", "LOS ANGELES", "90802", "CALIFORNIA"),
                    new Address("1021 E BROADWAY", "", "GLENDALE", "LOS ANGELES", "91205", "CALIFORNIA")
                }
            },
            { "NY", new List<Address>
                {
                    new Address("200 CENTRAL PARK WEST", "", "NEW YORK", "NEW YORK", "10024", "NEW YORK"),
                    new Address("30 ROCKEFELLER PLAZA", "", "NEW YORK", "NEW YORK", "10112", "NEW YORK"),
                    new Address("1000 5TH AVE", "", "NEW YORK", "NEW YORK", "10028", "NEW YORK"),
                    new Address("1 MUSEUM MILE", "", "NEW YORK", "NEW YORK", "10029", "NEW YORK"),
                    new Address("99 MARGARET CORBIN DR", "", "NEW YORK", "NEW YORK", "10040", "NEW YORK")
                }
            },
            { "TX", new List<Address>
                {
                    new Address("1100 CONGRESS AVE", "", "AUSTIN", "TRAVIS", "78701", "TEXAS"),
                    new Address("600 MARKET ST", "", "HOUSTON", "HARRIS", "77002", "TEXAS"),
                    new Address("1717 N HARWOOD ST", "", "DALLAS", "DALLAS", "75201", "TEXAS"),
                    new Address("739 E CESAR E CHAVEZ BLVD", "", "SAN ANTONIO", "BEXAR", "78205", "TEXAS"),
                    new Address("1200 EPCC TRANS MOUNTAIN", "", "EL PASO", "EL PASO", "79924", "TEXAS")
                }
            },
            { "FL", new List<Address>
                {
                    new Address("200 MUSEUM DR", "", "TALLAHASSEE", "LEON", "32310", "FLORIDA"),
                    new Address("14890 SW 179TH AVE", "", "MIAMI", "MIAMI-DADE", "33187", "FLORIDA"),
                    new Address("400 BROAD ST", "", "JACKSONVILLE", "DUVAL", "32202", "FLORIDA"),
                    new Address("777 W CEDAR ST", "", "ORLANDO", "ORANGE", "32801", "FLORIDA"),
                    new Address("420 W KENNEDY BLVD", "", "TAMPA", "HILLSBOROUGH", "33606", "FLORIDA")
                }
            },
            { "IL", new List<Address>
                {
                    new Address("111 S MICHIGAN AVE", "", "CHICAGO", "COOK", "60603", "ILLINOIS"),
                    new Address("233 S WACKER DR", "", "CHICAGO", "COOK", "60606", "ILLINOIS"),
                    new Address("2121 S GOEBBERT RD", "", "ARLINGTON HEIGHTS", "COOK", "60005", "ILLINOIS"),
                    new Address("1 SE OLD STATE CAPITOL PLZ", "", "SPRINGFIELD", "SANGAMON", "62701", "ILLINOIS"),
                    new Address("1200 S LAKE SHORE DR", "", "CHICAGO", "COOK", "60605", "ILLINOIS")
                }
            },
            { "WA", new List<Address>
                {
                    new Address("1483 ALASKAN WAY", "", "SEATTLE", "KING", "98101", "WASHINGTON"),
                    new Address("325 5TH AVE N", "", "SEATTLE", "KING", "98109", "WASHINGTON"),
                    new Address("800 OCCIDENTAL AVE S", "", "SEATTLE", "KING", "98134", "WASHINGTON"),
                    new Address("2550 S MYRTLE ST", "", "SEATTLE", "KING", "98108", "WASHINGTON"),
                    new Address("3015 NW 54TH ST", "", "SEATTLE", "KING", "98107", "WASHINGTON")
                }
            },
            { "MA", new List<Address>
                {
                    new Address("24 BEACON ST", "", "BOSTON", "SUFFOLK", "02133", "MASSACHUSETTS"),
                    new Address("1 SCIENCE PARK", "", "BOSTON", "SUFFOLK", "02114", "MASSACHUSETTS"),
                    new Address("4 YAWKEY WAY", "", "BOSTON", "SUFFOLK", "02215", "MASSACHUSETTS"),
                    new Address("465 HUNTINGTON AVE", "", "BOSTON", "SUFFOLK", "02115", "MASSACHUSETTS"),
                    new Address("1 FRANKLIN PARK RD", "", "BOSTON", "SUFFOLK", "02121", "MASSACHUSETTS")
                }
            },
            { "GA", new List<Address>
                {
                    new Address("225 BAKER ST NW", "", "ATLANTA", "FULTON", "30313", "GEORGIA"),
                    new Address("1280 PEACHTREE ST NE", "", "ATLANTA", "FULTON", "30309", "GEORGIA"),
                    new Address("660 PEACHTREE ST NE", "", "ATLANTA", "FULTON", "30308", "GEORGIA"),
                    new Address("4000 SUWANEE DAM RD", "", "SUWANEE", "GWINNETT", "30024", "GEORGIA"),
                    new Address("1 MUSEUM DR", "", "COLUMBUS", "MUSCOGEE", "31901", "GEORGIA")
                }
            },
            { "OH", new List<Address>
                {
                    new Address("1 CAPITOL SQUARE", "", "COLUMBUS", "FRANKLIN", "43215", "OHIO"),
                    new Address("800 E 17TH AVE", "", "COLUMBUS", "FRANKLIN", "43211", "OHIO"),
                    new Address("5200 EMERALD PKWY", "", "DUBLIN", "FRANKLIN", "43017", "OHIO"),
                    new Address("1000 BROAD ST", "", "CLEVELAND", "CUYAHOGA", "44115", "OHIO"),
                    new Address("44 W 6TH ST", "", "CINCINNATI", "HAMILTON", "45202", "OHIO")
                }
            }
        };
        
        // Reference data for realistic randomization
        private static readonly string[] LastNames = {
            "SMITH", "JOHNSON", "WILLIAMS", "BROWN", "JONES", "GARCIA", "MILLER", "DAVIS",
            "RODRIGUEZ", "MARTINEZ", "HERNANDEZ", "LOPEZ", "GONZALEZ", "WILSON", "ANDERSON",
            "THOMAS", "TAYLOR", "MOORE", "JACKSON", "MARTIN", "LEE", "PEREZ", "THOMPSON", "WHITE",
            "KIM", "PATEL", "ALI", "SINGH", "KHAN", "NGUYEN", "CHEN", "HARRIS", "LEWIS", "CLARK",
            "WALKER", "YOUNG", "ALLEN", "KING", "WRIGHT", "SCOTT", "TORRES", "RAMIREZ", "KELLY",
            "SANDERS", "PRICE", "EDWARDS", "MURPHY", "COOPER", "STEWART", "RIVERA", "PHILLIPS",
            "EVANS", "JONES", "BAKER", "TURNER", "CAMPBELL", "PARK", "ZHANG", "LI", "SATO", "YAMAMOTO",
            "N'DIAYE", "DIALLO", "OKAFOR", "NWANKWO", "SILVA", "SOUZA", "FERREIRA", "ROSSI", "ESPOSITO",
            "IVANOV", "PETROV", "POPOV", "NOVAK", "HORVAT", "KOWALSKI", "NOWAK", "SCHMIDT", "MÜLLER"
        };
        
        private static readonly string[] FirstNames = {
            "MARY", "PATRICIA", "JENNIFER", "LINDA", "ELIZABETH", "BARBARA", "SUSAN",
            "JESSICA", "SARAH", "KAREN", "NANCY", "LISA", "BETTY", "MARGARET", "SANDRA",
            "ASHLEY", "KIMBERLY", "EMILY", "DONNA", "MICHELLE", "DOROTHY", "CAROL", "AMANDA", "MELISSA",
            "ALICE", "VICTORIA", "GRACE", "SOFIA", "ISABELLA", "MIA", "AVA", "AALIYAH", "LAYLA", "ZOE",
            "PRIYA", "ANITA", "RANI", "FATIMA", "AISHA", "NOOR", "SARA", "YASMIN", "AMINA", "LEILA",
            "CHIHIRO", "AKIKO", "MEI", "YUNA", "HANNA", "INGRID", "ANNA", "KATARINA", "OLGA", "NATALIA",
            "LI", "LING", "XIAO", "WEI", "MIN", "HYEJIN", "SOO MIN", "JIYOUNG", "YURI", "NAOMI",
            "AMARA", "ZAINAB", "CHIOMA", "LUCIA", "CARLA", "DANIELA", "ROSA", "ELENA",
            "BRUNA", "GABRIELA", "FRANCESCA", "GIULIA", "EMILIA", "MAYA", "RUBY", "CHLOE"
        };
        
        private static readonly string[] MiddleNames = {
            "ANN", "MARIE", "LYNN", "ROSE", "JANE", "SUE", "JEAN", "LOUISE", "ELIZABETH",
            "KAY", "GRACE", "MAE", "DAWN", "NICOLE", "RENEE", "MICHELLE", "CLAIRE",
            "ANA", "MARIA", "SOFIA", "RUTH", "HOPE", "FAITH", "JOY", "BELLA", "ELLE",
            "MEI", "YUN", "HYE", "YUKI", "NIA", "ZARA", "AMAL", "NOOR", "LUCIA"
        };
        
        
        


        private static Address GetRandomPublicAddress(Dictionary<string, List<Address>> dict, string stateCode, Random rnd)
        {
            var normalizedStateCode = string.IsNullOrWhiteSpace(stateCode)
                ? string.Empty
                : stateCode.Trim().ToUpperInvariant();

            if (!dict.TryGetValue(normalizedStateCode, out var list))
            {
                var keys = new List<string>(dict.Keys);
                var randomKey = keys[rnd.Next(keys.Count)];
                list = dict[randomKey];
            }

            return list[rnd.Next(list.Count)];
        }

        private static string ExtractStreetNumber(string fullStreet)
        {
            var parts = fullStreet.Split(' ');
            return parts.Length > 0 ? parts[0] : "";
        }

        private static ParsedStreet ParseStreet(string fullStreet)
        {
            if (string.IsNullOrWhiteSpace(fullStreet))
            {
                return new ParsedStreet(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            var tokens = fullStreet
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (tokens.Length == 0)
            {
                return new ParsedStreet(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            var streetNumber = tokens[0];
            var startIndex = 1;
            var endIndex = tokens.Length - 1;
            var preDirection = string.Empty;
            var streetDesignator = string.Empty;
            var postDirection = string.Empty;

            if (startIndex <= endIndex && DirectionTokens.Contains(tokens[startIndex]))
            {
                preDirection = tokens[startIndex];
                startIndex++;
            }

            if (startIndex <= endIndex && DirectionTokens.Contains(tokens[endIndex]))
            {
                postDirection = tokens[endIndex];
                endIndex--;
            }

            if (startIndex <= endIndex && StreetDesignators.Contains(tokens[endIndex]))
            {
                streetDesignator = tokens[endIndex];
                endIndex--;
            }

            var streetName = startIndex <= endIndex
                ? string.Join(" ", tokens, startIndex, endIndex - startIndex + 1)
                : string.Empty;

            return new ParsedStreet(streetNumber, preDirection, streetName, streetDesignator, postDirection);
        }

        public TestIJEFileGenerator(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Generates a test .MOR (Mortality) file with sample data
        /// Total record length: 5000 characters
        /// </summary>
        public IReadOnlyList<string> GenerateMORRecords(int recordCount = 5, IReadOnlyList<string>? jurisdictionSampling = null, IReadOnlyList<int>? yearOfDeathSampling = null)
        {
            var records = new List<string>();
            var sampledStateCodes = new List<string>();
            var sampledDeathYears = new List<int>();

            if (jurisdictionSampling != null && jurisdictionSampling.Count > 0)
            {
                sampledStateCodes = BuildSampledStateAssignments(recordCount, jurisdictionSampling, "jurisdiction");
            }
            else
            {
                var stateKeys = new List<string>(StatePublicAddresses.Keys);
                for (int i = 0; i < recordCount; i++)
                {
                    sampledStateCodes.Add(stateKeys[_random.Next(stateKeys.Count)]);
                }
            }

            if (yearOfDeathSampling != null && yearOfDeathSampling.Count > 0)
            {
                sampledDeathYears = BuildSampledYearAssignments(recordCount, yearOfDeathSampling);
            }
            else
            {
                for (int i = 0; i < recordCount; i++)
                {
                    sampledDeathYears.Add(2023);
                }
            }

            for (int i = 0; i < recordCount; i++)
            {
                records.Add(GenerateMORRecord(i + 1, sampledStateCodes[i], sampledDeathYears[i]));
            }

            return records;
        }

        public void GenerateMORFile(string filePath, int recordCount = 5, IReadOnlyList<string>? jurisdictionSampling = null, IReadOnlyList<int>? yearOfDeathSampling = null)
        {
            var records = GenerateMORRecords(recordCount, jurisdictionSampling, yearOfDeathSampling);
            File.WriteAllLines(filePath, records);
        }

        /// <summary>
        /// Generates a test .NAT (Natality/Birth) file with sample data
        /// Total record length varies by year, typically ~5000 characters
        /// </summary>
        public IReadOnlyList<string> GenerateNATRecords(int recordCount = 5)
        {
            var records = new List<string>();

            for (int i = 0; i < recordCount; i++)
            {
                records.Add(GenerateNATRecord(i + 1));
            }

            return records;
        }

        public void GenerateNATFile(string filePath, int recordCount = 5)
        {
            var records = GenerateNATRecords(recordCount);
            File.WriteAllLines(filePath, records);
        }

        /// <summary>
        /// Generates a test .FET (Fetal Death) file with sample data
        /// Total record length: ~5000 characters
        /// </summary>
        public IReadOnlyList<string> GenerateFETRecords(int recordCount = 5, IReadOnlyList<string>? jurisdictionSampling = null, IReadOnlyList<int>? yearOfDeathSampling = null)
        {
            var records = new List<string>();
            var sampledStateCodes = new List<string>();
            var sampledDeathYears = new List<int>();

            if (jurisdictionSampling != null && jurisdictionSampling.Count > 0)
            {
                sampledStateCodes = BuildSampledStateAssignments(recordCount, jurisdictionSampling, "jurisdiction");
            }
            else
            {
                for (int i = 0; i < recordCount; i++)
                {
                    sampledStateCodes.Add("MI");
                }
            }

            if (yearOfDeathSampling != null && yearOfDeathSampling.Count > 0)
            {
                sampledDeathYears = BuildSampledYearAssignments(recordCount, yearOfDeathSampling);
            }
            else
            {
                for (int i = 0; i < recordCount; i++)
                {
                    sampledDeathYears.Add(2023);
                }
            }

            for (int i = 0; i < recordCount; i++)
            {
                records.Add(GenerateFETRecord(i + 1, sampledStateCodes[i], sampledDeathYears[i]));
            }

            return records;
        }

        public void GenerateFETFile(string filePath, int recordCount = 5, IReadOnlyList<string>? jurisdictionSampling = null, IReadOnlyList<int>? yearOfDeathSampling = null)
        {
            var records = GenerateFETRecords(recordCount, jurisdictionSampling, yearOfDeathSampling);
            File.WriteAllLines(filePath, records);
        }

        private List<string> BuildSampledStateAssignments(int recordCount, IReadOnlyList<string> samplingValues, string label)
        {
            var pool = new List<string>();

            foreach (var item in samplingValues)
            {
                var normalized = NormalizeStateCode(item);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (!pool.Contains(normalized))
                {
                    pool.Add(normalized);
                }
            }

            if (pool.Count == 0)
            {
                pool.Add("MI");
            }

            return BuildGuaranteedSpread(recordCount, pool, label);
        }

        private List<int> BuildSampledYearAssignments(int recordCount, IReadOnlyList<int> samplingValues)
        {
            var pool = new List<int>();

            foreach (var year in samplingValues)
            {
                if (!pool.Contains(year))
                {
                    pool.Add(year);
                }
            }

            if (pool.Count == 0)
            {
                pool.Add(2023);
            }

            return BuildGuaranteedSpread(recordCount, pool, "year-of-death");
        }

        private List<T> BuildGuaranteedSpread<T>(int recordCount, IReadOnlyList<T> pool, string label)
        {
            var assignments = new List<T>(recordCount);
            if (recordCount <= 0)
            {
                return assignments;
            }

            if (recordCount >= pool.Count)
            {
                assignments.AddRange(pool);
            }
            else
            {
                var shuffledPool = new List<T>(pool);
                ShuffleList(shuffledPool);
                assignments.AddRange(shuffledPool.GetRange(0, recordCount));
            }

            while (assignments.Count < recordCount)
            {
                assignments.Add(pool[_random.Next(pool.Count)]);
            }

            ShuffleList(assignments);
            return assignments;
        }

        private void ShuffleList<T>(IList<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        private string NormalizeStateCode(string stateCode)
        {
            if (string.IsNullOrWhiteSpace(stateCode))
            {
                return string.Empty;
            }

            var normalized = stateCode.Trim().ToUpperInvariant();
            return normalized.Length >= 2 ? normalized.Substring(0, 2) : normalized;
        }

        // Helper methods for generating random realistic data
        private string GetRandomSSN(int sequenceNumber)
        {
            // Use sequence number to ensure consistency across MOR/NAT/FET for same person
            // Format: sequence (3 digits) + derived digits (6 digits) for uniqueness
            var derived = (sequenceNumber * 123456) % 1000000; // Deterministic based on sequence
            return $"{sequenceNumber:D3}{derived:D6}";
        }

        private string GetRandomElement(string[] array)
        {
            return array[_random.Next(array.Length)];
        }

        private string GetRandomDate(int year, int monthStart = 1, int monthEnd = 12)
        {
            int month = _random.Next(monthStart, monthEnd + 1);
            int maxDay = DateTime.DaysInMonth(year, month);
            int day = _random.Next(1, maxDay + 1);
            return $"{year:D4}-{month:D2}-{day:D2}";
        }

        private string GetRandomTime()
        {
            return $"{_random.Next(0, 24):D2}{_random.Next(0, 60):D2}";
        }

        private int GetRandomAge(int min = 15, int max = 50)
        {
            return _random.Next(min, max + 1);
        }

        private string GetRandomBirthWeight(int min = 2000, int max = 4500)
        {
            return _random.Next(min, max + 1).ToString("D4");
        }

        private int GetRandomGestationalAge(int min = 20, int max = 42)
        {
            return _random.Next(min, max + 1);
        }

        private string GetRandomApgarScore()
        {
            // Most births have good Apgar scores (7-10)
            int score = _random.Next(100) < 85 ? _random.Next(7, 11) : _random.Next(0, 11);
            return score.ToString("D2");
        }

        private string GetRandomZipCode()
        {
            return $"48{_random.Next(100, 200):D3}{_random.Next(1000, 9999):D4}";
        }

        private string GetRandomStreetNumber()
        {
            return _random.Next(100, 9999).ToString();
        }

        private string GetRandomHeight()
        {
            // Height in feet (4-6) and inches (0-11)
            return $"{_random.Next(4, 7)}{_random.Next(0, 12):D2}";
        }

        private int GetRandomWeight(int min = 100, int max = 250)
        {
            return _random.Next(min, max + 1);
        }

        private string GetRandomEducation()
        {
            // 1=8th grade or less, 2=9-12 no diploma, 3=HS grad, 4=Some college, 5=Associate, 6=Bachelor, 7=Master, 8=Doctorate
            string[] eduLevels = { "1", "2", "3", "4", "5", "6", "7", "8" };
            return eduLevels[_random.Next(eduLevels.Length)];
        }

        private string GetRandomMaritalStatus()
        {
            string[] statuses = { "M", "S", "W", "D", "U" };
            return statuses[_random.Next(statuses.Length)];
        }

        private string GetRandomYesNo()
        {
            return _random.Next(2) == 0 ? "Y" : "N";
        }

        private string GetRandomYesNoUnknown()
        {
            string[] values = { "Y", "N", "U" };
            return values[_random.Next(values.Length)];
        }

        private int GetRandomPreviousBirths()
        {
            // Most women have 0-3 previous births
            return _random.Next(100) < 80 ? _random.Next(0, 4) : _random.Next(4, 9);
        }

        private string GetRandomCigarettes()
        {
            // Most don't smoke or smoke moderately
            int value = _random.Next(100);
            if (value < 70) return "00"; // Non-smoker
            if (value < 90) return _random.Next(1, 11).ToString("D2"); // Light smoker
            if (value < 95) return _random.Next(11, 31).ToString("D2"); // Moderate
            return _random.Next(31, 51).ToString("D2"); // Heavy
        }

        private string GenerateMORRecord(int sequenceNumber, string stateOfDeathSampling, int yearOfDeathSampling)
        {
            var sb = new StringBuilder(new string(' ', 5000));

            // Generate random but realistic data
            var age = GetRandomAge(15, 50);
            var deathYear = yearOfDeathSampling > 0 ? yearOfDeathSampling : 2023;
            var birthYear = deathYear - age;
            var deathDate = GetRandomDate(deathYear, 1, 12);
            var deathParts = deathDate.Split('-');
            var birthDate = GetRandomDate(birthYear, 1, 12);
            var birthParts = birthDate.Split('-');
            var ssn = GetRandomSSN(sequenceNumber);
            var firstName = GetRandomElement(FirstNames);
            var lastName = GetRandomElement(LastNames);
            var middleName = GetRandomElement(MiddleNames);
            

            // DOD_YR (1-4): Year of death
            SetField(sb, 0, 4, deathParts[0]);

            // DSTATE (5-6): State of death
            var normalizedStateCode = NormalizeStateCode(stateOfDeathSampling);
            if (string.IsNullOrWhiteSpace(normalizedStateCode))
            {
                var stateKeys = new List<string>(StatePublicAddresses.Keys);
                normalizedStateCode = stateKeys[_random.Next(stateKeys.Count)];
            }
            SetField(sb, 4, 2, normalizedStateCode);

            // FILENO (7-12): Certificate number
            SetField(sb, 6, 6, sequenceNumber.ToString("D6"));

            // AUXNO (14-25): Auxiliary number
            SetField(sb, 13, 12, (sequenceNumber * 100 + _random.Next(100)).ToString("D12"));

            // GNAME (27-76): Given/First name
            SetField(sb, 26, 50, firstName);

            // LNAME (78-127): Last name
            SetField(sb, 77, 50, lastName);

            // SSN (191-199): Social Security Number
            SetField(sb, 190, 9, ssn);

            // AGETYPE (200): Age type (1=years, 2=months, 4=days, 5=hours, 6=minutes, 9=not stated)
            SetField(sb, 199, 1, "1");

            // AGE (201-203): Age
            SetField(sb, 200, 3, age.ToString("D3"));

            // DOB_YR (205-208): Year of birth
            SetField(sb, 204, 4, birthParts[0]);

            // DOB_MO (209-210): Month of birth
            SetField(sb, 208, 2, birthParts[1]);

            // DOB_DY (211-212): Day of birth
            SetField(sb, 210, 2, birthParts[2]);

            // BPLACE_CNT (213-214): Country of birth
            SetField(sb, 212, 2, "US");

            // BPLACE_ST (215-216): State of birth
            SetField(sb, 214, 2, "MI");

            // CITYC (217-221): City code
            SetField(sb, 216, 5, _random.Next(10000, 99999).ToString());

            // COUNTYC (222-224): County code
            SetField(sb, 221, 3, _random.Next(100, 999).ToString());

            // STATEC (225-226): State code
            SetField(sb, 224, 2, normalizedStateCode);

            // COUNTRYC (227-228): Country code
            SetField(sb, 226, 2, "US");

            // MARITAL (230): Marital status
            SetField(sb, 229, 1, GetRandomMaritalStatus());

            // DPLACE (232): Place of death
            SetField(sb, 231, 1, _random.Next(1, 8).ToString());

            // COD (233-235): County of death
            SetField(sb, 232, 3, _random.Next(100, 999).ToString());

            // DOD_MO (237-238): Month of death
            SetField(sb, 236, 2, deathParts[1]);

            // DOD_DY (239-240): Day of death
            SetField(sb, 238, 2, deathParts[2]);

            // TOD (241-244): Time of death (HHMM)
            SetField(sb, 240, 4, GetRandomTime());

            // DEDUC (245): Decedent's education
            SetField(sb, 244, 1, GetRandomEducation());

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

            // Define occupation and industry arrays for randomization
            string[] occupations = new[]
            {
                "REGISTERED NURSE", "TEACHER", "SOFTWARE ENGINEER", "RETAIL SALES",
                "ADMINISTRATIVE ASSISTANT", "CASHIER", "TRUCK DRIVER", "ACCOUNTANT",
                "ELECTRICIAN", "WAITRESS", "CONSTRUCTION WORKER", "MANAGER",
                "MECHANIC", "CUSTOMER SERVICE REP", "MEDICAL ASSISTANT", "FACTORY WORKER"
            };

            string[] industries = new[]
            {
                "HEALTHCARE", "EDUCATION", "COMPUTER PROGRAMMING", "RETAIL TRADE",
                "BUSINESS ADMINISTRATION", "FOOD SERVICE", "TRANSPORTATION",
                "ACCOUNTING SERVICES", "CONSTRUCTION", "RESTAURANTS",
                "BUILDING CONSTRUCTION", "CORPORATE MANAGEMENT",
                "AUTO REPAIR", "TELECOMMUNICATIONS", "MEDICAL SERVICES", "MANUFACTURING"
            };

            string[] causesOfDeath = new[]
            {
                "ACUTE MYOCARDIAL INFARCTION", "ACUTE RESPIRATORY FAILURE", 
                "CEREBROVASCULAR ACCIDENT", "PULMONARY EMBOLISM",
                "CARDIAC ARREST", "SEPTIC SHOCK", "HEMORRHAGIC SHOCK",
                "RESPIRATORY ARREST", "MULTIPLE ORGAN FAILURE", "CARDIAC ARRHYTHMIA"
            };

            string[] underlyingCauses = new[]
            {
                "CORONARY ARTERY DISEASE", "PNEUMONIA", "ATHEROSCLEROSIS",
                "DEEP VEIN THROMBOSIS", "CONGESTIVE HEART FAILURE", "SEPSIS",
                "TRAUMA", "CHRONIC OBSTRUCTIVE PULMONARY DISEASE", "DIABETES MELLITUS",
                "HYPERTENSIVE HEART DISEASE"
            };

            string[] otherConditions = new[]
            {
                "HYPERTENSION", "DIABETES MELLITUS TYPE 2", "HYPERLIPIDEMIA",
                "CHRONIC KIDNEY DISEASE", "OBESITY", "CORONARY ARTERY DISEASE",
                "ATRIAL FIBRILLATION", "DEPRESSION", "ASTHMA", "ARTHRITIS"
            };

            string[] intervals = new[]
            {
                "MINUTES", "HOURS", "1 DAY", "2 DAYS", "3 DAYS", "1 WEEK",
                "2 WEEKS", "1 MONTH", "2 MONTHS", "6 MONTHS", "1 YEAR", "2 YEARS"
            };

            // OCCUP (575-614): Occupation
            SetField(sb, 574, 40, GetRandomElement(occupations));

            // INDUST (618-657): Industry
            SetField(sb, 617, 40, GetRandomElement(industries));

            // MANNER (701): Manner of death (N=Natural, A=Accident, S=Suicide, H=Homicide, P=Pending, C=Could not be determined)
            var manners = new[] { "N", "N", "N", "N", "A", "S", "H" }; // Weighted toward Natural
            SetField(sb, 700, 1, GetRandomElement(manners));

            // AUTOP (976): Autopsy performed
            SetField(sb, 975, 1, GetRandomYesNo());

            // AUTOPF (977): Autopsy findings available
            SetField(sb, 976, 1, GetRandomYesNo());

            // TOBAC (978): Tobacco use contributed
            SetField(sb, 977, 1, GetRandomYesNoUnknown());

            // PREG (979): Pregnancy status (1=Not pregnant, 2=Pregnant at death, etc.)
            SetField(sb, 978, 1, _random.Next(1, 5).ToString());

            // ARMEDF (1081): Armed Forces
            SetField(sb, 1080, 1, GetRandomYesNo());

            // Death address fields
            var addr = GetRandomPublicAddress(StatePublicAddresses, normalizedStateCode, _random);
            var parsedStreet = ParseStreet(addr.Street);

            // STNUM_D (1162-1171): Street number
            SetField(sb, 1161, 10, parsedStreet.StreetNumber);

            // PREDIR_D (1172-1181): Street pre-direction
            SetField(sb, 1171, 10, parsedStreet.PreDirection);

            // STNAME_D (1182-1231): Street name
            SetField(sb, 1181, 50, parsedStreet.StreetName);

            // STDESIG_D (1232-1241): Street designator
            SetField(sb, 1231, 10, parsedStreet.StreetDesignator);

            // POSTDIR_D (1242-1251): Street post-direction
            SetField(sb, 1241, 10, parsedStreet.PostDirection);

            // CITYTEXT_D (1252-1279): City
            SetField(sb, 1251, 28, addr.City);

            // STATETEXT_D (1280-1307): State
            SetField(sb, 1279, 28, addr.StateText);

            // ZIP9_D (1308-1316): ZIP code
            SetField(sb, 1307, 9, addr.Zip9);

            // COUNTYTEXT_D (1317-1344): County
            SetField(sb, 1316, 28, addr.County);

            // Residence address fields
            // STNUM_R (1485-1494): Street number
            SetField(sb, 1484, 10, parsedStreet.StreetNumber);

            // PREDIR_R (1495-1504): Street pre-direction
            SetField(sb, 1494, 10, parsedStreet.PreDirection);

            // STNAME_R (1505-1532): Street name
            SetField(sb, 1504, 28, parsedStreet.StreetName);

            // STDESIG_R (1533-1542): Street designator
            SetField(sb, 1532, 10, parsedStreet.StreetDesignator);

            // POSTDIR_R (1543-1552): Street post-direction
            SetField(sb, 1542, 10, parsedStreet.PostDirection);

            // CITYTEXT_R (1560-1587): City
            SetField(sb, 1559, 28, addr.City);

            // ZIP9_R (1588-1596): ZIP code
            SetField(sb, 1587, 9, addr.Zip9);

            // COUNTYTEXT_R (1597-1624): County
            SetField(sb, 1596, 28, addr.County);

            // DMIDDLE (1808-1857): Middle name
            SetField(sb, 1807, 50, middleName);

            // DMAIDEN (3342-3391): Maiden name
            SetField(sb, 3341, 50, GetRandomElement(LastNames));

            // COD1A (2542-2661): Cause of death Part I Line A (immediate cause)
            SetField(sb, 2541, 120, GetRandomElement(causesOfDeath));

            // INTERVAL1A (2662-2681): Interval Line A
            SetField(sb, 2661, 20, GetRandomElement(intervals));

            // COD1B (2682-2801): Cause of death Part I Line B (due to)
            var hasLineB = _random.Next(100) < 70; // 70% chance
            SetField(sb, 2681, 120, hasLineB ? GetRandomElement(underlyingCauses) : "");

            // INTERVAL1B (2802-2821): Interval Line B
            SetField(sb, 2801, 20, hasLineB ? GetRandomElement(intervals) : "");

            // COD1C (2822-2941): Cause of death Part I Line C (due to)
            var hasLineC = hasLineB && _random.Next(100) < 30; // 30% chance if B exists
            SetField(sb, 2821, 120, hasLineC ? GetRandomElement(underlyingCauses) : "");

            // COD1D (2962-3081): Cause of death Part I Line D (due to)
            SetField(sb, 2961, 120, ""); // Rarely used

            // OTHERCONDITION (3102-3341): Other significant conditions
            var hasOther = _random.Next(100) < 60; // 60% chance
            SetField(sb, 3101, 240, hasOther ? GetRandomElement(otherConditions) : "");

            // DBPLACECITY (3397-3424): Birthplace city (align to selected address)
            SetField(sb, 3396, 28, addr.City);

            // VRO_STATUS (4993): VRO status
            SetField(sb, 4992, 1, "0");

            return sb.ToString();
        }

        private string GenerateNATRecord(int sequenceNumber)
        {
            var sb = new StringBuilder(new string(' ', 4000));

            // Generate random but realistic data - use same SSN as MOR for linking
            var ssn = GetRandomSSN(sequenceNumber);
            var birthYear = 2023;
            var birthDate = GetRandomDate(birthYear, 1, 12);
            var birthParts = birthDate.Split('-');
            var motherAge = GetRandomAge(15, 45);
            var motherBirthYear = birthYear - motherAge;
            var motherBirthDate = GetRandomDate(motherBirthYear, 1, 12);
            var motherBirthParts = motherBirthDate.Split('-');
            var fatherAge = motherAge + _random.Next(-5, 10);
            if (fatherAge < 15) fatherAge = 15;
            var fatherBirthYear = birthYear - fatherAge;
            var fatherBirthDate = GetRandomDate(fatherBirthYear, 1, 12);
            var fatherBirthParts = fatherBirthDate.Split('-');
            var gestationalAge = GetRandomGestationalAge(20, 42);
            var birthWeight = GetRandomBirthWeight(2000, 4500);

            // IDOB_YR (1-4): Infant year of birth
            SetField(sb, 0, 4, birthParts[0]);

            // BSTATE (5-6): State of birth
            SetField(sb, 4, 2, "MI");

            // FILENO (7-12): Certificate number
            SetField(sb, 6, 6, sequenceNumber.ToString("D6"));

            // AUXNO (14-25): Auxiliary number
            SetField(sb, 13, 12, (sequenceNumber * 100 + _random.Next(100)).ToString("D12"));

            // TB (26-29): Time of birth (HHMM)
            SetField(sb, 25, 4, GetRandomTime());

            // IDOB_MO (31-32): Infant month of birth
            SetField(sb, 30, 2, birthParts[1]);

            // IDOB_DY (33-34): Infant day of birth
            SetField(sb, 32, 2, birthParts[2]);

            // BPLACE (38): Place of birth
            SetField(sb, 37, 1, _random.Next(1, 8).ToString());

            // FNPI (39-50): Facility NPI
            SetField(sb, 38, 12, _random.Next(100000000, 999999999).ToString() + _random.Next(100, 999).ToString());

            // MDOB_YR (55-58): Mother year of birth
            SetField(sb, 54, 4, motherBirthParts[0]);

            // MDOB_MO (59-60): Mother month of birth
            SetField(sb, 58, 2, motherBirthParts[1]);

            // MDOB_DY (61-62): Mother day of birth
            SetField(sb, 60, 2, motherBirthParts[2]);

            // BPLACEC_ST_TER (64-65): Mother's birthplace state
            SetField(sb, 63, 2, "MI");

            // BPLACEC_CNT (66-67): Mother's birthplace country
            SetField(sb, 65, 2, "US");

            // STATEC (76-77): County code
            SetField(sb, 75, 2, "26");

            // FDOB_YR (81-84): Father year of birth
            SetField(sb, 80, 4, fatherBirthParts[0]);

            // FDOB_MO (85-86): Father month of birth
            SetField(sb, 84, 2, fatherBirthParts[1]);

            // MARN (91): Parents married
            SetField(sb, 90, 1, GetRandomYesNo());

            // ACKN (92): Paternity acknowledged
            SetField(sb, 91, 1, GetRandomYesNo());

            // MEDUC (93): Mother's education
            SetField(sb, 92, 1, GetRandomEducation());

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
            SetField(sb, 421, 1, GetRandomEducation());

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
            SetField(sb, 750, 1, _random.Next(1, 6).ToString());

            // TRAN (752): Transfer
            SetField(sb, 751, 1, GetRandomYesNo());

            // Calculate prenatal visit dates (first visit ~8 weeks into pregnancy, last near delivery)
            var firstVisitMonth = birthParts[1];
            var firstVisitDay = _random.Next(1, 28).ToString("D2");
            var firstVisitYear = (birthYear - 1).ToString();
            if (int.Parse(birthParts[1]) > 4)
            {
                firstVisitMonth = (int.Parse(birthParts[1]) - 4).ToString("D2");
                firstVisitYear = birthParts[0];
            }

            // DOFP_MO (753-754): Date of first prenatal visit - month
            SetField(sb, 752, 2, firstVisitMonth);

            // DOFP_DY (755-756): Date of first prenatal visit - day
            SetField(sb, 754, 2, firstVisitDay);

            // DOFP_YR (757-760): Date of first prenatal visit - year
            SetField(sb, 756, 4, firstVisitYear);

            // DOLP_MO (761-762): Date of last prenatal visit - month (1-2 weeks before birth)
            var lastVisitMonth = birthParts[1];
            var lastVisitDay = Math.Max(1, int.Parse(birthParts[2]) - _random.Next(7, 14)).ToString("D2");
            SetField(sb, 760, 2, lastVisitMonth);

            // DOLP_DY (763-764): Date of last prenatal visit - day
            SetField(sb, 762, 2, lastVisitDay);

            // DOLP_YR (765-768): Date of last prenatal visit - year
            SetField(sb, 764, 4, birthParts[0]);

            // NPREV (769-770): Number of previous births now living
            SetField(sb, 768, 2, GetRandomPreviousBirths().ToString("D2"));

            // HFT (772): Mother's height - feet
            var heightParts = GetRandomHeight();
            SetField(sb, 771, 1, heightParts[0].ToString());

            // HIN (773-774): Mother's height - inches
            SetField(sb, 772, 2, heightParts[1].ToString());

            // PWGT (776-778): Mother's prepregnancy weight
            var preWeight = GetRandomWeight(100, 200);
            SetField(sb, 775, 3, preWeight.ToString());

            // DWGT (780-782): Mother's delivery weight (add 15-35 lbs)
            var deliveryWeight = preWeight + _random.Next(15, 36);
            SetField(sb, 779, 3, deliveryWeight.ToString());

            // WIC (784): WIC
            SetField(sb, 783, 1, GetRandomYesNo());

            // PLBL (785-786): Number of prenatal visits
            var prenatalVisits = _random.Next(8, 16);
            SetField(sb, 784, 2, prenatalVisits.ToString("D2"));

            // PLBD (787-788): Plurality - number born alive
            SetField(sb, 786, 2, "01");

            // POPO (789-790): Plurality - number born dead
            SetField(sb, 788, 2, "00");

            // CIGPN (803-804): Cigarettes before pregnancy
            var cigsBefore = GetRandomCigarettes();
            SetField(sb, 802, 2, cigsBefore);

            // CIGFN (805-806): Cigarettes first trimester
            var cigsBeforeNum = int.Parse(cigsBefore);
            var cigsFirst = cigsBeforeNum > 0 ? Math.Max(0, cigsBeforeNum - _random.Next(0, 5)).ToString("D2") : "00";
            SetField(sb, 804, 2, cigsFirst);

            // CIGSN (807-808): Cigarettes second trimester
            var cigsFirstNum = int.Parse(cigsFirst);
            var cigsSecond = cigsFirstNum > 0 ? Math.Max(0, cigsFirstNum - _random.Next(0, 3)).ToString("D2") : "00";
            SetField(sb, 806, 2, cigsSecond);

            // CIGLN (809-810): Cigarettes third trimester
            var cigsSecondNum = int.Parse(cigsSecond);
            var cigsThird = cigsSecondNum > 0 ? Math.Max(0, cigsSecondNum - _random.Next(0, 2)).ToString("D2") : "00";
            SetField(sb, 808, 2, cigsThird);

            // PAY (811): Payment source
            SetField(sb, 810, 1, _random.Next(1, 6).ToString());

            // Calculate last menstrual period (approximately 40 weeks before birth)
            var lmpYear = birthYear - 1;
            var lmpMonth = int.Parse(birthParts[1]);
            var lmpDay = int.Parse(birthParts[2]);
            if (lmpMonth > 3)
            {
                lmpMonth -= 3;
                lmpYear = birthYear;
            }
            else
            {
                lmpMonth = lmpMonth + 9;
            }

            // DLMP_YR (812-815): Date of last menses - year
            SetField(sb, 811, 4, lmpYear.ToString());

            // DLMP_MO (816-817): Date of last menses - month
            SetField(sb, 815, 2, lmpMonth.ToString("D2"));

            // DLMP_DY (818-819): Date of last menses - day
            SetField(sb, 817, 2, lmpDay.ToString("D2"));

            // Medical risk factors
            SetField(sb, 819, 1, GetRandomYesNoUnknown()); // PDIAB
            SetField(sb, 820, 1, GetRandomYesNoUnknown()); // GDIAB
            SetField(sb, 821, 1, GetRandomYesNoUnknown()); // PHYPE
            SetField(sb, 822, 1, GetRandomYesNoUnknown()); // GHYPE
            SetField(sb, 823, 1, GetRandomYesNoUnknown()); // PPB
            SetField(sb, 824, 1, GetRandomYesNoUnknown()); // PPO

            // NPCES (829-830): Number of previous cesareans
            var prevCesareans = _random.Next(0, 3);
            SetField(sb, 828, 2, prevCesareans.ToString("D2"));

            // Infections
            SetField(sb, 831, 1, GetRandomYesNoUnknown()); // GON
            SetField(sb, 832, 1, GetRandomYesNoUnknown()); // SYPH
            SetField(sb, 833, 1, GetRandomYesNoUnknown()); // HSV

            // Obstetric procedures
            SetField(sb, 853, 1, GetRandomYesNo()); // ATTF
            SetField(sb, 854, 1, GetRandomYesNo()); // ATTV
            SetField(sb, 855, 1, _random.Next(1, 4).ToString()); // PRES - Presentation
            SetField(sb, 856, 1, _random.Next(1, 5).ToString()); // ROUT - Route of delivery

            // BWG (865-868): Birth weight in grams
            SetField(sb, 864, 4, birthWeight.ToString());

            // OWGEST (870-871): Obstetric estimate of gestation
            SetField(sb, 869, 2, gestationalAge.ToString("D2"));

            // APGAR5 (873-874): 5-minute Apgar score
            SetField(sb, 872, 2, GetRandomApgarScore());

            // APGAR10 (875-876): 10-minute Apgar score
            SetField(sb, 874, 2, "99"); // Usually not assessed

            // PLUR (877-878): Plurality
            SetField(sb, 876, 2, "01");

            // SORD (879-880): Set order
            SetField(sb, 878, 2, "01");

            // ITRAN (909): Infant transferred
            SetField(sb, 908, 1, GetRandomYesNo());

            // ILIV (910): Infant living
            SetField(sb, 909, 1, "Y");

            // BFED (911): Breastfed
            SetField(sb, 910, 1, GetRandomYesNo());

            // MAGER (920-921): Mother's age
            SetField(sb, 919, 2, motherAge.ToString("D2"));

            // FAGER (922-923): Father's age
            SetField(sb, 921, 2, fatherAge.ToString("D2"));

            // Generate names
            var motherFirstName = GetRandomElement(FirstNames);
            var motherMiddleName = GetRandomElement(MiddleNames);
            var motherLastName = GetRandomElement(LastNames);
            var motherMaidenName = GetRandomElement(LastNames);
            var fatherFirstName = GetRandomElement(FirstNames);
            var fatherMiddleName = GetRandomElement(MiddleNames);
            var fatherLastName = GetRandomElement(LastNames);
            

            // MFNAME (1001-1050): Mother's first name
            SetField(sb, 1000, 50, motherFirstName);

            // MMNAME (1051-1100): Mother's middle name
            SetField(sb, 1050, 50, motherMiddleName);

            // MLNAME (1101-1150): Mother's last name
            SetField(sb, 1100, 50, motherLastName);

            // MMAIDEN (1151-1200): Mother's maiden name
            SetField(sb, 1150, 50, motherMaidenName);

            // FFNAME (1251-1300): Father's first name
            SetField(sb, 1250, 50, fatherFirstName);

            // FMNAME (1301-1350): Father's middle name
            SetField(sb, 1300, 50, fatherMiddleName);

            // FLNAME (1351-1400): Father's last name
            SetField(sb, 1350, 50, fatherLastName);

            // STNUM (1451-1457): Mother's residence street number
            var addrN = GetRandomPublicAddress(StatePublicAddresses, "", _random);
            SetField(sb, 1450, 7, ExtractStreetNumber(addrN.Street));

            // STNAME (1501-1550): Mother's residence street name
            SetField(sb, 1500, 50, addrN.Street);

            // CITYTEXT (1601-1628): Mother's residence city
            SetField(sb, 1600, 28, addrN.City);

            // COUNTYTEXT (1629-1657): Mother's residence county
            SetField(sb, 1628, 29, addrN.County);

            // ZIP9 (1687-1695): Mother's residence ZIP
            SetField(sb, 1686, 9, addrN.Zip9);

            // Mother's SSN (2000-2008): Must match MOR SSN for linking
            SetField(sb, 1999, 9, ssn);

            return sb.ToString();
        }

        private string GenerateFETRecord(int sequenceNumber, string stateOfDeathSampling, int yearOfDeathSampling)
        {
            var sb = new StringBuilder(new string(' ', 6000));

            // Generate random but realistic data - use same SSN as MOR for linking
            var ssn = GetRandomSSN(sequenceNumber);
            var deathYear = yearOfDeathSampling > 0 ? yearOfDeathSampling : 2023;
            var deathDate = GetRandomDate(deathYear, 1, 12);
            var deathParts = deathDate.Split('-');
            var motherAge = GetRandomAge(15, 45);
            var motherBirthYear = deathYear - motherAge;
            var motherBirthDate = GetRandomDate(motherBirthYear, 1, 12);
            var motherBirthParts = motherBirthDate.Split('-');
            var fatherAge = motherAge + _random.Next(-5, 10);
            if (fatherAge < 15) fatherAge = 15;
            var fatherBirthYear = deathYear - fatherAge;
            var fatherBirthDate = GetRandomDate(fatherBirthYear, 1, 12);
            var fatherBirthParts = fatherBirthDate.Split('-');
            var gestationalAge = GetRandomGestationalAge(20, 42);
            var birthWeight = GetRandomBirthWeight(500, 4000);

            // FDOD_YR (1-4): Year of death
            SetField(sb, 0, 4, deathParts[0]);

            // DSTATE (5-6): State
            var normalizedStateCode = NormalizeStateCode(stateOfDeathSampling);
            SetField(sb, 4, 2, string.IsNullOrWhiteSpace(normalizedStateCode) ? "MI" : normalizedStateCode);

            // FILENO (7-12): Certificate number
            SetField(sb, 6, 6, sequenceNumber.ToString("D6"));

            // AUXNO (14-25): Auxiliary number
            SetField(sb, 13, 12, (sequenceNumber * 100 + _random.Next(100)).ToString("D12"));

            // TD (26-29): Time of delivery (HHMM)
            SetField(sb, 25, 4, GetRandomTime());

            // FSEX (30): Sex of fetus
            SetField(sb, 29, 1, _random.Next(2) == 0 ? "M" : "F");

            // FDOD_MO (31-32): Month of death
            SetField(sb, 30, 2, deathParts[1]);

            // FDOD_DY (33-34): Day of death
            SetField(sb, 32, 2, deathParts[2]);

            // DPLACE (38): Place of delivery
            SetField(sb, 37, 1, _random.Next(1, 8).ToString());

            // FNPI (39-50): Facility NPI
            SetField(sb, 38, 12, _random.Next(100000000, 999999999).ToString() + _random.Next(100, 999).ToString());

            // MDOB_YR (55-58): Mother year of birth
            SetField(sb, 54, 4, motherBirthParts[0]);

            // MDOB_MO (59-60): Mother month of birth
            SetField(sb, 58, 2, motherBirthParts[1]);

            // MDOB_DY (61-62): Mother day of birth
            SetField(sb, 60, 2, motherBirthParts[2]);

            // BPLACEC_ST_TER (64-65): Mother's birthplace state
            SetField(sb, 63, 2, "MI");

            // BPLACEC_CNT (66-67): Mother's birthplace country
            SetField(sb, 65, 2, "US");

            // STATEC (76-77): County code
            SetField(sb, 75, 2, "26");

            // FDOB_YR (81-84): Father year of birth
            SetField(sb, 80, 4, fatherBirthParts[0]);

            // FDOB_MO (85-86): Father month of birth
            SetField(sb, 84, 2, fatherBirthParts[1]);

            // MARN (91): Parents married
            SetField(sb, 90, 1, GetRandomYesNo());

            // MEDUC (93): Mother's education
            SetField(sb, 92, 1, GetRandomEducation());

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
            SetField(sb, 421, 1, _random.Next(1, 6).ToString());

            // TRAN (423): Transfer
            SetField(sb, 422, 1, GetRandomYesNo());

            // Calculate prenatal visit dates (first visit ~8 weeks into pregnancy)
            var firstVisitMonth = deathParts[1];
            var firstVisitDay = _random.Next(1, 28).ToString("D2");
            var firstVisitYear = (deathYear - 1).ToString();
            if (int.Parse(deathParts[1]) > 4)
            {
                firstVisitMonth = (int.Parse(deathParts[1]) - 4).ToString("D2");
                firstVisitYear = deathParts[0];
            }

            // DOFP_MO (424-425): Date of first prenatal visit - month
            SetField(sb, 423, 2, firstVisitMonth);

            // DOFP_DY (426-427): Date of first prenatal visit - day
            SetField(sb, 425, 2, firstVisitDay);

            // DOFP_YR (428-431): Date of first prenatal visit - year
            SetField(sb, 427, 4, firstVisitYear);

            // DOLP_MO (432-433): Date of last prenatal visit - month (1-2 weeks before death)
            var lastVisitMonth = deathParts[1];
            var lastVisitDay = Math.Max(1, int.Parse(deathParts[2]) - _random.Next(7, 14)).ToString("D2");
            SetField(sb, 431, 2, lastVisitMonth);

            // DOLP_DY (434-435): Date of last prenatal visit - day
            SetField(sb, 433, 2, lastVisitDay);

            // DOLP_YR (436-439): Date of last prenatal visit - year
            SetField(sb, 435, 4, deathParts[0]);

            // NPREV (440-441): Number of previous births
            SetField(sb, 439, 2, GetRandomPreviousBirths().ToString("D2"));

            // HFT (443): Mother's height - feet
            var heightParts = GetRandomHeight();
            SetField(sb, 442, 1, heightParts[0].ToString());

            // HIN (444-445): Mother's height - inches
            SetField(sb, 443, 2, heightParts[1].ToString());

            // PWGT (447-449): Mother's prepregnancy weight
            var preWeight = GetRandomWeight(100, 200);
            SetField(sb, 446, 3, preWeight.ToString());

            // DWGT (451-453): Mother's delivery weight
            var deliveryWeight = preWeight + _random.Next(10, 30);
            SetField(sb, 450, 3, deliveryWeight.ToString());

            // WIC (455): WIC
            SetField(sb, 454, 1, GetRandomYesNo());

            // PLBL (456-457): Number born alive
            SetField(sb, 455, 2, "00");

            // PLBD (458-459): Number born dead
            SetField(sb, 457, 2, "01");

            // POPO (460-461): Number of other pregnancy outcomes
            SetField(sb, 459, 2, "00");

            // Cigarettes - progressive reduction pattern
            var cigsBefore = GetRandomCigarettes();
            var cigsBeforeNum = int.Parse(cigsBefore);
            var cigsFirst = cigsBeforeNum > 0 ? Math.Max(0, cigsBeforeNum - _random.Next(0, 5)).ToString("D2") : "00";
            var cigsFirstNum = int.Parse(cigsFirst);
            var cigsSecond = cigsFirstNum > 0 ? Math.Max(0, cigsFirstNum - _random.Next(0, 3)).ToString("D2") : "00";
            var cigsSecondNum = int.Parse(cigsSecond);
            var cigsThird = cigsSecondNum > 0 ? Math.Max(0, cigsSecondNum - _random.Next(0, 2)).ToString("D2") : "00";

            // CIGPN (474-475): Cigarettes before pregnancy
            SetField(sb, 473, 2, cigsBefore);

            // CIGFN (476-477): Cigarettes first trimester
            SetField(sb, 475, 2, cigsFirst);

            // CIGSN (478-479): Cigarettes second trimester
            SetField(sb, 477, 2, cigsSecond);

            // CIGLN (480-481): Cigarettes third trimester
            SetField(sb, 479, 2, cigsThird);

            // Calculate last menstrual period (approximately 40 weeks before death)
            var lmpYear = deathYear - 1;
            var lmpMonth = int.Parse(deathParts[1]);
            var lmpDay = int.Parse(deathParts[2]);
            if (lmpMonth > 3)
            {
                lmpMonth -= 3;
                lmpYear = deathYear;
            }
            else
            {
                lmpMonth = lmpMonth + 9;
            }

            // DLMP_YR (482-485): Date of last menses - year
            SetField(sb, 481, 4, lmpYear.ToString());

            // DLMP_MO (486-487): Date of last menses - month
            SetField(sb, 485, 2, lmpMonth.ToString("D2"));

            // DLMP_DY (488-489): Date of last menses - day
            SetField(sb, 487, 2, lmpDay.ToString("D2"));

            // Risk factors
            SetField(sb, 489, 1, GetRandomYesNoUnknown()); // PDIAB
            SetField(sb, 490, 1, GetRandomYesNoUnknown()); // GDIAB
            SetField(sb, 491, 1, GetRandomYesNoUnknown()); // PHYPE
            SetField(sb, 492, 1, GetRandomYesNoUnknown()); // GHYPE
            SetField(sb, 493, 1, GetRandomYesNoUnknown()); // PPB
            SetField(sb, 494, 1, GetRandomYesNoUnknown()); // PPO

            // NPCES (499-500): Number of previous cesareans
            var prevCesareans = _random.Next(0, 3);
            SetField(sb, 498, 2, prevCesareans.ToString("D2"));

            // ATTF (512): Fetal presentation attendant
            SetField(sb, 511, 1, GetRandomYesNo());

            // ATTV (513): Vertex presentation attendant
            SetField(sb, 512, 1, GetRandomYesNo());

            // PRES (514): Presentation
            SetField(sb, 513, 1, _random.Next(1, 4).ToString());

            // ROUT (515): Route of delivery
            SetField(sb, 514, 1, "1");

            // FWG (524-527): Fetal weight in grams
            SetField(sb, 523, 4, "1850");

            // OWGEST (529-530): Obstetric estimate of gestation
            SetField(sb, 528, 2, "26");

            // ROUT (515): Route of delivery
            SetField(sb, 514, 1, _random.Next(1, 5).ToString());

            // FWG (517-520): Fetal weight in grams
            SetField(sb, 516, 4, birthWeight.ToString());

            // OWGEST (522-523): Obstetric estimate of gestation
            SetField(sb, 521, 2, gestationalAge.ToString("D2"));

            // PLUR (536-537): Plurality
            SetField(sb, 535, 2, "01");

            // SORD (538-539): Set order
            SetField(sb, 537, 2, "01");

            // Congenital anomalies
            SetField(sb, 548, 1, GetRandomYesNoUnknown()); // ANEN
            SetField(sb, 549, 1, GetRandomYesNoUnknown()); // MNSB
            SetField(sb, 550, 1, GetRandomYesNoUnknown()); // CCHD

            // MAGER (569-570): Mother's age
            SetField(sb, 568, 2, motherAge.ToString("D2"));

            // FAGER (571-572): Father's age
            SetField(sb, 570, 2, fatherAge.ToString("D2"));

            // FEDUC (4289): Father's education
            SetField(sb, 4288, 1, GetRandomEducation());

            // Mother's SSN (4039-4047): Must match MOR SSN for linking
            SetField(sb, 4038, 9, ssn);

            // Generate names
            var motherFirstName = GetRandomElement(FirstNames);
            var motherLastName = GetRandomElement(LastNames);
            var motherMaidenName = GetRandomElement(LastNames);
            

            // MOMFNAME (3257-3306): Mother's first name
            SetField(sb, 3256, 50, motherFirstName);

            // MOMLNAME (3357-3406): Mother's last name
            SetField(sb, 3356, 50, motherLastName);

            // MOMMAIDN (3517-3566): Mother's maiden name
            SetField(sb, 3516, 50, motherMaidenName);

            // HOSP_D (2905-2954): Hospital of delivery
            var addrF = GetRandomPublicAddress(StatePublicAddresses, "", _random);
            SetField(sb, 2904, 50, $"{addrF.City} MEDICAL CENTER");

            // ADDRESS_D (3052-3101): Address of delivery
            SetField(sb, 3051, 50, addrF.Street);

            // ZIPCODE_D (3102-3110): ZIP code of delivery
            SetField(sb, 3101, 9, addrF.Zip9);

            // CITY_D (3139-3166): City of delivery
            SetField(sb, 3138, 28, addrF.City);

            // CNTY_D (3111-3138): County of delivery
            SetField(sb, 3110, 28, addrF.County);

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
        public IReadOnlyList<GeneratedIJEFile> GenerateAllFiles(
            int recordsPerFile = 5,
            string stateCode = "LOCALHOST",
            IReadOnlyList<string>? jurisdictionSampling = null,
            IReadOnlyList<int>? yearOfDeathSampling = null,
            DateTime? timestamp = null)
        {
            var now = timestamp ?? DateTime.Now;
            var normalizedStateCode = string.IsNullOrWhiteSpace(stateCode)
                ? "LOCALHOST"
                : stateCode.ToUpperInvariant();
            string filePrefix = $"{now.Year}_{now:yyyy_MM_dd}_{normalizedStateCode}";

            return new List<GeneratedIJEFile>
            {
                new GeneratedIJEFile
                {
                    FileName = $"{filePrefix}.MOR",
                    FileType = "MOR",
                    Records = new List<string>(GenerateMORRecords(recordsPerFile, jurisdictionSampling, yearOfDeathSampling))
                },
                new GeneratedIJEFile
                {
                    FileName = $"{filePrefix}.NAT",
                    FileType = "NAT",
                    Records = new List<string>(GenerateNATRecords(recordsPerFile))
                },
                new GeneratedIJEFile
                {
                    FileName = $"{filePrefix}.FET",
                    FileType = "FET",
                    Records = new List<string>(GenerateFETRecords(recordsPerFile, jurisdictionSampling, yearOfDeathSampling))
                }
            };
        }

        public void GenerateAllTestFiles(
            string outputDirectory,
            int recordsPerFile = 5,
            string stateCode = "LOCALHOST",
            IReadOnlyList<string>? jurisdictionSampling = null,
            IReadOnlyList<int>? yearOfDeathSampling = null)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var files = GenerateAllFiles(recordsPerFile, stateCode, jurisdictionSampling, yearOfDeathSampling);

            foreach (var file in files)
            {
                var outputPath = Path.Combine(outputDirectory, file.FileName);
                File.WriteAllLines(outputPath, file.Records);
            }
        }
    }
}
