var schema = null;
var mmria_path_to_definition_name = null;
var g_data = null;
var base_api_url = location.protocol + '//' + location.host + "/api/version?path=";
var g_available_version_list = null;
var g_path_to_csv_all = {};



var g_MMRIA_Calculations = null;
var g_validation = null;
var g_ui_specification = null;


function main()
{
    document.getElementById("base_api_url").value = base_api_url;
    get_available_versions();
}



function get_available_versions()
{
  

  $.ajax
  ({

      url: location.protocol + '//' + location.host + '/api/version/list',
  })
  .done(function(response) 
  {

      g_available_version_list = response;

      render_available_version_list();
      
	});
}


function get_version_click()
{
    let version_id = document.getElementById("available_version").value;
  	$.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + `/api/metadata/${version_id}`
	}).done(function(response) {
            g_data = response;
            if(g_data.definition_set == null)
            {
                g_data.definition_set = {};
            }
            base_api_url = location.protocol + '//' + location.host + "/api/version/" + g_data.name + "/?path=";

            document.getElementById("base_api_url").value = base_api_url;

            render_selected_version();
	});
}

function get_name_map_click()
{
    document.getElementById("add_attachment_document_content").value = "";

    let version_id = document.getElementById("available_version").value;
  	$.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + `/api/version/export-names/${version_id}/all`
	}).done(function(response) {
            let file_map = eval("(" + response + ")");
            g_data.path_to_csv_all = {};
            //g_data.path_to_csv_all_field = {};

            for(let file_name in file_map)
            {
                let path_to_field = file_map[file_name];
                for(let path in path_to_field)
                {
                    //let field_name = path_to_field[path];
                    let field_name = get_sass_name(g_metadata, path, "");
                    
                    g_data.path_to_csv_all[path] = { "file_name": file_name, "field_name": field_name };
                }
            }
            get_core_name_map();
	});
}


function get_core_name_map()
{
    let version_id = document.getElementById("available_version").value;
  	$.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + `/api/version/export-names/${version_id}/core`
	}).done(function(response) {
            let file_map = eval("(" + response + ")");
            g_data.path_to_csv_core = {};
            //g_data.path_to_csv_core_field = {};

            for(let file_name in file_map)
            {
                let path_to_field = file_map[file_name];
                
                for(let path in path_to_field)
                {
                    //let field_name = path_to_field[path];
                    let field_name = get_sass_name(g_metadata, path, "");
                    g_data.path_to_csv_core[path] = { "file_name": file_name, "field_name": field_name };
                    
                }
            }

            document.getElementById("add_attachment_document_content").value = "get name map finished";
	});
}


function render_available_version_list()
{
    let available_version = document.getElementById("available_version");

    let result = []
    for(let i = 0; i < g_available_version_list.length; i++)
    {
      let item = g_available_version_list[i];
      let is_selected = "";
      if(i== 0)
      {
          is_selected = "selected=true"
      }
      if(item._id.indexOf("_design/auth") < 0 && item.name != null)
      {
          result.push(`<option value="${item._id}" ${is_selected}>${item.name}</option>`)
      }
    }
    available_version.innerHTML = result.join("");
}

function render_selected_version()
{
    document.getElementById("selected_version_name").innerHTML = g_data.name;
    document.getElementById("selected_version_2").innerHTML = g_data.name;
    document.getElementById("selected_publish_status").value = g_data.publish_status;

}

function get_metadata()
{
  	$.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + '/api/metadata'
	}).done(function(response) {
            g_metadata = response;
            g_metadata.version = g_data.name;
            g_data.metadata = JSON.stringify(g_metadata);

            get_MMRIA_Calculations();
	});
}

function get_MMRIA_Calculations()
{
  	$.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + '/api/metadata/GetCheckCode'
	}).done(function(response) {
            g_MMRIA_Calculations = response;
            get_validation()
	});
}

function get_validation()
{
  	$.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + '/api/validator'
	}).done(function(response) {
            g_validation = response;
            get_ui_specification();
	});
}

