using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace mmria.console.schema;

public sealed class Generatelib
{

    public void Execute
    (
        string dll_name,
        string csharp_source_path,
        string json_schema_path
    )
    {

        var dd = typeof(Enumerable).GetTypeInfo().Assembly.Location;
        var coreDir = Directory.GetParent(dd);

        var source_code = System.IO.File.ReadAllText(csharp_source_path);


        source_code = @"using System; 
namespace test{

    public sealed class tes
    {

    public string unescape(string Text)
    { 
    return Uri.UnescapeDataString(Text);
    } 

    }

}";
/* */
        System.Console.Write("1: " + typeof(Object).GetTypeInfo().Assembly.Location);
        System.Console.Write("2: " + typeof(Uri).GetTypeInfo().Assembly.Location);


        try
        {
            var compilation = CSharpCompilation.Create(Guid.NewGuid().ToString() + ".dll")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences
                (
                    MetadataReference.CreateFromFile(typeof(Object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Uri).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "mscorlib.dll"),
                    MetadataReference.CreateFromFile("/media/jhaines/tera27/file-set/mmrds/MMRDS/source-code/mmria/mmria-console/bin/Release/netcoreapp2.0/ubuntu.16.10-x64/publish" + Path.DirectorySeparatorChar + "Newtonsoft.Json.dll"),
                    
                    MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.ObjectModel.dll"),
                    MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.dll"),
                    MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Diagnostics.Tools.dll"),
                    MetadataReference.CreateFromFile("/media/jhaines/tera27/file-set/mmrds/MMRDS/source-code/mmria/mmria-console/bin/Release/netcoreapp2.0/ubuntu.16.10-x64/publish" + Path.DirectorySeparatorChar + "System.ComponentModel.Annotations.dll"),
                        /**/
                    MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Runtime.dll")


                )
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source_code));

            var fileName = $"{dll_name}.dll";

            compilation.Emit(fileName);
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
        }

/*

https://stackoverflow.com/questions/26851214/including-an-embedded-resource-in-a-compilation-made-by-roslyn
https://stackoverflow.com/questions/8609476/how-to-open-a-pdf-file-that-is-also-a-project-resource/8609599#8609599

const string resourcePath = @"C:\Projects\...\Properties\Resources.resources";
var resourceDescription = new ResourceDescription(
            "[namespace].Resources.resources",
            () => File.OpenRead(resourcePath),
            true);

var result = mutantCompilation.Emit(file, manifestResources: new [] {resourceDescription}); 

*/


        /*

        var a = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(fileName));

        a.GetType("C").GetMethod("M").Invoke(null, null);
        */

    }
}

