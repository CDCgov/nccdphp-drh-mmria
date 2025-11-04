var g_release_version = null;
var g_data = null;
var g_couchdb_url = null;
var g_metadata = null;

var g_reportType = "Summary";
var g_webport = -1;
var g_report_index = 0;


var g_list_lookup = {};

const g_filter = {
    reporting_state: sanitize_encodeHTML(window.location.host.split("-")[0]),
    pregnancy_relatedness: [
        1,
        0,
        2,
        99
    ],
    date_of_review: { begin: new Date(1900,00,01), end: new Date() },
    date_of_death: { begin: new Date(1900,00,01), end: new Date() }
}

const BAR_CHART_COLOR = '#712177';

const g_ui = { 
	user_summary_list:[],
	user_list:[],
	data:null,
	url_state: {
    selected_form_name: null,
    selected_id: null,
    selected_child_id: null,
    path_array : []

  }
};


const g_nav_map = new Map();
g_nav_map.set(0,"Overview");
g_nav_map.set(1,"Primary Underlying Causes of Death");
g_nav_map.set(2,"Pregnancy-Relatedness");
g_nav_map.set(3,"Preventability");
g_nav_map.set(4,"Timing of Death");
g_nav_map.set(5,"OMB Race Recode");
g_nav_map.set(6,"Race");
g_nav_map.set(7,"Race/Ethnicity");
g_nav_map.set(8,"Age");
g_nav_map.set(9,"Education");
g_nav_map.set(10,"Committee Determinations");
g_nav_map.set(11,"Emotional Stress");
g_nav_map.set(12,"Living Arrangements");

const relatedness_map = new Map();
relatedness_map.set(9999, "(blank)");
relatedness_map.set(1, "Pregnancy-Related");
relatedness_map.set(0, "Pregnancy-Associated, but NOT -Related");
relatedness_map.set(2, "Pregnancy-Associated but Unable to Determine Pregnancy-Relatedness");
relatedness_map.set(99, "Not Pregnancy-Related or -Associated (i.e. False Positive)");


function sanitize_encodeHTML(s) 
{
	let result = s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    return result;
}


$(async function ()
{
  'use strict';
	document.getElementById('report_output_id').innerHTML = "";
	await get_release_version();

    if (window.onhashchange) 
    {
      window.onhashchange({ isTrusted: true, newURL: window.location.href });
    } 
    else 
    {
      window.onhashchange = window_on_hash_change;
      window.onhashchange({ isTrusted: true, newURL: window.location.href });
    }
});


async function window_on_hash_change(e) 
{
    if (e.isTrusted) 
    {
        const url = e.newURL || window.location.href;

        let index = -1;

        const url_array = url.split('#');

        if(url_array.length > 1)
        {
            index = parseInt(url_array[1]);
        }

        await render();

        
    }

}

async function get_release_version()
{
    const get_release_version_response = await $.ajax
    ({

        url: location.protocol + '//' + location.host + '/api/version/release-version',
    });
      
    g_release_version = get_release_version_response;

    g_webport = window.location.host.split("-")[0].split(":")[1];

    const g_metadata_response = await $.ajax
    (
        {
            url: location.protocol + '//' + location.host + `/api/version/${g_release_version}/metadata`
        }
    );

    g_metadata = g_metadata_response;

    render();
}


