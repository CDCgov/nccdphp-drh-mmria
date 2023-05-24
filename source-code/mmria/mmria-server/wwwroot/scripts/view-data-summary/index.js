'use strict';

var g_metadata = null;
var g_metadata_set = {};
var g_release_version_name = null;
var g_release_version_specification = null;
var g_selected_version_specification = null;
var g_selected_version_name = null;
var g_version_list = null;


(async function() {

	await get_release_version();
})()


async function get_release_version()
{
	let response = await $.ajax({
		url: `${location.protocol}//${location.host}/api/version/release-version`
	});
    
    
    g_release_version_name = response;
    g_selected_version_name = g_release_version_name;
	
	response = await $.ajax({
		url: `${location.protocol}//${location.host}/api/metadata/version_specification-${g_release_version_name}`
	});

    g_metadata = JSON.parse(response.metadata);
    g_release_version_specification = response;

    document.getElementById('form_content_id').innerHTML = dictionary_render(g_metadata, "").join("")  + '<br/>';

	$('.spinner-content').removeClass('spinner-active');
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

async function get_indicator_values(p_indicator_id)
{
    const get_data_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/measure-indicator/${p_indicator_id}`
    });

    g_data = { total: 0, data: []};

    for(let i = 0; i < get_data_response.length; i++)
    {
        const item = get_data_response[i];
        if(Can_Pass_Filter(item))
        {
            g_data.data.push(item);
            g_data.total +=1;
        }
        else
        {
            //console.log("here");
        }
    }
    
    return g_data;
}
