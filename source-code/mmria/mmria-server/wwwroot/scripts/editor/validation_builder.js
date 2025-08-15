

var metadata_list = [];
var object_list = [];

var path_to_node_map = [];
var path_to_int_map = [];
var dictionary_path_to_int_map = [];
var dictionary_path_to_path_map = [];
var is_added_function_map = {};
var path_to_onblur_map = [];
var path_to_onclick_map = [];
var path_to_onfocus_map = [];
var path_to_onchange_map = [];
var path_to_source_validation = [];
var path_to_derived_validation = [];
var path_to_validation_description = [];
var object_path_to_metadata_path_map = [];
var g_ast = null;
var g_function_array = [];
var g_validator_ast = null;

var output_json = [] 

function generate_global(p_output_json, p_metadata, p_ast)
{

		generate_dictionary_path_to_int_map(0, p_metadata, "", dictionary_path_to_int_map, "", dictionary_path_to_path_map);

		global_ast.properties = [];

        g_ast = p_ast;

		//var temp_ast = escodegen.attachComments(p_metadata.global, p_metadata.global.comments, p_metadata.global.tokens);
		//g_ast = escodegen.attachComments(p_metadata.global, p_metadata.global.comments, p_metadata.global.tokens);
		//var global_code = escodegen.generate(temp_ast, { comment: true });
		//g_ast = esprima.parse(global_code, { comment: true, loc: true });

		//map_ast(p_metadata.global, create_global_ast);
		map_ast(p_ast, create_global_ast, output_json);
		var ast = global_ast.generate();

		if(g_validator_ast && g_validator_ast != "")
		{
			ast.body.push(g_validator_ast);
		}

		Array.prototype.push.apply(ast.body, g_function_array);

		var test = get_code(ast);
		if(test)
		{
			p_output_json.push(test);
			p_output_json.push("\n");
		}
}

function generate_derived_validator(p_output_json, p_metadata, p_path,  p_path_to_int_map)
{

	var f_name = "x" + p_path_to_int_map[p_path].toString(16) + "_ob";
    if(is_added_function_map[f_name])
    {
			p_output_json.push("path_to_onblur_map['");
			p_output_json.push(p_path);
			p_output_json.push("']='");
			p_output_json.push(f_name);
			p_output_json.push("';\n");

    }

		f_name = "x" + p_path_to_int_map[p_path].toString(16) + "_ocl";
    if(is_added_function_map[f_name])
    {
			p_output_json.push("path_to_onclick_map['");
			p_output_json.push(p_path);
			p_output_json.push("']='");
			p_output_json.push("x" + p_path_to_int_map[p_path].toString(16) + "_ocl");
			p_output_json.push("';\n");

    }

		f_name = "x" + p_path_to_int_map[p_path].toString(16) + "_of";
    if(is_added_function_map[f_name])
    {
			p_output_json.push("path_to_onfocus_map['");
			p_output_json.push(get_eval_string(p_path));
			p_output_json.push("']='");
			p_output_json.push("x" + p_path_to_int_map[p_path].toString(16) + "_of");
			p_output_json.push("';\n");
		
    }

		f_name = "x" + p_path_to_int_map[p_path].toString(16) + "_och";
    if(is_added_function_map[f_name])
    {
			p_output_json.push("path_to_onchange_map['");
			p_output_json.push(p_path);
			p_output_json.push("']='");
			p_output_json.push("x" + p_path_to_int_map[p_path].toString(16) + "_och");
			p_output_json.push("';\n");

    }
 
		f_name = "x" + p_path_to_int_map[p_path].toString(16) + "_sv";
    if(is_added_function_map[f_name])
    {
				p_output_json.push("path_to_source_validation['");
				p_output_json.push(p_path);
				p_output_json.push("']='");
				p_output_json.push("x" + p_path_to_int_map[p_path].toString(16) + "_sv");
				p_output_json.push("';\n");

				var test = add_derived_validator_function(p_path_to_int_map, p_metadata, p_path)(p_path_to_int_map, p_metadata, p_path);
				if(test)
				{
					p_output_json.push("path_to_derived_validation['");
					p_output_json.push(p_path);
					p_output_json.push("']='");
					p_output_json.push("x" + p_path_to_int_map[p_path].toString(16) + "_dv");
					p_output_json.push("';\n");
				}
			}
    


	if(p_metadata.children && p_metadata.children.length > 0)
	{		
		for(var i = 0; i < p_metadata.children.length; i++)
		{
			var child = p_metadata.children[i];

			generate_derived_validator(p_output_json, child, p_path + "/children/" + i, p_path_to_int_map);
		}
	}

}


