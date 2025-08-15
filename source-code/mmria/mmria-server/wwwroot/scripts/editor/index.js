var g_metadata = null;
var g_attachment_stub = null;
var g_data = null;
var g_copy_clip_board = null;
var g_delete_value_clip_board = null;
var g_delete_attribute_clip_board = null;
var g_delete_node_clip_board = null;

var g_ui = { is_collapsed : [] };

var g_path_to_csv_all = {};


var md = {
/*

required:
	name
	prompt	
	type

optional:
is_core_summary
is_required

regex_pattern
validation
onblur

max_value
min_value
control_style
radio
checkbox


controls:
	label
	button

*/
	create_app: function(p_user_name)
	{
		var date = new Date().toISOString(); 
		return {
			"_id": date,
			"_rev": null,
			"name":"mmria",
			"type":"app",
			"date_created": date,
			"created_by": p_user_name,
			"date_last_updated": date,
			"last_updated_by": p_user_name,
			"children":[]
			}
	},
	create_form: function(			
			p_name,
			p_prompt,
			p_cardinality)
	{
		return {
			"name": p_name,
			"prompt": p_prompt,
			"cardinality":p_cardinality,
			"type": "form",
			"children": []
		}

	},
	create_grid: function(p_name, p_prompt)
	{
		
			return {
				"name": p_name,
				"prompt": p_prompt,
				"type": "grid",
				"children": []
			}
	},
	create_group: function(p_name, p_prompt, p_type, p_style)
	{
		if(p_style)
		{
			return {
				"name": p_name,
				"prompt": p_prompt,
				"type": "group",
				"children": []
			}
		}
		else
		{
return {
				"name": p_name,
				"prompt": p_prompt,
				"type": "group",
				"children": []
			}
		}
	},
	create_chart: function(p_name, p_prompt)
	{
		return {
			"name": p_name,
			"prompt": p_prompt,
			"type": "chart",
			"x_axis": "",
			"y_axis":"",
			"x_label": "",
			"y_label":"",
			"x_type": "",
			"y_type":""
		}
	},
	create_value_list: function(p_name, p_prompt, p_type, p_data_type)
	{
		return {
			"name": p_name,
			"prompt": p_prompt,
			"type": p_type,
			"data_type": p_data_type,
			"sass_export_name": "",
			"values": []
		}
	},
	create_value: function(p_name, p_prompt, p_type)
	{ 
		let result = null;
		switch(p_type.toLowerCase())
		{
			case "button":
			case "label":
				result = {
					"name": p_name,
					"prompt": p_prompt,
					"type": p_type,
				}
				break;
		
			default:
				result = {
					"name": p_name,
					"prompt": p_prompt,
					"type": p_type,
					"sass_export_name": ""
				}
				break;
		}
		return result;
	}

};

var $$ = {


 is_id: function(value)
 {
   // 2016-06-12T13:49:24.759Z
    if(value)
    {
      var test = value.match(/^\d+-\d+-\d+T\d+:\d+:\d+.\d+Z$/);
      return (test)? true : false;
    }
    else
    {
        return false;
    }
  }
};

var metadata_changed_event = new Event('metadata_changed');

window.addEventListener('metadata_changed', function (e) 
{ 

	if(preview_window)
	{
		//preview_window.metadata_changed(g_metadata);

		if(last_preview_update)
		{
			var diff = new Date().getTime() - last_preview_update.getTime();

			if(diff > 1000)
			{
				preview_window.metadata_changed(g_metadata);
				last_preview_update = new Date();
			}
		}
		else
		{
			preview_window.metadata_changed(g_metadata);
			last_preview_update = new Date();
		}
	}
}, false);


var preview_window = null;
var last_preview_update = null;

function open_preview_window()
{
	if(! preview_window)
	{
		preview_window = window.open('./preview.html','_preview',null,false);

		window.setTimeout(function()
		{
			preview_window.metadata_changed(g_metadata);
			last_preview_update = new Date();
		}, 3000);
	}
	else
	{
		preview_window.metadata_changed(g_metadata);
		last_preview_update = new Date();
	}


}

window.onload = main;

