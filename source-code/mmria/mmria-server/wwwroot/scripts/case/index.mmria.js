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
  
    add_new_case: function (
      p_first_name,
      p_middle_name,
      p_last_name,
      p_month_of_death,
      p_day_of_death,
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
      result.home_record.case_status.overall_case_status = 1;
      result.home_record.case_status.abstraction_begin_date = convert_date_to_storage_format(new Date());
  
      if (g_jurisdiction_list.length > 0) 
      {
        result.home_record.jurisdiction_id = g_jurisdiction_list[0];
      } 
      else 
      {
        result.home_record.jurisdiction_id = '/';
      }
  
      result.home_record.last_name = p_last_name;
      result.home_record.first_name = p_first_name;
      result.home_record.middle_name = p_middle_name;
      result.home_record.state_of_death_record = p_state_of_death;
      result.home_record.date_of_death.year = p_year_of_death;
      result.home_record.date_of_death.month = p_month_of_death;
      result.home_record.date_of_death.day = p_day_of_death;
  
      let reporting_state = sanitize_encodeHTML(window.location.host.split("-")[0]);
  
      if 
      (
          (
              !result.home_record.record_id || 
              result.home_record.record_id == ''
          ) && 
          result.home_record.state_of_death_record && 
          result.home_record.state_of_death_record != '' && 
          result.home_record.date_of_death.year && 
          parseInt(result.home_record.date_of_death.year) > 999 && 
          parseInt(result.home_record.date_of_death.year) < 2500
      ) 
      {
          
          let new_record_id = reporting_state.trim() + '-' + result.home_record.date_of_death.year.trim() + '-' + $mmria.getRandomCryptoValue().toString().substring(2, 6);
          while(g_record_id_list.has(new_record_id))
          {
              new_record_id = reporting_state.trim() + '-' + result.home_record.date_of_death.year.trim() + '-' + $mmria.getRandomCryptoValue().toString().substring(2, 6);
          }
  
          result.home_record.record_id = new_record_id.toUpperCase();
  
          g_record_id_list.add(new_record_id.toUpperCase());
      }
  
      var new_data = [];
  
      for (var i in g_ui.case_view_list) 
      {
        new_data.push(g_ui.case_view_list[i]);
      }
  
      new_data.push({
        id: result._id,
        key: result._id,
        value: {
          first_name: result.home_record.first_name,
          middle_name: result.home_record.middle_name,
          last_name: result.home_record.last_name,
          date_of_death_year: result.home_record.date_of_death.year,
          date_of_death_month: result.home_record.date_of_death.month,
  
          date_created: result.date_created,
          created_by: result.created_by,
          date_last_updated: result.date_last_updated,
          last_updated_by: result.last_updated_by,
  
          record_id: result.home_record.record_id,
          agency_case_id: result.agency_case_id,
          date_of_committee_review: result.committee_review.date_of_review,
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
                 '/home_record';
  
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
      case_status: "all",
      jurisdiction: "all",
      year_of_death: "all",
      status: "all",
      classification: "all",
      field_selection: "all",
      pregnancy_relatedness:"all",
      offline_only: false,
      get_query_string: function () {
        var result = [];
        result.push('?skip=' + (this.page - 1) * this.take);
        result.push('take=' + this.take);
        result.push('sort=' + this.sort);
        result.push('descending=' + this.descending);

        result.push('case_status=' + this.case_status);
        result.push('pregnancy_relatedness=' + this.pregnancy_relatedness);
        
        
        result.push('field_selection=' + this.field_selection);
        result.push('offline_only=' + this.offline_only);
  
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
        let month_list = [];
        let day_list = [];
        let year_list = [];
        let state_html = [];
        let month_html = [];
        let day_html = [];
        let year_html = [];

        for(let i = 0; i < g_metadata.lookup.length; i++)
        {
            let item = g_metadata.lookup[i];
            switch(item.name.toLowerCase())
            {
                case "state":
                    state_list = item;
                    break;
                case "month":
                    month_list = item;
                    break;
                case "day":
                    day_list = item;
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

        for(let i = 0; i < month_list.values.length; i++)
        {
            let item = month_list.values[i];
            month_html.push(`<option value='${item.value}'>${item.display}</option>`)
        }


        for(let i = 0; i < day_list.values.length; i++)
        {
            let item = day_list.values[i];
            day_html.push(`<option value='${item.value}'>${item.display}</option>`)
        }

        for(let i = 0; i < year_list.values.length; i++)
        {
            let item = year_list.values[i];
            year_html.push(`<option value='${item.value}'>${item.display}</option>`)
        }

        let result = [];

        result.push(`
            <input type="hidden" id="add_new_state" value="init" />

            <h1 class="h2">Add New Case - Generate MMRIA Record ID#</h1>

            <div id="new_validation_message_area">
                <p><span class="info-icon x20 fill-p cdc-icon-info-circle-solid ml-1"></span> All fields are required to generate record id number except Decedent's Middle Name.</p>
            </div>

            <div>
                <div class="form-row">
                    <div class="col mb-3">
                        <label for="new_first_name">Decedent's First Name*</label>
                        <input id="new_first_name" class="form-control w-100" type="text" value="" />
                    </div>

                    <div class="col mb-3">
                        <label for="new_middle_name">Decedent's Middle Name</label>
                        <input id="new_middle_name" class="form-control w-100" type="text" value="" />
                    </div>
                    
                    <div class="col mb-3">
                        <label for="new_last_name">Decedent's Last Name*</label>
                        <input id="new_last_name" class="form-control w-100" type="text" value="" />
                    </div>
                </div>

                <div class="form-row align-items-end">
                    <div class="col-8 mb-3 batch" style="margin-top: 36px">
                        <fieldset class="batch__body">
                            <legend class="batch__title">Date of Death*</legend>
                            <div class="form-row">
                                <div class="col-4">
                                    <label for="new_month_of_death">Month*</label>
                                    <select id="new_month_of_death" class="form-control">${month_html.join("")}</select>
                                </div>

                                <div class="col-4">
                                    <label for="new_day_of_death">Day*</label>
                                    <select id="new_day_of_death" class="form-control">${day_html.join("")}</select>
                                </div>
                                
                                <div class="col-4">
                                    <label for="new_year_of_death">Year*</label>
                                    <select id="new_year_of_death" class="form-control">${year_html.join("")}</select>
                                </div>
                            </div>
                        </fieldset>
                    </div>

                    <div class="col-4 mb-3" style="padding-bottom: 10px">
                        <label for="new_state_of_death">State of Death Record*</label>
                        <select id="new_state_of_death" class="form-control">${state_html.join("")}</select>
                    </div>
                </div>

                <div class="form-row mt-3">
                    <button class="btn btn-primary mr-2" onclick="add_new_case_button_click()">Generate Record ID & Continue</button>
                    <button class="btn btn-light" onclick="g_render();">Cancel</button>
                </div>
                
            </div>

            <dialog id="add_new_confirm_dialog" class="set-radius p-0" role="dialog" tabindex="-1" aria-describedby="mmria_dialog" aria-labelledby="ui-id-1"></dialog>
        `);


        el.innerHTML = result.join("");
    }
    else if(state.value == "init")
    {
        let new_validation_message_area = document.getElementById("new_validation_message_area");
        let new_first_name = document.getElementById("new_first_name");
        let new_middle_name = document.getElementById("new_middle_name");
        let new_last_name = document.getElementById("new_last_name");
        let new_month_of_death = document.getElementById("new_month_of_death");
        let new_day_of_death = document.getElementById("new_day_of_death");
        let new_year_of_death = document.getElementById("new_year_of_death");
        let new_state_of_death = document.getElementById("new_state_of_death");

        if(
            new_first_name.value == null ||
            new_last_name.value == null ||
            new_month_of_death.value == null ||
            new_day_of_death.value == null ||
            new_year_of_death.value == null ||
            new_state_of_death.value == null ||
            new_first_name.value.length < 1 ||
            new_last_name.value.length < 1 ||
            new_month_of_death.value == 9999 ||
            new_day_of_death.value == 9999 ||
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
                            <li><strong>Decedent's Last Name</strong></li>
                            <li><strong>Decedent's First Name</strong></li>
                            <li><strong>Date of Death (MM, DD, YYYY)</strong></li>
                            <li><strong>State of Death Record</strong></li>
                        </ul>
                        <p>The only field not required to create a new case form is <strong>Decedent's Middle Name</strong>.</p>
                    </div>
                </div>
            `;
        }
        else
        {
            add_new_case_check_for_duplicate(
                new_first_name.value, 
                new_last_name.value, 
                new_month_of_death.value, 
                new_day_of_death.value, 
                new_year_of_death.value, 
                new_state_of_death.value
            );

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
                    <p><strong>Decedent’s Name (First, Middle, Last):</strong> ${new_first_name.value} ${new_middle_name.value} ${new_last_name.value}</p>
                    <p><strong>Date of Death:</strong> ${new_month_of_death.value== 9999? "(blank)" :new_month_of_death.value}/${new_day_of_death.value == 9999? "(blank)":new_day_of_death.value}/${new_year_of_death.value == 9999? "(blank)": new_year_of_death.value}</p>
                    <p><strong>State of Death Record:</strong> ${new_state_of_death.value==9999? "(blank)": new_state_of_death.value}</p>
                    <p class="d-flex align-items-start mb-0">
                        <span class="info-icon x20 fill-p cdc-icon-info-circle-solid mt-1 mr-2"></span>
                        <span>After you generate the MMRIA Record ID#, you will <strong>not</strong> be able to edit the Year of Death.</span>
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
                new_first_name.value,
                new_middle_name.value,
                new_last_name.value,
                new_month_of_death.value,
                new_day_of_death.value,
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
    pFirstName, 
    pLastName, 
    pMonthOfDeath, 
    pDayOfDeath, 
    pYearOfDeath, 
    pStateOfDeath
) 
{

    let data = { 
        FirstName: pFirstName, 
        LastName: pLastName, 
        MonthOfDeath: pMonthOfDeath, 
        DayOfDeath: pDayOfDeath, 
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

const independent_autocalc_niosh_set = new Set();

independent_autocalc_niosh_set.add("/death_certificate/demographics/occupation_business_industry");
independent_autocalc_niosh_set.add("/death_certificate/demographics/primary_occupation");
independent_autocalc_niosh_set.add("/birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry");
independent_autocalc_niosh_set.add("/birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation");
independent_autocalc_niosh_set.add("/birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry");
independent_autocalc_niosh_set.add("/birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation");
independent_autocalc_niosh_set.add("/social_and_environmental_profile/socio_economic_characteristics/occupation");

const independent_autocalc_gestation_event_set = new Set();




independent_autocalc_gestation_event_set.add("/prenatal/other_lab_tests/date_and_time");
independent_autocalc_gestation_event_set.add("/prenatal/current_pregnancy/date_of_1st_prenatal_visit");
independent_autocalc_gestation_event_set.add("/prenatal/current_pregnancy/date_of_last_prenatal_visit");
independent_autocalc_gestation_event_set.add("/prenatal/routine_monitoring/date_and_time");
independent_autocalc_gestation_event_set.add("/prenatal/diagnostic_procedures/date");
independent_autocalc_gestation_event_set.add("/prenatal/problems_identified_grid/date_1st_noted");
independent_autocalc_gestation_event_set.add("/prenatal/medications_and_drugs_during_pregnancy/date");
independent_autocalc_gestation_event_set.add("/prenatal/pre_delivery_hospitalizations_details/date");
independent_autocalc_gestation_event_set.add("/prenatal/medical_referrals/date");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge");
independent_autocalc_gestation_event_set.add("/other_medical_office_visits/visit/date_of_medical_office_visit");
independent_autocalc_gestation_event_set.add("/medical_transport/date_of_transport");
independent_autocalc_gestation_event_set.add("/medical_transport/transport_vital_signs/date_and_time");
independent_autocalc_gestation_event_set.add("/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening");
independent_autocalc_gestation_event_set.add("/prenatal/current_pregnancy/date_of_1st_prenatal_visit/month");
independent_autocalc_gestation_event_set.add("/prenatal/current_pregnancy/date_of_1st_prenatal_visit/day");
independent_autocalc_gestation_event_set.add("/prenatal/current_pregnancy/date_of_1st_prenatal_visit/year");
independent_autocalc_gestation_event_set.add("/prenatal/current_pregnancy/date_of_last_prenatal_visit/month");
independent_autocalc_gestation_event_set.add("/prenatal/current_pregnancy/date_of_last_prenatal_visit/day");
independent_autocalc_gestation_event_set.add("/prenatal/current_pregnancy/date_of_last_prenatal_visit/year")
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/month");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/day");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/year");

independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/month");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/day");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/year");

independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/month");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/day");
independent_autocalc_gestation_event_set.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/year");






const independent_autocalc_list = new Set()

independent_autocalc_list.add("/prenatal/current_pregnancy/estimated_date_of_confinement/month");
independent_autocalc_list.add("/prenatal/current_pregnancy/estimated_date_of_confinement/day");
independent_autocalc_list.add("/prenatal/current_pregnancy/estimated_date_of_confinement/year");
independent_autocalc_list.add("/prenatal/current_pregnancy/date_of_last_normal_menses/month");
independent_autocalc_list.add("/prenatal/current_pregnancy/date_of_last_normal_menses/day");
independent_autocalc_list.add("/prenatal/current_pregnancy/date_of_last_normal_menses/year");

independent_autocalc_list.add("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month");
independent_autocalc_list.add("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day");
independent_autocalc_list.add("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year");


const dependent_autocalc_list = new Set();
dependent_autocalc_list.add("/prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_weeks");
dependent_autocalc_list.add("/prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_days");
dependent_autocalc_list.add("/prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit");
dependent_autocalc_list.add("/prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit_days");
dependent_autocalc_list.add("/prenatal/routine_monitoring/gestational_age_weeks");
dependent_autocalc_list.add("/prenatal/routine_monitoring/gestational_age_days");
dependent_autocalc_list.add("/prenatal/other_lab_tests/gestational_age_weeks");
dependent_autocalc_list.add("/prenatal/other_lab_tests/gestational_age_days");
dependent_autocalc_list.add("/prenatal/diagnostic_procedures/gestational_age_weeks");
dependent_autocalc_list.add("/prenatal/diagnostic_procedures/gestational_age_days");
dependent_autocalc_list.add("/prenatal/problems_identified_grid/gestational_age_weeks");
dependent_autocalc_list.add("/prenatal/problems_identified_grid/gestational_age_days");
dependent_autocalc_list.add("/prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks");
dependent_autocalc_list.add("/prenatal/medications_and_drugs_during_pregnancy/gestational_age_days");
dependent_autocalc_list.add("/prenatal/pre_delivery_hospitalizations_details/gestational_age_weeks");
dependent_autocalc_list.add("/prenatal/pre_delivery_hospitalizations_details/gestational_age_days");
dependent_autocalc_list.add("/prenatal/medical_referrals/gestational_age_weeks");
dependent_autocalc_list.add("/prenatal/medical_referrals/gestational_age_days");
dependent_autocalc_list.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_weeks");
dependent_autocalc_list.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_days");
dependent_autocalc_list.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_weeks");
dependent_autocalc_list.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_days");
dependent_autocalc_list.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_weeks");
dependent_autocalc_list.add("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_days");
dependent_autocalc_list.add("/other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_weeks");
dependent_autocalc_list.add("/other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_days");
dependent_autocalc_list.add("/medical_transport/date_of_transport/gestational_age_weeks");
dependent_autocalc_list.add("/medical_transport/date_of_transport/gestational_age_days");
dependent_autocalc_list.add("/medical_transport/transport_vital_signs/gestational_weeks");
dependent_autocalc_list.add("/medical_transport/transport_vital_signs/gestational_days");
dependent_autocalc_list.add("/mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks");
dependent_autocalc_list.add("/mental_health_profile/were_there_documented_mental_health_conditions/gestational_days");

// grids
/*
/prenatal/other_lab_tests/gestational_age_weeks"
/prenatal/other_lab_tests/gestational_age_days"
/prenatal/diagnostic_procedures/gestational_age_weeks"
/prenatal/diagnostic_procedures/gestational_age_days"
/prenatal/problems_identified_grid/gestational_age_weeks"
/prenatal/problems_identified_grid/gestational_age_days"
/prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks"
/prenatal/medications_and_drugs_during_pregnancy/gestational_age_days"
/prenatal/pre_delivery_hospitalizations_details/gestational_age_weeks"
/prenatal/pre_delivery_hospitalizations_details/gestational_age_days"
/prenatal/medical_referrals/gestational_age_weeks"
/prenatal/medical_referrals/gestational_age_days"
/medical_transport/transport_vital_signs/gestational_weeks"
/medical_transport/transport_vital_signs/gestational_days"
/mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks"
/mental_health_profile/were_there_documented_mental_health_conditions/gestational_days"
*/

let autorecalculate_count = 0;
let autorecalculate_niosh = 0;
let autorecalculate_independent = 0;
let autorecalculate_gestation_all = 0;
let autorecalculate_gestation = 0;
let autorecalculate_entry = 0;

let is_log_auto_count = true;

async function autorecalculate
(
    p_independent_variable_mmria_path,
    p_form_index,
    p_grid_index
)
{
    //console.log("autorecalculate called");
    if(independent_autocalc_list.has(p_independent_variable_mmria_path))
    {
        await autorecalculate_all_gestation(p_form_index, p_grid_index);
        autorecalculate_gestation +=1;
    }

    if(autocalc_map.has(p_independent_variable_mmria_path))
    {
        let entry = autocalc_map.get(p_independent_variable_mmria_path);
        
        if(Array.isArray(entry))
        {
            for(let i = 0; i < entry.length; i++)
            {
                entry[i](p_form_index, p_grid_index);
                autorecalculate_entry +=1;
            }
        }
        else
        {
            entry(p_form_index, p_grid_index);
            autorecalculate_entry +=1;
        }
    }

    if(independent_autocalc_gestation_event_set.has(p_independent_variable_mmria_path))
    {

        const edd_year = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.year);
        const edd_month = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.month);
        const edd_day = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.day);
        const lmp_year = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.year);
        const lmp_month = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.month);
        const lmp_day = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.day);
    
        const edd_date = new Date(edd_year, edd_month - 1, edd_day);
        const lmp_date = new Date(lmp_year, lmp_month - 1, lmp_day);
    
        let is_edd = false;
        let is_lmp = false;
    
        if 
        (
            //$global.isValidDate(event_year, event_month, event_day) == true && 
            $global.isValidDate(edd_year, edd_month, edd_day) == true
        )
        {
            is_edd = true;
    
        }
        else if 
        (
            //$global.isValidDate(event_year, event_month, event_day) == true && 
            $global.isValidDate(lmp_year, lmp_month, lmp_day) == true
        )
        {
            is_lmp = true;
        }
        else
        {
            //return;
        }


        const delivery_month = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month
        const delivery_day = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day
        const delivery_year = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year

        const delivery_date = new Date(delivery_year, delivery_month -1, delivery_day);
        const is_valid_delivery_date = $global.isValidDate(delivery_year, delivery_month, delivery_day);


        let map = null;

        switch(p_independent_variable_mmria_path)
        {
            case "/prenatal/other_lab_tests/date_and_time":

                map = autorecalculate_get_event_date("/prenatal/other_lab_tests/date_and_time", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.other_lab_tests[p_grid_index].gestational_age_weeks = map.get('ga')[0];
                    g_data.prenatal.other_lab_tests[p_grid_index].gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.other_lab_tests[p_grid_index].gestational_age_weeks = "";
                    g_data.prenatal.other_lab_tests[p_grid_index].gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/other_lab_tests/gestational_age_weeks", 
                    g_data.prenatal.other_lab_tests[p_grid_index].gestational_age_weeks, 
                    p_form_index, p_grid_index
                );
                $mmria.set_control_value
                (
                    "prenatal/other_lab_tests/gestational_age_days", 
                    g_data.prenatal.other_lab_tests[p_grid_index].gestational_age_days, 
                    p_form_index, p_grid_index
                );
            break;
            case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit":
            case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit/month":
            case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit/day":
            case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit/year":
                map = autorecalculate_get_event_date("/prenatal/current_pregnancy/date_of_1st_prenatal_visit", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_weeks = map.get('ga')[0];
                    g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_weeks = "";
                    g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_weeks", 
                    g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_weeks, 
                    p_form_index, p_grid_index
                );
                $mmria.set_control_value
                (
                    "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_days", 
                    g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/prenatal/current_pregnancy/date_of_last_prenatal_visit":
            case "/prenatal/current_pregnancy/date_of_last_prenatal_visit/month":
            case "/prenatal/current_pregnancy/date_of_last_prenatal_visit/day":
            case "/prenatal/current_pregnancy/date_of_last_prenatal_visit/year":
                map = autorecalculate_get_event_date("/prenatal/current_pregnancy/date_of_last_prenatal_visit", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit = map.get('ga')[0];
                    g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit = "";
                    g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit", 
                    g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit_days", 
                    g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit_days, 
                    p_form_index, p_grid_index
                );
                

            break;
            case "/prenatal/routine_monitoring/date_and_time":
                map = autorecalculate_get_event_date("/prenatal/routine_monitoring/date_and_time", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.routine_monitoring[p_grid_index].gestational_age_weeks = map.get('ga')[0];
                    g_data.prenatal.routine_monitoring[p_grid_index].gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.routine_monitoring[p_grid_index].gestational_age_weeks = "";
                    g_data.prenatal.routine_monitoring[p_grid_index].gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/routine_monitoring/gestational_age_weeks", 
                    g_data.prenatal.routine_monitoring[p_grid_index].gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "prenatal/routine_monitoring/gestational_age_days", 
                    g_data.prenatal.routine_monitoring[p_grid_index].gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/prenatal/diagnostic_procedures/date":
                map = autorecalculate_get_event_date("/prenatal/diagnostic_procedures/date", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.diagnostic_procedures[p_grid_index].gestational_age_weeks = map.get('ga')[0];
                    g_data.prenatal.diagnostic_procedures[p_grid_index].gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.diagnostic_procedures[p_grid_index].gestational_age_weeks = "";
                    g_data.prenatal.diagnostic_procedures[p_grid_index].gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/diagnostic_procedures/gestational_age_weeks", 
                    g_data.prenatal.diagnostic_procedures[p_grid_index].gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "prenatal/diagnostic_procedures/gestational_age_days", 
                    g_data.prenatal.diagnostic_procedures[p_grid_index].gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            
            break;
            case "/prenatal/problems_identified_grid/date_1st_noted":
                map = autorecalculate_get_event_date("/prenatal/problems_identified_grid/date_1st_noted", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.problems_identified_grid[p_grid_index].gestational_age_weeks = map.get('ga')[0];
                    g_data.prenatal.problems_identified_grid[p_grid_index].gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.problems_identified_grid[p_grid_index].gestational_age_weeks = "";
                    g_data.prenatal.problems_identified_grid[p_grid_index].gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/problems_identified_grid/gestational_age_weeks", 
                    g_data.prenatal.problems_identified_grid[p_grid_index].gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "prenatal/problems_identified_grid/gestational_age_days", 
                    g_data.prenatal.problems_identified_grid[p_grid_index].gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/prenatal/medications_and_drugs_during_pregnancy/date":
                map = autorecalculate_get_event_date("/prenatal/medications_and_drugs_during_pregnancy/date", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.medications_and_drugs_during_pregnancy[p_grid_index].gestational_age_weeks = map.get('ga')[0];
                    g_data.prenatal.medications_and_drugs_during_pregnancy[p_grid_index].gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.medications_and_drugs_during_pregnancy[p_grid_index].gestational_age_weeks = "";
                    g_data.prenatal.medications_and_drugs_during_pregnancy[p_grid_index].gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks", 
                    g_data.prenatal.medications_and_drugs_during_pregnancy[p_grid_index].gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "prenatal/medications_and_drugs_during_pregnancy/gestational_age_days", 
                    g_data.prenatal.medications_and_drugs_during_pregnancy[p_grid_index].gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/prenatal/pre_delivery_hospitalizations_details/date":
                map = autorecalculate_get_event_date("/prenatal/pre_delivery_hospitalizations_details/date", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.pre_delivery_hospitalizations_details[p_grid_index].gestational_age_weeks = map.get('ga')[0];
                    g_data.prenatal.pre_delivery_hospitalizations_details[p_grid_index].gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.pre_delivery_hospitalizations_details[p_grid_index].gestational_age_weeks = "";
                    g_data.prenatal.pre_delivery_hospitalizations_details[p_grid_index].gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/pre_delivery_hospitalizations_details/gestational_age_weeks", 
                    g_data.prenatal.pre_delivery_hospitalizations_details[p_grid_index].gestational_age_weeks, 
                    p_form_index, 
                    p_grid_index
                );

                $mmria.set_control_value
                (
                    "prenatal/pre_delivery_hospitalizations_details/gestational_age_days", 
                    g_data.prenatal.pre_delivery_hospitalizations_details[p_grid_index].gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/prenatal/medical_referrals/date":
                map = autorecalculate_get_event_date("/prenatal/medical_referrals/date", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.prenatal.medical_referrals[p_grid_index].gestational_age_weeks = map.get('ga')[0];
                    g_data.prenatal.medical_referrals[p_grid_index].gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.prenatal.medical_referrals[p_grid_index].gestational_age_weeks = "";
                    g_data.prenatal.medical_referrals[p_grid_index].gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "prenatal/medical_referrals/gestational_age_weeks", 
                    g_data.prenatal.medical_referrals[p_grid_index].gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "prenatal/medical_referrals/gestational_age_days", 
                    g_data.prenatal.medical_referrals[p_grid_index].gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/time_of_arrival":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/month":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/day":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/year":
                map = autorecalculate_get_event_date("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.gestational_age_weeks = map.get('ga')[0];
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.gestational_age_weeks = "";
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_weeks", 
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_days", 
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/month":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/day":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/year":
                    
                map = autorecalculate_get_event_date("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_weeks = map.get('ga')[0];
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_weeks = "";
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_weeks", 
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_days", 
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/month":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/day":
            case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/year":
        

                map = autorecalculate_get_event_date("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_weeks = map.get('ga')[0];
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_weeks = "";
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_weeks", 
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_days", 
                    g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/other_medical_office_visits/visit/date_of_medical_office_visit":
                
                map = autorecalculate_get_event_date("/other_medical_office_visits/visit/date_of_medical_office_visit", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_weeks = map.get('ga')[0];
                    g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_weeks = "";
                    g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_days = "";
                }


                $mmria.set_control_value
                (
                    "other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_weeks", 
                    g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_days", 
                    g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
    
            break;
            case "/medical_transport/date_of_transport":
                map = autorecalculate_get_event_date("/medical_transport/date_of_transport", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.medical_transport[p_form_index].date_of_transport.gestational_age_weeks = map.get('ga')[0];
                    g_data.medical_transport[p_form_index].date_of_transport.gestational_age_days = map.get('ga')[1];
                }
                else
                {
                    g_data.medical_transport[p_form_index].date_of_transport.gestational_age_weeks = "";
                    g_data.medical_transport[p_form_index].date_of_transport.gestational_age_days = "";
                }

                $mmria.set_control_value
                (
                    "medical_transport/date_of_transport/gestational_age_weeks", 
                    g_data.medical_transport[p_form_index].date_of_transport.gestational_age_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "medical_transport/date_of_transport/gestational_age_days", 
                    g_data.medical_transport[p_form_index].date_of_transport.gestational_age_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/medical_transport/transport_vital_signs/date_and_time":
                map = autorecalculate_get_event_date("/medical_transport/transport_vital_signs/date_and_time", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.medical_transport[p_form_index].transport_vital_signs[p_grid_index].gestational_weeks = map.get('ga')[0];
                    g_data.medical_transport[p_form_index].transport_vital_signs[p_grid_index].gestational_days = map.get('ga')[1];
                }
                else
                {
                    g_data.medical_transport[p_form_index].transport_vital_signs[p_grid_index].gestational_weeks = "";
                    g_data.medical_transport[p_form_index].transport_vital_signs[p_grid_index].gestational_days = "";
                }

                $mmria.set_control_value
                (
                    "medical_transport/transport_vital_signs/gestational_weeks", 
                    g_data.medical_transport[p_form_index].transport_vital_signs[p_grid_index].gestational_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "medical_transport/transport_vital_signs/gestational_days", 
                    g_data.medical_transport[p_form_index].transport_vital_signs[p_grid_index].gestational_days, 
                    p_form_index, p_grid_index
                );
                
            break;
            case "/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening":
                map = autorecalculate_get_event_date("/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
                if 
                (
                    map.get('ga').length > 1 &&
                    (
                        ! is_valid_delivery_date ||
                        is_valid_delivery_date &&
                        map.get('event_date') <= delivery_date
                    )
                ) 
                {
                    g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_weeks = map.get('ga')[0];
                    g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_days = map.get('ga')[1];              
                }
                else
                {
                    g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_weeks = "";
                    g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_days = "";
                }

                $mmria.set_control_value
                (
                    "mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks", 
                    g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_weeks, 
                    p_form_index, p_grid_index
                );

                $mmria.set_control_value
                (
                    "mental_health_profile/were_there_documented_mental_health_conditions/gestational_days", 
                    g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_days, 
                    p_form_index, p_grid_index
                );
                


                //let map = autorecalculate_get_event_date_combined("/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening")
                if 
                (
                    is_valid_delivery_date &&
                    map.get('event_date') > delivery_date
                ) 
                {
                    const days = $global.calc_days(delivery_date, map.get('event_date'));
                    g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].days_postpartum = days;
                    $mmria.set_control_value("mental_health_profile/were_there_documented_mental_health_conditions/days_postpartum", days, p_form_index, p_grid_index);
                } 
                else 
                {

                    g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].days_postpartum = "";
                    $mmria.set_control_value("mental_health_profile/were_there_documented_mental_health_conditions/days_postpartum", "", p_form_index, p_grid_index);

                }

            break;
        }
        
    }

    if(independent_autocalc_niosh_set.has(p_independent_variable_mmria_path))
    {
        autorecalculate_niosh += 1;
        let niosh_result = {};
        switch(p_independent_variable_mmria_path)
        {
            case "/death_certificate/demographics/occupation_business_industry":
            case "/death_certificate/demographics/primary_occupation":
                niosh_result = await get_niosh_codes
                (
                    g_data.death_certificate.demographics.primary_occupation,
                    g_data.death_certificate.demographics.occupation_business_industry
                )

                if(niosh_result.industry.length > 0)
                {
                    g_data.death_certificate.demographics.dc_m_industry_code_1 = niosh_result.industry[0].code;
                }
                else
                {
                    g_data.death_certificate.demographics.dc_m_industry_code_1 = "";
                }
                
                if(niosh_result.industry.length > 1)
                {
                    g_data.death_certificate.demographics.dc_m_industry_code_2 = niosh_result.industry[1].code;
                }
                else
                {
                    g_data.death_certificate.demographics.dc_m_industry_code_2 = "";
                }
                
                if(niosh_result.industry.length > 2)
                {
                    g_data.death_certificate.demographics.dc_m_industry_code_3 = niosh_result.industry[2].code;
                }
                else
                {
                    g_data.death_certificate.demographics.dc_m_industry_code_3 = "";
                }
                
                if(niosh_result.occupation.length > 0)
                {
                    g_data.death_certificate.demographics.dc_m_occupation_code_1 = niosh_result.occupation[0].code;
                }
                else
                {
                    g_data.death_certificate.demographics.dc_m_occupation_code_1 = "";
                }
                
                if(niosh_result.occupation.length > 1)
                {
                    g_data.death_certificate.demographics.dc_m_occupation_code_2 = niosh_result.occupation[1].code;
                }
                else
                {
                    g_data.death_certificate.demographics.dc_m_occupation_code_2 = "";
                }
                
                if(niosh_result.occupation.length > 2)
                {
                    g_data.death_certificate.demographics.dc_m_occupation_code_3 = niosh_result.occupation[2].code;
                }
                else
                {
                    g_data.death_certificate.demographics.dc_m_occupation_code_3 = "";
                }
                


            break;
            case "/birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry":
            case "/birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation":


                niosh_result = await get_niosh_codes
                (
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.occupation_business_industry,
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.primary_occupation
                )
            

                if(niosh_result.industry.length > 0)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_1 = niosh_result.industry[0].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_1 = "";
                }             
                
                if(niosh_result.industry.length > 1)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_2 = niosh_result.industry[1].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_2 = "";
                }
                
                if(niosh_result.industry.length > 2)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_3 = niosh_result.industry[2].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_3 = "";
                }
                
                if(niosh_result.occupation.length > 0)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_1 = niosh_result.occupation[0].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_1 = "";
                }
                
                if(niosh_result.occupation.length > 1)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_2 = niosh_result.occupation[1].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_2 = "";
                }
                
                if(niosh_result.occupation.length > 2)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_3 = niosh_result.occupation[2].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_3 = "";
                }
                

            break;
            case "/birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry":
            case "/birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation":
                niosh_result = await get_niosh_codes
                (
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.occupation_business_industry,
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.primary_occupation
                )    
            
                if(niosh_result.industry.length > 0)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_1 = niosh_result.industry[0].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_1 = "";
                }
                
                if(niosh_result.industry.length > 1)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_2 = niosh_result.industry[1].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_2 = "";
                }
                
                if(niosh_result.industry.length > 2)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_3 = niosh_result.industry[2].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_3 = "";
                }
                
                if(niosh_result.occupation.length > 0)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_1 = niosh_result.occupation[0].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_1 = "";
                }
                
                if(niosh_result.occupation.length > 1)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_2 = niosh_result.occupation[1].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_2 = "";
                }
                
                if(niosh_result.occupation.length > 2)
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_3 = niosh_result.occupation[2].code;
                }
                else
                {
                    g_data.birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_3 = "";
                }
                

            break;
            case "/social_and_environmental_profile/socio_economic_characteristics/occupation":
                niosh_result = await get_niosh_codes
                (
                    g_data.social_and_environmental_profile.socio_economic_characteristics.occupation,
                    null
                )
 

                if(niosh_result.industry.length > 0)
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_1 = niosh_result.industry[0].code;
                }
                else
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_1 = "";
                }
                
                if(niosh_result.industry.length > 1)
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_2 = niosh_result.industry[1].code;
                }
                else
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_2 = "";
                }
                
                if(niosh_result.industry.length > 2)
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_3 = niosh_result.industry[2].code;
                }
                else
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_3 = "";
                }
                
                if(niosh_result.occupation.length > 0)
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_1 = niosh_result.occupation[0].code;
                }
                else
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_1 = "";
                }
                
                if(niosh_result.occupation.length > 1)
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_2 = niosh_result.occupation[1].code;
                }
                else
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_2 = "";
                }
                
                if(niosh_result.occupation.length > 2)
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_3 = niosh_result.occupation[2].code;
                }
                else
                {
                    g_data.social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_3 = "";
                }
                


            break;
        }
    }
    
    /*
    if(is_log_auto_count)
    {
        console.log(`
        autorecalculate_count ${autorecalculate_count}
        autorecalculate_niosh ${autorecalculate_niosh}
        autorecalculate_independent ${autorecalculate_independent}
        autorecalculate_gestation_all ${autorecalculate_gestation_all}
        autorecalculate_gestation ${autorecalculate_gestation}
        autorecalculate_entry ${autorecalculate_entry}
        `);
    }
    */
}

async function autorecalculate_all_gestation
(
    p_form_index,
    p_grid_index
)
{
    const edd_year = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.year);
    const edd_month = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.month);
    const edd_day = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.day);
    const lmp_year = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.year);
    const lmp_month = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.month);
    const lmp_day = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.day);

    const edd_date = new Date(edd_year, edd_month - 1, edd_day);
    const lmp_date = new Date(lmp_year, lmp_month - 1, lmp_day);

    let is_edd = false;
    let is_lmp = false;


    const delivery_month = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month
    const delivery_day = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day
    const delivery_year = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year

    const delivery_date = new Date(delivery_year, delivery_month -1, delivery_day);
    const is_valid_delivery_date = $global.isValidDate(delivery_year, delivery_month, delivery_day);




    if 
    (
        //$global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(edd_year, edd_month, edd_day) == true
    )
    {
        is_edd = true;

    }
    else if 
    (
        //$global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(lmp_year, lmp_month, lmp_day) == true
    )
    {
        is_lmp = true;
    }
    else
    {
        //return;
    }
/*

    is_edd
    edd_date
    is_lmp
    lmp_date


    var event_year = parseInt(this.year);
    var event_month = parseInt(this.month);
    var event_day = parseInt(this.day);
    var event_date = autorecalculate_get_event_date();
    const result = new Date(event_year, event_month - 1, event_day);

    ga = $global.calc_ga_edd(event_date, edd_date);
    ga = $global.calc_ga_lmp(lmp_date, event_date);
    */






    let map = null;


    map = autorecalculate_get_event_date("/prenatal/current_pregnancy/date_of_last_prenatal_visit", is_edd, edd_date, is_lmp, lmp_date)
    if 
    (
        map.get('ga').length > 1 &&
        (
            ! is_valid_delivery_date ||
            is_valid_delivery_date &&
            map.get('event_date') <= delivery_date
        )
    ) 
    {
        g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit = map.get('ga')[0];
        g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit_days = map.get('ga')[1];
    }
    else
    {
        g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit = "";
        g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit_days = "";
    }

    $mmria.set_control_value
    (
        "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit", 
        g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit
    );

    $mmria.set_control_value
    (
        "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit_days", 
        g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit_days
    );
    

    map = autorecalculate_get_event_date("/prenatal/current_pregnancy/date_of_1st_prenatal_visit", is_edd, edd_date, is_lmp, lmp_date)
    if 
    (
        map.get('ga').length > 1 &&
        (
            ! is_valid_delivery_date ||
            is_valid_delivery_date &&
            map.get('event_date') <= delivery_date
        )
    ) 
    {
        g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_weeks = map.get('ga')[0];
        g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_days = map.get('ga')[1];
    }
    else
    {
        g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_weeks = "";
        g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_days = "";
    }

    $mmria.set_control_value
    (
        "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_weeks", 
        g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_weeks
    );

    $mmria.set_control_value
    (
        "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_days", 
        g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_days
    );
    

/*

    ga = autorecalculate_get_event_date("/prenatal/current_pregnancy/date_of_1st_ultrasound", is_edd, edd_date, is_lmp, lmp_date)
    if (ga.length > 1) 
    {
        if (ga.length > 1) 
        {
            g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.gestational_age_at_first_ultrasound = map.get('ga')[0];
            g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.gestational_age_at_first_ultrasound_days = map.get('ga')[1];
            $mmria.set_control_value('prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound', map.get('ga')[0]);
            $mmria.set_control_value('prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound_days', map.get('ga')[1]);
        }
    }*/

    // GRID
    g_data.prenatal.routine_monitoring.forEach
    (
        function (item, index) 
        {
            map = autorecalculate_get_event_date("/prenatal/routine_monitoring/date_and_time", is_edd, edd_date, is_lmp, lmp_date, null, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            )  
            {
                item.gestational_age_weeks = map.get('ga')[0];
                item.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.gestational_age_weeks = "";
                item.gestational_age_days = "";
            }

            $mmria.set_control_value("prenatal/routine_monitoring/gestational_age_weeks", item.gestational_age_weeks, null, index);
            $mmria.set_control_value("prenatal/routine_monitoring/gestational_age_days", item.gestational_age_days, null, index);
            
        }
    );

    // GRID
    g_data.prenatal.other_lab_tests.forEach
    (
        function (item, index) 
        {
            map = autorecalculate_get_event_date("/prenatal/other_lab_tests/date_and_time", is_edd, edd_date, is_lmp, lmp_date, null, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.gestational_age_weeks = map.get('ga')[0];
                item.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.gestational_age_weeks = "";
                item.gestational_age_days = "";
            }
                $mmria.set_control_value("prenatal/other_lab_tests/gestational_age_weeks", item.gestational_age_weeks,null,  index);
                $mmria.set_control_value("prenatal/other_lab_tests/gestational_age_days", item.gestational_age_days, null, index);
            
        }
    );
    // GRID 
    g_data.prenatal.diagnostic_procedures.forEach
    (
        function (item, index)
        {
            map = autorecalculate_get_event_date("/prenatal/diagnostic_procedures/date", is_edd, edd_date, is_lmp, lmp_date, null, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.gestational_age_weeks = map.get('ga')[0];
                item.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.gestational_age_weeks = "";
                item.gestational_age_days = "";
            }

            $mmria.set_control_value("prenatal/diagnostic_procedures/gestational_age_weeks", item.gestational_age_weeks, null, index);
            $mmria.set_control_value("prenatal/diagnostic_procedures/gestational_age_days", item.gestational_age_days, null, index);
            
        }
    );

    // GRID
    g_data.prenatal.problems_identified_grid.forEach
    (
        function(item, index)
        {
            map = autorecalculate_get_event_date("/prenatal/problems_identified_grid/date_1st_noted", is_edd, edd_date, is_lmp, lmp_date, null, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.gestational_age_weeks = map.get('ga')[0];
                item.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.gestational_age_weeks = "";
                item.gestational_age_days = "";
            }
                $mmria.set_control_value("prenatal/problems_identified_grid/gestational_age_weeks", item.gestational_age_weeks, null, index);
                $mmria.set_control_value("prenatal/problems_identified_grid/gestational_age_days", item.gestational_age_days, null, index);
            
        }
    );

    // GRID
    g_data.prenatal.medications_and_drugs_during_pregnancy.forEach
    (
        function(item, index)
        {
            map = autorecalculate_get_event_date("/prenatal/medications_and_drugs_during_pregnancy/date", is_edd, edd_date, is_lmp, lmp_date, null, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.gestational_age_weeks = map.get('ga')[0];
                item.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.gestational_age_weeks = "";
                item.gestational_age_days = "";
            }
                $mmria.set_control_value("prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks", item.gestational_age_weeks, null, index);
                $mmria.set_control_value("prenatal/medications_and_drugs_during_pregnancy/gestational_age_days", item.gestational_age_days, null, index);
            
        }
    );

    // GRID
    g_data.prenatal.pre_delivery_hospitalizations_details.forEach
    (
        function(item, index)
        {
            map = autorecalculate_get_event_date("/prenatal/pre_delivery_hospitalizations_details/date", is_edd, edd_date, is_lmp, lmp_date, null, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.gestational_age_weeks = map.get('ga')[0];
                item.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.gestational_age_weeks = "";
                item.gestational_age_days = "";
            }

            $mmria.set_control_value("prenatal/pre_delivery_hospitalizations_details/gestational_age_weeks", item.gestational_age_weeks, null, index);
            $mmria.set_control_value("prenatal/pre_delivery_hospitalizations_details/gestational_age_days", item.gestational_age_days, null, index);
            
        }
    );

    // GRID
    g_data.prenatal.medical_referrals.forEach
    (
        function(item, index)
        {
            map = autorecalculate_get_event_date("/prenatal/medical_referrals/date", is_edd, edd_date, is_lmp, lmp_date, null, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.gestational_age_weeks = map.get('ga')[0];
                item.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.gestational_age_weeks = "";
                item.gestational_age_days = "";
            }
                $mmria.set_control_value("prenatal/medical_referrals/gestational_age_weeks", item.gestational_age_weeks, null, index);
                $mmria.set_control_value("prenatal/medical_referrals/gestational_age_days", item.gestational_age_days, null, index);
            
        }
    );

    //er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_weeks
    g_data.er_visit_and_hospital_medical_records.forEach
    (
        function (item, index) 
        {
            map = autorecalculate_get_event_date("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival", is_edd, edd_date, is_lmp, lmp_date, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.basic_admission_and_discharge_information.date_of_arrival.gestational_age_weeks = map.get('ga')[0];
                item.basic_admission_and_discharge_information.date_of_arrival.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.basic_admission_and_discharge_information.date_of_arrival.gestational_age_weeks = "";
                item.basic_admission_and_discharge_information.date_of_arrival.gestational_age_days = "";
            }
            $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_weeks", item.basic_admission_and_discharge_information.date_of_arrival.gestational_age_weeks, index);
            $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_days", item.basic_admission_and_discharge_information.date_of_arrival.gestational_age_days, index);
            

            map = autorecalculate_get_event_date("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission", is_edd, edd_date, is_lmp, lmp_date, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            )  
            {
                item.basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_weeks = map.get('ga')[0];
                item.basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_weeks = "";
                item.basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_days = "";
            }

            $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_weeks", item.basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_weeks, index);
            $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_days", item.basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_days, index);
            

            map = autorecalculate_get_event_date("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge", is_edd, edd_date, is_lmp, lmp_date, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_weeks = map.get('ga')[0];
                item.basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_weeks = "";
                item.basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_days = "";
            }
            $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_weeks", item.basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_weeks, index);
            $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_days", item.basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_days, index);
            
        }
    );

    g_data.other_medical_office_visits.forEach
    (
        function (item, index) 
        {
            map = autorecalculate_get_event_date("/other_medical_office_visits/visit/date_of_medical_office_visit", is_edd, edd_date, is_lmp, lmp_date, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.visit.date_of_medical_office_visit.gestational_age_weeks = map.get('ga')[0];
                item.visit.date_of_medical_office_visit.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                item.visit.date_of_medical_office_visit.gestational_age_weeks = "";
                item.visit.date_of_medical_office_visit.gestational_age_days = "";
            }

            $mmria.set_control_value("other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_weeks", item.visit.date_of_medical_office_visit.gestational_age_weeks, index);
            $mmria.set_control_value("other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_days", item.visit.date_of_medical_office_visit.gestational_age_days, index);
            
        }
    );

    //GRID
    g_data.medical_transport.forEach
    (
        function (form_item, form_index) 
        {
            map = autorecalculate_get_event_date("/medical_transport/date_of_transport", is_edd, edd_date, is_lmp, lmp_date, form_index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                form_item.date_of_transport.gestational_age_weeks = map.get('ga')[0];
                form_item.date_of_transport.gestational_age_days = map.get('ga')[1];
            }
            else
            {
                form_item.date_of_transport.gestational_age_weeks = "";
                form_item.date_of_transport.gestational_age_days = "";
            }

            $mmria.set_control_value("medical_transport/date_of_transport/gestational_age_weeks", form_item.date_of_transport.gestational_age_weeks, form_index);
            $mmria.set_control_value("medical_transport/date_of_transport/gestational_age_days", form_item.date_of_transport.gestational_age_days, form_index);
            

            form_item.transport_vital_signs.forEach
            (
                function(grid_item, grid_index)
                {
                    map = autorecalculate_get_event_date("/medical_transport/transport_vital_signs/date_and_time", is_edd, edd_date, is_lmp, lmp_date, form_index, grid_index)
                    if 
                    (
                        map.get('ga').length > 1 &&
                        (
                            ! is_valid_delivery_date ||
                            is_valid_delivery_date &&
                            map.get('event_date') <= delivery_date
                        )
                    ) 
                    {
                        grid_item.gestational_weeks = map.get('ga')[0];
                        grid_item.gestational_days = map.get('ga')[1];
                    }
                    else
                    {
                        grid_item.gestational_weeks = "";
                        grid_item.gestational_days = "";
                    }

                    $mmria.set_control_value("medical_transport/transport_vital_signs/date_and_time/gestational_age_weeks", grid_item.gestational_weeks, form_index, grid_index);
                    $mmria.set_control_value("medical_transport/transport_vital_signs/date_and_time/gestational_age_days", grid_item.gestational_days, form_index, grid_index);
                    
                }
            );
        }
    )


    //GRID
    g_data.mental_health_profile.were_there_documented_mental_health_conditions.forEach
    (
        function(item, index)
        {
            map = autorecalculate_get_event_date("/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening", is_edd, edd_date, is_lmp, lmp_date, null, index)
            if 
            (
                map.get('ga').length > 1 &&
                (
                    ! is_valid_delivery_date ||
                    is_valid_delivery_date &&
                    map.get('event_date') <= delivery_date
                )
            ) 
            {
                item.gestational_weeks = map.get('ga')[0];
                item.gestational_days = map.get('ga')[1];  
            }
            else
            {
                item.gestational_weeks = "";
                item.gestational_days = ""; 
            }

            $mmria.set_control_value("mental_health_profile/were_there_documented_mental_health_conditions/gestational_age_weeks", item.gestational_weeks, null, index);
            $mmria.set_control_value("mental_health_profile/were_there_documented_mental_health_conditions/gestational_age_days", item.gestational_days, null, index);
            
        }
    );
        

/*
    /prenatal/other_lab_tests/gestational_age_weeks
    /prenatal/other_lab_tests/gestational_age_days
    /prenatal/diagnostic_procedures/gestational_age_weeks
    /prenatal/diagnostic_procedures/gestational_age_days
    /prenatal/problems_identified_grid/gestational_age_weeks
    /prenatal/problems_identified_grid/gestational_age_days
    /prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks
    /prenatal/medications_and_drugs_during_pregnancy/gestational_age_days
    /prenatal/pre_delivery_hospitalizations_details/gestational_age_weeks
    /prenatal/pre_delivery_hospitalizations_details/gestational_age_days
    /prenatal/medical_referrals/gestational_age_weeks
    /prenatal/medical_referrals/gestational_age_days
    /medical_transport/transport_vital_signs/gestational_weeks
    /medical_transport/transport_vital_signs/gestational_days
    /mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks
    /mental_health_profile/were_there_documented_mental_health_conditions/gestational_days
    */
    

}



function autorecalculate_get_event_date
(
    p_mmria_path,
    p_is_edd,
    p_edd_date,
    p_is_lmp,
    p_lmp_date,
    p_form_index,
    p_grid_index
)
{
    let result = [];

    let event_date = null;
/*
    /prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_weeks
    /prenatal/problems_identified_grid/gestational_age_days
    /prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks
    
    //multiform Grid
    /medical_transport/transport_vital_signs/gestational_weeks
    /medical_transport/transport_vital_signs/gestational_days
    

    //Grid
    /mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks
    /mental_health_profile/were_there_documented_mental_health_conditions/gestational_days
    /mental_health_profile/were_there_documented_mental_health_conditions/days_postpartum
   
   
   /er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge
   
   
    */
    

//birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses

    switch(p_mmria_path)
    {
        case "/prenatal/current_pregnancy/date_of_1st_ultrasound":
            event_date = autorecalculate_get_event_date_separate(g_data.prenatal.current_pregnancy.date_of_1st_ultrasound);
        break;
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge":
            event_date = autorecalculate_get_event_date_separate(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge);
        break;
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission":
            //console.log("here");
            event_date = autorecalculate_get_event_date_separate(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission);
        break;
        case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_weeks":
        case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_days":
        case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit":
        case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit/month":
        case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit/day":
        case "/prenatal/current_pregnancy/date_of_1st_prenatal_visit/year":
            event_date = autorecalculate_get_event_date_separate(g_data.prenatal.current_pregnancy.date_of_1st_prenatal_visit);
        break;
        case "/prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit":
        case "/prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit_days":
        case "/prenatal/current_pregnancy/date_of_last_prenatal_visit":
        case "/prenatal/current_pregnancy/date_of_last_prenatal_visit/month":
        case "/prenatal/current_pregnancy/date_of_last_prenatal_visit/day":
        case "/prenatal/current_pregnancy/date_of_last_prenatal_visit/year":
  
            event_date = autorecalculate_get_event_date_separate(g_data.prenatal.current_pregnancy.date_of_last_prenatal_visit);
        break;
        case "/prenatal/routine_monitoring/gestational_age_weeks":
        case "/prenatal/routine_monitoring/gestational_age_days":
        case "/prenatal/routine_monitoring/date_and_time":
            event_date = autorecalculate_get_event_date_combined(g_data.prenatal.routine_monitoring[p_grid_index].date_and_time);
        break;
        case "/prenatal/other_lab_tests/gestational_age_weeks":
        case "/prenatal/other_lab_tests/gestational_age_days":
        case "/prenatal/other_lab_tests/date_and_time":
            event_date = autorecalculate_get_event_date_combined(g_data.prenatal.other_lab_tests[p_grid_index].date_and_time);
        break;
        case "/prenatal/diagnostic_procedures/gestational_age_weeks":
        case "/prenatal/diagnostic_procedures/gestational_age_days":
        case "/prenatal/diagnostic_procedures/date":
            event_date = autorecalculate_get_event_date_combined(g_data.prenatal.diagnostic_procedures[p_grid_index].date)
        break;
        case "/prenatal/problems_identified_grid/gestational_age_weeks":
        case "/prenatal/problems_identified_grid/gestational_age_days":
        case "/prenatal/problems_identified_grid/date_1st_noted":
            event_date = autorecalculate_get_event_date_combined(g_data.prenatal.problems_identified_grid[p_grid_index].date_1st_noted)
        break;
        case "/prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks":
        case "/prenatal/medications_and_drugs_during_pregnancy/gestational_age_days":
        case "/prenatal/medications_and_drugs_during_pregnancy/date":
            event_date = autorecalculate_get_event_date_combined(g_data.prenatal.medications_and_drugs_during_pregnancy[p_grid_index].date)
        break;
        case "/prenatal/pre_delivery_hospitalizations_details/gestational_age_weeks":
        case "/prenatal/pre_delivery_hospitalizations_details/gestational_age_days":
        case "/prenatal/pre_delivery_hospitalizations_details/date":
            event_date = autorecalculate_get_event_date_combined(g_data.prenatal.pre_delivery_hospitalizations_details[p_grid_index].date)
        break;
        case "/prenatal/medical_referrals/gestational_age_weeks":
        case "/prenatal/medical_referrals/gestational_age_days":
        case "/prenatal/medical_referrals/date":
            event_date = autorecalculate_get_event_date_combined(g_data.prenatal.medical_referrals[p_grid_index].date)
        break;
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_weeks":
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_days":
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival":
            event_date = autorecalculate_get_event_date_separate(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival)
        break;
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_weeks":
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_days":
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission":
            event_date = autorecalculate_get_event_date_separate(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission)
        break;
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_weeks":
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_days":
        case "/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge":
            event_date = autorecalculate_get_event_date_separate(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge)
        break;
        case "/other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_weeks":
        case "/other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_days":
        case "/other_medical_office_visits/visit/date_of_medical_office_visit":
            event_date = autorecalculate_get_event_date_separate(g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit)
        break;
        case "/medical_transport/date_of_transport/gestational_age_weeks":
        case "/medical_transport/date_of_transport/gestational_age_days":
        case "/medical_transport/date_of_transport":
            event_date = autorecalculate_get_event_date_separate(g_data.medical_transport[p_form_index].date_of_transport)
        break;
        case "/medical_transport/transport_vital_signs/gestational_weeks":
        case "/medical_transport/transport_vital_signs/gestational_days":
        case "/medical_transport/transport_vital_signs/date_and_time":
            event_date = autorecalculate_get_event_date_combined(g_data.medical_transport[p_form_index].transport_vital_signs[p_grid_index].date_and_time)
        break;
        case "/mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks":
        case "/mental_health_profile/were_there_documented_mental_health_conditions/gestational_days":
        case "/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening":
            event_date = autorecalculate_get_event_date_combined(g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].date_of_screening);
        break;
        case "/medical_transport/date_of_transport":
        case "/medical_transport/date_of_transport/year":
        case "/medical_transport/date_of_transport/month":
        case "/medical_transport/date_of_transport/day":
            event_date = autorecalculate_get_event_date_separate(g_data.medical_transport[p_form_index].date_of_transport);
        break;
        default:
            console.log("autorecalculate_get_event_date: switch missing" + p_mmria_path)
            break;
        /*
        /prenatal/routine_monitoring/date_and_time
        /prenatal/other_lab_tests/date_and_time
        /prenatal/diagnostic_procedures/date
        /prenatal/problems_identified_grid/date_1st_noted   
        /prenatal/pre_delivery_hospitalizations_details/date    
        /medical_transport/transport_vital_signs/date_and_time
        /mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening        
        */

    }

    if(event_date != null)
    {
        if(p_is_edd)
        {
            result = $global.calc_ga_edd(event_date, p_edd_date);
        }
        else if(p_is_lmp)
        {
            result = $global.calc_ga_lmp(p_lmp_date, event_date);
        }
    }

    const map = new Map();
    map.set('event_date', event_date);
    map.set('ga', result);
    return map;
}

function autorecalculate_get_event_date_separate(p_value)
{
    let result = null;

    const event_year = parseInt(p_value.year);
    const event_month = parseInt(p_value.month);
    const event_day = parseInt(p_value.day);

    if
    (
        event_year != null &&
        event_month != null &&
        event_day != null &&
        event_year != "" &&
        event_month != "" &&
        event_day != "" &&
        event_year != 9999 &&
        event_month != 9999 &&
        event_day != 9999
    )
    {
        result = new Date(event_year, event_month - 1, event_day);
    }


    return result;
}

function autorecalculate_get_event_date_combined(p_value)
{
    let result = null;

    if(p_value != null)
    {
        const arr = p_value.split("-");
        if(arr.length > 2)
        {
            const event_year = parseInt(arr[0]);
            const event_month = parseInt(arr[1]);
            const event_day = parseInt(arr[2]);

            result = new Date(event_year, event_month - 1, event_day);
        }
    }

    return result;
}




/*
/death_certificate/demographics/occupation_business_industry
/death_certificate/demographics/primary_occupation

/birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry
/birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation

/birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry
/birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation

/social_and_environmental_profile/socio_economic_characteristics/occupation

*/

const niosh_autocalc_set = new Set();

/*

 Industry and Occupation Code 1-3

NIOSH Industry and Occupation Computerized Coding System (NIOCCS)
 - Mother’s Occupation Code #1 on SEP. 
 See (external link) 
 https://csams.cdc.gov/nioccs/HelpWebService.aspx and 
 https://csams.cdc.gov/nioccs/HelpCodingSchemes.aspx for value descriptions.
*/


//death_certificate/demographics/occupation_business_industry
//death_certificate/demographics/primary_occupation
niosh_autocalc_set.add("/death_certificate/demographics/dc_m_industry_code_1");
niosh_autocalc_set.add("/death_certificate/demographics/dc_m_industry_code_2");
niosh_autocalc_set.add("/death_certificate/demographics/dc_m_industry_code_3");
niosh_autocalc_set.add("/death_certificate/demographics/dc_m_occupation_code_1");
niosh_autocalc_set.add("/death_certificate/demographics/dc_m_occupation_code_2");
niosh_autocalc_set.add("/death_certificate/demographics/dc_m_occupation_code_3");

//birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry
//birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation

niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_1");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_2");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_3");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_1");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_2");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_3");

//birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry
//birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation

niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_1");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_2");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_3");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_1");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_2");
niosh_autocalc_set.add("/birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_3");

//social_and_environmental_profile/socio_economic_characteristics/occupation


niosh_autocalc_set.add("/social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_1");
niosh_autocalc_set.add("/social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_2");
niosh_autocalc_set.add("/social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_3");
niosh_autocalc_set.add("/social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_1");
niosh_autocalc_set.add("/social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_2");
niosh_autocalc_set.add("/social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_3");


async function get_niosh_codes(p_occupation, p_industry)
{
    let result = { Industry:[], Occupation:[] };
    const builder = [ `${location.protocol}//${location.host}/api/niosh?` ];
    let has_occupation = false;
    let has_industry = false;

    if(p_occupation && p_occupation.length > 0)
    {
        has_occupation = true;
        builder.push(`o=${p_occupation}`);
    }

    if(p_industry && p_industry.length > 0)
    {
        has_industry = true;

        if(has_occupation)
        {
            builder.push(`&i=${p_industry}`);
        }
        else
        {
            builder.push(`i=${p_industry}`);
        }
    }

    if(has_occupation || has_industry)
    {
        const niosh_url = builder.join("");

        try
        {
            result = await $.ajax
            ({
                url: niosh_url,
            });
        }
        catch(e)
        {
            // do nothing for now
        }
    }
    //{"Industry": [{"Code": "611110","Title": "Elementary and Secondary Schools","Probability": "9.999934E-001"},{"Code": "611310","Title": "Colleges, Universities, and Professional Schools","Probability": "2.598214E-006"},{"Code": "009990","Title": "Insufficient information","Probability": "2.312557E-006"}],"Occupation": [{"Code": "00-9900","Title": "Insufficient Information","Probability": "9.999897E-001"},{"Code": "11-9032","Title": "Education Administrators, Elementary and Secondary School","Probability": "6.550550E-006"},{"Code": "53-3022","Title": "Bus Drivers, School or Special Client","Probability": "4.932875E-007"}],"Scheme": "NAICS 2012 and SOC 2010"}
    return result;

}




const autocalc_map = new Map([
    [ "/birth_fetal_death_certificate_parent/maternal_biometrics/height_feet", arc_prepregnancy_bmi],
    [ "/birth_fetal_death_certificate_parent/maternal_biometrics/height_inches", arc_prepregnancy_bmi],
    [ "/birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight", arc_prepregnancy_bmi],

    [ "/autopsy_report.biometrics/mother/height/feet", arc_autopsy_bmi ],
    [ "/autopsy_report.biometrics/mother/height/inches", arc_autopsy_bmi ],
    [ "/autopsy_report.biometrics/mother/weight", arc_autopsy_bmi ],

    [ "/prenatal/current_pregnancy/height/feet", arc_prenatal_bmi ],
    [ "/prenatal/current_pregnancy/height/inches", arc_prenatal_bmi ],
    [ "/prenatal/current_pregnancy/pre_pregnancy_weight", arc_prenatal_bmi ],
    
    [ "/er_visit_and_hospital_medical_records/maternal_biometrics/height/feet", arc_er_hosp_bmi ],
    [ "/er_visit_and_hospital_medical_records/maternal_biometrics/height/inches", arc_er_hosp_bmi ],
    [ "/er_visit_and_hospital_medical_records/maternal_biometrics/admission_weight", arc_er_hosp_bmi ],
    
]
);


autocalc_map.safe_set = function (p_path, p_function)
{
    if(this.has(p_path))
    {
        if(Array.isArray( this.get(p_path) ))
        {
            this.get(p_path).push(p_function);
        }
        else
        {
            let current_function = this.get(p_path);

            this.set(p_path, [ current_function, p_function ]);
        }
    }
    else
    {
        this.set(p_path, p_function);

    }
}

//$calc_bmi(p_height, p_weight) 

//CALCULATE PRE-PREGNANCY BMI ON BC
/*
path=birth_fetal_death_certificate_parent/maternal_biometrics/bmi
event=onfocus

birth_fetal_death_certificate_parent/maternal_biometrics/bmi
birth_fetal_death_certificate_parent/maternal_biometrics/height_feet
birth_fetal_death_certificate_parent/maternal_biometrics/height_inches
birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight
*/
function arc_prepregnancy_bmi() 
{
    var bmi = null;
    var height_feet = parseFloat(g_data.birth_fetal_death_certificate_parent.maternal_biometrics.height_feet);
    var height_inches = parseFloat(g_data.birth_fetal_death_certificate_parent.maternal_biometrics.height_inches);
    var weight = parseFloat(g_data.birth_fetal_death_certificate_parent.maternal_biometrics.pre_pregnancy_weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) 
    {
        var bmi = $global.calc_bmi(height, weight);
        g_data.birth_fetal_death_certificate_parent.maternal_biometrics.bmi = bmi;
        $mmria.set_control_value("birth_fetal_death_certificate_parent/maternal_biometrics/bmi", bmi);
        
    }
}
//CALCULATE BMI FOR AUTOPSY
/*
path=autopsy_report/biometrics/mother/bmi
event=onfocus
*/
function arc_autopsy_bmi() 
{
    var bmi = null;
    var height_feet = parseFloat(g_data.autopsy_report.biometrics.mother.height.feet);
    var height_inches = parseFloat(g_data.autopsy_report.biometrics.mother.height.inches);
    var weight = parseFloat(g_data.autopsy_report.biometrics.mother.weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) 
    {
        var bmi = $global.calc_bmi(height, weight);
        g_data.autopsy_report.biometrics.mother.bmi = bmi;
        $mmria.set_control_value("autopsy_report/biometrics/mother/bmi", bmi);
    }
}
//CALCULATE BMI FOR PRENATAL CARE RECORD
/*
path=prenatal/current_pregnancy/bmi
event=onfocus

prenatal/current_pregnancy/height/feet
prenatal/current_pregnancy/height/inches
prenatal/current_pregnancy/pre_pregnancy_weight
arc_prenatal_bmi
*/
function arc_prenatal_bmi() 
{
    var bmi = null;
    var height_feet = parseFloat(g_data.prenatal.current_pregnancy.height.feet);
    var height_inches = parseFloat(g_data.prenatal.current_pregnancy.height.inches);
    var weight = parseFloat(g_data.prenatal.current_pregnancy.pre_pregnancy_weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) 
    {
        var bmi = $global.calc_bmi(height, weight);
        g_data.prenatal.current_pregnancy.bmi = bmi;
        $mmria.set_control_value("prenatal/current_pregnancy/bmi", bmi);
    }
}
//CALCULATE BMI FOR ER VISIT OR HOSPITALIZATION MEDICAL RECORD
/*
path=er_visit_and_hospital_medical_records/maternal_biometrics/height/bmi
event=onfocus

er_visit_and_hospital_medical_records/maternal_biometrics
/er_visit_and_hospital_medical_records/maternal_biometrics/height/feet
/er_visit_and_hospital_medical_records/maternal_biometrics/height/inches
/er_visit_and_hospital_medical_records/maternal_biometrics/admission_weight

arc_er_hosp_bmi
g_data.er_visit_and_hospital_medical_records[p_form_index].maternal_biometrics

*/
function arc_er_hosp_bmi(p_form_index) 
{
    var bmi = null;
    var height_feet = parseFloat(g_data.er_visit_and_hospital_medical_records[p_form_index].maternal_biometrics.height.feet);
    var height_inches = parseFloat(g_data.er_visit_and_hospital_medical_records[p_form_index].maternal_biometrics.height.inches);

    var weight = parseFloat(g_data.er_visit_and_hospital_medical_records[p_form_index].maternal_biometrics.admission_weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) 
    {
        var bmi = $global.calc_bmi(height, weight);
        g_data.er_visit_and_hospital_medical_records[p_form_index].maternal_biometrics.height.bmi = bmi;
        $mmria.set_control_value("er_visit_and_hospital_medical_records/maternal_biometrics/height/bmi", bmi, p_form_index);
    }
}


//CALCULATE WEIGHT GAIN DURING PREGNANCY ON BC
/*
path=birth_fetal_death_certificate_parent/maternal_biometrics/weight_gain
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery", arc_weight_gain );
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight", arc_weight_gain );

function arc_weight_gain() 
{
    var weight_del = parseFloat(g_data.birth_fetal_death_certificate_parent.maternal_biometrics.weight_at_delivery);
    var weight_pp = parseFloat(g_data.birth_fetal_death_certificate_parent.maternal_biometrics.pre_pregnancy_weight);
    if (weight_del > 50 && weight_del < 800 && weight_pp > 50 && weight_pp < 800) 
    {
        var gain = weight_del - weight_pp;
        g_data.birth_fetal_death_certificate_parent.maternal_biometrics.weight_gain = gain;
        $mmria.set_control_value("birth_fetal_death_certificate_parent/maternal_biometrics/weight_gain", gain);
    }
}
//CALCULATE WEIGHT GAIN DURING PREGNANCY ) ON PC
/*
path=prenatal/current_pregnancy/weight_gain
event=onfocus
*/

autocalc_map.safe_set("/prenatal/current_pregnancy/weight_at_last_visit", arc_weight_gain2);
autocalc_map.safe_set("/prenatal/current_pregnancy/pre_pregnancy_weight", arc_weight_gain2);

function arc_weight_gain2() 
{
    var weight_del = parseFloat(g_data.prenatal.current_pregnancy.weight_at_last_visit);
    var weight_pp = parseFloat(g_data.prenatal.current_pregnancy.pre_pregnancy_weight);
    if (weight_del > 50 && weight_del < 800 && weight_pp > 50 && weight_pp < 800) 
    {
        var gain = weight_del - weight_pp;
        g_data.prenatal.current_pregnancy.weight_gain = gain;
        $mmria.set_control_value("prenatal/current_pregnancy/weight_gain", gain);
    }
}
//CALCULATE INTER-BIRTH INTERVAL IN MONTHS ON BC
/*
path=birth_fetal_death_certificate_parent/pregnancy_history/live_birth_interval
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year", arc_birth_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month", arc_birth_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/day", arc_birth_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_birth_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_birth_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_birth_interval);

function arc_birth_interval() 
{
    var interval = null;
    var start_year = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.year);
    var start_month = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.month);
    var start_day = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.day);
    var event_year = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var event_month = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var event_day = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    if ($global.isValidDate(start_year, start_month, start_day) == true && $global.isValidDate(event_year, event_month, event_day) == true) 
    {
        var start_date = new Date(start_year, start_month - 1, start_day);
        var event_date = new Date(event_year, event_month - 1, event_day);
        interval = Math.trunc($global.calc_days(start_date, event_date) / 30.4375);
        g_data.birth_fetal_death_certificate_parent.pregnancy_history.live_birth_interval = interval;
        $mmria.set_control_value("birth_fetal_death_certificate_parent/pregnancy_history/live_birth_interval", interval);
    }
}

/*

This should NOT be calculated

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year", arc_prenatal_1st_ultra_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month", arc_prenatal_1st_ultra_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/day", arc_prenatal_1st_ultra_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_prenatal_1st_ultra_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_prenatal_1st_ultra_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_prenatal_1st_ultra_ga);


autocalc_map.safe_set("/prenatal/current_pregnancy/date_of_1st_ultrasound/month", arc_prenatal_1st_ultra_ga);
autocalc_map.safe_set("/prenatal/current_pregnancy/date_of_1st_ultrasound/day", arc_prenatal_1st_ultra_ga);
autocalc_map.safe_set("/prenatal/current_pregnancy/date_of_1st_ultrasound/year", arc_prenatal_1st_ultra_ga);

function arc_prenatal_1st_ultra_ga(p_control) 
{
    var ga = [];
    var weeks = null;
    var days = null;
    var event_year = parseInt(g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.year);
    var event_month = parseInt(g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.month);
    var event_day = parseInt(g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.day);
    var edd_year = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.year);
    var edd_month = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.month);
    var edd_day = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.day);
    var lmp_year = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.year);
    var lmp_month = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.month);
    var lmp_day = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.day);
    var edd_date = new Date(edd_year, edd_month - 1, edd_day);
    var lmp_date = new Date(lmp_year, lmp_month - 1, lmp_day);
    var event_date = new Date(event_year, event_month - 1, event_day);
    if 
    (
        $global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(edd_year, edd_month, edd_day) == true
    ) 
    {
        ga = $global.calc_ga_edd(event_date, edd_date);
        if (ga.length > 1) 
        {
            g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.gestational_age_at_first_ultrasound = map.get('ga')[0];
            g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.gestational_age_at_first_ultrasound_days = map.get('ga')[1];
            $mmria.set_control_value('prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound', map.get('ga')[0]);
            $mmria.set_control_value('prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound_days', map.get('ga')[1]);
        }
    } 
    else if 
    (
        $global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(lmp_year, lmp_month, lmp_day) == true
    ) 
    {
        ga = $global.calc_ga_lmp(lmp_date, event_date);
        if (ga.length > 1) 
        {
            g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.gestational_age_at_first_ultrasound = map.get('ga')[0];
            g_data.prenatal.current_pregnancy.date_of_1st_ultrasound.gestational_age_at_first_ultrasound_days = map.get('ga')[1];
            $mmria.set_control_value('prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound', map.get('ga')[0]);
            $mmria.set_control_value('prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound_days', map.get('ga')[1]);
        }
    }
}

*/

//CALCULATE INTER-PREGNANCY INTERVAL IN MONTHS ON BC
/*
path=birth_fetal_death_certificate_parent/pregnancy_history/pregnancy_interval
event=onfocus
*/


autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/year", arc_pregnancy_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/month", arc_pregnancy_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/day", arc_pregnancy_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year", arc_pregnancy_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month", arc_pregnancy_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/day", arc_pregnancy_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_pregnancy_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_pregnancy_interval);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_pregnancy_interval);


function arc_pregnancy_interval() 
{
    var interval = null;
    var fd_year = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_other_outcome.year);
    var fd_month = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_other_outcome.month);
    var fd_day = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_other_outcome.day);
    var lb_year = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.year);
    var lb_month = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.month);
    var lb_day = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.day);
    var event_year = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var event_month = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var event_day = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    if ($global.isValidDate(fd_year, fd_month, fd_day) == true && $global.isValidDate(event_year, event_month, event_day) == true) 
    {
        var fd_date = new Date(fd_year, fd_month - 1, fd_day);
        var event_date = new Date(event_year, event_month - 1, event_day);
        interval = Math.trunc($global.calc_days(fd_date, event_date) / 30.4375);
        g_data.birth_fetal_death_certificate_parent.pregnancy_history.pregnancy_interval = interval;
        $mmria.set_control_value("birth_fetal_death_certificate_parent/pregnancy_history/pregnancy_interval", interval);
    } 
    else if ($global.isValidDate(lb_year, lb_month, lb_day) == true && $global.isValidDate(event_year, event_month, event_day) == true) 
    {
        var lb_date = new Date(lb_year, lb_month - 1, lb_day);
        var event_date = new Date(event_year, event_month - 1, event_day);
        interval = Math.trunc($global.calc_days(lb_date, end_date) / 30.4375);
        g_data.birth_fetal_death_certificate_parent.pregnancy_history.pregnancy_interval = interval;
        $mmria.set_control_value("birth_fetal_death_certificate_parent/pregnancy_history/pregnancy_interval", interval);
    }
}


