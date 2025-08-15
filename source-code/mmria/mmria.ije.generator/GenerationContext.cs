
namespace mmria.ije.generator;
public class GenerationContext
{
    public GenerationContext(int seed) 
    {
        rnd = new Random(seed);
    }

    const int seed = 1337;

    public Random rnd { get;}

    public double percentage_threshold = .5;

    public string Get(IList<string> list)
    {
        int index = rnd.Next(0, list.Count);

        return list[index];
    }

    public static List<string> zero_or_one = new()
    {
        "0",
        "1"
    };

    public static List<string> a_z_blank = new()
    {
        " ",
        "A",
        "B",
        "C",
        "D",
        "E",
        "F",
        "G",
        "H",
        "I",
        "J",
        "K",
        "L",
        "M",
        "N",
        "O",
        "P",
        "Q",
        "R",
        "S",
        "T",
        "U",
        "V",
        "W",
        "X",
        "Y",
        "Z"
    };


    public static List<string> state_list = new()
    {
        "AL",
        "AK",
        "AZ",
        "AR",
        "CA",
        "CO",
        "CT",
        "DE",
        "DC",
        "FL",
        "GA",
        "HI",
        "ID",
        "IL",
        "IN",
        "IA",
        "KS",
        "KY",
        "LA",
        "ME",
        "MT",
        "NE",
        "NV",
        "NH",
        "NJ",
        "NM",
        "NY",
        "NC",
        "ND",
        "OH",
        "OK",
        "OR",
        "MD",
        "MA",
        "MI",
        "MN",
        "MS",
        "MO",
        "PA",
        "RI",
        "SC",
        "SD",
        "TN",
        "TX",
        "UT",
        "VT",
        "VA",
        "WA",
        "WV",
        "WI",
        "WY",
        "ZZ", // UNKNOWN OR BLANK U.S. STATE OR TERRITORY OR UNKNOWN CANADIAN PROVINCE OR UNKNOWN/ UNCLASSIFIABLE COUNTRY
        "XX", // UNKNOWN STATE WHERE COUNTRY IS KNOWN, BUT NOT U.S. OR CANADA 
      
        "MP", //  NORTHERN MARIANAS
        "AS", //  AMERICAN SAMOA
        "GU", //  GUAM
        "VI", //   VIRGIN ISLANDS
        "PR", //  PUERTO RICO
        "Fo", //r Canadian Provinces:
        "AB", //  ALBERTA  
        "BC", //  BRITISH COLUMBIA 
        "MB", //  MANITOBA 
        "NB", //  NEW BRUNSWICK  
        "NF", // NEWFOUNDLAND  
        "NS", //  NOVA SCOTIA 
        "NT", //  NORTHWEST TERRITORIES
        "NU", //  NUNAVUT
        "ON", //  ONTARIO
        "PE", //  PRINCE EDWARD ISLAND 
        "QC", //  QUEBEC  
        "SK", //  SASKATCHEWAN
        "YT", //  

    };

    public static List<string> country_list = new()
    {
        "US",
        "ZZ", // UNKNOWN OR BLANK U.S. STATE OR TERRITORY OR UNKNOWN CANADIAN PROVINCE OR UNKNOWN/ UNCLASSIFIABLE COUNTRY
        "XX", // UNKNOWN STATE WHERE COUNTRY IS KNOWN, BUT NOT U.S. OR CANADA 
        "MP", //  NORTHERN MARIANAS
        "AS", //  AMERICAN SAMOA
        "GU", //  GUAM
        "VI", //   VIRGIN ISLANDS
        "PR", //  PUERTO RICO
        "Fo", //r Canadian Provinces:
        "AB", //  ALBERTA  
        "BC", //  BRITISH COLUMBIA 
        "MB", //  MANITOBA 
        "NB", //  NEW BRUNSWICK  
        "NF", // NEWFOUNDLAND  
        "NS", //  NOVA SCOTIA 
        "NT", //  NORTHWEST TERRITORIES
        "NU", //  NUNAVUT
        "ON", //  ONTARIO
        "PE", //  PRINCE EDWARD ISLAND 
        "QC", //  QUEBEC  
        "SK", //  SASKATCHEWAN
        "YT" //  
    };

    public static List<string> city_list = new()
    {
        "US",
        "ZZ", // UNKNOWN OR BLANK U.S. STATE OR TERRITORY OR UNKNOWN CANADIAN PROVINCE OR UNKNOWN/ UNCLASSIFIABLE COUNTRY
        "XX", // UNKNOWN STATE WHERE COUNTRY IS KNOWN, BUT NOT U.S. OR CANADA 
        "MP", //  NORTHERN MARIANAS
        "AS", //  AMERICAN SAMOA
        "GU", //  GUAM
        "VI", //   VIRGIN ISLANDS
        "PR", //  PUERTO RICO
        "Fo", //r Canadian Provinces:
        "AB", //  ALBERTA  
        "BC", //  BRITISH COLUMBIA 
        "MB", //  MANITOBA 
        "NB", //  NEW BRUNSWICK  
        "NF", // NEWFOUNDLAND  
        "NS", //  NOVA SCOTIA 
        "NT", //  NORTHWEST TERRITORIES
        "NU", //  NUNAVUT
        "ON", //  ONTARIO
        "PE", //  PRINCE EDWARD ISLAND 
        "QC", //  QUEBEC  
        "SK", //  SASKATCHEWAN
        "YT" //  
    };

