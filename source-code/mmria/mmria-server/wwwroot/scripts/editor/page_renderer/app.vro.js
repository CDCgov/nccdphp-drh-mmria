function app_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx) 
{

    g_pinned_case_count = 0;
    
    p_result.push("<section id='app_summary'>");

    /* The Intro */
    p_result.push("<div>");

    p_result.push("<h1 class='content-intro-title h2' tabindex='-1'>Vital Records Office Line Listing Summary</h1>");
    
    
    p_result.push("<div class='row no-gutters align-items-center'>");
    
    let is_read_only_html = '';
    
    //if(g_is_data_analyst_mode)
    //{
        is_read_only_html = "disabled='disabled'";
        // is_read_only_html = "disabled='disabled'";
    //}

    p_result.push(`<button type='button' id='add-new-case' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' ${is_read_only_html}>Add New Case</button>`);

    p_result.push("<span class='spinner-container spinner-inline ml-2'><span class='spinner-body text-primary'><span class='spinner'></span></span>");
    p_result.push("</div>");
    p_result.push("</div> <!-- end .content-intro -->");

    p_result.push(`<hr class="border-top mt-4 mb-4" />`);

    p_result.push("<div class='mb-4'>");
    /* Custom Search */
    p_result.push("<div class='form-inline mb-2'>");
    p_result.push("<label for='search_text_box' class='mr-2'> Search for:</label>");
    p_result.push("<input type='text' class='form-control mr-2' id='search_text_box' onchange='g_ui.case_view_request.search_key=this.value;' value='");
    if (g_ui.case_view_request.search_key != null) 
    {
        p_result.push(p_ui.case_view_request.search_key.replace(/'/g, "&quot;"));
    }
    p_result.push("' />");

    p_post_html_render.push("$('#search_text_box').bind(\"enterKey\",function(e){");
    p_post_html_render.push("	get_case_set();");
    p_post_html_render.push(" });");
    p_post_html_render.push("$('#search_text_box').keyup(function(e){");
    p_post_html_render.push("	if(e.keyCode == 13)");
    p_post_html_render.push("	{");
    p_post_html_render.push("	$(this).trigger(\"enterKey\");");
    p_post_html_render.push("	}");
    p_post_html_render.push("});");

    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_field_selection" class="mr-2">Search in:</label>
            <select id="search_field_selection" name="search_field_selection" class="custom-select" onchange="search_field_selection_onchange(this.value)">
                ${render_field_selection(p_ui.case_view_request)}
            </select>
        </div>`
    );

    
    p_result.push("</div>");

    p_result.push(
        `<div class="form-inline mb-2">
            <table>
            <tr><td>
            <label for="search_jurisdiction" class="mr-2">Jurisdiction:</label>
            <select id="search_jurisdiction" class="custom-select" onchange="search_jurisdiction_onchange(this.value)" disabled="disabled">
                ${render_jurisdiction(p_ui.case_view_request)}
            </select>
            </td><td>
            <label for="search_yod" class="mr-2">YOD:</label>
            <select id="search_yod" class="custom-select" onchange="search_year_of_death_onchange(this.value)">
                ${render_year_of_death(p_ui.case_view_request)}
            </select>
            </td>
            <td>
            <label for="search_case_status" class="mr-2">Status:</label>
            <select id="search_case_status" class="custom-select" onchange="search_case_status_onchange(this.value)">
                ${render_case_status(p_ui.case_view_request)}
            </select>
            </td></tr>
            </table>
        </div>`
    );


    /* Case Status */
    p_result.push(
        `<div class="form-inline mb-2">

            <label for="search_classification" class="mr-2">Classification:</label>
            <select id="search_classification" class="custom-select" onchange="search_classification_onchange(this.value)">
                ${render_classification(p_ui.case_view_request)}
            </select>

        </div>`
    );
    /* Sort By: */
    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_sort_by" class="mr-2">Sort:</label>
            <select id="search_sort_by" class="custom-select" onchange="g_ui.case_view_request.sort = this.options[this.selectedIndex].value;">
                ${render_sort_by_include_in_export(p_ui.case_view_request)}
            </select>
        </div>`
    );

    /* Records per page */
    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_records_per_page" class="mr-2">Records per page:</label>
            <select id="search_records_per_page" class="custom-select" onchange="records_per_page_change(this.value);">
                ${render_filter_records_per_page(p_ui.case_view_request)}
            </select>
        </div>`
    );

    /* Descending Order */
    p_result.push(
        `<div class="form-inline mb-3">
            <label for="sort_descending" class="mr-2">Descending order:</label>
            <input id="sort_descending" name="sort_descending" type="checkbox" onchange="g_ui.case_view_request.descending = this.checked" ${p_ui.case_view_request.descending && 'checked' || ''} />
        </div>`
    );

    /* Apply Filters Btn */
    p_result.push(
        `<div class="form-inline">
            <button type="button" class="btn btn-secondary mr-2" alt="Apply filters" onclick="init_inline_loader(function(){ get_case_set() })">Apply Filters</button>
            <button type="button" class="btn btn-secondary" alt="Reset filters" id="search_command_button" onclick="init_inline_loader(function(){ clear_case_search() })">Reset</button>
            <span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>
        </div>`
    );

    p_result.push("</div> <!-- end .content-intro -->");

    let pagination_current_page = p_ui.case_view_request.page;
    const pagination_number_of_pages = Math.ceil(p_ui.case_view_request.total_rows / p_ui.case_view_request.take);
    if(pagination_number_of_pages == 0)
    {
        pagination_current_page = 0;
    }

    p_result.push("<div class='table-pagination row align-items-center no-gutters'>");
        p_result.push("<div class='col'>");
            p_result.push("<div class='row no-gutters'>");
                p_result.push("<p class='mb-0'>Total Records: ");
                    p_result.push("<strong>" + p_ui.case_view_request.total_rows + "</strong>");
                p_result.push("</p>");
                p_result.push("<p class='mb-0 ml-2 mr-2'>|</p>");
                p_result.push("<p class='mb-0'>Viewing Page(s): ");
                    p_result.push("<strong>" + pagination_current_page + "</strong> ");
                    p_result.push("of ");
                    p_result.push("<strong>" + pagination_number_of_pages + "</strong>");
                p_result.push("</p>");
            p_result.push("</div>");
        p_result.push("</div>");
        p_result.push("<div class='col row no-gutters align-items-center justify-content-end'>");
            p_result.push("<p class='mb-0'>Select by page:</p>");
            for(var current_page = 1; (current_page - 1) * p_ui.case_view_request.take < p_ui.case_view_request.total_rows; current_page++)
            {
                p_result.push("<button type='button' class='table-btn-link btn btn-link' alt='select page " + current_page + "' onclick='g_ui.case_view_request.page=");
                    p_result.push(current_page);
                    p_result.push(";get_case_set();'>");
                    p_result.push(current_page);
                p_result.push("</button>");
            }
        p_result.push("</div>");
    p_result.push("</div>");
    
    p_result.push(`
        <table class="table mb-0">
            <thead class='thead'>
                <tr class='tr bg-tertiary'>
                    <th class='th h4' colspan='7' scope='colgroup'>Case Listing</th>
                </tr>
            </thead>
            <thead class='thead'>
                <tr class='tr'>
                    <th class='th' scope='col'>Case Information</th>
                    <th class='th' scope='col'>Case Status</th>
                    <!--th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th-->
                    <th class='th' scope='col'>Created</th>
                    <th class='th' scope='col'>Last Updated</th>
                    <th class='th' scope='col'>Currently Edited By</th>
                    ${!g_is_data_analyst_mode ? `<th class='th' scope='col' style="width: 115px;">Actions</th>` : ''}
                </tr>
            </thead>
            <tbody class="tbody">
                
                ${ !g_is_data_analyst_mode ? p_ui.case_view_list.map((item, i) => render_app_pinned_summary_result(item, i)).join('') : ""}

                ${p_ui.case_view_list.map((item, i) => render_app_summary_result_item(item, i)).join('')}
            </tbody>
        </table>
    `);

    p_result.push("<div class='table-pagination row align-items-center no-gutters'>");
        p_result.push("<div class='col'>");
            p_result.push("<div class='row no-gutters'>");
                p_result.push("<p class='mb-0'>Total Records: ");
                    p_result.push("<strong>" + p_ui.case_view_request.total_rows + "</strong>");
                p_result.push("</p>");
                p_result.push("<p class='mb-0 ml-2 mr-2'>|</p>");
                p_result.push("<p class='mb-0'>Viewing Page(s): ");
                    p_result.push("<strong>" + pagination_current_page + "</strong> ");
                    p_result.push("of ");
                    p_result.push("<strong>" + pagination_number_of_pages + "</strong>");
                p_result.push("</p>");
            p_result.push("</div>");
        p_result.push("</div>");
        p_result.push("<div class='col row no-gutters align-items-center justify-content-end'>");
            p_result.push("<p class='mb-0'>Select by page:</p>");
            for(var current_page = 1; (current_page - 1) * p_ui.case_view_request.take < p_ui.case_view_request.total_rows; current_page++) 
            {
                p_result.push("<button type='button' class='table-btn-link btn btn-link' alt='select page " + current_page + "' onclick='g_ui.case_view_request.page=");
                    p_result.push(current_page);
                    p_result.push(";get_case_set();'>");
                    p_result.push(current_page);
                p_result.push("</button>");
            }
        p_result.push("</div>");
    p_result.push("</div>");

    p_result.push("</section>");

    if (p_ui.url_state.path_array.length > 1) 
    {
        if(p_ui.url_state.path_array[1] == "field_search")
        {
            var search_text = p_ui.url_state.path_array[2].replace(/%20/g, " ");
            p_result.push("<section id='field_search_id'>");
            let is_case_read_only = false;
            let is_checked_out = is_case_checked_out(g_data);
            let case_is_locked = is_case_locked(g_data);


            if(case_is_locked || g_is_data_analyst_mode)
            {
                is_case_read_only = true;
            }
            else if(!is_checked_out)
            {
                is_case_read_only = true;
            }

            quick_edit_header_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, { search_text: search_text, is_read_only: is_case_read_only });
            
            var search_text_context = get_seach_text_context(p_result, [], p_metadata, p_data, p_dictionary_path, p_metadata_path, p_object_path, search_text, is_case_read_only);

            render_search_text(search_text_context);

//            p_post_html_render = search_text_context.post_html_render;
            Array.prototype.push.apply(p_post_html_render, search_text_context.post_html_render);
/*
            var search_ctx = { search_text: search_text, is_match: false };
            for (var i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i]

                if(child.type.toLowerCase() == "form")
                {
                    let child_data = p_data[child.name]
                
                    Array.prototype.push.apply(p_result, page_render(child, child_data, p_ui, p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name, false, p_post_html_render, search_ctx));
                }
            }
            
*/
            p_result.push("</section>");
        }
        else
        {
            for (var i = 0; i < p_metadata.children.length; i++) 
            {
                var child = p_metadata.children[i];

                if (child.type.toLowerCase() == 'form' && p_ui.url_state.path_array[1] == child.name) 
                {
                    if (p_data[child.name] || p_data[child.name] == 0) 
                    {
                        // do nothing 
                    }
                    else 
                    {
                        p_data[child.name] = create_default_object(child, {})[child.name];
                    }

                    //Array.prototype.push.apply(p_result, page_render(child, p_data[child.name], p_ui, p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name, false, p_post_html_render));
                    const page_render_array = page_render(child, p_data[child.name], p_ui, p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name, false, p_post_html_render);
                    for(let j = 0; j < page_render_array.length; j++)
                    {
                        p_result.push(page_render_array[j]);
                    }
                }
            }
        }
    }
}

function render_sort_by_include_in_export(p_sort)
{
	const sort_list = [
        {
            value : 'by_date_created',
            display : 'By date created'
        },
        {
            value : 'by_date_last_updated',
            display : 'By date last updated'
        },
        {
            value : 'by_pmssno',
            display : 'PMSS#'
        },
        {
            value : 'by_death_certificate_number',
            display : 'Death certificate number'
        },
        {
            value : 'by_dod',
            display : 'Date of Death (calculated)'
        },
        {
            value : 'by_dob',
            display : 'Date of Birth (calculated)'
        },
        {
            value : 'by_reszip',
            display : 'Zip code of residence'
        },
        {
            value : 'by_mage',
            display : 'Maternal age at Death'
        },
        {
            value : 'by_manner',
            display : 'Manner'
        },
        {
            value : 'by_cod',
            display : 'Cause of Death'
        },
        {
            value : 'by_cod_other_condition',
            display : 'Cause of Death Part II'
        },
        {
            value : 'by_jurisdiction',
            display : 'Jurisdiction'
        },
        {
            value : 'by_track_year',
            display : 'Year of Death'
        },
        {
            value : 'by_case_status',
            display : 'Status'
        },
        {
            value : 'by_classification',
            display : 'Classification'
        },
        {
            value : 'by_created_by',
            display : 'Created By'
        },
        {
            value : 'by_last_updated_by',
            display : 'Last Updated By'
        }
	];

    const f_result = [];

	sort_list.map((item) => {
        f_result.push(`<option value="${item.value}" ${item.value === p_sort.sort ? 'selected' : ''}>${item.display}</option>`);
    });

	return f_result.join(''); 
}

function render_field_selection(p_sort)
{
	const sort_list = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : 'by_pmssno',
            display : 'PMSS#'
        },
        {
            value : 'by_death_certificate_number',
            display : 'Death certificate number'
        },
        {
            value : 'by_dod',
            display : 'Date of Death (calculated)'
        },
        {
            value : 'by_dob',
            display : 'Date of Birth (calculated)'
        },
        {
            value : 'by_reszip',
            display : 'Zip code of residence'
        },
        {
            value : 'by_mage',
            display : 'Maternal age at Death'
        },
        {
            value : 'by_manner',
            display : 'Manner'
        },
        {
            value : 'by_cod',
            display : 'Cause of Death'
        },
        {
            value : 'by_cod_other_condition',
            display : 'Cause of Death Part II'
        },
        {
            value : 'by_jurisdiction',
            display : 'Jurisdiction'
        },
        {
            value : 'by_track_year',
            display : 'Year of Death'
        },
        {
            value : 'by_case_status',
            display : 'Status'
        },
        {
            value : 'by_classification',
            display : 'Classification'
        },
        {
            value : 'by_date_created',
            display : 'Date Created'
        },
        {
            value : 'by_date_last_updated',
            display : 'Date Last Updated'
        },
        {
            value : 'by_created_by',
            display : 'Created By'
        },
        {
            value : 'by_last_updated_by',
            display : 'Last Updated By'
        }
	];

    const f_result = [];

	sort_list.map((item) => {
       f_result.push(`<option value="${item.value}" ${item.value === p_sort.field_selection ? 'selected' : ''}>${item.display}</option>`);
    });

	return f_result.join('');
}

function render_jurisdiction(p_case_view)
{
    const lookup_value = eval(convert_dictionary_path_to_lookup_object("lookup/state"));
    const values = [];
    
    values.push(       {
        value : 'all',
        display : '-- All --'
    });

    lookup_value.map((item)=> values.push({
        value : item.value,
        display : item.display
    }));
    const list = [];

    values.map((status, i) => {

        if(status.value == 'all' && g_user_role_jurisdiction_list[0] == '/')
        {
            return list.push(`<option value="${status.value}" ' selected '}>Top Folder</option>`);
        }
        else
        {
            return list.push(`<option value="${status.value}" ${"/" + status.value == g_user_role_jurisdiction_list[0] ? ' selected ' : ''}>${status.display}</option>`);
        }

        
    });

    return list.join(''); 
}

function render_year_of_death(p_case_view)
{
		const lookup_value = eval(convert_dictionary_path_to_lookup_object("lookup/year"));
    
        const values = [];
        
        values.push(       {
            value : 'all',
            display : '-- All --'
        });
    
        lookup_value.map((item)=> values.push({
            value : item.value,
            display : item.display
        }));

        const list = [];
    
        values.map((status, i) => {
    
            return list.push(`<option value="${status.value}" ${status.value == p_case_view.year_of_death ? ' selected ' : ''}>${status.display}</option>`);
        });
    
        return list.join(''); 
}

function render_case_status(p_case_view)
{
    const lookup_value = eval(convert_dictionary_path_to_lookup_object("lookup/case_status"));
    
    const values = [];
    
    values.push(       {
        value : 'all',
        display : '-- All --'
    });

    lookup_value.map((item)=> values.push({
        value : item.value,
        display : item.display
    }));

    const list = [];

    values.map((status, i) => {

        if
        (
            status.value.indexOf("STEVE: Pending VRO Investigation") == 0 ||
            status.value == "all"
            
        )
        return list.push(`<option value="${status.value}" ${status.value == g_ui.case_view_request.status ? ' selected ' : ''}>${status.display}</option>`);
    });

    return list.join(''); 
}


function render_classification(p_case_view)
{
    const index_list = [];
    
    function find_form(item, i)
    { 
        if(item.name== "cause_of_death")
            index_list.push(i);
    }

    function find_field(item, i)
    { 
        if(item.name== "class")
            index_list.push(i);
    }

    g_metadata.children.map(find_form);

    g_metadata.children[index_list[0]].children.map(find_field);

    const lookup_value = g_metadata.children[index_list[0]].children[index_list[1]].values
    const values = [];
        
    values.push(       {
        value : 'all',
        display : '-- All --'
    });

    lookup_value.map((item)=> values.push({
        value : item.value,
        display : item.display
    }));

    const list = [];

	values.map((status, i) => {

        return list.push(`<option value="${status.value}" ${status.value == p_case_view.classification ? ' selected ' : ''}>${status.display}</option>`);
    });

	return list.join(''); 
}


function render_filter_records_per_page(p_sort)
{
    const sort_list = [25, 50, 100, 250, 500, 1000];
    const f_result = [];

    sort_list.map((item) => {
        f_result.push(`<option value="${item}" ${item == p_sort.take ? 'selected' : ''}>${item}</option>`)
    });

    return f_result.join('');
}

function clear_case_search() 
{
    g_ui.case_view_request.search_key = '';
    g_ui.case_view_request.sort = 'by_date_created';
    g_ui.case_view_request.jurisdiction = 'all'
    g_ui.case_view_request.year_of_death = 'all';
    g_ui.case_view_request.status = 'all';
    g_ui.case_view_request.classification = 'all';
    g_ui.case_view_request.field_selection = 'all';
    g_ui.case_view_request.descending = true;
    g_ui.case_view_request.take = 100;
    g_ui.case_view_request.page = 1;
    g_ui.case_view_request.skip = 0;
    g_ui.case_view_list = [];

    get_case_set();
}

function search_year_of_death_onchange(p_value)
{
    if(g_ui.case_view_request.year_of_death != p_value)
    {
        g_ui.case_view_request.year_of_death = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
}


function search_case_status_onchange(p_value)
{
    if(g_ui.case_view_request.status != p_value)
    {
        g_ui.case_view_request.status = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
}

function search_jurisdiction_onchange(p_value)
{
    if(g_ui.case_view_request.jurisdiction != p_value)
    {
        g_ui.case_view_request.jurisdiction = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
    
}

function search_classification_onchange(p_value)
{
    if(g_ui.case_view_request.classification != p_value)
    {
        g_ui.case_view_request.classification = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
    
}


function search_field_selection_onchange(p_value)
{
    if(g_ui.case_view_request.field_selection != p_value)
    {
        g_ui.case_view_request.field_selection = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
    
}

function records_per_page_change(p_value)
{
    if(p_value != g_ui.case_view_request.take)
    {
        g_ui.case_view_request.take = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
}


function app_is_item_pinned(p_id)
{
    var is_pin = 0;
    
    if
    (
        g_pinned_case_set!= null && 
        Object.hasOwn(g_pinned_case_set, 'list')
    )
    {
        if(Object.hasOwn(g_pinned_case_set.list, 'everyone'))
        {
            if(g_pinned_case_set.list.everyone.indexOf(p_id) != -1)
            {
                is_pin = 2;
            }
        }

        if(is_pin == 0)
        {
            if(Object.hasOwn(g_pinned_case_set.list, g_user_name))
            {
                if(g_pinned_case_set.list[g_user_name].indexOf(p_id) != -1)
                {
                    is_pin = 1;
                }
            }
        }
    }

    return is_pin;
}

function render_pin_un_pin_button
(
    p_case_view_item,
    p_is_checked_out,
    p_is_checked_out_expired,
    p_delete_enabled_html
)
{


    const is_pinned = app_is_item_pinned(p_case_view_item.id);

    if(is_pinned == 0)
    {
        return `
    
    <input type="image" src="../img/icon_pin.png" title="Pin this case." alt="Pin this case." style="width:16px;height:32px;vertical-align:middle;" onclick="pin_case_clicked('${p_case_view_item.id}')"/>
    
    `;
    }
    else if(is_pinned == 1)
    {
        return `
    
    <input type="image" src="../img/icon_unpin.png"  title="Unpin this case." alt="Unpin this case." style="width:16px;height:32px;vertical-align:middle;" onclick="unpin_case_clicked('${p_case_view_item.id}')"/>
    
    `;
    }
    else
    {
        let click_event = ` onclick="unpin_case_clicked('${p_case_view_item.id}')" `;
        let cursor_pointer = "";
        if(is_pinned == 2 && g_is_jurisdiction_admin == false)
        {
            cursor_pointer = "disabled=disabled";
            click_event = "";
        }


        return `
    
    <input type="image" src="../img/icon_unpinMultiple.png" title="Unpin this case." alt="Unpin this case." style="width:16px;height:32px;vertical-align:middle;" ${cursor_pointer} ${click_event}/>
    
    `;
    }

}



function render_app_summary_result_item(item, i)
{

    if(app_is_item_pinned(item.id) != 0)
    {
        return "";
    }

    let is_checked_out = is_case_checked_out(item.value);
    let case_is_locked = is_case_view_locked(item.value);
    // let checked_out_html = ' [not checked out] ';
    let checked_out_html = '';
    let delete_enabled_html = ''; 

    if(case_is_locked || g_is_data_analyst_mode)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(is_checked_out)
    {
        // checked_out_html = ' [checked out by you] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else  if(!is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }

    delete_enabled_html = ' disabled = "disabled" ';

    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.case_folder;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const lockedBy = item.value.last_checked_out_by;
    const currentCaseStatus = app_get_case_status_value_to_display(item.value.status)
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    
    const track_year = item.value.track_year;
    const pmssno = item.value.pmssno
    const jurisdiction = item.value.jurisdiction
    const med_coder_check = item.value.med_coder_check
    const med_dir_check = item.value.med_dir_check
    const death_certificate_number = item.value.death_certificate_number
    const status = item.value.status
    const agreement_status = item.value.agreement_status
    const dod = item.value.dod
    const dob = item.value.dob
    const residence_zip = item.value.residence_zip



    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;


    return (
    `<tr class="tr" path="${caseID}">
        <td class="td"><a href="#/${i}/${case_listing_get_start_form()}">${get_header_listing_name(item, jurisdiction)}</a>
            ${checked_out_html}</td>
        <td class="td" scope="col">${currentCaseStatus}</td>
        <!--td class="td">${reviewDates}</td-->
        <td class="td">${createdBy} - ${dateCreated}</td>
        <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
        <td class="td">
            ${is_checked_out ? (`
            <span class="icn-info">${lockedBy}</span>
            `) : ''}
            ${!is_checked_out && !is_checked_out_expired(item.value) ? (`
            <span class="row no-gutters align-items-center">
                <span class="icn icn--round icn--border bg-primary" title="Case is locked"><span class="d-flex x14 fill-w cdc-icon-lock-alt"></span></span>
                <span class="icn-info">${lockedBy}</span>
            </span>
            `) : ''}
        </td>
        ${!g_is_data_analyst_mode ? (
            `<td class="td">       
                <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15" ${delete_enabled_html}>Delete</button>

                ${render_pin_un_pin_button
                    (
                        item, 
                        is_checked_out, 
                        is_checked_out_expired(item.value),
                        delete_enabled_html
                    )
                }
                </td>`
            ) : ''}
        </tr>`
    );


}


function render_app_pinned_summary_result(item, i)
{
    if(app_is_item_pinned(item.id) == 0)
    {
        return "";
    }

    let is_checked_out = is_case_checked_out(item.value);
    let case_is_locked = is_case_view_locked(item.value);
    // let checked_out_html = ' [not checked out] ';
    let checked_out_html = '';
    let delete_enabled_html = ''; 

    if(case_is_locked || g_is_data_analyst_mode)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(is_checked_out)
    {
        // checked_out_html = ' [checked out by you] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else  if(!is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }

    delete_enabled_html = ' disabled = "disabled" ';
 
    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.case_folder;
    const jurisdiction = item.value.jurisdiction;
    const pmssno = item.value.pmssno ? `- (${item.value.pmssno})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const lockedBy = item.value.last_checked_out_by;
    const currentCaseStatus = app_get_case_status_value_to_display(item.value.status);
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    g_pinned_case_count += 1;

    let border_bottom_color = ""
    if(g_pinned_case_count == mmria_count_number_pinned())
    {
        border_bottom_color = 'style="border-bottom-color: #712177;border-bottom-width:2px"';
    }

    return (
    `<tr class="tr" path="${caseID}" style="background-color: #f7f2f7;">
        <td class="td" ${border_bottom_color}><a href="#/${i}/${case_listing_get_start_form()}">${get_header_listing_name(item, jurisdiction)}</a>
            ${checked_out_html}</td>
        <td class="td" scope="col" ${border_bottom_color}>${currentCaseStatus}</td>
        <!--td class="td" ${border_bottom_color}>${reviewDates}</td-->
        <td class="td" ${border_bottom_color}>${createdBy} - ${dateCreated}</td>
        <td class="td" ${border_bottom_color}>${lastUpdatedBy} - ${lastUpdatedDate}</td>
        <td class="td" ${border_bottom_color}>
            ${is_checked_out ? (`
            <span class="icn-info">${lockedBy}</span>
            `) : ''}
            ${!is_checked_out && !is_checked_out_expired(item.value) ? (`
            <span class="row no-gutters align-items-center">
                <span class="icn icn--round icn--border bg-primary" title="Case is locked"><span class="d-flex x14 fill-w cdc-icon-lock-alt"></span></span>
                <span class="icn-info">${lockedBy}</span>
            </span>
            `) : ''}
        </td>
        ${!g_is_data_analyst_mode ? (
            `<td class="td" ${border_bottom_color}>
                <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15" ${delete_enabled_html}>Delete</button>
                
                ${render_pin_un_pin_button
                    (
                        item, 
                        is_checked_out, 
                        is_checked_out_expired(item.value),
                        delete_enabled_html
                    )
                }
                </td>`
            ) : ''}
        </tr>`
    );
}

