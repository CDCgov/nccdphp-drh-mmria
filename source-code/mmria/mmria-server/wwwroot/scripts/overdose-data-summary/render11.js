async function render11(p_post_html) {
    const metadata = indicator_map.get(11);
    const data_list = await get_indicator_values(metadata.indicator_id);

    const metadata2 = indicator_map.get(11.2);
    const data_list2 = await get_indicator_values(metadata2.indicator_id);

    return `
        ${render_header()}
        <br>
        ${render_navigation_strip(11)}
        <div>
            <h3>${metadata.title}</h3>
            <p>${metadata.description}</p>
            <div class="d-flex flex-column">
                <div class="d-flex mr-1">
                    <div class="col-md-6 pr-3">
                        <div>
                            ${await render111_chart(p_post_html, metadata, data_list)}
                        </div>
                        <div class="mt-4">
                            ${await render111_table(metadata, data_list)}
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div>
                            ${await render112_chart(p_post_html, metadata2, data_list2)}
                        </div>
                        <div class="d-flex flex-column mt-4">
                            ${await render112_table(metadata2, data_list2)}
                        </div>
                    </div>
                </div>
                <div class="d-flex justify-content-center">
                    <i>
                        This data has been taken directly from the MMRIA database and is not a final report.
                    </i>
                </div>
            </div>
            ${render_navigation_strip(11)}
        </div>
    `;
}

async function render111_chart(p_post_html, p_metadata, p_data_list) {
    const totals = new Map();
    const categories = [];

    for (let i = 0; i < p_metadata.field_id_list.length; i++) {
        const item = p_metadata.field_id_list[i];
        if (item.name !== p_metadata.blank_field_id) {
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
    totals.forEach((value) => data.push(value));

    render_chart_post_html(p_post_html, p_metadata, data, categories, totals, "chart1");

    return `
        <div class="card-container-light">
            <div class="header">
                <span class="h5 m-1">${p_metadata.chart_title}</span>
            </div>
            <div class="card-content">
                <div id="chart1"></div>
            </div>
        </div>
    `;
}

async function render112_chart(p_post_html, p_metadata, p_data_list) {
    const totals = new Map();
    const categories = [];

    for (let i = 0; i < p_metadata.field_id_list.length; i++) {
        const item = p_metadata.field_id_list[i];
        if (item.name !== p_metadata.blank_field_id) {
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
    totals.forEach((value) => data.push(value));

    render_chart_post_html(p_post_html, p_metadata, data, categories, totals, "chart2");

    return `
        <div class="card-container-light">
            <div class="header">
                <span class="h5 m-1">${p_metadata.chart_title}</span>
            </div>
            <div class="card-content">
                <div id="chart2"></div>
            </div>
        </div>
    `;
}

async function render111_table(p_metadata, p_data_list) {
    const totals = new Map();
    const name_to_title = new Map();
    const categories = [];

    for (let i = 0; i < p_metadata.field_id_list.length; i++) {
        const item = p_metadata.field_id_list[i];
        categories.push(`"${item.title}"`);
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
            data.push(`
                <tr>
                    <td>${name_to_title.get(key)}</td>
                    <td align="right">${value}</td>
                </tr>
            `);
            total += value;
        }
    });

    return `
        <table class="table rounded-0 mb-0" style="width:100%"
            title="${p_metadata.table_title_508 != null ? p_metadata.table_title_508.replace("'", "") : ""}">
            <thead class="thead">
                <tr style="background-color:#e3d3e4">
                    <th valign="top">${p_metadata.table_title}</th>
                    <th style="width:25%; text-align:right;">Number of deaths</th>
                </tr>
            </thead>
            <tbody>
                ${data.join("")}
            </tbody>
            <tfoot>
                <tr style="background-color:#e3d3e4">
                    <td><strong>Total</strong></td>
                    <td align="right"><strong>${total}</strong></td>
                </tr>
            </tfoot>
        </table>
        <div style="border-color: #d5d5d5 !important; margin-top: 3.5rem !important;" class="d-flex align-self-end border rounded border-light text-left p-2 mt-3 mb-4 col-md-12">
            <span class="font-weight-bold">Number of deaths with missing (blank) values:</span>
            <span class="ml-auto">${totals.get(p_metadata.blank_field_id)}</span>
        </div>
    `;
}

async function render112_table(p_metadata, p_data_list) {
    const totals = new Map();
    const name_to_title = new Map();
    const categories = [];

    for (let i = 0; i < p_metadata.field_id_list.length; i++) {
        const item = p_metadata.field_id_list[i];
        categories.push(`"${item.title}"`);
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
            data.push(`
                <tr>
                    <td>${name_to_title.get(key)}</td>
                    <td align="right">${value}</td>
                </tr>
            `);
            total += value;
        }
    });

    return `
        <table class="table rounded-0 mb-0" style="width:100%"
            title="${p_metadata.table_title_508 != null ? p_metadata.table_title_508.replace("'", "") : ""}">
            <thead class="thead">
                <tr style="background-color:#e3d3e4">
                    <th>${p_metadata.table_title}</th>
                    <th style="width:25%; text-align:right;">Number of deaths</th>
                </tr>
            </thead>
            <tbody>
                ${data.join("")}
            </tbody>
        </table>
        <div style="border-color: #d5d5d5 !important;" class="d-flex align-self-end border rounded border-light text-left p-2 mt-3 mb-4 col-md-12">
            <span class="font-weight-bold">Number of deaths with missing (blank) values:</span>
            <span class="ml-auto">${totals.get(p_metadata.blank_field_id)}</span>
        </div>
    `;
}
