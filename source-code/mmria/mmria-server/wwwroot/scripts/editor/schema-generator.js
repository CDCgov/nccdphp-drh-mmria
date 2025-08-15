function get_initial_schema()
{
    return {
    "$schema": "http://json-schema.org/draft-04/schema#",
    "title": "mmria_case",
    "type": "object",
    "description": "Maternal Mortality case schema",
    "properties": {},
    "definitions": {},
    "required": ["_id", "version"]
  };
}


function get_schema_context(p_metadata, p_schema, p_version, p_path)
{
    return {
        metadata: p_metadata,
        schema: p_schema,
        version: p_version,
        path: p_path
    };
}


function get_definition_context(p_metadata, p_definition_node, p_mmria_path_to_definition_name, p_path)
{
    return {
        metadata: p_metadata,
        definition_node: p_definition_node,
        path: p_path,
        mmria_path_to_definition_name: p_mmria_path_to_definition_name

    };
}


function get_definition_name(p_name, p_definition_node)
{
    let base_name = p_name.toLowerCase();

    let result = base_name;

    let name_count = 0;
    while (result in p_definition_node)
    {
        name_count+=1;


        result = base_name + name_count.toString();
    }

    return result;
}


function generate_schema_phase2(p_schema_context)
{

    let object = null;
    let new_schema_context = null;

    switch(p_schema_context.metadata.type.toLowerCase())
    {
        
        case "app":
            p_schema_context.schema.properties._id = {
                    "type": "string"
            };

            p_schema_context.schema.properties.version = {
                    "type": "string",
                    "enum": [ p_schema_context.version ],
                    "default" : p_schema_context.version
            };

            //object = p_schema_context.schema[child.name.toLowerCase()] =  get_grid(child,  "/" + child.name.toLowerCase());
 
            if(p_schema_context.metadata.children)
            {
                for(var i = 0; i < p_schema_context.metadata.children.length; i++)
                {
                    var child = p_schema_context.metadata.children[i];
        
                    new_schema_context = get_schema_context(child, p_schema_context.schema.properties, p_schema_context.version, "");
                    generate_schema_phase2(new_schema_context);
                    /*
                    if(child.name == "home_record")
                    {
                        break;
                    }*/
                }
            }

            break;
        
        case "group":
            object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] =  {
                "type": "object",
                "properties": {
                }
            };
            if(p_schema_context.metadata.children)
            {
                for(var i = 0; i < p_schema_context.metadata.children.length; i++)
                {
                    var child = p_schema_context.metadata.children[i];
                    new_schema_context = get_schema_context(child, object.properties, p_schema_context.version, p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase());
                    generate_schema_phase2(new_schema_context);
                }
            }
            break;
        case "form":
            if(p_schema_context.metadata.type.toLowerCase() == "form" && (p_schema_context.metadata.cardinality == "*" ||  p_schema_context.metadata.cardinality == "+") )
            {
                var definition_name = mmria_path_to_definition_name[p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase()];

                object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] =  {
                    "type": "array",
                    "items": { "$ref": "#/definitions/" + definition_name }
                };
            }
            else
            {
                var definition_name = mmria_path_to_definition_name[p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase()];
                object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] =  {
                    "type": "object",
                    "$ref": "#/definitions/" + definition_name
                };
            }
            break;
        case "grid":
            var definition_name = mmria_path_to_definition_name[p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase()];
            object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] =  {
                "type": "object",
                "$ref": "#/definitions/" + definition_name
            };
    }
}