async function main()
{//http://www.w3schools.com/html/html_layout.asp
  'use strict';
  //get_name_map_click();

/*
	profile.on_login_call_back = function (){
			load_metadata();
    };

  	profile.initialize_profile();
*/
	$(document).keydown(function(evt){
		if (evt.keyCode==83 && (evt.ctrlKey)){
			evt.preventDefault();
			metadata_save();
		}

		if (evt.keyCode==80 && (evt.ctrlKey)){
			evt.preventDefault();
			open_preview_window();
		}

		if (evt.keyCode==76 && (evt.ctrlKey)){
			evt.preventDefault();
			//profile.initialize_profile();
		}

	});

	
    await load_metadata()
	//create_check_code_submit();

	//window.setInterval(profile.update_session_timer, 120000);

}


function get_name_map_click()
{
    $.ajax({
            //url: 'http://test-mmria.services-dev.cdc.gov/api/metadata/2016-06-12T13:49:24.759Z',
            url: location.protocol + '//' + location.host + `/api/version/export-names/version_specification-20.07.13/all`
	}).done(function(response) {
            let file_map = eval("(" + response + ")");
            g_path_to_csv_all = {};
            //g_data.path_to_csv_all_field = {};

            for(let file_name in file_map)
            {
                let path_to_field = file_map[file_name];
                for(let path in path_to_field)
                {
                    let field_name = path_to_field[path];

                    g_path_to_csv_all[path] = { "file_name": file_name, "field_name": field_name };
                }
            }

			load_metadata();

	});
}





async function load_metadata()
{
	var metadata_url = location.protocol + '//' + location.host + '/api/metadata';

	g_metadata = await $.ajax({
			url: metadata_url
	});

    convert_value_to_object(g_metadata);

    g_data = create_default_object(g_metadata, {});
    g_ui.url_state = url_monitor.get_url_state(window.location.href);

    //document.getElementById('navigation_id').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");

    document.getElementById('form_content_id').innerHTML = editor_render(g_metadata, "", g_ui, "app").join("");

	
}


async function save_check_code()
{
	var metadata_url = location.protocol + '//' + location.host + '/api/metadata';

	$.ajax({
			url: metadata_url
	}).done(function(response) {
			g_metadata = response;

			convert_value_to_object(g_metadata);

			g_data = create_default_object(g_metadata, {});
			g_ui.url_state = url_monitor.get_url_state(window.location.href);

			//document.getElementById('navigation_id').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");

			document.getElementById('form_content_id').innerHTML = editor_render(g_metadata, "", g_ui, "app").join("");

	});
}




function convert_value_to_object(p_metadata)
{
	if(p_metadata.values)
	{
		for(var i = 0; i < p_metadata.values.length; i++)
		{
			var child = p_metadata.values[i];
			if (typeof child === 'string' || child instanceof String)
			{
				p_metadata.values[i] = { "value": child, "description": "", "display": "" };
			}
		}
	}

	if(p_metadata.children)
	{
		for(var i = 0; i < p_metadata.children.length; i++)
		{
			var child = p_metadata.children[i];
			convert_value_to_object(child);
		}
	}
}


function metadata_save()
{

	console.log("metadata_change");

	var current_auth_session = $mmria.getCookie("AuthSession");
	perform_save(current_auth_session);

/*
	if(current_auth_session == null && profile.auth_session != null)
	{ 
		current_auth_session = profile.auth_session;
		$mmria.addCookie("AuthSession", profile.auth_session);
	}

	if(current_auth_session)
	{ 
		perform_save(current_auth_session);
	}
	else
	{
		profile.try_session_login(perform_save);
	}
*/
}


function perform_save(current_auth_session)
{
	$.ajax({
			url: location.protocol + '//' + location.host + '/api/metadata',
			contentType: 'application/json; charset=utf-8',
			dataType: 'json',
			data: JSON.stringify(g_metadata),
			type: "POST"
	}).done(function(response) 
	{
				//var response_obj = JSON.parse(response);
				if(response.ok)
				{
					g_metadata._rev = response.rev; 
					
					document.getElementById('form_content_id').innerHTML = editor_render(g_metadata, "", g_ui, "app").join("");

					if(response.auth_session)
					{
						//profile.auth_session = response.auth_session;
						//$mmria.addCookie("AuthSession", response.auth_session);
					}
					perform_validation_save(g_metadata);



				}
				else
				{
					console.log("failed to save", response);
				}
				//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
			console.log("metadata sent", response);
	});
}

