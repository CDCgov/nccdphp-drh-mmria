
const view_model = {

    search_text: "",
    search_results: [],
    selected_id: "",
    selected_jurisdiction: "",
    selected_revision_result: null,
    selected_revision_result_index: -1,
    audit_change_set_list: null
};

var g_release_version = null;
var g_metadata = null;

var g_data = null;

var g_look_up = {};
var g_value_to_display_lookup = {};
var g_name_to_value_lookup = {};
var g_display_to_value_lookup = {};
var g_value_to_index_number_lookup = {};
var g_name_to_value_lookup = {};
var g_is_confirm_for_case_lock = false;
var g_target_case_status = null;
var g_previous_case_status = null;
var g_other_specify_lookup = {};
var g_record_id_list = {};

window.onload = main;


async function main()
{
    g_jurisdiction_list.sort();

    const release_version = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/release-version`,
    });
    
    
    g_release_version = release_version;
    
    const metadata_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/${g_release_version}/metadata`,
    });

    g_metadata = metadata_response;

    //build_other_specify_lookup(g_other_specify_lookup, g_metadata);

    set_list_lookup
    (
      g_display_to_value_lookup,
      g_value_to_display_lookup,
      g_value_to_index_number_lookup,
      g_metadata,
      ''
    );

    for (let i in g_metadata.lookup) 
    {
      const child = g_metadata.lookup[i];

      g_look_up['lookup/' + child.name] = child.values;
    }/**/



    await render();
}


async function render()
{
    const el = document.getElementById("output")
    

    if
    (
        view_model.selected_id != null &&
        view_model.selected_id != ""
    )
    {
        el.innerHTML = render_search_ui() +
        render_versions_for_selected_id();
    }
    else
    {
        el.innerHTML = render_search_ui();
    }
}

function render_search_ui()
{

    const jurisdiction_html_builder = [];
    jurisdiction_html_builder.push("<p>Select Jurisdition</p>\n<select id=\"jurisdiction-select\" onchange=\"juridiction_changed(this.value)\">");

    jurisdiction_html_builder.push(`<option value="" selected>Select a Jurisdition</option>`);
    for(const i in g_jurisdiction_list)
    {
        const item = g_jurisdiction_list[i];
        if(item == view_model.selected_jurisdiction)
        {
            jurisdiction_html_builder.push(`
            <option value="${item}" selected>${item}</option>
            `);
        }
        else
        {
            jurisdiction_html_builder.push(`
            <option value="${item}">${item}</option>
            `);
        }
    }
    jurisdiction_html_builder.push("</select>");

    let result = [];
    if
    (
        view_model.search_results != null &&
        view_model.search_results.rows != null &&
        view_model.search_results.rows.length != 0
    )
    {
        for(const row in view_model.search_results.rows)
        {
            const detail = view_model.search_results.rows[row].value;
            const id = view_model.search_results.rows[row].id;

            result.push(`
            <li>
                <input type="button" onclick="show_versions_for_id_click('${id}')" value="show versions" />
                ${detail.last_name}, ${detail.first_name} ${detail.record_id}
                created: ${detail.date_created} ${detail.created_by}<br/>
                last updated: ${detail.date_last_updated} ${detail.last_updated_by}
            </li>
            `);
        }
    }
    else if
    (
        view_model.search_text != null &&
        view_model.search_text.length != 0
    )
    {
        result.push(`<li>No record_id matches found for search text: ${view_model.search_text}`);
    }

    return `
    ${jurisdiction_html_builder.join("")}
    <p>Enter Record Id:
    <input id="search_text"
        value="${view_model.search_text}" />
    <input type="button" onclick="search_text_click()" value="Search" />
    
    <ul>
        ${result.join("")}
    </ul>

    `;
}

async function juridiction_changed(p_value)
{
    view_model.selected_jurisdiction = p_value;
}