function get_ui_specification()
{
  	$.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + '/api/ui_specification/default_ui_specification'
	}).done(function(response) {
            g_ui_specification = response;
	});
}



function get_gen_from_metadata()
{
    g_metadata = response;
    schema = get_initial_schema();

    for(var child in g_metadata.lookup)
    {
        var child_node = g_metadata.lookup[child];
        set_lookUp(schema.definitions, child_node);
    }

    var schema_context = get_schema_context(g_metadata, schema, "v1",  "");

    var definition_context = get_definition_context(g_metadata, schema_context.schema.definitions, {}, "");
    set_definitions(definition_context);

    mmria_path_to_definition_name = definition_context.mmria_path_to_definition_name;

    generate_schema_phase2(schema_context);

    schema = schema_context.schema;

    //console.log(schema);
    let el = document.getElementById("json_schema")
        
    el.value = JSON.stringify(schema);
    //get_cs_code(schema);
	
}


function get_cs_code(p_schema)
{
    var code_gen_request = { action:"generate", payload: p_schema };
    $.ajax
    ({
            url: location.protocol + '//' + location.host + '/api/version_code_gen',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify(code_gen_request),
            type: "POST",
            headers:{ Accept: 'text/plain; charset=utf-8'}
    })
    .done
    (
        function(response) 
        {
            $("#generated_code").value = response;
        }
    ).fail
    (
        function(xhr, ajaxOptions, thrownError) 
        {
            $("#generated_code").val(xhr.responseText);
        }
    );
}

function convert_dictionary_path_to_lookup_object(p_path)
{

	//g_data.prenatal.routine_monitoring.systolic_bp
	var result = null;
	var temp_result = []
	var temp = "g_metadata." + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");
	var index = temp.lastIndexOf('.');
	temp_result.push(temp.substr(0, index));
	temp_result.push(temp.substr(index + 1, temp.length - (index + 1)));

	var lookup_list = eval(temp_result[0]);

	for(var i = 0; i < lookup_list.length; i++)
	{
		if(lookup_list[i].name == temp_result[1])
		{
			result = lookup_list[i].values;
			break;
		}
	}


	return result;
}


function load_metadata_click()
{
    get_metadata();
}
/*
function generate_schema_click()
{


    let path = document.getElementById("mmria_path").value;
    let selected_metatdata = document.getElementById("selected_metatdata");
    let json = eval("(" + selected_metatdata.value + ")");

    let schema = generate_schema_from_metadata(json, path);
    document.getElementById("json_schema").value = JSON.stringify(schema);

}

function generate_code_click()
{
    let el = document.getElementById("json_schema");

    let json = eval("(" + el.value + ")");

    get_cs_code(json);
}
*/


function get_sass_name(p_metadata, p_search_path, p_path)
{
    let result = null;
    switch(p_metadata.type.toLowerCase())
    {
        case "app":
        case "form":
        case "group":
            if
            (
                p_metadata.sass_export_name != null &&
                p_metadata.sass_export_name != "" && 
                p_search_path.toLowerCase() == p_path.toLowerCase()

            )
            {
                result = p_metadata.sass_export_name;
            }
            else for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                result = get_sass_name(child, p_search_path, p_path + "/" + child.name);
                if(result != null)
                {
                    break;
                }
            }
            break;
        case "grid":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                result = get_sass_name(child, p_search_path, p_path + "/" + child.name);
                if(result != null)
                {
                    break;
                }
            }
            break;
        default:
            if
            (
                p_metadata.sass_export_name != null &&
                p_metadata.sass_export_name != "" && 
                p_search_path.toLowerCase() == p_path.toLowerCase()

            )
            {
                result = p_metadata.sass_export_name;
            }
            break;
    }
    return result;
    
}

