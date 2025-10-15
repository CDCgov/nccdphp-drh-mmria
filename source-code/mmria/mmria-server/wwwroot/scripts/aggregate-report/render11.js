async function render11(p_post_html) {
    const metadata = indicator_map.get(11);
    const data_list = await get_indicator_values(metadata.indicator_id);

    return `
        ${render_header(11)}
        ${render_navigation_strip(11)}
        <div class="mb-4 mt-4">
            <h3 class="h4 font-weight-bold">${metadata.title}</h3>
            <p>${metadata.description}</p>
            <div class="mb-3" align="center">
                ${await render11_chart(p_post_html, metadata, data_list)}
            </div>
            <div align="center">
                ${await render11_table(metadata, data_list)}
            </div>
        </div>
        <div align="center">
            <i>This data has been taken directly from the MMRIA database and is not a final report.</i>
        </div>
        ${render_navigation_strip(11)}
    `;
}

async function render11_chart(p_post_html, p_metadata, p_data_list) {
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
    totals.forEach((value) => {
        data.push(value);
    });

    render_chart_post_html(p_post_html, p_metadata, data, categories, totals);

    return render_chart_card_container(p_metadata.chart_title);
}

async function render11_table(p_metadata, p_data_list) {
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
            data.push(`<tr><td>${name_to_title.get(key)}</td><td align="right">${value}</td></tr>`);
            total += value;
        }
    });

    return `
        <table class="table" style="width:50%" title="${p_metadata.table_title_508 != null ? p_metadata.table_title_508.replace("'", "") : ""}">
            <thead>
                <tr class="header-level-2">
                    <th>${p_metadata.table_title}</th>
                    <th style="width:25%;text-align:right;">Number of Deaths</th>
                </tr>
            </thead>
            <tbody>
                ${data.join("")}
            </tbody>
        </table>
    `;
}