async function search_text_click()
{
    const search_text = document.getElementById("search_text").value.trim();
    
    view_model.selected_id = "";
    view_model.search_results = [];
    view_model.selected_revision_result = null;
    view_model.selected_revision_result_index = -1;

    view_model.search_text = search_text;
    view_model.search_results = await get_case_view(search_text);

    //console.log(view_model.search_results);

    await render();
}


async function get_case_view(p_search_text)
{
    const result = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/caseRevisionList_case_view?jurisdiction_id=${view_model.selected_jurisdiction}&search_key=${encodeURIComponent(p_search_text)}`
    });


    
    return result;
}

async function get_case_revision_list(p_case_id)
{
    const result = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/caseRevisionList?jurisdiction_id=${view_model.selected_jurisdiction}&case_id=${p_case_id}`
    });

    return result;
}

async function get_case_revision(p_case_id, p_revision_id)
{
    const result = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/caseRevision?jurisdiction_id=${view_model.selected_jurisdiction}&case_id=${p_case_id}&revision_id=${p_revision_id}`
    });

    return result;
}


async function get_audit_change_set_list(p_case_id)
{
    try
    {
        const result = await $.ajax
        ({
            url: `${location.protocol}//${location.host}/api/AuditRecoverUtil?jurisdiction_id=${view_model.selected_jurisdiction}&case_id=${p_case_id}`
        });

        return result;
    }
    catch(e)
    {
        return null;
    }
}


async function show_versions_for_id_click(p_id)
{
    view_model.selected_id = p_id;

    view_model.selected_revision_result = await get_case_revision_list(p_id);

    view_model.audit_change_set_list = await get_audit_change_set_list(p_id);

    g_data = await get_case_revision(p_id, view_model.selected_revision_result._rev);

    render();
}


function render_versions_for_selected_id()
{
    const result = [];

    result.push(`<p>g_data
    
    [ 
        <a href="${location.protocol}//${location.host}/api/caseRevision?jurisdiction_id=${view_model.selected_jurisdiction}&case_id=${view_model.selected_id}&revision_id=${view_model.selected_revision_result._rev}" target="_blank">View</a> 
    |
        <a href="javascript:g_data_pdf_case_onclick('${view_model.selected_jurisdiction}','${view_model.selected_id}','${view_model.selected_revision_result._rev}')">View PDF</a> 
        
    ]
    </p>
    <textarea id="g_data_json" cols=120 rows=7 style="white-space: pre-wrap;">
    ${JSON.stringify(g_data, null, 2)}
    </textarea>
    
    `);

    result.push("<ol>");
    if
    (
        view_model.selected_revision_result != null &&
        view_model.selected_revision_result._revisions != null &&
        view_model.selected_revision_result._revisions.ids.length > 0
    )
    {
        const array = view_model.selected_revision_result._revisions.ids;

        let rev_start = view_model.selected_revision_result._revisions.start;
        for(var i in array)
        {
            //let rev_id = `${rev_start}-${array[i]}`;
            let next_rev_id = ``;
            const j = parseInt(i)+1;
            if(j < array.length - 1)
            {
                
                next_rev_id = `${rev_start -1}-${array[j]}`;
            }
            result.push(`
                <li>
                    ${array[i]} ${render_audit_for(next_rev_id)}
                    [ 
                        <a href="${location.protocol}//${location.host}/api/caseRevision?jurisdiction_id=${view_model.selected_jurisdiction}&case_id=${view_model.selected_id}&revision_id=${rev_start}-${array[i]}" target="_blank">View</a> 
                    |
                        <a href="javascript:pdf_case_onclick('${view_model.selected_jurisdiction}','${view_model.selected_id}','${rev_start}-${array[i]}')">View PDF</a> 
                        
                    ]
                </li>
            `);

            rev_start -= 1;
        }


    }
    result.push("</ol>");

    return result.join("");
}