function create_new_version_click()
{
    let version_name = document.getElementById("version_name").value;
    if(version_name!= null && version_name != "")
    {
        g_data = {
            _id : "version_specification-" + version_name,
            data_type : "version-specification",
            date_created : new Date().toISOString(),
            created_by : "isu7@cdc.gov",
            date_last_updated :  new Date().toISOString(),
            last_updated_by : "isu7@cdc.gov",
            name: version_name,
            publish_status: 0,
            metadata: "",
            ui_specification:"",
            schema: { },
            definition_set: { },
            path_to_csv_all_file: {},
            path_to_csv_all_field: {},
            path_to_csv_core_file: {},
            path_to_csv_core_field: {}
        };


        g_available_version_list.push(g_data);

        render_available_version_list();
        render_selected_version();
    }

    
}


function save_version_click()
{
    $.ajax
    (
        {
        //url: location.protocol + '//' + location.host + '/api/version/save/' + g_data._id,
        url: location.protocol + '//' + location.host + '/api/version/save',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify(g_data),
        type: "POST"
        }
    )
    .done
    (
        function(case_response) 
        {
            console.log("save_case: success");
        
            if(g_data && g_data._id == case_response.id)
            {
                g_data._rev = case_response.rev;
            }
        }
    )
    .fail
    (
        function(xhr, err) 
        { 
            console.log("server save_case: failed", err); 
            
            if(xhr.status == 401)
            {
                /*
                let redirect_url = location.protocol + '//' + location.host;
                window.location = redirect_url;
                */
            }
            
        }
    );
}


function get_saved_version_spec()
{
  	$.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + '/api/metadata/' + g_data._id
	}).done(function(response) {
            g_data = response

            
            document.getElementById("selected_version_name").value = g_data.name;
            document.getElementById("selected_publish_status").value = g_data.publish_status;

	});
}

function show_selected_metadata_click()
{
    let path = document.getElementById("mmria_path").value;
    let selected_metatdata = document.getElementById("selected_metatdata");
    selected_metatdata.value = "";

    let source_metadata = eval("(" + g_data.metadata + ")");

    if(path != null && path != "")
    {

        

        if(path.indexOf("/lookup/") == 0)
        {
            let new_path = path.replace("/lookup/", "")
            for(let i = 0; i < source_metadata.lookup.length; i++)
            {
                let child = source_metadata.lookup[i];

                if(child.name.toLowerCase() == new_path.toLowerCase())
                {
                    //let metadata = source_metadata.lookup[new_path];
                    selected_metatdata.value = JSON.stringify(child);
                    break;
                }
    
            }
    
        }
        else
        {
            let metadata = find_metadata(source_metadata, path);
            selected_metatdata.value = JSON.stringify(metadata);
    
        }

    }
}



function pair_schema_click()
{
    let path = document.getElementById("mmria_path").value;
    let json_schema = document.getElementById("json_schema");
    g_data.schema[path] = json_schema.value;
}


function set_definition_click()
{
    let path = document.getElementById("mmria_path").value;
    let json_schema = document.getElementById("json_schema");
    g_data.definition_set[path] = json_schema.value;
}


function find_metadata(p_metadata, p_path)
{
    var result = null;
    let path_array = p_path.split("/");
    
    switch(p_metadata.type.toLowerCase())
    {
        case "app":
                if
                (
                    path_array.length == 1 ||
                    (
                        path_array.length == 2 &&
                        path_array[0] == "" &&
                        path_array[1] == ""
                    )
                )
                {
                    result =  p_metadata;
                }
                else
                {
                    for(let i = 0; i < p_metadata.children.length; i++)
                    {
                        let child = p_metadata.children[i];
                        let new_path = path_array.slice(1, path_array.length);
    
                        result = find_metadata(child, new_path.join("/"))
                        if(result)
                        {
                            break;
                        }
    
                    }
                }
                break;            
        case "form":
        case "group":
        case "grid":
            if
            (
                path_array.length == 1
            )
            {
                if(path_array[0].toLowerCase() == p_metadata.name.toLowerCase())
                {
                    result =  p_metadata;
                }
            }
            else
            {
                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let child = p_metadata.children[i];
                    let new_path = path_array.slice(1, path_array.length);

                    result = find_metadata(child, new_path.join("/"))
                    if(result)
                    {
                        break;
                    }

                }
            }
        break;
        default:
            if(p_path.toLowerCase() == p_metadata.name.toLowerCase())
            {
                result = p_metadata;
            }
            break;

    }
    return result;
}



