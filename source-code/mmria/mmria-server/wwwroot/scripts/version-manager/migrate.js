var g_release_version = null;
var g_version_specification = null;
var g_metadata = null;
var g_data = null;
var g_data_view_rows = [];
var g_save_data_rows = [];
var g_unchanged_data_rows = [];

var g_value_to_name_lookup = {};
var g_name_to_value_lookup = {};


var g_missed_convert_output = [];
var g_passed_convert_output = [];

var mmria_path_to_definition_name = null;

var base_api_url = location.protocol + '//' + location.host + "/api/version?path=";
var g_available_version_list = null;


var g_MMRIA_Calculations = null;
var g_validation = null;
var g_ui_specification = null;


var case_view_request = {
    total_rows: 0,
    page :1,
    skip : 0,
    take : 10000,
    sort : "by_date_last_updated",
    search_key : null,
    descending : false,
    get_query_string : function(){
      var result = [];
      result.push("?skip=" + (this.page - 1) * this.take);
      result.push("take=" + this.take);
      result.push("sort=" + this.sort);
  
      if(this.search_key)
      {
        result.push("search_key=\"" + this.search_key.replace(/"/g, '\\"').replace(/\n/g,"\\n") + "\"");
      }
  
      result.push("descending=" + this.descending);
  
      return result.join("&");
    }
  };



function main()
{
    
    get_release_version();
}



function get_release_version()
{
  $.ajax
  ({

      url: location.protocol + '//' + location.host + '/api/version/release-version',
  })
  .done(function(response) 
  {
      g_release_version = response;
      document.getElementById("current_release").innerHTML = g_release_version;
      
      get_metadata();
	});
}

function get_metadata()
{
    $.ajax
    (
        {
            url: location.protocol + '//' + location.host + `/api/version/${g_release_version}/metadata`
        }
    )
    .done
    (
        function(response) 
        {
            g_metadata = response;


            set_list_lookup(g_value_to_name_lookup, g_name_to_value_lookup, g_metadata, "");

            get_case_set();
        }
    );
}


function get_case_set()
{

    var case_view_url = location.protocol + '//' + location.host + '/api/case_view' + case_view_request.get_query_string();

    $.ajax
    (
        {
            url: case_view_url,
        }
    )
    .done
    (
        function(case_view_response) 
        {
            case_view_request.total_rows = case_view_response.total_rows;

            g_data_view_rows = case_view_response.rows;

            document.getElementById("output").innerHTML = "case_view_request.total_rows: " + g_data_view_rows.length;
        }
    );
}

function set_list_lookup(p_list_lookup, p_name_to_value_lookup, p_metadata, p_path)
{

    switch(p_metadata.type.toLowerCase())
    {
        case "app":
        case "form":
        case "group":
        case "grid":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                set_list_lookup(p_list_lookup, p_name_to_value_lookup, child, p_path + "/" + child.name);

            }

            break;
        default:
            if(p_metadata.type.toLowerCase() == "list")
            {
                let data_value_list = p_metadata.values;

                if(p_metadata.path_reference && p_metadata.path_reference != "")
                {
                    data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
            
                    if(data_value_list == null)	
                    {
                        data_value_list = p_metadata.values;
                    }
                }
    
                p_list_lookup[p_path] = {};
                p_name_to_value_lookup[p_path] = {};
                for(let i = 0; i < data_value_list.length; i++)
                {
                    let item = data_value_list[i];
                    p_list_lookup[p_path][item.display.toLowerCase()] = item.value;
                    p_name_to_value_lookup[p_path][item.value] = item.display.toLowerCase();
                }
            }
            break;

    }
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

function traverse_object(p_data, p_metadata, p_path, p_call_back)
{
    let is_changed = false;


    switch(p_metadata.type.toLowerCase())
    {
        case "form":
            if
            (
                p_metadata.cardinality == "+" ||
                p_metadata.cardinality == "*"
            )
            {
                if(Array.isArray(p_data))
                {
                    for(let j = 0; j < p_data.length; j++)
                    {
                        for(let i = 0; i < p_metadata.children.length; i++)
                        {
                            let child = p_metadata.children[i];
                            //let data_child = p_data[j][child.name];
                            if(p_data[j][child.name])
                            {
                                if(child.children != null)
                                {
                                    is_changed = is_changed || traverse_object(p_data[j][child.name], child, p_path + "/" + child.name);
                                }
                                else if
                                (
                                    child.type.toLowerCase() == "list" && 
                                    (
                                        child.control_style == null || 
                                        child.control_style.toLowerCase().indexOf("editable") == -1
                                    )
                                )
                                {
                                    let temp_value = get_value(p_path + "/" + child.name, p_data[j][child.name]);
                                    if(temp_value != p_data[j][child.name])
                                    {
                                        p_data[j][child.name] = temp_value;
                                        is_changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let child = p_metadata.children[i];
                    //let data_child = p_data[child.name];
                    if(p_data[child.name])
                    {
                        if(child.children != null)
                        {
                            traverse_object(p_data[child.name], child, p_path + "/" + child.name);
                        }
                        else if
                        (
                            child.type.toLowerCase() == "list" && 
                            (
                                child.control_style == null || 
                                child.control_style.toLowerCase().indexOf("editable") == -1
                            )                           
                        )
                        {
                            let temp_value = get_value(p_path + "/" + child.name, p_data[child.name]);
                            if(temp_value != p_data[child.name])
                            {
                                p_data[child.name] = temp_value;
                                is_changed = true;
                            }
                            
                        }
                    }
                }
            }
            break;
        case "app":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                //let data_child = p_data[child.name];
                if(p_data[child.name])
                {
                    if(child.children != null)
                    {
                        is_changed = is_changed || traverse_object(p_data[child.name], child, p_path + "/" + child.name);
                    }
                    else if
                    (
                        child.type.toLowerCase() == "list" && 
                        (
                            child.control_style == null || 
                            child.control_style.toLowerCase().indexOf("editable") == -1
                        )
                    )
                    {
                        let temp_value = get_value(p_path + "/" + child.name, p_data[child.name]);
                        if(temp_value != p_data[child.name])
                        {
                            p_data[child.name] = temp_value;
                            is_changed = true;
                        }
                        
                    }
                }
            }

            if(p_call_back)
            {
                p_call_back(is_changed, p_data);
            }

            break;
        case "group":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                //let data_child = p_data[child.name];
                if(p_data[child.name])
                {
                    if(child.children != null)
                    {
                        is_changed = is_changed || traverse_object(p_data[child.name], child, p_path + "/" + child.name);
                    }
                    else if
                    (
                        child.type.toLowerCase() == "list" && 
                        (
                            child.control_style == null || 
                            child.control_style.toLowerCase().indexOf("editable") == -1
                        )
                    )
                    {
                        let temp_value = get_value(p_path + "/" + child.name, p_data[child.name]);
                        if(temp_value != p_data[child.name])
                        {
                            p_data[child.name] = temp_value;
                            is_changed = true;
                        }
                        
                    }
                }
            }
            break;
        case "grid":
            for(let j = 0; j < p_data.length; j++)
            {
                let grid_item = p_data[j];

                for(let i = 0; i < p_metadata.children.length; i++)
                {

                    let child = p_metadata.children[i];
                    //let data_child = p_data[child.name];
                    if(grid_item[child.name])
                    {
                        if(child.children != null)
                        {
                            is_changed = is_changed || traverse_object(grid_item[child.name], child, p_path + "/" + child.name);
                        }
                        else if
                        (
                            child.type.toLowerCase() == "list" && 
                            (
                                child.control_style == null || 
                                child.control_style.toLowerCase().indexOf("editable") == -1
                            )
                            
                        )
                        {
                            let temp_value = get_value(p_path + "/" + child.name, grid_item[child.name]);
                            if(temp_value != grid_item[child.name])
                            {
                                grid_item[child.name] = temp_value;
                                is_changed = true;
                            }
                            
                        }
                    }
                }
            }
            break;
        default:
            if
            (
                p_metadata.type.toLowerCase() == "list" && 
                (
                    p_metadata.control_style == null || 
                    p_metadata.control_style.toLowerCase().indexOf("editable") == -1
                )
            )
            {
                let data_value_list = g_value_to_name_lookup[p_path];

                if(Array.isArray(p_data))
                {
                    for(let i = 0; i < p_data.length; i++)
                    {
                        let item = p_data[i];

                        if(item == null || item =="")
                        {
                            let temp_value = data_value_list["(blank)"];
                            if(temp_value != p_data[i])
                            {
                                p_data[i] = temp_value;
                                is_changed = true;
                            }
                            
                        }
                        else if(data_value_list[item])
                        {
                            let temp_value = data_value_list[item];
                            if(temp_value != p_data[i])
                            {
                                p_data[i] = temp_value;
                                is_changed = true;
                            }
                            
                        }                   
                        else if(p_data[i] == "No, not Spanish/ Hispanic/ Latino")
                        {
                            p_data[i] = "0"
                            is_changed = true;
                        }
                        else if
                        (
                            p_data[i].length > 3 && 
                            (
                                p_data[i].substr(2,1) == "-" ||
                                p_data[i].substr(1,1) == "-"
                            )
                        )
                        {
                            let val = p_data[i].split("-")[1].trim();
                            if(data_value_list[val])
                            {
                                let temp_value = data_value_list[val];
                                if(temp_value != p_data[i])
                                {
                                    p_data[i] = temp_value;
                                    is_changed = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if(p_data == null || p_data =="")
                    {
                        let temp_value = data_value_list["(blank)"];
                        if(temp_value != p_data)
                        {
                            p_data = temp_value;
                            is_changed = true;
                        }
                    }
                    else if(data_value_list[p_data])
                    {
                        let temp_value = data_value_list[p_data];
                        if(temp_value != p_data)
                        {
                            p_data = temp_value;
                            is_changed = true;
                        }
                        
                    }
                    else if(p_data == "No, not Spanish/ Hispanic/ Latino")
                    {
                        p_data = "0";
                        is_changed = true;
                    }
                    else if
                    (
                        p_data.length > 3 && 
                        (
                            p_data.substr(2,1) == "-" ||
                            p_data.substr(1,1) == "-"
                        )
                        
                    )
                    {
                        let val = p_data.split("-")[1].trim();
                        if(data_value_list[val])
                        {
                            let temp_value = data_value_list[val];
                            if(temp_value != p_data)
                            {
                                p_data = temp_value;
                                is_changed = true;
                            }
                            
                        }
                    }
                }
            }
            break;

    }

    return is_changed;
}


function get_value(p_path, p_data)
{
    let data_value_list = g_value_to_name_lookup[p_path];
    let result = p_data;

    let is_number_regex = /^\-?\d+\.?\d*$/;

    /*
    if(p_path == "/autopsy_report/causes_of_death/type")
    {
        console.log("break");
    }*/

    try
    {

        if(p_path == "/informant_interviews/race")
        {
            if(Array.isArray(p_data))
            {
                result = 9999;

                for(let i = 0; i < p_data.length; i++)
                {
                    let val = p_data[i].toLowerCase();

                    if
                    (
                        val != null && 
                        val != ""
                     )
                     {
                         if(data_value_list[val])
                        {
                            //p_data[i] = data_value_list[val];
                            result = data_value_list[val];
                        }
                        else if
                        (   
                            g_name_to_value_lookup[p_path][p_data[i]] == null
                        )
                        {
                            g_missed_convert_output.push(`${p_path} - ${p_data[i]}`);
                        }
                    }
                }
            }
            else if(data_value_list[p_data.toLowerCase()])
            {
                result = data_value_list[p_data.toLowerCase()];
            }
            else if
            (   
                g_name_to_value_lookup[p_path][p_data.toLowerCase()] == null
            )
            {
                g_missed_convert_output.push(`${p_path} - ${p_data}`);
            }
        }
        else if(Array.isArray(p_data))
        {
            for(let i = 0; i < p_data.length; i++)
            {
                let item = p_data[i];

                if(item == null || item =="")
                {
                    result[i] = data_value_list["(blank)"];
                }
                else if(is_number_regex.test(data_value_list[item]) && data_value_list[item])
                {
                    result[i] = data_value_list[item];
                }
                else if (typeof item === "boolean")
                {
                    g_missed_convert_output.push(`${p_path} - ${item}`);
                }
                else if(!is_number_regex.test(data_value_list[item]) && data_value_list[item.toLowerCase()])
                {
                    result[i] = data_value_list[item.toLowerCase()];
                }                   
                else if(p_data[i] == "No, not Spanish/ Hispanic/ Latino")
                {
                    result[i] = "0";
                }
                else if(p_data[i] == "Yes, Other Spanish/ Hispanic/ Latino")
                {
                    result[i] = "4";
                }
                else if
                (
                    p_data[i].length > 3 && 
                    (
                        p_data[i].substr(2,1) == "-" ||
                        p_data[i].substr(1,1) == "-"
                    )
                )
                {
                    let val = p_data[i].split("-")[1].trim().toLowerCase();
                    if(data_value_list[val])
                    {
                        result[i] = data_value_list[val];
                    }
                }
                else if(p_data[i] == "-9")
                {
                    result[i] = "9999";
                }
                else if(p_data[i] == "-8")
                {
                    result[i] = "8888";
                }
                else if(p_data[i] == "-7")
                {
                    result[i] = "7777";
                }
                else if
                (   
                    g_name_to_value_lookup[p_path][p_data[i]] == null
                )
                {
                    g_missed_convert_output.push(`${p_path} - ${p_data[i]}`);
                }
            }
        }
        else
        {
            if(p_data == null)
            {
                result = data_value_list["(blank)"];
            }
            else if(p_data =="")
            {
                result = data_value_list["(blank)"];
            }
            else if(is_number_regex.test(p_data) && data_value_list[p_data])
            {
                result = data_value_list[p_data];
            }
            else if(typeof p_data === "boolean")
            {
                if
                (
                    p_path == "/social_and_environmental_profile/health_care_system/no_prenatal_care"
                )
                {
                    if(p_data)
                    {
                        result = 0;
                    }
                    else
                    {
                        result = 1;
                    }
                    
                }
                else if(p_data && data_value_list["yes"])
                {
                    result = data_value_list["yes"];
                }
                else if(data_value_list["no"])
                {
                    result = data_value_list["no"];
                }
                else
                {
                    g_missed_convert_output.push(`${p_path} - ${p_data}`);
                }
            }
            else if(!is_number_regex.test(p_data) && data_value_list[p_data.toLowerCase()])
            {
                result = data_value_list[p_data.toLowerCase()];
            }
            else if(p_data == "No, not Spanish/ Hispanic/ Latino")
            {
                result = "0"
            }
            else if(p_data == "Yes, Other Spanish/ Hispanic/ Latino")
            {
                result = "4"
            }
            else if
            (
                p_data.length > 3 && 
                (
                    p_data.substr(2,1) == "-" ||
                    p_data.substr(1,1) == "-"
                )
                
            )
            {
                let val = p_data.split("-")[1].trim().toLowerCase();
                if(data_value_list[val])
                {
                    result = data_value_list[val];
                }
            }
            else if(p_data == "-9")
            {
                result = "9999";
            }
            else if(p_data == "-8")
            {
                result = "8888";
            }
            else if(p_data == "-7")
            {
                result = "7777";
            }
            else if(p_path == "/birth_certificate_infant_fetal_section/is_multiple_gestation" && p_data == "Multiple gestation")
            {
                result = 1;
            }
            else if(p_path == "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/deceased_at_discharge" && p_data == "Deceased at the time of discharge?")
            {
                result = 1;
            }
            else if(p_path == "/committee_review/critical_factors_worksheet/prevention")
            {
                if(p_data.toLowerCase() == "prevent incidence")
                {
                    result = 1;
                }
                else if(p_data.toLowerCase() == "prevent progression")
                {
                    result = 2;
                }
                else if(p_data.toLowerCase() == "prevent complications")
                {
                    result = 3;
                }
                else if
                (   
                    g_name_to_value_lookup[p_path][p_data] == null
                )
                {
                    g_missed_convert_output.push(`${p_path} - ${p_data}`);
                }
            }
            else if(p_path == "/birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin" && p_data == "Yes, other Spanish/ Hispanic/ Latino")
            {
                result = 4;
            }/*
            else if
            (   
                g_name_to_value_lookup[p_path][p_data] == null 
            )
            {
                g_missed_convert_output.push(`${p_path} - ${p_data}`);
            }*/
            else
            {
                switch(p_path.toLowerCase())
                {
                    case "/birth_fetal_death_certificate_parent/demographic_of_mother/country_of_birth":
                    case "/social_and_environmental_profile/socio_economic_characteristics/country_of_birth":
                        if(p_data.trim().toLowerCase() == "(see geography)")
                        {
                            result = "9999";
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/committee_review/critical_factors_worksheet/class":
                        if(p_data.trim().toLowerCase() == "enforcement")
                        {
                            result = 20;
                        }
                        else if(p_data.trim().toLowerCase() == "access/finacial")
                        {
                            result = 11;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/committee_review/pmss_mm":
                        if(p_data.trim().toLowerCase().indexOf("91 pulmonary conditions") > -1)
                        {
                            result = 91;
                        }
                        else if(p_data.trim().toLowerCase().indexOf("83 collagen vascular") > -1)
                        {
                            result = 83;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/death_certificate/injury_associated_information/date_of_injury/year":
                        if(p_data == "203")
                        {
                            result = 9999;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/year":
                        if(p_data == "14")
                        {
                            result = 9999;
                        }
                        else if(p_data == "9")
                        {
                            result = 9999;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/informant_interviews/race":
                        if(p_data.trim().toLowerCase() == "white")
                        {
                            result = 0;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/other_medical_office_visits/physical_exam/body_system":
                        if(p_data.trim().toLowerCase() == "endoncrine")
                        {
                            result = 7;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/prenatal/current_pregnancy/attended_prenatal_visits_alone":
                        if(p_data.trim().toLowerCase() == "unknown")
                        {
                            result = 8888;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/prenatal/primary_prenatal_care_facility/principal_source_of_payment":
                        if(p_data.trim().toLowerCase() == "private")
                        {
                            result = 0;
                        }
                        else if(p_data.trim().toLowerCase() == "public")
                        {
                            result = 1;
                        }
                        else if(p_data.trim().toLowerCase() == "self")
                        {
                            result = 2;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/social_and_environmental_profile/health_care_system/no_prenatal_care":
                        if
                        (
                            typeof p_data === "boolean" &&
                            p_data
                        )
                        {
                            result = 0;
                        }
                        else
                        {
                            g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        }
                        break;
                    case "/committee_review/pregnancy_relatedness":
                        if(p_data.trim().toLowerCase() == "pregnancy-associated but not related")
                        {
                            result = 0;
                        }
                        else if(p_data.trim().toLowerCase() == "Not Pregnancy Related or Associated (i.e. False Positive)".toLowerCase())
                        {
                            result = 99;
                        }
                        
                        break;
                    default:
                        g_passed_convert_output.push(`val paassed ${p_path} - ${p_data}`);
                        break;
                }
            }
            
        }
    }
    catch(ex)
    {
        console.log(ex);
        g_missed_convert_output.push(`${p_path} - ${p_data}`);
    }

    return result;
}


function set_value(p_data, p_metadata, p_path, p_target_path, p_target_data)
{
    let is_changed = false;


    switch(p_metadata.type.toLowerCase())
    {
        case "form":
            if
            (
                p_metadata.cardinality == "+" ||
                p_metadata.cardinality == "*"
            )
            {
                if(Array.isArray(p_data))
                {
                    for(let j = 0; j < p_data.length; j++)
                    {
                        for(let i = 0; i < p_metadata.children.length; i++)
                        {
                            let child = p_metadata.children[i];
                            //let data_child = p_data[j][child.name];
                            if(p_data[j][child.name])
                            {
                                if(child.children != null)
                                {
                                    is_changed = is_changed || set_value(p_data[j][child.name], child, p_path + "/" + child.name, p_target_path, p_target_data);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let child = p_metadata.children[i];
                    //let data_child = p_data[child.name];
                    if(p_data[child.name])
                    {
                        if(child.children != null)
                        {
                            set_value(p_data[child.name], child, p_path + "/" + child.name, p_target_path, p_target_data);
                        }
                    }
                }
            }
            break;
        case "app":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                //let data_child = p_data[child.name];
                if(p_data[child.name])
                {
                    if(child.children != null)
                    {
                        is_changed = is_changed || set_value(p_data[child.name], child, p_path + "/" + child.name, p_target_path, p_target_data);
                    }
                }
            }
            break;
        case "group":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                //let data_child = p_data[child.name];
                if(p_data[child.name])
                {
                    if(child.children != null)
                    {
                        is_changed = is_changed || set_value(p_data[child.name], child, p_path + "/" + child.name, p_target_path, p_target_data);
                    }
                }
            }
            break;
        case "grid":
            for(let j = 0; j < p_data.length; j++)
            {
                let grid_item = p_data[j];

                for(let i = 0; i < p_metadata.children.length; i++)
                {

                    let child = p_metadata.children[i];
                    //let data_child = p_data[child.name];
                    if(grid_item[child.name])
                    {
                        if(child.children != null)
                        {
                            is_changed = is_changed || set_value(grid_item[child.name], child, p_path + "/" + child.name, p_target_path, p_target_data);
                        }
                    }
                }
            }
            break;
        default:
            if(p_path.toLowerCase() == p_target_path.toLowerCase())
            {
                p_data = p_target_data;
                is_changed = true;
            }
            break;

    }
}


/*
"autopsy_report/toxicology/substance"
"prenatal/substance_use_grid/substance"
"medical_transport/origin_information/place_of_origin"
"social_and_environmental_profile/if_yes_specify_substances/substance"
*/

function migrate_cases_click()
{
    let el = document.getElementById("output");
    g_missed_convert_output = [];

    for(let i = 0; i < g_data_view_rows.length; i++)
    {
        let current_case = g_data_view_rows[i];

        get_specific_case
        (
            current_case.id, 
            traverse_case,
            ()=> { el.innerHTML = `Migration Processed:${(i+1)} cases
            g_save_data_rows = ${g_save_data_rows.length}
            g_unchanged_data_rows ${g_unchanged_data_rows.length}`; }
        );
        
    }


}

function view_conversion_misses_click()
{

    let unique_array = Array.from(new Set(g_missed_convert_output));
    let unique_passed_array = Array.from(new Set(g_passed_convert_output));
    
    document.getElementById("output_2").innerHTML = unique_array.sort().join("\n");
    //document.getElementById("output_2").innerHTML = "missed values\n\n" + unique_array.sort().join("\n") + "\n\n\npassed values:\n" + unique_passed_array.sort().join("\n");

}


function save_changes_click()
{
    let el = document.getElementById("output");
    let i = 0;
    let current_case = g_save_data_rows.shift();
    while(current_case != null)
    {
        i++;
        let message = "Save Processed:" + i + " cases";

        current_case.version = g_release_version;
        save_case
        (
            current_case, ()=> { el.innerHTML =  message}
        );
        
        current_case = g_save_data_rows.shift();
    }


}

function tally_case(p_save_case, p_case) 
{ 
    if(p_save_case)
    {
        g_save_data_rows.push(p_case)
    }
    else
    {
        g_unchanged_data_rows.push(p_case)
    }
}



function traverse_case(p_case, p_call_back)
{
    let all_cases = document.getElementById("all_cases").checked;

    if(all_cases)
    { 
        let result = false;
        if(p_case.host_state == null || p_case.host_state == "")
        {
            p_case.host_state = window.location.host.split("-")[0];
            result = true;
        }
        result = result || traverse_object(p_case, g_metadata, "");
        tally_case(result, p_case);
    }
    else if(p_case.version == null || p_case.version != g_release_version)
    {
        let result = false;
        if(p_case.host_state == null || p_case.host_state == "")
        {
            p_case.host_state = window.location.host.split("-")[0];
            result = true;
        }

        result = result || traverse_object(p_case, g_metadata, "");
        tally_case(result, p_case);
    }
    
    if(p_call_back)
    {
      p_call_back();
    }

}

function get_specific_case(p_id, p_call_back, p_call_back2)
{

    var case_url = location.protocol + '//' + location.host + '/api/case?case_id=' + p_id;
    $.ajax
    (
        {
          url: case_url,
        }
    )
    .done
    (
        function(case_response) 
        {
            if(case_response)
            { 
                if(p_call_back)
                {
                  p_call_back(case_response, p_call_back2);
                }
            }
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


function save_case(p_data, p_call_back)
{


    $.ajax({
      url: location.protocol + '//' + location.host + '/api/case',
      contentType: 'application/json; charset=utf-8',
      dataType: 'json',
      data: JSON.stringify(p_data),
      type: "POST"
  }).done(function(case_response) {

      console.log("save_case: success");

      g_change_stack = [];

      if(p_data && p_data._id == case_response.id)
      {
        p_data._rev = case_response.rev;
        //console.log('set_value save finished');
      }



      if(p_call_back)
      {
        p_call_back();
      }


  })
  .fail
  (
    function(xhr, err) 
    { 
      console.log("server save_case: failed", err); 
      if(xhr.status == 401)
      {
        let redirect_url = location.protocol + '//' + location.host;
        window.location = redirect_url;
      }
      
    }
  );

}


function load_metadata_click()
{
    get_metadata();
}

