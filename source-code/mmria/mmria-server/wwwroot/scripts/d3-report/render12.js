async function render12(p_post_html)
{
    const metadata = indicator_map.get(12);
    const data_list = await get_indicator_values(metadata.indicator_id);

    const metadata2 = indicator_map.get(13);
    const data_list2 = await get_indicator_values(metadata2.indicator_id);


    return `
    ${render_header()}

${render_navigation_strip(12)}
<div>
<h3>${metadata.title}</h3>
<p>${metadata.description}</p>
<ul style="align:center;">
    <li style="display: inline-block;float:left;width:48%">
        <div align=center>${await render121_chart(p_post_html, metadata, data_list)}</div>
        <br/>
        <div align=center>${await render121_table(metadata, data_list)}</div>
        </div>

    </li>

    <li style="display: inline-block;float:right;width:48%">
        <div align=center>${await render122_chart(p_post_html, metadata2, data_list2)}</div>
        <br/>
        <div align=center>${await render122_table(metadata2, data_list2)}</div>
        </div>

    </li>
</ul>

<br style="clear:both;"/>
${render_navigation_strip(12)}
`;


}


async function render121_chart(p_post_html, p_metadata, p_data_list)
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
    

    p_post_html.push
    (
        
    `var chart1 = c3.generate({
        legend: {
            show: false
        },
        data: {
            columns: [
                ["${p_metadata.indicator_id}", ${data.join(",")}
                 ],
            ],
            types: {
                ${p_metadata.indicator_id}: 'bar',        
            },
            names: {
                ${p_metadata.indicator_id}: '${p_metadata.x_axis_title}',
            },
            labels: true 
        },
        padding: {
            //  left: 375
        },
        axis: {
            rotated: true, 
            
            x: {
                 label: {
                 text: '${p_metadata.x_axis_title}',
                 position: 'outer-middle'  
                 },
                 tick: {
                    multiline: false,
                    culling: false,
                    outer: false
                },
                type: 'category',
                categories: [${categories}],
            },
            y: {
                label: {
                    text: '${p_metadata.y_axis_title}',
                    position: 'outer-center' 
                },
                tick: {
                    multiline: false,
                    culling: false,
                    outer: false
                },
            }
        },
        //size: {
        //    height: 600, 
        //    width: 600
        //  },
          transition: {
            duration: null
          },
          bindto: '#chart1',
          onrendered: function()
          {
            const title_element = document.createElement('title');
            title_element.innerText = '${p_metadata.chart_title_508}';

            const description_element = document.createElement('desc');
            description_element.innerText = '${render_chart_508_description(p_metadata, data, totals)}';

            const svg_char = document.querySelector('#chart1 svg');

            if(svg_char != null)
            {
                svg_char.appendChild(title_element);
                svg_char.appendChild(description_element);
            }
          }
        }); ` 
    );

    return `
    <div class="card">
        <div class="card-header bg-secondary">
        <h4 class="h5">${p_metadata.chart_title}</h4>
        </div>
        <div class="card-body">
            <div id="chart1"></div>
        </div>
    </div>
    
    `
}

async function render122_chart(p_post_html, p_metadata, p_data_list)
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
    

    p_post_html.push
    (
        
    `var chart2 = c3.generate({
        legend: {
            show: false
        },
        data: {
            columns: [
                ["${p_metadata.indicator_id}", ${data.join(",")}
                 ],
            ],
            types: {
                ${p_metadata.indicator_id}: 'bar',        
            },
            names: {
                ${p_metadata.indicator_id}: '${p_metadata.x_axis_title}',
            },
            labels: true 
        },
        padding: {
            //  left: 375
        },
        axis: {
            rotated: true, 
            
            x: {
                 label: {
                 text: '${p_metadata.x_axis_title}',
                 position: 'outer-middle'  
                 },
                tick: {
                    multiline: false,
                },
                type: 'category',
                categories: [${categories}],
            },
            y: {
                label: {
                    text: '${p_metadata.y_axis_title}',
                    position: 'outer-center' 
                }
            }
        },
        //size: {
        //    height: 600, 
        //    width: 600
        //  },
          transition: {
            duration: null
          },
          bindto: '#chart2',
          onrendered: function()
          {
            const title_element = document.createElement('title');
            title_element.innerText = '${p_metadata.chart_title_508}';

            const description_element = document.createElement('desc');
            description_element.innerText = '${render_chart_508_description(p_metadata, data, totals)}}';

            const svg_char = document.querySelector('#chart2 svg');

            if(svg_char != null)
            {
                svg_char.appendChild(title_element);
                svg_char.appendChild(description_element);
            }
          }
        }); ` 
    );

    return `
    <div class="card">
        <div class="card-header bg-secondary">
        <h4 class="h5">${p_metadata.chart_title}</h4>
        </div>
        <div class="card-body">
            <div id="chart2"></div>
        </div>
    </div>
    
    `
}

async function render121_table(p_metadata, p_data_list)
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

    return `<table class="table rounded-0 mb-0"  style="width:80%"
    title="${p_metadata.table_title_508 != null ? p_metadata.table_title_508.replace("'", ""): ""}"</CAPTION>
    >
    <thead class="thead">
    <tr style="background-color:#e3d3e4">
        <th valign=top>${p_metadata.table_title}</th>
        <th align=right style="width:25%;align:right;">Number of deaths</th>
    </tr>
    </thead>
    <tbody>
        ${data.join("")}
    </tbody>
    <tfoot>
        <tr style="background-color:#e3d3e4"><td><strong>Total</strong></td>
        <td align=right><strong>${total}</strong></td></tr>
    </tfoot>
    </table>
    <br/>
    <p><strong>Number of deaths with missing (blank) values:</strong> ${totals.get(p_metadata.blank_field_id)} </p>
    <br/>
    <p>This data has been taken directly from the MMRIA database and is not a final report</p>
    <br/>
    `
}

async function render122_table(p_metadata, p_data_list)
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
    


    return `<table class="table rounded-0 mb-0" style="width:80%"
    title="${p_metadata.table_title_508 != null ? p_metadata.table_title_508.replace("'", ""): ""}"</CAPTION>
    >
    <thead class="thead">
    <tr style="background-color:#e3d3e4">
        <th>${p_metadata.table_title}</th>
        <th align=right style="width:25%">Number of deaths</th>
    </tr>
    </thead>
    <tbody>
        ${data.join("")}
    </tbody>
    <!--tfoot>
        <tr style="background-color:#e3d3e4"><td><strong>Total</strong></td>
        <td align=right><strong>${total}</strong></td></tr>
    </tfoot-->
    </table>
    <br/>
    <p><strong>Number of deaths with missing (blank) values:</strong> ${totals.get(p_metadata.blank_field_id)} </p>
    <br/>
    `
}