output_json.push("var path_to_int_map = [];\n");
output_json.push("var path_to_onblur_map = [];\n");
output_json.push("var path_to_onclick_map = [];\n");
output_json.push("var path_to_onfocus_map = [];\n");
output_json.push("var path_to_onchange_map = [];\n");
output_json.push("var path_to_source_validation = [];\n");
output_json.push("var path_to_derived_validation = [];\n");
output_json.push("var path_to_validation_description = [];\n");




function generate_dictionary_path_to_int_map(p_number, p_metadata, p_dictionary_path, p_dictionary_path_to_int_map, p_path, p_dictionary_path_to_path_map)
{
    p_dictionary_path_to_int_map[p_dictionary_path] = p_number;
	p_dictionary_path_to_path_map[p_dictionary_path] = p_path;

    if(p_metadata.children && p_metadata.children.length > 0)
    {		
        for(var i = 0; i < p_metadata.children.length; i++)
        {
            var child = p_metadata.children[i];
            if(p_dictionary_path == "")
            {
                generate_dictionary_path_to_int_map(p_number + 1, child, child.name, p_dictionary_path_to_int_map, p_path + "/children/" + i, p_dictionary_path_to_path_map);
            }
            else
            {
                generate_dictionary_path_to_int_map(p_number + 1, child, p_dictionary_path + "/" + child.name, p_dictionary_path_to_int_map, p_path + "/children/" + i, p_dictionary_path_to_path_map);
            }
        }
    }

}

function generate_validation(p_output_json, p_metadata, p_metadata_list, p_path, p_object_list, p_object_path, p_path_to_node_map, p_path_to_int_map, p_path_to_onblur_map, p_path_to_onclick_map, p_path_to_onfocus_map, p_path_to_onchange_map, p_path_to_source_validation, p_path_to_derived_validation, p_path_to_validation_description, p_object_path_to_metadata_path_map)
{
    p_path_to_node_map[p_path] = p_metadata;

    p_path_to_int_map[p_path] = p_metadata_list.length;
			p_output_json.push("path_to_int_map['");
			p_output_json.push(get_eval_string(p_path));
			p_output_json.push("']= ");
			p_output_json.push(p_path_to_int_map[p_path]);
			p_output_json.push(";\n");

	p_metadata_list.push(p_path);
	p_object_list.push(p_object_path);

    p_object_path_to_metadata_path_map[p_object_path] = p_path;



	
	if(p_metadata.validation_description && p_metadata.validation_description != '')
	{
			p_path_to_validation_description[p_path] = p_metadata.validation_description;

			p_output_json.push("path_to_validation_description['");
			p_output_json.push(p_path);
			p_output_json.push("']='");
			p_output_json.push(p_metadata.validation_description);
			p_output_json.push("';\n");
	}

	if(p_metadata.children && p_metadata.children.length > 0)
	{		
		for(var i = 0; i < p_metadata.children.length; i++)
		{
			var child = p_metadata.children[i];

 			generate_validation(p_output_json, child, p_metadata_list, p_path + "/children/" + i, p_object_list, p_object_path + "/" + child.name, p_path_to_node_map, p_path_to_int_map, p_path_to_onblur_map, p_path_to_onclick_map, p_path_to_onfocus_map, p_path_to_onchange_map, p_path_to_source_validation, p_path_to_derived_validation, p_path_to_validation_description, p_object_path_to_metadata_path_map);
		}
	}

}


