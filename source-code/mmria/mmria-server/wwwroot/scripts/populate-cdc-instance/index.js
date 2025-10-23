'use strict';

var message_history = [];
var g_data = {};
var g_server = {}


window.onload = async function()
{
    g_data = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/populate_cdc_instance`,
    });

    /*
    g_server = await $.ajax
    (
        {
				url: location.protocol + '//' + location.host + '/api/populate_cdc_instance',
				//contentType: 'application/json; charset=utf-8',
				//dataType: 'json',
				//data: JSON.stringify(g_data),
				//type: "Get"
		}
    );*/
    main();
}


function main()
{
    render();
}

function render()
{
    const el = document.getElementById("output");



    const result = [];

    result.push(render_transfer_status());
    
    result.push(`<div id='message-area-id'>${render_messages()} </div>`)
    
    result.push("<p  align=center style='text-align:right'>")

    result.push(render_save_button());
    result.push(render_submit_button());
    
    result.push("</p>")
    result.push(render_table());

    el.innerHTML = result.join("");
}

function render_save_button()
{
    let is_diabled = '';
    if(g_data.transfer_status_number === 1)
    {
        is_diabled = 'disabled="disabled"';
    }

    return `
<label><button id="save_btn" class="btn primary-button" onclick="save_selections_button_click()" ${is_diabled}>
Save Selections
</button></label>`;
}

function render_submit_button()
{
    let is_diabled = '';
    if(g_data.transfer_status_number === 1)
    {
        is_diabled = 'disabled="disabled"';
    }
    return `
    <label>
<button id="generate_btn" class="btn primary-button" onclick="submit_button_click()" ${is_diabled}>
Submit
</button></label>`;

}

function render_transfer_status()
{
    const result = [];

    switch(g_data.transfer_status_number)
    {

        case 1:
            result.push(`
                <div class="info-banner ml-1 mr-1">
                    <img class="refresh-icon" src="./img/icon_refresh.svg" alt="Transfer in progress">
                    <span>${g_data.transfer_result}</span>
                </div>
            `);
            break;
        case 2:
            result.push(`
            <div class="error-banner ml-1 mr-1">
                <img class="error-icon" src="./img/icon_error.svg" alt="Transfer error">
                <span>${g_data.transfer_result}</span>
            </div>
        `);
            break;
        case 0:
        default:
            result.push(`
            <div class="success-banner ml-1 mr-1">
                <img class="success-icon" src="./img/icon_success.svg" alt="Transfer complete">
                <span>${g_data.transfer_result}</span>
            </div>
            `);
            break;
    }

    return result.join("");
}

function render_table()
{
    const result = [];
    result.push(`

    <table class="table" align=center>
        <caption class="table-caption">
            Table listing all MMRIA sites and checkboxes to select which sites to transfer to the central MMRIA instance.
        </caption>
        <thead>
            <tr class="header-level-2" align=center>
                <th class="text-left">#</th>
                <th style="margin-left:10px;margin-right:10px">Transfer to Central MMRIA Instance</th>
                <!--th>Prefix</th-->
                <th>MMRIA Site Name</th>
            </tr>
        </thead>
        ${rendert_state_list()}
    </table>

    `);

    return result.join("");
}


function rendert_state_list()
{
    const result = [];

    let is_diabled = '';
    if(g_data.transfer_status_number === 1)
    {
        is_diabled = 'disabled="disabled"';
    }

    
    

    for(let i = 0; i < g_data.state_list.length; i++)
    {
        const item = g_data.state_list[i];
        const number = i + 1;

        result.push(`
            <tr>
                <td>${number}</td>
                <td class="d-flex justify-content-center">
                    <div class="form-check">
                        <input class="form-input-check big-checkbox" aria-label="Select ${item.name}" id='checkbox${i}' type=checkbox value=${i} onclick='checkbox_clicked(${i})' ${item.is_included == true ? "checked":""} ${is_diabled}/>
                        <label></label>
                    </div>
                </td>
                <!--td style='text-align:left'><input type=text value=${item.prefix} onchange='prefix_changed(${i}, this.value)' ${is_diabled}/></td>
                <td style='text-align:left'>${item.prefix}</td>
                <td style='text-align:left'><input type=text size=50 value='${item.name}' onchange='name_changed(${i}, this.value)' ${is_diabled}/></td
                -->
                <td style='text-align:left'><label for='checkbox${i}'>${item.name}<label></td>
            </tr>
        `);
    }

    return result.join("");
}

function render_messages()
{
    if(message_history.length > 0)
    {
        return message_history[message_history.length - 1];
    }
    else return "";
}


async function save_selections_button_click()
{

	const response = await $.ajax
    (
        {
				url: location.protocol + '//' + location.host + '/api/populate_cdc_instance',
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				data: JSON.stringify(g_data),
				type: "POST"
		}
    );
    

    if(response.ok)
    {
        g_data._rev = response.rev; 
        message_history.push(`
        <div class="success-banner ml-1 mr-1">
            <img class="success-icon" src="./img/icon_success.svg" alt="Save successful">
            <span>Save successful on ${formatDate(new Date())}</span>
        </div>`);
        render();
    }
    else
    {
        message_history.push(`
        <div class="error-banner ml-1 mr-1">
            <img class="error-icon" src="./img/icon_error.svg" alt="Error when saving">
            <span>Current selections could not be saved. Please contact your system administrator for assistance.</span>
        </div>`);
        render();
    }
		
		
}

function sanitize_encodeHTML(s) 
{
	let result = s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    return result;
}

async function checkbox_clicked(i)
{
    g_data.state_list[i].is_included = !g_data.state_list[i].is_included;
}

async function prefix_changed(i, value)
{
    g_data.state_list[i].prefix = value;
}

async function name_changed(i, value)
{
    g_data.state_list[i].name = value;
}

async function submit_button_click()
{


    const save_response = await $.ajax
    (
        {
				url: location.protocol + '//' + location.host + '/api/populate_cdc_instance',
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				data: JSON.stringify(g_data),
				type: "POST"
		}
    );

    message_history = [];
        
    if(!save_response.ok)
    {
        g_data._rev = save_response.rev; 
        message_history.push(`
        <div class="error-banner ml-1 mr-1">
            <img class="error-icon" src="./img/icon_error.svg" alt="Error when saving">
            <span>Current selections could not be saved. Please contact your system administrator for assistance.</span>
        </div>`);
        render();
        return;
    }


	const response = await $.ajax
    (
        {
				url: location.protocol + '//' + location.host + '/api/populate_cdc_instance',
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				data: JSON.stringify(g_data),
				type: "PUT"
		}
    );
        

    if(response.transfer_status_number == 1)
    {
        g_data = await $.ajax
        ({
            url: `${location.protocol}//${location.host}/api/populate_cdc_instance`,
        });
        render();
    }
    else
    {
        message_history.push(`Current selections could not be saved. Please contact your system administrator for assistance.`);
        render();
    }

    
		
		
}

function pad_number(n) 
{
    n = n + '';
    return n.length >= 2 ? n : new Array(2 - n.length + 1).join("0") + n;
}

function formatDate(p_value)
{
    const result= `${pad_number(p_value.getMonth() + 1)}/${pad_number(p_value.getDate())}/${p_value.getFullYear()} at ${pad_number(p_value.getHours())}:${pad_number(p_value.getMinutes())}:${pad_number(p_value.getSeconds())}`;

    return result;
}