//CALCULATE DAYS BETWEEN BIRTH OF CHILD AND DEATH OF MOM
/*
path=birth_fetal_death_certificate_parent/cmd_length_between_child_birth_and_death_of_mother
event=onclick
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_birth_2_death);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_birth_2_death);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_birth_2_death);


autocalc_map.safe_set("/home_record/date_of_death/year", arc_birth_2_death);
autocalc_map.safe_set("/home_record/date_of_death/month", arc_birth_2_death);
autocalc_map.safe_set("/home_record/date_of_death/day", arc_birth_2_death);

/*
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_birth_2_death);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_birth_2_death);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_birth_2_death);


function arc_birth_2_death() 
{
    var days = null;
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var start_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var end_year = parseInt(g_data.home_record.date_of_death.year);
    var end_month = parseInt(g_data.home_record.date_of_death.month);
    var end_day = parseInt(g_data.home_record.date_of_death.day);
    if ($global.isValidDate(start_year, start_month, start_day) == true && $global.isValidDate(end_year, end_month, end_day) == true) 
    {
        var start_date = new Date(start_year, start_month - 1, start_day);
        var end_date = new Date(end_year, end_month - 1, end_day);
        var days = $global.calc_days(start_date, end_date);
        g_data.birth_fetal_death_certificate_parent.length_between_child_birth_and_death_of_mother = days;
        $mmria.set_control_value("birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother", days);
    }
}*/