async function perform_validation_save(p_metadata, p_check_code_text)
{
	metadata_list = [];
	object_list = [];

	path_to_node_map = [];
	path_to_int_map = [];
	dictionary_path_to_int_map = [];
	dictionary_path_to_path_map = [];
	is_added_function_map = {};
	path_to_onblur_map = [];
	path_to_onclick_map = [];
	path_to_onfocus_map = [];
	path_to_onchange_map = [];
	path_to_source_validation = [];
	path_to_derived_validation = [];
	path_to_validation_description = [];
	object_path_to_metadata_path_map = [];
	g_ast = null;
	g_function_array = [];
	g_validator_ast = null;

	output_json = [];



	output_json.push("var path_to_int_map = [];\n");
	output_json.push("var dictionary_path_to_path_map = [];\n");
	output_json.push("var path_to_onblur_map = [];\n");
	output_json.push("var path_to_onclick_map = [];\n");
	output_json.push("var path_to_onfocus_map = [];\n");
	output_json.push("var path_to_onchange_map = [];\n");
	output_json.push("var path_to_source_validation = [];\n");
	output_json.push("var path_to_derived_validation = [];\n");
	output_json.push("var path_to_validation_description = [];\n");

	generate_validation(output_json, p_metadata, metadata_list, "", object_list, "", path_to_node_map, path_to_int_map, path_to_onblur_map, path_to_onclick_map, path_to_onfocus_map, path_to_onchange_map, path_to_source_validation, path_to_derived_validation, path_to_validation_description, object_path_to_metadata_path_map);
	

	try 
	{
		const response = await fetch(location.protocol + '//' + location.host + '/api/checkcode');

		//var temp_ast = escodegen.attachComments(p_metadata.global, p_metadata.global.comments, p_metadata.global.tokens);
		//g_ast = escodegen.attachComments(p_metadata.global, p_metadata.global.comments, p_metadata.global.tokens);
		//var global_code = escodegen.generate(temp_ast, { comment: true });
        const text = await response.text();
		g_ast = esprima.parse(text, { comment: true, loc: true });
	} 
	catch (e) 
	{
		console.log(e.message);
	}


	generate_global(output_json, p_metadata, g_ast);

	for(var key in dictionary_path_to_path_map)
	{
		if (dictionary_path_to_path_map.hasOwnProperty(key)) 
		{
			output_json.push("dictionary_path_to_path_map['");
			output_json.push(key);
			output_json.push("']='");
			output_json.push(get_eval_string(dictionary_path_to_path_map[key]));
			output_json.push("';\n");
		}
	}
	

	generate_derived_validator(output_json, p_metadata, "", path_to_int_map);



		$.ajax({
			url: location.protocol + '//' + location.host + '/api/validator',
			//contentType: 'application/json; charset=utf-8',
			contentType: 'multipart/form-data; charset=utf-8',
			dataType: 'text',
			data: output_json.join(""),
			type: "POST",/*
			beforeSend: function (request)
			{
			  	//request.setRequestHeader("AuthSession", profile.get_auth_session_cookie());
			  	request.setRequestHeader("If-Match", g_metadata._rev);
			}*/
	}).done(function(response) 
	{

		if(response && response.ok == null)
		{
			response = JSON.parse(response);
		}
		
		if(response.ok)
		{
			g_metadata._rev = response.rev; 
			
			document.getElementById('form_content_id').innerHTML = editor_render(g_metadata, "", g_ui, "app").join("");
			console.log("perform_validation_save: complete");
		}

		
	}).fail(function(x) {
    console.log(x);
  });
}

