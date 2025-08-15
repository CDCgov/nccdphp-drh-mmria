var indicator_map = new Map();

indicator_map.set
(
    1,
    {
        indicator_id : "mPregRelated",
        title:"Pregnancy-Relatedness",
        description:"Determined by Pregnancy-Relatedness entered on Committee Decisions form.",
        blank_field_id: "MPregRel5",

        chart_title:"Number of deaths by pregnancy-relatedness",
        chart_title_508:"Bar chart showing number of deaths by pregnancy relatedness.",
        x_axis_title:"Pregnancy-Relatedness",
        y_axis_title:"Number of deaths",

        table_title:"Number of deaths by pregnancy-relatedness",
        table_title_508:"Table showing number of deaths by pregnancy relatedness.",
        

        field_id_list : [
        { name: "MPregRel1", title: "Pregnancy-related" },
        { name: "MPregRel2", title: "Pregnancy-associated but not -related" },
        { name: "MPregRel3", title: "Pregnancy-associated but unable to determine pregnancy-relatedness" },
        { name: "MPregRel4", title: "Not pregnancy-associated or -related (false positive)" },
        { name: "MPregRel5", title: "(blank)" },
            ],
    }
);



indicator_map.set
(
    2,
    {
        indicator_id : "mTimingofDeath",
        title:"Timing of Death",
        description:"The timing of death is determined by calculating the length of time between the date of death on the Home Record and the date of delivery on the Birth/Fetal Death Certificate form. If any elements of either date are missing (month, day, or year), the abstractor-assigned timing of death fields on the Home Record are used to assign the timing. If timing of death is still missing, the pregnancy checkbox on the Death Certificate form is used to assign timing of death in relation to pregnancy.",
        blank_field_id: "MTimeD4",

        chart_title:"Number of deaths by timing of death in relation to pregnancy",
        chart_title_508:"Bar chart showing number of deaths by timing of death in relation to pregnancy.",

        x_axis_title:"Timing of death in relation to pregnancy",
        y_axis_title:"Number of deaths",
        
        table_title:"Timing of death in relation to pregnancy",
        table_title_508:"Table showing number of deaths by timing of death in relation to pregnancy.",

        field_id_list : [
        { name: "MTimeD1", title: "During pregnancy" },
        { name: "MTimeD2", title: "Within 42 days of pregnancy" },
        { name: "MTimeD3", title: "Within 43 days to 1 year of pregnancy" },
        { name: "MTimeD4", title: "(Blank)" },
        ],
    }
);



indicator_map.set
(
    3,
    {
        indicator_id : "mDeathsbyRaceEth",
        title:"Race/Ethnicity",
        description:"To be included in one of these categories, both the race and Hispanic origin variables must be completed on the Birth/Fetal Death Certificate or Death Certificate. Priority is given to data entered on the Birth/Fetal Death Certificate because it is more likely to be self-reported, and if that is missing or incomplete, race/ethnicity is ascertained from the Death Certificate.",
        blank_field_id: "MRaceEth20",

        chart_title:"Number of deaths by race/ethnicity",
        chart_title_508:"Bar chart showing number of deaths by race/ethnicity.",
        x_axis_title:"Race/ethnicity",
        y_axis_title:"Number of deaths",
        
        table_title:"Race/ethnicity",
        table_title_508:"Table showing number of deaths by race/ethnicity.",
        

        field_id_list : [

            { name: "MRaceEth5", title: "Non-Hispanic White" },
            { name: "MRaceEth4", title: "Non-Hispanic Black" },
            { name: "MRaceEth3", title: "Hispanic" },
            /*
            { name: "MRaceEth6", title: "American Indian / Alaska Native" },
            { name: "MRaceEth7", title: "Native Hawaiian" },
            { name: "MRaceEth8", title: "Guamanian or Chamorro" },
            { name: "MRaceEth9", title: "Samoan" },
            { name: "MRaceEth10", title: "Other Pacific Islander" },
            { name: "MRaceEth11", title: "Asian Indian" },
            { name: "MRaceEth12", title: "Filipino" },
            { name: "MRaceEth13", title: "Korean" },
            { name: "MRaceEth14", title: "Other Asian" },
            { name: "MRaceEth15", title: "Chinese" },
            { name: "MRaceEth16", title: "Japanese" },
            { name: "MRaceEth17", title: "Vietnamese" },*/
            { name: "MRaceEth18", title: "Other" },
            //{ name: "MRaceEth19", title: "Race Not Specified" },
            { name: "MRaceEth20", title: "(Blank)" },
        ],
    }
);