//CALCULATE POST-PARTUM DAYS ON ER-HOSPITAL FORM AT ARRIVAL
/*
path=er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/days_postpartum
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_eha_days_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_eha_days_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_eha_days_postpartum);

autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/year", arc_eha_days_postpartum);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/month", arc_eha_days_postpartum);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/day", arc_eha_days_postpartum);




function arc_eha_days_postpartum(p_form_index) 
{
    if(p_form_index == undefined) return;
    
    let days = null;
    let start_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    let start_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    let start_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    let end_year = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.year);
    let end_month = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.month);
    let end_day = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.day);
    let start_date = new Date(start_year, start_month - 1, start_day);
    let end_date = new Date(end_year, end_month - 1, end_day);
    if 
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true && 
        start_date < end_date
    ) 
    {
        days = $global.calc_days(start_date, end_date);
        g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.days_postpartum = days;
    }
    else
    {
        g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.days_postpartum = days;
    }

    $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/days_postpartum", days);
}
//CALCULATE POST-PARTUM DAYS ON ER-HOSPITAL FORM AT ADMISSION
/*
path=er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/days_postpartum
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_eha_days_postpartum2);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_eha_days_postpartum2);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_eha_days_postpartum2);


autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/year", arc_eha_days_postpartum2);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/month", arc_eha_days_postpartum2);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/day", arc_eha_days_postpartum2);



function arc_eha_days_postpartum2(p_form_index) 
{

    if(p_form_index == null) return;

    var days = null;
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var start_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var end_year = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.year);
    var end_month = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.month);
    var end_day = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.day);
    var start_date = new Date(start_year, start_month - 1, start_day);
    var end_date = new Date(end_year, end_month - 1, end_day);
    if 
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true && 
        start_date < end_date
    ) 
    {
        days = $global.calc_days(start_date, end_date);
        g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.days_postpartum = days;
    }
    else
    {
        g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_admission.days_postpartum = "";
    }

    $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/days_postpartum", days);
}
//CALCULATE POST-PARTUM DAYS ON ER-HOSPITAL FORM AT DISCHARGE
/*
path=er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/days_postpartum
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_ehd_days_dhdc_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_ehd_days_dhdc_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_ehd_days_dhdc_postpartum);


autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/year", arc_ehd_days_dhdc_postpartum);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/month", arc_ehd_days_dhdc_postpartum);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/day", arc_ehd_days_dhdc_postpartum);


function arc_ehd_days_dhdc_postpartum(p_form_index) 
{
    if(p_form_index == null) return;

    var days = null;
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var start_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var end_year = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.year);
    var end_month = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.month);
    var end_day = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.day);
    var start_date = new Date(start_year, start_month - 1, start_day);
    var end_date = new Date(end_year, end_month - 1, end_day);
    if 
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true && 
        start_date < end_date
    ) 
    {
        days = $global.calc_days(start_date, end_date);
        g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.days_postpartum = days;
    }
    else
    {
        g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_hospital_discharge.days_postpartum = "";
    }

    $mmria.set_control_value("er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/days_postpartum", days);
}
//CALCULATE DAYS POST-PARTUM ON OFFICE VISIT FORM
/*
path=other_medical_office_visits/visit/date_of_medical_office_visit/days_postpartum
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_omov_days_calc_gestataion_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_omov_days_calc_gestataion_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_omov_days_calc_gestataion_postpartum);

autocalc_map.safe_set("/other_medical_office_visits/visit/date_of_medical_office_visit/year", arc_omov_days_calc_gestataion_postpartum);
autocalc_map.safe_set("/other_medical_office_visits/visit/date_of_medical_office_visit/month", arc_omov_days_calc_gestataion_postpartum);
autocalc_map.safe_set("/other_medical_office_visits/visit/date_of_medical_office_visit/day", arc_omov_days_calc_gestataion_postpartum);



function arc_omov_days_calc_gestataion_postpartum(p_form_index) 
{
    if(p_form_index == null) return;

    var days = null;
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var start_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var end_year = parseInt(g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.year);
    var end_month = parseInt(g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.month);
    var end_day = parseInt(g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.day);
    var start_date = new Date(start_year, start_month - 1, start_day);
    var end_date = new Date(end_year, end_month - 1, end_day);

    if 
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true && 
        start_date < end_date
    ) 
    {
        days = $global.calc_days(start_date, end_date);
        g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.days_postpartum = days;    
    }
    else
    {
        g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.days_postpartum = "";
    }

    $mmria.set_control_value("other_medical_office_visits/visit/date_of_medical_office_visit/days_postpartum", g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.days_postpartum, p_form_index);

    var edd_year = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.year);
    var edd_month = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.month);
    var edd_day = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.day);
    var lmp_year = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.year);
    var lmp_month = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.month);
    var lmp_day = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.day);
    var edd_date = new Date(edd_year, edd_month - 1, edd_day);
    var lmp_date = new Date(lmp_year, lmp_month - 1, lmp_day);


    let is_edd = false;
    let is_lmp = false;

    if 
    (
        //$global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(edd_year, edd_month, edd_day) == true
    )
    {
        is_edd = true;

    }
    else if 
    (
        //$global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(lmp_year, lmp_month, lmp_day) == true
    )
    {
        is_lmp = true;
    }
    else
    {
        //return;
    }

    const delivery_month = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month
    const delivery_day = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day
    const delivery_year = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year

    const delivery_date = new Date(delivery_year, delivery_month -1, delivery_day);
    const is_valid_delivery_date = $global.isValidDate(delivery_year, delivery_month, delivery_day);

    const map = autorecalculate_get_event_date("/other_medical_office_visits/visit/date_of_medical_office_visit", is_edd, edd_date, is_lmp, lmp_date, p_form_index)
    if 
    (
        map.get('ga').length > 1 &&
        (
            ! is_valid_delivery_date ||
            is_valid_delivery_date &&
            map.get('event_date') <= delivery_date
        )
    ) 
    {
        g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_weeks = map.get('ga')[0];
        g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_days = map.get('ga')[1];
    }
    else
    {
        g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_weeks = "";
        g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_days = "";
    }

    $mmria.set_control_value("other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_weeks", g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_weeks, p_form_index);
    $mmria.set_control_value("other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_days", g_data.other_medical_office_visits[p_form_index].visit.date_of_medical_office_visit.gestational_age_days, p_form_index);



}
//CALCULATE DAYS POST-PARTUM IN MEDICAL TRANSPORT FORM 
/*
path=medical_transport/date_of_transport/days_postpartum
event=onfocus
*/


