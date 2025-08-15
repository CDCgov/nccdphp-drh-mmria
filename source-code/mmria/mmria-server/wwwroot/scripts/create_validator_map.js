
function get_metadata_eval_string(p_path)
{
	var result = "g_metadata" + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");

	return result;
}


function create_validator_map(p_validator_map, p_validation_description_map, p_metadata, p_path)
{

	if(p_metadata.validation_description && p_metadata.validation_description != '')
	{
			p_validation_description_map[p_path] = p_metadata.validation_description;
	}


	var result = [];
	result.push("( function (value)\n{\n ");
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
			var source_code = escodegen.generate(p_metadata.validation);
			var code_array = [];
			code_array.push("return ");
			code_array.push(source_code.substring(0, source_code.length-1));
			code_array.push("(value);\n")
			validation_added = true;
			Array.prototype.push.apply(result,code_array);
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

	result.push(" return true;\n})")

	if(validation_added)
	{
		p_validator_map[p_path] = eval(result.join(""));
	}
	
	if(p_metadata.children)
	{
		for(var i = 0; i < p_metadata.children.length; i++)
		{
			var child = p_metadata.children[i]
			create_validator_map(p_validator_map, p_validation_description_map, child, p_path + ".children[" + i + "]");
		}
	}

/*
if(p_metadata.is_required && p_metadata.is_required == true)
{

}

if(p_metadata.max_value && p_metadata.max_value == true)
{

}
 
if(p_metadata.min_value && p_metadata.min_value == true)
{

}

if(p_metadata.regex_pattern && p_metadata.regex_pattern  == true)
{

}

if(p_metadata.validator && p_metadata.validator == true)
{

}
*/
// is_read_only

}
