var g_lat = null;
var g_lon  = null;
var g_year = null;
var g_record_id = null;
var output_element = null;
var report_log = [];
var g_is_running = false;
var g_countdown_timer = null;

const CVS_MAX_ATTEMPTS = window.CVS_MAX_ATTEMPTS ?? 10;
const CVS_RETRY_DELAY_SECONDS = window.CVS_RETRY_DELAY_SECONDS ?? 60;

// CVS_FORCE_MODE — append ?cvs_force_mode=N (or &cvs_force_mode=N) to the CVS popup URL to force a specific scenario.
// Useful for UX testing and demo. Survives F5 since it lives in the URL.
//   1 = Error              — stops immediately with error message
//   2 = Retrying           — returns unavailable on every attempt; shows countdown
//                            (tip: also add &cvs_retry_delay_seconds=5 to speed up)
//   3 = Retry limit exceeded — same response as 2; combine with &cvs_max_attempts=2 to reach limit quickly
//   4 = Success            — returns file ready; shows download button (no real PDF is produced)
const CVS_FORCE_MODE = new URLSearchParams(window.location.search).get('cvs_force_mode');

const bc = new BroadcastChannel('cvs_channel');
bc.onmessage = (message_data) => {

    g_lat = message_data.data.lat;
    g_lon  = message_data.data.lon;
    g_year = message_data.data.year;
    g_record_id = message_data.data.record_id;

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

    await run_cvs_report_polling(header, el, spinner, report_output_element, true);
}

window.onload = main;

async function run_cvs_report_polling(header, el, spinner, report_output_element, reset_log)
{
    if (g_is_running)
    {
        return;
    }

    g_is_running = true;

    if (reset_log)
    {
        report_log = [];
    }

    try
    {
        for (let attempt = 1; attempt <= CVS_MAX_ATTEMPTS; attempt++)
        {
            show_active_request(header, el, attempt);

            report_log.push(`calling community vital signs service @ ${new Date()}`);
            render_report_log(report_output_element);

            const response = await get_cvs_api_dashboard_info(g_lat, g_lon, g_year, g_record_id);

            const file_status = normalize_file_status(response.file_status);

            report_log.push(`file_status: ${file_status} @ ${new Date()}`);
            render_report_log(report_output_element);

            if (response.is_valid_address != undefined && response.is_valid_address == false)
            {
                header.innerHTML = "Community Vital Signs PDF cannot be generated.";
                el.innerHTML = "Decedent Resident Address is not available.<br/>Please contact your jurisdiction abstractor to resolve this issue.";
                spinner.innerHTML = render_close_button_html();
                post_cvs_status("error");
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
                return;
            }
            else if (response.is_valid_year != undefined && response.is_valid_year == false)
            {
                header.innerHTML = "Community Vital Signs PDF cannot be generated.";
                el.innerHTML = "Decedent year of death is out of range.<br/>Decedent year of death is outside of the range that is provided by the Erase MM CVS API.";
                spinner.innerHTML = render_close_button_html();
                post_cvs_status("error");
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
                return;
            }
            else if (file_status == "file ready")
            {
                header.innerHTML = "<span style='color:007700;'>PDF Generated.</span>";
                el.innerHTML = "Press the download button to save the report.";
                spinner.innerHTML = `${render_close_button_html()}&nbsp;${render_download_button_html()}`;
                post_cvs_status("file_ready");
                window.setTimeout(()=> { const download_button = document.getElementById("download_button"); download_button.focus(); }, 0);
                return;
            }
            else if (file_status == "error")
            {
                header.innerHTML = "Error: Community Vital Sign PDF";
                el.innerHTML = "PDF cannot be generated.<br/><br/><span style='color:FF0000;'>External Community Vital Signs Server cannot generate PDF for this location and year.</span> <br/><br/> Please try again later.";
                spinner.innerHTML = render_close_button_html();
                post_cvs_status("error");
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
                return;
            }
            else if (file_status == "generating" || file_status == "unavailable" || file_status == "")
            {
                if (attempt < CVS_MAX_ATTEMPTS)
                {
                    await wait_for_next_attempt(header, el, attempt + 1);
                }
            }
        }

        // Max retries exhausted without a terminal result
        header.innerHTML = "Community Vital Signs PDF";
        el.innerHTML = "PDF cannot be generated.<br/><br/><span style='color:FF0000;'>Maximum retry attempts reached.</span> <br/><br/> Please try again later.";
        spinner.innerHTML = `${render_close_button_html()}&nbsp;${render_try_again_button_html()}`;
        post_cvs_status("max_retries");
        window.setTimeout(()=> { const try_again_button = document.getElementById("try_again_button"); try_again_button.focus(); }, 0);
    }
    finally
    {
        g_is_running = false;
        if (g_countdown_timer !== null)
        {
            window.clearInterval(g_countdown_timer);
            g_countdown_timer = null;
        }
    }
}