autocalc_map.safe_set("/medical_transport/date_of_transport/year", arc_mt_days_postpartum);
autocalc_map.safe_set("/medical_transport/date_of_transport/month", arc_mt_days_postpartum);
autocalc_map.safe_set("/medical_transport/date_of_transport/day", arc_mt_days_postpartum);

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_mt_days_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_mt_days_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_mt_days_postpartum);

function arc_mt_days_postpartum(p_form_index) 
{

    if(p_form_index == null) return;

    var days = null;
    var end_year = parseInt(g_data.medical_transport[p_form_index].date_of_transport.year);
    var end_month = parseInt(g_data.medical_transport[p_form_index].date_of_transport.month);
    var end_day = parseInt(g_data.medical_transport[p_form_index].date_of_transport.day);
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var start_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var start_date = new Date(start_year, start_month - 1, start_day);
    var end_date = new Date(end_year, end_month - 1, end_day);
    if 
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true && 
        start_date < end_date
    ) 
    {
        days = $global.calc_days(start_date, end_date);
        g_data.medical_transport[p_form_index].date_of_transport.days_postpartum = days;
    }
    else
    {
        g_data.medical_transport[p_form_index].date_of_transport.days_postpartum = "";
    }

    $mmria.set_control_value("medical_transport/date_of_transport/days_postpartum", g_data.medical_transport[p_form_index].date_of_transport.days_postpartum);
 


    var edd_year = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.year);
    var edd_month = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.month);
    var edd_day = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.day);
    var lmp_year = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.year);
    var lmp_month = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.month);
    var lmp_day = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.day);
    var edd_date = new Date(edd_year, edd_month - 1, edd_day);
    var lmp_date = new Date(lmp_year, lmp_month - 1, lmp_day);

    let is_edd = false;
    let is_lmp = false;

    if 
    (
        //$global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(edd_year, edd_month, edd_day) == true
    )
    {
        is_edd = true;

    }
    else if 
    (
        //$global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(lmp_year, lmp_month, lmp_day) == true
    )
    {
        is_lmp = true;
    }
    else
    {
        return;
    }

    const delivery_month = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month
    const delivery_day = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day
    const delivery_year = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year

    const delivery_date = new Date(delivery_year, delivery_month -1, delivery_day);
    const is_valid_delivery_date = $global.isValidDate(delivery_year, delivery_month, delivery_day);

    const map = autorecalculate_get_event_date("/medical_transport/date_of_transport", is_edd, edd_date, is_lmp, lmp_date, p_form_index)
    if 
    (
        map.get('ga').length > 1 &&
        (
            ! is_valid_delivery_date ||
            is_valid_delivery_date &&
            map.get('event_date') <= delivery_date
        )
    ) 
    {
        
        g_data.medical_transport[p_form_index].date_of_transport.gestational_age_weeks = map.get('ga')[0];
        g_data.medical_transport[p_form_index].date_of_transport.gestational_age_days = map.get('ga')[1];
    }
    else
    {
        g_data.medical_transport[p_form_index].date_of_transport.gestational_age_weeks = "";
        g_data.medical_transport[p_form_index].date_of_transport.gestational_age_days = "";
    }
    
    $mmria.set_control_value("medical_transport/date_of_transport/gestational_age_weeks", g_data.medical_transport[p_form_index].date_of_transport.gestational_age_weeks, p_form_index);
    $mmria.set_control_value("medical_transport/date_of_transport/gestational_age_days", g_data.medical_transport[p_form_index].date_of_transport.gestational_age_days, p_form_index);

}
//CALCULATE DAYS POST-PARTUM IN MENTAL HEALTH FORM IN MENTAL HEALTH CONDITIONS GRID
/*
path=mental_health_profile/were_there_documented_mental_health_conditions/days_postpartum
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_mh_days_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_mh_days_postpartum);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_mh_days_postpartum);

autocalc_map.safe_set("/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening", arc_mh_days_postpartum);

function arc_mh_days_postpartum(p_form_index, p_grid_index) 
{
    //if(p_form_index == null) return;

    if(p_grid_index == null) return;


    const delivery_month = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month
    const delivery_day = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day
    const delivery_year = g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year

    const delivery_date = new Date(delivery_year, delivery_month -1, delivery_day);
    const is_valid_delivery_date = $global.isValidDate(delivery_year, delivery_month, delivery_day);

    var days = null;
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var start_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var start_date = new Date(start_year, start_month - 1, start_day);

    var edd_year = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.year);
    var edd_month = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.month);
    var edd_day = parseInt(g_data.prenatal.current_pregnancy.estimated_date_of_confinement.day);
    var lmp_year = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.year);
    var lmp_month = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.month);
    var lmp_day = parseInt(g_data.prenatal.current_pregnancy.date_of_last_normal_menses.day);
    var edd_date = new Date(edd_year, edd_month - 1, edd_day);
    var lmp_date = new Date(lmp_year, lmp_month - 1, lmp_day);

    let is_edd = false;
    let is_lmp = false;

    if 
    (
        //$global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(edd_year, edd_month, edd_day) == true
    )
    {
        is_edd = true;

    }
    else if 
    (
        //$global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(lmp_year, lmp_month, lmp_day) == true
    )
    {
        is_lmp = true;
    }


    let map = autorecalculate_get_event_date
    (
        "/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening", 
        is_edd,
        edd_date, 
        is_lmp, 
        lmp_date,
        p_form_index,
        p_grid_index
    );

    
    if 
    (
        is_valid_delivery_date && 
        g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].date_of_screening != null && 
        g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].date_of_screening != ''
    ) 
    {
        
        if 
        (

            is_valid_delivery_date &&
            map.get('event_date') > delivery_date
            
        ) 
        {
            days = $global.calc_days(start_date,  map.get('event_date'));
            g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].days_postpartum = days;
        } 
        else 
        {

            g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].days_postpartum = "";
        }

        $mmria.set_control_value("mental_health_profile/were_there_documented_mental_health_conditions/days_postpartum", days, p_form_index, p_grid_index);

        //map = autorecalculate_get_event_date("/mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening", is_edd, edd_date, is_lmp, lmp_date, p_form_index, p_grid_index)
        if 
        (
            map.get('ga').length > 1 &&
            (
                ! is_valid_delivery_date ||
                is_valid_delivery_date &&
                map.get('event_date') <= delivery_date
            )
        ) 
        {
            g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_weeks = map.get('ga')[0];
            g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_days = map.get('ga')[1];              
        }
        else
        {
            g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_weeks = "";
            g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_days = "";
        }

        $mmria.set_control_value
        (
            "mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks", 
            g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_weeks, 
            p_form_index, p_grid_index
        );

        $mmria.set_control_value
        (
            "mental_health_profile/were_there_documented_mental_health_conditions/gestational_days", 
            g_data.mental_health_profile.were_there_documented_mental_health_conditions[p_grid_index].gestational_days, 
            p_form_index, p_grid_index
        );
        
        
    }
}

//CALCULATE TIME BETWEEN ONSET OF LABOR AND ARRIVAL AT HOSPITAL
/*
path=er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/cmd_duration_of_labor_prior_to_arrival
event=onclick
*/

