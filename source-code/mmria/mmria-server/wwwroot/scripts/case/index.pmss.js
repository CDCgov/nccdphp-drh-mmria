var g_ui = {
    url_state: {
      selected_form_name: null,
      selected_id: null,
      selected_child_id: null,
      path_array: [],
    },
  
    data_list: [],
  
    broken_rules: {},
  
    set_value: function (p_path, p_value) 
    {
      //console.log('g_ui.set_value: ', p_path, p_value);
      //console.log('value: ', p_value.value);
      //console.log('get_eval_string(p_path): ', g_ui.get_eval_string(p_path));
  
      eval
      (
        g_ui.get_eval_string
        (
          p_path + ' = "' + p_value.value.replace('"', '\\"') + '"'
        )
      );
    },
  
    get_eval_string: function (p_path) 
    {
      var result =
        'g_data' +
        p_path
          .replace(new RegExp('/', 'gm'), '.')
          .replace(new RegExp('\\.(\\d+)\\.', 'g'), '[$1]\\.')
          .replace(new RegExp('\\.(\\d+)$', 'g'), '[$1]');
  
      return result;
    },
  
    add_new_case: async function (
      p_year_of_death,
      p_state_of_death
    ) 
    {
      if (g_autosave_interval != null) 
      {
        window.clearInterval(g_autosave_interval);
      }
  
      
  
      g_autosave_interval = window.setInterval(autosave, 10000);
  
      var result = create_default_object(g_metadata, {});
  
      result.date_created = new Date();
      result.created_by = g_user_name;
      result.date_last_updated = new Date();
      result.last_updated_by = g_user_name;
      result.date_last_checked_out = new Date();
      result.last_checked_out_by = g_user_name;
      result.version = g_release_version;
      result.tracking.admin_info.status = "Incomplete";
      result.tracking.admin_info.steve_transfer = 2;
  
      if (g_jurisdiction_list.length > 0) 
      {
        result.tracking.admin_info.case_folder = "/" + p_state_of_death;
      } 
      else 
      {
        result.tracking.admin_info.case_folder = '/';
      }
  
      result.tracking.admin_info.jurisdiction = p_state_of_death;
      result.tracking.statdth = p_state_of_death
      result.tracking.admin_info.track_year = p_year_of_death;
      result.host_state = window.location.host.split('-')[0];
  /*    result.tracking.date_of_death.year = p_year_of_death;
      result.tracking.date_of_death.month = p_month_of_death;
      result.tracking.date_of_death.day = p_day_of_death;
  */
      let reporting_state = sanitize_encodeHTML(window.location.host.split("-")[0]);
  
      if 
      (
          (
              !result.tracking.admin_info.pmssno || 
              result.tracking.admin_info.pmssno == ''
          ) && 
          result.tracking.admin_info.jurisdiction && 
          result.tracking.admin_info.jurisdiction != '' /*&& 
          
          result.tracking.date_of_death.year && 
          parseInt(result.tracking.date_of_death.year) > 999 && 
          parseInt(result.tracking.date_of_death.year) < 2500*/
      ) 
      {
          
          /*
          let new_record_id = reporting_state.trim() + '-' + result.tracking.date_of_death.year.trim() + '-' + $mmria.getRandomCryptoValue().toString().substring(2, 6);
          while(g_record_id_list[new_record_id] != null)
          {
              new_record_id = reporting_state.trim() + '-' + result.tracking.date_of_death.year.trim() + '-' + $mmria.getRandomCryptoValue().toString().substring(2, 6);
          }
  
          result.tracking.admin_info.pmssno = new_record_id.toUpperCase();
          */
      }
  
      var new_data = [];
  
      for (var i in g_ui.case_view_list) 
      {
        new_data.push(g_ui.case_view_list[i]);
      }
  
      const new_pmss_number_response = await $.ajax({
          url: `${location.protocol}//${location.host}/api/case_view/next-pmss-number/${p_state_of_death}-${p_year_of_death.substr(2)}`
      });
  
      result.tracking.admin_info.pmssno = new_pmss_number_response;
  
      new_data.push({
        id: result._id,
        key: result._id,
        value: {
          /*
          first_name: result.tracking.first_name,
          middle_name: result.tracking.middle_name,
          last_name: result.tracking.last_name,*/
          date_of_death_year: result.tracking.date_of_death.year,
          //date_of_death_month: result.tracking.date_of_death.month,
          host_state: result.host_state,
  
          date_created: result.date_created,
          created_by: result.created_by,
          date_last_updated: result.date_last_updated,
          last_updated_by: result.last_updated_by,
  
          pmssno: new_pmss_number_response,
          track_year: p_year_of_death,
          jurisdiction: p_state_of_death
          //agency_case_id: result.agency_case_id,
          //date_of_committee_review: result.committee_review.date_of_review,
        },
      });
  
      g_ui.case_view_list = new_data;
      g_data = result;
      g_data_is_checked_out = true;
      g_change_stack = [];
      g_ui.selected_record_id = result._id;
      g_ui.selected_record_index = g_ui.case_view_list.length - 1;
  
      set_local_case
      (
          g_data,
          async function () 
          {
              await save_case(g_data, function () 
              {
                  var url =
                  location.protocol +
                  '//' +
                  location.host +
                  '/Case#/' +
                  g_ui.selected_record_index +
                  '/tracking';
  
                  window.location = url;
              }, "add_new_case");
          }
      );
  
      return result;
    },
  
    case_view_list: [],
  
    case_view_request: {
      total_rows: 0,
      page: 1,
      skip: 0,
      take: 100,
      sort: 'by_date_last_updated',
      search_key: null,
      descending: true,
      jurisdiction: "all",
      year_of_death: "all",
      status: "all",
      classification: "all",
      field_selection: "all",
      pregnancy_relatedness:"all",
      get_query_string: function () {
        var result = [];
        result.push('?skip=' + (this.page - 1) * this.take);
        result.push('take=' + this.take);
        result.push('sort=' + this.sort);
        result.push('descending=' + this.descending);
        result.push('jurisdiction=' + this.jurisdiction);
        result.push('year_of_death=' + this.year_of_death);
        result.push('status=' + encodeURIComponent(this.status));
        result.push('classification=' + this.classification);
        
        result.push('field_selection=' + this.field_selection);
  
  
        if(g_is_data_analyst_mode == null || g_is_data_analyst_mode !="da")
        {
          result.push('include_pinned_cases=true');
        }
        
        if (this.search_key)
        {
          result.push(
            'search_key=' +
            //encodeURIComponent(this.search_key.replace(/"/g, '\\"').replace(/\n/g, '\\n'))
            encodeURIComponent(this.search_key.trim())
          );
        }
  
        return result.join('&');
      },
    },
  };