async function pin_case_clicked(p_id)
{
    if(g_is_jurisdiction_admin)
    {
        $mmria.pin_un_pin_dialog_show(p_id, true);
    }
    else
    {
        await mmria_pin_case_click(p_id, false)
    }
}

async function unpin_case_clicked(p_id)
{
    if(g_is_jurisdiction_admin && app_is_item_pinned(p_id) != 1)
    {
        $mmria.pin_un_pin_dialog_show(p_id, false);
    }
    else
    {
        await mmria_un_pin_case_click(p_id, false)
    }
}


function get_header_listing_name
(
    p_item, 
    p_jurisdiction
)
{
	const metadata_value_list = eval(convert_dictionary_path_to_lookup_object("lookup/state"));
	let display_name = p_jurisdiction;
	for(const element of metadata_value_list)
	{
		if( element.value == p_jurisdiction)
		{
			const start_index = element.display.indexOf("(");
			const last_index = element.display.indexOf(")");

			display_name = element.display.substring(start_index + 1, last_index);

			break;
		}
	}	
	
	return `${display_name} - ${p_item.value.track_year} - ${p_item.value.death_certificate_number} (${p_item.value.pmssno})`;
}


function app_get_case_status_value_to_display(p_value)
{
    let result = p_value;
    const lookup_value = eval(convert_dictionary_path_to_lookup_object("lookup/case_status"));
    lookup_value.map((item)=> {
        if(item.value == p_value)
        { 
            result = item.display
        }
    
    } );

    return result; 
}


function app_get_lookup_value_to_display(p_field_name, p_value)
{
    let result = p_value;
    const lookup_value = eval(convert_dictionary_path_to_lookup_object(`lookup/${p_field_name}`));
    lookup_value.map((item)=> {
        if(item.value == p_value)
        { 
            result = item.display
        }
    
    } );

    return result; 
}

function case_listing_get_start_form()
{
    if
    (
        role_set.size == 1 &&
        role_set.has("vro")
    )
    {
        return "vro_case_determination"
    }

    return  "tracking";
}