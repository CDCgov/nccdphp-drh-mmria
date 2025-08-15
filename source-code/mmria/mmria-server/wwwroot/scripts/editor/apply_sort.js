
function g_apply_sort(p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path)
{
    switch(p_metadata.type.toLowerCase())
	{
		case 'grid':
            let sort_function = g_apply_sort_create_sort(p_metadata, p_dictionary_path);

            if(sort_function != null)
            {
                p_data.sort(sort_function);
            }
            

			for (let i = 0; i < p_metadata.children.length; i++) 
            {
                let child = p_metadata.children[i];
                if (p_data[child.name] != null) 
                {
                    g_apply_sort(child, p_data[child.name], p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name);
                }
            }
			break;

		case 'group':
            for (let i = 0; i < p_metadata.children.length; i++) 
            {
                let child = p_metadata.children[i];
                if (p_data[child.name] != null) 
                {
                    g_apply_sort(child, p_data[child.name], p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name);
                }
            }
			break;

		case 'form':
            if (p_metadata.cardinality == "+" || p_metadata.cardinality == "*") 
            {

                let sort_function = g_apply_sort_create_sort(p_metadata, p_dictionary_path);

                if(sort_function != null)
                {
                    p_data.sort(sort_function);
                }

                for (let i = 0; i < p_data.length; i++) 
                {
                    let item = p_data[i];

                    for (let i = 0; i < p_metadata.children.length; i++) 
                    {
                        let child = p_metadata.children[i];
                        if (item[child.name] != null) 
                        {
                            g_apply_sort(child, item[child.name], p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name);
                        }
                    }
                }
            }
            else
            {
                for (let i = 0; i < p_metadata.children.length; i++) 
                {
                    let child = p_metadata.children[i];
                    if (p_data[child.name] != null) 
                    {
                        g_apply_sort(child, p_data[child.name], p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name);
                    }
                }
            }
			break;

		case 'app':
            for (let i = 0; i < p_metadata.children.length; i++) 
            {
                let child = p_metadata.children[i];

                if (child.type.toLowerCase() == 'form') 
                {
                    if (p_data[child.name] != null) 
                    {
                        g_apply_sort(child, p_data[child.name], p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name);
                    }
                }
            }
			break;

		case 'label':
		case 'button':
		case 'string':
		case 'address':
		case 'textarea':
        case 'html_area':
		case 'number':
		case 'boolean':
		case 'list':
		case 'date':
		case 'datetime':
		case 'time':
		case 'chart':
		case 'hidden':
		case 'jurisdiction':
            break;

		default:
			console.log("page_render not processed", p_metadata);
			break;
	}

}


function g_find_metadata_node_by_path(p_metadata, p_dictionary_path, p_find_path)
{
    let result = null;

    switch(p_metadata.type.toLowerCase())
	{
		case 'grid':
			for (let i = 0; i < p_metadata.children.length; i++) 
            {
                let child = p_metadata.children[i];

                result = g_find_metadata_node_by_path(child, p_dictionary_path + "/" + child.name, p_find_path);
                
                if(result != null) break;
            }
			break;

		case 'group':
            for (let i = 0; i < p_metadata.children.length; i++) 
            {
                let child = p_metadata.children[i];

                result = g_find_metadata_node_by_path(child, p_dictionary_path + "/" + child.name, p_find_path);
                
                if(result != null) break;
            }
			break;

		case 'form':

            for (let i = 0; i < p_metadata.children.length; i++) 
            {
                let child = p_metadata.children[i];
                result = g_find_metadata_node_by_path(child, p_dictionary_path + "/" + child.name, p_find_path);
                
                if(result != null) break;
            }
            
			break;

		case 'app':
            for (let i = 0; i < p_metadata.children.length; i++) 
            {
                let child = p_metadata.children[i];

                if (child.type.toLowerCase() == 'form') 
                {
                    if (p_data[child.name] != null) 
                    {
                        result = g_find_metadata_node_by_path(child, p_dictionary_path + "/" + child.name, p_find_path);
                    }
                    if(result != null) break;
                }
            }
			break;

		case 'label':
		case 'button':
		case 'string':
		case 'address':
		case 'textarea':
		case 'number':
		case 'boolean':
		case 'list':
		case 'date':
		case 'datetime':
		case 'time':
		case 'chart':
		case 'hidden':
		case 'jurisdiction':
            if(p_dictionary_path == p_find_path)
            {
                result = p_metadata;
            }
            break;

		default:
			console.log("page_render not processed", p_metadata);
			break;
    }
    
    return result;

}

