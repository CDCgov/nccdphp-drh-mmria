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
<label><button id="save_btn" class="btn btn-primary btn-lg " onclick="save_selections_button_click()" ${is_diabled}>
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
<button id="generate_btn" class="btn btn-primary btn-lg " onclick="submit_button_click()" ${is_diabled}>
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
                <p style="
                vertical-align: middle;
                width: 100%; 
                border-radius: 4px;
                border: 1px solid #e3d3e4;
                background-color: #f7f2f7;
                "><img src=${location.protocol}//${location.host}/img/TransferInProgress.svg alt="Transfer in progress." />
                ${g_data.transfer_result}
                </p>
            `);
            break;
        case 2:
            result.push(`
            <p style="
            vertical-align: middle;
            width: 100%; 
            border-radius: 4px;
            border: 1px solid #FFC2C2;
            background-color: #FFE7E7;
            "><img src=${location.protocol}//${location.host}/img/TransferError.svg alt="Transfer error."/>
            ${g_data.transfer_result}
            </p>
        `);
            break;
        case 0:
        default:
            result.push(`
            <p style="
            vertical-align: middle;
            width: 100%; 
            border-radius: 4px;
            border: 1px solid #DCEDC8;
            background-color: #F1F8E9;
            "><img src=${location.protocol}//${location.host}/img/TransferComplete.svg alt="Transfer complete." />
            ${g_data.transfer_result}
            </p>
            `);
            break;
    }

    return result.join("");
}

function render_table()
{
    const result = [];
    result.push(`

    <table align=center>
        <thead>
            <tr style="background-color:#b890bb;" align=center>
                <th>#</th>
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

        let bg_color = '';

        if(i % 2 == 1)
        {
            bg_color = "style='background-color:#DDDDDD;'"
        }

        result.push(`
            <tr ${bg_color}>
                <td>${number}</td>
                <td style='text-align:center'><input id='checkbox${i}' type=checkbox value=${i} onclick='checkbox_clicked(${i})' ${item.is_included == true ? "checked":""} ${is_diabled}/></td>
                <!--td style='text-align:left'><input type=text value=${item.prefix} onchange='prefix_changed(${i}, this.value)' ${is_diabled}/></td>
                <td style='text-align:left'>${item.prefix}</td>
                <td style='text-align:left'><input type=text size=50 value='${item.name}' onchange='name_changed(${i}, this.value)'  ${bg_color} ${is_diabled}/></td
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
        <p style="
        vertical-align: middle;
        width: 100%; 
        border-radius: 4px;
        border: 1px solid #DCEDC8;
        background-color: #F1F8E9;
        "><img src=${location.protocol}//${location.host}/img/TransferComplete.svg  alt="Save successful"/>
        Save successful on ${formatDate(new Date())}
        </p>`);
        render();
    }
    else
    {
        message_history.push(`
        <p style="
        vertical-align: middle;
        width: 100%; 
        border-radius: 4px;
        border: 1px solid #FFC2C2;
        background-color: #FFE7E7;
        "><img src=${location.protocol}//${location.host}/img/TransferError.svg  alt="Error when saving." />
        Current selections could not be saved. Please contact your system administrator for assistance.
        </p>
        `);
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
        <p style="
        vertical-align: middle;
        width: 100%; 
        border-radius: 4px;
        border: 1px solid #FFC2C2;
        background-color: #FFE7E7;
        "><img src=${location.protocol}//${location.host}/img/TransferError.svg  alt="Error when saving."/>
        Current selections could not be saved. Please contact your system administrator for assistance.
        </p>`);
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