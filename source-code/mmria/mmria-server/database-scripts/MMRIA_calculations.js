// FUNCTION TO RECODE RACE TO OMB STANDARD
function $calculate_omb_recode(p_value_list) 
{

/*
9999    (blank)
0       White
1       Black
2       American Indian/Alaska Native
3       Native Hawaiian
4       Guamanian or Chamorro
5       Samoan
6       Other Pacific Islander
7       Asian Indian
8       Chinese
9       Filipino
10      Japanese
11      Korean
12      Vietnamese
13      Other Asian
14      Other Race
8888    Race Not Specified

9999    (blank)
0       White
1       Black
2       American Indian/Alaska Native
3       Pacific Islander
4       Asian
5       Bi-Racial
6       Multi-Racial
14       Other Race
8888    Race Not Specified

*/



   // p_value_list is an array
    var result = null;
    var asian_list = [
            7, //'Asian Indian',
            8, //'Chinese',
            9, //'Filipino',
            10, //'Japanese',
            11, //'Korean',
            12, //'Vietnamese',
            13 //'Other Asian'
        ];
    var islander_list = [
            3, //'Native Hawaiian',
            4, //'Guamanian or Chamorro',
            5, //'Samoan',
            6 //'Other Pacific Islander'
        ];

/*
9999    (blank)
0       White
1       Black
2       American Indian/Alaska Native
3       Pacific Islander
4       Asian
5       Bi-Racial
6       Multi-Racial
14       Other Race
8888    Race Not Specified

*/        
    if (p_value_list.length == 0) 
    {
        result = 9999;
    } 
    else if (p_value_list.length == 1) 
    {
        if ($global.get_intersection(p_value_list, asian_list).length > 0) 
        {
            result = 4; //'Asian';
        } 
        else if ($global.get_intersection(p_value_list, islander_list).length > 0) 
        {
            result = 3; //'Pacific Islander';
        } 
        else 
        {
            result = p_value_list[0];
        }
    }
    else // more than 1 item has been selected.
    {
        if 
        (
            p_value_list.includes('Race Not Specified') || 
            p_value_list.includes(8888) ||
            p_value_list.includes("8888")
        ) 
        {
            result = 8888; //'Race Not Specified';
        } 
        else 
        {
            /* Description of recode process
        
        total unique = non list items + is_asian + is_islander
        
        non list items   is_asian   is_islander = total unique
        2 - 1 - 1 = 0      	   1          1              2
        2 - 1 - 0 = 1      	   1          0              2
        2 - 0 - 1 = 1      	   0          1              2
        2 - 0 - 0 = 2      	   0          0              2
        2 - 0 - 2 = 0      	   0          1              1
        2 - 2 - 0 = 0      	   1          0              1
        3 - 0 - 0 = 3      	   0          0              3
        3 - 1 - 1 = 1     	   1          1              3
        3 - 2 - 0 = 1      	   1          0              2
        */
            var asian_intersection_count = $global.get_intersection(p_value_list, asian_list).length;
            var is_asian = 0;
            var islander_intersection_count = $global.get_intersection(p_value_list, islander_list).length;
            var is_islander = 0;
            if (asian_intersection_count > 0)
            {
                is_asian = 1;
            }

            if (islander_intersection_count > 0)
            {
                is_islander = 1;
            }

            var number_not_in_asian_or_islander_categories = p_value_list.length - asian_intersection_count - islander_intersection_count;
            var total_unique_items = number_not_in_asian_or_islander_categories + is_asian + is_islander;
            switch (total_unique_items) 
            {
                case 1:
                    if (is_asian == 1) 
                    {
                        result = 4; //'Asian';
                    } 
                    else if (is_islander == 1) 
                    {
                        result = 3; //'Pacific Islander';
                    } 
                    else 
                    {
                        console.log('This should never happen bug');
                    }
                    break;
                case 2:
                    result = 5; //'Bi-Racial';
                    break;
                default:
                    result = 6; //'Multi-Racial';
                    break;
            }
        }
    }
    return result;
}
// CALCULATE INTERSECTION FOR OMB RACE RECODE
function $get_intersection(p_list_1, p_list_2) 
{
    var result = [];
    for(let i = 0; i < p_list_1.length; i++)
    {
        let item1 = p_list_1[i];

        if (typeof item1 === 'string' || item1 instanceof String)
        {
            item1 = parseInt(item1);
        }

        for(let j = 0; j < p_list_2.length; j++)
        {
            let item2 = p_list_2[j]
            if (typeof item1 === 'string' || item1 instanceof String)
            {
                item2 = parseInt(item2);
            }

            if(item1 == item2)
            {
                result.push(item1)
            }
        }
    }

    return result;
}


//CALCLATE NUMBER OF DAYS BETWEEN 2 DATES
function $calc_days
(
    start_date, 
    end_date
)
{  
    const MILLISECONDS_PER_DAY = 1000 * 60 * 60 * 24;  
    
    var utc1 = Date.UTC
    (
        start_date.getFullYear(), 
        start_date.getMonth(), 
        start_date.getDate()
    );
      
    var utc2 = Date.UTC
    (
        end_date.getFullYear(), 
        end_date.getMonth(), 
        end_date.getDate()
    ); 
    
     return Math.floor((utc2 - utc1) / MILLISECONDS_PER_DAY);
}


//CALCLATE NUMBER OF DAYS BETWEEN 2 DATES
/*
function $calc_days(p_start_date, p_end_date) 
{
    var days = null;
    p_start_date = p_start_date.getTime() / 86400000;
    p_end_date = p_end_date.getTime() / 86400000;
    days = Math.trunc(p_end_date - p_start_date);
    return days;
}*/
//CALCLATE NUMBER OF WEEKS BETWEEN 2 DATES
function $calc_weeks(p_start_date, p_end_date) 
{
    var weeks = null;
    p_start_date = p_start_date.getTime() / 604800000;
    p_end_date = p_end_date.getTime() / 604800000;
    weeks = Math.trunc(p_end_date - p_start_date);
    return weeks;
}
//CALCLATE NUMBER OF YEARS BETWEEN 2 DATES
function $calc_years(p_start_date, p_end_date) 
{
    var years = null;
    p_start_date = p_start_date.getTime() / 31557600000;
    p_end_date = p_end_date.getTime() / 31557600000;
    years = Math.trunc(p_end_date - p_start_date);
    return years;
}
//CALCLATE BMI FROM HEIGHT (IN INCHES)AND WEIGHT (IN POUNDS)
function $calc_bmi(p_height, p_weight) 
{
    var bmi = null;
    var height = parseInt(p_height);
    var weight = parseInt(p_weight);
    height /= 39.3700787;
    weight /= 2.20462;
    bmi = Math.round(weight / Math.pow(height, 2) * 10) / 10;
    return bmi;
}
// CALCULATE DISTANCE BETWEEN 2 COORDINATE PAIRS - HAVERSINE FORMULA (GREAT CIRCLE))
// UNIT DEFAULT = MILES
// LAT-LON ARE IN DECIMAL DEGREES
function $calc_distance(lat1, lon1, lat2, lon2) 
{
    const radlat1 = Math.PI * lat1 / 180;
    const radlat2 = Math.PI * lat2 / 180;
    const theta = lon1 - lon2;
    const radtheta = Math.PI * theta / 180;
    let dist = Math.sin(radlat1) * Math.sin(radlat2) + Math.cos(radlat1) * Math.cos(radlat2) * Math.cos(radtheta);

    if(dist > 1)
    {
        dist = 1;
    }
    else if(dist < -1)
    {
        dist = -1;
    }

    dist = Math.acos(dist);
    dist = dist * 180 / Math.PI;
    dist = Math.round(dist * 60 * 1.1515 * 100) / 100;
    return dist;
}
//VALIDATE A DATE WHEN USING YEAR-MONTH-DAY SEPARATE DATEPART INPUT
function $isValidDate(p_year, p_month, p_day) 
{
    var year = parseInt(p_year);
    var month = parseInt(p_month);
    var day = parseInt(p_day);
    if (isNaN(year) || isNaN(month) || isNaN(day))
        return false;
    var months31 = [
            1,
            3,
            5,
            7,
            8,
            10,
            12
        ];
    // months with 31 days
    var months30 = [
            4,
            6,
            9,
            11
        ];
    // months with 30 days
    var months28 = [2];
    // the only month with 28 days (29 if year isLeap)
    var isLeap = year % 4 === 0 && year % 100 !== 0 || year % 400 === 0;
    var valid = months31.indexOf(month) !== -1 && day <= 31 || months30.indexOf(month) !== -1 && day <= 30 || months28.indexOf(month) !== -1 && day <= 28 || months28.indexOf(month) !== -1 && day <= 29 && isLeap;
    
    if(year < 1900 || year > 2100)
        valid = false;
        
    return valid;
}
//CALCLATE GESTATIONAL AGE WITH LMP 
function $calc_ga_lmp(p_start_date, p_end_date) 
{
    result = [];
    var weeks = null;
    var days = null;
    weeks = Math.trunc($global.calc_days(p_start_date, p_end_date) / 7);
    days = $global.calc_days(p_start_date, p_end_date) % 7;
    result.push(weeks);
    result.push(days);
    return result;
}
//CALCLATE GESTATIONAL AGE WITH EDD 
function $calc_ga_edd(p_start_date, p_end_date) 
{
    result = [];
    var weeks = null;
    var days = null;
    if (p_start_date <= p_end_date) {
        if ($global.calc_days(p_start_date, p_end_date) % 7 == 0) {
            var weeks = 40 - Math.trunc($global.calc_days(p_start_date, p_end_date) / 7);
            var days = $global.calc_days(p_start_date, p_end_date) % 7;
        } else {
            var weeks = 39 - Math.trunc($global.calc_days(p_start_date, p_end_date) / 7);
            var days = 7 - $global.calc_days(p_start_date, p_end_date) % 7;
        }
    } else if (p_start_date > p_end_date) {
        var weeks = 40 - Math.trunc($global.calc_days(p_start_date, p_end_date) / 7);
        var days = Math.trunc($global.calc_days(p_start_date, p_end_date) % 7) * -1 + 1;
    }
    result.push(weeks);
    result.push(days);
    return result;
}
//INDEX FUNCTION FOR MULTI-PAGE FORMS
function $get_current_multiform_index() 
{
    var result = parseInt(window.location.href.substr(window.location.href.lastIndexOf('/') + 1, window.location.href.length - (window.location.href.lastIndexOf('/') + 1)));
    return result;
}
//----------------- Begin events ---------------------------------------------------


//CODE EXECUTED WHEN DOCUMENT EDIT IS SELECTED
function $case_document_begin_edit ()
 {
    let controlId = 'g_data_home_record_overall_assessment_of_timing_of_death_number_of_days_after_end_of_pregnancey_control';
    let value = parseInt($('#g_data_home_record_overall_assessment_of_timing_of_death_abstrator_assigned_status_control').find(':selected').val());
    if (value === 9999
        || value === 0
        || value === 1
        || value === 4
        || value === 88
    ) 
    {
        $('label[for=' + controlId + '], #' + controlId).hide();
    }
    else 
    {
        $('label[for=' + controlId + '], #' + controlId).show();
    }
}


/*
path=committee_review/date_of_review
event=onblur
*/
function committee_review_date_of_review_changed(p_control) 
{
    g_data.cmpquarter = $mmria.get_year_and_quarter(p_control.value);
}




/*
path=death_certificate/place_of_last_residence/view_community_vital_signs_button
event=onclick
*/
function dc_plc_cvs_button_click(p_control) 
{
    $mmria.dc_plc_cvs_button_click(p_control);
}




/*
path=cvs/view_community_vital_signs_button
event=onclick
*/
function cvs_view_community_vital_signs_button_click(p_control) 
{
    $mmria.cvs_view_community_vital_signs_button_click(p_control);
}