indicator_map.set
(
    4,
    {
        indicator_id : "mAgeatDeath",
        title:"Age",
        description:"Age is calculated using the date of death on the Home Record and the date of birth on the Death Certificate form. If this data is not available, age is calculated using the date of death on the Home Record and the date of mother’s birth from the Birth/Fetal Death Certificate- Parent Section form.",
        blank_field_id: "MAgeD8",


        chart_title:"Number of deaths by mother’s age at death",
        chart_title_508: "Bar chart showing number of deaths by age of mother at death in years.",
        x_axis_title:"Age of mother at death (years)",
        y_axis_title:"Number of deaths",
        
        table_title:"Age of mother at death (years)",
        table_title_508:"Table showing number of deaths by Age.",
        

        field_id_list : [
        { name: "MAgeD1", title: "< 20" },
        { name: "MAgeD2", title: "20-24" },
        { name: "MAgeD3", title: "25-29" },
        { name: "MAgeD4", title: "30-34" },
        { name: "MAgeD5", title: "35-39" },
        { name: "MAgeD6", title: "40-44" },
        { name: "MAgeD7", title: "45+" },
        { name: "MAgeD8", title: "(blank)" },
        
        ],
    }
);

indicator_map.set
(
    5,
    {
        indicator_id : "mEducation",
        title:"Education",
        description:"To be included in one of these categories, education must be completed on the Birth/Fetal Death Certificate or Death Certificate. Priority is given to data entered on the Birth/Fetal Death Certificate because it is self-reported, and if that is missing or incomplete, education level is pulled from the Death Certificate.",
        blank_field_id: "MEduc5",


        chart_title:"Number of deaths by mother’s educational attainment",
        chart_title_508:"Bar chart showing number of deaths by educational attainment of mother.",
        x_axis_title:"Educational attainment of mother",
        y_axis_title:"Number of deaths",
        
        table_title:"Educational attainment of mother",
        table_title_508:"Table showing number of deaths by Education.",
        

        field_id_list : [
        { name: "MEduc1", title: "High school diploma equivalent or less" },
        { name: "MEduc2", title: "Completed some college" },
        { name: "MEduc3", title: "Associate or bachelor degree" },
        { name: "MEduc4", title: "Completed advanced degree" },
        { name: "MEduc5", title: "(blank)" },
        ],
    }
);


indicator_map.set
(
    6,
    {
        indicator_id : "mDeathSubAbuseEvi",
        title:"Substance Use",
        description:"Evidence of substance use in prenatal records is determined by the question ‘Was There Evidence of Substance Use?’ on the Prenatal Records form; History of documented substance use is determined by the question ‘Was There Documented Substance Use?’ on the Social and Environmental Profile, and includes any documented substance use beyond just the prenatal period.",
        blank_field_id: "MEviSub3",

        chart_title:"Number of deaths with evidence of substance use in prenatal records",
        chart_title_508:"Bar chart showing number of deaths by evidence of substance use in prenatal records.",
        x_axis_title:"Evidence of substance use in prenatal records",
        y_axis_title:"Number of deaths",
        
        table_title:"Evidence of substance use in prenatal records",
        table_title_508:"Table chart showing number of deaths by evidence of substance use in prenatal records.",

        field_id_list : [
        { name: "MEviSub1", title: "Yes" },
        { name: "MEviSub2", title: "No" },
        { name: "MEviSub3", title: "(Blank)" },
        { name: "MEviSub4", title: "Unknown" },
        ],
    }
);