async function add_new_case_button_click(p_input)
{
    let state = document.getElementById("add_new_state");

    if(state == null)
    {
        let el = document.getElementById("app_summary");
        let state_list = [];
        let year_list = [];

        let state_html = [];
        let year_html = [];

        for(let i = 0; i < g_metadata.lookup.length; i++)
        {
            let item = g_metadata.lookup[i];
            switch(item.name.toLowerCase())
            {
                case "state":
                    state_list = item;
                    break;
                case "year":
                    year_list = item;
                    break;
            }
        }

        for(let i = 0; i < state_list.values.length; i++)
        {
            let item = state_list.values[i];

            state_html.push(`<option value='${item.value}'>${item.display}</option>`)
        }

        for(let i = 0; i < year_list.values.length; i++)
        {
            let item = year_list.values[i];
            year_html.push(`<option value='${item.value}'>${item.display}</option>`)
        }

        let result = [];

        result.push(`
            <input type="hidden" id="add_new_state" value="init" />

            <h1 class="h2">Add New Case - Generate PMSS#</h1>

            <div id="new_validation_message_area">
                <p><span class="info-icon x20 fill-p cdc-icon-info-circle-solid ml-1"></span> All fields are required to generate PMSS#.</p>
            </div>

            <div>
                <div class="form-row">
                    <div class="col-4 mb-3" style="padding-bottom: 10px">
                        <label for="new_state_of_death">Reporting Jurisdiction</label>
                        <select id="new_state_of_death" class="form-control">${state_html.join("")}</select>
                    </div>

                    <div class="col-4 mb-3">
                                    <label for="new_year_of_death">Reporting Year</label>
                                    <select id="new_year_of_death" class="form-control">${year_html.join("")}</select>
                    </div>
                </div>

                <div class="form-row mt-3">
                    <button class="btn btn-primary mr-2" onclick="add_new_case_button_click()">Generate PMSS# & Continue</button>
                    <button class="btn btn-light" onclick="g_render();">Cancel</button>
                </div>
                
            </div>

            <dialog id="add_new_confirm_dialog" class="set-radius p-0" role="dialog" tabindex="-1" aria-describedby="mmria_dialog" aria-labelledby="ui-id-1"></dialog>
        `);


        el.innerHTML = result.join("");
    }
    else if(state.value == "init")
    {
        const new_validation_message_area = document.getElementById("new_validation_message_area");
        const new_year_of_death = document.getElementById("new_year_of_death");
        const new_state_of_death = document.getElementById("new_state_of_death");

        if(
            new_year_of_death.value == null ||
            new_state_of_death.value == null ||
            new_year_of_death.value == 9999 ||
            new_state_of_death.value == 9999
        )
        {
            new_validation_message_area.innerHTML = `
                <div class="construct__header-alert row no-gutters p-2 mb-3">
                    <span class="left-col x32 fill-p cdc-icon-alert_02"></span>
                    <div class="right-col pl-3">
                        <p>Please enter data for the required fields below and try again:</p>
                        <ul id="validation_summary_list" class="mb-0">
                            <li><strong>Reporting Jurisdiction</strong></li>
                            <li><strong>Reporting Year</strong></li>
                        </ul>
                        <p>Both <b>Reporting Jurisdiction</b> and <b>Reporting Year</b> are required to create a new case form.</p>
                    </div>
                </div>
            `;
        }
        else
        {

            document.getElementById("add_new_state").value = "preconfirm";
        
            add_new_case_button_click();
/*
            add_new_case_check_for_duplicate(
                new_first_name.value, 
                new_last_name.value, 
                new_month_of_death.value, 
                new_day_of_death.value, 
                new_year_of_death.value, 
                new_state_of_death.value
            );
*/
        } 
        
        
    }
    else if(state.value =="preconfirm")
    {
        
        state.value = "confirm";

        let add_new_confirm_dialog = document.getElementById("add_new_confirm_dialog");
        add_new_confirm_dialog.innerHTML = `
            <div class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                <span id="ui-id-1" class="ui-dialog-title">Generate Record ID?</span>
                <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="add_new_case_button_click('no')"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
            </div>
            <div id="mmria_dialog8" style="width: auto; min-height: 101px; max-height: none; height: auto;" class="ui-dialog-content ui-widget-content">
                <div class="modal-body">
                    <p><strong>Reporting Jurisdiction:</strong> ${app_get_lookup_value_to_display("state", new_state_of_death.value)}</p>
                    <p><strong>Reporting Year:</strong> ${new_year_of_death.value == 9999? "(blank)": new_year_of_death.value}</p>
                    
                    <p class="d-flex align-items-start mb-0">
                        <span class="info-icon x20 fill-p cdc-icon-info-circle-solid mt-1 mr-2"></span>
                        <span>Are you sure you want to generate a new PMSS Record?</span>
                    </p>
                </div>
                <footer class="modal-footer">
                    <button class="btn btn-primary mr-1" onclick="add_new_case_button_click('yes')">OK</button>
                    <button class="btn btn-light" onclick="add_new_case_button_click('no')">Cancel</button>
                </footer>
            </div>
        `;
        add_new_confirm_dialog.showModal();
    }
    else if(state.value == "duplicate_name")
    {
        document.getElementById("add_new_state").value = "init";

        let add_new_confirm_dialog = document.getElementById("add_new_confirm_dialog");
        add_new_confirm_dialog.innerHTML = `
            <div class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
                <span id="ui-id-1" class="ui-dialog-title">Duplicate Name Found</span>
                <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="add_new_case_button_click('no')"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
            </div>
            <div id="mmria_dialog9" style="width: auto; min-height: 101px; max-height: none; height: auto;" class="ui-dialog-content ui-widget-content">
                <div class="modal-body">
                    <p><strong>Decedent’s Name (First, Middle, Last):</strong> ${new_first_name.value} ${new_middle_name.value} ${new_last_name.value}</p>
                    <p><strong>Date of Death:</strong> ${new_month_of_death.value== 9999? "(blank)" :new_month_of_death.value}/${new_day_of_death.value == 9999? "(blank)":new_day_of_death.value}/${new_year_of_death.value == 9999? "(blank)": new_year_of_death.value}</p>
                    <p><strong>State of Death Record:</strong> ${new_state_of_death.value==9999? "(blank)": new_state_of_death.value}</p>
                    <p class="d-flex align-items-start mb-0">
                        <span class="info-icon x20 fill-p cdc-icon-info-circle-solid mt-1 mr-2"></span>
                        <span>Duplicate: A Record with the same information was found.  Please check your information again.</span>
                    </p>
                </div>
                <footer class="modal-footer">
                    <button class="btn btn-light" onclick="add_new_confirm_dialog.close()">Cancel</button>
                </footer>
            </div>
        `;
        add_new_confirm_dialog.showModal();
    }
    else if(state.value == "confirm")
    {
        let add_new_confirm_dialog = document.getElementById("add_new_confirm_dialog");
        add_new_confirm_dialog.close();
        if(p_input == "yes")
        {
            //state.value = "generate_record";
            state.value = "init";
            new_validation_message_area.innerHTML = "generate confirmed";

            await Get_Record_Id_List(

            function () {
                g_ui.add_new_case(
                new_year_of_death.value,
                new_state_of_death.value);
            });

        }
        else
        {
            //new_validation_message_area.innerHTML = "cancelled generate";
            new_validation_message_area.innerHTML = `
                <p><span class="info-icon x20 fill-p cdc-icon-info-circle-solid"></span> All fields are required to generate record id number except Decedent's Middle Name.</p>
            `;
            state.value = "init";
        }
        
    }
    else if("generate_record")
    {

    }
}


