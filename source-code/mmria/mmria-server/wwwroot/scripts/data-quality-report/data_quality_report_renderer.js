
function data_quality_report_render(p_quarters) 
{
	var result = [];
	var data_quality_report_quarters_list = render_data_quality_report_quarters(p_quarters);
	g_model.selectedQuarter = p_quarters[0];
	g_model.reportType = 'Summary';

	result.push(`
        <div class="row">
            <div class="col">                    
                    <div class="mb-4">
                        <div><strong>Select Report Type:</strong></div>
                        <div>
                            <input type="radio" id="summary-report" name="report-type" value="Summary" onclick="updateReportType(event)" checked>
                            <label for="summary-report" class="mb-0 font-weight-normal mr-2">Summary Report</label>
                        </div>
                        <div>
                            <input type="radio" id="detail-report" name="report-type" value="Detail" onclick="updateReportType(event)">
                            <label for="detail-report" class="mb-0 font-weight-normal mr-2">Detail Report</label>
                        </div>
                        <div>
                            <input type="radio" id="summary-detail-report" name="report-type" value="Summary & Detail" onclick="updateReportType(event)">
                            <label for="summary-detail-report" class="mb-0 font-weight-normal mr-2">Summary & Detail Report</label>
                        </div>
                        <div style="display:none;">
                            <input type="radio" id="summary-detail-report-debug" name="report-type" value="Debug" onclick="updateReportType(event)">
                            <label for="summary-detail-report" class="mb-0 font-weight-normal mr-2">Debug</label>
                            <input type="text" id="debug-report-question" name="report-type" value=""/><br/>
                            <textarea id="debug-report-external-list" rows=7 cols=40></textarea>
                        </div>
                    </div>
             </div>

            <div class="col">
                <div class="mb-4">
                    <label for="quarters-list" class="mb-0 font-weight-bold mr-2">Select Quarter:</label><br>
                    <select size=10 style="width:160px"
                        name="quarters-list"
                        id="quarters-list"
                        onchange="updateQuarter(event)"
                    >	
                        ${data_quality_report_quarters_list}
                    </select>
                </div>
                <div id="quarter_msg" class="mb-3">
                ${g_model.selectedQuarter} is the currently selected quarter that will be used to compare to the previous 4 quarters.
                </div>
            </div>
						<div class="col">   
								<div class="mb-4" id="case_folder"></div>
						</div>
        </div>

        <div class="row">
				<div class="card-footer bg-gray-13"  style="width:100%">
					<button 
						id="generate_btn" 
						class="btn btn-primary btn-lg w-100" 
						onclick="download_data_quality_report_button_click()"
					>
						Generate ${g_model.reportType} Report for ${g_model.selectedQuarter}
					</button>
				</div>
			</div>
		</div>
	`);

	return result;
}