indicator_map.set
(
    6.2,
    {
        indicator_id : "mHxofSubAbu",
        title:"Substance Use",
        description:"",
        blank_field_id: "MHxSub3",

        chart_title:"History of documented substance use",
        chart_title_508:"Bar chart showing number of deaths by documented substance use.",
        
        x_axis_title:"Documented substance use",
        y_axis_title:"Number of deaths",
        

        table_title:"Documented substance use",
        table_title_508:"Table chart showing number of deaths by documented substance use.",

        field_id_list : [
        { name: "MHxSub1", title: "Yes" },
        { name: "MHxSub2", title: "No" },
        { name: "MHxSub3", title: "(Blank)" },
        { name: "MHxSub4", title: "Unknown" },
        ],
    }
);

indicator_map.set
(
    7,
    {
        indicator_id : "mSubstAutop",
        title:"Toxicology",
        description:"This graph is based on substances listed as toxicology results on the Autopsy form. The substances from the list were classified as Alcohol, Amphetamine, Benzodiazepines, Buprenorphine/Methadone, Cannabinoid, Cocaine, Opioid (excl Buprenorphine/Methadone), or Substance with Other Chemical Classification. Substances entered that were not on the dropdown, including metabolites of substances on the dropdown, are categorized as ‘Other’. This report does not take into account the specific substance levels noted in the toxicology report. Each category on the graph reflects the number of persons who had at least one substance in the category indicated.",
        blank_field_id: "MSubAuto8",

        chart_title:"Number of deaths by substances on toxicology results",
        chart_title_508: "Bar chart showing number of deaths by drug class.",

        
        x_axis_title:"Drug class",
        y_axis_title:"Number of deaths",
        
        table_title:"Drug class",

        field_id_list : [
        { name: "MSubAuto1", title: "Alcohol" },
        { name: "MSubAuto2", title: "Amphetamine" },
        { name: "MSubAuto3", title: "Benzodiazepine" },
        { name: "MSubAuto4", title: "Buprenorphine/Methadone" },
        { name: "MSubAuto9", title: "Cannabinoid" },
        { name: "MSubAuto5", title: "Cocaine" },
        { name: "MSubAuto6", title: "Opioid (excl Buprenorphine/Methadone)" },
        { name: "MSubAuto7", title: "Substance with other chemical classification" },
        { name: "MSubAuto8", title: "(blank)" },
        { name: "MSubAuto10", title: "Other" },

        ],
    }
);

indicator_map.set
(
    8,
    {
        indicator_id : "mDeathCause",
        title:"Committee Determinations",
        description:"This table is based on committee determination of factors surrounding the death from the first page of Committee Decisions form, including whether mental health conditions and/or substance use disorder contributed to the death, and whether the death was a suicide.",
        blank_field_id: "",
    
        chart_title:"Frequency of selected committee determinations on circumstances surrounding death",
        x_axis_title:"Committee determination of cause of death",
        y_axis_title:"Number of deaths",
        
        table_title:"Committee determinations", 
        table_title_508:"Table showing number of deaths by Cause of Death.",
        
    
        field_id_list : [
    /*
            { name: "MCauseD16", title: "Obesity - Yes" },
            { name: "MCauseD17", title: "Obesity - No" },
            { name: "MCauseD18", title: "Obesity - Probably" },
            { name: "MCauseD19", title: "Obesity - Unknown" },
            { name: "MCauseD20", title: "Obesity - (blank)" },
            { name: "MCauseD21", title: "Discrimination - Yes" },
            { name: "MCauseD22", title: "Discrimination - No" },
            { name: "MCauseD23", title: "Discrimination - Probably" },
            { name: "MCauseD24", title: "Discrimination - Unknown" },
            { name: "MCauseD25", title: "Discrimination - (blank)" },
            */
            { name: "MCauseD1", title: "Mental Health Conditions - Yes" },
            { name: "MCauseD2", title: "Mental Health Conditions - No" },
            { name: "MCauseD3", title: "Mental Health Conditions - Probably" },
            { name: "MCauseD4", title: "Mental Health Conditions - Unknown" },
            { name: "MCauseD5", title: "Mental Health Conditions - (blank)" },
            { name: "MCauseD6", title: "Substance Use Disorder - Yes" },
            { name: "MCauseD7", title: "Substance Use Disorder - No" },
            { name: "MCauseD8", title: "Substance Use Disorder - Probably" },
            { name: "MCauseD9", title: "Substance Use Disorder - Unknown" },
            { name: "MCauseD10", title: "Substance Use Disorder - (blank)" },
            { name: "MCauseD11", title: "Suicide - Yes" },
            { name: "MCauseD12", title: "Suicide - No" },
            { name: "MCauseD13", title: "Suicide - Probably" },
            { name: "MCauseD14", title: "Suicide - Unknown" },
            { name: "MCauseD15", title: "Suicide - (blank)" },
            /*{ name: "MCauseD26", title: "Homicide - Yes" },
            { name: "MCauseD27", title: "Homicide - No" },
            { name: "MCauseD28", title: "Homicide - Probably" },
            { name: "MCauseD29", title: "Homicide - Unknown" },
            { name: "MCauseD30", title: "Homicide - (blank)" },
            */
            //{ name: "MCauseD31", title: "MCauseD31" },
    
        ],

    }
);