function extract_form_index_from_object_path(p_dictionary_path, value)
{
    let result = null;
    //g_data.er_visit_and_hospital_medical_records[6].referrals_and_consultations[4].date"

    const start_index = value.indexOf("[");
    const end_index = value.indexOf("]");
    if
    (
        start_index > -1 && 
        end_index > -1 &&
        (
            multiform_set.has(p_dictionary_path) ||
            multiform_grid_set.has(p_dictionary_path)
        )
    )
        result =value.substr(start_index + 1, end_index - start_index - 1);

    return result;
}


const multiform_grid_set = new Set();
const multiform_set = new Set();

function render_audit_for(p_revision_id)
{
    const result = [];

    function is_rev(x) 
    {
        return x.case_rev == p_revision_id; 
    } 

    if(view_model.audit_change_set_list == null)
    {
        return "";
    }

    const results = view_model.audit_change_set_list.ls.filter(is_rev);
    if(results.length < 1)
    {
        return "";
    }
    else
    {

        for(const result_index in results)
        {
            const change = results[result_index];
            result.push(`
            
            <ul>
            <li>stack ${parseInt(result_index) + 1} of ${results.length}</li>
            <li>date_created: ${change.date_created} : ${new Date(change.date_created).toLocaleString()}</li>
            <li>user_name: ${change.user_name}</li>
            <li>number of changes: ${change.items.length} </li>
            <li>note: ${change.note}</li>
            `);

            for(const c in change.items)
            {
                const max_number_of_characters = 100;

                const ci = change.items[c];

                let new_value = ci.new_value;
                
                const value_lookup = g_value_to_display_lookup[ci.dictionary_path];
                if(value_lookup != null)
                {
                    new_value = `${new_value} : ${value_lookup[new_value]}`;
                }
                else if(new_value != null && new_value.length > max_number_of_characters)
                {
                    new_value = new_value.substring(0, max_number_of_characters).replace(/</g,"&lt;");
                }

                let form_index = '';
                let grid_index = '';



                if(ci.form_index == null || ci.form_index == '')
                {
                    const index_result = extract_form_index_from_object_path(ci.dictionary_path, ci.object_path);
                    if(index_result != null)
                    {
                        form_index =  'f: ' + index_result;
                        ci.form_index = index_result;
                    }
                }
                else 
                {
                    form_index = 'f: ' + ci.form_index;

                }

                if(ci.grid_index != null)
                {
                	grid_index = 'g: ' + ci.grid_index;
                }
                result.push(`<li>${form_index} ${grid_index} ${ci.dictionary_path} -> ${new_value} [ <a href="javascript:on_apply_change_click('${p_revision_id}',${result_index},${c},'${ci.dictionary_path}','${ci.metadata_path}','${ci.object_path}',${ci.form_index},${ci.grid_index})">apply change</a> ]</li>`);
            }

            

            result.push('</ul>');
        }
        
    }

    return result.join("");
}


