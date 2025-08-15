function $case_status_confirm()
{
    g_target_case_status = null;
    g_is_confirm_for_case_lock = false;

    g_data.tracking.case_status.case_locked_date = new Date().toISOString().split("T")[0];
}


function $case_document_begin_edit()
{
    //console.log("begin edit");
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

//CALCULATE NUMBER OF DAYS BETWEEN 2 DATES
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

//CALCULATE NUMBER OF YEARS BETWEEN 2 DATES
function $calc_years
(
    p_start_date, 
    p_end_date
)
{
    var years = null;
    p_start_date = p_start_date.getTime() / 31557600000;
    p_end_date = p_end_date.getTime() / 31557600000;
    years = Math.trunc(p_end_date - p_start_date);
    return years;
}


//CALCULATE AGE IN YEARS (DOD - DOB)
function $update_age_difference
(
    start_month,
    start_day,
    start_year,
    end_month,
    end_day,
    end_year 
)
{
    let start_date = null
    let end_date = null

    if
    (
        start_month != null &&
        start_month != 9999 &&
        start_month != 99 &&
        start_day != null &&
        start_day != 9999 &&
        start_day != 99 &&
        start_year != null &&
        start_year != 9999 
    )
    {
        const new_date_string = `${start_year}-${start_month.toString().padStart(2, "0")}-${start_day.toString().padStart(2, "0")}`;
        const date_display = `${start_month.toString().padStart(2, "0")}/${start_day.toString().padStart(2, "0")}/${start_year}`;

        start_date = new Date(new_date_string);
    }
    else
    {
        // ;
    }

    if
    (
        end_month != null &&
        end_month != 9999 &&
        end_month != 99 &&
        end_day != null &&
        end_day != 9999 &&
        end_day != 99 &&
        end_year != null &&
        end_year != 9999 
    )
    {
        const new_date_string = `${end_year}-${end_month.toString().padStart(2, "0")}-${end_day.toString().padStart(2, "0")}`;
        const date_display = `${end_month.toString().padStart(2, "0")}/${end_day.toString().padStart(2, "0")}/${end_year}`;

        end_date = new Date(new_date_string);
    }
    else
    {
        // ;
    }

    if 
    (
        start_date != null && 
        end_date != null
    )
    {
        let age_difference = $global.calc_years
        (
            start_date,
            end_date
        );

        g_data.demographic.date_of_birth.agedif = age_difference;
        $mmria.set_control_value('demographic/date_of_birth/agedif', age_difference);
    }
    else
    {
        g_data.demographic.date_of_birth.agedif = '';
        $mmria.set_control_value('demographic/date_of_birth/agedif', '');
    }
}

//CALCULATE TERM IN DAYS (DOD - DTERM)
function $update_term_days
(
    start_month,
    start_day,
    start_year,
    end_month,
    end_day,
    end_year 
)
{
    let start_date = null
    let end_date = null

    if
    (
        start_month != null &&
        start_month != 9999 &&
        start_month != 99 &&
        start_day != null &&
        start_day != 9999 &&
        start_day != 99 &&
        start_year != null &&
        start_year != 9999 
    )
    {
        const new_date_string = `${start_year}-${start_month.toString().padStart(2, "0")}-${start_day.toString().padStart(2, "0")}`;
        const date_display = `${start_month.toString().padStart(2, "0")}/${start_day.toString().padStart(2, "0")}/${start_year}`;

        start_date = new Date(new_date_string);
    }
    else
    {
        // ;
    }

    if
    (
        end_month != null &&
        end_month != 9999 &&
        end_month != 99 &&
        end_day != null &&
        end_day != 9999 &&
        end_day != 99 &&
        end_year != null &&
        end_year != 9999 
    )
    {
        const new_date_string = `${end_year}-${end_month.toString().padStart(2, "0")}-${end_day.toString().padStart(2, "0")}`;
        const date_display = `${end_month.toString().padStart(2, "0")}/${end_day.toString().padStart(2, "0")}/${end_year}`;

        end_date = new Date(new_date_string);
    }
    else
    {
        // ;
    }

    if 
    (
        start_date != null && 
        end_date != null
    )
    {
        let term_days = $global.calc_days
        (
            start_date,
            end_date
        );
        
        g_data.outcome.dterm_grp.daydif = term_days;
        $mmria.set_control_value('outcome/dterm_grp/daydif', term_days);
    }
    else
    {
        g_data.outcome.dterm_grp.daydif = '';
        $mmria.set_control_value('outcome/dterm_grp/daydif', '');
    }
}

// UPDATE CONTROL: DOD
function $tracking_dod_update
(
    month,
    day,
    year
)
{
    if
    (
        month != null &&
        month != 9999 &&
        month != 99 &&
        day != null &&
        day != 9999 &&
        day != 99 &&
        year != null &&
        year != 9999 
    )
    {
        const new_date_string = `${year}-${month.toString().padStart(2, "0")}-${day.toString().padStart(2, "0")}`;
        const date_display = `${month.toString().padStart(2, "0")}/${day.toString().padStart(2, "0")}/${year}`;

        g_data.tracking.date_of_death.dod = new_date_string;
        $mmria.set_control_value('tracking/date_of_death/dod', date_display);
    }
    else
    {
        g_data.tracking.date_of_death.dod = '';
        $mmria.set_control_value('tracking/date_of_death/dod', '');
    }
}


/*
path=tracking/date_of_death/month
event=onchange
*/
function tracking_date_of_death_month_onchange(p_control) 
{
    const month = p_control.value;
    const day = g_data.tracking.date_of_death.day;
    const year = g_data.tracking.date_of_death.year;
    $global.tracking_dod_update
    (
        month,
        day,
        year
    );

    const start_month = g_data.demographic.date_of_birth.month;
    const start_day = g_data.demographic.date_of_birth.day;
    const start_year = g_data.demographic.date_of_birth.year;
    $global.update_age_difference
    (
        start_month,
        start_day,
        start_year,
        month,
        day,
        year 
    );

    const start2_month = g_data.outcome.dterm_grp.dterm_mo;
    const start2_day = g_data.outcome.dterm_grp.dterm_dy;
    const start2_year = g_data.outcome.dterm_grp.dterm_yr;
    $global.update_term_days
    (
        start2_month,
        start2_day,
        start2_year,
        month,
        day,
        year 
    );   
}

/*
path=tracking/date_of_death/day
event=onchange
*/
function tracking_date_of_death_day_onchange(p_control) 
{
    const month = g_data.tracking.date_of_death.month;
    const day = p_control.value;
    const year = g_data.tracking.date_of_death.year;
    $global.tracking_dod_update
    (
        month,
        day,
        year
    );

    const start_month = g_data.demographic.date_of_birth.month;
    const start_day = g_data.demographic.date_of_birth.day;
    const start_year = g_data.demographic.date_of_birth.year;
    $global.update_age_difference
    (
        start_month,
        start_day,
        start_year,
        month,
        day,
        year 
    );

    const start2_month = g_data.outcome.dterm_grp.dterm_mo;
    const start2_day = g_data.outcome.dterm_grp.dterm_dy;
    const start2_year = g_data.outcome.dterm_grp.dterm_yr;
    $global.update_term_days
    (
        start2_month,
        start2_day,
        start2_year,
        month,
        day,
        year 
    );     
}

/*
path=tracking/date_of_death/year
event=onchange
*/
function tracking_date_of_death_year_onchange(p_control) 
{
    const month = g_data.tracking.date_of_death.month;
    const day = g_data.tracking.date_of_death.day;
    const year = p_control.value;
    $global.tracking_dod_update
    (
        month,
        day,
        year
    );

    const start_month = g_data.demographic.date_of_birth.month;
    const start_day = g_data.demographic.date_of_birth.day;
    const start_year = g_data.demographic.date_of_birth.year;
    $global.update_age_difference
    (
        start_month,
        start_day,
        start_year,
        month,
        day,
        year 
    );

    const start2_month = g_data.outcome.dterm_grp.dterm_mo;
    const start2_day = g_data.outcome.dterm_grp.dterm_dy;
    const start2_year = g_data.outcome.dterm_grp.dterm_yr;
    $global.update_term_days
    (
        start2_month,
        start2_day,
        start2_year,
        month,
        day,
        year 
    );      
}


// UPDATE CONTROL: DOB
function $demographic_dob_update
(
    month,
    day,
    year
)
{
    if
    (
        month != null &&
        month != 9999 &&
        month != 99 &&
        day != null &&
        day != 9999 &&
        day != 99 &&
        year != null &&
        year != 9999 
    )
    {
        const new_date_string = `${year}-${month.toString().padStart(2, "0")}-${day.toString().padStart(2, "0")}`;
        const date_display = `${month.toString().padStart(2, "0")}/${day.toString().padStart(2, "0")}/${year}`;

        g_data.demographic.date_of_birth.dob = new_date_string;
        $mmria.set_control_value('demographic/date_of_birth/dob', date_display);
    }
    else
    {
        g_data.demographic.date_of_birth.dob = '';
        $mmria.set_control_value('demographic/date_of_birth/dob', '');
    }
}


/*
path=demographic/date_of_birth/month
event=onchange
*/
function demographic_date_of_birth_month_onchange(p_control) 
{
    const month = p_control.value;
    const day = g_data.demographic.date_of_birth.day;
    const year = g_data.demographic.date_of_birth.year;
    $global.demographic_dob_update
    (
        month,
        day,
        year
    );

    const end_month = g_data.tracking.date_of_death.month;
    const end_day = g_data.tracking.date_of_death.day;
    const end_year = g_data.tracking.date_of_death.year;
    $global.update_age_difference
    (
        month,
        day,
        year,
        end_month,
        end_day,
        end_year
    );
}


/*
path=demographic/date_of_birth/day
event=onchange
*/
function demographic_date_of_birth_day_onchange(p_control) 
{
    const month = g_data.demographic.date_of_birth.month;
    const day = p_control.value;
    const year = g_data.demographic.date_of_birth.year;
    $global.demographic_dob_update
    (
        month,
        day,
        year
    );

    const end_month = g_data.tracking.date_of_death.month;
    const end_day = g_data.tracking.date_of_death.day;
    const end_year = g_data.tracking.date_of_death.year;
    $global.update_age_difference
    (
        month,
        day,
        year,
        end_month,
        end_day,
        end_year
    );
}


/*
path=demographic/date_of_birth/year
event=onchange
*/
function demographic_date_of_birth_year_onchange(p_control) 
{
    const month = g_data.demographic.date_of_birth.month;
    const day = g_data.demographic.date_of_birth.day;
    const year = p_control.value;
    $global.demographic_dob_update
    (
        month,
        day,
        year
    );

    const end_month = g_data.tracking.date_of_death.month;
    const end_day = g_data.tracking.date_of_death.day;
    const end_year = g_data.tracking.date_of_death.year;
    $global.update_age_difference
    (
        month,
        day,
        year,
        end_month,
        end_day,
        end_year
    );
}


// UPDATE CONTROL: DTERM
function $outcome_dterm_update
(
    month,
    day,
    year
)
{
    if
    (
        month != null &&
        month != 9999 &&
        month != 99 &&
        day != null &&
        day != 9999 &&
        day != 99 &&
        year != null &&
        year != 9999 
    )
    {
        const new_date_string = `${year}-${month.toString().padStart(2, "0")}-${day.toString().padStart(2, "0")}`;
        const date_display = `${month.toString().padStart(2, "0")}/${day.toString().padStart(2, "0")}/${year}`;

        g_data.outcome.dterm_grp.dterm = new_date_string;
        $mmria.set_control_value('outcome/dterm_grp/dterm', date_display);
    }
    else
    {
        g_data.outcome.dterm_grp.dterm = '';
        $mmria.set_control_value('outcome/dterm_grp/dterm', '');
    }

}


/*
path=outcome/dterm_grp/dterm_mo
event=onchange
*/
function outcome_dterm_grp_dterm_mo_onchange(p_control) 
{
    const month = p_control.value;
    const day = g_data.outcome.dterm_grp.dterm_dy;
    const year = g_data.outcome.dterm_grp.dterm_yr;
    $global.outcome_dterm_update
    (
        month,
        day,
        year
    );

    const end_month = g_data.tracking.date_of_death.month;
    const end_day = g_data.tracking.date_of_death.day;
    const end_year = g_data.tracking.date_of_death.year;
    $global.update_term_days
    (
        month,
        day,
        year,
        end_month,
        end_day,
        end_year
    );
}


/*
path=outcome/dterm_grp/dterm_dy
event=onchange
*/
function outcome_dterm_grp_dterm_dy_onchange(p_control) 
{
    const month = g_data.outcome.dterm_grp.dterm_mo;
    const day = p_control.value;
    const year = g_data.outcome.dterm_grp.dterm_yr;
    $global.outcome_dterm_update
    (
        month,
        day,
        year
    );

    const end_month = g_data.tracking.date_of_death.month;
    const end_day = g_data.tracking.date_of_death.day;
    const end_year = g_data.tracking.date_of_death.year;
    $global.update_term_days
    (
        month,
        day,
        year,
        end_month,
        end_day,
        end_year
    );
}


/*
path=outcome/dterm_grp/dterm_yr
event=onchange
*/
function outcome_dterm_grp_dterm_yr_onchange(p_control) 
{
    const month = g_data.outcome.dterm_grp.dterm_mo;
    const day = g_data.outcome.dterm_grp.dterm_dy;
    const year = p_control.value;
    $global.outcome_dterm_update
    (
        month,
        day,
        year
    );

    const end_month = g_data.tracking.date_of_death.month;
    const end_day = g_data.tracking.date_of_death.day;
    const end_year = g_data.tracking.date_of_death.year;
    $global.update_term_days
    (
        month,
        day,
        year,
        end_month,
        end_day,
        end_year
    );
}


//CALCULATE BMI
function $update_bmi
(
    height,
    weight 
)
{
    let my_height = null
    let my_weight = null

    if
    (
        height != null &&
        height != '' &&
        height != 99  &&
        height != 999
    )
    {
        my_height = height;
    }
    else
    {
        // ;
    }

    if
    (
        weight != null &&
        weight != '' &&
        weight != 666  &&
        weight != 777 &&
        weight != 999 
    )
    {
        my_weight = weight;
    }
    else
    {
        // ;
    }

    if 
    (
        my_height != null && 
        my_weight != null
    )
    {
        let my_bmi = $global.calc_bmi
        (
            my_height,
            my_weight
        );
        
        g_data.demographic.bmi = my_bmi;
        $mmria.set_control_value('demographic/bmi', my_bmi);
    }
    else
    {
        g_data.demographic.bmi = '';
        $mmria.set_control_value('demographic/bmi', '');
    }
}

/*
path=demographic/height
event=onchange
*/
function  demographic_height_onchange(p_control) 
{
    const height = p_control.value;
    const weight = g_data.demographic.wtpreprg;
    $global.update_bmi
    (
        height,
        weight
    );
}


/*
path=demographic/wtpreprg
event=onchange
*/
function  demographic_wtpreprg_onchange(p_control) 
{
    const height = g_data.demographic.height;
    const weight = p_control.value;
    $global.update_bmi
    (
        height,
        weight
    );
}


/*
path=tracking/q1/amssno
event=onchange
*/
function  tracking_q1_amsssno_onchange(p_control) 
{
    // tracking/q1/amssrel
    let controlId = 'g_data_tracking_q1_amssrel_control';

    const my_value = p_control.value;
    if 
    (
        my_value != null && 
        my_value != "00000"
    )
    {
        // Show field: tracking/q1/amssrel
        // $('label[for=' + controlId + '], #' + controlId).show();
        $mmria.set_disable('tracking/q1/amssrel', null, null) 
    }
    else
    {
        g_data.tracking.q1.rel = 9999;
        $mmria.set_control_value('tracking/q1/amssrel', 9999);
        $mmria.set_enable('tracking/q1/amssrel', null, null) 
        // Hide field: tracking/death_certificate_number
        // $('label[for=' + controlId + '], #' + controlId).hide();    
    }

    /*
    let controlId = 'g_data_home_record_overall_assessment_of_timing_of_death_number_of_days_after_end_of_pregnancey_control';
    let value = parseInt($('#g_data_home_record_overall_assessment_of_timing_of_death_abstrator_assigned_status_control').find(':selected').val());
    if (value === 9999
        || value === 0
        || value === 1
        || value === 4
        || value === 88
    ) {
        $('label[for=' + controlId + '], #' + controlId).hide();    
    }
    else {
        $('label[for=' + controlId + '], #' + controlId).show();
    }
    */
}


/* 
path=vro_case_determination/vro_update/vro_resolution_status
event=onchange
*/
function  vro_case_determination_vro_update_vro_resolution_status_onchange(p_control) 
{
    const my_value = p_control.value;
    if 
    (
        my_value != null && 
        my_value != "9999" && 
        my_value != "STEVE: Pending VRO Investigation"
    )
    {
        g_data.tracking.admin_info.status = "STEVE: Updated by VRO";
        // $mmria.set_control_value('tracking/admin_info/status', "STEVE-Transfer: Updated by VRO");
    }
    else
    {

    }
}


/* 
path=tracking/admin_info/status
event=onchange
*/
function  tracking_admin_info_status_onchange(p_control) 
{
    const my_value = p_control.value;
    if 
    (
        my_value == "STEVE: Pending VRO Investigation, Re-review Requested by CDC"
    )
    {
        g_data.vro_case_determination.vro_update.vro_resolution_status = "Pending VRO Investigation";
        // $mmria.set_control_value('tracking/admin_info/vro_resolution_status_mirror', "Pending VRO Investigation");
    }
    else
    {

    }
}

// CALCULATE OMB RACE
function $calc_race_omb(p_white, p_black, p_amindalknat, p_asianindian, p_chinese, p_filipino, p_japanese, p_korean, p_vietnamese, p_otherasian, p_nativehawaiian, p_guamcham, p_samoan, p_otherpacific, p_other, p_notspecified) 
{
    let race_omb = 9999;

    let rW = 0;
    let rB = 0;
    let rA = 0;
    let rAiAn = 0;
    let rPI = 0;
    let rO = 0;
    let rNS = 0;
    
    if 
    (
        p_white == "N" && 
        p_black == "N" && 
        p_amindalknat == "N" && 
        p_asianindian == "N" && 
        p_chinese == "N" && 
        p_filipino == "N" && 
        p_japanese == "N" && 
        p_korean == "N" && 
        p_vietnamese == "N" && 
        p_otherasian == "N" && 
        p_nativehawaiian == "N" && 
        p_guamcham == "N" && 
        p_samoan == "N" && 
        p_otherpacific == "N" && 
        p_other == "N" && 
        p_notspecified == "N"
    ) 
    {
      rNS = 1;
    }
    else
    {
        if (p_notspecified == "Y")
        { 
            rNS = 1;
        }
        if (p_white == "Y") 
        {
            rW = 1;
        }
        if (p_black == "Y") 
        {
            rB = 1;
        }
        if (p_amindalknat == "Y")
        {
            rAiAn = 1;
        }
        if (p_other == "Y") 
        {
            rO = 1;
        }
      
        if (p_asianindian == "Y")
        {
            rA = 1;
        }
        if (p_chinese == "Y")
        {
            rA = 1;
        }
        if (p_filipino == "Y")
        {
            rA = 1;
        }
        if (p_japanese == "Y")
        {
            rA = 1;
        }
        if (p_korean == "Y")
        {
            rA = 1;
        }
        if (p_vietnamese == "Y")
        {
            rA = 1;
        }
        if (p_otherasian == "Y")
        {
            rA = 1;
        }
      
        if (p_nativehawaiian == "Y")
        {
            rPI = 1;
        }
        if (p_guamcham == "Y")
        {
            rPI = 1;
        }
        if (p_samoan == "Y")
        {
            rPI = 1;
        }
        if (p_otherpacific == "Y")
        {
            rPI = 1;
        }
    }
      
    if ((rW + rB + rAiAn + rA + rPI + rO + rNS) == 1)
    {
        if (rW = 1)
        {
            race_omb = 1;
        }
        else if (rB = 1)
        {
            race_omb = 2;
        }
        else if (rA = 1) 
        {
            race_omb = 3;
        }
        else if (rAiAn = 1)
        {
            race_omb = 4;
        }
        else if (rPI = 1)
        {
            race_omb = 5;
        }
        else if (rO = 1) 
        {
            race_omb = 8;
        }
        else if (rNS = 1)
        {
            race_omb = 9;
        }
    }
    
    else if ((rW + rB + rAiAn + Ra + rPI + rO) == 2)
    {
        race_omb = 6;
    }
    else if ((rW + rB + rAiAn + Ra + rPI + rO) > 2)
    {
        race_omb = 7;
    }
    else if (rNS = 1) 
    {
        race_omb = 9;
    }

    return race_omb;
}

//CALCULATE RACE OMB
function $update_race_omb
(
    race_white,
    race_black,
    race_amindalknat,
    race_asianindian,
    race_chinese,
    race_filipino,
    race_japanese,
    race_korean,
    race_vietnamese,
    race_otherasian,
    race_nativehawaiian,
    race_guamcham,
    race_samoan,
    race_otherpacific,
    race_other,
    race_notspecified
)
{
    let my_race_white = null
    let my_race_black = null
    let my_race_amindalknat = null
    let my_race_asianindian = null
    let my_race_chinese = null
    let my_race_filipino = null
    let my_race_japanese = null
    let my_race_korean = null
    let my_race_vietnamese = null
    let my_race_otherasian = null
    let my_race_nativehawaiian = null
    let my_race_guamcham = null
    let my_race_samoan = null
    let my_race_otherpacific = null
    let my_race_other = null
    let my_race_notspecified = null
	
	
	function GetValue(value)
	{
		let result = null;
		
		if
		(
		value != null &&
        value != '' &&
        value != "9999"
		)
		result = value;
		
		return result;
	}
	
    my_race_white = GetValue(race_white);
    my_race_black = GetValue(race_black);
    my_race_amindalknat = GetValue(race_amindalknat);
    my_race_asianindian = GetValue(race_asianindian);
    my_race_chinese = GetValue(race_chinese);
    my_race_filipino = GetValue(race_filipino);
    my_race_japanese = GetValue(race_japanese);
    my_race_korean = GetValue(race_korean);
    my_race_vietnamese = GetValue(race_vietnamese);
    my_race_otherasian = GetValue(race_otherasian);
    my_race_nativehawaiian = GetValue(race_nativehawaiian);
    my_race_guamcham = GetValue(race_guamcham);
    my_race_samoan = GetValue(race_samoan);
    my_race_otherpacific = GetValue(race_otherpacific);
    my_race_other = GetValue(race_other);
    my_race_notspecified = GetValue(race_notspecified);
    
    
    if 
    (
        my_race_white != null && 
        my_race_black != null && 
        my_race_amindalknat != null && 
        my_race_asianindian != null && 
        my_race_chinese != null && 
        my_race_filipino != null && 
        my_race_japanese != null && 
        my_race_korean != null && 
        my_race_vietnamese != null && 
        my_race_otherasian != null && 
        my_race_nativehawaiian != null && 
        my_race_guamcham != null && 
        my_race_samoan != null && 
        my_race_otherpacific != null && 
        my_race_other != null && 
        my_race_notspecified != null
    )

    {
        let my_race_omb = $global.calc_race_omb
        (
            my_race_white,
            my_race_black, 
            my_race_amindalknat, 
            my_race_asianindian, 
            my_race_chinese, 
            my_race_filipino, 
            my_race_japanese, 
            my_race_korean, 
            my_race_vietnamese, 
            my_race_otherasian, 
            my_race_nativehawaiian, 
            my_race_guamcham,
            my_race_samoan, 
            my_race_otherpacific, 
            my_race_other,
            my_race_notspecified,
        );
        
        g_data.demographic.q12.group.race_omb = my_race_omb;
        $mmria.set_control_value('demographic/q12/group/race_omb', my_race_omb);
    }
    else
    {
        g_data.demographic.q12.group.race_omb = 9999;  
        $mmria.set_control_value('demographic/q12/group/race_omb', 9999); 
    }
}

/*
path=demographic/q12/group/race_white
event=onchange
*/
function  demographic_q12_group_race_white_onchange(p_control) 
{
    const race_white = p_control.value;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_black
event=onchange
*/
function  demographic_q12_group_race_black_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = p_control.value;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_amindalknat
event=onchange
*/
function  demographic_q12_group_race_amindalknat_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = p_control.value;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_asianindian
event=onchange
*/
function  demographic_q12_group_race_asianindian_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = p_control.value;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_chinese
event=onchange
*/
function  demographic_q12_group_race_chinese_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = p_control.value;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_filipino
event=onchange
*/
function  demographic_q12_group_race_filipino_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = p_control.value;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_japanese
event=onchange
*/
function  demographic_q12_group_race_japanese_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = p_control.value;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_korean
event=onchange
*/
function  demographic_q12_group_race_korean_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = p_control.value;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_vietnamese
event=onchange
*/
function  demographic_q12_group_race_vietnamese_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = p_control.value;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_otherasian
event=onchange
*/
function  demographic_q12_group_race_otherasian_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = p_control.value;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_nativehawaiian
event=onchange
*/
function  demographic_q12_group_race_nativehawaiian_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = p_control.value;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_guamcham
event=onchange
*/
function  demographic_q12_group_race_guamcham_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = p_control.value;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_samoan
event=onchange
*/
function  demographic_q12_group_race_samoan_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = p_control.value;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_otherpacific
event=onchange
*/
function  demographic_q12_group_race_otherpacific_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = p_control.value;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_other
event=onchange
*/
function  demographic_q12_group_race_other_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = p_control.value;
    const race_notspecified = g_data.demographic.q12.group.race_notspecified;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}

/*
path=demographic/q12/group/race_notspecified
event=onchange
*/
function  demographic_q12_group_race_notspecified_onchange(p_control) 
{
    const race_white = g_data.demographic.q12.group.race_white;
    const race_black = g_data.demographic.q12.group.race_black;
    const race_amindalknat = g_data.demographic.q12.group.race_amindalknat;
    const race_asianindian = g_data.demographic.q12.group.race_asianindian;
    const race_chinese = g_data.demographic.q12.group.race_chinese;
    const race_filipino = g_data.demographic.q12.group.race_filipino;
    const race_japanese = g_data.demographic.q12.group.race_japanese;
    const race_korean = g_data.demographic.q12.group.race_korean;
    const race_vietnamese = g_data.demographic.q12.group.race_vietnamese;
    const race_otherasian = g_data.demographic.q12.group.race_otherasian;
    const race_nativehawaiian = g_data.demographic.q12.group.race_nativehawaiian;
    const race_guamcham = g_data.demographic.q12.group.race_guamcham;
    const race_samoan = g_data.demographic.q12.group.race_samoan;
    const race_otherpacific = g_data.demographic.q12.group.race_otherpacific;
    const race_other = g_data.demographic.q12.group.race_other;
    const race_notspecified = p_control.value;
    $global.update_race_omb
    (
        race_white,
        race_black,
        race_amindalknat,
        race_asianindian,
        race_chinese,
        race_filipino,
        race_japanese,
        race_korean,
        race_vietnamese,
        race_otherasian,
        race_nativehawaiian,
        race_guamcham,
        race_samoan,
        race_otherpacific,
        race_other,
        race_notspecified
    );
}


/*
path=tracking/admin_info/steve_transfer
event=onchange
*/
function disable_on_selected_item_list(p_control)
{

    const control_path = new Set();

    control_path.add('tracking/cdc_case_matching_results/preg_cb_match');
    control_path.add('tracking/cdc_case_matching_results/literal_cod_match');
    control_path.add('tracking/cdc_case_matching_results/icd10_match');
    control_path.add('tracking/cdc_case_matching_results/bc_match');
    control_path.add('tracking/cdc_case_matching_results/fdc_match');
    /*
        1 ije_dc
        1 ije_bc
        1 ije_fetaldc
        1 tracking/cdc_case_matching_results/preg_cb_match
        1 tracking/cdc_case_matching_results/literal_cod_match
        1 tracking/cdc_case_matching_results/icd10_match
        1 tracking/cdc_case_matching_results/bc_match
        1 tracking/cdc_case_matching_results/fdc_match

        set_enable: function(p_dictionary_path, p_form_index, p_grid_index)
        set_disable: function(p_dictionary_path, p_form_index, p_grid_index)

    */

    if(p_control.value == 1)
    {

        for(let dictionary_path of control_path)
        {
            $mmria.set_disable(dictionary_path, null, null) 
        }
    }
    else
    {
        for(let dictionary_path of control_path)
        {
            $mmria.set_enable(dictionary_path, null, null) 
        }
    }


}