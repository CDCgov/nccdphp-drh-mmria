function render0()
{
    return `
    ${render_header()}
<h3 style="clear:both;"><strong>Overview</strong></h3>

<p>The Overdose report in MMRIA grew out of the Rapid Maternal Overdose Review initiative. This initiative ensures the MMRC scope is inclusive of full abstraction and review of all overdose deaths during and within one year of the end of pregnancy; the MMRC is multidisciplinary and representative of maternal mental health, substance use disorder prevention, and addiction medicine; and the team determines contributing factors and recommendations, regardless of whether the death is determined to be pregnancy-related.</p>

<p><strong>This report only includes deaths where the Means of Fatal Injury was “Poisoning/Overdose” in the Manner of Death section of the MMRIA Committee Decisions Form.</strong> The committee should be documenting means of fatal injury for all pregnancy-associated deaths, but if the committee is not consistently doing this the number of pregnancy-associated overdose deaths reviewed could be underestimated in the report.</p>

<p>The Overdose Report can be used to look at broad categories of overdose deaths within MMRIA but should not replace more specific analysis. For example, the Overdose Report is only able to show race/ethnicity as non-Hispanic Black, non-Hispanic White, Hispanic, and Other while an individual jurisdiction can look at other race/ethnicity groupings after downloading the data. The Overdose Report can provide quick analysis for questions asked by committees or team leadership and provide areas to consider more thoroughly during analysis. </p>

<p>Select a page in the table below</p>

<table class="table table-header-light table-striped table-hover table-hover-light nein-scroll">

<thead class="thead-dark">
<tr bgcolor=silver><th>Report Number</th><th><strong>Report Page</strong></th><th><strong>Description</strong></th></tr>
</thead>

<tbody>

<tr onclick="window.location='#1'" style="cursor: pointer;">
<td><strong>1</strong></td>
<td><strong><a href="#1">Pregnancy-Relatedness</a></strong></td>
<td>${indicator_map.get(1).description}</td>
</tr>



<tr bgcolor=CCCCCC onclick="window.location='#2'" style="cursor: pointer;">
<td><strong>2</strong></td>
<td><strong><a href="#2">Timing of Death</a></strong></td>
<td>${indicator_map.get(2).description}</td>
</tr>



<tr onclick="window.location='#3'" style="cursor: pointer;">
<td><strong>3</strong></td>
<td><strong><a href="#3">Race/Ethnicity</strong></td>
<td>${indicator_map.get(3).description}</td>
</tr>



<tr bgcolor=CCCCCC onclick="window.location='#4'" style="cursor: pointer;">
<td><strong>4</strong></td>
<td><strong><a href="#4">Age</a></strong></td>
<td>${indicator_map.get(4).description}</td>
</tr>



<tr onclick="window.location='#5'" style="cursor: pointer;">
<td><strong>5</strong></td>
<td><strong><a href="#5">Education</strong></td>
<td>${indicator_map.get(5).description}</td>
</tr>


<tr onclick="window.location='#6'" style="cursor: pointer;">
<td><strong>6</strong></td>
<td><strong><a href="#6">Substance Use</strong></td>
<td>${indicator_map.get(6).description}</td>
</tr>



<tr bgcolor=CCCCCC onclick="window.location='#7'" style="cursor: pointer;">
<td><strong>7</strong></td>
<td><strong><a href="#7">Toxicology</strong></td>
<td>${indicator_map.get(7).description}</td>
</tr>



<tr onclick="window.location='#8'" style="cursor: pointer;">
<td><strong>8</strong></td>
<td><strong><a href="#8">Committee Determinations</strong></td>
<td>${indicator_map.get(8).description}</td>
</tr>



<tr bgcolor=CCCCCC onclick="window.location='#9'" style="cursor: pointer;">
<td><strong>9</strong></td>
<td><strong><a href="#9">Treatment History</strong></td>
<td>${indicator_map.get(9).description}</td>
</tr>



<tr onclick="window.location='#10'" style="cursor: pointer;">
<td><strong>10</strong></td>
<td><strong><a href="#10">Emotional Stress</strong></td>
<td>${indicator_map.get(10).description}</td>
</tr>



<tr bgcolor=CCCCCC onclick="window.location='#11'" style="cursor: pointer;">
<td><strong>11</strong></td>
<td><strong><a href="#11">Living Arrangements</a></strong></td>
<td>${indicator_map.get(11).description}</td>
</tr>

<tr bgcolor=CCCCCC onclick="window.location='#12'" style="cursor: pointer;">
<td><strong>12</strong></td>
<td><strong><a href="#12">Incarceration History</a></strong></td>
<td>${indicator_map.get(12).description}</td>
</tr>
</tbody>
</table>
`;

}


