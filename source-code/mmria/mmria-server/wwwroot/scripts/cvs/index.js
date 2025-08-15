var g_lat = null;
var g_lon  = null;
var g_year = null;
var g_record_id = null;
var output_element = null;
var report_log = [];

const bc = new BroadcastChannel('cvs_channel');
bc.onmessage = (message_data) => {

g_lat = message_data.data.lat;
g_lon  = message_data.data.lon;
g_year = message_data.data.year;
g_record_id = message_data.data.record_id

    pre_render(message_data.data);
}



async function main()
{
    window.setTimeout(async ()=> await main_continue(), 5000);
}
async function main_continue()
{
    g_lat = document.getElementById("lat").value;
    g_lon = document.getElementById("lon").value;
    g_year = document.getElementById("year").value;
    g_record_id = document.getElementById("id").value;

    const report_output_element = document.getElementById("report_output_id");
    const spinner = document.getElementById("spinner-id");
    const header = document.getElementById("header");
    const el = document.getElementById("output");

    let is_finished = false;

    while(! is_finished)
    {
        report_log.push(`calling community vital signs service @ ${new Date()}`)
        report_output_element.innerHTML = `<hr/><br/><pre>${report_log.join("\n\n")}</pre>`;
        const response = await get_cvs_api_dashboard_info
        (
            g_lat,
            g_lon,
            g_year,
            g_record_id
        )


        if(response.file_status != null)
        {
            report_log.push(`file_status: ${response.file_status} @ ${new Date()}`);
            if
            (
                response.is_valid_address != undefined &&
                response.is_valid_address == false
            )
            {
                header.innerHTML = "Community Vital Signs PDF cannot be generated.";
                el.innerHTML = "Decedent Resident Address is not available.<br/>Please contact your jurisdiction abstractor to resolve this issue.";
                spinner.innerHTML = render_close_button_html();
                is_finished = true;
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
            }
            else if
            (
                response.is_valid_year != undefined &&
                response.is_valid_year == false
            )
            {
                header.innerHTML = "Community Vital Signs PDF cannot be generated.";
                el.innerHTML = "Decedent year of death is out of range.<br/>Decedent year of death is outside of the range that is provided by the Erase MM CVS API.";
                spinner.innerHTML = render_close_button_html();
                is_finished = true;
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
            }
            else if(response.file_status == "file ready")
            {
                header.innerHTML = "<span style='color:007700;'>PDF Generated.</span>";
                el.innerHTML = "Press the download button to save the report.";
                spinner.innerHTML = `${render_close_button_html()}&nbsp;${render_download_button_html()}`;

                is_finished = true;
                window.setTimeout(()=> { const download_button = document.getElementById("download_button"); download_button.focus(); }, 0);
            }
            else if(response.file_status == "error")
            {
                //console.log(response);

                header.innerHTML = "Error: Community Vital Sign PDF";
                el.innerHTML = "PDF cannot be generated.<br/><br/><span style='color:FF0000;'>External Community Vital Signs Server is unavailable.</span> <br/><br/> Please wait 60 seconds, then click the refresh button on the browser toolbar. If still unsuccessful, please try again later.";
                spinner.innerHTML = render_close_button_html();
                is_finished = true;
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
                //$mmria.info_dialog_show("Community Vital Sign PDF","An error occured  when calling the Community Vital Signs. Please try again later.");
            }
            else if(response.file_status == "generating")
            {
                //console.log(response);
            }
            else
            {
                header.innerHTML = "Error: Community Vital Sign PDF";
                el.innerHTML = "PDF cannot be generated.<br/><span style='color:FF0000;'>External Community Vital Signs Server is unavailable.</span> Please try again later.";
                spinner.innerHTML = render_close_button_html();
                is_finished = true;
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
            }
        }
        else
        {
            header.innerHTML = "Error: Community Vital Sign PDF";
            el.innerHTML = "PDF cannot be generated.<br/><span style='color:FF0000;'>External Community Vital Signs Server is unavailable.</span> Please try again later.";

            spinner.innerHTML = render_close_button_html();
            report_log.push(`CVS reponse Status Code: ${response.status} @ ${new Date()} ${response.detail}`);
            is_finished = true;
            window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
            
        }

        report_output_element.innerHTML = `<ul><li>${report_log.join("</li><li>")}</li></ul>`;
    }
}

window.onload = main;

async function get_cvs_api_dashboard_info 
(
    lat,
    lon, 
    year,
    id,
)
{            
    var base_url = `${location.protocol}//${location.host}/api/cvsAPI`

    try
    {

    
        const response = await fetch
        (
            base_url,
            {
                method: "POST",
                headers:
                {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json, application/xml, text/plain, text/html, *.*'
                },
                body: JSON.stringify({
                    action: "dashboard",
                    lat: lat,
                    lon: lon, 
                    year: year,
                    id: id

                }),
            }
        )

        //console.log(response);

        return response.json();
    }
    catch(ex)
    {
        return { statusCode: 500, body: ex };
    }
}


async function get_file(p_id)
{

    var base_url = `${location.protocol}//${location.host}/api/cvsAPI/${p_id}`

    try
    {

    
        const response = await fetch
        (
            base_url,
            {
                method: "GET",
                headers:
                {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json, application/xml, text/plain, text/html, *.*'
                },
            }
        )

        //console.log(response);

        return response;
    }
    catch(ex)
    {
        return { statusCode: 500, body: ex };
    }
}

async function pre_render(p_data)
{
    const output_element = document.getElementById('output');
    const report_output_element = document.getElementById('report_output_id');
}


function render_close_button_html()
{
    return `<input id="close_button" type="button" alt="Close this tab." value="Close this tab" onclick="window.close();" />`
}

function render_download_button_html()
{
    var pdf_url = `${location.protocol}//${location.host}/api/cvsAPI/${g_record_id}`;
    return `
    <a id="a_download" href="${pdf_url}">
    <input id="download_button" type="button" alt="Download ${g_record_id} PDF" value="Download ${g_record_id} PDF" onclick="download_button_click()" />
    `;
}


function download_button_click()
{
    const el = document.getElementById("a_download");
    el.click();

}