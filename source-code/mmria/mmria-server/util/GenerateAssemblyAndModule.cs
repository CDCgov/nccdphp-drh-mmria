using System;
using System.Reflection;
using System.Reflection.Emit;

namespace mmria.server;

/*

//http://www.codeproject.com/Articles/13337/Introduction-to-Creating-Dynamic-Types-with-Reflec

public interface IORMapper
{
    void PopulateObject(object entity, DataRow data);
}

public sealed class Builder
{
    private static AssemblyBuilder asmBuilder = null;
    private static ModuleBuilder modBuilder = null;

    private string type_name;

    public Builder(string type_name)
    {

        this.type_name = type_name;
        */
        /*
        IORMapper mapper = 
            ObjectMapperFactory.CreateObjectMapper(typeof(Customer));
        foreach (DataRow row in data.Rows)
        {
            Customer c = new Customer();
            mapper.PopulateObject(row, c);
            customers.Add(c);
        }


        i have a xml file with this structure

        <graph>
        <id>0</id>
        <name>John</name>
        <link>http://test.com</link>
        </graph>
        <graph>
        <id>1</id>
        <name>Roger</name>
        <link>http://test2.com</link>
        </graph>
        i want use Reflection.Emit class for create two class first:

            class A {
            int id;
            string name;
            string link
        }
        second:

        Class B{
            List<A> graphs;
        }
        i read this paper (Introduction to Creating Dynamic Types with Reflection.Emit) and can create class A in runtime but the problem is using this (A) in another runtime class(B) and also have a List of A.
            */


        /*
            Builder MCB=new Builder("A");
        var myclass = MCB.CreateObject(new string[3] { "id", "name", "link" }, new Type[3] { typeof(int), typeof(string), typeof(string) });
        Type TP = myclass.GetType();
        Builder MCB1=new Builder("B");
        //Here is my confusion typeof(List<TP>) ?????? Error Ocurred
        var myclass2 = MCB1.CreateObject(new string[1] {"graphs"}, new Type[1]{typeof(List<TP>)});
        //set value of class A and work
        object tp = Activator.CreateInstance(TP);
        TP.GetProperty("id").SetValue(tp,0);
        TP.GetProperty("name").SetValue(tp,"Joh");
        TP.GetProperty("link").SetValue(tp,"http://test.com");

    }

    private static void GenerateAssemblyAndModule()
    {
        if (asmBuilder == null)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "DynamicORMapper";
            AppDomain thisDomain = Thread.GetDomain();
            asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName, 
                AssemblyBuilderAccess.Run);

            modBuilder = assBuilder.DefineDynamicModule(
                asmBuilder.GetName().Name, false);
        }
    }


    private static TypeBuilder CreateType(ModuleBuilder modBuilder, string typeName)
    {
        TypeBuilder typeBuilder = modBuilder.DefineType(typeName, 
            TypeAttributes.Public | 
            TypeAttributes.Class |
            TypeAttributes.AutoClass | 
            TypeAttributes.AnsiClass | 
            TypeAttributes.BeforeFieldInit | 
            TypeAttributes.AutoLayout, 
            typeof(object), 
            new Type[] {typeof(IORMapper)});

        return typeBuilder;
    }

    private static void CreateConstructor(TypeBuilder typeBuilder)
    {
        ConstructorBuilder constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public | 
            MethodAttributes.SpecialName | 
            MethodAttributes.RTSpecialName, 
            CallingConventions.Standard, 
            new Type[0]);
        //Define the reflection ConstructorInfor for System.Object
        ConstructorInfo conObj = typeof(object).GetConstructor(new Type[0]);

        //call constructor of base object
        ILGenerator il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, conObj);
        il.Emit(OpCodes.Ret);
    }

    TypeBuilder typeBuilder = null;
    public static IORMapper CreateORMapper(Type mappedType)
    {
        //check to see if type is already created
        if (typeBuilder == null)
        {
            //Didnt exist, so create assembly and module 
            GenerateAssemblyAndModule();                                  

            //create new type for table name
            TypeBuilder typeBuilder = CreateType(modBuilder, 
                "ORMapper_" + mappedType.Name);
            //create constructor
            CreateConstructor(typeBuilder);

            //create O/R populate object
            CreatePopulateObject(typeBuilder, mappedType);
        }

        Type mapperType = typeBuilder.CreateType();

        try
        {
            IORMapper mapper = (IORMapper)mapperType.GetConstructor(
                Type.EmptyTypes).Invoke(Type.EmptyTypes);
        }
        catch (Exception ex)
        {
            //Log error if desired
            return null;
        }
        return mapper;
    }

}*/