function render_case_folder_include_list()
{
  const el = document.getElementById("case_folder");
  const html_array = [];

	// Only run this if there is more than 1 case_folder
	if ( has_multiple_case_folder() )
	{
		//  Add title
		html_array.push("<div class='mb-0 font-weight-bold mr-2' >Select Cases From:</div>");

        for(var i = 0; i < g_case_folder_list.length; i++)
        {
            const child = g_case_folder_list[i];
            const element_id = child == "/" ? "topfolder": child.replace("/","_");
            
            
                    html_array.push("<div>")
                    html_array.push(`<label for='${element_id}'>`)
                    html_array.push(`<input id='${element_id}' value='`);
                    html_array.push(child.replace(/'/g, "&#39;"));
                    html_array.push("' ");
                    html_array.push("type='checkbox' ");
                    html_array.push("name='case_folder_checkbox' ");
                    html_array.push("onchange='updatecase_folder(event)' ");
                    html_array.push("checked>");
                    html_array.push(`${ (child == "/") ? "Top Folder" : child}`);
                    html_array.push("</label></div>")        
        }

        el.innerHTML = html_array.join("");
	}
	else
	{
		el.style.display = "none";
	}
}

function updateQuarter(e) 
{
	g_model.selectedQuarter = e.target.value;
	renderQuarterInfo();
}

function updateReportType(e)
{
	g_model.reportType = e.target.value;
	renderQuarterInfo();
}

function updatecase_folder(e)
{
	var included_case_folder = [];

	var checkboxes = document.getElementsByName('case_folder_checkbox');

	for ( var checkbox of checkboxes )
	{
		if ( checkbox.checked )
		{
			included_case_folder.push(checkbox.value);

		}
	}


	// Replace the case folder with checked items
	g_model.includedCaseFolder = [...included_case_folder];
	renderQuarterInfo();
}

function renderQuarterInfo() 
{
	// Update the message to show the currently selected quarter
	document.getElementById('quarter_msg').innerHTML =
		`${g_model.selectedQuarter} is the currently selected quarter that will be used to compare to the previous 4 quarters.`;

	// Update the button to show the currently selected quarter
	document.getElementById('generate_btn').innerHTML =
		`Generate ${g_model.reportType} Report for ${g_model.selectedQuarter}`;

	// Get the Generate button id - disable if no case_folder items in array
	document.getElementById('generate_btn').disabled = ( g_model.includedCaseFolder.length == 0 ) ? true : false;

}

function render_data_quality_report_quarters() 
{
	let result = [];

	// Build the dropdown list
	g_quarters.map((value, index) => {
		result.push(`<option value='${value}' ${(index == 0) ? ' selected' : ''}>${value}</option>`)
	});

	return result.join("");
}



const question_detail_map = new Map();
const case_detail_map = new Map();
const case_header_map = new Map();


function set_case_header
(

    p_detail
)
{
    case_header_map.set
    (
        p_detail._id,
        {
            //num: 316,
            rec_id: p_detail.record_id,
            dt_death: p_detail.dt_death,
            dt_com_rev: p_detail.dt_com_rev,
            ia_id: p_detail._id
        }
    );
}

function set_map_detail_data
(
    p_qid, //: 39,
    p_type, //: 'Current Quarter, Missing',
    p_case_id//: [
)
{

    let qid_map = question_detail_map.get(p_qid);
    if(qid_map == null)
    {
        qid_map = new Map();
        question_detail_map.set(p_qid,qid_map)
    }

    let type_map = qid_map.get(p_type);
    if(type_map == null)
    {
        type_map = new Set();
        qid_map.set(p_type, type_map);
    }

    type_map.add(p_case_id);


    let case_map = case_detail_map.get(p_case_id);
    if(case_map == null)
    {
        case_map = new Map();
        case_detail_map.set(p_case_id, case_map);
    }

    let case_qid_map = case_map.get(p_qid);
    if(case_qid_map == null)
    {
        case_qid_map = new Set();
        case_map.set(p_qid, case_qid_map);
    }

    case_qid_map.add({
        case_id: p_case_id,
        type: p_type
    })

}

function set_detail_data_case
(
    p_qid, //: 39,
    p_type, //: 'Current Quarter, Missing',
    p_detail//: [
)
{
    /*
    num: 316,
    rec_id: 'WI-2017-4726',
    dt_death: '10/17/2017',
    dt_com_rev: '05/14/2021',
    ia_id: '6d632b47-4950-a4d1-fa17-e7368eaeefe',
    */
}

var g_debug_report_question = null;
var g_debug_report_external_list = null;
var g_internal_set = null;
var g_current_set = new Set();
var g_previous_set = new Set();
var g_external_set = null;

const g_debug_list = [];
async function create_debug()
{
    //debug-report-question
    //debug-report-external-list
}

async function download_data_quality_report_button_click()
{

    show_loading_modal();

    g_debug_report_question = document.getElementById("debug-report-question").value;
    g_debug_report_external_list = document.getElementById("debug-report-external-list").value;
    g_internal_set = new Set();
    g_external_set = new Set();
    g_current_set = new Set();
    g_previous_set = new Set();

    question_detail_map.clear();
    case_detail_map.clear();
    case_header_map.clear();
    

    let selected_quarter = document.getElementById('quarters-list').value;
    let arr = selected_quarter.split("-");
    let quarter_number = parseFloat(`${arr[1].trim('"')}.${((parseInt(arr[0].replace("Q","")) - 1) * .25).toString().replace("0.","")}`);

    const selected_case_folder_list = get_selected_folder_list();

    let dqr_detail_data = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/dqr-detail/${selected_quarter}`,
    });
      
		

    let summary_data = get_new_summary_data();
    dqr_detail_data.docs.sort(
    (a, b) => {
        return a.record_id - b.record_id;
    });

    for(let i = 0; i < dqr_detail_data.docs.length; i++)
    {
        let item = dqr_detail_data.docs[i];

        const is_only_one_folder = selected_case_folder_list.length == 0;

        if(is_only_one_folder)
        {
            if
            (
                g_case_folder_list.indexOf("/") > -1
            )
            {
                // do nothing console.log("here");
            }
            else if
            (
                g_case_folder_list.indexOf(item.case_folder) < 0
            )
            {
                continue;
            }
        }
        else
        {
            const has_not_selected_root_folder = selected_case_folder_list.indexOf("/") < 0;

            if
            (
                has_not_selected_root_folder &&
                selected_case_folder_list.indexOf(item.case_folder) < 0
            )
            {
                continue;
            }

            const has_selected_root_folder = selected_case_folder_list.indexOf("/") > -1; 

            if
            (
                has_selected_root_folder &&
                item.case_folder != "/"
            )
            {
                if
                (           
                    
                    selected_case_folder_list.indexOf(item.case_folder) < 0
                )
                {
                
                    if(g_case_folder_list.indexOf(item.case_folder) < 0)
                    {
                        
                    }
                    else
                    {
                        continue;
                    }
                }
                
            }
            else if
            (
                g_case_folder_list.indexOf("/") > -1
            )
            {
                // do nothing console.log("here");
            }
            else if
            (

                g_case_folder_list.indexOf(item.case_folder) < 0
            )
            {
                continue;
            }
        }

        set_case_header(item);
        const new_id = item._id.replace("dqr-", "")
        
        if ( item.add_quarter_number <= quarter_number ) 
        {
            if(g_model.reportType == "Debug")
            {
                
                switch(g_debug_report_question)
                {
                    case "1":
                        if(item.n01 == 1) g_internal_set.add(new_id);
                    break;
                    case "2":
                        if(item.n02 == 1) g_internal_set.add(new_id);
                    break;
                    case "3":
                        if(item.n03 == 1) g_internal_set.add(new_id);
                    break;
                    case "4":
                        if(item.cmp_quarter_number <= quarter_number)
                        {
                            if(item.n04 == 1) g_internal_set.add(new_id);
                        }
                    break;
                    case "5":
                        if(item.n05 == 1) g_internal_set.add(new_id);
                    break;
                    case "6":
                        if(item.cmp_quarter_number == quarter_number)
                        /*if
                        (
                            item.cmp_quarter_number < quarter_number &&
                            item.cmp_quarter_number >= quarter_number - 1
                        )*/
                        {
                            if(item.n06 == 1) g_internal_set.add(new_id);
                        }
                    break;
                    case "7":
                        if(item.n07 == 1) g_internal_set.add(new_id);
                    break;
                    case "49":
                        
                        if(item.cmp_quarter_number == quarter_number)
                        {
                            if
                            (
                                item.n49.t == 1 //|| 
                                //item.n49.u == 1 
                            ) 
                            {
                                g_internal_set.add(new_id)
                                if(item.n49.p == 1)
                                {
                                    g_current_set.add(new_id);
                                }
                                else
                                {
                                    //g_previous_set.add(new_id);
                                }
                            }
                        }
                        else if
                        (
                            item.cmp_quarter_number < quarter_number &&
                            item.cmp_quarter_number >= quarter_number - 1.0
                        )
                        {
                            if
                            (
                                item.n49.t == 1 //|| 
                                //item.n49.u == 1 
                            ) 
                            {
                                g_internal_set.add(new_id)
                                if(item.n49.p == 1)
                                {
                                    g_previous_set.add(new_id);
                                }
                                else
                                {
                                    //g_previous_set.add(new_id);
                                }
                            }
                        }

                    break;
                    case "header":
                        if
                        (
                            item.cmp_quarter_number < quarter_number &&
                            item.cmp_quarter_number >= quarter_number - 1.25
                        )
                        {
                            if(item.n06 == 1) g_internal_set.add(new_id);
                        }
                        break; 
                    default:
                        break;
                }
                
            }

            summary_data.n01 += item.n01;
			summary_data.n02 += item.n02;


			summary_data.n03[0] += item.n03[0];
			summary_data.n03[1] += item.n03[1];
			summary_data.n03[2] += item.n03[2];
			summary_data.n03[3] += item.n03[3];
			summary_data.n03[4] += item.n03[4];
			summary_data.n03[5] += item.n03[5];
			summary_data.n03[6] += item.n03[6];
			summary_data.n03[7] += item.n03[7];

            if(item.cmp_quarter_number <= quarter_number)
            {
			    summary_data.n04 += item.n04;
                summary_data.n05 += item.n05;

            }
			
			
        

            if(item.cmp_quarter_number == quarter_number)
            {
                summary_data.n06 += item.n06;
                summary_data.n07 += item.n07;
                summary_data.current_hrcpr_bcp_secti_is_2 += item.hrcpr_bcp_secti_is_2
                summary_data.current_is_preventable_death += item.is_preventable_death
            }
            else if
            (
                item.cmp_quarter_number < quarter_number &&
                item.cmp_quarter_number >= quarter_number - 1.0
            )
            {
                summary_data.previous_hrcpr_bcp_secti_is_2 += item.hrcpr_bcp_secti_is_2
                summary_data.previous_is_preventable_death += item.is_preventable_death
                summary_data.previous4QuarterReview += item.n06;

                summary_data.n08 += item.n08;
                summary_data.n09 += item.n09;
            }


            if 
            ( 
                item.cmp_quarter_number == quarter_number 
            ) 
            {

                for(let i = 10; i < 50; i++)
                {
                    let fld = `n${i}`;
                    if
                    (
                        item[fld].m == 1 ||
                        (
                            i > 43 &&
                            item[fld].t != null &&
                            item[fld].p != null &&
                            item[fld].t == 1 &&
                            item[fld].p == 0
                        )
                    )
                    {
                        set_map_detail_data(i, "Current Quarter, Missing", item._id);
                    }

                    if
                    (
                        item[fld].u == 1 ||
                        ( 
                            i > 43 &&
                            item[fld].t != null &&
                            item[fld].p != null &&
                            item[fld].t == 1 &&
                            item[fld].p == 0
                        )
                    )
                    {
                        set_map_detail_data(i, "Current Quarter, Unknown", item._id);
                    }

                    if
                    ( 
                        i > 43 &&
                        item[fld].t != null &&
                        item[fld].p != null &&
                        item[fld].t == 1 &&
                        item[fld].p == 0
                    )
                    {
                        set_map_detail_data(i, "Current Quarter, Failed Logic Check", item._id);
                    }

                    // 10-44
                    if(i < 44)
                    {
                        summary_data[fld].s.mn += item[fld].m;
                        summary_data[fld].s.un += item[fld].u;
                    }
                    else
                    {
                        summary_data[fld].s.tn += item[fld].t;
                        summary_data[fld].s.pn += item[fld].p;
                    }
                }
            }
            

            if 
            ( 
                item.cmp_quarter_number < quarter_number &&
                item.cmp_quarter_number >= quarter_number - 1.0
            ) 
            {

                for(let i = 10; i < 50; i++)
                {
                    let fld = `n${i}`;

                    if
                    (
                        item[fld].m == 1 /*||
                        (
                            i > 43 &&
                            item[fld].t != null &&
                            item[fld].p != null &&
                            item[fld].t == 1 &&
                            item[fld].p == 0
                        )*/
                    )
                    {
                        set_map_detail_data(i, "Previous 4 Quarters, Missing", item._id);
                    }

                    if
                    (
                        item[fld].u == 1 /*||
                        (
                            i > 43 &&
                            item[fld].t != null &&
                            item[fld].p != null &&
                            item[fld].t == 1 &&
                            item[fld].p == 0
                        )*/
                    )
                    {
                        set_map_detail_data(i, "Previous 4 Quarters, Unknown", item._id);
                    }

                    if
                    (
                        
                        i > 43 &&
                        item[fld].t != null &&
                        item[fld].p != null &&
                        item[fld].t == 1 &&
                        item[fld].p == 0
                        
                    )
                    {
                        set_map_detail_data(i, "Previous 4 Quarters, Failed Logic Check", item._id);
                    }

                    // 10-44
                    if(i < 44)
                    {
                        summary_data[fld].p.mn += item[fld].m;
                        summary_data[fld].p.un += item[fld].u;
                    }
                    else
                    {
                        summary_data[fld].p.tn += item[fld].t;
                        summary_data[fld].p.pn += item[fld].p;
                    }
                }

            }
        }
    }

    // calculate summary percentages
    let startLoop = 10;
    let endLoop = 49;
    for ( let i = startLoop; i <= endLoop; i++ )
    {

        let fld = 'n' + i;

        if(i < 44)
        {
            // 12 14 17 22 25 26 

            if
            (
                i == 12 ||
                i == 14 ||
                i == 17 ||
                i == 22 ||
                i == 25 ||
                i == 26 ||
                i == 30
            )
            {
                if ( summary_data.current_hrcpr_bcp_secti_is_2 > 0)
                {
                    summary_data[fld].s.mp = (summary_data[fld].s.mn / summary_data.current_hrcpr_bcp_secti_is_2) * 100;
                    summary_data[fld].s.up = (summary_data[fld].s.un / summary_data.current_hrcpr_bcp_secti_is_2) * 100;
                }

                if ( summary_data.previous_hrcpr_bcp_secti_is_2 > 0)
                {
                    summary_data[fld].p.mp = (summary_data[fld].p.mn / summary_data.previous_hrcpr_bcp_secti_is_2) * 100;
                    summary_data[fld].p.up = (summary_data[fld].p.un / summary_data.previous_hrcpr_bcp_secti_is_2) * 100;
                }
            }
            else
            {
                if ( summary_data.n06 > 0)
                {
                    summary_data[fld].s.mp = (summary_data[fld].s.mn / summary_data.n06) * 100;
                    summary_data[fld].s.up = (summary_data[fld].s.un / summary_data.n06) * 100;
                }

                if ( summary_data.n08 > 0)
                {
                    summary_data[fld].p.mp = (summary_data[fld].p.mn / summary_data.n08) * 100;
                    summary_data[fld].p.up = (summary_data[fld].p.un / summary_data.n08) * 100;
                }
            }
        }
        else
        {
            if (summary_data[fld].s.tn > 0 )
            {
                summary_data[fld].s.pp = (summary_data[fld].s.pn / summary_data[fld].s.tn) * 100;
            }

            if (summary_data[fld].p.tn > 0 )
            {
                summary_data[fld].p.pp = (summary_data[fld].p.pn / summary_data[fld].p.tn) * 100;
            }
        }
    }

    //console.log('dqr_detail_data: ', dqr_detail_data);
    //console.log('summary_data: ', summary_data);


    if(g_model.reportType == "Debug")
    {
        /*
        g_internal_set
        var g_debug_report_question = null;
        var g_debug_report_external_list = null;
        var g_internal_set = null;
        g_external_set
        */

        const internal_only = new Set();
        const external_only = new Set();

        for(let item of g_debug_report_external_list.split("\n"))
        {
            g_external_set.add(item);
        }


        for(let item of g_internal_set)
        {
            if( !g_external_set.has(item))
            {
                internal_only.add(item);
            }
        }

        for(let item of g_external_set)
        {
            if( !g_internal_set.has(item))
            {
                external_only.add(item);
            }
        }

        const dd = {
            content: [
                `Selected DQR Question: ${g_debug_report_question}`,
            ]
            
        }

        const internal_ul = { ul: [] };
        dd.content.push( { text: "\n\n"});
        dd.content.push(`INTERNAL ONLY ****** ${internal_only.size}`);
        for(let item of internal_only)
        {
            const detail = case_header_map.get("dqr-" + item);
            const is_missing = g_current_set.has(item) ? "current" : "";
            const is_unknown = g_previous_set.has(item) ? "previous" : "";
            internal_ul.ul.push(`${item} ${detail.rec_id} ${detail.dt_death} ${detail.dt_com_rev} ${is_missing} ${is_unknown}`);
        }

        dd.content.push(internal_ul);
       
        dd.content.push( { text: "\n\n"});
        dd.content.push(`EXTERNAL ONLY ****** ${external_only.size}`);

        const external_ul = { ul: [] };
        for(let item of external_only)
        {
            external_ul.ul.push(`${item}`);
        }
        dd.content.push(external_ul);

        await pdfMake.createPdf(dd).open();
        
    }

    if 
    ( 
        g_model.reportType == 'Summary' || 
        g_model.reportType == 'Summary & Detail'
    )
    {
        let headers = {
            title: `Data Quality Report for: ${ getCaseFolder() }`,
            subtitle: `Reporting Period: ${ get_header_reporting_period(g_model.selectedQuarter) }    Previous 4 Periods: ${ getPreviousFourQuarters()}`,
        };

        await create_pdf(
            'Summary', summary_data, g_model.selectedQuarter, headers );
    }


    if 
    ( 
        g_model.reportType == 'Detail' || 
        g_model.reportType == 'Summary & Detail'
    )
    {
        let headers = {
            title: `Data Quality Report Details for: ${ getCaseFolder() }`,
            subtitle: `Reporting Period: ${ get_header_reporting_period(g_model.selectedQuarter) }   Previous 4 Periods: ${ getPreviousFourQuarters()}`,
        };
        

        // *****
        // Uncomment the lines below to test for empty detail report that show empty message
        // *****

        // let detail_data2 = {
        // 	questions: [],
        // 	cases: [],
        // 	total: 0,
        // };


       let detail_data = {
			questions: [
				/*{
					qid: 39,
					typ: 'Current Quarter, Missing',
					detail: [
						{
							num: 114,
							rec_id: 'WI-2016-8592',
							dt_death: '10/11/2016',
							dt_com_rev: '05/14/2021',
							ia_id: '6d632b47-4950-a4d1-fa17-e7368eaeefe',
						},
						{
							num: 115,
							rec_id: 'WI-2017-0052',
							dt_death: '10/02/2017',
							dt_com_rev: '11/20/2020',
							ia_id: '7e632b47-4950-a4d1-fa17-e7368eaeefe',
						},
						{
							num: 116,
							rec_id: 'WI-2017-4726',
							dt_death: '10/17/2017',
							dt_com_rev: '05/14/2021',
							ia_id: '6d632b47-4950-a4d1-fa17-e7368eaeefe',
						},
					],
				},
				{
					qid: 39,
					typ: 'Current Quarter, Unknown',
					detail: [],
				},
				{
					qid: 39,
					typ: 'Previous 4 Quarters, Missing',
					detail: [
						{
							num: 314,
							rec_id: 'WI-2016-8592',
							dt_death: '10/11/2016',
							dt_com_rev: '05/14/2021',
							ia_id: '6d632b47-4950-a4d1-fa17-e7368eaeefe',
						},
						{
							num: 315,
							rec_id: 'WI-2017-0052',
							dt_death: '10/02/2017',
							dt_com_rev: '11/20/2020',
							ia_id: '7e632b47-4950-a4d1-fa17-e7368eaeefe',
						},
						{
							num: 316,
							rec_id: 'WI-2017-4726',
							dt_death: '10/17/2017',
							dt_com_rev: '05/14/2021',
							ia_id: '6d632b47-4950-a4d1-fa17-e7368eaeefe',
						},
					],
				},
				{
					qid: 39,
					typ: 'Previous 4 Quarters, Unknown',
					detail: [],
				},
				{
					qid: 40,
					typ: 'Current Quarter, Missing',
					detail: [
						{
							num: 1,
							rec_id: 'OR-2019-4806',
							dt_death: '05/27/2009',
							dt_com_rev: '07/19/2021',
							ia_id: 'd1632b47-4950-a4d1-fa17-e7368eaeefe',
						},
						{
							num: 2,
							rec_id: 'TN-2020-4226',
							dt_death: '01/10/2019',
							dt_com_rev: '08/31/2021',
							ia_id: '2c632b47-4950-a4d1-fa17-e7368eaeefe',
						},
					],
				},
			*/],
			cases: [/*
				{
					rec_id: 'WI-2017-7951',
					ab_case_id: '',
					dt_death: '06/10/2017',
					dt_com_rev: '09/11/2020',
					ia_id: '2c632b47-4950-a4d1-fa17-e7368eaeefe',
					detail: [
						{
							qid: 19,
							typ: 'Previous 4 Quarters, Missing',
						},
						{
							qid: 29,
							typ: 'Previous 4 Quarters, Missing',
						},
						{
							qid: 32,
							typ: 'Previous 4 Quarters, Missing',
						},
						{
							qid: 39,
							typ: 'Previous 4 Quarters, Unknown',
						},
					]
				},
				{
					rec_id: 'WV-2019-1760',
					ab_case_id: '2019EleH',
					dt_death: '01/15/2019',
					dt_com_rev: '09/09/2021',
					ia_id: '8f032b47-4950-a4d1-fa17-e7368eaeefe',
					detail: [
						{
							qid: 19,
							typ: 'Current Quarter, Unknown',
						},
						{
							qid: 38,
							typ: 'Current Quarter, Missing',
						},
						{
							qid: 39,
							typ: 'Current Quarter, Unknown',
						},
					]
				},
			*/],
			total: 667,
		};


        let v_44_48 = new Set();
        v_44_48.add("n44");
        v_44_48.add("n45");
        v_44_48.add("n46");
        v_44_48.add("n47");
        v_44_48.add("n48");

        question_detail_map.forEach
        (
            (qitem, qid) => 
            {
                const current_quarter_missing = 
                {
                    qid: qid,
                    typ: "Current Quarter, Missing",
                    detail: []
                }
                const current_quarter_unknown = 
                {
                    qid: qid,
                    typ: "Current Quarter, Unknown",
                    detail: []
                }
                const current_quarter_failed_logic_check = 
                {
                    qid: qid,
                    typ: "Current Quarter, Failed Logic Check",
                    detail: []
                }
                const previous_quarter_missing = 
                {
                    qid: qid,
                    typ: "Previous 4 Quarters, Missing",
                    detail: []
                }

                const previous_quarter_unknown = 
                {
                    qid: qid,
                    typ: "Previous 4 Quarters, Unknown",
                    detail: []
                }

                const previous_quarter_failed_logic_check = 
                {
                    qid: qid,
                    typ: "Previous 4 Quarters, Failed Logic Check",
                    detail: []
                }
                
                qitem.forEach
                (
                    (t_item, type_id) => 
                    {
                        let num_count = 1;
                        t_item.forEach
                        (
                            (case_id) =>
                            {
                                const header = case_header_map.get(case_id);
                                switch(type_id)
                                {
                                    case "Current Quarter, Missing":

                                        //if(v_44_48.has())

                                        current_quarter_missing.detail.push({
                                            num: num_count,
                                            rec_id: header.rec_id,
                                            dt_death: header.dt_death,
                                            dt_com_rev: header.dt_com_rev,
                                            ia_id: header.ia_id.substring(4),
                                        });
                                        break;
                                    case "Current Quarter, Unknown":
                                        current_quarter_unknown.detail.push({
                                            num: num_count,
                                            rec_id: header.rec_id,
                                            dt_death: header.dt_death,
                                            dt_com_rev: header.dt_com_rev,
                                            ia_id: header.ia_id.substring(4),
                                        });
                                        break;
                                    case "Current Quarter, Failed Logic Check":
                                        current_quarter_failed_logic_check.detail.push({
                                            num: num_count,
                                            rec_id: header.rec_id,
                                            dt_death: header.dt_death,
                                            dt_com_rev: header.dt_com_rev,
                                            ia_id: header.ia_id.substring(4),
                                        });
                                        break;
                                    case "Previous 4 Quarters, Missing":
                                        previous_quarter_missing.detail.push({
                                            num: num_count,
                                            rec_id: header.rec_id,
                                            dt_death: header.dt_death,
                                            dt_com_rev: header.dt_com_rev,
                                            ia_id: header.ia_id.substring(4),
                                        });
                                        break;
                                    case "Previous 4 Quarters, Unknown":
                                        previous_quarter_unknown.detail.push({
                                            num: num_count,
                                            rec_id: header.rec_id,
                                            dt_death: header.dt_death,
                                            dt_com_rev: header.dt_com_rev,
                                            ia_id: header.ia_id.substring(4),
                                        });
                                        break;
                                    case "Previous 4 Quarters, Failed Logic Check":
                                        previous_quarter_failed_logic_check.detail.push({
                                            num: num_count,
                                            rec_id: header.rec_id,
                                            dt_death: header.dt_death,
                                            dt_com_rev: header.dt_com_rev,
                                            ia_id: header.ia_id.substring(4),
                                        });
                                        break;
                                }

                                num_count += 1;
                                //console.log("here");
                            }
                        );
                    }
                );


                if(current_quarter_missing.detail.length > 0)  
                {
                    detail_data.questions.push(current_quarter_missing);
                }
                if(current_quarter_unknown.detail.length > 0)  
                {
                    detail_data.questions.push(current_quarter_unknown);
                }
                if(current_quarter_failed_logic_check.detail.length > 0)  
                {
                    detail_data.questions.push(current_quarter_failed_logic_check);
                }
                if(previous_quarter_missing.detail.length > 0) 
                {
                    detail_data.questions.push(previous_quarter_missing);
                }
                if(previous_quarter_unknown.detail.length > 0) 
                {
                    detail_data.questions.push(previous_quarter_unknown);
                }

                if(previous_quarter_failed_logic_check.detail.length > 0) 
                {
                    detail_data.questions.push(previous_quarter_failed_logic_check);
                }
            }
        );

        detail_data.questions.sort((a, b) => {
            return a.qid - b.qid;
        });
        detail_data.total = detail_data.questions.length;




        case_detail_map.forEach
        (
            (qitem, case_id) => 
            {
                const header = case_header_map.get(case_id);
                let new_item = {
                    rec_id: header.rec_id,
                    dt_death: header.dt_death,
                    dt_com_rev: header.dt_com_rev,
                    ia_id: header.ia_id.substring(4),
					ab_case_id: '',
					detail: []
                }
                
                qitem.forEach
                (
                    (t_item, qid) => 
                    {

                        new_item.detail.push({ qid: qid, typ:t_item.values().next().value.type});
                    }
                );

                detail_data.cases.push(new_item);
            }
        );

        await create_pdf( 'Detail', detail_data, g_model.selectedQuarter, headers );
    }

    close_loading_modal();
}



function getCaseFolder()
{
    const top_folder_name = sanitize_encodeHTML(window.location.host.toUpperCase().split("-")[0]) + "-MMRIA";

    var case_folder_display = top_folder_name;
    var case_folder_exclude = ' - Exclude: ';

    const display_size = 25;
    const display_number = 22;


    if ( g_case_folder_list.length == 1 )
    {
        case_folder_display = top_folder_name;
        return case_folder_display.length > display_size? case_folder_display.substring(0, display_number) + "..." : case_folder_display;
    }

    
    if ( g_case_folder_list.length == g_model.includedCaseFolder.length )
    {
        case_folder_display = top_folder_name;
        return case_folder_display;
    }

    if ( g_model.includedCaseFolder[0] == '/' )
    {

        case_folder_display = top_folder_name;


        g_case_folder_list.map( (j, i) => {
            if ( j != '/' )
            {
                case_folder_exclude += ( g_model.includedCaseFolder.indexOf(j) > -1 ) ? '' : j + ', ';
            }
        });


        case_folder_display += case_folder_exclude.substring(0, (case_folder_exclude.length - 2));
        
        return case_folder_display.length > display_size? case_folder_display.substring(0, display_number) + "..." : case_folder_display;
    }


    case_folder_display = '';
    g_model.includedCaseFolder.map( (j, i) => {
        if ( i > 0 )
        {
            case_folder_display += ', ';
        }
        case_folder_display += j;
    });

    return case_folder_display.length > display_size? case_folder_display.substring(0, display_number) + "..." : case_folder_display;
}


function getPreviousFourQuarters()
{
    let qStr = '';

    let arr = g_model.selectedQuarter.split("-");
    let qtr = parseInt(arr[0][1]);
    let yy = parseInt(arr[1].substr(2));
    
    for ( let i = 0; i < 4; i++ )
    {
        
        qtr--;
        if ( qtr == 0 )
        {
            qtr = 4;
            yy--;
        }
        qStr += `Q${qtr}-${yy}`;

        if ( i < 3 ) qStr += ', ';
    }

    return qStr;
}


function get_header_reporting_period(value)
{
    let result = value;
    const arr = value.split("-");
    const year_string = arr[1];
    const year_two_digit = arr[1].substr(2);

    switch(arr[0].toUpperCase())
    {
        case 'Q1':
            result = `Q1-${year_two_digit} (Jan-Mar ${year_string})`;
        break;
        case 'Q2':
            result = `Q2-${year_two_digit} (Apr-Jun ${year_string})`;
        break;
        case 'Q3':
            result = `Q3-${year_two_digit} (Jul-Sep ${year_string})`;
        break;
        case 'Q4':
            result = `Q4-${year_two_digit} (Oct-Dec ${year_string})`;
        break;

    }


    return result;
}


async function create_pdf( report_type, data, quarter, headers )
{
	await create_data_quality_report_pdf( report_type, data, quarter, headers );
}
