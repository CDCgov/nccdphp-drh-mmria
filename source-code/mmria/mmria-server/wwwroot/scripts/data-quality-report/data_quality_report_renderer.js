function data_quality_report_render(p_quarters) {
    const result = [];
    const data_quality_report_quarters_list = render_data_quality_report_quarters(p_quarters);
    g_model.selectedQuarter = p_quarters[0];
    g_model.reportType = 'Summary';

    result.push(`
        <div class="d-flex">
            <div class="mb-4">
                <div class="font-weight-bold h6 mt-2">Select Report Type</div>
                <div style='margin-left: 1.2rem !important;' class="form-check mb-1">
                    <input class="form-check-input big-checkbox" type="radio" id="summary-report" name="report-type" value="Summary" onclick="updateReportType(event)" checked>
                    <label class="form-check-label" for="summary-report">Summary Report</label>
                </div>
                <div style='margin-left: 1.2rem !important;' class="form-check mb-1">
                    <input class="form-check-input big-checkbox" type="radio" id="detail-report" name="report-type" value="Detail" onclick="updateReportType(event)">
                    <label class="form-check-label" for="detail-report">Detail Report</label>
                </div>
                <div style='margin-left: 1.2rem !important;' class="form-check mb-1">
                    <input class="form-check-input big-checkbox" type="radio" id="summary-detail-report" name="report-type" value="Summary & Detail" onclick="updateReportType(event)">
                    <label class="form-check-label" for="summary-detail-report">Summary & Detail Report</label>
                </div>
                <div style="display:none;">
                    <input class="form-check-input big-checkbox" type="radio" id="summary-detail-report-debug" name="report-type" value="Debug" onclick="updateReportType(event)">
                    <label class="form-check-label" for="summary-detail-report-debug">Debug</label>
                    <input type="text" id="debug-report-question" name="report-type" value=""/><br/>
                    <textarea id="debug-report-external-list" rows=7 cols=40></textarea>
                </div>
            </div>
        </div>
        <div class="d-flex">
            <div class="vertical-control">
                <label for="quarters-list" class="mb-0 font-weight-bold mr-2">Select Quarter</label>
                <div id="quarter_msg" class="mb-3">
                    <i>${g_model.selectedQuarter} is the currently selected quarter that will be used to compare to the previous 4 quarters.</i>
                </div>
                <select class="form-select form-control col-md-5" name="quarters-list" id="quarters-list" onchange="updateQuarter(event)">
                    ${data_quality_report_quarters_list}
                </select>
            </div>
        </div>
        <div class="d-flex">
            <div class="mb-3 mt-3" id="case_folder"></div>
        </div>
        <div class="d-flex">
            <button id="generate_btn" class="btn primary-button pl-1 pr-1" onclick="download_data_quality_report_button_click()">
                Generate ${g_model.reportType} Report for ${g_model.selectedQuarter}
            </button>
        </div>
    `);
    return result;
}

function render_case_folder_include_list() {
    const el = document.getElementById("case_folder");
    const html_array = [];

    if (has_multiple_case_folder()) {
        html_array.push("<div class='mb-2 font-weight-bold mr-2'>Select Cases From Folder(s):</div>");
        for (let i = 0; i < g_case_folder_list.length; i++) {
            const child = g_case_folder_list[i];
            const element_id = child === "/" ? "topfolder" : child.replace("/", "_");
            html_array.push("<div style='margin-left: 1.2rem !important;' class='form-check mt-1'>");
            html_array.push(`<input class='form-check-input big-checkbox' id='${element_id}' value='${child.replace(/'/g, "&#39;")}' type='checkbox' name='case_folder_checkbox' onchange='updatecase_folder(event)' checked>`);
            html_array.push(`<label class='form-check-label ml-0 mb-0 mt-0 pt-2' for='${element_id}'>${child === "/" ? "Top Folder" : child}</label></div>`);
        }
        el.innerHTML = html_array.join("");
    } else {
        el.style.display = "none";
    }
}

function updateQuarter(e) {
    g_model.selectedQuarter = e.target.value;
    renderQuarterInfo();
}

function updateReportType(e) {
    g_model.reportType = e.target.value;
    renderQuarterInfo();
}