function wait_for_next_attempt(header, el, next_attempt)
{
    return new Promise((resolve) =>
    {
        let remaining_seconds = CVS_RETRY_DELAY_SECONDS;
        update_countdown_message(header, el, remaining_seconds, next_attempt);
        g_countdown_timer = window.setInterval(() =>
        {
            remaining_seconds--;
            update_countdown_message(header, el, remaining_seconds, next_attempt);
            if (remaining_seconds <= 0)
            {
                window.clearInterval(g_countdown_timer);
                g_countdown_timer = null;
                resolve();
            }
        }, 1000);
    });
}

function update_countdown_message(header, el, remaining_seconds, next_attempt)
{
    header.innerHTML = "Please wait.";
    el.innerHTML = `The Community Vital Signs report is being prepared. Next check in ${remaining_seconds} seconds. Attempt ${next_attempt} of ${CVS_MAX_ATTEMPTS} will run automatically.`;
}

function show_active_request(header, el, attempt)
{
    header.innerHTML = "Please wait.";
    el.innerHTML = `Generating PDF... Checking Community Vital Signs service, attempt ${attempt} of ${CVS_MAX_ATTEMPTS}.`;
}

function normalize_file_status(file_status)
{
    if (file_status == null || file_status == undefined)
    {
        return "";
    }
    return String(file_status).trim().toLowerCase();
}

function render_report_log(report_output_element)
{
    report_output_element.innerHTML = `<ul><li>${report_log.join("</li><li>")}</li></ul>`;
}

function render_try_again_button_html()
{
    return `<input id="try_again_button" type="button" alt="Try again." value="Try again" onclick="try_again_button_click();" />`;
}

function render_disabled_try_again_button_html()
{
    return `<input id="try_again_button" type="button" alt="Try again." value="Try again" disabled />`;
}

async function try_again_button_click()
{
    const report_output_element = document.getElementById("report_output_id");
    const spinner = document.getElementById("spinner-id");
    const header = document.getElementById("header");
    const el = document.getElementById("output");

    spinner.innerHTML = `${render_close_button_html()}&nbsp;${render_disabled_try_again_button_html()}`;
    await run_cvs_report_polling(header, el, spinner, report_output_element, false);
}

function post_cvs_status(status)
{
    // BroadcastChannel post — schema consumed by Story 10.4
    bc.postMessage({ type: "cvs_status", status: status });
}

async function get_cvs_api_dashboard_info 
(
    lat,
    lon, 
    year,
    id,
)
{            
    if (CVS_FORCE_MODE != null)
    {
        const forced = { 1: "error", 2: "unavailable", 3: "unavailable", 4: "file ready" };
        const file_status = forced[CVS_FORCE_MODE];
        if (file_status !== undefined)
        {
            return { file_status };
        }
    }

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
        );

        if (!response.ok)
        {
            const file_status = (response.status >= 500 || response.status == 408 || response.status == 429)
                ? "unavailable"
                : "error";
            return { file_status: file_status, status: response.status, detail: `HTTP ${response.status}` };
        }

        return response.json();
    }
    catch(ex)
    {
        return { file_status: "unavailable", detail: ex.message || String(ex) };
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