function add_derived_validator_function(p_path_to_int_map, p_metadata, p_path)
{

	var result = [];
	result.push("\n function x");
	result.push(p_path_to_int_map[p_path].toString(16));
	result.push("_dv (value)\n{\n ");
	var validation_added = false;
 
	// is required
	// max_value
	// min_value
	// regex_pattern
	// validator
	// is_read_only


	if(p_metadata.is_read_only && p_metadata.is_read_only == true)
	{
			result.push("\nreturn false;\n");
			validation_added = true;
	}

	switch(p_metadata.type.toLowerCase())
	{
		case "number":
			if(p_metadata.is_required && p_metadata.is_required == true)
			{
					result.push("if(value == null || value == '') return false;\n");
					validation_added = true;
			}

			if(p_metadata.max_value && p_metadata.max_value == true)
			{
					result.push("if(value > new Number(");
					result.push(p_metadata.max_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			
			if(p_metadata.min_value && p_metadata.min_value == true)
			{
					result.push("if(value < new Number(");
					result.push(p_metadata.min_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			break;
		case "date":
		case "datetime":
			if(p_metadata.is_required && p_metadata.is_required == true)
			{
					result.push("if(value == null || value == '') return false;\n");
					validation_added = true;
			}

			if(p_metadata.max_value && p_metadata.max_value == true)
			{
					result.push("if(value > new Date(");
					result.push(p_metadata.max_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			
			if(p_metadata.min_value && p_metadata.min_value == true)
			{
					result.push("if(value < new Date(");
					result.push(p_metadata.min_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			break;
		case "string":
		default:
			if(p_metadata.is_required && p_metadata.is_required == true)
			{
				result.push("if(value == null || value == '') return false;\n");
				validation_added = true;
			}

			if(p_metadata.max_value && p_metadata.max_value == true)
			{
					result.push("if(value.length > new Number(");
					result.push(p_metadata.max_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			
			if(p_metadata.min_value && p_metadata.min_value == true)
			{
					result.push("if(value.length < new Number(");
					result.push(p_metadata.min_value);
					result.push(")) return false;\n");
					validation_added = true;
			}			
			break;
	}

	if(p_metadata.regex_pattern && p_metadata.regex_pattern.length > 0)
	{
		try
		{
			var reg_exp = new RegExp(p_metadata.regex_pattern);
		
			result.push("var regexp = /");
			result.push(p_metadata.regex_pattern);
			result.push("/;\nvar matches_array = value.match(regexp);");
			result.push("\nif(matches_array)\n{\t if(matches_array.length < 1) return false;\n\t}\n else return false;\n\n");
			validation_added = true;
		}
		catch(err)
		{

		}
	}

	var f_name = "x" + p_path_to_int_map[p_path].toString(16) + "_sv";

	if
	(
		is_added_function_map[f_name]
	)
	{
		try
		{
			result.push("\n return x");
			result.push(p_path_to_int_map[p_path].toString(16));
			result.push("_sv(value);\n");
			validation_added = true;
		}
		catch(err)
		{
			/*
			try
			{
				var code_array = [];
				code_array.push("return ");
				code_array.push(p_metadata.validation);
				code_array.push("(value);\n")
				validation_added = true;
				Array.prototype.push.apply(result,code_array);
			}
			catch(err2)
			{

			}*/
			
		}
		
	}

	result.push("\n}");

	//result.push(" return true;\n})");

	if(validation_added)
	{
		return result.join("");
	}
	else
	{
		return "";
	}

}

function create_derived_validator_function(p_path_to_int_map, p_metadata, p_path)
{

	var result = [];
	result.push("\n function x");
	result.push(p_path_to_int_map[p_path].toString(16));
	result.push("_dv (value)\n{\n ");
	var validation_added = false;
 
	// is required
	// max_value
	// min_value
	// regex_pattern
	// validator
	// is_read_only


	if(p_metadata.is_read_only && p_metadata.is_read_only == true)
	{
			result.push("\nreturn false;\n");
			validation_added = true;
	}

	switch(p_metadata.type.toLowerCase())
	{
		case "number":
			if(p_metadata.is_required && p_metadata.is_required == true)
			{
					result.push("if(value == null || value == '') return false;\n");
					validation_added = true;
			}

			if(p_metadata.max_value && p_metadata.max_value == true)
			{
					result.push("if(value > new Number(");
					result.push(p_metadata.max_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			
			if(p_metadata.min_value && p_metadata.min_value == true)
			{
					result.push("if(value < new Number(");
					result.push(p_metadata.min_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			break;
		case "date":
		case "datetime":
			if(p_metadata.is_required && p_metadata.is_required == true)
			{
					result.push("if(value == null || value == '') return false;\n");
					validation_added = true;
			}

			if(p_metadata.max_value && p_metadata.max_value == true)
			{
					result.push("if(value > new Date(");
					result.push(p_metadata.max_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			
			if(p_metadata.min_value && p_metadata.min_value == true)
			{
					result.push("if(value < new Date(");
					result.push(p_metadata.min_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			break;
		case "string":
		default:
			if(p_metadata.is_required && p_metadata.is_required == true)
			{
				result.push("if(value == null || value == '') return false;\n");
				validation_added = true;
			}

			if(p_metadata.max_value && p_metadata.max_value == true)
			{
					result.push("if(value.length > new Number(");
					result.push(p_metadata.max_value);
					result.push(")) return false;\n");
					validation_added = true;
			}
			
			if(p_metadata.min_value && p_metadata.min_value == true)
			{
					result.push("if(value.length < new Number(");
					result.push(p_metadata.min_value);
					result.push(")) return false;\n");
					validation_added = true;
			}			
			break;
	}

	if(p_metadata.regex_pattern && p_metadata.regex_pattern.length > 0)
	{
		try
		{
			var reg_exp = new RegExp(p_metadata.regex_pattern);
		
			result.push("var regexp = /");
			result.push(p_metadata.regex_pattern);
			result.push("/;\nvar matches_array = value.match(regexp);");
			result.push("\nif(matches_array)\n{\t if(matches_array.length < 1) return false;\n\t}\n else return false;\n\n");
			validation_added = true;
		}
		catch(err)
		{

		}
	}


	if
	(
		p_metadata.validation && 
		p_metadata.validation != ""
	)
	{
		try
		{

			result.push("\n return x");
			result.push(p_path_to_int_map[p_path].toString(16));
			result.push("_sv(value);\n");
			validation_added = true;

			//[var].body[0].id.name
			//p_metadata.validataion.body[0].id.name = "x" + p_path_to_int_map[p_path].toString(16) + "_sv";
			/*
			var source_code = escodegen.generate(p_metadata.validation);
			var code_array = [];
			code_array.push("return ");
			code_array.push(source_code);
			code_array.push("(value);\n");
			validation_added = true;
			Array.prototype.push.apply(result,code_array);
			*/
			
			
		}
		catch(err)
		{
			/*
			try
			{
				var code_array = [];
				code_array.push("return ");
				code_array.push(p_metadata.validation);
				code_array.push("(value);\n")
				validation_added = true;
				Array.prototype.push.apply(result,code_array);
			}
			catch(err2)
			{

			}*/
			
		}
		
	}

	result.push("\n}");

	//result.push(" return true;\n})");

	if(validation_added)
	{
		return result.join("");
	}
	else
	{
		return "";
	}

}


function get_code(p_value)
{
	var result = null;

	try
	{
		result = escodegen.generate(p_value);
	}
	catch(e)
	{
		console.log(e);
	}

	return result;
}

//[var].body[0].id.name

var test_exp = {
            "type": "Program",
            "body": [
              {
                "type": "FunctionDeclaration",
                "id": {
                  "type": "Identifier",
                  "name": "f"
                },
                "params": [
                  {
                    "type": "Identifier",
                    "name": "p_control"
                  }
                ],
                "body": {
                  "type": "BlockStatement",
                  "body": [
                    {
                      "type": "IfStatement",
                      "test": {
                        "type": "BinaryExpression",
                        "operator": "==",
                        "left": {
                          "type": "MemberExpression",
                          "computed": false,
                          "object": {
                            "type": "Identifier",
                            "name": "p_control"
                          },
                          "property": {
                            "type": "Identifier",
                            "name": "value"
                          }
                        },
                        "right": {
                          "type": "Literal",
                          "value": "",
                          "raw": "\"\""
                        }
                      },
                      "consequent": {
                        "type": "BlockStatement",
                        "body": [
                          {
                            "type": "ExpressionStatement",
                            "expression": {
                              "type": "AssignmentExpression",
                              "operator": "=",
                              "left": {
                                "type": "MemberExpression",
                                "computed": false,
                                "object": {
                                  "type": "ThisExpression"
                                },
                                "property": {
                                  "type": "Identifier",
                                  "name": "first_name"
                                }
                              },
                              "right": {
                                "type": "Literal",
                                "value": "bubba",
                                "raw": "\"bubba\""
                              }
                            }
                          }
                        ]
                      },
                      "alternate": null
                    }
                  ]
                },
                "generator": false,
                "expression": false
              }
            ],
            "sourceType": "script"
          }



var global_ast =  {
	properties: [],
	generate: function() { return {
    "type": "Program",
    "body": [
{
        "type": "VariableDeclaration",
        "declarations": [
          {
            "type": "VariableDeclarator",
            "id": {
              "type": "Identifier",
              "name": "$global"
            },
            "init": {
              "type": "ObjectExpression",
              "properties": global_ast.properties
            }
          }
        ],
        "kind": "var"
      }
    ],
    "sourceType": "script"
  }}
	
};


var create_property_ast = function(
	p_name,
	p_params,
	p_body
) 
{
	return {
                  "type": "Property",
                  "key": {
                    "type": "Identifier",
                    "name": p_name
                  },
                  "computed": false,
                  "value": {
                    "type": "FunctionExpression",
                    "id": null,
                    "params": p_params,
                    "body": p_body,
                    "generator": false,
                    "expression": false
                  },
                  "kind": "init",
                  "method": false,
                  "shorthand": false
                }

};

function map_ast(object, f, p_output) 
{
				var key, child;

				if (f.call(null, object, p_output) === false) {
						return;
				}
				for (key in object) {
						if (object.hasOwnProperty(key)) {
								child = object[key];
								if (typeof child === 'object' && child !== null) {
										map_ast(child, f, p_output);
								}
						}
				}
}

function create_global_ast(x, p_output_json)
{
     if(x.type)
		 {
				if(x.type == "FunctionDeclaration")
				{
						if(x.id.name.indexOf('$validator') == 0)
						{
								g_validator_ast = x;
						}
						else if(x.id.name.indexOf('$') == 0)
						{
							var res = create_property_ast(x.id.name.substring(1), x.params, x.body);
							global_ast.properties.push(res);
						}
						else
						{
								var path_and_event = find_path_and_event(x.loc.start.line, g_ast.comments);
								if(path_and_event && path_and_event.path && path_and_event.event)
								{
									var dictionary_path = path_and_event.path;
									var p_path = dictionary_path_to_path_map[dictionary_path];
									var f_name = "x" + path_to_int_map[p_path].toString(16);
									switch(path_and_event.event)
									{
											case 'onblur':
													f_name += "_ob";
													p_output_json.push("\n");
													p_output_json.push("path_to_onblur_map['");
													p_output_json.push(get_eval_string(p_path));
													p_output_json.push("']='");
													p_output_json.push(f_name);
													p_output_json.push("';\n");

													break;
											case 'onfocus':
													f_name += "_of";
													p_output_json.push("\n");
													
													p_output_json.push("path_to_onfocus_map['");
													p_output_json.push(get_eval_string(p_path));
													p_output_json.push("']='");
													p_output_json.push(f_name);
													p_output_json.push("';\n");													
													break;
											case 'onclick':
													f_name += "_ocl";
													p_output_json.push("\n");
													
													p_output_json.push("path_to_onclick_map['");
													p_output_json.push(get_eval_string(p_path));
													p_output_json.push("']='");
													p_output_json.push(f_name);
													p_output_json.push("';\n");													
													break;
											case 'onchange':
													f_name += "_och";
													p_output_json.push("\n");
													
													p_output_json.push("path_to_onchange_map['");
													p_output_json.push(get_eval_string(p_path));
													p_output_json.push("']='");
													p_output_json.push(f_name);
													p_output_json.push("';\n");	
													break;												
											case 'validate':
													f_name += "_sv";
													p_output_json.push("\n");

													p_output_json.push("path_to_source_validation['");
													p_output_json.push(get_eval_string(p_path));
													p_output_json.push("']='");
													p_output_json.push(f_name);
													p_output_json.push("';\n");

													break;
									}

									x.id.name = f_name;
									is_added_function_map[f_name] = true;
									g_function_array.push(x)
									 
								}
						}
						

				}
		 }
}

function find_path_and_event(p_start_line, p_comment_ast)
{
	var result = null;

	for(var i = 0; i < p_comment_ast.length; i++)
	{
		var comment = p_comment_ast[i];
		var diff = p_start_line - comment.loc.end.line;
		if(diff > 0 && diff < 3)
		{
				result = { path: null, event: null };

				var check = comment.value.split("\n");
				for(var j = 0; j < check.length; j++)
				{
					if(check[j].indexOf("path=") >= 0)
					{
							var pair = check[j].split("=");
							result.path = pair[1].trim();

					}
					else if(check[j].indexOf("event=") >= 0)
					{
							var pair = check[j].split("=");
							result.event = pair[1].trim();
					}
				}
				break;
		}
	}

	if(result && result.path && result.event)
	{
		return result;
	}
	else
	{
		return null;
	}
	
}