function create_check_code_submit()
{

	$("#check_code_form").submit
	(	function(e)
		{
			e.preventDefault();

			var file_element = document.querySelector("#check_code_json");
			if(file_element)
			{
				if(file_element.value && file_element.value.endsWith("MMRIA_calculations.js"))
				{
					//alert(file_element.value);

					var file = document.getElementById('check_code_json').files[0]; //Files[0] = 1st file
					var reader = new FileReader();
					reader.readAsText(file, 'UTF-8');
					reader.onload = send_data;

					function send_data(event) 
					{
						var check_code_text = event.target.result;

						try 
						{
							syntax = esprima.parse(check_code_text, { tolerant: true, loc: true });
							errors = syntax.errors;
							if (errors.length > 0) 
							{
								alert("Syntax Error in Script Fix error before uploading");
							}
							else
							{
								var fileName = document.getElementById('check_code_json').files[0].name; //Should be 'picture.jpg'
								$.ajax
								({
									url: location.protocol + '//' + location.host + '/api/checkcode',
									data: check_code_text,
									contentType: 'multipart/form-data; charset=utf-8',
									dataType: 'text',
									type: "POST"/*,
									beforeSend: function (request)
									{
										//request.setRequestHeader("AuthSession", profile.get_auth_session_cookie());
										request.setRequestHeader("If-Match", g_metadata._rev);
									}*/

								}).done(function(response) 
								{
									console.log("PutCheckCode: complete");
									var response_obj = JSON.parse(response);
									if(response_obj.ok)
									{
										g_metadata._rev = response_obj.rev; 
										
										document.getElementById('form_content_id').innerHTML = editor_render(g_metadata, "", g_ui, "app").join("");
					
										if(response_obj.auth_session)
										{
											//profile.auth_session = response_obj.auth_session;
											//$mmria.addCookie("AuthSession", response_obj.auth_session);
										}
										
										perform_validation_save(g_metadata);
									}
									else
									{
										alert("PutCheckCode failed to save" + response_obj);
										console.log("PutCheckCode failed to save", response_obj);
									}

								}).fail(function(x) {
									alert("create_check_code_submit.send_data" + x);
								console.log("create_check_code_submit.send_data",x);
								});
							}
						}
						catch (e) 
						{
							alert("Syntax Error in Script Fix error before uploading\n" + e.toString());
							console.log("create_check_code_submit.send_data", e.toString());
							
						}
					}
				}
			}
			//e.preventDefault(); //Prevent Default action. 
			//e.unbind();
		}
	); 
}


var g_content_list = [];
var g_file_stat_list = [];

async function readmultifiles(event, files) 
{
    const self = $(event.target);
    let ul_list = [];
    g_file_stat_list = [];

    //show spinner
    self.next('.spinner-inline').fadeIn();

    for (let i = 0; i < files.length; i++) 
    {
        let item = files[i];
		if(item.name != "MMRIA_calculations.js") 
		{
			const el = document.getElementById("check_code_upload_result");
			el.innerText = `Invalid file ${item.name} must upload file named MMRIA_calculations.js`
			break;
		}
        readFile(i);
        g_file_stat_list.push({ name: item.name, index: i })

		const el = document.getElementById("check_code_upload_result");
		el.innerText = `Loaded ${item.name}`

		//await  SendCheckCode();

    }

    function readFile(index) 
    {
        if (index >= files.length) return;

        var file = files[index];
        var reader = new FileReader();
        reader.onload = function (e) {
            // get file content  
            g_content_list[index] = e.target.result;
            // do sth with bin
            readFile(index + 1)
        }
        reader.readAsText(file);



        window.setTimeout(SendCheckCode, 1000);
    }
}

async function SendCheckCode()
{
		//var fileName = document.getElementById('check_code_json').files[0].name; //Should be 'picture.jpg'

		const payload = {
			data: g_content_list[0]
		}
		$.ajax
		({
			url: location.protocol + '//' + location.host + '/api/checkcode',
			contentType: 'application/json; charset=utf-8',
			dataType: 'json',
			data: JSON.stringify(payload),
			type: "POST"/*,
			beforeSend: function (request)
			{
				//request.setRequestHeader("AuthSession", profile.get_auth_session_cookie());
				request.setRequestHeader("If-Match", g_metadata._rev);
			}*/

		}).done(function(response_obj) 
		{
			console.log("PutCheckCode: complete");
			//var response_obj = JSON.parse(response);
			if(response_obj.ok)
			{
				g_metadata._rev = response_obj.rev; 
				
				document.getElementById('form_content_id').innerHTML = editor_render(g_metadata, "", g_ui, "app").join("");

				if(response_obj.auth_session)
				{
					//profile.auth_session = response_obj.auth_session;
					//$mmria.addCookie("AuthSession", response_obj.auth_session);
				}
				
				perform_validation_save(g_metadata);
			}
			else
			{
				const el = document.getElementById("check_code_upload_result");
				el.innerText = `PutCheckCode failed to save ${response_obj}`

				//alert("PutCheckCode failed to save" + response_obj);
				console.log("PutCheckCode failed to save", response_obj);
			}

		}).fail(function(x) {

			const el = document.getElementById("check_code_upload_result");
			el.innerText = `create_check_code_submit.send_data ${x}`
			//alert("create_check_code_submit.send_data" + x);
		console.log("create_check_code_submit.send_data",x);
		});

}