function review_begin_date_change(p_value)
{
    const arr = p_value.split("-");

    let date_changed = arr[0] >= 1900 ? false :true;

    let test_date = new Date(arr[0] > 1900 ? arr[0] : 1900, arr[1] - 1, arr[2]);

    if(arr[0] < 1900)
    {
        test_date = new Date(1900 , 0, 1);
    }

    const current_date = new Date();


    if(test_date <= current_date && test_date <= g_filter.date_of_review.end )
    {
        g_filter.date_of_review.begin = test_date;
        const el = document.getElementById("review_end_date");
        el.setAttribute("min", p_value);

        if(date_changed)
        {
            el.setAttribute("min", ControlFormatDate(test_date));

            const el2 = document.getElementById("review_begin_date");
            el2.value = ControlFormatDate(g_filter.date_of_review.begin);
        }
    }
    else
    {
        const el = document.getElementById("review_begin_date");
        el.value = ControlFormatDate(g_filter.date_of_review.begin);
    }
}
function review_end_date_change(p_value)
{
    const arr = p_value.split("-");
    
    let date_changed = arr[0] >= 1900 ? false :true;

    let test_date = new Date(arr[0] > 1900 ? arr[0] : 1900, arr[1] - 1, arr[2]);

    if(arr[0] < 1900)
    {
        test_date = new Date(1900 , 0, 1);
    }

    const current_date = new Date();

    if(test_date <= current_date && g_filter.date_of_review.begin <= test_date)
    {
        g_filter.date_of_review.end = test_date;
        const el = document.getElementById("review_begin_date");
        el.setAttribute("max", p_value);

        if(date_changed)
        {
            el.setAttribute("max", ControlFormatDate(test_date));

            const el2 = document.getElementById("review_end_date");
            el2.value = ControlFormatDate(g_filter.date_of_review.end);
        }
    }
    else
    {
        const el = document.getElementById("review_end_date");
        el.value = ControlFormatDate(g_filter.date_of_review.end);
    }
}
function death_begin_date_change(p_value)
{
    const arr = p_value.split("-");
    
    let date_changed = arr[0] >= 1900 ? false :true;

    let test_date = new Date(arr[0] > 1900 ? arr[0] : 1900, arr[1] - 1, arr[2]);

    if(arr[0] < 1900)
    {
        test_date = new Date(1900 , 0, 1);
    }

    const current_date = new Date();

    if(test_date <= current_date && test_date <= g_filter.date_of_death.end)
    {
        g_filter.date_of_death.begin = test_date;
        const el = document.getElementById("death_end_date");
        el.setAttribute("min", p_value);

        if(date_changed)
        {
            el.setAttribute("min", ControlFormatDate(test_date));

            const el2 = document.getElementById("death_begin_date");
            el2.value = ControlFormatDate(g_filter.date_of_death.begin);
        }
    }
    else
    {
        const el = document.getElementById("death_begin_date");
        el.value = ControlFormatDate(g_filter.date_of_death.begin);
    }
}
function death_end_date_change(p_value)
{
    const arr = p_value.split("-");
    
    let date_changed = arr[0] >= 1900 ? false :true;

    let test_date = new Date(arr[0] > 1900 ? arr[0] : 1900, arr[1] - 1, arr[2]);

    if(arr[0] < 1900)
    {
        test_date = new Date(1900 , 0, 1);
    }

    const current_date = new Date();

    if(test_date <= current_date && g_filter.date_of_death.begin <=  test_date)
    {
        g_filter.date_of_death.end = test_date;
        const el = document.getElementById("death_begin_date");
        el.setAttribute("max", p_value);

        if(date_changed)
        {
            el.setAttribute("max", ControlFormatDate(test_date));

            const el2 = document.getElementById("death_end_date");
            el2.value = ControlFormatDate(g_filter.date_of_death.end);
        }
    }
    else
    {
        const el = document.getElementById("death_end_date");
        el.value = ControlFormatDate(g_filter.date_of_death.end);
    }
}


function  pregnancy_relatedness_all_change(p_control)
{
    const element_id_list = [
        "Pregnancy-Relatedness-1",
        "Pregnancy-Relatedness-0",
        "Pregnancy-Relatedness-2",
        "Pregnancy-Relatedness-99"
    ];

    if(p_control.checked)
    {
        if(g_filter.pregnancy_relatedness.indexOf(1) < 0)
        {
            g_filter.pregnancy_relatedness.push(1);
        }

        if(g_filter.pregnancy_relatedness.indexOf(0) < 0)
        {
            g_filter.pregnancy_relatedness.push(0);
        }

        if(g_filter.pregnancy_relatedness.indexOf(2) < 0)
        {
            g_filter.pregnancy_relatedness.push(2);
        }


        if(g_filter.pregnancy_relatedness.indexOf(99) < 0)
        {
            g_filter.pregnancy_relatedness.push(99);
        }

        for(let i = 0; i < element_id_list.length; i++)
        {
            const elem = document.getElementById(element_id_list[i]);
            elem.checked = true;
        }
    }
    else
    {
        if(g_filter.pregnancy_relatedness.indexOf(1) < 0)
        {
            g_filter.pregnancy_relatedness.splice(g_filter.pregnancy_relatedness.indexOf(1), 1);
        }

        if(g_filter.pregnancy_relatedness.indexOf(0) < 0)
        {
            g_filter.pregnancy_relatedness.splice(g_filter.pregnancy_relatedness.indexOf(0), 1);
        }

        if(g_filter.pregnancy_relatedness.indexOf(2) < 0)
        {
            g_filter.pregnancy_relatedness.splice(g_filter.pregnancy_relatedness.indexOf(2), 1);
        }


        if(g_filter.pregnancy_relatedness.indexOf(99) < 0)
        {
            g_filter.pregnancy_relatedness.splice(g_filter.pregnancy_relatedness.indexOf(99), 1);
        }

        for(let i = 0; i < element_id_list.length; i++)
        {
            const elem = document.getElementById(element_id_list[i]);
            elem.checked = false;
        }
    }

}

