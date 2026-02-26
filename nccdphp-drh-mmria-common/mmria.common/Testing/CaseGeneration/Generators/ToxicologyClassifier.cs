using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.common.Testing.CaseGeneration.Generators
{
    /// <summary>
    /// Maps toxicology results (substance names) to standardized drug classes
    /// used by the overdose data summary report.
    /// </summary>
    public class ToxicologyClassifier
    {
        private static readonly Dictionary<string, string> SubstanceToClassMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Alcohol
            ["alcohol"] = "alcohol",
            ["ethanol"] = "alcohol",
            ["etoh"] = "alcohol",
            
            // Amphetamines
            ["amphetamine"] = "amphetamine",
            ["methamphetamine"] = "amphetamine",
            ["meth"] = "amphetamine",
            ["dextroamphetamine"] = "amphetamine",
            ["adderall"] = "amphetamine",
            
            // Benzodiazepines
            ["benzodiazepine"] = "benzodiazepine",
            ["alprazolam"] = "benzodiazepine",
            ["xanax"] = "benzodiazepine",
            ["diazepam"] = "benzodiazepine",
            ["valium"] = "benzodiazepine",
            ["lorazepam"] = "benzodiazepine",
            ["ativan"] = "benzodiazepine",
            ["clonazepam"] = "benzodiazepine",
            ["klonopin"] = "benzodiazepine",
            
            // Buprenorphine/Methadone (medication-assisted treatment)
            ["buprenorphine"] = "buprenorphine_methadone",
            ["suboxone"] = "buprenorphine_methadone",
            ["methadone"] = "buprenorphine_methadone",
            ["dolophine"] = "buprenorphine_methadone",
            
            // Cannabinoid
            ["cannabis"] = "cannabinoid",
            ["thc"] = "cannabinoid",
            ["marijuana"] = "cannabinoid",
            ["marihuana"] = "cannabinoid",
            ["cbd"] = "cannabinoid",
            
            // Cocaine
            ["cocaine"] = "cocaine",
            ["crack"] = "cocaine",
            ["crack cocaine"] = "cocaine",
            
            // Opioids (excluding buprenorphine/methadone - those are classified separately)
            ["heroin"] = "opioid",
            ["morphine"] = "opioid",
            ["codeine"] = "opioid",
            ["oxycodone"] = "opioid",
            ["oxycontin"] = "opioid",
            ["hydrocodone"] = "opioid",
            ["vicodin"] = "opioid",
            ["tramadol"] = "opioid",
            ["ultram"] = "opioid",
            ["fentanyl"] = "opioid",
            ["fentanyl patch"] = "opioid",
            ["hydromorphone"] = "opioid",
            ["dilaudid"] = "opioid",
            ["diacetylmorphine"] = "opioid",
            ["dam"] = "opioid",
            ["propoxyphene"] = "opioid",
            ["darvon"] = "opioid"
        };

        public string Classify(string substance)
        {
            if (string.IsNullOrWhiteSpace(substance))
                return "other";

            // Try exact match first
            if (SubstanceToClassMap.TryGetValue(substance, out var drugClass))
                return drugClass;

            // Try partial match (for metabolites and variations)
            var lowerSubstance = substance.ToLower();
            foreach (var kvp in SubstanceToClassMap)
            {
                if (lowerSubstance.Contains(kvp.Key))
                    return kvp.Value;
            }

            // Default to "other" for unclassified substances
            return "other";
        }
    }
}