    public static List<string> first_name = new()
    {

        "Lida",
        "Nova",
        "Estella",
        "Isobel",
        "Branden",
        "Dominica",
        "Kraig",
        "Krysta",
        "Lilliana",
        "Vicenta",
        "Kyong",
        "Jeramy",
        "Lizette",
        "Robbi",
        "Oscar",
        "Pinkie",
        "Larisa",
        "Janella",
        "Angelika",
        "Dena",
        "Stacee",
        "Anissa",
        "Melva",
        "Anh",
        "Corene",
        "Bennett",
        "Eunice",
        "Shalon",
        "Jacques",
        "Song",
        "Cassandra",
        "Lizzette",
        "Georgine",
        "Adriane",
        "Davida",
        "Earlene",
        "Vinita",
        "Harlan",
        "Cathern",
        "Sandra",
        "Delois",
        "Hortense",
        "Launa",
        "Angelita",
        "Katherin",
        "Bernadine",
        "Lakendra",
        "Cameron",
        "Patty",
        "Annika",
        "Misha",
        "Cathie",
        "Rana",
        "Gricelda",
        "Nathanael",
        "Eddie",
        "Jere",
        "Aundrea",
        "Aliza",
        "Molly",
        "Shirlene",
        "Shanna",
        "Alton",
        "Hannelore",
        "Yaeko",
        "Taisha",
        "Kerrie",
        "Daniel",
        "Oliva",
        "Raelene",
        "Antionette",
        "Hilma",
        "Nicolette",
        "Chere",
        "Sharolyn",
        "Erasmo",
        "Silas",
        "Stevie",
        "Simonne",
        "Eladia",
        "Susanne",
        "Lester",
        "Marylyn",
        "Yessenia",
        "Michiko",
        "Carlota",
        "Kimi",
        "Gino",
        "Sophie",
        "Wei",
        "Alesha",
        "Sherita",
        "Hildred",
        "Almeta",
        "Josefa",
        "Sulema",
        "Gertha",
        "Hyacinth",
        "Clemmie",
        "Catherin",
        "Lee",
        "Xi"
    };

    static public List<string> last_name = new()
    {
        "Li",
        "Donohue",
        "Bearden",
        "Matson",
        "Fryer",
        "Mount",
        "Moultrie",
        "Minton",
        "Batchelor",
        "Hollis",
        "Crockett",
        "Sisco",
        "Skipper",
        "Ingram",
        "Keith",
        "Newsom",
        "Eaves",
        "Higginbotham",
        "Toliver",
        "Duffy",
        "Stott",
        "See",
        "Mattox",
        "Ponder",
        "Frame",
        "Thrasher-Cobb",
        "Devlin",
        "Hass-Lawler",
        "Bunn",
        "Mclaughlin",
        "Beebe",
        "Leary",
        "Stack",
        "Martens",
        "Medlin",
        "Mathias",
        "Garland",
        "Saldivar",
        "Toney",
        "Kirby",
        "Jefferson",
        "Hopkins",
        "Mojica",
        "Foreman",
        "Meador",
        "Hodges",
        "Kenny",
        "Pool",
        "Marquardt",
        "Mcduffie",
        "Noonan",
        "Britt",
        "Marcum",
        "Peebles",
        "Spruill",
        "Silva",
        "Huddleston",
        "Spurgeon",
        "Pool",
        "Havens",
        "Ratcliff",
        "Goodson-Henry",
        "Wicker",
        "Meek",
        "Mohr",
        "Hamer",
        "Bundy",
        "Finnegan",
        "Mcnamara",
        "Brandenburg",
        "Desimone",
        "Meadows",
        "Colbert",
        "Spellman-Mccartney",
        "Oglesby",
        "Florence",
        "Gatewood-Belton",
        "Randall",
        "Kellogg",
        "Carbone",
        "Grice",
        "Mattson",
        "Dickey",
        "Galvan",
        "Treadway",
        "Romeo",
        "Wang-Brackett",
        "Brown",
        "Labelle",
        "Forbes",
        "Jaeger",
        "Treadwell",
        "Penrod",
        "Narvaez",
        "Utley",
        "Dubois",
        "East",
        "Hodges",
        "Kasper",
        "Connelly",
        "Dewitt"
    };

    public static List<string> suffix_list = new()
    {
        "Jr.",
        "II",
        "III"
    };

    public static List<string> age_type_list = new()
    {
        "1"	, //	AGETYPE	"1 = Years
        "2", // = Months
        "4", // = Days
        "5", // = Hours
        "6", // = Minutes
        "9", // = Unknown (not classifiable)"
    };

}