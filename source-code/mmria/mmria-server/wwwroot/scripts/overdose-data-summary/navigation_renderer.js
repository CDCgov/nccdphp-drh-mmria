

function render_navigation_strip(p_current_index)
{
    if(p_current_index < 1)
    {
        return "";
    }

    const previous_index = p_current_index - 1;
    const next_index = p_current_index + 1;


    const previous_tab_name = g_nav_map.get(previous_index);
    const next_tab_name = g_nav_map.get(next_index);

    let list_options = [];

    g_nav_map.forEach
    (
        (value, index) =>
        {
            if
            (
                -1 < index && index <= 12
            )
            {
                if(index == p_current_index)
                {
                    list_options.push(`<option selected value="${index}">${value}</option>`)
                }
                else
                {
                    list_options.push(`<option value="${index}">${value}</option>`)
                }
            }
        }
    );

    if(next_index < 13)
    {
        return `
        <!--p align=center>
            <span  class="spinner-container spinner-inline ml-2"  style="display: inline">
                <span class="spinner-body text-primary">
                    <span class="spinner"></span>
                    <span class="spinner-info"></span>
                </span>
            </span>
        </p-->
        <nav role="navigation" aria-label="Previous and Next Pages" class="d-flex mt-3 col-md-12 pr-0">
            <div class="bottom-nav align-items-center col-md-12">
                <div class="pl-2 col-md-4">
                    <a class="d-flex align-items-center" href="#${previous_index}" title="Previous Page"><span class="x24 cdc-icon-chevron-right reverse pl-1"></span><span class="pt-1">${previous_tab_name}</span></a>
                </div>
                <div style="margin-bottom: 0px !important;" class="horizontal-control col-md-4">
                    <label>Report:</label>
                    <select aria-label="Select Report" class="form-control form-select" onchange="nav_dropdown_change(this.value)">
                        ${list_options.join()}
                    </select>
                </div>
                <div class="pr-2 col-md-4 d-flex justify-content-end">
                    <a class="d-flex align-items-center" href="#${next_index}" title="Next Page"><span class="pb-1">${next_tab_name}</span><span class="x24 cdc-icon-chevron-right pl-1"></span></a>
                </div>
            </div>
        </nav>

    `;
    }
    else
    {
        return `
        <!--p align=center>
            <span  class="spinner-container spinner-inline ml-2"  style="display: inline">
                <span class="spinner-body text-primary">
                    <span class="spinner"></span>
                    <span class="spinner-info"></span>
                </span>
            </span>
        </p-->
        <nav role="navigation" aria-label="Previous and Next Pages" class="d-flex mt-3 col-md-12 pr-0">
            <div class="bottom-nav align-items-center col-md-12">
                <div class="pl-2 col-md-4">
                    <a class="d-flex align-items-center" href="#${previous_index}" title="Previous Page"><span class="x24 cdc-icon-chevron-right reverse pl-1"></span><span class="pt-1">${previous_tab_name}</span></a>
                </div>
                <div style="margin-bottom: 0px !important;" class="horizontal-control col-md-4">
                    <label>Report:</label>
                    <select aria-label="Select Report" class="form-control form-select" onchange="nav_dropdown_change(this.value)">
                        ${list_options.join()}
                    </select>
                </div>
                <div class="col-md-4">&nbsp;</div>
            </div>
        </nav>

    `;
    }

}

function nav_dropdown_change(p_value)
{
    window.location = "#" + p_value;
}


