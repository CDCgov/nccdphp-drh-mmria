global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;

namespace mmria.ije.generator;
public class Program
{

    static HashSet<string> tab_name = new()
    {
        "Mortality",
        "Natality",
        "Fetal Death"
    };
    public static async Task Main(string[] args)
    {
        const int seed = 1337;
        if(args.Length == 3)
        {
            int start;
            int length;
            if
            (
                int.TryParse(args[1], out start) &&
                int.TryParse(args[2], out length)
            )
            get_field_freq
            (
                args[0], //string file_path, 
                start, //int start,
                length //int length
            );
            return;
        }

        GenerationContext Context = new (seed);

        System.IO.File.WriteAllText("output/mort.MOR", new GenMortality(Context).ToString());
        System.IO.File.WriteAllText("output/nat.NAT", new GenNatality(Context).ToString());
        System.IO.File.WriteAllText("output/fet.FET", new GenFetality(Context).ToString());

        Console.WriteLine("IJE Generation Complete.");
    }

    public static void get_field_freq
    (
        string file_path, 
        int start,
        int length
    )
    {
        var dictionary = new System.Collections.Generic.Dictionary<string,float>(StringComparer.OrdinalIgnoreCase);


        var file_contents = System.IO.File.ReadAllText(file_path);
        foreach(var line in file_contents.Split("\n"))
        {
            if(string.IsNullOrWhiteSpace(line)) continue;

            var item = line.Substring(start, length);
            if(!dictionary.ContainsKey(item))
            {
                dictionary.Add(item, 0);
            }
            dictionary[item]+=1;
        }

        var sum = 0.0;
        foreach(var kvp in dictionary.OrderByDescending( kvp=> kvp.Value))
        {
            sum+= kvp.Value;
        }

        
        foreach(var kvp in dictionary.OrderByDescending( kvp=> kvp.Value))
        {
            
            System.Console.WriteLine($"{kvp.Key}:{kvp.Value} %{(kvp.Value / sum) * 100.0}");
        }
    }



}

