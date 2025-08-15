'use strict';

var g_batch_list = [];
var g_batch_list_state = [];
var g_batch_item_list = [];
var g_state_list = [];
var g_state_date_list = {};
var g_state_year_of_death_list = {};
var g_date_list = [];
var g_year_of_death_list = new Set();
var g_state_to_year_of_death_map = new Map();
var g_batch_list_excel_export = [];


var batch_item_status = [
    "Validating",
    "InProcess",
    "NewCaseAdded",
    "ExistingCaseSkipped",
    "ImportFailed",
    "BatchRejected"
];


var batch_item_status_display = [
    "Validating",
    "In Process",
    "New Case Added",
    "Existing Case Skipped",
    "Import Failed",
    "Batch Rejected - Import Failed"
];

var batch_status = [
    "Validating",
    "InProcess",
    "Finished",
    "FinishedSynchronized",
    "Deleted",
    "DeletedSynchronized",
    "BatchRejected"
];


window.onload = async function()
{
    get_batch_set();
    let el = document.getElementById("clear_data");
    if(el != null)
    {
        el.onclick = clear_all_data_click;
    }

    window.setTimeout(()=> { $mmria.get_cvs_api_server_info(()=>{},()=>{}); }, 0);

} 

async function get_batch_set()
{
    let el = document.getElementById("batch_list");
    el.innerHTML = `
    <div class="card-body bg-tertiary set-radius">
        <p class="mb-0">Fetching data...</p>
    </div>
    `

	const response = await $.ajax({
		url: location.protocol + '//' + location.host + `/api/ije_message`
	}).fail(function (xhr, err) 
    {
        alert(`server save_case: failed\n${err}\n${xhr.responseText}`);
/*
        $mmria.unstable_network_dialog_show(xhr, p_note);
        if (xhr.status == 401) 
        {
            let redirect_url = location.protocol + '//' + location.host;
            window.location = redirect_url;
        }
        else if (xhr.status == 200 && xhr.responseText.length >= 49000) 
        {
            let redirect_url = location.protocol + '//' + location.host;
            window.location = redirect_url;
        }
        */
    });


    if(response.error != null)
    {

        el.innerHTML = `
    <div class="card-body bg-tertiary set-radius">
        <p class="mb-0">Fetching data error... server save_case: failed\n${response.error}\n${response.reason}</p>
    </div>`
        return;
    }

    g_year_of_death_list.clear();
    g_batch_list = [];
    for(let i = 0; i < response.rows.length; i++)
    {
        let item = response.rows[i].doc;


        let reporting_state = item.reporting_state.toUpperCase();

        if(g_state_list.indexOf(reporting_state) < 0)
        {
            g_state_list.push(reporting_state);
        }

        
        if(g_state_date_list[reporting_state] == null)
        {
            g_state_date_list[reporting_state] = [];
        }

        if(g_state_year_of_death_list[reporting_state] == null)
        {
            g_state_year_of_death_list[reporting_state]  = new Set();
        }    

        let import_date = `${item.importDate.substr(5, 2)}/${item.importDate.substr(8, 2)}/${item.importDate.substr(0, 4)}`

        if( g_state_date_list[reporting_state].indexOf(import_date) < 0)
        {
            g_state_date_list[reporting_state].push(import_date);
        }

        
        function compare_dates(a, b)
        {
            const a_arr = a.split("/");
            const a_year = parseInt(a_arr[2]);
            const a_month = parseInt(a_arr[0]);
            const a_day = parseInt(a_arr[1]);

            const b_arr = b.split("/");
            const b_year = parseInt(b_arr[2]);
            const b_month = parseInt(b_arr[0]);
            const b_day = parseInt(b_arr[1]);

            if(b_year == a_year)
            {
                if(b_month == a_month)
                {
                    return b_day - a_day;
                }
                else
                {
                    return b_month - a_month;
                }
            }
            else
            {
                return b_year - a_year;
            }
            
        }

        
        if(g_date_list.indexOf(import_date) < 0)
        {
            g_date_list.push(import_date);
        }

        g_date_list.sort(compare_dates);
        g_state_date_list[reporting_state].sort(compare_dates);

        item.import_date = import_date;
        item.reporting_state = reporting_state;

        let is_batch_rejected  = false;
        if(item.status == 6)
        {
            is_batch_rejected = true;
        }

        for(let j = 0; j < item.record_result.length; j++)
        {
            let batch_item = item.record_result[j];
            
            if (batch_item.dateOfDeath != null || batch_item.dateOfDeath.length > 0)
            {
                let year_of_death_to_add = batch_item.dateOfDeath.split('-')[0];
                g_state_year_of_death_list[reporting_state].add(year_of_death_to_add);
                g_year_of_death_list.add(year_of_death_to_add);
            }

            batch_item.import_date = import_date;
            batch_item.reporting_state = reporting_state;

            if(is_batch_rejected)
            {
                batch_item.status = 5;
                batch_item.statusDetail = "Whole batch rejected";
            }

            g_batch_item_list.push(batch_item);
        }
        
        if(item.record_result.length > 0)
        {
            g_batch_list.push(item);
        }

    }


    initialize_ui()
        
        

	
}