function generate_schema_from_metadata(p_metadata, p_path)
{
    let result = null;

    switch(p_metadata.type.toLowerCase())
    {
        case "app":
                result = { 
                    "$schema": "http://json-schema.org/draft-04/schema#",
                    "$id": base_api_url + p_path,
                    "title": "MMRIA_Case",
                    "description": "Maternal Mortality Review Information Application (MMRIA) case schema",
                    "type": "object",
                    "properties" :
                    {
                        "_id": {"type": "string" },
                        "_rev": {"type": "string" },
                        "version": {
                            "type": "string",
                            "enum": [ g_data.name ],
                            "default" : g_data.name
                        }
        
                    },
                    "definitions": {},
                    "required": ["_id", "version"]
                };

                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let child = p_metadata.children[i];
                    switch(child.type.toLowerCase())
                    {
                        case "form":
                            result.properties[child.name] = { 
                                "type": "object",
                                "$ref": base_api_url + p_path + child.name
                            };

                            break;
                        case "group":
                        case "grid":
                            result.properties[child.name] = { 
                                "type": "object",
                                "$ref": base_api_url + p_path + child.name
                            };
                        break;
                        default:
                            Array.prototype.push.apply(result, generate_schema_from_metadata(child, p_path + child.name));
                            break;
                    }
                }
            break;
        case "form":
        case "group":
        case "grid":
            result = { 
                "$schema": "http://json-schema.org/draft-04/schema#",
                "$id": base_api_url + p_path,
                "title": p_metadata.name,
                "type": "object",
                "properties" :
                {
                    
                }
                
            };

            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                switch(child.type.toLowerCase())
                {
                    case "group":
                    case "grid":
                        result.properties[child.name] = { 
                            "type": "object",
                            "$ref": base_api_url + p_path + "/" + child.name
                        };
                    break;
                    default:
                        Array.prototype.push.apply(result, generate_schema_from_metadata(child, p_path + "/" + child.name));
                        break;
                }
            }
            break;
        case "number":
            result = { "type" : "number"}
            break;                    
        case "string":
            result = { "type" : "string"}
            break;
        case "date":
            result = { "type" : "string", "format": "date" }
            break;            
        case "time":
            result = { "type" : "string", "format": "time" }
            break;            
        case "datetime":            
            result = { "type" : "string", "format": "date-time" }
            break;
        case "list":
                
        
            if(p_path.indexOf("/lookup/") == 0)
            {
                result = { };
                result[p_metadata.name] = { "type" : p_metadata.data_type, "x-enumNames": [], "enum": [] };


                for(let j = 0; j < p_metadata.values.length; j++ )
                {
                    result[p_metadata.name]["x-enumNames"].push(p_metadata.values[j].display);
                    result[p_metadata.name]["enum"].push(p_metadata.values[j].value);
                }

            }
            else
            {

                let data_type = "string";

                if
                (
                    p_metadata.data_type != null &&
                    p_metadata.data_type != ""
                )
                {
                    data_type = p_metadata.data_type;
                }

                if
                (
                    p_metadata.is_multiselect != null &&
                    p_metadata.is_multiselect == "true"
                )
                {
                    let data_value_list = p_metadata.values;

                    if
                    (
                        p_metadata.path_reference &&
                        p_metadata.path_reference != ""
                    )
                    {
                        data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
                
                        if(data_value_list == null)	
                        {
                            data_value_list = p_metadata.values;
                        }

                        
                        result = { "type": "array", "items": { "type": data_type, "x-enumNames": [], "enum":[] } }

                        for(let j = 0; j < data_value_list.length; j++ )
                        {
                            result["items"]["x-enumNames"].push(data_value_list[j].display);
                            result["items"]["enum"].push(data_value_list[j].value);
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
                        result = { "type": "array", "items": { "type": data_type, "x-enumNames": [], "enum":[] } }

                        for(let j = 0; j < p_metadata.values.length; j++ )
                        {
                            result["items"]["x-enumNames"].push(p_metadata.values[j].display);
                            result["items"]["enum"].push(p_metadata.values[j].value);
                        }
                    }

                }
                else
                {

                    

                    if(p_metadata.path_reference && p_metadata.path_reference != "")
                    {
                        data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
                
                        if(data_value_list == null)	
                        {
                            data_value_list = p_metadata.values;

                        }


                        result = { "type": data_type,  "x-enumNames": [], "enum":[] }
                            
                        for(let j = 0; j < data_value_list.length; j++ )
                        {
                            result["x-enumNames"].push(data_value_list[j].display);
                            result["enum"].push(data_value_list[j].value);
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
                        result = { "type": data_type, "x-enumNames": [], "enum":[] }

                        for(let j = 0; j < p_metadata.values.length; j++ )
                        {
                            result["x-enumNames"].push(p_metadata.values[j].display);
                            result["enum"].push(p_metadata.values[j].value);
                        }
                    }
                }
            }
            break;            
        default:
            result = {"type": "string"}
            break;
    }

    return result;

}