indicator_map.set
(
    9,
    {
        indicator_id : "mMHTxTiming",
        title:"Treatment History",
        description:"Treatment history is determined from variables on the Mental Health Profile that ask if treatment for mental health conditions was received prior to, during, or after the most recent pregnancy. The mental health conditions represented here are documented by the abstractor and may include depression, anxiety disorder, bipolar disorder, psychotic disorder, substance use disorder, or ‘other’. Each timing category includes all persons with at least one indicated treatment in that time period.",
        blank_field_id: "MMHTx4",

        chart_title:"Documented mental health treatment (including substance use disorder)",
        chart_title_508:"Bar chart showing number of deaths by mental health treatment timing.",
        x_axis_title:"Mental health treatment timing",
        y_axis_title:"Number of deaths",

        table_title:"Mental health treatment timing",
        table_title_508:"Table showing number of deaths by Mental health treatment timing.",
        

        field_id_list : [
        { name: "MMHTx1", title: "Before most recent pregnancy" },
        { name: "MMHTx2", title: "During most recent pregnancy" },
        { name: "MMHTx3", title: "After most recent pregnancy" },
        { name: "MMHTx4", title: "(blank)" },
        ],
    }
);

indicator_map.set
(
    10,
    {
        indicator_id : "mHxofEmoStress",
        title: "Emotional Stress",
        description:"History of social and emotional stress is determined using the corresponding variable on the Social and Environmental Profile. Each person can have multiple stressors entered, and the graph reflects the number of persons with each stressor selected.",
        blank_field_id: "MEmoStress12",

        chart_title:"Number of deaths by presence of social or emotional stressor",
        chart_title_508:"Bar chart showing number of deaths by presence of social or emotional stressor.",
        x_axis_title:"Social or emotional stressor",
        y_axis_title:"Number of deaths",
        
        table_title:"Social or emotional stressor",
        table_title_508:"Table showing number of deaths by Emotional Stress.",
        
        field_id_list : [
        { name: "MEmoStress3", title: "Child Protective Services involvement" },
        { name: "MEmoStress9", title: "History of childhood trauma" },
        { name: "MEmoStress1", title: "History of domestic violence" },
        { name: "MEmoStress2", title: "History of psychiatric hospitalizations or treatment" },
        { name: "MEmoStress4", title: "History of substance use" },
        { name: "MEmoStress6", title: "History of substance use treatment" },
        { name: "MEmoStress7", title: "Pregnancy unwanted" },
        { name: "MEmoStress10", title: "Prior suicide attempts" },
        

        { name: "MEmoStress8", title: "Recent trauma" },
        { name: "MEmoStress5", title: "Unemployment" },
       
        { name: "MEmoStress11", title: "Other" },
        //{ name: "MEmoStress12", title: "Other" },
        { name: "MEmoStress13", title: "Unknown" },
        { name: "MEmoStress14", title: "None" },
        ],
    }
);

