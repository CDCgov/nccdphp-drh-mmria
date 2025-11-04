function render0() {
    return `
        ${render_header(0)}
        <h3 class="h4 font-weight-bold">Overview</h3>
        <p>
            The Aggregate Report can provide quick analysis for questions asked by committees or team leadership and provide areas to consider more thoroughly during analysis. This report can be used to look at broad categories of pregnancy-associated deaths within MMRIA but should not replace more specific analysis. For example, this report is only able to show race/ethnicity as non-Hispanic Black, non-Hispanic White, Hispanic, and Other while an individual jurisdiction can look at other race/ethnicity groupings after downloading the data.
        </p>
        <p>Select a page in the table below</p>
        <table class="table hover nein-scroll">
            <thead>
                <tr class="header-level-2">
                    <th width="50">Number</th>
                    <th width="295">Report Page</th>
                    <th>Description</th>
                </tr>
            </thead>
            <tbody>
                ${[1,2,3,4,5,6,7,8,9,10,11,12].map(i => `
                    <tr onclick="window.location='#${i}'" style="cursor: pointer;">
                        <td><strong>${i}</strong></td>
                        <td><strong><a style="text-decoration: underline;" href="#${i}">${indicator_map.get(i).title || getTitle(i)}</a></strong></td>
                        <td>${indicator_map.get(i).description}</td>
                    </tr>
                `).join('')}
            </tbody>
        </table>
    `;
    // Helper function for titles if not present in indicator_map
    function getTitle(i) {
        const titles = [
            "", "Primary Underlying Cause of Death", "Pregnancy-Relatedness", "Preventability",
            "Timing of Death", "OMB Race Recode", "Race", "Race/Ethnicity", "Age", "Education",
            "Committee Determinations", "Emotional Stress", "Living Arrangements"
        ];
        return titles[i] || "";
    }
}

function render_table(p_metadata, p_data, p_totals, p_total, p_disable_blank) {
    let blank_html = `<p><strong>Number of deaths with missing (blank) values:</strong> ${p_totals.get(p_metadata.blank_field_id)}</p>`;
    if (p_disable_blank != null || p_disable_blank === true) {
        blank_html = '';
    }
    return `
        <table class="table" style="width:50%"
            title="${p_metadata.table_title_508 != null ? p_metadata.table_title_508.replace("'", "") : ""}">
            <thead class="thead">
                <tr class="header-level-2">
                    <th>${proper_casing(p_metadata.table_title)}</th>
                    <th style="width:25%;text-align:right;">Number of Deaths</th>
                </tr>
            </thead>
            <tbody>
                ${p_data.join("")}
            </tbody>
            <tfoot>
                <tr class="table-footer">
                    <td><strong>Total</strong></td>
                    <td align="right"><strong>${p_total}</strong></td>
                </tr>
            </tfoot>
        </table>
        <br/>
        <div style="border-color: #d5d5d5 !important;" class="d-flex border rounded border-light text-left p-2 mt-2 mb-4 col-md-6"><span class="font-weight-bold">Number of Deaths with missing (blank) values:</span><span class="ml-auto">${p_totals.get(p_metadata.blank_field_id)}</span></div>
        <i>This data has been taken directly from the MMRIA database and is not a final report.</i>
    `;
}

function render_chart_508_description(p_metadata, p_data, p_totals) {
    let i = 0;
    const html = [];
    p_totals.forEach((value, key) => {
        if (key !== p_metadata.blank_field_id) {
            html.push(`${value} for ${p_metadata.field_id_list[i].title}`);
            i++;
        }
    });
    return `Bar chart shows ${html.join(", ")}. See the table view for additional details.`;
}

function render_chart_post_html(p_chart_height, p_post_html, p_metadata, p_data, p_categories, p_totals, p_chart_name = "chart") {
    p_post_html.push(`
        var ${p_chart_name} = c3.generate({
            legend: { show: false },
            data: {
                columns: [
                    ["${p_metadata.indicator_id}", ${p_data.join(",")}],
                ],
                types: { ${p_metadata.indicator_id}: 'bar' },
                names: { ${p_metadata.indicator_id}: "${p_metadata.x_axis_title}" },
                labels: true,
                colors: { ${p_metadata.indicator_id}: '${BAR_CHART_COLOR}' }
            },
            padding: {
                bottom: 20,
            },
            axis: {
                rotated: true,
                x: {
                    label: { text: '${proper_casing(p_metadata.x_axis_title)}', position: 'outer-middle' },
                    tick: { multiline: false, culling: false, outer: false },
                    type: 'category',
                    categories: [${p_categories}],
                },
                y: {
                    label: { text: '${proper_casing(p_metadata.y_axis_title)}', position: 'outer-center' },
                }
            },
            size: {
                height: ${p_chart_height},
            },
            transition: { duration: null },
            bindto: '#${p_chart_name}',
            onrendered: function() {
                const title_element = document.createElement("title");
                title_element.innerText = '${p_metadata.chart_title_508}';
                const description_element = document.createElement("desc");
                description_element.innerText = '${render_chart_508_description(p_metadata, p_data, p_totals)}';
                const svg_char = document.querySelector('#${p_chart_name} svg');
                if (svg_char != null) {
                    svg_char.setAttribute('alt', '${p_metadata.chart_title_508}');
                    if (!svg_char.querySelector('title')) {
                        svg_char.appendChild(title_element);
                    }
                    if (!svg_char.querySelector('desc')) {
                        svg_char.appendChild(description_element);
                    }
                }
            }
        });
    `);
}