function initialize_ui()
{

    let el = document.getElementById("batch_list");
    el.innerHTML =`
    <div class="card-body bg-tertiary set-radius">
        <p class="mb-0">Data retrieved initializing UI...</p>
    </div>
    `;

    render_batch_list()
}

/*
function clear_all_data_click()
{
	$.ajax({
        type: "DELETE",
		url: location.protocol + '//' + location.host + `/api/ije_message`
	}).done(function(response) {
        g_batch_list = [];
        
        render_batch_list()
        document.getElementById("report").innerHTML = "";

	});
}
*/

var g_current_state_batch = '';

function render_batch_list()
{
    let html_builder = [];
    g_batch_list_state = [];

    if(g_batch_list == null)
    {
        html_builder.push(`
            <div class="card-body bg-tertiary set-radius">
                <p class="mb-0">Unable to connect to vitals service. Please reload the page or come back later.</p>
            </div>
        `);
    }

    else if(g_batch_list.length > 0)
    {
        let batchedStateOptions = [];

        for (let j = 0; j < g_batch_list.length; j++) 
        {

            let item = g_batch_list[j];

            let itemState = item.reporting_state;

            if (batchedStateOptions.indexOf(itemState) === -1) 
            {
                batchedStateOptions.push(itemState)
            }
        }

        g_state_list.sort();


        html_builder.push(`
            <div class="form-inline">
                <label class="justify-content-start" style="width: 130px" for="state-list"><strong>State:</strong></label>
                <select id="state-list" class="form-control" style="width: 300px" onchange="javascript:state_list_onchange(this.value)">
                    <option value="">Select State</option>
                    <option value="all">All</option>
                    ${g_state_list.map(state => `<option value="${state}">${state}</option>`)}
                </select>
            </div>
            <div class="d-flex">
                <div class="form-inline mt-3 mr-3">
                    <label class="justify-content-start" style="width: 130px" for="date-list"><strong>Import Date:</strong></label>
                    <select id="date-list" class="form-control" style="width: 300px" onchange="javascript:date_list_onchange(this.value)">
                        <option value="all" selected>All</option>
                        ${g_date_list.map(date => `<option value="${date}">${date}</option>`)}
                    </select>
                </div>
                <div class="form-inline mt-3 mr-3">
                    <label class="justify-content-start" style="width: 130px" for="year-of-death-list"><strong>Year of Death:</strong></label>
                    <select id="year-of-death-list" class="form-control" style="width: 300px" onchange="javascript:year_of_death_date_list_onchange(this.value)">
                        <option value="all" selected>All</option>
                        ${[...g_year_of_death_list].map(year => `<option value="${year}">${year}</option>`)}
                    </select>
                </div>
                <div class="mt-4 ml-2">
                    <button id="vital-import-excel-download" aria-disabled="true" disabled type="button" class="btn btn-link" onclick="javascript:download_excel()">Download Excel</button>
                </div>
            </div>
        `);
    }
    else
    {

            html_builder.push(`
            <div class="card-body bg-tertiary set-radius">
                <p class="mb-0">No history of IJE uploads found.</p>
            </div>
        `);
        
    }


    let el = document.getElementById("batch_list");
    el.innerHTML = html_builder.join("");
}