function on_apply_change_click
(
    p_revision_id,
    p_result_index,
    p_change_index,
    p_dictionary_path,
    p_metadata_path,
    p_object_path,
    p_form_index,
    p_grid_index
)
{
    const result = [];

    function is_rev(x) 
    {
        return x.case_rev == p_revision_id; 
    } 

    function Get_Form_Grid_PathSet()
    {
        const array = p_dictionary_path.substring(1).split("/");
        const path = [];
        const result = {
            is_multiform : false,
            is_grid :false,
            form_path : "",
            grid_path : ""
        };


        for(let i = 0; i < array.length; i++)
        {
            const item = array[i];

            path.push(item);
            if
            (
                i == 0 
            )
            {
                result.form_path = "g_data." + item;
                if
                (
                    p_form_index == null ||
                    p_form_index == ''
                )
                {
                    const index_result = extract_form_index_from_object_path(p_dictionary_path, p_object_path);

                    if(index_result != null)
                    {
                        result.is_multiform = true;
                        p_form_index = index_result;
                    }
                }
                else 
                {
                    result.is_multiform = true;
                }
            }

            if
            (
                i == array.length -2 &&
                p_grid_index != null
            )
            {
                result.grid_path = "g_data." + path.join(".");
                result.is_grid = true;
                
            }
        }

        return result;


    }

    const Form_Grid_PathSet = Get_Form_Grid_PathSet();

    if(view_model.audit_change_set_list == null)
    {
        return "";
    }

    const results = view_model.audit_change_set_list.ls.filter(is_rev);
    if(results.length < 1)
    {
        return;;
    }
    else
    {



        if(Form_Grid_PathSet.is_multiform)
        {
            const metadata_array = p_metadata_path.split(".");
            const dictionary_array = p_dictionary_path.split("/");
            

            var form_metadata_path = `${metadata_array[0]}.${metadata_array[1]}`;
            var form_dictionary_path = `/${dictionary_array[1]}`;


            const form_array = eval(Form_Grid_PathSet.form_path);
            while(form_array.length - 1 < p_form_index)
            {
                add_form_item(form_metadata_path, Form_Grid_PathSet.form_path, form_dictionary_path) 
            }
        }

        if(Form_Grid_PathSet.is_grid)
        {
            var grid_metadata_path = p_metadata_path.substring(0, p_metadata_path.lastIndexOf("."));
            var grid_dictionary_path = p_dictionary_path.substring(0, p_dictionary_path.lastIndexOf("/"));

            let grid_path = Form_Grid_PathSet.grid_path
            if(Form_Grid_PathSet.is_multiform)
            {
                const start_index = p_object_path.lastIndexOf("[");
                const end_index = p_object_path.lastIndexOf("]");
                grid_path = p_object_path.substring(0, start_index);
            }
            const grid = eval(grid_path);
            while(grid.length - 1 < p_grid_index)
            {
                add_grid_item(grid_path, grid_metadata_path, p_dictionary_path) 
            }

        }

        const change = results[p_result_index];
        const ci = change.items[p_change_index];

        const location = eval(`${ci.object_path}`);
        if(location == null)
        {
            console.log("location is null");
            return;
        }

        if
        ( 
            ci.metadata_typ == "html_area" ||
            ci.object_path == "/case_narrative/case_opening_overview"
        )
        {
            eval(`${ci.object_path} = "${ci.new_value.replace(/"/g, '\\"').replace(/\n/g, '\\n')}"`);
        }
        else
        {
            eval(`${ci.object_path} = '${ci.new_value.replace(/"/g, '\\"').replace(/'/g, "\\'").replace(/\n/g, '\\n')}'`);
        }
        

        //console.log("here");


    }

    const el = document.getElementById("g_data_json");
    el.value = JSON.stringify(g_data, null, 2);

    //console.log("here");

}

async function pdf_case_onclick(jurisdiction_id, case_id, revision_id) 
{
    const dropdown = "view";
    const unique_tab_name = '_pdf_tab_' + Math.random().toString(36).substring(2, 9);
    const section_name = 'all';

    const g_data = await get_case_revision(case_id, revision_id);

    window.setTimeout(function()
    {
        openTab('./pdf-version',  unique_tab_name, section_name, g_metadata, g_data, true);
    }, 1000);	

}

async function g_data_pdf_case_onclick(jurisdiction_id, case_id, revision_id) 
{
    const dropdown = "view";
    const unique_tab_name = '_pdf_tab_' + Math.random().toString(36).substring(2, 9);
    const section_name = 'all';

    //const g_data = await get_case_revision(case_id, revision_id);

    window.setTimeout(function()
    {
        openTab('./pdf-version',  unique_tab_name, section_name, g_metadata, g_data, true);
    }, 1000);	

}