autocalc_map.safe_set("/er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/year", arc_duration_of_labor);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/month", arc_duration_of_labor);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/day", arc_duration_of_labor);
autocalc_map.safe_set("/er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/time_of_onset_of_labor", arc_duration_of_labor);

function arc_duration_of_labor(p_form_index)
{
    if(p_form_index == null) return;

    var hours = null;

    var onset_year = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.year);
    var onset_month = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.month);
    var onset_day = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.day);
	
	var onset_time = null;
    if (g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor instanceof Date) 
	{
		onset_time = g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor;
    }
	else
	{
        onset_time = new Date('January 1, 1900 ' + g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor);
    }
	
    var arrival_year = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.year);
    var arrival_month = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.month);
    var arrival_day = parseInt(g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.day);
	
	var arrival_time = null;
    if (g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival instanceof Date) 
	{
        arrival_time = g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival;
    } 
	else 
	{
        arrival_time = new Date('January 1, 1900 ' + g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival);
    }
    if 
    (
        $global.isValidDate(onset_year, onset_month, onset_day) == true && 
        $global.isValidDate(arrival_year, arrival_month, arrival_day) == true && 
        (
            g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor != '' || 
            g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor != null
        ) 
        && 
        (
            g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival != '' ||
            g_data.er_visit_and_hospital_medical_records[p_form_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival_time != null
        )
    ) 
    {
        var onset_date = new Date(onset_year, onset_month - 1, onset_day, onset_time.getHours(), onset_time.getMinutes());
        var arrival_date = new Date(arrival_year, arrival_month - 1, arrival_day, arrival_time.getHours(), arrival_time.getMinutes());
        var hours = Math.round((arrival_date - onset_date) / 3600000 * 100) / 100;
        if (hours > 1) 
        {
            g_data.er_visit_and_hospital_medical_records[p_form_index].onset_of_labor.date_of_onset_of_labor.duration_of_labor_prior_to_arrival = hours;
            $mmria.set_control_value("er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/duration_of_labor_prior_to_arrival", hours);
        }
    }
}



