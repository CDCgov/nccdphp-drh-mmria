async function render10(p_post_html) {
    const metadata = indicator_map.get(10);
    const data_list = await get_indicator_values(metadata.indicator_id);

    return `
        ${render_header(10)}
        <br>
        ${render_navigation_strip(10)}
        <div>
            <h3 class="h4 font-weight-bold">${metadata.title}</h3>
            <p>${metadata.description}</p>
            <div align="center">${await render10_chart(p_post_html, metadata, data_list)}</div>
            <br/>
            <div align="center">${await render10_table(metadata, data_list)}</div>
        </div>
        ${render_navigation_strip(10)}
    `;
}

async function render10_chart(p_post_html, p_metadata, p_data_list) {
    const totals = new Map();
    const categories = [];

    for (let i = 0; i < p_metadata.field_id_list.length; i++) {
        const item = p_metadata.field_id_list[i];
        if (!item.title.includes("(blank)")) {
            categories.push(`"${item.title}"`);
            totals.set(item.name, 0);
        }
    }

    for (let i = 0; i < p_data_list.data.length; i++) {
        const item = p_data_list.data[i];
        if (totals.has(item.field_id)) {
            let val = totals.get(item.field_id);
            totals.set(item.field_id, val + 1);
        }
    }

    const data = [];
    totals.forEach((value) => {
        data.push(value);
    });

    // Chart rendering logic can be added here
    return ``;
}

async function render10_table(p_metadata, p_data_list) {
    const totals = new Map();
    const name_to_title = new Map();
    const categories = [];

    for (let i = 0; i < p_metadata.field_id_list.length; i++) {
        const item = p_metadata.field_id_list[i];
        if (!item.title.includes("(blank)")) {
            categories.push(`"${item.title}"`);
        }
        totals.set(item.name, 0);
        name_to_title.set(item.name, item.title);
    }

    for (let i = 0; i < p_data_list.data.length; i++) {
        const item = p_data_list.data[i];
        if (totals.has(item.field_id)) {
            let val = totals.get(item.field_id);
            totals.set(item.field_id, val + 1);
        }
    }

    const data = [];
    let total = 0;

    totals.forEach((value, key) => {
        if (key !== p_metadata.blank_field_id) {
            data.push(`<tr><td>${name_to_title.get(key)}</td><td align="right">${value}</td></tr>`);
            total += value;
        }
    });

    return `
        <table class="col-md-10 table" title="${p_metadata.table_title_508 ? p_metadata.table_title_508.replace("'", "") : ""}">
            <thead>
                <tr class="header-level-top-black">
                    <th colspan="5">${proper_casing(p_metadata.chart_title)}</th>
                </tr>
                <tr class="header-level-2">
                    <th>${proper_casing(p_metadata.table_title)}</th>
                    <th style="text-align:left;">Yes</th>
                    <th style="text-align:left;">No</th>
                    <th style="text-align:left;">Probably</th>
                    <th style="text-align:left;">Unknown</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Did obesity contribute to the death?</td>
                    <td align="left">${totals.get("MCauseD16")}</td>
                    <td align="left">${totals.get("MCauseD17")}</td>
                    <td align="left">${totals.get("MCauseD18")}</td>
                    <td align="left">${totals.get("MCauseD19")}</td>
                </tr>
                <tr>
                    <td>Did discrimination contribute to the death?</td>
                    <td align="left">${totals.get("MCauseD21")}</td>
                    <td align="left">${totals.get("MCauseD22")}</td>
                    <td align="left">${totals.get("MCauseD23")}</td>
                    <td align="left">${totals.get("MCauseD24")}</td>
                </tr>
                <tr>
                    <td>Did mental health conditions contribute to the death?</td>
                    <td align="left">${totals.get("MCauseD1")}</td>
                    <td align="left">${totals.get("MCauseD2")}</td>
                    <td align="left">${totals.get("MCauseD3")}</td>
                    <td align="left">${totals.get("MCauseD4")}</td>
                </tr>
                <tr>
                    <td>Did substance use disorder contribute to the death?</td>
                    <td align="left">${totals.get("MCauseD6")}</td>
                    <td align="left">${totals.get("MCauseD7")}</td>
                    <td align="left">${totals.get("MCauseD8")}</td>
                    <td align="left">${totals.get("MCauseD9")}</td>
                </tr>
                <tr>
                    <td>Was this death a suicide?</td>
                    <td align="left">${totals.get("MCauseD11")}</td>
                    <td align="left">${totals.get("MCauseD12")}</td>
                    <td align="left">${totals.get("MCauseD13")}</td>
                    <td align="left">${totals.get("MCauseD14")}</td>
                </tr>
                <tr>
                    <td>Was this death a homicide?</td>
                    <td align="left">${totals.get("MCauseD26")}</td>
                    <td align="left">${totals.get("MCauseD27")}</td>
                    <td align="left">${totals.get("MCauseD28")}</td>
                    <td align="left">${totals.get("MCauseD29")}</td>
                </tr>
            </tbody>
        </table>
        <table class="table col-md-8 mt-3 mb-4">
            <thead>
                <tr class="header-level-top-black">
                    <th colspan="2">Deaths with Missing (blank) Values</th>
                </tr>
                <tr class="header-level-2">
                    <th width="735">Circumstances</th>
                    <th>Number of Deaths</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Obesity</td>
                    <td align="right">${totals.get("MCauseD20")}</td>
                </tr>
                <tr>
                    <td>Discrimination</td>
                    <td align="right">${totals.get("MCauseD25")}</td>
                </tr>
                <tr>
                    <td>Mental Health Conditions</td>
                    <td align="right">${totals.get("MCauseD5")}</td>
                </tr>
                <tr>
                    <td>Substance Use Disorder</td>
                    <td align="right">${totals.get("MCauseD10")}</td>
                </tr>
                <tr>
                    <td>Suicide</td>
                    <td align="right">${totals.get("MCauseD15")}</td>
                </tr>
                <tr>
                    <td>Homicide</td>
                    <td align="right">${totals.get("MCauseD30")}</td>
                </tr>
            </tbody>
        </table>
        <i>This data has been taken directly from the MMRIA database and is not a final report.</i>
    `;
}
