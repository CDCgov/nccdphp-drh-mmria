async function render(p_index)
{
    let index = -1;
    if(p_index == null)
    {
        const url = window.location.href;

        const url_array = url.split('#');

        if(url_array.length > 1)
        {
            index = parseInt(url_array[1]);
        }
    }
    else
    {
        index = p_index;
    }

    const post_html = [];
    switch(index)
    {
        case 1:
            document.getElementById('output').innerHTML = await render1(post_html);
            eval(post_html.join(""));
            break;
        case 2:
            document.getElementById('output').innerHTML = await render2(post_html);
            eval(post_html.join(""));
            break;
        case 3:
            document.getElementById('output').innerHTML = await render3(post_html);
            eval(post_html.join(""));
            break;
        case 4:
            document.getElementById('output').innerHTML = await render4(post_html);
            eval(post_html.join(""));
            break;
        case 5:
            document.getElementById('output').innerHTML = await render5(post_html);
            eval(post_html.join(""));
            break;
        case 6:
            document.getElementById('output').innerHTML = await render6(post_html);
            eval(post_html.join(""));
            break;
        case 7:
            document.getElementById('output').innerHTML = await render7(post_html);
            eval(post_html.join(""));
            break;
        case 8:
            document.getElementById('output').innerHTML = await render8(post_html);
            eval(post_html.join(""));
            break;
        case 9:
            document.getElementById('output').innerHTML = await render9(post_html);
            eval(post_html.join(""));
            break;
    
        case 10:
            document.getElementById('output').innerHTML = await render10(post_html);
            eval(post_html.join(""));
            break;
                    
        case 11:
            document.getElementById('output').innerHTML = await render11(post_html);
            eval(post_html.join(""));
            break;

        case 12:
            document.getElementById('output').innerHTML = await render12(post_html);
            eval(post_html.join(""));
            break;
        case 1:
            document.getElementById('output').innerHTML = await render1(post_html);
            eval(post_html.join(""));
            break;
        case -1:
        default:
            document.getElementById('output').innerHTML = render0();
    }

    return;
}


function pad_number(n) 
{
    n = n + '';
    return n.length >= 2 ? n : new Array(2 - n.length + 1).join("0") + n;
}

function formatDate(p_value)
{
    const result= pad_number(p_value.getMonth() + 1) + '/' + pad_number(p_value.getDate()) + '/' +  p_value.getFullYear();

    return result;
}

function ControlFormatDate(p_value)
{
    const result= p_value.getFullYear() + '-' + pad_number(p_value.getMonth() + 1) + '-' + pad_number(p_value.getDate());

    return result;
}




function render_header()
{
    const reporting_state_element = document.getElementById("reporting_state")
    reporting_state_element.innerHTML = `<strong>Reporting State: </strong> ${g_filter.reporting_state}`;

    const current_datetime = new Date();

    const report_datetime_element = document.getElementById("report_datetime")
    report_datetime_element.innerHTML = `${current_datetime.toDateString().replace(/(\d{2})/, "$1,")} ${current_datetime.toLocaleTimeString()}`;

    let pregnancy_relatedness_html = "All";
    if(g_filter.pregnancy_relatedness.length == 4)
    {

    }
    else
    {
        const html = [];
        html.push("<ul>");

        relatedness_map.forEach
        (
            (value, key) =>
            {

                if(g_filter.pregnancy_relatedness.indexOf(key) > -1)
                {
                    html.push("<li>");
                    html.push(value);
                    html.push("</li>");
                }
            }
        );
        
        html.push("</ul>");
        pregnancy_relatedness_html = html.join("");
    }

    return `
<div 
    id="filter-summary"
    style="width:415px;padding: 10px;border: 2px solid #000;border-radius: 15px;-moz-border-radius: 15px;"
>
    <p><strong>Pregnancy-Relatedness:</strong> ${pregnancy_relatedness_html}  <span style="float:right"><button class="btn btn-primary" onclick="show_filter_dialog()">Filter</button></span></p>
    <p><strong>Review Dates:</strong> ${formatDate(g_filter.date_of_review.begin)} - ${formatDate(g_filter.date_of_review.end)}</p>
    <p><strong>Dates of Death:</strong> ${formatDate(g_filter.date_of_death.begin)} - ${formatDate(g_filter.date_of_death.end)}</p>
</div>

<dialog  id="filter-dialog" style="top:65%;width:65%" class="p-0 set-radius">
</dialog>

    `;
}

function render_filter_summary()
{
    let pregnancy_relatedness_html = "All";
    if(g_filter.pregnancy_relatedness.length == 4)
    {

    }
    else
    {
        const html = [];
        html.push("<ul>");
        g_filter.pregnancy_relatedness.forEach
        (
            (value) =>
            {
                const item = relatedness_map.get(value);
                html.push("<li>");
                html.push(item);
                html.push("</li>");
            }
        );
        
        html.push("</ul>");
        pregnancy_relatedness_html = html.join("");
    }


    let el  = document.getElementById("filter-summary");
    
    el.innerHTML = `
    <p><strong>Pregnancy-Relatedness:</strong> ${pregnancy_relatedness_html} <span style="float:right"><button class="btn btn-secondary" onclick="show_filter_dialog()">Filter</button></span></p>
    <p><strong>Review Dates:</strong> ${formatDate(g_filter.date_of_review.begin)} - ${formatDate(g_filter.date_of_review.end)}</p>
    <p><strong>Dates of Death:</strong> ${formatDate(g_filter.date_of_death.begin)} - ${formatDate(g_filter.date_of_death.begin)}</p>
    `;
}