function g_apply_sort_compareStringAsc(a, b) 
{
    return a.localeCompare(b);
}

function g_apply_sort_compareStringDesc(a, b) 
{
    return a.localeCompare(b) * -1;
}

function g_apply_sort_compareNumberAsc(x, y) 
{
    let a = new Number(x);
    let b = new Number(y);
    return a - b;
}


function g_apply_sort_compareNumberDesc(x, y) 
{
    let a = new Number(x);
    let b = new Number(y);
    return (a - b) *-1;
}

function g_apply_sort_compareDateAsc(x, y) 
{
    let a = new Date(x);
    let b = new Date(y);
    return a - b;
}


function g_apply_sort_compareDateDesc(x, y) 
{
    let a = new Date(x);
    let b = new Date(y);
    return (a - b) *-1;
}

function g_apply_sort_compareTimeAsc(x, y) 
{
    if(x == null) return -1;
    if(y == null) return 1;

    const A = x.split(":");
    const B = y.split(":");

    let a = parseInt(A[0].padStart(2, '0') + A[1].padStart(2, '0'));
    let b = parseInt(B[0].padStart(2, '0') + B[1].padStart(2, '0'));
    return a - b;
}


function g_apply_sort_compareTimeDesc(x, y) 
{
    if(x == null) return -1;
    if(y == null) return 1;

    const A = x.split(":");
    const B = y.split(":");

    let a = parseInt(A[0].padStart(2, '0') + A[1].padStart(2, '0'));
    let b = parseInt(B[0].padStart(2, '0') + B[1].padStart(2, '0'));
    return (a - b) *-1;
}