async function download_excel()
{
    function select_cases()
    {
        let state_value = document.getElementById('state-list').value;
        let date_value = document.getElementById('date-list').value;
        let year_of_death_value = document.getElementById('year-of-death-list').value;
        let result = [];
        for (let i = 0; i < g_batch_item_list.length; i++) 
        {
            let item = g_batch_item_list[i];
    
            let is_valid_state = false;
            let is_valid_date = false;
            let is_valid_year_of_death = false;
    
            if(state_value == "all")
            {
                is_valid_state = true;
            }
            else if(item.reporting_state == state_value)
            {
                is_valid_state = true;
            }
    
    
            if(date_value == "all")
            {
                is_valid_date = true;
            }
            else if(item.import_date == date_value)
            {
                is_valid_date = true;
            }
    
            if(year_of_death_value == "all")
            {
                is_valid_year_of_death = true;
            }
            else if (item.dateOfDeath.split('-')[0] == year_of_death_value)
            {
                is_valid_year_of_death = true;
            }
    
            if(is_valid_state && is_valid_date && is_valid_year_of_death)
            {
                result.push(item);
            }
    
            
        }
        return result;
    }


    const case_set = select_cases();


    await $.ajax({
        url: location.protocol + '//' + location.host + '/api/ije_message/DownloadVitalImportExcel',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(case_set),
        xhrFields: {
            responseType: 'blob'
        },
        success: function(blob, status, xhr) {
            // check for a filename
            var filename = "";
            var disposition = xhr.getResponseHeader('Content-Disposition');
            if (disposition && disposition.indexOf('attachment') !== -1) {
                var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                var matches = filenameRegex.exec(disposition);
                if (matches != null && matches[1]) filename = matches[1].replace(/['"]/g, '');
            }
    
            if (typeof window.navigator.msSaveBlob !== 'undefined') {
                // IE workaround for "HTML7007: One or more blob URLs were revoked by closing the blob for which they were created. These URLs will no longer resolve as the data backing the URL has been freed."
                window.navigator.msSaveBlob(blob, filename);
            } else {
                var URL = window.URL || window.webkitURL;
                var downloadUrl = URL.createObjectURL(blob);
    
                if (filename) {
                    // use HTML5 a[download] attribute to specify filename
                    var a = document.createElement("a");
                    // safari doesn't support this yet
                    if (typeof a.download === 'undefined') {
                        window.location.href = downloadUrl;
                    } else {
                        a.href = downloadUrl;
                        a.download = filename;
                        document.body.appendChild(a);
                        a.click();
                    }
                } else {
                    window.location.href = downloadUrl;
                }
    
                setTimeout(function () { URL.revokeObjectURL(downloadUrl); }, 100); // cleanup
            }
        }
    }).catch(function(e) {
        console.log(e);
    });
}

async function state_list_onchange(p_value)
{
    const date_select = document.getElementById('date-list');
    const year_of_death_select = document.getElementById('year-of-death-list');
    const excel_download_button = document.getElementById('vital-import-excel-download');
    if(excel_download_button.disabled == true)
    {
        excel_download_button.disabled = false;
        excel_download_button.setAttribute('aria-disabled', 'false');
    }

    if(p_value == 'all' || p_value == '')
    { 
        date_select.innerHTML = create_new_option_list_html(p_value, g_date_list);
        year_of_death_select.innerHTML = render_year_of_death_option_list_html(p_value);

        if(p_value == '')
        {
            excel_download_button.disabled = true;
            excel_download_button.setAttribute('aria-disabled', 'true');      
        }


    }
    else
    {
        date_select.innerHTML = create_new_option_list_html(p_value);
        year_of_death_select.innerHTML = render_year_of_death_option_list_html(p_value);
    }
    prepare_batch();
}

function create_new_option_list_html(p_state)
{
    let subset = []
    
    if
    (
        p_state != null &&         
        p_state.trim().length != 0 &&
        p_state.toLowerCase() != "all"
        
        
    )
    {
        subset = g_state_date_list[p_state];
    }
    else
    {
        subset = g_date_list
    }
    const year_data = new Set();
    for(const item of subset)
    {
        year_data.add(item)
    }
    const html = [];
    html.push(`<option value="all">All</option>`);
    let listArray = Array.from(year_data);
    for (let i = 0; i < listArray.length; i++) 
    {
        const item = listArray[i];
        html.push(`<option value="${item}">${item}</option>`);
    }
    return html.join("");
}


function render_year_of_death_option_list_html(p_state)
{
    let subset = []
    
    if
    (
        p_state != null &&         
        p_state.trim().length != 0 &&
        p_state.toLowerCase() != "all"
        
        
    )
    {
        subset = g_state_date_list[p_state];
    }
    else
    {
        subset = g_date_list
    }
    const year_data = new Set();
    for(const item of subset)
    {
        year_data.add(item.substring(6,10))
    }
    const html = [];
    html.push(`<option value="all">All</option>`);
    let listArray = Array.from(year_data);
    for (let i = 0; i < listArray.length; i++) 
    {
        const item = listArray[i];
        html.push(`<option value="${item}">${item}</option>`);
    }
    return html.join("");
}


function date_list_onchange(p_value) 
{
    if(p_value != 'all')
        document.getElementById('year-of-death-list').value = 'all';
    prepare_batch();
}

function year_of_death_date_list_onchange(p_value)
{
    if(p_value != 'all')
        document.getElementById('date-list').value = 'all';
    prepare_batch();
}

function prepare_batch()
{
    let state_value = document.getElementById('state-list').value;
    let date_value = document.getElementById('date-list').value;
    let year_of_death_value = document.getElementById('year-of-death-list').value;
    let batch = [];
    for (let i = 0; i < g_batch_item_list.length; i++) 
    {
        let item = g_batch_item_list[i];

        let is_valid_state = false;
        let is_valid_date = false;
        let is_valid_year_of_death = false;

        if(state_value == "all")
        {
            is_valid_state = true;
        }
        else if(item.reporting_state == state_value)
        {
            is_valid_state = true;
        }


        if(date_value == "all")
        {
            is_valid_date = true;
        }
        else if(item.import_date == date_value)
        {
            is_valid_date = true;
        }

        if(year_of_death_value == "all")
        {
            is_valid_year_of_death = true;
        }
        else if (item.dateOfDeath.split('-')[0] == year_of_death_value)
        {
            is_valid_year_of_death = true;
        }

        if(is_valid_state && is_valid_date && is_valid_year_of_death)
        {
            batch.push(item);
        }

        
    }
    render_batch(batch);
}


function render_batch(p_batch)
{
    let html_builder = [];

    if (p_batch.length < 1) 
    {
        html_builder.push("");
    } 
    else 
    {
        
        const batchedItemsByStatus = {};

        for (let i = 0; i < batch_item_status.length; i++) 
        {
            batchedItemsByStatus[`batch_item_status_${i}`] = [];
        }
        
        for (let i = 0; i < p_batch.length; i++) 
        {

            let item = p_batch[i];
            for (let k = 0; k < batch_item_status.length; k++) 
            {
                if (item.status === k) 
                {
                    batchedItemsByStatus[`batch_item_status_${k}`].push(item);
                }
            }
            
        }

       for (let i = 0; i < batch_item_status.length; i++) 
        {
            if (batchedItemsByStatus[`batch_item_status_${i}`].length > 0) 
            {
                renderVitalsReportTable(i, batchedItemsByStatus[`batch_item_status_${i}`]);
            }
        }
    }

    let el = document.getElementById("report");
    el.innerHTML = html_builder.join("");

    function renderVitalsReportTable(index, items) 
    {
        const sortedItems = items.slice().sort((a,b) => new Date(b.importDate) - new Date(a.importDate));
        if(g_batch_list_state.length == 0)
            g_batch_list_state = sortedItems;
        else
            g_batch_list_state = g_batch_list_state.concat(sortedItems);
        //Build out the table
        html_builder.push(`<div class="report-section">`);
            html_builder.push(`<p>Total Records: <strong>${sortedItems.length}</strong></p>`);
            html_builder.push(`<table class="table">`);
                html_builder.push(`
                    <thead class="thead">
                        <tr class="tr bg-tertiary">
                            <th class="th" colspan="99" scope="colgroup">
                                <h4 class="m-0">${batch_item_status_display[index]}</h4>
                            </th>
                        </tr>
                    </thead>
                    <thead class="thead">
                        <tr class="tr" align="center">
                            <th class="th" width=65px>#</th>
                            <th class="th">MMRIA Record ID</th>
                            <th class="th">CDC Unique ID</th>
                            <th class="th">Last Name</th>
                            <th class="th">First Name</th>
                            <th class="th" width=120px>Date of Birth</th>
                            <th class="th" width=120px>Date of Death</th>
                            <th class="th">Reporting State</th>
                            <th class="th">State of<br/>Death Record</th>
                            <th class="th">Status Detail</th>
                        </tr>
                    </thead>
                    <tbody class="tbody">
                `);
                


                for(let i = 0; i < sortedItems.length; i++)
                {
                    let item = sortedItems[i];
                    html_builder.push
                    (`
                        <tr class="tr" data-import-state="${item.reportingState}" data-import-date="${item.importDate}">
                            <td class="td text-align-center">${i+1}</td>
                            <td class="td">${item.mmria_record_id == null? "": item.mmria_record_id}</td>
                            <td class="td">${item.cdcUniqueID}</td>
                            <td class="td">${item.lastName}</td>
                            <td class="td">${item.firstName}</td>
                            <td class="td">
                                ${item.dateOfBirth.split('-')[1]}/${item.dateOfBirth.split('-')[2]}/${item.dateOfBirth.split('-')[0]}
                            </td>
                            <td class="td">
                                ${item.dateOfDeath.split('-')[1]}/${item.dateOfDeath.split('-')[2]}/${item.dateOfDeath.split('-')[0]}
                            </td>
                            <td class="td">${item.reportingState}</td>
                            <td class="td">${item.stateOfDeathRecord}</td>
                            <td class="td"><span title="${item.statusDetail==null?"": item.statusDetail}">${item.statusDetail == null ? "" : item.statusDetail.substring(0,20)}</span></td>
                        </tr>
                    `);
                }
                html_builder.push("</tbody>");
            html_builder.push("</table>");
            html_builder.push(`<p>Total Records: <strong>${sortedItems.length}</strong></p>`);
        html_builder.push(`</div>`);
    }
}