function updatecase_folder(e) {
    const included_case_folder = [];
    const checkboxes = document.getElementsByName('case_folder_checkbox');
    for (const checkbox of checkboxes) {
        if (checkbox.checked) {
            included_case_folder.push(checkbox.value);
        }
    }
    g_model.includedCaseFolder = [...included_case_folder];
    renderQuarterInfo();
}

function renderQuarterInfo() {
    document.getElementById('quarter_msg').innerHTML =
        `<i>${g_model.selectedQuarter} is the currently selected quarter that will be used to compare to the previous 4 quarters.</i>`;
    document.getElementById('generate_btn').innerHTML =
        `Generate ${g_model.reportType} Report for ${g_model.selectedQuarter}`;
    document.getElementById('generate_btn').disabled = (g_model.includedCaseFolder.length === 0);
}

function render_data_quality_report_quarters() {
    return g_quarters.map((value, index) =>
        `<option value='${value}'${index === 0 ? ' selected' : ''}>${value}</option>`
    ).join("");
}

// --- Data Maps ---
const question_detail_map = new Map();
const case_detail_map = new Map();
const case_header_map = new Map();

function set_case_header(p_detail) {
    case_header_map.set(p_detail._id, {
        rec_id: p_detail.record_id,
        dt_death: p_detail.dt_death,
        dt_com_rev: p_detail.dt_com_rev,
        ia_id: p_detail._id
    });
}

function set_map_detail_data(p_qid, p_type, p_case_id) {
    let qid_map = question_detail_map.get(p_qid);
    if (!qid_map) {
        qid_map = new Map();
        question_detail_map.set(p_qid, qid_map);
    }
    let type_map = qid_map.get(p_type);
    if (!type_map) {
        type_map = new Set();
        qid_map.set(p_type, type_map);
    }
    type_map.add(p_case_id);

    let case_map = case_detail_map.get(p_case_id);
    if (!case_map) {
        case_map = new Map();
        case_detail_map.set(p_case_id, case_map);
    }
    let case_qid_map = case_map.get(p_qid);
    if (!case_qid_map) {
        case_qid_map = new Set();
        case_map.set(p_qid, case_qid_map);
    }
    case_qid_map.add({ case_id: p_case_id, type: p_type });
}

// --- Debug Variables ---
var g_debug_report_question = null;
var g_debug_report_external_list = null;
var g_internal_set = null;
var g_current_set = new Set();
var g_previous_set = new Set();
var g_external_set = null;

const g_debug_list = [];
async function create_debug() {
    // Placeholder for debug logic
}