function add_new_case_check_for_duplicate
(
    pYearOfDeath, 
    pStateOfDeath
) 
{

    let data = { 
        YearOfDeath: pYearOfDeath, 
        StateOfDeath: pStateOfDeath
    };
    
  $.ajax({
    url:
      location.protocol + '//' + location.host + '/api/isDuplicateCase',
      contentType: 'application/json; charset=utf-8',
      dataType: 'json',
      data: JSON.stringify(data),
      type: 'POST',
  }).done(function (response) {
      let is_duplicate_response = true;
      if(response == is_duplicate_response)
      {
        document.getElementById("add_new_state").value = "duplicate_name";
      }
      else
      {
        document.getElementById("add_new_state").value = "preconfirm";
      }
      add_new_case_button_click();
  });
}

function is_case_locked(p_case)
{
    let result = false;

    let selected_value = 9999;
    
    if
    (
        p_case.tracking &&
        p_case.tracking.case_status &&
        p_case.tracking.case_status.overall_case_status &&
        p_case.tracking.case_status.overall_case_status != ""
    )
    {
        selected_value = new Number(p_case.tracking.case_status.overall_case_status);
    }
    
    if
    (
        p_case.tracking &&
        p_case.tracking.case_status &&
        p_case.tracking.case_status &&
        p_case.tracking.case_status.case_locked_date != "" &&
        (
            selected_value == 4 ||
            selected_value == 5 ||
            selected_value == 6
        )
    )
    {
        if (! g_is_confirm_for_case_lock)
        {
            result = true;
        }
    }

    return result;
}