/*
path=home_record/case_status/overall_case_status
event=onchange
*/
function case_status_value_change(p_control) 
{
//home_record/case_status/case_status_info
//4	Review Complete and Decision Entered
/*
the Form Status of All MMRIA Forms must be set to either “Complete”, “Not Available“ or “Not Applicable”. 
2)	User must click the OK button in the 

9999	(blank)	
0	Not Started	
1	In Progress	
2	Completed	
3	Not Available	
4	Not Applicable

home_record/case_progress_report/death_certificate
home_record/case_progress_report/autopsy_report
home_record/case_progress_report/birth_certificate_parent_section
home_record/case_progress_report/birth_certificate_infant_or_fetal_death_section
home_record/case_progress_report/prenatal_care_record
home_record/case_progress_report/er_visits_and_hospitalizations
home_record/case_progress_report/other_medical_visits
home_record/case_progress_report/medical_transport
home_record/case_progress_report/social_and_psychological_profile
home_record/case_progress_report/mental_health_profile
home_record/case_progress_report/informant_interviews	
home_record/case_progress_report/case_narrative
home_record/case_progress_report/committe_review_worksheet
g_data


/home_record/case_progress_report/community_vital_signs
Cannont be 9999, 0 or 1

*/

    let selected_value = new Number(p_control.value);
    g_previous_case_status = this.overall_case_status;
    this.overall_case_status = selected_value;

    let is_valid_status = false;
    if(selected_value == 4)
    {
        let is_correct_staus = {
            "2":true,
            "3":true,
            "4":true,
        }
        let is_invalid_status = {
            "9999": true
        }

        if
        (
            is_correct_staus[g_data.home_record.case_progress_report.death_certificate] &&
            is_correct_staus[g_data.home_record.case_progress_report.autopsy_report] &&
            is_correct_staus[g_data.home_record.case_progress_report.birth_certificate_parent_section] &&
            is_correct_staus[g_data.home_record.case_progress_report.birth_certificate_infant_or_fetal_death_section] &&
            is_correct_staus[g_data.home_record.case_progress_report.prenatal_care_record] &&
            is_correct_staus[g_data.home_record.case_progress_report.er_visits_and_hospitalizations] &&
            is_correct_staus[g_data.home_record.case_progress_report.other_medical_visits] &&
            is_correct_staus[g_data.home_record.case_progress_report.medical_transport] &&
            is_correct_staus[g_data.home_record.case_progress_report.social_and_psychological_profile] &&
            is_correct_staus[g_data.home_record.case_progress_report.mental_health_profile] &&
            is_correct_staus[g_data.home_record.case_progress_report.informant_interviews	] &&
            is_correct_staus[g_data.home_record.case_progress_report.case_narrative] &&
            is_correct_staus[g_data.home_record.case_progress_report.committe_review_worksheet] &&
            is_correct_staus[g_data.home_record.case_progress_report.community_vital_signs]

        )
        {
            is_valid_status = true;
        }
       
        if(is_valid_status)
        {
            //console.log(is_valid_status);
            

            if (is_invalid_status[g_data.home_record.overall_assessment_of_timing_of_death.abstrator_assigned_status])
            {
                is_valid_status = false;

                g_data.home_record.case_status.overall_case_status = g_previous_case_status;
                $mmria.set_control_value('home_record/case_status/overall_case_status', g_previous_case_status);

                $mmria.info_dialog_show("Invalid Status Selection", "Case Progress", '<p>Before a Case can be marked as "Review Complete and Decisions Entered":</p> <p style="padding-left: 10px;">You must specify the “Abstractor-assigned pregnancy status based on overall review of records”.</p>');
            }
        }
        else
        {
            if (is_invalid_status[g_data.home_record.overall_assessment_of_timing_of_death.abstrator_assigned_status])
            {

                g_data.home_record.case_status.overall_case_status = g_previous_case_status;
                $mmria.set_control_value('home_record/case_status/overall_case_status', g_previous_case_status);

                $mmria.info_dialog_show("Invalid Status Selection", "Case Progress", '<p>Before a Case can be marked as "Review Complete and Decisions Entered":</p> <p style="padding-left: 10px;">1. You must specify the “Abstractor-assigned pregnancy status based on overall review of records”. </p> <p style="padding-left: 10px;">2. The Form Status of All MMRIA Forms must be set to either “Complete”, “Not Available”, or “Not Applicable”.</p> ');
            }
            else
            {
                g_data.home_record.case_status.overall_case_status = g_previous_case_status;
                $mmria.set_control_value('home_record/case_status/overall_case_status', g_previous_case_status);

                $mmria.info_dialog_show("Invalid Status Selection", "Case Progress", '<p>Before a Case can be marked as "Review Complete and Decisions Entered":</p> <p style="padding-left: 10px;">The Form Status of All MMRIA Forms must be set to either “Complete”, “Not Available”, or “Not Applicable”.</p>');
            }

        }
    }
    
    if 
    (
        (
            this.case_locked_date != null ||
            this.case_locked_date != ""
        ) 
        &&
        (
            (selected_value == 4 && is_valid_status) ||
            selected_value == 5 ||
            selected_value == 6
        )
    ) 
    {

        g_is_confirm_for_case_lock = true;
        g_target_case_status = selected_value;
        $mmria.show_confirmation_dialog($global.case_status_confirm, $global.case_status_cancel);
    }
    else if
    (
        (
            this.case_locked_date != null ||
            this.case_locked_date != ""
        )
        &&
        ! (
            selected_value == 4 ||
            selected_value == 5 ||
            selected_value == 6
        )
    )
    {
        this.case_locked_date = null;
        $mmria.set_control_value('home_record/case_status/case_locked_date', this.case_locked_date);
    }
}

function $case_status_confirm()
{
    g_target_case_status = null;
    g_is_confirm_for_case_lock = false;

    g_data.home_record.case_status.case_locked_date = new Date().toISOString().split("T")[0];
    //g_data.date_last_checked_out = null;
    //g_data.last_checked_out_by = null;
    //g_data_is_checked_out = false;
    save_and_finish_click();
    //$mmria.save_current_record();

    g_render();
  
}

function $case_status_cancel()
{
    g_data.home_record.case_status.overall_case_status = g_previous_case_status;
    $mmria.set_control_value('home_record/case_status/overall_case_status', g_previous_case_status);
    g_is_confirm_for_case_lock = false;
    g_target_case_status = null;
    g_previous_case_status = null;
}

/*
path=home_record/overall_assessment_of_timing_of_death/abstrator_assigned_status
event=onchange
*/
function abstrator_assigned_status_value_change(p_control)
{
    let controlId = 'g_data_home_record_overall_assessment_of_timing_of_death_number_of_days_after_end_of_pregnancey_control';
    let value = parseInt($('#g_data_home_record_overall_assessment_of_timing_of_death_abstrator_assigned_status_control').find(':selected').val());
    if (value === 9999
        || value === 0
        || value === 1
        || value === 4
        || value === 88
    ) 
    {
        document.getElementById(controlId).value = '';
        $('label[for=' + controlId + '], #' + controlId).hide();
        g_data.home_record.overall_assessment_of_timing_of_death.number_of_days_after_end_of_pregnancey = null;
    }
    else {
        $('label[for=' + controlId + '], #' + controlId).show();
    }
}


//CALCULATE MOTHERS AGE AT DEATH ON DC
/*
path=death_certificate/demographics/age
event=onfocus
*/
function mothers_age_death(p_control) 
{
    var years = null;
    var start_year = parseInt(this.date_of_birth.year);
    var start_month = parseInt(this.date_of_birth.month);
    var start_day = parseInt(this.date_of_birth.day);
    var end_year = parseInt(g_data.home_record.date_of_death.year);
    var end_month = parseInt(g_data.home_record.date_of_death.month);
    var end_day = parseInt(g_data.home_record.date_of_death.day);
    if ($global.isValidDate(start_year, start_month, start_day) == true && $global.isValidDate(end_year, end_month, end_day) == true) {
        var start_date = new Date(start_year, start_month - 1, start_day);
        var end_date = new Date(end_year, end_month - 1, end_day);
        var years = $global.calc_years(start_date, end_date);
        this.age = years;
        p_control.value = this.age;
    }
}
//CALCULATE MOTHERS AGE AT DELIVERY ON BC
/*
path=birth_fetal_death_certificate_parent/demographic_of_mother/age
event=onfocus
*/
function mothers_age_delivery(p_control) {
    var years = null;
    var start_year = parseInt(this.date_of_birth.year);
    var start_month = parseInt(this.date_of_birth.month);
    var start_day = parseInt(this.date_of_birth.day);
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
        this.age = years;
        p_control.value = this.age;
    }
}
//CALCULATE FATHERS AGE AT DELIVERY ON BC
/*
path=birth_fetal_death_certificate_parent/demographic_of_father/age
event=onfocus
*/
function fathers_age_delivery(p_control) 
{
    var years = null;
    var start_year = parseInt(this.date_of_birth.year);
    var start_month = parseInt(this.date_of_birth.month);
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
        this.age = years;
        p_control.value = this.age;
    }
}
//CALCULATE PRE-PREGNANCY BMI ON BC
/*
path=birth_fetal_death_certificate_parent/maternal_biometrics/bmi
event=onfocus
*/
function prepregnancy_bmi(p_control) {
    var bmi = null;
    var height_feet = parseFloat(this.height_feet);
    var height_inches = parseFloat(this.height_inches);
    var weight = parseFloat(this.pre_pregnancy_weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) {
        var bmi = $global.calc_bmi(height, weight);
        this.bmi = bmi;
        p_control.value = this.bmi;
    }
}
//CALCULATE BMI FOR AUTOPSY
/*
path=autopsy_report/biometrics/mother/bmi
event=onfocus
*/
function autopsy_bmi(p_control) {
    var bmi = null;
    var height_feet = parseFloat(this.height.feet);
    var height_inches = parseFloat(this.height.inches);
    var weight = parseFloat(this.weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) {
        var bmi = $global.calc_bmi(height, weight);
        this.bmi = bmi;
        p_control.value = this.bmi;
    }
}
//CALCULATE BMI FOR PRENATAL CARE RECORD
/*
path=prenatal/current_pregnancy/bmi
event=onfocus
*/
function prenatal_bmi(p_control) {
    var bmi = null;
    var height_feet = parseFloat(this.height.feet);
    var height_inches = parseFloat(this.height.inches);
    var weight = parseFloat(this.pre_pregnancy_weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) {
        var bmi = $global.calc_bmi(height, weight);
        this.bmi = bmi;
        p_control.value = this.bmi;
    }
}
//CALCULATE BMI FOR ER VISIT OR HOSPITALIZATION MEDICAL RECORD
/*
path=er_visit_and_hospital_medical_records/maternal_biometrics/height/bmi
event=onfocus
*/
function er_hosp_bmi(p_control) {
    var bmi = null;
    var height_feet = parseFloat(this.feet);
    var height_inches = parseFloat(this.inches);
    var current_er_index = $global.get_current_multiform_index();
    var weight = parseFloat(g_data.er_visit_and_hospital_medical_records[current_er_index].maternal_biometrics.admission_weight);
    var height = height_feet * 12 + height_inches;
    if (height > 24 && height < 108 && weight > 50 && weight < 800) {
        var bmi = $global.calc_bmi(height, weight);
        this.bmi = bmi;
        p_control.value = this.bmi;
    }
}
//CALCULATE WEIGHT GAIN DURING PREGNANCY ON BC
/*
path=birth_fetal_death_certificate_parent/maternal_biometrics/weight_gain
event=onfocus
*/
function weight_gain(p_control) {
    var weight_del = parseFloat(this.weight_at_delivery);
    var weight_pp = parseFloat(this.pre_pregnancy_weight);
    if (weight_del > 50 && weight_del < 800 && weight_pp > 50 && weight_pp < 800) {
        var gain = weight_del - weight_pp;
        this.weight_gain = gain;
        p_control.value = this.weight_gain;
    }
}
//CALCULATE WEIGHT GAIN DURING PREGNANCY ) ON PC
/*
path=prenatal/current_pregnancy/weight_gain
event=onfocus
*/
function weight_gain(p_control) {
    var weight_del = parseFloat(this.weight_at_last_visit);
    var weight_pp = parseFloat(this.pre_pregnancy_weight);
    if (weight_del > 50 && weight_del < 800 && weight_pp > 50 && weight_pp < 800) {
        var gain = weight_del - weight_pp;
        this.weight_gain = gain;
        p_control.value = this.weight_gain;
    }
}
//CALCULATE INTER-BIRTH INTERVAL IN MONTHS ON BC
/*
path=birth_fetal_death_certificate_parent/pregnancy_history/live_birth_interval
event=onfocus
*/
function birth_interval(p_control) {
    var interval = null;
    var start_year = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.year);
    var start_month = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.month);
    var start_day = parseFloat(g_data.birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.day);
    var event_year = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
    var event_month = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
    var event_day = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
    if ($global.isValidDate(start_year, start_month, start_day) == true && $global.isValidDate(event_year, event_month, event_day) == true) {
        var start_date = new Date(start_year, start_month - 1, start_day);
        var event_date = new Date(event_year, event_month - 1, event_day);
        interval = Math.trunc($global.calc_days(start_date, event_date) / 30.4375);
        this.live_birth_interval = interval;
        p_control.value = this.live_birth_interval;
    }
}
//CALCULATE INTER-PREGNANCY INTERVAL IN MONTHS ON BC
/*
path=birth_fetal_death_certificate_parent/pregnancy_history/pregnancy_interval
event=onfocus
*/
function pregnancy_interval(p_control) {
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
    if ($global.isValidDate(fd_year, fd_month, fd_day) == true && $global.isValidDate(event_year, event_month, event_day) == true) {
        var fd_date = new Date(fd_year, fd_month - 1, fd_day);
        var event_date = new Date(event_year, event_month - 1, event_day);
        interval = Math.trunc($global.calc_days(fd_date, event_date) / 30.4375);
        this.pregnancy_interval = interval;
        p_control.value = this.pregnancy_interval;
    } else if ($global.isValidDate(lb_year, lb_month, lb_day) == true && $global.isValidDate(event_year, event_month, event_day) == true) {
        var lb_date = new Date(lb_year, lb_month - 1, lb_day);
        var event_date = new Date(event_year, event_month - 1, event_day);
        interval = Math.trunc($global.calc_days(lb_date, end_date) / 30.4375);
        this.pregnancy_interval = interval;
        p_control.value = this.pregnancy_interval;
    }
}
// CALCULATE DISTANCE FROM PLACE OF LAST RESIDENCE TO PLACE OF DEATH 
/*
path=death_certificate/address_of_death/cmd_calculate_distance
event=onclick
*/
function death_distance(p_control) 
{
    var dist = null;
    var res_lat = parseFloat(g_data.death_certificate.place_of_last_residence.latitude);
    var res_lon = parseFloat(g_data.death_certificate.place_of_last_residence.longitude);
    var hos_lat = parseFloat(this.latitude);
    var hos_lon = parseFloat(this.longitude);
    if (res_lat >= -90 && res_lat <= 90 && res_lon >= -180 && res_lon <= 180 && hos_lat >= -90 && hos_lat <= 90 && hos_lon >= -180 && hos_lon <= 180) {
        dist = $global.calc_distance(res_lat, res_lon, hos_lat, hos_lon);
        this.estimated_death_distance_from_residence = dist;
        $mmria.save_current_record();
        $mmria.set_control_value('death_certificate/address_of_death/estimated_death_distance_from_residence', this.estimated_death_distance_from_residence);
    }


}
// CALCULATE DISTANCE FROM PLACE OF RESIDENCE TO HOSPITAL OF DELIVERY OF INFANT
/*
path=birth_fetal_death_certificate_parent/location_of_residence/cmd_calc_distance
event=onclick
*/
function birth_distance(p_control) {
    var dist = null;
    var res_lat = parseFloat(this.latitude);
    var res_lon = parseFloat(this.longitude);
    var hos_lat = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.latitude);
    var hos_lon = parseFloat(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.longitude);
    if (res_lat >= -90 && res_lat <= 90 && res_lon >= -180 && res_lon <= 180 && hos_lat >= -90 && hos_lat <= 90 && hos_lon >= -180 && hos_lon <= 180) {
        dist = $global.calc_distance(res_lat, res_lon, hos_lat, hos_lon);
        this.estimated_distance_from_residence = dist;
        $mmria.save_current_record();
        $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/estimated_distance_from_residence', this.estimated_distance_from_residence);
    }
}

