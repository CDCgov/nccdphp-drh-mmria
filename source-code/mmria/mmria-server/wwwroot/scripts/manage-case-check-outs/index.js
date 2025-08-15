var g_data = null;
var g_user_name = null;
var g_value_to_display_lookup = {};
var g_display_to_value_lookup = {};
var case_view_list = [];
var case_view_request = {
    total_rows: 0,
    page :1,
    skip : 0,
    take : 100,
    sort : "by_date_created",
    search_key : null,
    descending : true,
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



$(function ()
{//http://www.w3schools.com/html/html_layout.asp
  'use strict';
	/*profile.on_login_call_back = function (){
				load_users();
    };*/
	//profile.initialize_profile();

	loadUserParam();
	getCaseSet();
});

function loadUserParam()
{
	$.ajax({
    url: location.protocol + '//' + location.host + '/api/user/my-user',
	}).done(function(response) {
		g_user_name = response.name;
	});
}

function getCaseSet()
{
	var case_view_url = location.protocol + '//' + location.host + '/api/case_view' + case_view_request.get_query_string();
	var p_time = null;

  $.ajax({
		url: case_view_url,
  }).done(function(case_view_response) {
		//console.log(case_view_response);
    const checkedOutCases = [];
		case_view_request.total_rows = case_view_response.total_rows;

    for(let i = 0; i < case_view_response.rows.length; i++)
    {
			let caseView = case_view_response.rows[i];
			let caseLastUpdated = caseView.value.date_last_checked_out;
			// let isCheckedOut = caseView.value.last_checked_out_by;

			if (isCaseCheckedOut(caseView))
			{
				checkedOutCases.push(caseView);
			}

			// let case_view = case_view_response.rows[i];

			// if(is_case_checked_out(case_view))
			// {
			// 	case_view_list.push(case_view);
			// }
		}
    
    document.getElementById('output').innerHTML = renderCheckedOutCases(checkedOutCases).join('');
  });
}

function isCaseCheckedOut(p_case)
{
	let is_checked_out = false;
  let current_date = new Date();
  
  if(p_case.value.date_last_checked_out != null && p_case.value.date_last_checked_out != "")
  {
		let try_date = null;
		let is_date = false;

		if(!(p_case.value.date_last_checked_out instanceof Date))
		{
				try_date = new Date(p_case.value.date_last_checked_out);
		}
		else
		{
			try_date = p_case.value.date_last_checked_out;
		}
		
		if
		(
				getMinuteDifference(try_date, current_date) <= 120
				// p_case.value.last_checked_out_by.toLowerCase() == g_user_name.toLowerCase() //commented out but leaving for reference as Im not exactly sure what this is doing
		)
		{
			is_checked_out = true;
		}
	}

  return is_checked_out;
}

function renderCheckedOutCases(p_cases)
{
	const result = [];

	if (p_cases.length < 1)
	{
		result.push(
			`<p>No cases currently checked out</p>`
		);
	}
	else
	{
		result.push(
			`<table class="table">
				<thead class="thead">
					<tr class="tr">
						<th class="th h4 bg-secondary" colspan="6" scope="col">Current checked out cases</th>
					</tr>
				</thead>
				<thead class="thead">
					<tr class="tr">
						<th class="th" scope="col">Case Title</th>
						<th class="th" scope="col">Last Updated</th>
						<th class="th" scope="col">Time Locked</th>
						<th class="th" scope="col">Locked By</th>
						<th class="th" scope="col">Case Status</th>
						<th class="th" scope="col"></th>
					</tr>
				</thead>
				<tbody class="tbody">
					<!-- START loop through checked out cases -->
					${p_cases.map((item) => {
						const caseID = item.id;
						const jurisdictionID = item.value.jurisdiction_id;
						const firstName = item.value.first_name;
						const lastName = item.value.last_name;
						const recordID = item.value.record_id;
						const agencyCaseID = item.value.agency_case_id;

						let lastUpdatedDate = item.value.date_last_updated;
						lastUpdatedDate = new Date(lastUpdatedDate); //convert ISO format to MM/DD/YYYY
						lastUpdatedDate = lastUpdatedDate.toLocaleDateString('en-US');
						
						const lastCheckedOutDate = item.value.date_last_updated;
						let timeLocked = Math.abs(new Date(lastCheckedOutDate) - new Date());
						timeLocked = convertToReadableTime(timeLocked);

						const lockedBy = item.value.last_checked_out_by;
						const currentCaseStatus = item.value.case_status;
                        
                        const caseStatuses = {
                            "9999":"(blank)",	
                            "1":"Abstracting (Incomplete)",
                            "2":"Abstraction Complete",
                            "3":"Ready for Review",
                            "4":"Review Complete and Decision Entered",
                            "5":"Out of Scope and Death Certificate Entered",
                            "6":"False Positive and Death Certificate Entered",
                            "0":"Vitals Import"
                        }; 

						return (
							`<tr class="tr" data-id="${caseID}">
								<td class="td">
									${jurisdictionID && `${jurisdictionID}: ` || ''}
									${lastName && lastName || ''}, ${firstName && firstName || ''}
									${recordID && ` - (${recordID})`}
									${agencyCaseID && ` ac_id: ${agencyCaseID}`}
								</td>
								<td class="td">${lastUpdatedDate && lastUpdatedDate || ''}</td>
								<td class="td">${timeLocked && `${timeLocked} minutes` || ''}</td>
								<td class="td">${lockedBy && lockedBy || ''}</td>
								<td class="td" data-current-status="${currentCaseStatus}">${currentCaseStatus == null ? '(blank)' : caseStatuses[currentCaseStatus.toString()]}</td>
								<td class="td text-center"><button class="anti-btn link" onclick="handleCaseRelease('${caseID}')">Release</button></td>
							</tr>`
						)
					}).join('')}
				</tbody>
			</table>`
		);
	}
	
	return result;
}

function handleCaseRelease(p_id) 
{
    $.ajax({
		url: location.protocol + '//' + location.host + '/api/case?case_id=' + p_id //call the API and get current case
  }).done((response) => {
		g_data = response; //set to local var
		g_data.date_last_updated = new Date(); //set 'date_last_updated' prop
		g_data.date_last_checked_out = null; //set 'date_last_checked_out' prop
		g_data.last_checked_out_by = null; //set 'last_checked_out_by' prop

		//save and release case with a callback to rerender the table
		saveCaseAndRelease(g_data, getCaseSet);
	});
}

function saveCaseAndRelease(p_data, p_call_back) 
{
    let save_case_request = { 
        Change_Stack:{
            _id: $mmria.get_new_guid(),
            case_id: g_data._id,
            case_rev: g_data._rev,
            date_created: new Date().toISOString(),
            user_name: g_user_name, 
            items: [],
            metadata_version: "",
            note: "Manage Case Release"

        },
        Case_Data:p_data
    };

	$.ajax({
    url: location.protocol + '//' + location.host + '/api/case',
    contentType: 'application/json; charset=utf-8',
    dataType: 'json',
    data: JSON.stringify(save_case_request),
    type: "POST"
  }).done(function(response) {
		console.log("save_case: success");

		if(p_call_back)
    {
      p_call_back();
    }
	}).fail(function(xhr, err) { 
		console.log("server save_case: failed", err);

		if(xhr.status == 401)
		{
			let redirect_url = location.protocol + '//' + location.host;
			window.location = redirect_url;
		}
	});
}

function getMinuteDifference(dt1, dt2) 
{
  let diff =(dt2.getTime() - dt1.getTime()) / 1000;

	diff /= 60;
  return Math.abs(Math.round(diff));
}

function convertToReadableTime(millis) {
  var minutes = Math.floor(millis / 60000);
	var seconds = ((millis % 60000) / 1000).toFixed(0);
	
  return minutes + ":" + (seconds < 10 ? '0' : '') + seconds;
}
