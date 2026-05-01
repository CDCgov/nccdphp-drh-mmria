var g_lat = null;
var g_lon  = null;
var g_year = null;
var g_record_id = null;
var output_element = null;
var report_log = [];
var g_is_running = false;
var g_countdown_timer = null;

const CVS_RETRY_DELAY_SECONDS = 60;
const CVS_MAX_ATTEMPTS = 5;

const bc = typeof BroadcastChannel === "undefined" ?
    { postMessage: () => {} } :
    new BroadcastChannel('cvs_channel');

if (typeof BroadcastChannel !== "undefined") {
bc.onmessage = (message_data) => {
    const message = message_data.data || {};
    if (message.type === "cvs-report-status") {
        return;
    }

    g_lat = message.lat;
    g_lon = message.lon;
    g_year = message.year;
    g_record_id = message.record_id

    pre_render(message);
}
}



async function main()
{
    await main_continue();
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

async function run_cvs_report_polling(header, el, spinner, report_output_element, reset_log)
{
    if (g_is_running) {
        return;
    }

    g_is_running = true;
    if (reset_log) {
        report_log = [];
    }

    post_cvs_status("started");
    spinner.innerHTML = render_close_button_html() + "&nbsp;" + render_disabled_try_again_button_html();

    try
    {
        for (let attempt = 1; attempt <= CVS_MAX_ATTEMPTS; attempt++)
        {
            show_active_request(header, el, attempt);
            report_log.push(`calling community vital signs service, attempt ${attempt} of ${CVS_MAX_ATTEMPTS} @ ${new Date()}`);
            render_report_log(report_output_element);

            const response = await get_cvs_api_dashboard_info
            (
                g_lat,
                g_lon,
                g_year,
                g_record_id
            );

            const file_status = normalize_file_status(response.file_status);
            report_log.push(`file_status: ${file_status || "unknown"} @ ${new Date()}`);
            render_report_log(report_output_element);

            if
            (
                response.is_valid_address != undefined &&
                response.is_valid_address == false
            )
            {
                header.innerHTML = "Community Vital Signs PDF cannot be generated.";
                el.innerHTML = "Decedent Resident Address is not available.<br/>Please contact your jurisdiction abstractor to resolve this issue.";
                spinner.innerHTML = render_close_button_html();
                post_cvs_status("validation_error");
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
                return;
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
                post_cvs_status("validation_error");
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
                return;
            }

            if(file_status == "validation error")
            {
                header.innerHTML = "Community Vital Signs PDF cannot be generated.";
                el.innerHTML = "The case is missing required Community Vital Signs report inputs. Please verify the address and year of death, then try again.";
                spinner.innerHTML = render_close_button_html();
                post_cvs_status("validation_error");
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
                return;
            }

            if(file_status == "file ready")
            {
                header.innerHTML = "<span style='color:#007700;'>PDF Generated.</span>";
                el.innerHTML = "Press the download button to save the report.";
                spinner.innerHTML = `${render_close_button_html()}&nbsp;${render_download_button_html()}`;

                post_cvs_status("ready");
                window.setTimeout(()=> { const download_button = document.getElementById("download_button"); download_button.focus(); }, 0);
                return;
            }

            if(file_status == "generating" || file_status == "unavailable")
            {
                if (attempt < CVS_MAX_ATTEMPTS) {
                    const status_text = file_status == "unavailable" ?
                        "The Community Vital Signs service is not ready yet." :
                        "The Community Vital Signs PDF is still being generated.";
                    report_log.push(`${status_text} Waiting ${CVS_RETRY_DELAY_SECONDS} seconds before the next check.`);
                    render_report_log(report_output_element);
                    await wait_for_next_attempt(header, el, attempt + 1);
                    continue;
                }

                header.innerHTML = "Error: Community Vital Sign PDF";
                el.innerHTML = "PDF cannot be generated yet.<br/><br/><span style='color:#990000;'>The external Community Vital Signs service is still preparing the report.</span><br/><br/>Use Try again to check again without refreshing the browser.";
                spinner.innerHTML = `${render_close_button_html()}&nbsp;${render_try_again_button_html()}`;
                post_cvs_status("max_retries");
                window.setTimeout(()=> { const try_again_button = document.getElementById("try_again_button"); try_again_button.focus(); }, 0);
                return;
            }

            if(file_status == "error")
            {
                header.innerHTML = "Error: Community Vital Sign PDF";
                el.innerHTML = "PDF cannot be generated.<br/><span style='color:#FF0000;'>External Community Vital Signs Server is unavailable.</span> Please try again later.";
                spinner.innerHTML = render_close_button_html();
                post_cvs_status("failed");
                window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
                return;
            }

            header.innerHTML = "Error: Community Vital Sign PDF";
            el.innerHTML = "PDF cannot be generated.<br/><span style='color:#FF0000;'>External Community Vital Signs Server is unavailable.</span> Please try again later.";
            spinner.innerHTML = render_close_button_html();
            report_log.push(`CVS response Status Code: ${response.status} @ ${new Date()} ${response.detail || ""}`);
            render_report_log(report_output_element);
            post_cvs_status("failed");
            window.setTimeout(()=> { const close_button = document.getElementById("close_button"); close_button.focus(); }, 0);
            return;
        }
    }
    finally
    {
        g_is_running = false;
        if (g_countdown_timer != null) {
            window.clearInterval(g_countdown_timer);
            g_countdown_timer = null;
        }
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

        if (!response.ok) {
            let detail = "";
            try {
                const error_text = await response.text();
                const error_response = error_text ? JSON.parse(error_text) : {};
                detail = error_response.detail || error_response.title || "";
            }
            catch(ex) {
                detail = response.statusText || "";
            }

            return {
                file_status: response.status >= 500 || response.status == 408 || response.status == 429 ? "unavailable" : "error",
                status: response.status,
                detail: detail
            };
        }

        return response.json();
    }
    catch(ex)
    {
        return { file_status: "unavailable", statusCode: 500, body: ex, detail: ex.message || String(ex) };
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

function show_active_request(header, el, attempt)
{
    header.innerHTML = "Please wait.";
    el.innerHTML = `Generating PDF... Checking Community Vital Signs service, attempt ${attempt} of ${CVS_MAX_ATTEMPTS}.`;
}

function wait_for_next_attempt(header, el, next_attempt)
{
    let remaining_seconds = CVS_RETRY_DELAY_SECONDS;
    update_countdown_message(header, el, remaining_seconds, next_attempt);

    return new Promise((resolve) => {
        g_countdown_timer = window.setInterval(() => {
            remaining_seconds -= 1;
            update_countdown_message(header, el, remaining_seconds, next_attempt);

            if (remaining_seconds <= 0) {
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

function normalize_file_status(file_status)
{
    if (file_status == null) {
        return "";
    }

    return String(file_status).trim().toLowerCase();
}

function render_report_log(report_output_element)
{
    report_output_element.innerHTML = `<ul><li>${report_log.map(escape_html).join("</li><li>")}</li></ul>`;
}

function post_cvs_status(status)
{
    bc.postMessage({
        type: "cvs-report-status",
        status: status,
        record_id: g_record_id,
        lat: g_lat,
        lon: g_lon,
        year: g_year
    });
}

function escape_html(value)
{
    if (value == null) return "";
    return String(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}


function render_close_button_html()
{
    return `<input id="close_button" type="button" alt="Close this tab." value="Close this tab" onclick="window.close();" />`
}

function render_try_again_button_html()
{
    return `<input id="try_again_button" type="button" alt="Try again." value="Try again" onclick="try_again_button_click();" />`
}

function render_disabled_try_again_button_html()
{
    return `<input id="try_again_button" type="button" alt="Try again." value="Try again" disabled />`
}

function render_download_button_html()
{
    var pdf_url = `${location.protocol}//${location.host}/api/cvsAPI/${encodeURIComponent(g_record_id)}`;
    const safe_record_id = escape_html(g_record_id);
    return `
    <a id="a_download" href="${escape_html(pdf_url)}">
    <input id="download_button" type="button" alt="Download ${safe_record_id} PDF" value="Download ${safe_record_id} PDF" onclick="download_button_click()" />
    </a>
    `;
}


function download_button_click()
{
    const el = document.getElementById("a_download");
    el.click();

}

async function try_again_button_click()
{
    const report_output_element = document.getElementById("report_output_id");
    const spinner = document.getElementById("spinner-id");
    const header = document.getElementById("header");
    const el = document.getElementById("output");
    await run_cvs_report_polling(header, el, spinner, report_output_element, false);
}