//GEOCODE PLACE OF LAST RESIDENCE ON DC FORM
/*
path=death_certificate/place_of_last_residence/get_coordinates
event=onclick
*/
async function geocode_dc_last_res(p_control) 
{
    var street = this.street;
    var city = this.city;
    var state = this.state;
    var zip = this.zip_code;
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        var urban_status = null;
        var state_county_fips = null;
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.death_certificate.place_of_last_residence.latitude = geo_data.latitude;
            g_data.death_certificate.place_of_last_residence.longitude = geo_data.longitude;
            g_data.death_certificate.place_of_last_residence.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.death_certificate.place_of_last_residence.census_state_fips = geo_data.CensusStateFips;
            g_data.death_certificate.place_of_last_residence.census_county_fips = geo_data.CensusCountyFips;
            g_data.death_certificate.place_of_last_residence.census_tract_fips = geo_data.CensusTract;
            g_data.death_certificate.place_of_last_residence.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.death_certificate.place_of_last_residence.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.death_certificate.place_of_last_residence.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.death_certificate.place_of_last_residence.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.death_certificate.place_of_last_residence.state_county_fips = state_county_fips;


            const t_geoid = state_county_fips + g_data.death_certificate.place_of_last_residence.census_tract_fips.replace(".","").padStart(6, "0");
            $mmria.get_cvs_api_data_info
            (
                g_data.death_certificate.place_of_last_residence.state_county_fips,  //c_geoid, // = "13089",
                t_geoid, // = "13089021204",
                g_data.home_record.date_of_death.year, //year = "2012"
                $mmria.callback_cvs_data_success,
                $mmria.callback_cvs_data_error
            );



            $mmria.save_current_record();
            $mmria.set_control_value('death_certificate/place_of_last_residence/latitude', g_data.death_certificate.place_of_last_residence.latitude);
            $mmria.set_control_value('death_certificate/place_of_last_residence/longitude', g_data.death_certificate.place_of_last_residence.longitude);
            $mmria.set_control_value('death_certificate/place_of_last_residence/feature_matching_geography_type', g_data.death_certificate.place_of_last_residence.feature_matching_geography_type);
            $mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_code', g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_type', g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_census_tract_certainty_code', g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_census_tract_certainty_type', g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('death_certificate/place_of_last_residence/census_state_fips', g_data.death_certificate.place_of_last_residence.census_state_fips);
            $mmria.set_control_value('death_certificate/place_of_last_residence/census_county_fips', g_data.death_certificate.place_of_last_residence.census_county_fips);
            $mmria.set_control_value('death_certificate/place_of_last_residence/census_tract_fips', g_data.death_certificate.place_of_last_residence.census_tract_fips);
            $mmria.set_control_value('death_certificate/place_of_last_residence/census_cbsa_fips', g_data.death_certificate.place_of_last_residence.census_cbsa_fips);
            $mmria.set_control_value('death_certificate/place_of_last_residence/census_cbsa_micro', g_data.death_certificate.place_of_last_residence.census_cbsa_micro);
            $mmria.set_control_value('death_certificate/place_of_last_residence/census_met_div_fips', g_data.death_certificate.place_of_last_residence.census_met_div_fips);
            $mmria.set_control_value('death_certificate/place_of_last_residence/urban_status', g_data.death_certificate.place_of_last_residence.urban_status);
            $mmria.set_control_value('death_certificate/place_of_last_residence/state_county_fips', g_data.death_certificate.place_of_last_residence.state_county_fips);
		}
		else
		{
			g_data.death_certificate.place_of_last_residence.feature_matching_geography_type = 'Unmatchable';
			g_data.death_certificate.place_of_last_residence.latitude = '';
			g_data.death_certificate.place_of_last_residence.longitude = '';
			g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_code = '';
			g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_type = '';
			g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_code = '';
			g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_type = '';
			g_data.death_certificate.place_of_last_residence.census_state_fips = '';
			g_data.death_certificate.place_of_last_residence.census_county_fips = '';
			g_data.death_certificate.place_of_last_residence.census_tract_fips = '';
			g_data.death_certificate.place_of_last_residence.census_cbsa_fips = '';
			g_data.death_certificate.place_of_last_residence.census_cbsa_micro = '';
			g_data.death_certificate.place_of_last_residence.census_met_div_fips = '';
			g_data.death_certificate.place_of_last_residence.urban_status = '';
			g_data.death_certificate.place_of_last_residence.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('death_certificate/place_of_last_residence/feature_matching_geography_type', g_data.death_certificate.place_of_last_residence.feature_matching_geography_type);
			$mmria.set_control_value('death_certificate/place_of_last_residence/latitude', g_data.death_certificate.place_of_last_residence.latitude);
			$mmria.set_control_value('death_certificate/place_of_last_residence/longitude', g_data.death_certificate.place_of_last_residence.longitude);
			$mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_code', g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_type', g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_census_tract_certainty_code', g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_census_tract_certainty_type', g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('death_certificate/place_of_last_residence/census_state_fips', g_data.death_certificate.place_of_last_residence.census_state_fips);
			$mmria.set_control_value('death_certificate/place_of_last_residence/census_county_fips', g_data.death_certificate.place_of_last_residence.census_county_fips);
			$mmria.set_control_value('death_certificate/place_of_last_residence/census_tract_fips', g_data.death_certificate.place_of_last_residence.census_tract_fips);
			$mmria.set_control_value('death_certificate/place_of_last_residence/census_cbsa_fips', g_data.death_certificate.place_of_last_residence.census_cbsa_fips);
			$mmria.set_control_value('death_certificate/place_of_last_residence/census_cbsa_micro', g_data.death_certificate.place_of_last_residence.census_cbsa_micro);
			$mmria.set_control_value('death_certificate/place_of_last_residence/census_met_div_fips', g_data.death_certificate.place_of_last_residence.census_met_div_fips);
			$mmria.set_control_value('death_certificate/place_of_last_residence/urban_status', g_data.death_certificate.place_of_last_residence.urban_status);
			$mmria.set_control_value('death_certificate/place_of_last_residence/state_county_fips', g_data.death_certificate.place_of_last_residence.state_county_fips);

		}
    });
}
//GEOCODE PLACE OF INJURY ON DC FORM
/*
path=death_certificate/address_of_injury/cmd_get_coordinates
event=onclick
*/
function geocode_dc_injury_place(p_control) 
{
    var street = this.street;
    var city = this.city;
    var state = this.state;
    var zip = this.zip_code;
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        var urban_status = null;
        var state_county_fips = null;
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.death_certificate.address_of_injury.latitude = geo_data.latitude;
            g_data.death_certificate.address_of_injury.longitude = geo_data.longitude;
            g_data.death_certificate.address_of_injury.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.death_certificate.address_of_injury.census_state_fips = geo_data.CensusStateFips;
            g_data.death_certificate.address_of_injury.census_county_fips = geo_data.CensusCountyFips;
            g_data.death_certificate.address_of_injury.census_tract_fips = geo_data.CensusTract;
            g_data.death_certificate.address_of_injury.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.death_certificate.address_of_injury.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.death_certificate.address_of_injury.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.death_certificate.address_of_injury.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.death_certificate.address_of_injury.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('death_certificate/address_of_injury/latitude', g_data.death_certificate.address_of_injury.latitude);
            $mmria.set_control_value('death_certificate/address_of_injury/longitude', g_data.death_certificate.address_of_injury.longitude);
            $mmria.set_control_value('death_certificate/address_of_injury/feature_matching_geography_type', g_data.death_certificate.address_of_injury.feature_matching_geography_type);
            $mmria.set_control_value('death_certificate/address_of_injury/naaccr_gis_coordinate_quality_code', g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('death_certificate/address_of_injury/naaccr_gis_coordinate_quality_type', g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('death_certificate/address_of_injury/naaccr_census_tract_certainty_code', g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('death_certificate/address_of_injury/naaccr_census_tract_certainty_type', g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('death_certificate/address_of_injury/census_state_fips', g_data.death_certificate.address_of_injury.census_state_fips);
            $mmria.set_control_value('death_certificate/address_of_injury/census_county_fips', g_data.death_certificate.address_of_injury.census_county_fips);
            $mmria.set_control_value('death_certificate/address_of_injury/census_tract_fips', g_data.death_certificate.address_of_injury.census_tract_fips);
            $mmria.set_control_value('death_certificate/address_of_injury/census_cbsa_fips', g_data.death_certificate.address_of_injury.census_cbsa_fips);
            $mmria.set_control_value('death_certificate/address_of_injury/census_cbsa_micro', g_data.death_certificate.address_of_injury.census_cbsa_micro);
            $mmria.set_control_value('death_certificate/address_of_injury/census_met_div_fips', g_data.death_certificate.address_of_injury.census_met_div_fips);
            $mmria.set_control_value('death_certificate/address_of_injury/urban_status', g_data.death_certificate.address_of_injury.urban_status);
            $mmria.set_control_value('death_certificate/address_of_injury/state_county_fips', g_data.death_certificate.address_of_injury.state_county_fips);
		}
		else
		{
			g_data.death_certificate.address_of_injury.feature_matching_geography_type = 'Unmatchable';
			g_data.death_certificate.address_of_injury.latitude = '';
			g_data.death_certificate.address_of_injury.longitude = '';
			g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_code = '';
			g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_type = '';
			g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_code = '';
			g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_type = '';
			g_data.death_certificate.address_of_injury.census_state_fips = '';
			g_data.death_certificate.address_of_injury.census_county_fips = '';
			g_data.death_certificate.address_of_injury.census_tract_fips = '';
			g_data.death_certificate.address_of_injury.census_cbsa_fips = '';
			g_data.death_certificate.address_of_injury.census_cbsa_micro = '';
			g_data.death_certificate.address_of_injury.census_met_div_fips = '';
			g_data.death_certificate.address_of_injury.urban_status = '';
			g_data.death_certificate.address_of_injury.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('death_certificate/address_of_injury/feature_matching_geography_type', g_data.death_certificate.address_of_injury.feature_matching_geography_type);
			$mmria.set_control_value('death_certificate/address_of_injury/latitude', g_data.death_certificate.address_of_injury.latitude);
			$mmria.set_control_value('death_certificate/address_of_injury/longitude', g_data.death_certificate.address_of_injury.longitude);
			$mmria.set_control_value('death_certificate/address_of_injury/naaccr_gis_coordinate_quality_code', g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('death_certificate/address_of_injury/naaccr_gis_coordinate_quality_type', g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('death_certificate/address_of_injury/naaccr_census_tract_certainty_code', g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('death_certificate/address_of_injury/naaccr_census_tract_certainty_type', g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('death_certificate/address_of_injury/census_state_fips', g_data.death_certificate.address_of_injury.census_state_fips);
			$mmria.set_control_value('death_certificate/address_of_injury/census_county_fips', g_data.death_certificate.address_of_injury.census_county_fips);
			$mmria.set_control_value('death_certificate/address_of_injury/census_tract_fips', g_data.death_certificate.address_of_injury.census_tract_fips);
			$mmria.set_control_value('death_certificate/address_of_injury/census_cbsa_fips', g_data.death_certificate.address_of_injury.census_cbsa_fips);
			$mmria.set_control_value('death_certificate/address_of_injury/census_cbsa_micro', g_data.death_certificate.address_of_injury.census_cbsa_micro);
			$mmria.set_control_value('death_certificate/address_of_injury/census_met_div_fips', g_data.death_certificate.address_of_injury.census_met_div_fips);
			$mmria.set_control_value('death_certificate/address_of_injury/urban_status', g_data.death_certificate.address_of_injury.urban_status);
			$mmria.set_control_value('death_certificate/address_of_injury/state_county_fips', g_data.death_certificate.address_of_injury.state_county_fips);

		}
    });
}



//GEOCODE PLACE OF DEATH ON DC FORM
/*
path=death_certificate/address_of_death/cmd_get_coordinates
event=onclick
*/
function  geocode_dc_death_place(p_control)
{
    var street = this.street;
    var city = this.city;
    var state = this.state;
    var zip = this.zip_code;
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        var urban_status = null;
        var state_county_fips = null;
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.death_certificate.address_of_death.latitude = geo_data.latitude;
            g_data.death_certificate.address_of_death.longitude = geo_data.longitude;
            g_data.death_certificate.address_of_death.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.death_certificate.address_of_death.census_state_fips = geo_data.CensusStateFips;
            g_data.death_certificate.address_of_death.census_county_fips = geo_data.CensusCountyFips;
            g_data.death_certificate.address_of_death.census_tract_fips = geo_data.CensusTract;
            g_data.death_certificate.address_of_death.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.death_certificate.address_of_death.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.death_certificate.address_of_death.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.death_certificate.address_of_death.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.death_certificate.address_of_death.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('death_certificate/address_of_death/latitude', g_data.death_certificate.address_of_death.latitude);
            $mmria.set_control_value('death_certificate/address_of_death/longitude', g_data.death_certificate.address_of_death.longitude);
            $mmria.set_control_value('death_certificate/address_of_death/feature_matching_geography_type', g_data.death_certificate.address_of_death.feature_matching_geography_type);
            $mmria.set_control_value('death_certificate/address_of_death/naaccr_gis_coordinate_quality_code', g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('death_certificate/address_of_death/naaccr_gis_coordinate_quality_type', g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('death_certificate/address_of_death/naaccr_census_tract_certainty_code', g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('death_certificate/address_of_death/naaccr_census_tract_certainty_type', g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('death_certificate/address_of_death/census_state_fips', g_data.death_certificate.address_of_death.census_state_fips);
            $mmria.set_control_value('death_certificate/address_of_death/census_county_fips', g_data.death_certificate.address_of_death.census_county_fips);
            $mmria.set_control_value('death_certificate/address_of_death/census_tract_fips', g_data.death_certificate.address_of_death.census_tract_fips);
            $mmria.set_control_value('death_certificate/address_of_death/census_cbsa_fips', g_data.death_certificate.address_of_death.census_cbsa_fips);
            $mmria.set_control_value('death_certificate/address_of_death/census_cbsa_micro', g_data.death_certificate.address_of_death.census_cbsa_micro);
            $mmria.set_control_value('death_certificate/address_of_death/census_met_div_fips', g_data.death_certificate.address_of_death.census_met_div_fips);
            $mmria.set_control_value('death_certificate/address_of_death/urban_status', g_data.death_certificate.address_of_death.urban_status);
            $mmria.set_control_value('death_certificate/address_of_death/state_county_fips', g_data.death_certificate.address_of_death.state_county_fips);

		}
		else
		{
			g_data.death_certificate.address_of_death.feature_matching_geography_type = 'Unmatchable';
			g_data.death_certificate.address_of_death.latitude = '';
			g_data.death_certificate.address_of_death.longitude = '';
			g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_code = '';
			g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_type = '';
			g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_code = '';
			g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_type = '';
			g_data.death_certificate.address_of_death.census_state_fips = '';
			g_data.death_certificate.address_of_death.census_county_fips = '';
			g_data.death_certificate.address_of_death.census_tract_fips = '';
			g_data.death_certificate.address_of_death.census_cbsa_fips = '';
			g_data.death_certificate.address_of_death.census_cbsa_micro = '';
			g_data.death_certificate.address_of_death.census_met_div_fips = '';
			g_data.death_certificate.address_of_death.urban_status = '';
			g_data.death_certificate.address_of_death.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('death_certificate/address_of_death/feature_matching_geography_type', g_data.death_certificate.address_of_death.feature_matching_geography_type);
			$mmria.set_control_value('death_certificate/address_of_death/latitude', g_data.death_certificate.address_of_death.latitude);
			$mmria.set_control_value('death_certificate/address_of_death/longitude', g_data.death_certificate.address_of_death.longitude);
			$mmria.set_control_value('death_certificate/address_of_death/naaccr_gis_coordinate_quality_code', g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('death_certificate/address_of_death/naaccr_gis_coordinate_quality_type', g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('death_certificate/address_of_death/naaccr_census_tract_certainty_code', g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('death_certificate/address_of_death/naaccr_census_tract_certainty_type', g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('death_certificate/address_of_death/census_state_fips', g_data.death_certificate.address_of_death.census_state_fips);
			$mmria.set_control_value('death_certificate/address_of_death/census_county_fips', g_data.death_certificate.address_of_death.census_county_fips);
			$mmria.set_control_value('death_certificate/address_of_death/census_tract_fips', g_data.death_certificate.address_of_death.census_tract_fips);
			$mmria.set_control_value('death_certificate/address_of_death/census_cbsa_fips', g_data.death_certificate.address_of_death.census_cbsa_fips);
			$mmria.set_control_value('death_certificate/address_of_death/census_cbsa_micro', g_data.death_certificate.address_of_death.census_cbsa_micro);
			$mmria.set_control_value('death_certificate/address_of_death/census_met_div_fips', g_data.death_certificate.address_of_death.census_met_div_fips);
			$mmria.set_control_value('death_certificate/address_of_death/urban_status', g_data.death_certificate.address_of_death.urban_status);
			$mmria.set_control_value('death_certificate/address_of_death/state_county_fips', g_data.death_certificate.address_of_death.state_county_fips);

		}
    });
}
//GEOCODE FACILITY OF DELIVERY ON BC-PARENT FORM
/*
path=birth_fetal_death_certificate_parent/facility_of_delivery_location/cmd_get_coordinates
event=onclick
*/
function geocode_bc_delivery_place(p_control) 
{
    var street = this.street;
    var city = this.city;
    var state = this.state;
    var zip = this.zip_code;
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        var urban_status = null;
        var state_county_fips = null;
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.latitude = geo_data.latitude;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.longitude = geo_data.longitude;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_state_fips = geo_data.CensusStateFips;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_county_fips = geo_data.CensusCountyFips;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_tract_fips = geo_data.CensusTract;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/latitude', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.latitude);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/longitude', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.longitude);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/feature_matching_geography_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.feature_matching_geography_type);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_code', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_code', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_state_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_state_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_county_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_county_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_tract_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_tract_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_micro', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_micro);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_met_div_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_met_div_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/urban_status', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.urban_status);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/state_county_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.state_county_fips);
		}
		else
		{
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.feature_matching_geography_type = 'Unmatchable';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.latitude = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.longitude = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_code = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_type = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_code = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_type = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_state_fips = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_county_fips = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_tract_fips = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_fips = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_micro = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_met_div_fips = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.urban_status = '';
			g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/feature_matching_geography_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.feature_matching_geography_type);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/latitude', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.latitude);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/longitude', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.longitude);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_code', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_code', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_state_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_state_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_county_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_county_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_tract_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_tract_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_micro', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_micro);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_met_div_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_met_div_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/urban_status', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.urban_status);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/state_county_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.state_county_fips);

		}
    });
}
//GEOCODE LOCATION OF MOTHERS RESIDENCE ON BC-PARENT FORM
/*
path=birth_fetal_death_certificate_parent/location_of_residence/cmd_get_coordinates
event=onclick
*/
function geocode_bc_residence(p_control) 
{
    var street = this.street;
    var city = this.city;
    var state = this.state;
    var zip = this.zip_code;
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        var urban_status = null;
        var state_county_fips = null;
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.birth_fetal_death_certificate_parent.location_of_residence.latitude = geo_data.latitude;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.longitude = geo_data.longitude;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.census_state_fips = geo_data.CensusStateFips;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.census_county_fips = geo_data.CensusCountyFips;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.census_tract_fips = geo_data.CensusTract;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.birth_fetal_death_certificate_parent.location_of_residence.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.birth_fetal_death_certificate_parent.location_of_residence.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.birth_fetal_death_certificate_parent.location_of_residence.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/latitude', g_data.birth_fetal_death_certificate_parent.location_of_residence.latitude);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/longitude', g_data.birth_fetal_death_certificate_parent.location_of_residence.longitude);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/feature_matching_geography_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.feature_matching_geography_type);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_code', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_code', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_state_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_state_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_county_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_county_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_tract_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_tract_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_micro', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_micro);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_met_div_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_met_div_fips);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/urban_status', g_data.birth_fetal_death_certificate_parent.location_of_residence.urban_status);
            $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/state_county_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.state_county_fips);
		}
		else
		{
			g_data.birth_fetal_death_certificate_parent.location_of_residence.feature_matching_geography_type = 'Unmatchable';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.latitude = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.longitude = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_code = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_type = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_code = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_type = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.census_state_fips = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.census_county_fips = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.census_tract_fips = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_fips = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_micro = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.census_met_div_fips = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.urban_status = '';
			g_data.birth_fetal_death_certificate_parent.location_of_residence.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/feature_matching_geography_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.feature_matching_geography_type);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/latitude', g_data.birth_fetal_death_certificate_parent.location_of_residence.latitude);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/longitude', g_data.birth_fetal_death_certificate_parent.location_of_residence.longitude);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_code', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_code', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_state_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_state_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_county_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_county_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_tract_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_tract_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_micro', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_micro);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_met_div_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_met_div_fips);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/urban_status', g_data.birth_fetal_death_certificate_parent.location_of_residence.urban_status);
			$mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/state_county_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.state_county_fips);

		}
    });
}
//GEOCODE LOCATION OF PRIMARY PRENATAL CARE FACILITY ON PC FORM
/*
path=prenatal/location_of_primary_prenatal_care_facility/cmd_get_coordinates
event=onclick
*/
function geocode_pc_primary_care_location(p_control) 
{
    var street = this.street;
    var city = this.city;
    var state = this.state;
    var zip = this.zip_code;
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        var urban_status = null;
        var state_county_fips = null;
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.prenatal.location_of_primary_prenatal_care_facility.latitude = geo_data.latitude;
            g_data.prenatal.location_of_primary_prenatal_care_facility.longitude = geo_data.longitude;
            g_data.prenatal.location_of_primary_prenatal_care_facility.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.prenatal.location_of_primary_prenatal_care_facility.census_state_fips = geo_data.CensusStateFips;
            g_data.prenatal.location_of_primary_prenatal_care_facility.census_county_fips = geo_data.CensusCountyFips;
            g_data.prenatal.location_of_primary_prenatal_care_facility.census_tract_fips = geo_data.CensusTract;
            g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.prenatal.location_of_primary_prenatal_care_facility.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.prenatal.location_of_primary_prenatal_care_facility.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.prenatal.location_of_primary_prenatal_care_facility.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/latitude', g_data.prenatal.location_of_primary_prenatal_care_facility.latitude);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/longitude', g_data.prenatal.location_of_primary_prenatal_care_facility.longitude);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/feature_matching_geography_type', g_data.prenatal.location_of_primary_prenatal_care_facility.feature_matching_geography_type);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_code', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_type', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_code', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_type', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_state_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_state_fips);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_county_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_county_fips);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_tract_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_tract_fips);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_cbsa_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_fips);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_cbsa_micro', g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_micro);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_met_div_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_met_div_fips);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/urban_status', g_data.prenatal.location_of_primary_prenatal_care_facility.urban_status);
            $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/state_county_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.state_county_fips);
		}
		else
		{
			g_data.prenatal.location_of_primary_prenatal_care_facility.feature_matching_geography_type = 'Unmatchable';
			g_data.prenatal.location_of_primary_prenatal_care_facility.latitude = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.longitude = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_code = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_type = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_code = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_type = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.census_state_fips = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.census_county_fips = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.census_tract_fips = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_fips = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_micro = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.census_met_div_fips = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.urban_status = '';
			g_data.prenatal.location_of_primary_prenatal_care_facility.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/feature_matching_geography_type', g_data.prenatal.location_of_primary_prenatal_care_facility.feature_matching_geography_type);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/latitude', g_data.prenatal.location_of_primary_prenatal_care_facility.latitude);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/longitude', g_data.prenatal.location_of_primary_prenatal_care_facility.longitude);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_code', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_type', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_code', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_type', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_state_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_state_fips);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_county_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_county_fips);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_tract_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_tract_fips);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_cbsa_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_fips);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_cbsa_micro', g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_micro);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_met_div_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_met_div_fips);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/urban_status', g_data.prenatal.location_of_primary_prenatal_care_facility.urban_status);
			$mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/state_county_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.state_county_fips);

		}
    });
}
//GEOCODE LOCATION OF HOSPITAL ON ER-HOSPITALIZATIONS FORM
/*
path=er_visit_and_hospital_medical_records/name_and_location_facility/get_coordinates
event=onclick
*/
function geocode_erh_location(p_control) 
{
    var street = this.street;
    var city = this.city;
    var state = this.state;
    var zip = this.zip_code;
    var current_erh_index = $global.get_current_multiform_index();
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        var urban_status = null;
        var state_county_fips = null;
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.latitude = geo_data.latitude;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.longitude = geo_data.longitude;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_state_fips = geo_data.CensusStateFips;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_county_fips = geo_data.CensusCountyFips;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_tract_fips = geo_data.CensusTract;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/latitude', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.latitude);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/longitude', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.longitude);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/feature_matching_geography_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.feature_matching_geography_type);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_code', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_code', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_state_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_state_fips);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_county_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_county_fips);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_tract_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_tract_fips);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_fips);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_micro', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_micro);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_met_div_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_met_div_fips);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/urban_status', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.urban_status);
            $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/state_county_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.state_county_fips);
		}
		else
		{
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.feature_matching_geography_type = 'Unmatchable';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.latitude = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.longitude = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_code = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_type = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_code = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_type = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_state_fips = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_county_fips = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_tract_fips = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_fips = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_micro = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_met_div_fips = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.urban_status = '';
			g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/feature_matching_geography_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.feature_matching_geography_type);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/latitude', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.latitude);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/longitude', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.longitude);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_code', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_code', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_state_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_state_fips);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_county_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_county_fips);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_tract_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_tract_fips);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_fips);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_micro', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_micro);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_met_div_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_met_div_fips);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/urban_status', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.urban_status);
			$mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/state_county_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.state_county_fips);

		}
    });
}
//GEOCODE LOCATION OF OFFICE ON OTHER MEDICAL VISITS FORM
/*
path=other_medical_office_visits/location_of_medical_care_facility/get_coordinates
event=onclick
*/
function geocode_omov_location(p_control) 
{
    var street = this.street;
    var city = this.city;
    var state = this.state;
    var zip = this.zip_code;
    var current_omov_index = $global.get_current_multiform_index();
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        var urban_status = null;
        var state_county_fips = null;
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.latitude = geo_data.latitude;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.longitude = geo_data.longitude;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_state_fips = geo_data.CensusStateFips;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_county_fips = geo_data.CensusCountyFips;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_tract_fips = geo_data.CensusTract;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/latitude', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.latitude);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/longitude', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.longitude);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/feature_matching_geography_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.feature_matching_geography_type);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_code', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_code', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_state_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_state_fips);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_county_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_county_fips);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_tract_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_tract_fips);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_cbsa_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_fips);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_cbsa_micro', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_micro);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_met_div_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_met_div_fips);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/urban_status', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.urban_status);
            $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/state_county_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.state_county_fips);
		}
		else
		{
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.feature_matching_geography_type = 'Unmatchable';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.latitude = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.longitude = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_code = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_type = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_code = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_type = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_state_fips = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_county_fips = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_tract_fips = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_fips = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_micro = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_met_div_fips = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.urban_status = '';
			g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/feature_matching_geography_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.feature_matching_geography_type);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/latitude', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.latitude);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/longitude', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.longitude);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_code', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_code', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_state_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_state_fips);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_county_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_county_fips);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_tract_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_tract_fips);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_cbsa_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_fips);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_cbsa_micro', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_micro);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_met_div_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_met_div_fips);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/urban_status', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.urban_status);
			$mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/state_county_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.state_county_fips);

		}
    });
}

