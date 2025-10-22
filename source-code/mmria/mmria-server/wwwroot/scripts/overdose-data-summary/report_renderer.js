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

    g_report_index = index;

    const post_html = [
        `const all = document.getElementsByClassName('spinner-container')
        for(let i = 0; i < all.length; i++)
        {
            let item = all[i];
            item.remove();
        }`
        ];
    switch(index)
    {
        case 1:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render1(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 2:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render2(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 3:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render3(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 4:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render4(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 5:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render5(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 6:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render6(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 7:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render7(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 8:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render8(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 9:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render9(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;    
        case 10:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render10(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;                
        case 11:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render11(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 12:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render12(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        case 13:
            show_loading_modal();
            document.getElementById('output').innerHTML = await render13(post_html);
            eval(post_html.join(""));
            close_loading_modal();
            break;
        // case 1:
        //     document.getElementById('output').innerHTML = await render1(post_html);
        //     eval(post_html.join(""));
        //     break;
        case -1:
        default:
            g_reportType = "Summary";
            g_report_index = 0;
            document.getElementById('output').innerHTML = render0();
    }

    const summary_type_element = document.getElementById("summary-report")
    const detail_type_element = document.getElementById("detail-report")

    if(g_reportType == "Summary")
    {
        summary_type_element.checked = true;
    }
    else
    {
        detail_type_element.checked = true;
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
    reporting_state_element.innerHTML = `${g_filter.reporting_state}`;

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

    let current_page_html = `
    <input class="form-check-input big-radio" type="radio" id="detail-report" name="report-type" value="Detail" onclick="updateReportType(event)">
    <label style="margin-left: .2rem !important;" for="detail-report" class="mb-0 font-weight-normal">Current Report Page</label>
    `;

if(g_report_index < 1)
{
    current_page_html = '';
}

    return `
    <div id="filter-pdf-control" class="d-flex">
        <div class="col-md-9 p-0">
            <div id="filter-summary" class="card-container-dark dark-card-border col-md-12" style="height: 275px;">
                <div class="header">
                    Filters
                </div>
                <div class="card-content p-2 d-flex">
                    ${summary_filter_renderer()}
                </div>
            </div>
        </div>
        <div class="col-md-3 p-0 pl-2">
            <div style="height: 275px;" id="pdf-control" class="card-container-dark dark-card-border col-md-12">
                <div class="header">
                    Save and Print
                </div>
                <div style="height: 230px;" class="card-content d-flex flex-column p-2">
                    <span class="font-weight-bold">Select Item to Export</span>
                    <div class="form-check mt-2 ml-4">
                        <input class="form-check-input big-radio" type="radio" id="summary-report" name="report-type" value="Summary" onclick="updateReportType(event)" checked>
                        <label style="margin-left: .2rem !important;" for="summary-report" class="ml-0 mb-0 font-weight-normal">Full Report</label>
                    </div>
                    <div class="form-check mt-2 ml-4">
                        ${current_page_html}
                    </div>
                    <span class="align-self-end mt-auto">
                        <button class="btn primary-button" onclick="view_pdf_click()">View PDF</button>
                        <button class="btn primary-button" onclick="print_pdf_click()">Save PDF</button>
                    </span>
                </div>
            </div>
        </div>
    </div>
    <dialog id="filter-dialog" style="top:65%;width:65%" class="p-0 set-radius"></dialog>
    `;
}


const bc = new BroadcastChannel('overdose_pdf_channel');
bc.onmessage = (eventMessage) => {
  
}

function updateReportType(e)
{
	g_reportType = e.target.value;
}

function view_pdf_click()
{
	var url =  'overdose-data-summary/pdf';
    window.open(url, '_overdose_data_summary_report');

    const message_data = {
        reportType: g_reportType,
        report_index: g_report_index,
        view_or_print: "view",
        g_filter: g_filter
    }

    window.setTimeout(()=> bc.postMessage(message_data), 2000);
}

function print_pdf_click()
{
	var url =  'overdose-data-summary/pdf';
    window.open(url, '_overdose_data_summary_report');

    const message_data = {
        reportType: g_reportType,
        report_index: g_report_index,
        view_or_print: "print",
        g_filter: g_filter
    }

    window.setTimeout(()=> bc.postMessage(message_data), 2000);
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

function summary_filter_renderer()
{
    let result = '';
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

    result = `
        <div class="col-md-7 pl-0 pr-0">
            <div class="ml-2">
                <div>
                    <span class="font-weight-bold ml-0">Pregnancy-Relatedness:</span>
                    <div class="form-check mt-3 mb-2 ml-4">
                        <input type="checkbox" id="Pregnancy-Relatedness-1" class="form-check-input big-checkbox mt-0" onchange="pregnancy_relatedness_1_change(this)" ${is_checked_1_html}/>
                        <label for="Pregnancy-Relatedness-1" class="form-check-label m-0 pb-0">${relatedness_map.get(1)}</label>
                    </div>
                    <div class="form-check mb-2 ml-4">
                        <input type="checkbox" id="Pregnancy-Relatedness-0" class="form-check-input big-checkbox mt-0" onchange="pregnancy_relatedness_0_change(this)" ${is_checked_0_html}/>
                        <label for="Pregnancy-Relatedness-0" class="form-check-label m-0 pb-0">${relatedness_map.get(0)}</label>
                    </div>
                    <div class="form-check mb-2 ml-4">
                        <input type="checkbox" id="Pregnancy-Relatedness-2" class="form-check-input big-checkbox mt-0" onchange="pregnancy_relatedness_2_change(this)" ${is_checked_2_html}/>
                        <label for="Pregnancy-Relatedness-2" class="form-check-label m-0 pb-0">${relatedness_map.get(2)}</label>
                    </div>
                    <div class="form-check mb-2 ml-4">
                        <input type="checkbox" id="Pregnancy-Relatedness-99" class="form-check-input big-checkbox mt-0" onchange="pregnancy_relatedness_99_change(this)" ${is_checked_99_html}/>
                        <label for="Pregnancy-Relatedness-99" class="form-check-label m-0 pb-0">${relatedness_map.get(99)}</label>
                    </div>
                </div>
            </div>
        </div>
        <div style="border-left: 1px solid #cfcfcf;"></div>
        <div class="col-md-5 pr-0">
            <div>
                <span class="font-weight-bold">Review Dates:</span>
                <div class="d-flex mt-2">
                    <div class="horizontal-control col-md-6 pl-0 pr-2">
                        <input aria-label="begin review date" id="review_begin_date" type="date" class="form-control" value="${ControlFormatDate(g_filter.date_of_review.begin)}" max="${ControlFormatDate(g_filter.date_of_review.end)}" onblur="review_begin_date_change(this.value)" />
                    </div>
                    <span class="mt-1">-</span>
                    <div class="horizontal-control col-md-6 pl-2">
                        <input aria-label="end review date" id="review_end_date" type="date" class="form-control" value="${ControlFormatDate(g_filter.date_of_review.end)}" min="${ControlFormatDate(g_filter.date_of_review.begin)}" onblur="review_end_date_change(this.value)" />
                    </div>
                </div>
            </div>
            <div>
                <span class="font-weight-bold">Dates of Death:</span>
                <div class="d-flex mt-2">
                    <div class="horizontal-control col-md-6 pl-0 pr-2">
                        <input aria-label="begin death date" id="death_begin_date" type="date" class="form-control" value="${ControlFormatDate(g_filter.date_of_death.begin)}" max="${ControlFormatDate(g_filter.date_of_death.end)}" onblur="death_begin_date_change(this.value)" />
                    </div>
                    <span class="mt-1">-</span>
                    <div class="horizontal-control col-md-6 pl-2">
                        <input aria-label="end death date" id="death_end_date" type="date" class="form-control" value="${ControlFormatDate(g_filter.date_of_death.end)}" min="${ControlFormatDate(g_filter.date_of_death.begin)}" onblur="death_end_date_change(this.value)" />
                    </div>
                </div>
            </div>
            <div class="d-flex justify-content-end mr-2">
                <button id="close_filter" class="btn primary-button" onclick="close_filter()">Apply Filters</button>
            </div>
        </div>
    `;
    return result;

}

function close_filter()
{
    const el = document.getElementById("filter-dialog");
    //render_filter_summary();
    render();
}

function render_loading_modal()
{
    const el = document.getElementById("loading-modal");
    el.close();   

    el.innerHTML = ``;
}

function show_loading_modal()
{
    const el = document.getElementById("loading-modal");
    el.close();   

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

function render_chart_card_container(p_chart_title)
{
    return `
        <div class="card-container-light" style="width:90%;">
            <div class="header">
                <span class="h5 m-1">${p_chart_title}</span>
            </div>
            <div class="card-content">
                <div id="chart"></div>
            </div>
        </div>
    `;
}