function openTab(pageRoute, tabName, p_section, p_metadata, p_data, p_show_hidden) 
{

    if (!window[tabName] || window[tabName].closed) 
    {
      window[tabName] = window.open(pageRoute, tabName, null, false);
      window[tabName].addEventListener('load', () => {
        window[tabName].create_print_version2(
            p_metadata,
            p_data,
            p_section,
            null,
            1,
            null,
            true
        );
      });
    } 
    else 
    {
       window[tabName] = window.open(pageRoute, tabName, null, false);
       window[tabName].addEventListener('load', () => {
        window[tabName].create_print_version2(
            p_metadata,
            p_data,
            p_section,
            null,
            1,
            null,
            true
        );
        });
    }
}

function set_list_lookup
(
    p_list_lookup, 
    p_name_to_value_lookup, 
    p_value_to_index_number_lookup, 
    p_metadata, 
    p_path,
    p_is_multiform = false,
    p_is_grid = false

)
{
    switch(p_metadata.type.toLowerCase())
    {
        case "app":
        case "group":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                set_list_lookup
                (
                    p_list_lookup, 
                    p_name_to_value_lookup, 
                    p_value_to_index_number_lookup, 
                    child, p_path + "/" + child.name,
                    p_is_multiform,
                    p_is_grid
                );
            }

            break;        
        case "form":
            let is_multiform = p_is_multiform;
            if
            (
                p_metadata.cardinality != null &&
                (
                    p_metadata.cardinality == "+" ||
                    p_metadata.cardinality == "*" 
                )
            )
            {
                is_multiform = true;
                multiform_set.add(p_path);
            }

            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                set_list_lookup
                (
                    p_list_lookup, 
                    p_name_to_value_lookup, 
                    p_value_to_index_number_lookup, 
                    child, p_path + "/" + child.name,
                    is_multiform,
                    p_is_grid
                );
            }

            break;
        case "grid":
            let is_grid = true;

            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                set_list_lookup
                (
                    p_list_lookup, 
                    p_name_to_value_lookup, 
                    p_value_to_index_number_lookup, 
                    child, p_path + "/" + child.name,
                    p_is_multiform,
                    is_grid
                );
            }

            break;

        default:
            if(p_is_multiform && p_is_grid)
            {
                multiform_grid_set.add(p_path);
            }

            if(p_is_multiform)
            {
                multiform_set.add(p_path);
            }
            
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
                p_value_to_index_number_lookup[p_path] = {};

                for(let i = 0; i < data_value_list.length; i++)
                {
                    let item = data_value_list[i];
                    p_list_lookup[p_path][item.display.toLowerCase()] = item.value;
                    p_name_to_value_lookup[p_path][item.value] = item.display.toLowerCase();
                    p_value_to_index_number_lookup[p_path][item.value] = i;
                }
            }
            break;
    }
}



function convert_dictionary_path_to_lookup_object(p_path)
{

	//g_data.prenatal.routine_monitoring.systolic_bp
	let result = null;
	let temp_result = []
	let temp = "g_metadata." + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");
	let index = temp.lastIndexOf('.');
	temp_result.push(temp.substr(0, index));
	temp_result.push(temp.substr(index + 1, temp.length - (index + 1)));

	let lookup_list = eval(temp_result[0]);

	for(let i = 0; i < lookup_list.length; i++)
	{
		if(lookup_list[i].name == temp_result[1])
		{
			result = lookup_list[i].values;
			break;
		}
	}


	return result;
}

function add_grid_item(p_object_path, p_metadata_path, p_dictionary_path) 
{
  
  let metadata = eval(p_metadata_path);
  let new_line_item = create_default_object(metadata, {}, true);
  let grid = eval(p_object_path);

  grid.push(new_line_item[metadata.name][0]);


}


function add_form_item(p_metadata_path, p_object_path, p_dictionary_path) 
{
  //console.log('add_new_form_click: ' + p_metadata_path + ' , ' + p_object_path);

  var metadata = eval(p_metadata_path);
  var form_array = eval(p_object_path);
  var new_form = create_default_object(metadata, {}, true);
  var item = new_form[metadata.name][0];

  form_array.push(item);


}