// OMB RACE RECODE FOR CASE ON DC FORM
/*
path=death_certificate/race/race
event=onclick
*/

autocalc_map.safe_set("/death_certificate/race/race", arc_omb_race_recode_dc);
function arc_omb_race_recode_dc() 
{
    var race_recode = null;
    var race = g_data.death_certificate.race.race;
    race_recode = $global.calculate_omb_recode(race);
    g_data.death_certificate.race.omb_race_recode = race_recode;
    $mmria.set_control_value('death_certificate/race/omb_race_recode', race_recode);

}


// OMB RACE RECODE FOR MOM ON BC FORM
/*
path=birth_fetal_death_certificate_parent/race/race_of_mother
event=onclick
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/race/race_of_mother", arc_omb_mrace_recode_bc);

function arc_omb_mrace_recode_bc() 
{
    var race_recode = null;
    var race = g_data.birth_fetal_death_certificate_parent.race.race_of_mother;
    race_recode = $global.calculate_omb_recode(race);
    g_data.birth_fetal_death_certificate_parent.race.omb_race_recode = race_recode;
    $mmria.set_control_value('birth_fetal_death_certificate_parent/race/omb_race_recode', race_recode);

}

// OMB RACE RECODE FOR DAD ON BC FORM
/*
path=birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father
event=onclick
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father", arc_omb_frace_recode_bc);

function arc_omb_frace_recode_bc() 
{
    var race_recode = null;
    var race = g_data.birth_fetal_death_certificate_parent.demographic_of_father.race.race_of_father;
    race_recode = $global.calculate_omb_recode(race);
    g_data.birth_fetal_death_certificate_parent.demographic_of_father.race.omb_race_recode = race_recode;
    $mmria.set_control_value('birth_fetal_death_certificate_parent/demographic_of_father/race/omb_race_recode', race_recode);

}



/*
path=medical_transport/destination_information/address/calculated_distance
event=onclick
*/