function show_filter_dialog()
{

    let all_is_checked_html = "";
    let is_checked_1_html = "";
    let is_checked_0_html = "";
    let is_checked_2_html = "";
    let is_checked_99_html = "";


    if(g_filter.pregnancy_relatedness.length == 4)
    {
        all_is_checked_html = "checked";
    }

    if(g_filter.pregnancy_relatedness.indexOf(1) > -1)
    {
        is_checked_1_html = "checked";
    }

    if(g_filter.pregnancy_relatedness.indexOf(0) > -1)
    {
        is_checked_0_html = "checked";
    }

    if(g_filter.pregnancy_relatedness.indexOf(2) > -1)
    {
        is_checked_2_html = "checked";
    }

    if(g_filter.pregnancy_relatedness.indexOf(99) > -1)
    {
        is_checked_99_html = "checked";
    }

    let el  = document.getElementById("filter-dialog");
    
    el.innerHTML = `
 <div class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
        <span id="ui-id-1" class="ui-dialog-title">Filter</span>
        <!--label for="top_corner_close">Close</label-->
        <button id="top_corner_close" type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="close_filter()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
    </div>
    <div style="margin:15px;width:580px;">
        <p>
        <strong>Pregnancy-Relatedness:</strong>
        <ul>
            <!--li>
                <input type="checkbox" id="Pregnancy-Relatedness-All" onchange="pregnancy_relatedness_all_change(this)" ${all_is_checked_html}/> <label for="Pregnancy-Relatedness-All">All</label>
            </li-->
            <li>
                <input type="checkbox"  id="Pregnancy-Relatedness-1" onchange="pregnancy_relatedness_1_change(this)" ${is_checked_1_html}/> <label for="Pregnancy-Relatedness-1">${relatedness_map.get(1)}</label>
            </li>
            <li>
                <input type="checkbox"  id="Pregnancy-Relatedness-0" onchange="pregnancy_relatedness_0_change(this)" ${is_checked_0_html} /> <label for="Pregnancy-Relatedness-0">${relatedness_map.get(0)}</label>
            </li>
            <li>
                <input type="checkbox" id="Pregnancy-Relatedness-2" onchange="pregnancy_relatedness_2_change(this)" ${is_checked_2_html} /> <label for="Pregnancy-Relatedness-2">${relatedness_map.get(2)}</label>
            </li>
            <li>
                <input type="checkbox" id="Pregnancy-Relatedness-99" onchange="pregnancy_relatedness_99_change(this)" ${is_checked_99_html} /> <label for="Pregnancy-Relatedness-99">${relatedness_map.get(99)}</label>
            </li>
        </ul>    
        </p>
        <p>
            
            <table>
                <tr>
                    <th><strong>Review Dates:</strong></th>
                    <th>&nbsp;</th>
                    <th>&nbsp;</th>
                    <th>&nbsp;</th>
                    <th>&nbsp;</th>
                </tr>
                <tr>
                    <td>
                        <label for="review_begin_date">Begin</label>
                        <input id="review_begin_date" type="date" value="${ControlFormatDate(g_filter.date_of_review.begin)}" max="${ControlFormatDate(g_filter.date_of_review.end)}" onblur="review_begin_date_change(this.value)" />
                    </td>
                        <td>&nbsp;</td>
                        <td>&nbsp;</td>
                        <td>&nbsp;</td>
                    <td>
                        <label for="review_end_date">End</label>
                        <input  id="review_end_date" type="date" value="${ControlFormatDate(g_filter.date_of_review.end)}"  min="${ControlFormatDate(g_filter.date_of_review.begin)}" onblur="review_end_date_change(this.value)" />
                    </td>
                </tr>
            </table>
        </p>
        <p>
            
            <table>
                <tr>
                    <th><strong>Dates of Death:</strong></th>
                    <th>&nbsp;</th>
                    <th>&nbsp;</th>
                    <th>&nbsp;</th>
                    <th>&nbsp;</th>
                </tr>
                <tr>
                    <td>
                        <label for="death_begin_date">Begin</label>
                        <input id="death_begin_date" type="date" value="${ControlFormatDate(g_filter.date_of_death.begin)}" max="${ControlFormatDate(g_filter.date_of_death.end)}" onblur="death_begin_date_change(this.value)" />
                    </td>
                    <td>&nbsp;</td>
                    <td>&nbsp;</td>
                    <td>&nbsp;</td>
                    <td>
                        <label for="death_end_date">End</label>
                        <input  id="death_end_date" type="date" value="${ControlFormatDate(g_filter.date_of_death.end)}"  min="${ControlFormatDate(g_filter.date_of_death.begin)}" onblur="death_end_date_change(this.value)" />
                    </td>
                </tr>
            </table>
        </p>
    
        <p align="center">
        <button class="btn btn-secondary" onclick="close_filter()">Close</button>
        <!--button class="btn " onclick="close_filter()">Cancel</button-->
        </p>
    </div>
`;

    el.showModal();

}

function close_filter()
{
    let el = document.getElementById("filter-dialog");
    el.close();
    
    //render_filter_summary();
    render();
}