function  pregnancy_relatedness_1_change(p_control)
{
    if(p_control.checked)
    {
        if(g_filter.pregnancy_relatedness.indexOf(1) < 0)
        {
            g_filter.pregnancy_relatedness.push(1);
        }

    }
    else
    {
        if(g_filter.pregnancy_relatedness.indexOf(1) > -1)
        {
            g_filter.pregnancy_relatedness.splice(g_filter.pregnancy_relatedness.indexOf(1), 1);
        }
    }
}

function  pregnancy_relatedness_0_change(p_control)
{
    if(p_control.checked)
    {
        if(g_filter.pregnancy_relatedness.indexOf(0) < 0)
        {
            g_filter.pregnancy_relatedness.push(0);
        }

    }
    else
    {
        if(g_filter.pregnancy_relatedness.indexOf(0) > -1)
        {
            g_filter.pregnancy_relatedness.splice(g_filter.pregnancy_relatedness.indexOf(0), 1);
        }
    }
}

function  pregnancy_relatedness_2_change(p_control)
{
    if(p_control.checked)
    {
        if(g_filter.pregnancy_relatedness.indexOf(2) < 0)
        {
            g_filter.pregnancy_relatedness.push(2);
        }

    }
    else
    {
        if(g_filter.pregnancy_relatedness.indexOf(2) > -1)
        {
            g_filter.pregnancy_relatedness.splice(g_filter.pregnancy_relatedness.indexOf(2), 1);
        }
    }
}

function  pregnancy_relatedness_99_change(p_control)
{
    if(p_control.checked)
    {
        if(g_filter.pregnancy_relatedness.indexOf(99) < 0)
        {
            g_filter.pregnancy_relatedness.push(99);
        }

    }
    else
    {
        if(g_filter.pregnancy_relatedness.indexOf(99) > -1)
        {
            g_filter.pregnancy_relatedness.splice(g_filter.pregnancy_relatedness.indexOf(99), 1);
        }
    }
}

function proper_casing(str)
{
    if (!str) return str;
    
    const smallWords = ['of', 'at', 'the', 'in', 'by', 'on', 'for', 'to', 'a', 'an', 'and', 'or', 'but', 'with'];
    
    // Split by word boundaries (spaces, hyphens, slashes) while preserving separators
    return str.toLowerCase().split(/(\s+|-|\/)/g).map((part, index) => {
        // Skip separators (spaces, hyphens, slashes)
        if (/\s|-|\//.test(part)) {
            return part;
        }
        
        // Skip empty parts
        if (!part) return part;
        
        // Always capitalize first word (index 0) or if not a small word
        if (index === 0 || !smallWords.includes(part)) {
            return part.charAt(0).toUpperCase() + part.slice(1);
        }
        
        return part;
    }).join('');
}

function proper_case_categories(str)
{
    if (!str) return str;
    
    // Check if string has escaped quotes
    if (str.includes('\\"')) {
        // Handle escaped quotes: \"content\"
        const match = str.match(/^(\\")(.*?)(\\")/);
        if (match) {
            const prefix = match[1]; // \"
            const content = match[2]; // content
            const suffix = match[3]; // \"
            
            // Apply proper casing to the content
            const properCasedContent = proper_casing(content);
            
            // Return in original format with escaped quotes
            return prefix + properCasedContent + suffix;
        }
    } 
    else if (str.startsWith('"') && str.endsWith('"')) {
        // Handle regular quotes: "content"
        const content = str.slice(1, -1);
        const properCasedContent = proper_casing(content);
        return '"' + properCasedContent + '"';
    }
    
    // If no quotes, just apply proper casing
    return proper_casing(str);
}