async function download_data_quality_report_button_click() {
    show_loading_modal();

    g_debug_report_question = document.getElementById("debug-report-question").value;
    g_debug_report_external_list = document.getElementById("debug-report-external-list").value;
    g_internal_set = new Set();
    g_external_set = new Set();
    g_current_set = new Set();
    g_previous_set = new Set();

    question_detail_map.clear();
    case_detail_map.clear();
    case_header_map.clear();

    const selected_quarter = document.getElementById('quarters-list').value;
    const arr = selected_quarter.split("-");
    const quarter_number = parseFloat(`${arr[1].trim('"')}.${((parseInt(arr[0].replace("Q", "")) - 1) * .25).toString().replace("0.", "")}`);

    const selected_case_folder_list = get_selected_folder_list();

    let dqr_detail_data = await $.ajax({
        url: `${location.protocol}//${location.host}/api/dqr-detail/${selected_quarter}`,
    });

    let summary_data = get_new_summary_data();
    dqr_detail_data.docs.sort((a, b) => a.record_id - b.record_id);

    for (const item of dqr_detail_data.docs) {
        const is_only_one_folder = selected_case_folder_list.length === 0;
        if (is_only_one_folder) {
            if (g_case_folder_list.indexOf("/") > -1) {
                // root folder selected
            } else if (g_case_folder_list.indexOf(item.case_folder) < 0) {
                continue;
            }
        } else {
            const has_not_selected_root_folder = selected_case_folder_list.indexOf("/") < 0;
            if (has_not_selected_root_folder && selected_case_folder_list.indexOf(item.case_folder) < 0) {
                continue;
            }
            const has_selected_root_folder = selected_case_folder_list.indexOf("/") > -1;
            if (has_selected_root_folder && item.case_folder !== "/") {
                if (selected_case_folder_list.indexOf(item.case_folder) < 0) {
                    if (g_case_folder_list.indexOf(item.case_folder) < 0) {
                        // not in folder list
                    } else {
                        continue;
                    }
                }
            } else if (g_case_folder_list.indexOf("/") > -1) {
                // root folder selected
            } else if (g_case_folder_list.indexOf(item.case_folder) < 0) {
                continue;
            }
        }

        set_case_header(item);
        const new_id = item._id.replace("dqr-", "");

        if (item.add_quarter_number <= quarter_number) {
            if (g_model.reportType === "Debug") {
                switch (g_debug_report_question) {
                    case "1": if (item.n01 === 1) g_internal_set.add(new_id); break;
                    case "2": if (item.n02 === 1) g_internal_set.add(new_id); break;
                    case "3": if (item.n03 === 1) g_internal_set.add(new_id); break;
                    case "4":
                        if (item.cmp_quarter_number <= quarter_number && item.n04 === 1) g_internal_set.add(new_id);
                        break;
                    case "5": if (item.n05 === 1) g_internal_set.add(new_id); break;
                    case "6":
                        if (item.cmp_quarter_number === quarter_number && item.n06 === 1) g_internal_set.add(new_id);
                        break;
                    case "7": if (item.n07 === 1) g_internal_set.add(new_id); break;
                    case "49":
                        if (item.cmp_quarter_number === quarter_number && item.n49.t === 1) {
                            g_internal_set.add(new_id);
                            if (item.n49.p === 1) g_current_set.add(new_id);
                        } else if (item.cmp_quarter_number < quarter_number && item.cmp_quarter_number >= quarter_number - 1.0 && item.n49.t === 1) {
                            g_internal_set.add(new_id);
                            if (item.n49.p === 1) g_previous_set.add(new_id);
                        }
                        break;
                    case "header":
                        if (item.cmp_quarter_number < quarter_number && item.cmp_quarter_number >= quarter_number - 1.25 && item.n06 === 1) g_internal_set.add(new_id);
                        break;
                    default: break;
                }
            }

            summary_data.n01 += item.n01;
            summary_data.n02 += item.n02;
            for (let i = 0; i < 8; i++) {
                summary_data.n03[i] += item.n03[i];
            }
            if (item.cmp_quarter_number <= quarter_number) {
                summary_data.n04 += item.n04;
                summary_data.n05 += item.n05;
            }
            if (item.cmp_quarter_number === quarter_number) {
                summary_data.n06 += item.n06;
                summary_data.n07 += item.n07;
                summary_data.current_hrcpr_bcp_secti_is_2 += item.hrcpr_bcp_secti_is_2;
                summary_data.current_is_preventable_death += item.is_preventable_death;
            } else if (item.cmp_quarter_number < quarter_number && item.cmp_quarter_number >= quarter_number - 1.0) {
                summary_data.previous_hrcpr_bcp_secti_is_2 += item.hrcpr_bcp_secti_is_2;
                summary_data.previous_is_preventable_death += item.is_preventable_death;
                summary_data.previous4QuarterReview += item.n06;
                summary_data.n08 += item.n08;
                summary_data.n09 += item.n09;
            }

            if (item.cmp_quarter_number === quarter_number) {
                for (let i = 10; i < 50; i++) {
                    let fld = `n${i}`;
                    if (item[fld].m === 1 || (i > 43 && item[fld].t === 1 && item[fld].p === 0)) {
                        set_map_detail_data(i, "Current Quarter, Missing", item._id);
                    }
                    if (item[fld].u === 1 || (i > 43 && item[fld].t === 1 && item[fld].p === 0)) {
                        set_map_detail_data(i, "Current Quarter, Unknown", item._id);
                    }
                    if (i > 43 && item[fld].t === 1 && item[fld].p === 0) {
                        set_map_detail_data(i, "Current Quarter, Failed Logic Check", item._id);
                    }
                    if (i < 44) {
                        summary_data[fld].s.mn += item[fld].m;
                        summary_data[fld].s.un += item[fld].u;
                    } else {
                        summary_data[fld].s.tn += item[fld].t;
                        summary_data[fld].s.pn += item[fld].p;
                    }
                }
            }

            if (item.cmp_quarter_number < quarter_number && item.cmp_quarter_number >= quarter_number - 1.0) {
                for (let i = 10; i < 50; i++) {
                    let fld = `n${i}`;
                    if (item[fld].m === 1) {
                        set_map_detail_data(i, "Previous 4 Quarters, Missing", item._id);
                    }
                    if (item[fld].u === 1) {
                        set_map_detail_data(i, "Previous 4 Quarters, Unknown", item._id);
                    }
                    if (i > 43 && item[fld].t === 1 && item[fld].p === 0) {
                        set_map_detail_data(i, "Previous 4 Quarters, Failed Logic Check", item._id);
                    }
                    if (i < 44) {
                        summary_data[fld].p.mn += item[fld].m;
                        summary_data[fld].p.un += item[fld].u;
                    } else {
                        summary_data[fld].p.tn += item[fld].t;
                        summary_data[fld].p.pn += item[fld].p;
                    }
                }
            }
        }
    }

    // Calculate summary percentages
    for (let i = 10; i <= 49; i++) {
        let fld = 'n' + i;
        if (i < 44) {
            if ([12, 14, 17, 22, 25, 26, 30].includes(i)) {
                if (summary_data.current_hrcpr_bcp_secti_is_2 > 0) {
                    summary_data[fld].s.mp = (summary_data[fld].s.mn / summary_data.current_hrcpr_bcp_secti_is_2) * 100;
                    summary_data[fld].s.up = (summary_data[fld].s.un / summary_data.current_hrcpr_bcp_secti_is_2) * 100;
                }
                if (summary_data.previous_hrcpr_bcp_secti_is_2 > 0) {
                    summary_data[fld].p.mp = (summary_data[fld].p.mn / summary_data.previous_hrcpr_bcp_secti_is_2) * 100;
                    summary_data[fld].p.up = (summary_data[fld].p.un / summary_data.previous_hrcpr_bcp_secti_is_2) * 100;
                }
            } else {
                if (summary_data.n06 > 0) {
                    summary_data[fld].s.mp = (summary_data[fld].s.mn / summary_data.n06) * 100;
                    summary_data[fld].s.up = (summary_data[fld].s.un / summary_data.n06) * 100;
                }
                if (summary_data.n08 > 0) {
                    summary_data[fld].p.mp = (summary_data[fld].p.mn / summary_data.n08) * 100;
                    summary_data[fld].p.up = (summary_data[fld].p.un / summary_data.n08) * 100;
                }
            }
        } else {
            if (summary_data[fld].s.tn > 0) {
                summary_data[fld].s.pp = (summary_data[fld].s.pn / summary_data[fld].s.tn) * 100;
            }
            if (summary_data[fld].p.tn > 0) {
                summary_data[fld].p.pp = (summary_data[fld].p.pn / summary_data[fld].p.tn) * 100;
            }
        }
    }

    // Debug report
    if (g_model.reportType === "Debug") {
        for (const item of g_debug_report_external_list.split("\n")) {
            g_external_set.add(item);
        }
        const internal_only = new Set([...g_internal_set].filter(x => !g_external_set.has(x)));
        const external_only = new Set([...g_external_set].filter(x => !g_internal_set.has(x)));

        const dd = {
            content: [
                `Selected DQR Question: ${g_debug_report_question}`,
                { text: "\n\n" },
                `INTERNAL ONLY ****** ${internal_only.size}`,
                { ul: [...internal_only].map(item => {
                    const detail = case_header_map.get("dqr-" + item);
                    const is_missing = g_current_set.has(item) ? "current" : "";
                    const is_unknown = g_previous_set.has(item) ? "previous" : "";
                    return `${item} ${detail.rec_id} ${detail.dt_death} ${detail.dt_com_rev} ${is_missing} ${is_unknown}`;
                }) },
                { text: "\n\n" },
                `EXTERNAL ONLY ****** ${external_only.size}`,
                { ul: [...external_only] }
            ]
        };
        await pdfMake.createPdf(dd).open();
    }

    // Summary report
    if (g_model.reportType === 'Summary' || g_model.reportType === 'Summary & Detail') {
        const headers = {
            title: `Data Quality Report for: ${getCaseFolder()}`,
            subtitle: `Reporting Period: ${get_header_reporting_period(g_model.selectedQuarter)}    Previous 4 Periods: ${getPreviousFourQuarters()}`
        };
        await create_pdf('Summary', summary_data, g_model.selectedQuarter, headers);
    }

    // Detail report
    if (g_model.reportType === 'Detail' || g_model.reportType === 'Summary & Detail') {
        const headers = {
            title: `Data Quality Report Details for: ${getCaseFolder()}`,
            subtitle: `Reporting Period: ${get_header_reporting_period(g_model.selectedQuarter)}   Previous 4 Periods: ${getPreviousFourQuarters()}`
        };

        let detail_data = {
            questions: [],
            cases: [],
            total: 0,
        };

        question_detail_map.forEach((qitem, qid) => {
            const types = [
                "Current Quarter, Missing",
                "Current Quarter, Unknown",
                "Current Quarter, Failed Logic Check",
                "Previous 4 Quarters, Missing",
                "Previous 4 Quarters, Unknown",
                "Previous 4 Quarters, Failed Logic Check"
            ];
            types.forEach(type_id => {
                const t_item = qitem.get(type_id);
                if (t_item && t_item.size > 0) {
                    let num_count = 1;
                    const details = [];
                    t_item.forEach(case_id => {
                        const header = case_header_map.get(case_id);
                        details.push({
                            num: num_count++,
                            rec_id: header.rec_id,
                            dt_death: header.dt_death,
                            dt_com_rev: header.dt_com_rev,
                            ia_id: header.ia_id.substring(4),
                        });
                    });
                    detail_data.questions.push({ qid, typ: type_id, detail: details });
                }
            });
        });

        detail_data.questions.sort((a, b) => a.qid - b.qid);
        detail_data.total = detail_data.questions.length;

        case_detail_map.forEach((qitem, case_id) => {
            const header = case_header_map.get(case_id);
            let new_item = {
                rec_id: header.rec_id,
                dt_death: header.dt_death,
                dt_com_rev: header.dt_com_rev,
                ia_id: header.ia_id.substring(4),
                ab_case_id: '',
                detail: []
            };
            qitem.forEach((t_item, qid) => {
                new_item.detail.push({ qid, typ: t_item.values().next().value.type });
            });
            detail_data.cases.push(new_item);
        });

        await create_pdf('Detail', detail_data, g_model.selectedQuarter, headers);
    }

    close_loading_modal();
}