function set_all_lists_in_definition_click()
{

    let metadata = eval("(" + g_data.metadata + ")")
    g_metadata = metadata;
    
    set_all_lists_in_definition(g_data.definition_set, metadata, "")
}


function generate_definition_from_metadata(p_metadata)
{

    let result = { "type" : p_metadata.data_type, "x-enumNames": [], "enum": [] };


    for(let j = 0; j < p_metadata.values.length; j++ )
    {
        result["x-enumNames"].push(p_metadata.values[j].display);
        result["enum"].push(p_metadata.values[j].value);
    }



    return result;

}

/*
function generate_definition_click()
{

    let path = document.getElementById("mmria_path").value;
    let selected_metatdata = document.getElementById("selected_metatdata");
    let json = eval("(" + selected_metatdata.value + ")");

    let schema = generate_definition_from_metadata(json);
    document.getElementById("json_schema").value = JSON.stringify(schema);
}
*/

function set_all_lists_in_definition(p_definitions, p_metadata, p_path)
{
    let result = null;

    switch(p_metadata.type.toLowerCase())
    {
        case "app":
        case "form":
        case "group":
        case "grid":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                set_all_lists_in_definition(p_definitions, child, p_path + "/" + child.name);
            }
            break;
        case "list":
            /*
                p_definitions[p_path] = { "type" : p_metadata.data_type, "x-enumNames": [], "enum": [] };

                for(let j = 0; j < p_metadata.values.length; j++ )
                {
                    p_definitions[p_path]["x-enumNames"].push(p_metadata.values[j].display);
                    p_definitions[p_path]["enum"].push(p_metadata.values[j].value);
                }
            */

            let data_type = "string";

            if
            (
                p_metadata.data_type != null &&
                p_metadata.data_type != ""
            )
            {
                data_type = p_metadata.data_type;
            }

            if
            (
                p_metadata.is_multiselect != null &&
                p_metadata.is_multiselect == "true"
            )
            {
                let data_value_list = p_metadata.values;

                if
                (
                    p_metadata.path_reference &&
                    p_metadata.path_reference != ""
                )
                {
                    data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
            
                    if(data_value_list == null)	
                    {
                        data_value_list = p_metadata.values;
                    }

                    
                    p_definitions[p_path] = { "type": "array", "items": { "type": data_type, "x-enumNames": [], "enum":[] } }

                    for(let j = 0; j < data_value_list.length; j++ )
                    {
                        p_definitions[p_path]["items"]["x-enumNames"].push(data_value_list[j].display);
                        p_definitions[p_path]["items"]["enum"].push(data_value_list[j].value);
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
                    p_definitions[p_path] = { "type": "array", "items": { "type": data_type, "x-enumNames": [], "enum":[] } }

                    for(let j = 0; j < p_metadata.values.length; j++ )
                    {
                        p_definitions[p_path]["items"]["x-enumNames"].push(p_metadata.values[j].display);
                        p_definitions[p_path]["items"]["enum"].push(p_metadata.values[j].value);
                    }
                }

            }
            else
            {

                

                if(p_metadata.path_reference && p_metadata.path_reference != "")
                {
                    data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
            
                    if(data_value_list == null)	
                    {
                        data_value_list = p_metadata.values;

                    }


                    p_definitions[p_path] = { "type": data_type,  "x-enumNames": [], "enum":[] }
                        
                    for(let j = 0; j < data_value_list.length; j++ )
                    {
                        p_definitions[p_path]["x-enumNames"].push(data_value_list[j].display);
                        p_definitions[p_path]["enum"].push(data_value_list[j].value);
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
                    p_definitions[p_path] = { "type": data_type, "x-enumNames": [], "enum":[] }

                    for(let j = 0; j < p_metadata.values.length; j++ )
                    {
                        p_definitions[p_path]["x-enumNames"].push(p_metadata.values[j].display);
                        p_definitions[p_path]["enum"].push(p_metadata.values[j].value);
                    }
                }
            }

            break;            
        case "number":
        case "string":
        case "date":
        case "time":
        case "datetime":            
        default:
            break;
    }

    return result;

}


function add_attachment_click(p_value)
{
    const doc_name = p_value.value.toLowerCase();
    document.getElementById("add_attachment_id").value = g_data._id;
    document.getElementById("add_attachment_rev").value = g_data._rev;
    document.getElementById("add_attachment_doc_name").value = doc_name;


    switch(doc_name)
    {
        case "metadata":
            g_metadata.version = g_data.name;
            document.getElementById("add_attachment_document_content").value = JSON.stringify(g_metadata);
            add_attachment(g_data._id, g_data._rev, doc_name, g_metadata)
            break;
        case "mmria_calculations":
            document.getElementById("add_attachment_document_content").value = g_MMRIA_Calculations;
            add_attachment(g_data._id, g_data._rev, doc_name, g_MMRIA_Calculations)
            break;
        case "validation":
            document.getElementById("add_attachment_document_content").value = g_validation;
            add_attachment(g_data._id, g_data._rev, doc_name, g_validation)
            break;
        case "ui_specification":
            document.getElementById("add_attachment_document_content").value = JSON.stringify(g_ui_specification);
            add_attachment(g_data._id, g_data._rev, doc_name, g_ui_specification)
            break;
        
    }
}

function add_attachment(p_id, p_rev, p_doc_name, p_content) 
{
    //add_attachment/{_id}/{_rev}/{doc_name}
    ///${p_rev}/${p_doc_name}

    $.ajax({
        //url: `${location.protocol}//${location.host}/api/version_attach/add/${p_id}`,
        //url: `${location.protocol}//${location.host}/api/version_attach/add`,
        //url: `${location.protocol}//${location.host}/api/version_attach/add/${p_id}`,
        url: `${location.protocol}//${location.host}/api/version_attach`,
        //contentType: 'application/json; charset=utf-8',
        contentType: 'multipart/form-data; charset=utf-8',
        //dataType: 'text',
        data:  $("#add_attachment").serialize(),
        type: "POST"
}).done(function(response) 
{

    if(response && response.ok == null)
    {
        response = JSON.parse(response);
    }
    
    if(response.ok)
    {
        g_data._rev = response.rev; 
        console.log("add_attachment: complete");
    }

    
})
.fail
(
    function(x) 
    {
        console.log(x);
    }
);
}
function base_api_url_change(p_value)
{

    base_api_url = p_value;
}

function selected_publish_status_change(p_value)
{
    g_data.publish_status = p_value;
}