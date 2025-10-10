async function render7(p_post_html)
{
    const metadata = indicator_map.get(7);
    const data_list = await get_indicator_values(metadata.indicator_id);

    return `
    ${render_header()}
    <br>
${render_navigation_strip(7)}
<div>
<h3>${metadata.title}</h3>
<p>${metadata.description}</p>
<div align=center>${await render7_chart(p_post_html, metadata, data_list)}</div>
<br/>
<div align=center>${await render7_table(metadata, data_list)}</div>
</div>

${render_navigation_strip(7)}
`;


}

async function render7_chart(p_post_html, p_metadata, p_data_list)
{
    const totals = new Map();

    const categories = [];
    for(var i = 0; i < p_metadata.field_id_list.length; i++)
    {
        const item = p_metadata.field_id_list[i];
        if(item.name != p_metadata.blank_field_id)
        {
            categories.push(`"${item.title}"`);
            totals.set(item.name, 0);
        }
    }

    for(var i = 0; i <p_data_list.data.length; i++)
    {
        const item = p_data_list.data[i];
        if(totals.has(item.field_id))
        {
            let val = totals.get(item.field_id);
            totals.set(item.field_id, val + 1);
        }
    }

    const data = [];

    totals.forEach((value, key) =>
    {
        data.push(value);
    });
    
    render_chart_post_html(p_post_html, p_metadata, data, categories, totals);
    
    return render_chart_card_container(p_metadata.chart_title);
}

async function render7_table(p_metadata, p_data_list)
{
    const totals = new Map();
    const name_to_title = new Map();

    const categories = [];
    for(var i = 0; i < p_metadata.field_id_list.length; i++)
    {
        const item = p_metadata.field_id_list[i];
        categories.push(`"${item.title}"`);
        totals.set(item.name, 0);
        name_to_title.set(item.name, item.title);
    }

    for(var i = 0; i <p_data_list.data.length; i++)
    {
        const item = p_data_list.data[i];
        if(totals.has(item.field_id))
        {
            let val = totals.get(item.field_id);
            totals.set(item.field_id, val + 1);
        }
    }

    const data = [];
    let total = 0;

    totals.forEach((value, key) =>
    {
        if(key != p_metadata.blank_field_id)
        {
            data.push(`<tr><td>${name_to_title.get(key)}</td><td align=right>${value}</td></tr>`);
            total+=value;
        }

    });
    


    //return render_table(p_metadata, data, totals, total);

    return `<table class="table rounded-0 mb-0" style="width:50%"
    title="${p_metadata.table_title_508 != null ? p_metadata.table_title_508.replace("'", ""): ""}"
    >
    
    <thead class="thead">
    <tr style="background-color:#e3d3e4;">
        <th>${p_metadata.table_title}</th>
        <th style="width:25%;text-align:right;">Number of deaths</th>
    </tr>
    </thead>
    <tbody>
        ${data.join("")}
    </tbody>
    </table>
    <div style="border-color: #d5d5d5 !important;" class="d-flex align-self-end border rounded border-light text-left p-2 mt-3 mb-4 col-md-6">
        <span class="font-weight-bold">Number of deaths with missing (blank) values:</span>
        <span class="ml-auto"><i>${totals.get(p_metadata.blank_field_id)}</i></span>
    </div>
    <div><i>This data has been taken directly from the MMRIA database and is not a final report.</i></div>
    `
}