function render_table(p_metadata, p_data, p_totals, p_total)
{
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
        ${p_data.join("")}
    </tbody>
    <tfoot>
        <tr style="background-color:#e3d3e4"><td><strong>Total</strong></td>
        <td align=right><strong>${p_total}</strong></td></tr>
    </tfoot>
    </table><br/>
    <p><strong>Number of deaths with missing (blank) values:</strong> ${p_totals.get(p_metadata.blank_field_id)}</p>
    <br/>
    <p>This data has been taken directly from the MMRIA database and is not a final report.</p>
    <br/>
    `
}


function render_chart_508_description(p_metadata, p_data, p_totals)
{
    let i = 0;
    const html = [];

    p_totals.forEach((value, key) =>
    {
        if(key != p_metadata.blank_field_id)
        {
            html.push(`${value} for ${p_metadata.field_id_list[i].title}`);
            i++;
         }
    });
    

    return `Bar chart  shows ${html.join(",")} See the table view for additional details.`;

    /*
    return `<table class="table rounded-0 mb-0" style="width:50%">
    <thead class="thead">
    <tr style="background-color:#e3d3e4;">
        <th>${p_metadata.table_title}</th>
        <th style="width:25%;text-align:right;">Number of deaths</th>
    </tr>
    </thead>
    <tbody>
        ${p_data.join("")}
    </tbody>
    <tfoot>
        <tr style="background-color:#e3d3e4"><td><strong>Total</strong></td>
        <td align=right><strong>${p_total}</strong></td></tr>
    </tfoot>
    </table><br/>
    <p><strong>Number of deaths with missing (blank) values:</strong> ${p_totals.get(p_metadata.blank_field_id)} </p>
    `*/
}

function render_chart_post_html(p_post_html, p_metadata, p_data, p_categories, p_totals, p_chart_name = "chart")
{
    p_post_html.push
    (
        `var ${p_chart_name} = c3.generate({
            legend: {
                show: false
            },
            data: {
                columns: [
                    ["${p_metadata.indicator_id}", ${p_data.join(",")}
                     ],
                ],
                types: {
                    ${p_metadata.indicator_id}: 'bar',
                },
                names: {
                    ${p_metadata.indicator_id}: "${p_metadata.x_axis_title}",
                },
                labels: true 
            },
            padding: {
                  //left: 375
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
                    categories: [${p_categories}],
                },
                y: {
                    label: {
                        text: '${p_metadata.y_axis_title}',
                        position: 'outer-center' 
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
              bindto: '#${p_chart_name}',
              
              onrendered: function()
              {
                const title_element = document.createElement("title");
                title_element.innerText = '${p_metadata.chart_title_508}';

                const description_element = document.createElement("desc");
                description_element.innerText = '${render_chart_508_description(p_metadata, p_data, p_totals)}';

                const svg_char = document.querySelector('#${p_chart_name} svg');
                

                if(svg_char != null)
                {
                    svg_char.setAttribute('alt','${p_metadata.chart_title_508}');
                    const test_title = document.querySelector('#${p_chart_name} svg title');
                    const test_desc = document.querySelector('#${p_chart_name} svg desc');

                    if(test_title == null)
                    {
                        svg_char.appendChild(title_element);
                    }

                    if(test_desc == null)
                    {
                        svg_char.appendChild(description_element);
                    }
                }
                
              }
            }); ` 
    );
}