function g_apply_sort_create_sort(p_metadata, p_dictionary_path)
{
    let result = null;
    if
    (
        p_metadata.sort_order != null &&
        p_metadata.sort_order.length !=0
    )
    {
        let metadata_path_list = [];
        let metadata_node_list = [];
        let sort_direction_list = [];
        let property_list = [];
        let sort_item_list = p_metadata.sort_order.trim().split(",");
        for(let i = 0; i < sort_item_list.length; i++)
        {
            let item_array = sort_item_list[i].trim().split(' ');
            metadata_path_list.push(`${p_dictionary_path}/${item_array[0].trim()}`);
            property_list.push(`.${item_array[0].trim().replace(/\//g,".")}`);
            
            let node = g_find_metadata_node_by_path(p_metadata, p_dictionary_path, metadata_path_list[i]);
            if(node == null)
            {
                break;
            }
            metadata_node_list.push(node);
            if(item_array.length > 1)
            {
                let sort = item_array[1].toLowerCase().trim();
                switch(sort)
                {
                    case "display":
                    case "asc":
                    case "desc":
                        sort_direction_list.push(sort);
                        break;
                    default:
                        sort_direction_list.push("asc");
                        break;
                }
                
            }
            else
            {
                sort_direction_list.push("asc");
            }
        }


        let function_body = [];
        for(let i = 0; i < metadata_path_list.length; i++)
        {
            let data_type = "string";
            if(metadata_node_list[i].type== "list")
            {
                data_type = metadata_node_list[i].data_type;
            }
            else
            {
                data_type = metadata_node_list[i].type;
            }

            let property_name = property_list[i];


            if(i== 0)
            {
                switch(sort_direction_list[i].toLocaleLowerCase())
                {
                    case "asc":
                        switch(data_type.toLocaleLowerCase())
                        {
                            case "number":
                                function_body.push(`let result = g_apply_sort_compareNumberAsc(x${property_name}, y${property_name});`);
                            break;
                            case "date":
                            case "datetime":
                                function_body.push(`let result = g_apply_sort_compareDateAsc(x${property_name}, y${property_name});`);
                            break;
                            case "time":
                                function_body.push(`let result = g_apply_sort_comparTimeAsc(x${property_name}, y${property_name});`);
                            break;
                            case "string":
                            default:
                                function_body.push(`let result = g_apply_sort_compareStringAsc(x${property_name},y${property_name});`);
                                break;
                        }
                        
                    break;
                    case "desc":
                        switch(data_type.toLocaleLowerCase())
                        {
                            case "number":
                                function_body.push(`let result = g_apply_sort_compareNumberDesc(x${property_name}, y${property_name});`);
                            break;
                            case "date":
                            case "datetime":
                                function_body.push(`let result = g_apply_sort_compareDateDesc(x${property_name}, y${property_name});`);
                            break;
                            case "time":
                                function_body.push(`let result = g_apply_sort_compareTimeDesc(x${property_name}, y${property_name});`);
                            break;
                            case "string":
                            default:
                                function_body.push(`let result = g_apply_sort_compareStringDesc(x${property_name},y${property_name});`);
                                break;
                        }
                    break;
                    case "display":
                        function_body.push(`let result = g_apply_sort_compareNumberAsc(g_value_to_index_number_lookup['${metadata_path_list[i]}'][x${property_name}], g_value_to_index_number_lookup['${metadata_path_list[i]}'][y${property_name}]);`);
                    break;
                }
            }
            else
            {
                
                switch(sort_direction_list[i].toLocaleLowerCase())
                {
                    case "asc":
                        switch(data_type.toLocaleLowerCase())
                        {
                            case "number":
                                function_body.push(`if(result==0) { result = g_apply_sort_compareNumberAsc(x${property_name}, y${property_name}); }`);
                            break;
                            case "date":
                            case "datetime":
                                function_body.push(`if(result==0) { result = g_apply_sort_compareDateAsc(x${property_name}, y${property_name}); }`);
                            break;
                            case "time":
                                function_body.push(`if(result==0) { result = g_apply_sort_compareTimeAsc(x${property_name}, y${property_name}); }`);
                            break;
                            case "string":
                            default:
                                function_body.push(`if(result==0) { result = g_apply_sort_compareStringAsc(x${property_name},y${property_name}); }`);
                                break;
                        }
                        
                    break;
                    case "desc":
                        switch(data_type.toLocaleLowerCase())
                        {
                            case "number":
                                function_body.push(`if(result==0) { result = g_apply_sort_compareNumberDesc(x${property_name}, y${property_name}); }`);
                            break;
                            case "date":
                            case "datetime":
                                function_body.push(`if(result==0) { result = g_apply_sort_compareDateDesc(x${property_name}, y${property_name}); }`);
                            break;
                            case "time":
                                function_body.push(`if(result==0) { result = g_apply_sort_compareTimeDesc(x${property_name}, y${property_name}); }`);
                            break;
                            case "string":
                            default:
                                function_body.push(`if(result==0) { result = g_apply_sort_compareStringDesc(x${property_name},y${property_name}); }`);
                                break;
                        }
                    break;
                    case "display":
                        function_body.push(`if(result==0) { result = g_apply_sort_compareNumberAsc(g_value_to_index_number_lookup['${metadata_path_list[i]}'][x${property_name}], g_value_to_index_number_lookup['${metadata_path_list[i]}'][y${property_name}]); }`);
                    break;
                }
            }
        }
            
        function_body.push(" return result; ");
        result = new Function('x', 'y', function_body.join("\n"));

        //console.log(result);
    }
    
    return result;
}