function generate_schema(p_schema_context)
{

    let object = null;
    let new_schema_context = null;

    switch(p_schema_context.metadata.type.toLowerCase())
    {
        case "app":
            
            p_schema_context.schema.properties._id = {
                    "type": "string"
            };

            p_schema_context.schema.properties.version = {
                    "type": "string",
                    "enum": [ p_schema_context.version ],
                    "default" : p_schema_context.version
            };
           /* */
            //object = p_schema_context.schema[child.name.toLowerCase()] =  get_grid(child,  "/" + child.name.toLowerCase());
 
            if(p_schema_context.metadata.children)
            {
                for(let i = 0; i < p_schema_context.metadata.children.length; i++)
                {
                    let child = p_schema_context.metadata.children[i];
        
                    new_schema_context = get_schema_context(child, p_schema_context.schema.properties, p_schema_context.version, "");
                    generate_schema(new_schema_context);
                }
            }

            break;
        case "group":
            object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] =  {
                "type": "object",
                "properties": {
                }
            };
            if(p_schema_context.metadata.children)
            {
                for(let i = 0; i < p_schema_context.metadata.children.length; i++)
                {
                    let child = p_schema_context.metadata.children[i];
                    new_schema_context = get_schema_context(child, object.properties, p_schema_context.version, p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase());
                    generate_schema(new_schema_context);
                }
            }
            break;
        case "form":
            //var definition_name = mmria_path_to_definition_name[p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase()];
            object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] =  {
                "type": "object",
                "properties": {
                }
            };
            

            if(p_schema_context.metadata.children)
            {
                for(let i = 0; i < p_schema_context.metadata.children.length; i++)
                {
                    let child = p_schema_context.metadata.children[i];
        
                    new_schema_context = get_schema_context(child, object.properties, p_schema_context.version, p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase());
                    generate_schema(new_schema_context);
                }
            }
            
            break;
        case "grid":
            //var definition_name = mmria_path_to_definition_name[p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase()];
            object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] =  {
                "type": "object",
                "properties": {
                }
            };
            if(p_schema_context.metadata.children)
            {
                for(let i = 0; i < p_schema_context.metadata.children.length; i++)
                {
                    let child = p_schema_context.metadata.children[i];
        
                    let new_child_schema_context = get_schema_context(child, object.properties, p_schema_context.version, p_schema_context.path + "/" + p_schema_context.metadata.name.toLowerCase());
                    generate_schema(new_child_schema_context);
                }
            }
            break;        
        case "number":
            p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type" : "number"}
                break;                    
        case "string":
            p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type" : "string"}
            break;
        case "date":
            p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type" : "string", "format": "date" }
            break;            
        case "time":
            p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type" : "string", "format": "time" }
            break;            
        case "datetime":            
            p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type" : "string", "format": "date-time" }
            break;
        case "list":

            let data_type = "string";

            if
            (
                p_schema_context.metadata.data_type != null &&
                p_schema_context.metadata.data_type != ""
            )
            {
                data_type = p_schema_context.metadata.data_type;
            }

            if
            (
                p_schema_context.metadata.is_multiselect != null &&
                p_schema_context.metadata.is_multiselect == "true"
            )
            {
                let data_value_list = p_schema_context.metadata.values;

                if
                (
                    p_schema_context.metadata.path_reference &&
                    p_schema_context.metadata.path_reference != ""
                )
                {
                    data_value_list = eval(convert_dictionary_path_to_lookup_object(p_schema_context.metadata.path_reference));
            
                    if(data_value_list == null)	
                    {
                        data_value_list = p_schema_context.metadata.values;
                    }

                    
                    object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type": "array", "items": { "type": data_type, "x-enumNames": [], "enum":[] } }

                    for(let j = 0; j < data_value_list.length; j++ )
                    {
                        object["items"]["x-enumNames"].push(data_value_list[j].display);
                        object["items"]["enum"].push(data_value_list[j].value);
                    }

                    /*
                    else
                    {
                        object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type": "array",  "items": { "allOf": [{ "$ref": "#/definitions/" + p_schema_context.metadata.path_reference.replace("lookup/","") }] } }
                        
                        
                        object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type": "array", "items": { "type": data_type, "x-enumNames": [], "enum":[] } }

                        for(let j = 0; j < data_value_list.length; j++ )
                        {
                            object["items"]["x-enumNames"].push(data_value_list[j].display);
                            object["items"]["enum"].push(data_value_list[j].value);
                        }
                        
                    }*/

                }
                else
                {
                    object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type": "array", "items": { "type": data_type, "x-enumNames": [], "enum":[] } }

                    for(let j = 0; j < p_schema_context.metadata.values.length; j++ )
                    {
                        object["items"]["x-enumNames"].push(p_schema_context.metadata.values[j].display);
                        object["items"]["enum"].push(p_schema_context.metadata.values[j].value);
                    }
                }

            }
            else
            {

                

                if(p_schema_context.metadata.path_reference && p_schema_context.metadata.path_reference != "")
                {
                    data_value_list = eval(convert_dictionary_path_to_lookup_object(p_schema_context.metadata.path_reference));
            
                    if(data_value_list == null)	
                    {
                        data_value_list = p_schema_context.metadata.values;

                    }


                    object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type": data_type,  "x-enumNames": [], "enum":[] }
                        
                    for(let j = 0; j < data_value_list.length; j++ )
                    {
                        object["x-enumNames"].push(data_value_list[j].display);
                        object["enum"].push(data_value_list[j].value);
                    }

                    /*
                    else
                    {
                        object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = {  "type": data_type, "items": { "oneOf": [{"$ref": "#/definitions/" + p_schema_context.metadata.path_reference.replace("lookup/","") }] } }

                        
                        object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type": data_type, "x-enumNames": [], "enum":[] }

                        for(let j = 0; j < data_value_list.length; j++ )
                        {
                            object["x-enumNames"].push(data_value_list[j].display);
                            object["enum"].push(data_value_list[j].value);
                        }
                        
                    }*/

                }
                else
                {
                    object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type": data_type, "x-enumNames": [], "enum":[] }

                    for(let j = 0; j < p_schema_context.metadata.values.length; j++ )
                    {
                        object["x-enumNames"].push(p_schema_context.metadata.values[j].display);
                        object["enum"].push(p_schema_context.metadata.values[j].value);
                    }
                }


            }
        break;
    }

 
}