autocalc_map.safe_set("/medical_transport/origin_information/address/latitude", arc_medical_transport_destination_information_address_calculated_distance);
autocalc_map.safe_set("/medical_transport/origin_information/address/longitude", arc_medical_transport_destination_information_address_calculated_distance);


function arc_medical_transport_destination_information_address_calculated_distance() 
{
    let dist = null;

    let res_lat = parseFloat(g_data.medical_transport[p_form_index].origin_information.address.latitude);
    let res_lon = parseFloat(g_data.medical_transport[p_form_index].origin_information.address.longitude);
    let hos_lat = parseFloat(g_data.medical_transport[p_form_index].origin_information.address.latitude);
    let hos_lon = parseFloat(g_data.medical_transport[p_form_index].origin_information.address.longitude);
    if (res_lat >= -90 && res_lat <= 90 && res_lon >= -180 && res_lon <= 180 && hos_lat >= -90 && hos_lat <= 90 && hos_lon >= -180 && hos_lon <= 180) 
    {
        dist = $global.calc_distance(res_lat, res_lon, hos_lat, hos_lon);
        g_data.medical_transport[p_form_index].origin_information.address.estimated_distance = dist;
        $mmria.set_control_value('medical_transport/destination_information/address/estimated_distance', dist);

    }
}

