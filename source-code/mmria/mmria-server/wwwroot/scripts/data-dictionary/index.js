'use strict';

var g_metadata = null;
var g_metadata_set = {};
var g_release_version_name = null;
var g_release_version_specification = null;
var g_selected_version_specification = null;
var g_selected_version_name = null;
var g_version_list = null;

var g_filter = 	{
	date_of_death:
	{
		year: [
			'all'
		],
		month: [
			'all'
		],
		day: [
			'all'
		],
	},
	date_range: [
		{
			from: 'all',
			to:	'all'
		}
	],
	case_status: [
		'all'
	],
	case_jurisdiction: [
		'/all'
	],
	selected_form: "",
	search_text: ""
};


window.onload = main;

async function main()
{
	const version_name_response_promise = await fetch
    (		
        `${location.protocol}//${location.host}/api/version/release-version`
	);
    
    const version_name_response = await version_name_response_promise.text();
    
    g_release_version_name = version_name_response;
    g_selected_version_name = g_release_version_name;
	
	const g_metadata_response_promise = await fetch
    (
    	`${location.protocol}//${location.host}/api/metadata/version_specification-${g_release_version_name}`
	);

    const g_metadata_response = await g_metadata_response_promise.json();

    g_metadata = JSON.parse(g_metadata_response.metadata);
    g_release_version_specification = g_metadata_response;


    g_release_version_specification = g_metadata_response;
    g_selected_version_specification = g_release_version_specification;
	


    document.getElementById('form_content_id').innerHTML = dictionary_render(g_metadata, "").join("")  + '<br/>';

	$('.spinner-content').removeClass('spinner-active');

    window.setTimeout(get_version_list, 0);
}

async function load_metadata(p_version_id)
{
    let result;

    result = await $.ajax
    (
        {
            url: `${location.protocol}//${location.host}/api/version/${p_version_id}/metadata`,
        }
    );

    return result;
}


async function get_version_list()
{
    const response_promise = await fetch
    (
        `${location.protocol}//${location.host}/api/version/list`,
    );
    
    g_version_list = await response_promise.json();

    for(let i = 0; i < g_version_list.length; i++)
    {
        let item = g_version_list[i];

        if(item._id.indexOf("version_specification") == 0 && g_metadata_set[item._id] == null)
        {
            g_metadata_set[item._id] = await load_metadata(item.name);
        }
    }


    g_metadata = g_metadata_set[g_selected_version_specification._id];

    const el = document.getElementById("metadata_version_filter").innerHTML = render_metadata_version_filter();
}

async function metadata_version_filter_change(p_value)
{

    let idx = g_version_list.findIndex((x)=> {return x._id == p_value;});
    if(idx ==-1)
    {
        g_selected_version_name = g_release_version;
        g_selected_version_specification = g_release_version_name;
    }
    else
    {
        g_selected_version_name =  g_version_list[idx].name;
        g_selected_version_specification = g_version_list[idx];
    }

    g_metadata = g_metadata_set[g_selected_version_specification._id];

    document.getElementById('form_content_id').innerHTML = dictionary_render(g_metadata, "").join("")  + '<br/>';
}

function render_metadata_version_filter()
{
    let html_result = []

    html_result.push(`<option value="">(Select Version)</option>`)
    if(g_version_list != null)
    for(let i = 0; i < g_version_list.length; i++)
    {
        let item = g_version_list[i];
        let is_selected = "";

        if(item.name == g_selected_version_name)
        {
            is_selected = "selected=true"
        }
        
        if(item._id.indexOf("_design/auth") < 0 && item.name != null)
        {
          html_result.push(`<option value="${item._id}" ${is_selected}>${item.name}</option>`)
        }
    }


    return html_result.join("");

}

function search_text_change(p_value)
{
	g_filter.search_text = p_value;
}