function getCaseFolder() {
    const top_folder_name = sanitize_encodeHTML(window.location.host.toUpperCase().split("-")[0]) + "-MMRIA";
    let case_folder_display = top_folder_name;
    let case_folder_exclude = ' - Exclude: ';
    const display_size = 25;
    const display_number = 22;

    if (g_case_folder_list.length === 1) {
        return case_folder_display.length > display_size ? case_folder_display.substring(0, display_number) + "..." : case_folder_display;
    }
    if (g_case_folder_list.length === g_model.includedCaseFolder.length) {
        return case_folder_display;
    }
    if (g_model.includedCaseFolder[0] === '/') {
        g_case_folder_list.forEach(j => {
            if (j !== '/' && g_model.includedCaseFolder.indexOf(j) === -1) {
                case_folder_exclude += j + ', ';
            }
        });
        case_folder_display += case_folder_exclude.slice(0, -2);
        return case_folder_display.length > display_size ? case_folder_display.substring(0, display_number) + "..." : case_folder_display;
    }
    case_folder_display = g_model.includedCaseFolder.join(', ');
    return case_folder_display.length > display_size ? case_folder_display.substring(0, display_number) + "..." : case_folder_display;
}

function getPreviousFourQuarters() {
    let qStr = '';
    let arr = g_model.selectedQuarter.split("-");
    let qtr = parseInt(arr[0][1]);
    let yy = parseInt(arr[1].substr(2));
    for (let i = 0; i < 4; i++) {
        qtr--;
        if (qtr === 0) {
            qtr = 4;
            yy--;
        }
        qStr += `Q${qtr}-${yy}`;
        if (i < 3) qStr += ', ';
    }
    return qStr;
}

function get_header_reporting_period(value) {
    const arr = value.split("-");
    const year_string = arr[1];
    const year_two_digit = arr[1].substr(2);
    switch (arr[0].toUpperCase()) {
        case 'Q1': return `Q1-${year_two_digit} (Jan-Mar ${year_string})`;
        case 'Q2': return `Q2-${year_two_digit} (Apr-Jun ${year_string})`;
        case 'Q3': return `Q3-${year_two_digit} (Jul-Sep ${year_string})`;
        case 'Q4': return `Q4-${year_two_digit} (Oct-Dec ${year_string})`;
        default: return value;
    }
}

async function create_pdf(report_type, data, quarter, headers) {
    await create_data_quality_report_pdf(report_type, data, quarter, headers);
}