//CALCULATE TIME BETWEEN ONSET OF LABOR AND ARRIVAL AT HOSPITAL
/*
path=er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/cmd_duration_of_labor_prior_to_arrival
event=onclick
*/
function duration_of_labor(p_control)
{
    var hours = null;
    var current_dol_index = $global.get_current_multiform_index();
    var onset_year = parseInt(g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.year);
    var onset_month = parseInt(g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.month);
    var onset_day = parseInt(g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.day);
	
	var onset_time = null;
    if (g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor instanceof Date) 
	{
		onset_time = g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor;
    }
	else
	{
        onset_time = new Date('January 1, 1900 ' + g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor);
    }
	
    var arrival_year = parseInt(g_data.er_visit_and_hospital_medical_records[current_dol_index].basic_admission_and_discharge_information.date_of_arrival.year);
    var arrival_month = parseInt(g_data.er_visit_and_hospital_medical_records[current_dol_index].basic_admission_and_discharge_information.date_of_arrival.month);
    var arrival_day = parseInt(g_data.er_visit_and_hospital_medical_records[current_dol_index].basic_admission_and_discharge_information.date_of_arrival.day);
	
	var arrival_time = null;
    if (g_data.er_visit_and_hospital_medical_records[current_dol_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival instanceof Date) 
	{
        arrival_time = g_data.er_visit_and_hospital_medical_records[current_dol_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival;
    } 
	else 
	{
        arrival_time = new Date('January 1, 1900 ' + g_data.er_visit_and_hospital_medical_records[current_dol_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival);
    }
    if ($global.isValidDate(onset_year, onset_month, onset_day) == true && $global.isValidDate(arrival_year, arrival_month, arrival_day) == true && (g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor != '' || g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor != null) && (g_data.er_visit_and_hospital_medical_records[current_dol_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival != '' || g_data.er_visit_and_hospital_medical_records[current_dol_index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival_time != null)) {
        var onset_date = new Date(onset_year, onset_month - 1, onset_day, onset_time.getHours(), onset_time.getMinutes());
        var arrival_date = new Date(arrival_year, arrival_month - 1, arrival_day, arrival_time.getHours(), arrival_time.getMinutes());
        var hours = Math.round((arrival_date - onset_date) / 3600000 * 100) / 100;
        if (hours > 1) 
        {
            g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.duration_of_labor_prior_to_arrival = hours;
            $mmria.save_current_record();
            $mmria.set_control_value('er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/duration_of_labor_prior_to_arrival', g_data.er_visit_and_hospital_medical_records[current_dol_index].onset_of_labor.date_of_onset_of_labor.duration_of_labor_prior_to_arrival);
        }
    }
}


// OMB RACE RECODE FOR CASE ON DC FORM
/*
path=death_certificate/race/race
event=onclick
*/
function omb_race_recode_dc(p_control) 
{
    var race_recode = null;
    var race = this.race;
    race_recode = $global.calculate_omb_recode(race);
    this.omb_race_recode = race_recode;
    $mmria.save_current_record();
    $mmria.set_control_value('death_certificate/race/omb_race_recode', this.omb_race_recode);
}


// OMB RACE RECODE FOR MOM ON BC FORM
/*
path=birth_fetal_death_certificate_parent/race/race_of_mother
event=onclick
*/
function omb_mrace_recode_bc(p_control) 
{
    var race_recode = null;
    var race = this.race_of_mother;
    race_recode = $global.calculate_omb_recode(race);
    this.omb_race_recode = race_recode;
    $mmria.save_current_record();
    $mmria.set_control_value('birth_fetal_death_certificate_parent/race/omb_race_recode', this.omb_race_recode);
}
// OMB RACE RECODE FOR DAD ON BC FORM
/*
path=birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father
event=onclick
*/
function omb_frace_recode_bc(p_control) 
{
    var race_recode = null;
    var race = this.race_of_father;
    race_recode = $global.calculate_omb_recode(race);
    this.omb_race_recode = race_recode;
    $mmria.save_current_record();
    $mmria.set_control_value('birth_fetal_death_certificate_parent/demographic_of_father/race/omb_race_recode', this.omb_race_recode);
}

// CLEAR DISTANCE BETWEEN LAST RESIDENCE AND HOSPITAL OF DEATH ON DC FORM
/*
path=death_certificate/address_of_death/cmd_calculate_distance_clear
event=onclick
*/
function distance_clear_dc(p_control) 
{
    this.estimated_death_distance_from_residence = '';
    $mmria.save_current_record();
    $mmria.set_control_value('death_certificate/address_of_death/estimated_death_distance_from_residence', this.estimated_death_distance_from_residence);
}
// CLEAR DISTANCE BETWEEN LAST RESIDENCE AND HOSPITAL OF DELIVERY ON BC FORM
/*
path=birth_fetal_death_certificate_parent/location_of_residence/cmd_calc_distance_clear
event=onclick
*/
function distance_clear_bc(p_control) 
{
    this.estimated_distance_from_residence = '';
    $mmria.save_current_record();
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/estimated_distance_from_residence', this.estimated_distance_from_residence);
}

// CLEAR HOURS BETWEEN ONSET OF LABOR AND ARRIVAL ON HOSPITAL FORM
/*
path=er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/cmd_duration_of_labor_prior_to_arrival_clear
event=onclick
*/
function hours_clear_mr(p_control) 
{
    this.duration_of_labor_prior_to_arrival = '';
    $mmria.save_current_record();
    $mmria.set_control_value('er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/duration_of_labor_prior_to_arrival', this.duration_of_labor_prior_to_arrival);
}
// CLEAR COORDINATES FOR RESIDENCE ON DC FORM
/*
path=death_certificate/place_of_last_residence/get_coordinates_clear
event=onclick
*/
function coordinates_clear_res_dc(p_control) 
{
    g_data.death_certificate.place_of_last_residence.feature_matching_geography_type = '';
    g_data.death_certificate.place_of_last_residence.latitude = '';
    g_data.death_certificate.place_of_last_residence.longitude = '';
    g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_code = '';
    g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_type = '';
    g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_code = '';
    g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_type = '';
    g_data.death_certificate.place_of_last_residence.census_state_fips = '';
    g_data.death_certificate.place_of_last_residence.census_county_fips = '';
    g_data.death_certificate.place_of_last_residence.census_tract_fips = '';
    g_data.death_certificate.place_of_last_residence.census_cbsa_fips = '';
    g_data.death_certificate.place_of_last_residence.census_cbsa_micro = '';
    g_data.death_certificate.place_of_last_residence.census_met_div_fips = '';
	g_data.death_certificate.place_of_last_residence.urban_status = '';
    g_data.death_certificate.place_of_last_residence.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('death_certificate/place_of_last_residence/feature_matching_geography_type', g_data.death_certificate.place_of_last_residence.feature_matching_geography_type);
    $mmria.set_control_value('death_certificate/place_of_last_residence/latitude', g_data.death_certificate.place_of_last_residence.latitude);
    $mmria.set_control_value('death_certificate/place_of_last_residence/longitude', g_data.death_certificate.place_of_last_residence.longitude);
    $mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_code', g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_type', g_data.death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_census_tract_certainty_code', g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('death_certificate/place_of_last_residence/naaccr_census_tract_certainty_type', g_data.death_certificate.place_of_last_residence.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('death_certificate/place_of_last_residence/census_state_fips', g_data.death_certificate.place_of_last_residence.census_state_fips);
    $mmria.set_control_value('death_certificate/place_of_last_residence/census_county_fips', g_data.death_certificate.place_of_last_residence.census_county_fips);
    $mmria.set_control_value('death_certificate/place_of_last_residence/census_tract_fips', g_data.death_certificate.place_of_last_residence.census_tract_fips);
    $mmria.set_control_value('death_certificate/place_of_last_residence/census_cbsa_fips', g_data.death_certificate.place_of_last_residence.census_cbsa_fips);
    $mmria.set_control_value('death_certificate/place_of_last_residence/census_cbsa_micro', g_data.death_certificate.place_of_last_residence.census_cbsa_micro);
    $mmria.set_control_value('death_certificate/place_of_last_residence/census_met_div_fips', g_data.death_certificate.place_of_last_residence.census_met_div_fips);
	$mmria.set_control_value('death_certificate/place_of_last_residence/urban_status', g_data.death_certificate.place_of_last_residence.urban_status);
    $mmria.set_control_value('death_certificate/place_of_last_residence/state_county_fips', g_data.death_certificate.place_of_last_residence.state_county_fips);
}
// CLEAR COORDINATES FOR INJURY LOCATION ON DC FORM
/*
path=death_certificate/address_of_injury/cmd_get_coordinates_clear
event=onclick
*/
function coordinates_clear_inj_dc(p_control) 
{
    g_data.death_certificate.address_of_injury.feature_matching_geography_type = '';
    g_data.death_certificate.address_of_injury.latitude = '';
    g_data.death_certificate.address_of_injury.longitude = '';
    g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_code = '';
    g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_type = '';
    g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_code = '';
    g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_type = '';
    g_data.death_certificate.address_of_injury.census_state_fips = '';
    g_data.death_certificate.address_of_injury.census_county_fips = '';
    g_data.death_certificate.address_of_injury.census_tract_fips = '';
    g_data.death_certificate.address_of_injury.census_cbsa_fips = '';
    g_data.death_certificate.address_of_injury.census_cbsa_micro = '';
    g_data.death_certificate.address_of_injury.census_met_div_fips = '';
    g_data.death_certificate.address_of_injury.urban_status = '';
    g_data.death_certificate.address_of_injury.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('death_certificate/address_of_injury/feature_matching_geography_type', g_data.death_certificate.address_of_injury.feature_matching_geography_type);
    $mmria.set_control_value('death_certificate/address_of_injury/latitude', g_data.death_certificate.address_of_injury.latitude);
    $mmria.set_control_value('death_certificate/address_of_injury/longitude', g_data.death_certificate.address_of_injury.longitude);
    $mmria.set_control_value('death_certificate/address_of_injury/naaccr_gis_coordinate_quality_code', g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('death_certificate/address_of_injury/naaccr_gis_coordinate_quality_type', g_data.death_certificate.address_of_injury.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('death_certificate/address_of_injury/naaccr_census_tract_certainty_code', g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('death_certificate/address_of_injury/naaccr_census_tract_certainty_type', g_data.death_certificate.address_of_injury.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('death_certificate/address_of_injury/census_state_fips', g_data.death_certificate.address_of_injury.census_state_fips);
    $mmria.set_control_value('death_certificate/address_of_injury/census_county_fips', g_data.death_certificate.address_of_injury.census_county_fips);
    $mmria.set_control_value('death_certificate/address_of_injury/census_tract_fips', g_data.death_certificate.address_of_injury.census_tract_fips);
    $mmria.set_control_value('death_certificate/address_of_injury/census_cbsa_fips', g_data.death_certificate.address_of_injury.census_cbsa_fips);
    $mmria.set_control_value('death_certificate/address_of_injury/census_cbsa_micro', g_data.death_certificate.address_of_injury.census_cbsa_micro);
    $mmria.set_control_value('death_certificate/address_of_injury/census_met_div_fips', g_data.death_certificate.address_of_injury.census_met_div_fips);
    $mmria.set_control_value('death_certificate/address_of_injury/urban_status', g_data.death_certificate.address_of_injury.urban_status);
    $mmria.set_control_value('death_certificate/address_of_injury/state_county_fips', g_data.death_certificate.address_of_injury.state_county_fips);
}
// CLEAR COORDINATES FOR DEATH LOCATION ON DC FORM
/*
path=death_certificate/address_of_death/cmd_get_coordinates_clear
event=onclick
*/
function coordinates_clear_death_loc_dc(p_control) 
{
    g_data.death_certificate.address_of_death.feature_matching_geography_type = '';
    g_data.death_certificate.address_of_death.latitude = '';
    g_data.death_certificate.address_of_death.longitude = '';
    g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_code = '';
    g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_type = '';
    g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_code = '';
    g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_type = '';
    g_data.death_certificate.address_of_death.census_state_fips = '';
    g_data.death_certificate.address_of_death.census_county_fips = '';
    g_data.death_certificate.address_of_death.census_tract_fips = '';
    g_data.death_certificate.address_of_death.census_cbsa_fips = '';
    g_data.death_certificate.address_of_death.census_cbsa_micro = '';
    g_data.death_certificate.address_of_death.census_met_div_fips = '';
    g_data.death_certificate.address_of_death.urban_status = '';
    g_data.death_certificate.address_of_death.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('death_certificate/address_of_death/feature_matching_geography_type', g_data.death_certificate.address_of_death.feature_matching_geography_type);
    $mmria.set_control_value('death_certificate/address_of_death/latitude', g_data.death_certificate.address_of_death.latitude);
    $mmria.set_control_value('death_certificate/address_of_death/longitude', g_data.death_certificate.address_of_death.longitude);
    $mmria.set_control_value('death_certificate/address_of_death/naaccr_gis_coordinate_quality_code', g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('death_certificate/address_of_death/naaccr_gis_coordinate_quality_type', g_data.death_certificate.address_of_death.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('death_certificate/address_of_death/naaccr_census_tract_certainty_code', g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('death_certificate/address_of_death/naaccr_census_tract_certainty_type', g_data.death_certificate.address_of_death.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('death_certificate/address_of_death/census_state_fips', g_data.death_certificate.address_of_death.census_state_fips);
    $mmria.set_control_value('death_certificate/address_of_death/census_county_fips', g_data.death_certificate.address_of_death.census_county_fips);
    $mmria.set_control_value('death_certificate/address_of_death/census_tract_fips', g_data.death_certificate.address_of_death.census_tract_fips);
    $mmria.set_control_value('death_certificate/address_of_death/census_cbsa_fips', g_data.death_certificate.address_of_death.census_cbsa_fips);
    $mmria.set_control_value('death_certificate/address_of_death/census_cbsa_micro', g_data.death_certificate.address_of_death.census_cbsa_micro);
    $mmria.set_control_value('death_certificate/address_of_death/census_met_div_fips', g_data.death_certificate.address_of_death.census_met_div_fips);
    $mmria.set_control_value('death_certificate/address_of_death/urban_status', g_data.death_certificate.address_of_death.urban_status);
    $mmria.set_control_value('death_certificate/address_of_death/state_county_fips', g_data.death_certificate.address_of_death.state_county_fips);
}
// CLEAR COORDINATES FOR DELIVERY LOCATION ON BC FORM
/*
path=birth_fetal_death_certificate_parent/facility_of_delivery_location/cmd_get_coordinates_clear
event=onclick
*/
function coordinates_clear_delivery_loc_bc(p_control) 
{
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.feature_matching_geography_type = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.latitude = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.longitude = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_code = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_type = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_code = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_type = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_state_fips = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_county_fips = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_tract_fips = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_fips = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_micro = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_met_div_fips = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.urban_status = '';
    g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/feature_matching_geography_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.feature_matching_geography_type);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/latitude', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.latitude);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/longitude', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.longitude);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_code', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_code', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_type', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_state_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_state_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_county_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_county_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_tract_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_tract_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_micro', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_micro);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/census_met_div_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.census_met_div_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/urban_status', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.urban_status);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/facility_of_delivery_location/state_county_fips', g_data.birth_fetal_death_certificate_parent.facility_of_delivery_location.state_county_fips);
}
// CLEAR COORDINATES FOR RESIDENCE ON BC FORM
/*
path=birth_fetal_death_certificate_parent/location_of_residence/cmd_get_coordinates_clear
event=onclick
*/
function coordinates_clear_residence_bc(p_control) 
{
    g_data.birth_fetal_death_certificate_parent.location_of_residence.feature_matching_geography_type = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.latitude = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.longitude = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_code = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_type = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_code = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_type = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.census_state_fips = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.census_county_fips = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.census_tract_fips = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_fips = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_micro = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.census_met_div_fips = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.urban_status = '';
    g_data.birth_fetal_death_certificate_parent.location_of_residence.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/feature_matching_geography_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.feature_matching_geography_type);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/latitude', g_data.birth_fetal_death_certificate_parent.location_of_residence.latitude);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/longitude', g_data.birth_fetal_death_certificate_parent.location_of_residence.longitude);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_code', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_code', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_type', g_data.birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_state_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_state_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_county_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_county_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_tract_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_tract_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_micro', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_micro);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/census_met_div_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.census_met_div_fips);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/urban_status', g_data.birth_fetal_death_certificate_parent.location_of_residence.urban_status);
    $mmria.set_control_value('birth_fetal_death_certificate_parent/location_of_residence/state_county_fips', g_data.birth_fetal_death_certificate_parent.location_of_residence.state_county_fips);
}
// CLEAR COORDINATES FOR PNC LOCATION ON PC FORM
/*
path=prenatal/location_of_primary_prenatal_care_facility/cmd_get_coordinates_clear
event=onclick
*/
function coordinates_clear_pnc_loc_pc(p_control) 
{
    g_data.prenatal.location_of_primary_prenatal_care_facility.feature_matching_geography_type = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.latitude = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.longitude = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_code = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_type = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_code = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_type = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.census_state_fips = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.census_county_fips = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.census_tract_fips = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_fips = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_micro = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.census_met_div_fips = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.urban_status = '';
    g_data.prenatal.location_of_primary_prenatal_care_facility.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/feature_matching_geography_type', g_data.prenatal.location_of_primary_prenatal_care_facility.feature_matching_geography_type);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/latitude', g_data.prenatal.location_of_primary_prenatal_care_facility.latitude);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/longitude', g_data.prenatal.location_of_primary_prenatal_care_facility.longitude);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_code', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_type', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_code', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_type', g_data.prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_state_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_state_fips);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_county_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_county_fips);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_tract_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_tract_fips);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_cbsa_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_fips);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_cbsa_micro', g_data.prenatal.location_of_primary_prenatal_care_facility.census_cbsa_micro);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/census_met_div_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.census_met_div_fips);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/urban_status', g_data.prenatal.location_of_primary_prenatal_care_facility.urban_status);
    $mmria.set_control_value('prenatal/location_of_primary_prenatal_care_facility/state_county_fips', g_data.prenatal.location_of_primary_prenatal_care_facility.state_county_fips);
}
// CLEAR COORDINATES FOR HOSPITAL LOCATION ON MR FORM
/*
path=er_visit_and_hospital_medical_records/name_and_location_facility/get_coordinates_clear
event=onclick
*/
function coordinates_clear_hos_loc_mr(p_control) 
{
    var current_erh_index = $global.get_current_multiform_index();
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.feature_matching_geography_type = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.latitude = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.longitude = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_code = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_type = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_code = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_type = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_state_fips = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_county_fips = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_tract_fips = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_fips = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_micro = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_met_div_fips = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.urban_status = '';
    g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/feature_matching_geography_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.feature_matching_geography_type);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/latitude', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.latitude);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/longitude', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.longitude);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_code', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_code', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_type', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_state_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_state_fips);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_county_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_county_fips);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_tract_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_tract_fips);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_fips);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_micro', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_cbsa_micro);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/census_met_div_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.census_met_div_fips);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/urban_status', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.urban_status);
    $mmria.set_control_value('er_visit_and_hospital_medical_records/name_and_location_facility/state_county_fips', g_data.er_visit_and_hospital_medical_records[current_erh_index].name_and_location_facility.state_county_fips);
}
// CLEAR COORDINATES FOR HOSPITAL LOCATION ON OMV FORM
/*
path=other_medical_office_visits/location_of_medical_care_facility/get_coordinates_clear
event=onclick
*/
function coordinates_clear_office_loc_mv(p_control) 
{
    var current_omov_index = $global.get_current_multiform_index();
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.feature_matching_geography_type = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.latitude = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.longitude = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_code = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_type = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_code = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_type = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_state_fips = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_county_fips = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_tract_fips = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_fips = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_micro = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_met_div_fips = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.urban_status = '';
    g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/feature_matching_geography_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.feature_matching_geography_type);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/latitude', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.latitude);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/longitude', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.longitude);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_code', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_code', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_type', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_state_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_state_fips);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_county_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_county_fips);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_tract_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_tract_fips);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_cbsa_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_fips);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_cbsa_micro', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_cbsa_micro);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/census_met_div_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.census_met_div_fips);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/urban_status', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.urban_status);
    $mmria.set_control_value('other_medical_office_visits/location_of_medical_care_facility/state_county_fips', g_data.other_medical_office_visits[current_omov_index].location_of_medical_care_facility.state_county_fips);
}