indicator_map.set(
    11,
    {
        indicator_id : "mLivingArrange",
        title:"Living Arrangements",
        description:"Living arrangements at time of death and history of homelessness are determined using the corresponding variables on the Social and Environmental Profile. Each person can be placed into only one category for the mother’s living arrangements at time of death. Regarding homelessness, each person can have multiple time periods entered, and the graph reflects the number of persons who experienced homelessness in relation to pregnancy at that time period.",
        blank_field_id: "MLivD7",

        chart_title:"Mother&apos;s living arrangements at time of death",
        chart_title_508:"Bar chart showing mothers living arrangements at time of death.",
        
        x_axis_title:"Living arrangements at time of death",
        y_axis_title:"Number of deaths",
        
        table_title:"Mother&apos;s living arrangements at time of death",
        table_title_508:"Table showing mothers living arrangements at time of death.",

        field_id_list : [
        { name: "MLivD1", title: "Own" },
        { name: "MLivD2", title: "Rent" },
        { name: "MLivD3", title: "Public housing" },
        { name: "MLivD4", title: "Live with relative" },
        { name: "MLivD5", title: "Homeless" },
        { name: "MLivD6", title: "Other or Unknown" },
        { name: "MLivD7", title: "(blank)" },
        ],
    }
);

indicator_map.set
(
    11.2,
    {
        indicator_id : "mHomeless",
        title:"Living Arrangements",
        description:"",
        blank_field_id: "MHomeless5",

        chart_title:"Mother&apos;s experiences of homelessness in relation to pregnancy",
        chart_title_508:"Bar chart showing number of deaths by mothers experiences of homelessness in relation to pregnancy.",

        x_axis_title:"Homelessness in relation to pregnancy",
        y_axis_title:"Number of deaths",


        table_title:"Mother&apos;s experiences of homelessness in relation to pregnancy",
        table_title_508:"Table showing number of deaths by mothers experiences of homelessness in relation to pregnancy.",

        field_id_list : [
        { name: "MHomeless6", title: "More than 1 year prior to pregnancy" },
        { name: "MHomeless7", title: "Within 1 year prior to pregnancy" },
        { name: "MHomeless8", title: "During pregnancy" },
        { name: "MHomeless9", title: "After pregnancy" },
        { name: "MHomeless2", title: "In last 12 months (obsolete)" },
        { name: "MHomeless3", title: "More than 12 months ago (obsolete)" },
        { name: "MHomeless1", title: "Never experienced homelessness" },
        { name: "MHomeless4", title: "Unknown" },
        { name: "MHomeless5", title: "(blank)" },

        ],
    }
);

indicator_map.set
(
    12,
    {
        indicator_id : "mIncarHx",
        title:"Incarceration History",
        description: "Incarceration history is determined using the corresponding variable on the Social and Environmental Profile that asks, 'Was decedent ever incarcerated?’ Each person can have multiple time periods entered, and the graph reflects the number of persons incarcerated at that time period.",
        blank_field_id: "MHxIncar8",

        chart_title:"Number of deaths by mother&apos;s incarceration history in relation to pregnancy",
        chart_title_508:"Bar chart showing number of deaths by incarceration history of mother.",
        x_axis_title:" Incarceration history of mother",
        y_axis_title:"Number of deaths",
        
        table_title:" Incarceration history of mother",
        table_title_508:"Table showing number of deaths by Incarceration History.",

        field_id_list : [
        { name: "MHxIncar1", title: "Never incarcerated" },
        { name: "MHxIncar2", title: "More than 1 year prior to pregnancy" },
        { name: "MHxIncar3", title: "Within 1 year prior to pregnancy" },
        { name: "MHxIncar4", title: "During current pregnancy" },
        { name: "MHxIncar5", title: "After current pregnancy" },
        { name: "MHxIncar6", title: "Before current pregnancy (obsolete)" },
        { name: "MHxIncar7", title: "Unknown" },
        { name: "MHxIncar8", title: "(blank)" },
        ],
    }
);