function set_definitions(p_definition_context)
{

    let object = null;
    let new_definition_context = null;

    switch(p_definition_context.metadata.type.toLowerCase())
    {
        case "app":
            if(p_definition_context.metadata.children)
            {
                for(var i = 0; i < p_definition_context.metadata.children.length; i++)
                {
                    var child = p_definition_context.metadata.children[i];
        
                    new_definition_context = get_definition_context(child, p_definition_context.definition_node, p_definition_context.mmria_path_to_definition_name, "");//p_definition_context.path + "/" + p_definition_context.metadata.name.toLowerCase()
                    set_definitions(new_definition_context);
                }
            }
            break;
        case "group":
            object = p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] =  {
                "type": "object",
                "properties": {}
            };
            if(p_schema_context.metadata.children)
            {
                for(var i = 0; i < p_schema_context.metadata.children.length; i++)
                {
                    var child = p_schema_context.metadata.children[i];
                    new_definition_context = get_schema_context(child, object.properties, p_schema_context.version, p_schema_context.path + "/" + child.name.toLowerCase());
                    generate_schema(new_definition_context);
                }
            }
            break;
        case "form":
            var definition_name = get_definition_name
            (
                p_definition_context.metadata.name.toLowerCase(),
                p_definition_context.definition_node
            );

            p_definition_context.mmria_path_to_definition_name[p_definition_context.path + "/" + p_definition_context.metadata.name.toLowerCase()] = definition_name;

            object = p_definition_context.definition_node[definition_name] = {
                "type": "object",
                "properties": {}
            };
            
            if(p_definition_context.metadata.children)
            {
                for(var i = 0; i < p_definition_context.metadata.children.length; i++)
                {
                    var child = p_definition_context.metadata.children[i];
        
                    if(child.type.toLowerCase() == "grid")
                    {
                        //break;
                        var new_path = p_definition_context.path + "/" + p_definition_context.metadata.name.toLowerCase();
                        var new_grid_definition_context = get_definition_context(child, p_definition_context.definition_node, p_definition_context.mmria_path_to_definition_name, new_path);
                        set_definitions(new_grid_definition_context);

                        var grid_definition_name = new_grid_definition_context.mmria_path_to_definition_name[new_path + "/" + child.name.toLowerCase()];
                        object.properties[child.name.toLowerCase()] =  {
                            "type": "array",
                            "items": {"$ref": "#/definitions/" + grid_definition_name }
                        };
                    }
                    else if(child.type.toLowerCase() == "list" && child.is_multiselect && child.is_multiselect == true)
                    {
                        //break;
                        var new_list_path = p_definition_context.path + "/" + p_definition_context.metadata.name.toLowerCase();
                        var new_list_definition_context = get_definition_context(child, p_definition_context.definition_node, p_definition_context.mmria_path_to_definition_name, new_list_path);
                        set_definitions(new_list_definition_context);

                        var list_definition_name = new_list_definition_context.mmria_path_to_definition_name[new_list_path + "/" + child.name.toLowerCase()];
                        object.properties[child.name.toLowerCase()] =  {
                            "type": "array",
                            "items": {"$ref": "#/definitions/" + list_definition_name }
                        };
                    }
                    else
                    {
                        var new_form_schema_context = get_schema_context(child, object.properties, "", p_definition_context.path + "/" + p_definition_context.metadata.name.toLowerCase());
                        generate_schema(new_form_schema_context);
                    }

                }
            }
            break;
        case "grid":
            var definition_name = get_definition_name
            (
                p_definition_context.metadata.name.toLowerCase(),
                p_definition_context.definition_node
            );

            p_definition_context.mmria_path_to_definition_name[p_definition_context.path + "/" + p_definition_context.metadata.name.toLowerCase()] = definition_name;

            object = p_definition_context.definition_node[definition_name] = {
                "type": "object",
                "properties": {}
            };


            if(p_definition_context.metadata.children)
            {
                for(var i = 0; i < p_definition_context.metadata.children.length; i++)
                {
                    var child = p_definition_context.metadata.children[i];
        
                    var new_grid_schema_context = get_schema_context(child, object.properties, "", p_definition_context.path + "/" + p_definition_context.metadata.name.toLowerCase());
                    generate_schema(new_grid_schema_context);
                }
            }

            
            break;
        case "number":
            //p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type" : "number"}
                break;                    
        case "string":
            //p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type" : "string"}
            break;
        case "date":
        case "time":
        case "datetime":            
            //p_schema_context.schema[p_schema_context.metadata.name.toLowerCase()] = { "type" : "string", "format": "date-time" }
            break;
        case "list":

            var definition_name = get_definition_name
            (
                p_definition_context.metadata.name.toLowerCase(),
                p_definition_context.definition_node
            );

            p_definition_context.mmria_path_to_definition_name[p_definition_context.path + "/" + p_definition_context.metadata.name.toLowerCase()] = definition_name;
    
            /*
            if(p_definition_context.metadata.type.toLowerCase() == "list" && p_definition_context.metadata.is_multiselect && p_definition_context.metadata.is_multiselect == true)
            {
                console.log("multiselect" + p_definition_context.metadata.name);
            }
            else
            {*/
                //object = p_definition_context.definition_node[definition_name] = { "type": "string", "x-enumNames": [], "enum": [] }

                let data_type = p_definition_context.metadata.data_type;
                object = p_definition_context.definition_node[definition_name] = { "type": data_type, "items": { "type": data_type, "x-enumNames": [], "enum": [] } }
                for(var j in p_definition_context.metadata.values)
                {
                    object["items"]["x-enumNames"].push(p_definition_context.metadata.values[j].display);
                    object["items"]["enum"].push(p_definition_context.metadata.values[j].value);
                }
            //}
        break;
    }

 
}


