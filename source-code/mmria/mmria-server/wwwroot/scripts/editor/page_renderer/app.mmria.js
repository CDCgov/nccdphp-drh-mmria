function app_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx) 
{
    if (window.location.hash == '')
      window.location.hash = "#/summary";
    g_pinned_case_count = 0;
    
    p_result.push("<section id='app_summary'>");

    /* The Intro */
    p_result.push("<div>");
    p_result.push("<h1 class='content-intro-title h2' tabindex='-1'>");
    g_is_data_analyst_mode ? p_result.push("Analyst ") : p_result.push("Abstractor ");
    p_result.push("Line Listing Summary</h1>");
    p_result.push("<div class='row no-gutters align-items-center'>");
    
    let is_read_only_html = '';
    
    if(g_is_data_analyst_mode)
    {
        is_read_only_html = "disabled='disabled'";
        // is_read_only_html = "disabled='disabled'";
    }

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

    /* Case Status */
    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_case_status" class="mr-2">Case Status:</label>
            <select id="search_case_status" class="custom-select" onchange="search_case_status_onchange(this.value)">
                ${renderSortCaseStatus(p_ui.case_view_request)}
            </select>
        </div>`
    );
    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_pregnancy_relatedness" class="mr-2">Pregnancy Relatedness:</label>
            <select id="search_pregnancy_relatedness" class="custom-select" onchange="search_pregnancy_relatedness_onchange(this.value)">
                ${renderPregnancyRelatedness(p_ui.case_view_request)}
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
            <button type="button" class="btn btn-secondary mr-2" alt="Apply filters" onclick="init_inline_loader(async function(){ await apply_filter_click() })">Apply Filters</button>
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
                <tr class='tr'>
                    <th class='th' scope='col'>Case Information</th>
                    <th class='th' scope='col'>Case Status</th>
                    <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
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
            value : 'by_last_name',
            display : 'By last name'
        },
        {
            value : 'by_first_name',
            display : 'By first name'
        },
        {
            value : 'by_middle_name',
            display : 'By middle name'
        },
        {
            value : 'by_year_of_death',
            display : 'By year of death'
        },
        {
            value : 'by_month_of_death',
            display : 'By month of death'
        },
        {
            value : 'by_committee_review_date',
            display : 'By committee review date'
        },
        {
            value : 'by_created_by',
            display : 'By created by'
        },
        {
            value : 'by_last_updated_by',
            display : 'By last updated by'
        },
        {
            value : 'by_state_of_death',
            display : 'By state of death'
        },
        {
            value : 'by_agency_case_id',
            display : 'By agency-based case identifier'
        },
        {
            value : 'by_record_id',
            display : 'By Record id'
        },
        {
            value : 'by_pregnancy_relatedness',
            display : 'By pregnancy relatedness'
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
            value : 'by_agency_case_id',
            display : 'Agency-Based Case Identifier'
        },
        {
            value : 'by_record_id',
            display : 'Record Id'
        },
        {
            value : 'by_last_name',
            display : 'Last Name'
        },
        {
            value : 'by_first_name',
            display : 'First Name'
        },
        {
            value : 'by_middle_name',
            display : 'Middle Name'
        },
        {
            value : 'by_state_of_death',
            display : 'State of Death'
        },
        {
            value : 'by_year_of_death',
            display : 'Year of Death'
        },
        {
            value : 'by_month_of_death',
            display : 'Month of Death'
        },
        {
            value : 'by_committee_review_date',
            display : 'Committee Review Date'
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

function renderSortCaseStatus(p_case_view)
{
	const sortCaseStatuses = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : '9999',
            display : '(blank)'
        },
        ,
        {
            value : '1',
            display : 'Abstracting (incomplete)'
        },
        {
            value : '2',
            display : 'Abstraction Complete'
        },
        {
            value : '3',
            display : 'Ready For Review'
        },
        {
            value : '4',
            display : 'Review complete and decision entered'
        },
        {
            value : '5',
            display : 'Out of Scope and death certificate entered'
        },
        {
            value : '6',
            display : 'False Positive and death certificate entered'
        },
        {
            value : '0',
            display : 'Vitals Import'
        },
    ];
    const sortCaseStatusList = [];

	sortCaseStatuses.map((status, i) => {

        return sortCaseStatusList.push(`<option value="${status.value}" ${status.value == p_case_view.case_status ? ' selected ' : ''}>${status.display}</option>`);
    });

	return sortCaseStatusList.join('');
}


function renderPregnancyRelatedness(p_case_view)
{
	const sortCaseStatuses = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : '9999',
            display : '(blank)'
        },
        ,
        {
            value : '1',
            display : 'Pregnancy-related'
        },
        {
            value : '0',
            display : 'Pregnancy-Associated, but NOT-Related'
        },
        {
            value : '2',
            display : 'Pregnancy-Associated, but unable to Determine Pregnancy-Relatedness'
        },
        {
            value : '99',
            display : 'Not Pregnancy-Related or -Associated (i.e. False Positive)'
        }
    ];
    const sortCaseStatusList = [];

	sortCaseStatuses.map((status, i) => {

        return sortCaseStatusList.push(`<option value="${status.value}" ${status.value == p_case_view.pregnancy_relatedness ? ' selected ' : ''}>${status.display}</option>`);
    });

	return sortCaseStatusList.join(''); 
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
    g_ui.case_view_request.case_status = 'all'
    g_ui.case_view_request.pregnancy_relatedness = 'all';
    g_ui.case_view_request.field_selection = 'all';
    g_ui.case_view_request.descending = true;
    g_ui.case_view_request.take = 100;
    g_ui.case_view_request.page = 1;
    g_ui.case_view_request.skip = 0;
    g_ui.case_view_list = [];

    get_case_set();
}

function search_case_status_onchange(p_value)
{
    if(g_ui.case_view_request.case_status != p_value)
    {
        g_ui.case_view_request.case_status = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
}

function search_pregnancy_relatedness_onchange(p_value)
{
    if(g_ui.case_view_request.pregnancy_relatedness != p_value)
    {
        g_ui.case_view_request.pregnancy_relatedness = p_value;
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

    
    const caseStatuses = {
        "9999":"(blank)",	
        "1":"Abstracting (Incomplete)",
        "2":"Abstraction Complete",
        "3":"Ready for Review",
        "4":"Review Complete and Decision Entered",
        "5":"Out of Scope and Death Certificate Entered",
        "6":"False Positive and Death Certificate Entered",
        "0":"Vitals Import"
    }; 
    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const lockedBy = item.value.last_checked_out_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[item.value.case_status.toString()];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;


    return (
    `<tr class="tr" path="${caseID}">
        <td class="td"><a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
            ${checked_out_html}</td>
        <td class="td">${currentCaseStatus}</td>
        <td class="td">${reviewDates}</td>
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

    
    const caseStatuses = {
        "9999":"(blank)",	
        "1":"Abstracting (Incomplete)",
        "2":"Abstraction Complete",
        "3":"Ready for Review",
        "4":"Review Complete and Decision Entered",
        "5":"Out of Scope and Death Certificate Entered",
        "6":"False Positive and Death Certificate Entered",
        "0":"Vitals Import"
    }; 
    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const lockedBy = item.value.last_checked_out_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[item.value.case_status.toString()];
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
        <td class="td" ${border_bottom_color}><a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
            ${checked_out_html}</td>
        <td class="td" ${border_bottom_color}>${currentCaseStatus}</td>
        <td class="td" ${border_bottom_color}>${reviewDates}</td>
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