async function get_indicator_values(p_indicator_id)
{
    const get_data_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/measure-indicator/${p_indicator_id}`
    });

    g_data = { total: 0, data: []};

    for(let i = 0; i < get_data_response.length; i++)
    {
        const item = get_data_response[i];
        if(Can_Pass_Filter(item))
        {
            g_data.data.push(item);
            g_data.total +=1;
        }
        else
        {
            //console.log("here");
        }
    }
    
    return g_data;
}

function Can_Pass_Filter(p_value)
{

    let pregnancy_relatedness = true;
    let date_of_review_begin = true;
    let date_of_review_end = true;
    let date_of_death_begin = true;
    let date_of_death_end = true;

    const Overdose_Poisioning = 3;
    const is_overdose = p_value.means_of_fatal_injury == Overdose_Poisioning;
    /*
    {
        "case_id": "0190e93e-a359-441e-b27d-43f83d946de4",
        "host_state": "test",
        "means_of_fatal_injury": 3,
        "year_of_death": 2009,
        "month_of_death": 9,
        "day_of_death": 22,
        "case_review_year": 2011,
        "case_review_month": 9,
        "case_review_day": 30,
        "pregnancy_related": 2,
        "indicator_id": "mPregRelated",
        "field_id": "MPregRel3",
        "value": 1,
        "jurisdiction_id": "/"
    }*/


    date_of_review_begin = is_greater_than_date
    (
        p_value.case_review_year,
        p_value.case_review_month,
        p_value.case_review_day,
        g_filter.date_of_review.begin
    )

    date_of_review_end = is_less_than_date
    (
        p_value.case_review_year,
        p_value.case_review_month,
        p_value.case_review_day,
        g_filter.date_of_review.end
    )

    date_of_death_begin = is_greater_than_date
    (
        p_value.year_of_death,
        p_value.month_of_death,
        p_value.day_of_death,
        g_filter.date_of_death.begin
    )

    date_of_death_end = is_less_than_date
    (
        p_value.year_of_death,
        p_value.month_of_death,
        p_value.day_of_death,
        g_filter.date_of_death.end
    )
/*
    g_filter.pregnancy_relatedness
    g_filter.date_of_review.begin
    g_filter.date_of_review.end
    g_filter.date_of_death.begin
    g_filter.date_of_death.end
    */
    if(g_filter.pregnancy_relatedness.length == 4)
    {
        pregnancy_relatedness = true;
    }
    else
    {
        pregnancy_relatedness = g_filter.pregnancy_relatedness.indexOf(p_value.pregnancy_related) > -1;
    }

    return pregnancy_relatedness && 
        date_of_review_begin && 
        date_of_review_end && 
        date_of_death_begin && 
        date_of_death_end &&
        is_overdose;

}

function is_greater_than_date
(
    p_year,
    p_month,
    p_day,
    p_date
)
{
    let is_year = true;
    let is_month = true;
    let is_day = true;

    let year = 9999;
    let month = 9999;
    let day = 9999;

    if
    (
        p_year != null 
    )
    {
        year = p_year;
    }

    if
    (
        p_month != null
    )
    {
        month = p_month;
    }

    if
    (
        p_day != null
    )
    {
        day = p_day;
    }

    const filter_year = p_date.getFullYear();
    const filter_month = p_date.getMonth() + 1;
    const filter_day = p_date.getDate();

    if(year == 9999) return true;

    if(year != 9999)
    {
        if(year > filter_year)
        {
            return true;
        }
        is_year = year == filter_year;
    }

    if(is_year && month != 9999)
    {
        if(month > filter_month)
        {
            return true;
        }
        
        is_month = month == filter_month;
    }

    if(is_year && is_month && day != 9999)
    {
        is_day = day >= filter_day;
    }

    return is_year && is_month && is_day;
}

function is_less_than_date
(
    p_year,
    p_month,
    p_day,
    p_date
)
{
    let is_year = true;
    let is_month = true;
    let is_day = true;

    let year = 9999;
    let month = 9999;
    let day = 9999;

    if
    (
        p_year != null 
    )
    {
        year = p_year;
    }

    if
    (
        p_month != null
    )
    {
        month = p_month;
    }

    if
    (
        p_day != null
    )
    {
        day = p_day;
    }

    const filter_year = p_date.getFullYear();
    const filter_month = p_date.getMonth() + 1;
    const filter_day = p_date.getDate();


    if(year == 9999) return true;

    if(year != 9999)
    {
        if(year < filter_year)
        {
            return true;
        }

        is_year = year == filter_year;
    }

    if(is_year && month != 9999)
    {
        if(month < filter_month)
        {
            return true;
        }

        is_month = month == filter_month;
    }

    if(is_year && is_month && day != 9999)
    {
        is_day = day <= filter_day;
    }

    return is_year && is_month && is_day;
}