//CALCULATE MOTHERS AGE AT DEATH ON DC
/*
path=death_certificate/demographics/age
event=onfocus
*/

autocalc_map.safe_set("/death_certificate/demographics/date_of_birth/year", arc_mothers_age_death);
autocalc_map.safe_set("/death_certificate/demographics/date_of_birth/month", arc_mothers_age_death);
autocalc_map.safe_set("/death_certificate/demographics/date_of_birth/day", arc_mothers_age_death);
autocalc_map.safe_set("/home_record/date_of_death/year", arc_mothers_age_death);
autocalc_map.safe_set("/home_record/date_of_death/month", arc_mothers_age_death);
autocalc_map.safe_set("/home_record/date_of_death/day", arc_mothers_age_death);

function arc_mothers_age_death() 
{
    var years = null;
    var start_year = parseInt(g_data.death_certificate.demographics.date_of_birth.year);
    var start_month = parseInt(g_data.death_certificate.demographics.date_of_birth.month);
    var start_day = parseInt(g_data.death_certificate.demographics.date_of_birth.day);
    var end_year = parseInt(g_data.home_record.date_of_death.year);
    var end_month = parseInt(g_data.home_record.date_of_death.month);
    var end_day = parseInt(g_data.home_record.date_of_death.day);
    if 
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true
    ) 
    {
        var start_date = new Date(start_year, start_month - 1, start_day);
        var end_date = new Date(end_year, end_month - 1, end_day);
        var years = $global.calc_years(start_date, end_date);
        g_data.death_certificate.demographics.age = years;
        $mmria.set_control_value("death_certificate/demographics/age", years);
    }
}
//CALCULATE MOTHERS AGE AT DELIVERY ON BC
/*
path=birth_fetal_death_certificate_parent/demographic_of_mother/age
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/year", arc_mothers_age_delivery);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/month", arc_mothers_age_delivery);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/day", arc_mothers_age_delivery);

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_mothers_age_delivery);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_mothers_age_delivery);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_mothers_age_delivery);

function arc_mothers_age_delivery() 
{
    var years = null;
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.demographic_of_mother.date_of_birth.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.demographic_of_mother.date_of_birth.month);
    var start_day = parseInt(g_data.birth_fetal_death_certificate_parent.demographic_of_mother.date_of_birth.day);
    var end_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var end_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var end_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);

    if 
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true
    ) 
    {
        var start_date = new Date(start_year, start_month - 1, start_day);
        var end_date = new Date(end_year, end_month - 1, end_day);
        var years = $global.calc_years(start_date, end_date);
        g_data.birth_fetal_death_certificate_parent.demographic_of_mother.age = years;
        $mmria.set_control_value("birth_fetal_death_certificate_parent/demographic_of_mother/age", years);
    }
}
//CALCULATE FATHERS AGE AT DELIVERY ON BC
/*
path=birth_fetal_death_certificate_parent/demographic_of_father/age
event=onfocus
*/

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/year", arc_fathers_age_delivery);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/month", arc_fathers_age_delivery);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_fathers_age_delivery);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_fathers_age_delivery);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_fathers_age_delivery);

function arc_fathers_age_delivery() 
{
    var years = null;
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.demographic_of_father.date_of_birth.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.demographic_of_father.date_of_birth.month);
    var start_day = 1;
    var end_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var end_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var end_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    if
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true
    )
    {
        var start_date = new Date(start_year, start_month - 1, start_day);
        var end_date = new Date(end_year, end_month - 1, end_day);
        var years = $global.calc_years(start_date, end_date);
        g_data.birth_fetal_death_certificate_parent.demographic_of_father.age = years;
        $mmria.set_control_value("birth_fetal_death_certificate_parent/demographic_of_father/age", years);
    }
}


autocalc_map.safe_set("/birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation", arc_birth_2_death);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation_days", arc_birth_2_death);

function arc_birth_2_death() 
{
    var days = null;
    var start_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var start_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var start_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var end_year = parseInt(g_data.home_record.date_of_death.year);
    var end_month = parseInt(g_data.home_record.date_of_death.month);
    var end_day = parseInt(g_data.home_record.date_of_death.day);
    if 
    (
        $global.isValidDate(start_year, start_month, start_day) == true && 
        $global.isValidDate(end_year, end_month, end_day) == true
    ) 
    {
        var start_date = new Date(start_year, start_month - 1, start_day);
        var end_date = new Date(end_year, end_month - 1, end_day);
        var days = $global.calc_days(start_date, end_date);
        g_data.birth_fetal_death_certificate_parent.length_between_child_birth_and_death_of_mother = days;

    }
    else
    {
        g_data.birth_fetal_death_certificate_parent.length_between_child_birth_and_death_of_mother = "";
    }

    $mmria.set_control_value("birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother", g_data.birth_fetal_death_certificate_parent.length_between_child_birth_and_death_of_mother);
}


autocalc_map.safe_set("/autopsy_report/biometrics/mother/height/feet", arc_autopsy_biometrics_mother_bmi);
autocalc_map.safe_set("/autopsy_report/biometrics/mother/height/inches", arc_autopsy_biometrics_mother_bmi);
autocalc_map.safe_set("/autopsy_report/biometrics/mother/weight", arc_autopsy_biometrics_mother_bmi);


function arc_autopsy_biometrics_mother_bmi(p_form_index) 
{
    var bmi = null;
    var height_feet = parseFloat(g_data.autopsy_report.biometrics.mother.height.feet);
    var height_inches = parseFloat(g_data.autopsy_report.biometrics.mother.height.inches);

    var weight = parseFloat(g_data.autopsy_report.biometrics.mother.weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) 
    {
        var bmi = $global.calc_bmi(height, weight);
        g_data.autopsy_report.biometrics.mother.bmi = bmi;
        $mmria.set_control_value("autopsy_report/biometrics/mother/bmi", bmi, p_form_index);
    }
}


autocalc_map.safe_set("/birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/month", arc_birth_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/day", arc_birth_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/day", arc_birth_ga);

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_birth_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_birth_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_birth_ga);

autocalc_map.safe_set("/birth_fetal_death_certificate_parent.prenatal_care/date_of_last_normal_menses/year", arc_birth_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent.prenatal_care/date_of_last_normal_menses/month", arc_birth_ga);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent.prenatal_care/date_of_last_normal_menses/day", arc_birth_ga);

function arc_birth_ga() 
{
    var ga_lmp = [];
    var weeks = null;
    var days = null;
    var event_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var event_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var event_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var lmp_year = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.year);
    var lmp_month = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.month);
    var lmp_day = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.day);
    var lmp_date = new Date(lmp_year, lmp_month - 1, lmp_day);
    var event_date = new Date(event_year, event_month - 1, event_day);
    if 
    (
        $global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(lmp_year, lmp_month, lmp_day) == true
    ) 
    {
        ga_lmp = $global.calc_ga_lmp(lmp_date, event_date);
        if (ga_lmp.length > 1) 
        {
            g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation = ga_lmp[0];
            g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation_days = ga_lmp[1];

            $mmria.set_control_value('birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation', ga_lmp[0]);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation_days', ga_lmp[1]);
        }
    }
}


autocalc_map.safe_set("/birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/month", arc_prenatal_care_dlnm_gestation);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/day", arc_prenatal_care_dlnm_gestation);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/year", arc_prenatal_care_dlnm_gestation);

autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", arc_prenatal_care_dlnm_gestation);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", arc_prenatal_care_dlnm_gestation);
autocalc_map.safe_set("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", arc_prenatal_care_dlnm_gestation);


function arc_prenatal_care_dlnm_gestation() 
{

    ///birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation
    ///birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation_days
    var ga_lmp = [];
    var weeks = null;
    var days = null;
    var event_year = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.year);
    var event_month = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.month);
    var event_day = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.day);
    var lmp_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var lmp_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var lmp_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    var lmp_date = new Date(lmp_year, lmp_month - 1, lmp_day);
    var event_date = new Date(event_year, event_month - 1, event_day);

    if 
    (
        $global.isValidDate(event_year, event_month, event_day) == true && 
        $global.isValidDate(lmp_year, lmp_month, lmp_day) == true
    ) 
    {
        ga_lmp = $global.calc_ga_lmp(event_date, lmp_date);
        if (ga_lmp.length > 1) 
        {
            g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation = ga_lmp[0];
            g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation_days = ga_lmp[1];
        }
        else
        {
            g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation = "";
            g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation_days = "";
        }

        $mmria.set_control_value('birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation', g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation);
        $mmria.set_control_value('birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation_days', g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation_days);
        
    }
    else
    {
        g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation = "";
        g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation_days = "";

        $mmria.set_control_value('birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation', g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation);
        $mmria.set_control_value('birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation_days', g_data.birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation_days);
    }
}


//mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks

//mental_health_profile/were_there_documented_mental_health_conditions/gestational_days

//mental_health_profile/were_there_documented_mental_health_conditions/days_postpartum