/*
path=medical_transport/origin_information/address/get_coordinates
event=onclick
*/
function medical_transport_origin_information_address_get_coordinates(p_control) 
{
    let street = this.street;
    let city = this.city;
    let state = this.state;
    let zip = this.zip_code;
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        let urban_status = null;
        let state_county_fips = null;
        var current_mt_index = $global.get_current_multiform_index();
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.medical_transport[current_mt_index].origin_information.address.latitude = geo_data.latitude;
            g_data.medical_transport[current_mt_index].origin_information.address.longitude = geo_data.longitude;
            g_data.medical_transport[current_mt_index].origin_information.address.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.medical_transport[current_mt_index].origin_information.address.census_state_fips = geo_data.CensusStateFips;
            g_data.medical_transport[current_mt_index].origin_information.address.census_county_fips = geo_data.CensusCountyFips;
            g_data.medical_transport[current_mt_index].origin_information.address.census_tract_fips = geo_data.CensusTract;
            g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.medical_transport[current_mt_index].origin_information.address.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }
            

            g_data.medical_transport[current_mt_index].origin_information.address.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.medical_transport[current_mt_index].origin_information.address.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('medical_transport/origin_information/address/latitude', g_data.medical_transport[current_mt_index].origin_information.address.latitude);
            $mmria.set_control_value('medical_transport/origin_information/address/longitude', g_data.medical_transport[current_mt_index].origin_information.address.longitude);
            $mmria.set_control_value('medical_transport/origin_information/address/feature_matching_geography_type', g_data.medical_transport[current_mt_index].origin_information.address.feature_matching_geography_type);
            $mmria.set_control_value('medical_transport/origin_information/address/naaccr_gis_coordinate_quality_code', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('medical_transport/origin_information/address/naaccr_gis_coordinate_quality_type', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('medical_transport/origin_information/address/naaccr_census_tract_certainty_code', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('medical_transport/origin_information/address/naaccr_census_tract_certainty_type', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('medical_transport/origin_information/address/census_state_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_state_fips);
            $mmria.set_control_value('medical_transport/origin_information/address/census_county_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_county_fips);
            $mmria.set_control_value('medical_transport/origin_information/address/census_tract_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_tract_fips);
            $mmria.set_control_value('medical_transport/origin_information/address/census_cbsa_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_fips);
            $mmria.set_control_value('medical_transport/origin_information/address/census_cbsa_micro', g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_micro);
            $mmria.set_control_value('medical_transport/origin_information/address/census_met_div_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_met_div_fips);
            $mmria.set_control_value('medical_transport/origin_information/address/urban_status', g_data.medical_transport[current_mt_index].origin_information.address.urban_status);
            $mmria.set_control_value('medical_transport/origin_information/address/state_county_fips', g_data.medical_transport[current_mt_index].origin_information.address.state_county_fips);
		}
		else
		{
			g_data.medical_transport[current_mt_index].origin_information.address.feature_matching_geography_type = 'Unmatchable';
			g_data.medical_transport[current_mt_index].origin_information.address.latitude = '';
			g_data.medical_transport[current_mt_index].origin_information.address.longitude = '';
			g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_code = '';
			g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_type = '';
			g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_code = '';
			g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_type = '';
			g_data.medical_transport[current_mt_index].origin_information.address.census_state_fips = '';
			g_data.medical_transport[current_mt_index].origin_information.address.census_county_fips = '';
			g_data.medical_transport[current_mt_index].origin_information.address.census_tract_fips = '';
			g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_fips = '';
			g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_micro = '';
			g_data.medical_transport[current_mt_index].origin_information.address.census_met_div_fips = '';
			g_data.medical_transport[current_mt_index].origin_information.address.urban_status = '';
            g_data.medical_transport[current_mt_index].origin_information.address.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('medical_transport/origin_information/address/feature_matching_geography_type', g_data.medical_transport[current_mt_index].origin_information.address.feature_matching_geography_type);
			$mmria.set_control_value('medical_transport/origin_information/address/latitude', g_data.medical_transport[current_mt_index].origin_information.address.latitude);
			$mmria.set_control_value('medical_transport/origin_information/address/longitude', g_data.medical_transport[current_mt_index].origin_information.address.longitude);
			$mmria.set_control_value('medical_transport/origin_information/address/naaccr_gis_coordinate_quality_code', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('medical_transport/origin_information/address/naaccr_gis_coordinate_quality_type', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('medical_transport/origin_information/address/naaccr_census_tract_certainty_code', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('medical_transport/origin_information/address/naaccr_census_tract_certainty_type', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('medical_transport/origin_information/address/census_state_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_state_fips);
			$mmria.set_control_value('medical_transport/origin_information/address/census_county_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_county_fips);
			$mmria.set_control_value('medical_transport/origin_information/address/census_tract_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_tract_fips);
			$mmria.set_control_value('medical_transport/origin_information/address/census_cbsa_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_fips);
			$mmria.set_control_value('medical_transport/origin_information/address/census_cbsa_micro', g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_micro);
			$mmria.set_control_value('medical_transport/origin_information/address/census_met_div_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_met_div_fips);
			$mmria.set_control_value('medical_transport/origin_information/address/urban_status', g_data.medical_transport[current_mt_index].origin_information.address.urban_status);
			$mmria.set_control_value('medical_transport/origin_information/address/state_county_fips', g_data.medical_transport[current_mt_index].origin_information.address.state_county_fips);

		}
    });
}

/*
path=medical_transport/origin_information/address/get_coordinates_clear
event=onclick
*/
function medical_transport_origin_information_address_get_coordinates_clear(p_control) 
{
    var current_mt_index = $global.get_current_multiform_index();

    g_data.medical_transport[current_mt_index].origin_information.address.feature_matching_geography_type = '';
    g_data.medical_transport[current_mt_index].origin_information.address.latitude = '';
    g_data.medical_transport[current_mt_index].origin_information.address.longitude = '';
    g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_code = '';
    g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_type = '';
    g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_code = '';
    g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_type = '';
    g_data.medical_transport[current_mt_index].origin_information.address.census_state_fips = '';
    g_data.medical_transport[current_mt_index].origin_information.address.census_county_fips = '';
    g_data.medical_transport[current_mt_index].origin_information.address.census_tract_fips = '';
    g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_fips = '';
    g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_micro = '';
    g_data.medical_transport[current_mt_index].origin_information.address.census_met_div_fips = '';
	g_data.medical_transport[current_mt_index].origin_information.address.urban_status = '';
    g_data.medical_transport[current_mt_index].origin_information.address.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('medical_transport/origin_information/address/feature_matching_geography_type', g_data.medical_transport[current_mt_index].origin_information.address.feature_matching_geography_type);
    $mmria.set_control_value('medical_transport/origin_information/address/latitude', g_data.medical_transport[current_mt_index].origin_information.address.latitude);
    $mmria.set_control_value('medical_transport/origin_information/address/longitude', g_data.medical_transport[current_mt_index].origin_information.address.longitude);
    $mmria.set_control_value('medical_transport/origin_information/address/naaccr_gis_coordinate_quality_code', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('medical_transport/origin_information/address/naaccr_gis_coordinate_quality_type', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('medical_transport/origin_information/address/naaccr_census_tract_certainty_code', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('medical_transport/origin_information/address/naaccr_census_tract_certainty_type', g_data.medical_transport[current_mt_index].origin_information.address.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('medical_transport/origin_information/address/census_state_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_state_fips);
    $mmria.set_control_value('medical_transport/origin_information/address/census_county_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_county_fips);
    $mmria.set_control_value('medical_transport/origin_information/address/census_tract_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_tract_fips);
    $mmria.set_control_value('medical_transport/origin_information/address/census_cbsa_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_fips);
    $mmria.set_control_value('medical_transport/origin_information/address/census_cbsa_micro', g_data.medical_transport[current_mt_index].origin_information.address.census_cbsa_micro);
    $mmria.set_control_value('medical_transport/origin_information/address/census_met_div_fips', g_data.medical_transport[current_mt_index].origin_information.address.census_met_div_fips);
	$mmria.set_control_value('medical_transport/origin_information/address/urban_status', g_data.medical_transport[current_mt_index].origin_information.address.urban_status);
    $mmria.set_control_value('medical_transport/origin_information/address/state_county_fips', g_data.medical_transport[current_mt_index].origin_information.address.state_county_fips);
}


/*
path=medical_transport/destination_information/address/get_coordinates
event=onclick
*/
function medical_transport_destination_information_address_get_coordinates(p_control) 
{
    let street = this.street;
    let city = this.city;
    let state = this.state;
    let zip = this.zip_code;
    $mmria.get_geocode_info(street, city, state, zip, g_data.home_record.date_of_death.year, function (geo_data) 
    {
        let urban_status = null;
        let state_county_fips = null;
        var current_mt_index = $global.get_current_multiform_index();
        if (geo_data && geo_data.FeatureMatchingResultType) 
        {
            g_data.medical_transport[current_mt_index].destination_information.address.latitude = geo_data.latitude;
            g_data.medical_transport[current_mt_index].destination_information.address.longitude = geo_data.longitude;
            g_data.medical_transport[current_mt_index].destination_information.address.feature_matching_geography_type = geo_data.FeatureMatchingGeographyType;
            g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_code = geo_data.NAACCRGISCoordinateQualityCode;
            g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_type = geo_data.NAACCRGISCoordinateQualityType;
            g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_code = geo_data.NAACCRCensusTractCertaintyCode;
            g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_type = geo_data.NAACCRCensusTractCertaintyType;
            g_data.medical_transport[current_mt_index].destination_information.address.census_state_fips = geo_data.CensusStateFips;
            g_data.medical_transport[current_mt_index].destination_information.address.census_county_fips = geo_data.CensusCountyFips;
            g_data.medical_transport[current_mt_index].destination_information.address.census_tract_fips = geo_data.CensusTract;
            g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_fips = geo_data.CensusCbsaFips;
            g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_micro = geo_data.CensusCbsaMicro;
            g_data.medical_transport[current_mt_index].destination_information.address.census_met_div_fips = geo_data.CensusMetDivFips;
            // calculate urban_status
            if 
            (
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 && 
                parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 && 
                parseInt(geo_data.CensusCbsaFips) > 0
            )
            {
                if (geo_data.CensusMetDivFips) 
                {
                    urban_status = 'Metropolitan Division';
                } 
                else if (parseInt(geo_data.CensusCbsaMicro) == 0) 
                {
                    urban_status = 'Metropolitan';
                }
                else if (parseInt(geo_data.CensusCbsaMicro) == 1) 
                {
                    urban_status = 'Micropolitan';
                }
            }
            else if
			(			
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) > 0 &&
				parseInt(geo_data.NAACCRCensusTractCertaintyCode) < 7 &&
				geo_data.CensusCbsaFips == ''    
            ) 
			{
				urban_status = 'Rural';
			}
	        else  
            {
                urban_status = 'Undetermined';
            }

            let census_track_certainty_code = parseInt(geo_data.NAACCRCensusTractCertaintyCode);
            if
            (
                census_track_certainty_code != 1
            )
            {

                $mmria.info_dialog_show("Address Geocode","Validation: Census Tract Certainty Code is Not 1 (Census tract based on complete and valid street address.)", "There might be a potential error in the address. Please verify address.");
            }

            g_data.medical_transport[current_mt_index].destination_information.address.urban_status = urban_status;
            // calculate state_county_fips
            if (geo_data.CensusStateFips && geo_data.CensusCountyFips) 
			{
                state_county_fips = geo_data.CensusStateFips + geo_data.CensusCountyFips;
            }
            g_data.medical_transport[current_mt_index].destination_information.address.state_county_fips = state_county_fips;

            $mmria.save_current_record();
            $mmria.set_control_value('medical_transport/destination_information/address/latitude', g_data.medical_transport[current_mt_index].destination_information.address.latitude);
            $mmria.set_control_value('medical_transport/destination_information/address/longitude', g_data.medical_transport[current_mt_index].destination_information.address.longitude);
            $mmria.set_control_value('medical_transport/destination_information/address/feature_matching_geography_type', g_data.medical_transport[current_mt_index].destination_information.address.feature_matching_geography_type);
            $mmria.set_control_value('medical_transport/destination_information/address/naaccr_gis_coordinate_quality_code', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_code);
            $mmria.set_control_value('medical_transport/destination_information/address/naaccr_gis_coordinate_quality_type', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_type);
            $mmria.set_control_value('medical_transport/destination_information/address/naaccr_census_tract_certainty_code', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_code);
            $mmria.set_control_value('medical_transport/destination_information/address/naaccr_census_tract_certainty_type', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_type);
            $mmria.set_control_value('medical_transport/destination_information/address/census_state_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_state_fips);
            $mmria.set_control_value('medical_transport/destination_information/address/census_county_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_county_fips);
            $mmria.set_control_value('medical_transport/destination_information/address/census_tract_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_tract_fips);
            $mmria.set_control_value('medical_transport/destination_information/address/census_cbsa_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_fips);
            $mmria.set_control_value('medical_transport/destination_information/address/census_cbsa_micro', g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_micro);
            $mmria.set_control_value('medical_transport/destination_information/address/census_met_div_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_met_div_fips);
            $mmria.set_control_value('medical_transport/destination_information/address/urban_status', g_data.medical_transport[current_mt_index].destination_information.address.urban_status);
            $mmria.set_control_value('medical_transport/destination_information/address/state_county_fips', g_data.medical_transport[current_mt_index].destination_information.address.state_county_fips);
		}
		else
		{
			g_data.medical_transport[current_mt_index].destination_information.address.feature_matching_geography_type = 'Unmatchable';
			g_data.medical_transport[current_mt_index].destination_information.address.latitude = '';
			g_data.medical_transport[current_mt_index].destination_information.address.longitude = '';
			g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_code = '';
			g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_type = '';
			g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_code = '';
			g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_type = '';
			g_data.medical_transport[current_mt_index].destination_information.address.census_state_fips = '';
			g_data.medical_transport[current_mt_index].destination_information.address.census_county_fips = '';
			g_data.medical_transport[current_mt_index].destination_information.address.census_tract_fips = '';
			g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_fips = '';
			g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_micro = '';
			g_data.medical_transport[current_mt_index].destination_information.address.census_met_div_fips = '';
			g_data.medical_transport[current_mt_index].destination_information.address.urban_status = '';
			g_data.medical_transport[current_mt_index].destination_information.address.state_county_fips = '';
			$mmria.save_current_record();
			$mmria.set_control_value('medical_transport/destination_information/address/feature_matching_geography_type', g_data.medical_transport[current_mt_index].destination_information.address.feature_matching_geography_type);
			$mmria.set_control_value('medical_transport/destination_information/address/latitude', g_data.medical_transport[current_mt_index].destination_information.address.latitude);
			$mmria.set_control_value('medical_transport/destination_information/address/longitude', g_data.medical_transport[current_mt_index].destination_information.address.longitude);
			$mmria.set_control_value('medical_transport/destination_information/address/naaccr_gis_coordinate_quality_code', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_code);
			$mmria.set_control_value('medical_transport/destination_information/address/naaccr_gis_coordinate_quality_type', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_type);
			$mmria.set_control_value('medical_transport/destination_information/address/naaccr_census_tract_certainty_code', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_code);
			$mmria.set_control_value('medical_transport/destination_information/address/naaccr_census_tract_certainty_type', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_type);
			$mmria.set_control_value('medical_transport/destination_information/address/census_state_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_state_fips);
			$mmria.set_control_value('medical_transport/destination_information/address/census_county_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_county_fips);
			$mmria.set_control_value('medical_transport/destination_information/address/census_tract_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_tract_fips);
			$mmria.set_control_value('medical_transport/destination_information/address/census_cbsa_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_fips);
			$mmria.set_control_value('medical_transport/destination_information/address/census_cbsa_micro', g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_micro);
			$mmria.set_control_value('medical_transport/destination_information/address/census_met_div_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_met_div_fips);
			$mmria.set_control_value('medical_transport/destination_information/address/urban_status', g_data.medical_transport[current_mt_index].destination_information.address.urban_status);
			$mmria.set_control_value('medical_transport/destination_information/address/state_county_fips', g_data.medical_transport[current_mt_index].destination_information.address.state_county_fips);

		}
    });
}



/*
path=medical_transport/destination_information/address/get_coordinates_clear
event=onclick
*/
function medical_transport_destination_information_address_get_coordinates_clear(p_control) 
{
    var current_mt_index = $global.get_current_multiform_index();

    g_data.medical_transport[current_mt_index].destination_information.address.feature_matching_geography_type = '';
    g_data.medical_transport[current_mt_index].destination_information.address.latitude = '';
    g_data.medical_transport[current_mt_index].destination_information.address.longitude = '';
    g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_code = '';
    g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_type = '';
    g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_code = '';
    g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_type = '';
    g_data.medical_transport[current_mt_index].destination_information.address.census_state_fips = '';
    g_data.medical_transport[current_mt_index].destination_information.address.census_county_fips = '';
    g_data.medical_transport[current_mt_index].destination_information.address.census_tract_fips = '';
    g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_fips = '';
    g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_micro = '';
    g_data.medical_transport[current_mt_index].destination_information.address.census_met_div_fips = '';
	g_data.medical_transport[current_mt_index].destination_information.address.urban_status = '';
    g_data.medical_transport[current_mt_index].destination_information.address.state_county_fips = '';
    $mmria.save_current_record();
    $mmria.set_control_value('medical_transport/destination_information/address/feature_matching_geography_type', g_data.medical_transport[current_mt_index].destination_information.address.feature_matching_geography_type);
    $mmria.set_control_value('medical_transport/destination_information/address/latitude', g_data.medical_transport[current_mt_index].destination_information.address.latitude);
    $mmria.set_control_value('medical_transport/destination_information/address/longitude', g_data.medical_transport[current_mt_index].destination_information.address.longitude);
    $mmria.set_control_value('medical_transport/destination_information/address/naaccr_gis_coordinate_quality_code', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_code);
    $mmria.set_control_value('medical_transport/destination_information/address/naaccr_gis_coordinate_quality_type', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_gis_coordinate_quality_type);
    $mmria.set_control_value('medical_transport/destination_information/address/naaccr_census_tract_certainty_code', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_code);
    $mmria.set_control_value('medical_transport/destination_information/address/naaccr_census_tract_certainty_type', g_data.medical_transport[current_mt_index].destination_information.address.naaccr_census_tract_certainty_type);
    $mmria.set_control_value('medical_transport/destination_information/address/census_state_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_state_fips);
    $mmria.set_control_value('medical_transport/destination_information/address/census_county_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_county_fips);
    $mmria.set_control_value('medical_transport/destination_information/address/census_tract_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_tract_fips);
    $mmria.set_control_value('medical_transport/destination_information/address/census_cbsa_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_fips);
    $mmria.set_control_value('medical_transport/destination_information/address/census_cbsa_micro', g_data.medical_transport[current_mt_index].destination_information.address.census_cbsa_micro);
    $mmria.set_control_value('medical_transport/destination_information/address/census_met_div_fips', g_data.medical_transport[current_mt_index].destination_information.address.census_met_div_fips);
	$mmria.set_control_value('medical_transport/destination_information/address/urban_status', g_data.medical_transport[current_mt_index].destination_information.address.urban_status);
    $mmria.set_control_value('medical_transport/destination_information/address/state_county_fips', g_data.medical_transport[current_mt_index].destination_information.address.state_county_fips);
}


/*
path=medical_transport/destination_information/address/calculated_distance
event=onclick
*/
function medical_transport_destination_information_address_calculated_distance(p_control) 
{
    let dist = null;
    var current_mt_index = $global.get_current_multiform_index();
    let res_lat = parseFloat(g_data.medical_transport[current_mt_index].origin_information.address.latitude);
    let res_lon = parseFloat(g_data.medical_transport[current_mt_index].origin_information.address.longitude);
    let hos_lat = parseFloat(this.latitude);
    let hos_lon = parseFloat(this.longitude);
    if (res_lat >= -90 && res_lat <= 90 && res_lon >= -180 && res_lon <= 180 && hos_lat >= -90 && hos_lat <= 90 && hos_lon >= -180 && hos_lon <= 180) 
    {
        dist = $global.calc_distance(res_lat, res_lon, hos_lat, hos_lon);
        this.estimated_distance = dist;
        $mmria.save_current_record();
        $mmria.set_control_value('medical_transport/destination_information/address/estimated_distance', this.estimated_distance);
    }
}


/*
path=medical_transport/destination_information/address/clear_distance
event=onclick
*/
function medical_transport_destination_information_address_clear_distance(p_control) 
{
    this.estimated_distance = '';
    $mmria.save_current_record();
    $mmria.set_control_value('medical_transport/destination_information/address/estimated_distance', this.estimated_distance);

}
