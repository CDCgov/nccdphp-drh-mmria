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
  }).done(async function(case_view_response) {
		//console.log(case_view_response);
    const checkedOutCases = [];
    const offlineCases = [];
		case_view_request.total_rows = case_view_response.total_rows;

		// Fetch all active offline sessions before processing cases
		let offlineSessionsData = [];
		try {
			const offlineSessionsResponse = await $.ajax({
				url: location.protocol + '//' + location.host + '/api/OfflineCase/all-active-sessions'
			});
			
			// Handle both array and object responses
			if (Array.isArray(offlineSessionsResponse)) {
				offlineSessionsData = offlineSessionsResponse;
			} else if (offlineSessionsResponse && !offlineSessionsResponse.error) {
				offlineSessionsData = [offlineSessionsResponse];
			}
		} catch (error) {
			console.log('No active offline sessions found or error fetching:', error);
		}

    for(let i = 0; i < case_view_response.rows.length; i++)
    {
			let caseView = case_view_response.rows[i];
			let caseLastUpdated = caseView.value.date_last_checked_out;
			// let isCheckedOut = caseView.value.last_checked_out_by;

			if (isCaseCheckedOut(caseView))
			{
				checkedOutCases.push(caseView);
			}

			if (isCaseOffline(caseView))
			{
				// Find the associated offline session document
				const caseId = caseView.id;
				const offlineBy = caseView.value.offline_by;
				
				const associatedSession = offlineSessionsData.find(session => 
					session.offline_ids && 
					session.offline_ids.includes(caseId) &&
					session.created_by === offlineBy
				);
				
				// Append the offline session info to the caseView
				if (associatedSession) {
					caseView.offline_session = associatedSession;
				}
				
				offlineCases.push(caseView);
			}
		}
    
    // Render both tables
    let outputHtml = [];
    
    outputHtml.push(renderCheckedOutCases(checkedOutCases).join(''));
    outputHtml.push(renderOfflineCases(offlineCases).join(''));
    document.getElementById('output').innerHTML = outputHtml.join('');
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

function isCaseOffline(p_case)
{
	return p_case.value && p_case.value.is_offline === true;
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
						<th class="th h4 bg-secondary" colspan="6" scope="col">Online Cases</th>
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
								<td class="td text-center"><button class="btn btn-primary" onclick="handleCaseRelease('${caseID}')">Release</button></td>
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

function showOfflineKeyModal(p_case_id) 
{
	// Find the case in the rendered list
	const caseRow = document.querySelector(`tr[data-id="${p_case_id}"]`);
	if (!caseRow) {
		alert('Case not found');
		return;
	}

	// Get case data from the row's dataset
	const caseTitle = caseRow.querySelector('td:first-child')?.textContent?.trim() || 'Unknown Case';
	const lockedBy = caseRow.dataset.lockedBy || 'Unknown User';
	const offlineKey = caseRow.dataset.offlineKey || 'No key available';

	// Create modal HTML
	const modalHtml = `
		<div id="offline-key-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
			<div class="modal-dialog modal-lg" role="document">
				<div class="modal-content">
					<div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
						<h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Offline Key</h4>
						<button type="button" class="close" onclick="closeOfflineKeyModal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
							<span aria-hidden="true">&times;</span>
						</button>
					</div>
					<div class="modal-body" style="padding: 30px;">
						<div style="margin-bottom: 20px;">
							<div style="font-size: 14px; color: #6c757d; margin-bottom: 4px;">Case Title:</div>
							<div style="font-size: 16px; font-weight: 500;">${caseTitle}</div>
						</div>
						<div style="margin-bottom: 24px;">
							<div style="font-size: 14px; color: #6c757d; margin-bottom: 4px;">Locked By:</div>
							<div style="font-size: 16px; font-weight: 500;">${lockedBy}</div>
						</div>
						<div style="margin-bottom: 24px;">
							<label style="font-size: 16px; font-weight: 600; display: block; margin-bottom: 8px;">Offline Key:</label>
							<div style="display: flex; gap: 12px; align-items: center;">
								<input type="text" id="offlineKeyInput" value="${offlineKey}" readonly style="flex: 1; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px; background-color: #f8f9fa;">
								<button type="button" class="btn btn-outline-secondary" onclick="copyOfflineKey()" style="padding: 10px 20px; display: flex; align-items: center; gap: 8px; white-space: nowrap;">
									<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
										<rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
										<path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
									</svg>
									Copy Key
								</button>
							</div>
						</div>
					</div>
					<div class="modal-footer" style="padding: 20px 30px; text-align: right;">
						<button type="button" class="btn btn-primary" onclick="closeOfflineKeyModal()" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
							Close
						</button>
					</div>
				</div>
			</div>
		</div>
		<div id="offline-key-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
	`;

	// Add modal to body
	document.body.insertAdjacentHTML('beforeend', modalHtml);

	// Show modal with fade effect
	setTimeout(() => {
		const modal = document.getElementById('offline-key-modal');
		const backdrop = document.getElementById('offline-key-backdrop');
		if (modal && backdrop) {
			modal.classList.add('show');
			modal.style.display = 'block';
			backdrop.classList.add('show');
		}
	}, 10);
}

function closeOfflineKeyModal() 
{
	const modal = document.getElementById('offline-key-modal');
	const backdrop = document.getElementById('offline-key-backdrop');
	
	if (modal && backdrop) {
		modal.classList.remove('show');
		backdrop.classList.remove('show');
		
		setTimeout(() => {
			if (modal.parentNode) {
				modal.parentNode.removeChild(modal);
			}
			if (backdrop.parentNode) {
				backdrop.parentNode.removeChild(backdrop);
			}
		}, 150);
	}
}

function copyOfflineKey() 
{
	const input = document.getElementById('offlineKeyInput');
	if (input) {
		input.select();
		input.setSelectionRange(0, 99999); // For mobile devices
		
		try {
			document.execCommand('copy');
			
			// Show visual feedback
			const button = event.target.closest('button');
			const originalText = button.innerHTML;
			button.innerHTML = `
				<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<polyline points="20 6 9 17 4 12"></polyline>
				</svg>
				Copied!
			`;
			button.style.backgroundColor = '#28a745';
			button.style.borderColor = '#28a745';
			button.style.color = 'white';
			
			setTimeout(() => {
				button.innerHTML = originalText;
				button.style.backgroundColor = 'white';
				button.style.borderColor = '#772583';
				button.style.color = '#772583';
			}, 2000);
		} catch (err) {
			console.error('Failed to copy:', err);
			alert('Failed to copy key to clipboard');
		}
	}
}



function handleOfflineRemoval(p_id) 
{
    if (!confirm('Are you sure you want to remove this case from offline mode?')) {
        return;
    }
    
    $.ajax({
        url: location.protocol + '//' + location.host + '/api/case/toggle-offline/' + p_id,
        method: 'POST',
        contentType: 'application/json'
    }).done(function(response) {
        if (response.success) {
            console.log('Case removed from offline mode');
            getCaseSet(); // Refresh the list
        } else {
            alert('Failed to remove case from offline mode: ' + (response.message || 'Unknown error'));
        }
    }).fail(function(xhr, err) {
        console.log('Failed to remove case from offline mode', err);
        alert('Failed to remove case from offline mode. Please try again.');
    });
}

function renderOfflineCases(p_cases)
{
	const result = [];

	if (p_cases.length < 1)
	{
		result.push(
			`<div class="info-banner col-md-10 ml-1 mb-4">
				<img class="info-icon" src="./img/icon_info.svg" alt="Info">
				<span>No cases currently marked for offline work</span>
			</div>`
		);
	}
	else
	{
		result.push(
			`<div class="mb-4">
				<table class="table">
                    <thead class="thead">
                        <tr class="tr">
                            <th class="th h4 bg-secondary" colspan="8" scope="col">Offline cases</th>
                        </tr>
                    </thead>                
					<thead class="thead">						
						<tr class="tr">
							<th class="th" scope="col">Case Title</th>							
							<th class="th" scope="col">Last Updated</th>
							<th class="th" scope="col">Time Locked</th>							
							<th class="th" scope="col">Offline By</th>
                            <th class="th" scope="col">Case Status</th>
							<th class="th" scope="col">Lock Type</th>
							<th scope="col" class="th">Action</th>
						</tr>
					</thead>
					<tbody class="tbody">
						${p_cases.map((item) => {
							const caseID = item.id;
							const jurisdictionID = item.value.jurisdiction_id;
                            const host_state = item.value.host_state;
							const firstName = item.value.first_name;
							const lastName = item.value.last_name;
							const recordID = item.value.record_id;
						const agencyCaseID = item.value.agency_case_id;

						let lastUpdatedDate = '';
							if (item.value.date_last_updated) {
								try {
									lastUpdatedDate = new Date(item.value.date_last_updated).toLocaleDateString('en-US');
								} catch (e) {
									lastUpdatedDate = '';
								}
							}

						let offlineDate = '';
						let offlineTime = '';
						if (item.value.offline_date) {
							try {
								const offlineDateObj = new Date(item.value.offline_date);
								offlineDate = offlineDateObj.toLocaleDateString('en-US');
								// Format time as HH:MM in UTC
								const hours = offlineDateObj.getUTCHours();
								const minutes = offlineDateObj.getUTCMinutes();
								offlineTime = String(hours).padStart(2, '0') + ':' + String(minutes).padStart(2, '0');
							} catch (e) {
								offlineDate = '';
								offlineTime = '';
							}
						}

						const offlineBy = item.value.offline_by || '';
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

							const statusDisplay = currentCaseStatus == null ? '(blank)' : (caseStatuses[currentCaseStatus.toString()] || '(unknown)');

							// Get offline key from session if available
							const offlineKey = item.offline_session?.offline_key || 'No key available';
							const caseTitle = `${host_state ? host_state + ': ' : ''}${lastName || ''}${firstName ? ', ' + firstName : ''}${recordID ? ' - (' + recordID + ')' : ''}${agencyCaseID ? ' ac_id: ' + agencyCaseID : ''}`;
							const lockType = item.offline_session?.offline_key ? 'Offline' : 'Soft';
							const hasKey = item.offline_session?.offline_key ? true : false;

							return (
								`<tr class="tr" data-id="${caseID}" data-locked-by="${offlineBy}" data-offline-key="${offlineKey}">
									<td class="td">
										${caseTitle}
									</td>									
									<td class="td">${offlineDate}</td>
                                    <td class="td">${offlineTime}</td>
									<td class="td">${offlineBy}</td>									
                                    <td class="td">${statusDisplay}</td>
									<td class="td">${lockType}</td>
									<td class="td">
										<button class="btn btn-primary" onclick="handleOfflineRemoval('${caseID}')" title="Release" style="margin-right: 8px;">
											Release
										</button>
										<button class="btn btn-primary mt-2" onclick="showOfflineKeyModal('${caseID}')" title="View Key" ${!hasKey ? 'disabled' : ''}>
											View Key
										</button>
									</td>
								</tr>`
							)
						}).join('')}
					</tbody>
 <tfoot class='tfoot'>
                    <tr class='tr'>
                        <td class='td' colspan='8' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                            <div style='display: flex; justify-content: space-between; align-items: flex-start; gap: 20px;'>                        
                                <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #6c757d; line-height: 1.4; font-style: italic; flex: 1;'>
                                    <li style='margin-bottom: 4px;'>Upon release of an offline case, all changes made offline will be lost, and the case will revert to the last version saved on the server.</li>
                                    <li style='margin-bottom: 4px;'>Releasing an offline case will release the offline lock, and the case will only be available online.</li>
                                    <li style='margin-bottom: 4px;'>Please coordinate with the Abstractor working on the offline case before releasing the offline lock.</li>
                                </ul>
                                <div style='flex-shrink: 0; display: flex; align-items: flex-start;'>
                               
                                </div>                      
                            </div>                      
                        </td>                    
                    </tr>
                </tfoot>                                
				</table>
			</div>`
		);
	}
	
	return result;
}
