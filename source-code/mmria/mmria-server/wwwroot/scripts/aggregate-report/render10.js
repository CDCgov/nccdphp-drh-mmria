async function render10(p_post_html)
{
    const metadata = indicator_map.get(10);
    const data_list = await get_indicator_values(metadata.indicator_id);

    return `
    ${render_header(10)}
    <br>
${render_navigation_strip(10)}
<div>
<h3>${metadata.title}</h3>
<p>${metadata.description}</p>
<div align=center>${await render10_chart(p_post_html, metadata, data_list)}</div>
<br/>
<div align=center>${await render10_table(metadata, data_list)}</div>
</div>

${render_navigation_strip(10)}
`;


}

async function render10_chart(p_post_html, p_metadata, p_data_list)
{
    const totals = new Map();

    const categories = [];
    for(var i = 0; i < p_metadata.field_id_list.length; i++)
    {
        const item = p_metadata.field_id_list[i];
        
        if(item.title.indexOf("(blank)") < 0)
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
    const p_chart_name = "chart";
    

    return ``;

}

async function render10_table(p_metadata, p_data_list)
{
    const totals = new Map();
    const name_to_title = new Map();

    const categories = [];
    for(var i = 0; i < p_metadata.field_id_list.length; i++)
    {
        const item = p_metadata.field_id_list[i];
        if(item.title.indexOf("(blank)") < 0)
        {
            categories.push(`"${item.title}"`);
        }
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
    


    return `
    <div class="card" style="width:50%">
        <div class="card-header bg-secondary">
        <h4 class="h5">${p_metadata.chart_title}</h4>
        </div>
    </div>
    
    <br>
    <table class="table rounded-0 mb-0" style="width:50%"
    title="${p_metadata.table_title_508 != null ? p_metadata.table_title_508.replace("'", ""): ""}"
    >
    
    <thead class="thead">
    <tr style="background-color:#e3d3e4;">
        <th>${p_metadata.table_title}</th>
        <th style="text-align:right;">Yes</th>
        <th style="text-align:right;">No</th>
        <th style="text-align:right;">Probably</th>
        <th style="text-align:right;">Unknown</th>
    </tr>
    </thead>
    <tbody>
        <tr>
            <td>Did obesity contribute to the death?</td>
            <td align=right>${totals.get("MCauseD16")}</td>
            <td align=right>${totals.get("MCauseD17")}</td>
            <td align=right>${totals.get("MCauseD18")}</td>
            <td align=right>${totals.get("MCauseD19")}</td>
        </tr>
        <tr>
            <td>Did discrimination contribute to the death?</td>
            <td align=right>${totals.get("MCauseD21")}</td>
            <td align=right>${totals.get("MCauseD22")}</td>
            <td align=right>${totals.get("MCauseD23")}</td>
            <td align=right>${totals.get("MCauseD24")}</td>
        </tr>

        <tr>
            <td>Did mental health conditions contribute to the death?</td>
            <td align=right>${totals.get("MCauseD1")}</td>
            <td align=right>${totals.get("MCauseD2")}</td>
            <td align=right>${totals.get("MCauseD3")}</td>
            <td align=right>${totals.get("MCauseD4")}</td>
        </tr>


        <tr>
            <td>Did substance use disorder contribute to the death?</td>
            <td align=right>${totals.get("MCauseD6")}</td>
            <td align=right>${totals.get("MCauseD7")}</td>
            <td align=right>${totals.get("MCauseD8")}</td>
            <td align=right>${totals.get("MCauseD9")}</td>
        </tr>

        <tr>
            <td>Was this death a suicide?</td>
            <td align=right>${totals.get("MCauseD11")}</td>
            <td align=right>${totals.get("MCauseD12")}</td>
            <td align=right>${totals.get("MCauseD13")}</td>
            <td align=right>${totals.get("MCauseD14")}</td>
        </tr>


        <tr>
            <td>Was this death a homicide?</td>
            <td align=right>${totals.get("MCauseD26")}</td>
            <td align=right>${totals.get("MCauseD27")}</td>
            <td align=right>${totals.get("MCauseD28")}</td>
            <td align=right>${totals.get("MCauseD29")}</td>
        </tr>
    </tbody>

    </table><br/>
    <p><strong>Obesity - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD20")}</p>
    <p><strong>Discrimination - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD25")}</p>
    <p><strong>Mental Health Conditions - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD5")}</p>
    <p><strong>Substance Use Disorder - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD10")}</p>
    <p><strong>Suicide - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD15")}</p>
    <p><strong>Homicide - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD30")}</p>
    <br/>
    <p>This data has been taken directly from the MMRIA database and is not a final report.</p>
    <br/>
    `;
}