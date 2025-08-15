async function render8(p_post_html)
{
    const metadata = indicator_map.get(8);
    const data_list = await get_indicator_values(metadata.indicator_id);

    return `
    ${render_header()}
    <br>
${render_navigation_strip(8)}
<div>
<h3>${metadata.title}</h3>
<p>${metadata.description}</p>
<div align=center>${await render8_chart(p_post_html, metadata, data_list)}</div>
<br/>
<div align=center>${await render8_table(metadata, data_list)}</div>
</div>

${render_navigation_strip(8)}
`;


}

async function render8_chart(p_post_html, p_metadata, p_data_list)
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

async function render8_table(p_metadata, p_data_list)
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

    let total = 0;



    /*
    Frequency of selected committee 
    determinations on circumstances
    surrounding death

        Yes No Probably Unknown

        obesity
        discrimination
        mental health conditions
        substance use disorder
        suicide
        homicide

        { name: "MCauseD1", title: "Mental Health Conditions - Yes" },
        { name: "MCauseD2", title: "Mental Health Conditions - No" },
        { name: "MCauseD3", title: "Mental Health Conditions - Probably" },
        { name: "MCauseD4", title: "Mental Health Conditions - Unknown" },
        { name: "MCauseD5", title: "Mental Health Conditions - Blank" },
        { name: "MCauseD6", title: "Substance Use Disorder - Yes" },
        { name: "MCauseD7", title: "Substance Use Disorder - No" },
        { name: "MCauseD8", title: "Mental Health Conditions - Probably" },
        { name: "MCauseD9", title: "Mental Health Conditions - Unknown" },
        { name: "MCauseD10", title: "Substance Use Disorder" },
        { name: "MCauseD11", title: "Suicide - Yes" },
        { name: "MCauseD12", title: "Suicide - No" },
        { name: "MCauseD13", title: "Suicide - Probably" },
        { name: "MCauseD14", title: "Suicide - Unknown" },
        { name: "MCauseD15", title: "Suicide - Blank" },
        { name: "MCauseD16", title: "Obesity - Yes" },
        { name: "MCauseD17", title: "Obesity - No" },
        { name: "MCauseD18", title: "Obesity - Probably" },
        { name: "MCauseD19", title: "Obesity - Unknown" },
        { name: "MCauseD20", title: "Obesity - (blank)" },
        { name: "MCauseD21", title: "Discrimination - Yes" },
        { name: "MCauseD22", title: "Discrimination - No" },
        { name: "MCauseD23", title: "Discrimination - Probably" },
        { name: "MCauseD24", title: "Discrimination - Unknown" },
        { name: "MCauseD25", title: "Discrimination - (blank)" },
        { name: "MCauseD26", title: "Homicide - Yes" },
        { name: "MCauseD27", title: "Homicide - No" },
        { name: "MCauseD28", title: "Homicide - Probably" },
        { name: "MCauseD29", title: "Homicide - Unknown" },
        { name: "MCauseD30", title: "Homicide - (blank)" },

    */


    totals.forEach((value, key) =>
    {
        if(key != p_metadata.blank_field_id)
        {
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
    </tbody>

    </table><br/>
    <p><strong>Mental Health Conditions - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD5")}</p>
    <p><strong>Substance Use Disorder - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD10")}</p>
    <p><strong>Suicide - Number of deaths with missing (blank) values:</strong> ${totals.get("MCauseD15")}</p>

    <br/>
    <p>This data has been taken directly from the MMRIA database and is not a final report.</p>
    <br/>
    `;
}