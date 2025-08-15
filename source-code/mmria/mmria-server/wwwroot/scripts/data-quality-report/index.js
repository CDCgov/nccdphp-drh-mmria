var g_release_version = null;
var g_quarters = new Array();
var g_case_folder_list = [];
var g_user_role_case_folder_list = [];
var g_case_folder_tree = [];
var g_user_name = null;
var g_dqr_notes_list = null;

var g_model = {
    reportType: "Summary",
    selectedQuarter: "",
    includedCaseFolder: []
};


$(async function ()
{
  'use strict';
	document.getElementById('form_content_id').innerHTML = '';
	await main();

	display_page();
});

async function main()
{

    const release_version = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/release-version`,
    });
      
    g_release_version = release_version;

    const case_folder_tree_url = `${location.protocol}//${location.host}/api/jurisdiction_tree`;

    const case_folder_tree = await $.ajax
    ({
        url: case_folder_tree_url,
    });


    g_case_folder_tree = case_folder_tree;

    const my_user_response = await $.ajax
    ({
        url: location.protocol + '//' + location.host + '/api/user/my-user',
    });

    
    g_user_name = my_user_response.name;


    g_dqr_notes_list = get_dqr_notes_list();

    const my_role_list_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/user_role_jurisdiction_view/my-roles`, //&search_key=' + g_uid,
    });
    
    g_user_role_case_folder_list = [];
    for (let i in my_role_list_response.rows) 
    {
        let value = my_role_list_response.rows[i].value;
        if
        (
            value.role_name=="abstractor" ||
            value.role_name=="data_analyst"
        )
        {
            g_user_role_case_folder_list.push(value.jurisdiction_id);
        }
    }

    create_case_folder_list(g_case_folder_tree);

    g_model.includedCaseFolder = [...g_case_folder_list];

}


function display_page() 
{
	g_quarters = load_quarters();
	render();
}

function load_quarters()
{
	let today = new Date();
	let curYear = today.getFullYear();
	let curQuarter = Math.floor((today.getMonth() + 3) / 3);
	let quarters = new Array();
	let cutoffYear = 2019;	

	while ( curQuarter > 0 )
	{
		quarters.push( `Q${ curQuarter }-${ curYear }` );
		curQuarter--;
	}

	
	curYear--;
	while ( curYear >= cutoffYear )
	{
		quarters.push( `Q4-${ curYear }`);
		quarters.push( `Q3-${ curYear }`);
		quarters.push( `Q2-${ curYear }`);
		quarters.push( `Q1-${ curYear }`);
		curYear--;
	}

	return quarters;
}


function render()
{
	document.getElementById('form_content_id').innerHTML = data_quality_report_render(
		g_quarters
	).join('');

    render_case_folder_include_list();
}


function create_case_folder_list(p_data) 
{
  for (var i = 0; i < g_user_role_case_folder_list.length; i++) 
  {
    var case_folder_regex = new RegExp('^' + g_user_role_case_folder_list[i]);
    var match = p_data.name.match(case_folder_regex);

    if (match) 
    {
      g_case_folder_list.push(p_data.name);
      break;
    }
  }

  if (p_data.children != null) 
  {
    for (var i = 0; i < p_data.children.length; i++) 
    {
      var child = p_data.children[i];

      create_case_folder_list(child);
    }
  }
}



function get_selected_folder_list()
{
    const result = [];

    const elementList = document.querySelectorAll("input[checked]");
    
    for(let i = 0; i < elementList.length; i++)
    {
        const item = elementList[i];
        if(item.checked && item.value.indexOf("/") == 0)
        {
            result.push(item.value);
        }
    }

    return result;
}


function has_multiple_case_folder()
{
	return (g_case_folder_list.length > 1) ? true : false;
}

function get_new_summary_data()
{
    return {
        current_is_preventable_death: 0,
        previous_is_preventable_death: 0,
        current_hrcpr_bcp_secti_is_2: 0,
        previous_hrcpr_bcp_secti_is_2: 0,
        previous4QuarterReview: 0,
        n01:  0,
        n02 : 0,
        n03 : [ 0, 0, 0, 0, 0, 0, 0, 0 ],
        n04 : 0,
        n05 : 0,
        n06 : 0,
        n07 : 0,
        n08 : 0,
        n09 : 0,
				n10: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n11: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n12: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n13: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n14: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n15: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n16: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n17: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n18: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n19: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n20: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n21: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n22: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n23: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n24: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n25: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n26: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n27: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n28: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n29: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n30: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n31: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n32: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n33: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n34: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n35: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n36: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n37: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n38: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n39: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n40: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n41: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n42: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n43: {
					s: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
					p: { mn: 0, mp: 0.0, un: 0, up: 0.0 },
				},
				n44: {
					s: { tn: 0, pn: 0, pp: 0.0 },
					p: { tn: 0, pn: 0, pp: 0.0 },
				},
				n45: {
					s: { tn: 0, pn: 0, pp: 0.0 },
					p: { tn: 0, pn: 0, pp: 0.0 },
				},
				n46: {
					s: { tn: 0, pn: 0, pp: 0.0 },
					p: { tn: 0, pn: 0, pp: 0.0 },
				},
				n47: {
					s: { tn: 0, pn: 0, pp: 0.0 },
					p: { tn: 0, pn: 0, pp: 0.0 },
				},
				n48: {
					s: { tn: 0, pn: 0, pp: 0.0 },
					p: { tn: 0, pn: 0, pp: 0.0 },
				},
				n49: {
					s: { tn: 0, pn: 0, pp: 0.0 },
					p: { tn: 0, pn: 0, pp: 0.0 },
				},
    }
}

function show_loading_modal()
{
    const el = document.getElementById("loading-modal");    
    el.innerHTML = `
    <div style="padding:50px;" class="display-6">
    <div id="form_content_id" >
    <span class="spinner-container spinner-content spinner-active">
        <span class="spinner-body text-primary">
        <span class="spinner"></span>
        <span class="spinner-info">Loading...</span>
        </span>
    </span>
    </div>
    </div>
`;

    el.showModal();
}

function close_loading_modal()
{
    const el = document.getElementById("loading-modal");
    el.close();    

}