function set_lookUp(p_parent, p_node)
{
//https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Schema_JsonSchema.htm

    /*
    var schema = new NJsonSchema.JsonSchema();
    
    schema.Type =  NJsonSchema.JsonObjectType.Object;
    schema.Title = "mmria_case";
    schema.Description = "Here is a case for your ...!";
    
    schema.SchemaVersion = "http://json-schema.org/draft-06/schema#";
    */			


    var property = null;
    //schema.Properties.Add("name", new NJsonSchema.JsonSchemaProperty(){ Type = NJsonSchema.JsonObjectType.String});
    //schema.Properties.Add("prompt", new NJsonSchema.JsonSchemaProperty(){ Type = NJsonSchema.JsonObjectType.String});

    try
    {
        switch(p_node.type.toLowerCase())
        {						
            case "list":
                if(p_node.is_multiselect && p_node.is_multiselect == true)
                {
                    //"allOf": [ {
                    let data_type = p_node.data_type;
                    property = p_parent[p_node.name.toLowerCase()] = { "type": "array", "items": { "type": data_type, "x-enumNames": [], "enum": [] } }
                    for(var j in p_node.values)
                    {
                        property["items"]["x-enumNames"].push(p_node.values[j].display);
                        property["items"]["enum"].push(p_node.values[j].value);
                    }
                }
                else
                {

                    property = p_parent[p_node.name.toLowerCase()] = { "type": "string", "x-enumNames": [], "enum": [] }
                    for(var j in p_node.values)
                    {
                        property["x-enumNames"].push(p_node.values[j].display);
                        property["enum"].push(p_node.values[j].value);
                    }
                }
                break;
        }
    }
    catch(ex)
    {
        console.log("GetSchemaGetSchema(p_parent, p_node) Exception: {ex}");
    }
    //return p_parent;
}


function get_grid(p_schema_context)
{
    //p_metadata,p_path
    var definitions_node = {};


    for(var child_index in p_schema_context.metadata.children)
    {
        var child = p_schema_context.metadata.children[child_index];
        let new_schema_context = get_schema_context(child, definitions_node, "",  "/" + p_schema_context.metadata);
        //generate_schema(child, definitions_node, "",  "/" + p_schema_context.metadata);
        generate_schema(new_schema_context);
    }

    var result = {
        "$schema": "http://json-schema.org/draft-06/schema#",
      
        "definitions": definitions_node,
        "type": "object",
        "title": p_schema_context.metadata.prompt,
        "properties": {}
    };


    if(p_schema_context.metadata.description && p_schema_context.metadata.description.length > 0)
    {
        result.description = p_schema_context.metadata.description;
    }

    result.properties[p_schema_context.metadata.name.toLowerCase()] = { 
        "type": "array",
        "items": {
            "allOf": [
              {
                "$ref": "#/" + p_schema_context.metadata.name.toLowerCase() + "/definitions/" + p_schema_context.metadata.name.toLowerCase() 
              }
            ]
        }
    };

    return result;
}


function convert_to_enum_name(p_value)
{
    return p_value.replace(/\W/g,"_");
}