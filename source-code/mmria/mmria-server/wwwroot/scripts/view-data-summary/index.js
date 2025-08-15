'use strict';

var g_metadata = null;
var g_metadata_set = {};
var g_release_version_name = null;
var g_release_version_specification = null;
var g_selected_version_specification = null;
var g_selected_version_name = null;
var g_version_list = null;


var g_value_to_display_lookup = {};
var g_name_to_value_lookup = {};
var g_display_to_value_lookup = {};
var g_value_to_index_number_lookup = {};
var g_look_up = {};

const multiform_grid_set = new Set();
const multiform_set = new Set();

const g_report_stat_map = new Map();
const g_report_map = new Map();
const g_path_to_stat_type = new Map();

const g_form_field_map = new Map();


const g_path_to_value_map = new Map();
const g_path_and_value_to_display = new Map();

var g_data_is_loaded = false;


var g_filter = {
    date_of_death: {
      year: ['all'],
      month: ['all'],
      day: ['all'],
    },
    date_range: [
      {
        from: 'all',
        to: 'all',
      },
    ],
    selected_record_id: null,
    field_selection: new Set(['all']),
    case_status: ['all'],
    case_jurisdiction: ['/'],
    pregnancy_relatedness: ['all'],
    selected_form: '',
    search_text: '',
    include_blank_date_of_reviews :true,
    include_blank_date_of_deaths: true,
    do_not_display_frequencies_equal_to_zero: true,
      date_of_review: { begin: new Date(1900,0,1), end: new Date() },
      date_of_death: { begin: new Date(1900,0,1), end: new Date() }
  };

var g_case_view_request = {
    total_rows: 0,
    page: 1,
    skip: 0,
    take: 1000,
    sort: 'date_last_updated',
    search_key: null,
    descending: true,
    case_status: "all",
      field_selection: ["all"],
      pregnancy_relatedness:"all",
    get_query_string: function () {
      var result = [];
      result.push('?skip=' + (this.page - 1) * this.take);
      result.push('take=' + this.take);
      result.push('sort=' + this.sort);
      result.push('case_status=' + this.case_status);
        result.push('field_selection=' + Array.from(this.field_selection));
        result.push('pregnancy_relatedness=' + this.pregnancy_relatedness);
  
        
        if(g_filter.include_blank_date_of_reviews == false)
        {
          result.push(`date_of_review_range=${ControlFormatDate(g_filter.date_of_review.begin)}T${ControlFormatDate(g_filter.date_of_review.end)}`);
        }
        else
        {
          result.push('date_of_review_range=All');
        }
  
  
        
        if(g_filter.include_blank_date_of_deaths == false)
        {
          result.push(`date_of_death_range=${ControlFormatDate(g_filter.date_of_death.begin)}T${ControlFormatDate(g_filter.date_of_death.end)}`);
        }
        else
        {
          result.push('date_of_death_range=All');
        }
  
      if (this.search_key) {
        result.push(
          'search_key=' +
          encodeURI(this.search_key) +
            ''
        );
      }
  
      result.push('descending=' + this.descending);
  
      return result.join('&');
    },
  };


window.onload = main;


async function main()
{
    

	let response = await $.ajax({
		url: `${location.protocol}//${location.host}/api/version/release-version`
	});
    
    
    g_release_version_name = response;
    g_selected_version_name = g_release_version_name;
	
	response = await $.ajax({
		url: `${location.protocol}//${location.host}/api/metadata/version_specification-${g_release_version_name}`
	});

    g_metadata = JSON.parse(response.metadata);
    g_release_version_specification = response;

   

    set_list_lookup
    (
      g_display_to_value_lookup,
      g_value_to_display_lookup,
      g_value_to_index_number_lookup,
      g_metadata,
      '',
      'g_metadata'
    );

    for (let i in g_metadata.lookup) 
    {
        const child = g_metadata.lookup[i];

        g_look_up['lookup/' + child.name] = child.values;
    }


    document.getElementById('form_content_id').innerHTML = dictionary_render(g_metadata, "").join("")  + '<br/>';
    set_form_enable(false);
	$('.spinner-content').removeClass('spinner-active');

    window.setTimeout(get_all_report_data,0);
}

async function load_metadata(p_version_id)
{
    let result;

    result = await $.ajax
    (
        {
            url: `${location.protocol}//${location.host}/api/version/${p_version_id}/metadata`,
        }
    );

    return result;
}


function render_metadata_version_filter()
{
    let html_result = []

    html_result.push(`<option value="">(Select Version)</option>`)
    for(let i = 0; i < g_version_list.length; i++)
    {
        let item = g_version_list[i];
        let is_selected = "";

        if(item.name == g_selected_version_name)
        {
            is_selected = "selected=true"
        }
        
        if(item._id.indexOf("_design/auth") < 0 && item.name != null)
        {
          html_result.push(`<option value="${item._id}" ${is_selected}>${item.name}</option>`)
        }
    }


    return html_result.join("");

}

function search_text_change(p_value)
{
	g_filter.search_text = p_value;

    show_needs_apply_id(true);
}

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


let total_rows = 0;
const id_list = [];
const data_list = [];

async function get_all_report_data()
{
    $('.spinner-content').addClass('spinner-active');
//http://localhost:5984/report/_design/data_summary_view_report/_view/year_of_death?skip=10000&limit=100
/*
{"total_rows":1558,"offset":1558,"rows":[

]}
*/ 
    const result = await $.ajax
    (
        {
            url: `${location.protocol}//${location.host}/api/data-summary/0`,
        }
    );

    const total_rows = result.total_rows;
    let current_total = result.rows.length;
    let current_page = 1;

    result.rows.forEach(element => {
        if(element.value._id == null)
        {
            element.value._id = element.id;
        }
        data_list.push(element.value);
    });
    data_list

    while
    (
        data_list.length < total_rows
    )
    {
        const page_result = await get_report_data_page(current_page * 100);
        if(page_result.rows.length == 0)
        {
            break;
        }

        page_result.rows.forEach(element => {

            if(element.value._id == null)
            {
                element.value._id = element.id;
            }
        
            data_list.push(element.value);
        });
        current_page += 1;
    }

    //console.log("get_all_report_data fin.");

    g_data_is_loaded = true;


    await build_report();

    $('.spinner-content').removeClass('spinner-active');
    //return result;
}

function set_zero()
{
    for(const td of document.querySelectorAll('td'))
    {
        
        if(td.id == null || td.id.trim() == '') continue;

        const tr = document.getElementById("tr-" + td.id)

        if(tr == null) continue;

        if(g_filter.do_not_display_frequencies_equal_to_zero)
        {
            if(td.innerText == '0')
            {
                tr.style.display = 'none';
            }
            else
            {
                tr.style.display = 'table-row';
            }             
        }
        else
        { 
            tr.style.display = 'table-row';
           
        }

 


    }

    set_form_enable(true);

    $('.spinner-content').removeClass('spinner-active');
}

async function get_report_data_page(p_skip = 0)
{
    const result = await $.ajax
    (
        {
            url: `${location.protocol}//${location.host}/api/data-summary/${p_skip}`,
        }
    );

    return result;
}

function build_report()
{
    let salt = 0;
    g_report_stat_map.clear();
    g_report_map.clear();
    // public Dictionary<string, List<Detail>> path_to_detail { get; set; }
    data_list.forEach(item => {
        if(Can_Pass_Filter(item))
        {
            //g_report_data_set.add(item);

            const detail = item.path_to_detail;

            for(const sp of Object.keys(detail))
            {
                const s = sp.trim();

                if(!g_report_map.has(s))
                {
                    g_report_map.set(s, new Map());
                }

                if(!g_path_to_value_map.has(s))
                {
                    g_path_to_value_map.set(s, new Map());
                }

                if(!g_report_stat_map.has(s))
                {
                    g_report_stat_map.set(s, new Map());
                    g_report_stat_map.get(s).set("type", g_path_to_stat_type.get(s));
                    g_report_stat_map.get(s).set("count",0);
                    g_report_stat_map.get(s).set("total",0);
                    g_report_stat_map.get(s).set("missing",0);
                    g_report_stat_map.get(s).set("min",0);
                    g_report_stat_map.get(s).set("max",0);
                    g_report_stat_map.get(s).set("mode",0);
                    g_report_stat_map.get(s).set("median",0);
                    g_report_stat_map.get(s).set("std_dev",0);// square root of variance
                    g_report_stat_map.get(s).set("variance",0);

                }

                const detail_item = detail[s];
                const list_lookup = g_value_to_display_lookup["/" + s];
                
                for(const v of Object.keys(detail_item))
                {

                    /*
                    if
                    (
                        s == 'home_record/how_was_this_death_identified' && 
                        detail_item[v].value != null && detail_item[v].value.toString().length > 4
                    )
                    {
                        continue;
                    }
                    */

                    const detail_item_value = detail_item[v].value == null ||  detail_item[v].value.toString().trim().length == 0  ? "(-)" : detail_item[v].value.trim().toLowerCase();

                    if(!g_report_map.get(s).has(detail_item_value))
                    {
                        g_report_map.get(s).set(detail_item_value, 0);
                    }


                    if(!g_path_to_value_map.get(s).has(detail_item_value))
                    {
                        g_path_to_value_map.get(s).set(detail_item_value, new Set());
                    }

                    const current_entry_value = g_report_map.get(s).get(detail_item_value);
                    const detail_entry_count = detail_item[v].count;
                    g_report_map.get(s).set(detail_item_value, current_entry_value + detail_entry_count);

                    const current_total = g_report_stat_map.get(s).get("count");

                    /*if
                    (
                        detail_item_value != "(-)" 
                    )
                    {*/
                       g_report_stat_map.get(s).set("count", current_total + detail_entry_count); 
                    //}
                    

                    if(g_path_to_value_map.get(s).get(detail_item_value).has(item.record_id))
                    {
                        //console.log(`duplicate record_id: ${item.record_id} id:${item._id.replace("freq-", "")} for ${s}`);
                        const new_key = `${item.record_id} - ${item._id.replace("freq-", "")} salt:${salt}`
                        g_path_to_value_map.get(s).get(detail_item_value).add(new_key);
                        salt+=1;
                    }
                    else
                    {
                        g_path_to_value_map.get(s).get(detail_item_value).add(item.record_id);
                    }
                    
                    
                        
                        
                    
                    //if(entry_count > 0)
                    //{
                        
                    //}


                }

            }

            //console.log("here");
            
        }
        else
        {
            //console.log("here");
        }

    });

    

    for(const [k, v] of g_report_map)
    {
        

        const type = g_report_stat_map.get(k).get("type");
        let max = 0;
        let mode = 0;
        let mode_value = "";
        let max_value = "";
        let min = 99999999;
        let min_value = "";


        

        if(type == "STAT_D")
        {
            max = Date.parse("1900-01-01");
            min =  Date.parse("3000-01-01");
        }


        for(const [k2, v2] of v)
        {

            if
            (
                k2 == "(-)" ||
                k2.trim().length == 0
            )
            {
                g_report_stat_map.get(k).set("missing", v2 + g_report_stat_map.get(k).get("missing"));
            }
            else if(type == "STAT_N")
            {
                const c1 = parseFloat(k2)
                if(c1 < min) 
                {
                    min = c1;
                    min_value = k2;
                }

                if(c1 > max) 
                {
                    max = c1;
                    max_value = k2;
                }

                if(v2 > mode)
                {
                    mode = v2;
                    mode_value = k2;
                }


                const entry = g_report_stat_map.get(k).get("total");

                g_report_stat_map.get(k).set("total", entry + c1 * v2);
                
            }
            else if(type == "STAT_D")
            {
                const c1 = Date.parse(k2);
                if(c1 < min) 
                {
                    min = c1;
                    min_value = k2;
                }

                if(c1 > max) 
                {
                    max = c1;
                    max_value = k2;
                }

                if(v2 > mode)
                {
                    mode = v2;
                    mode_value = k2;
                }
            }

            
        }

        g_report_stat_map.get(k).set("min", `${min_value} @${min}`);
        g_report_stat_map.get(k).set("max", `${max_value} @${max}`);
        g_report_stat_map.get(k).set("mode", `${mode_value} @${mode}`);


        g_path_to_value_map.get(k).set("min", new Set());
        g_path_to_value_map.get(k).set("max", new Set());
        g_path_to_value_map.get(k).set("missing", new Set());
        g_path_to_value_map.get(k).set("count", new Set());
       
        const list_lookup = g_value_to_display_lookup["/" + k];
        for(const data_item of data_list)
        {
            const compare_data_item = data_item.path_to_detail[k];
            if(compare_data_item != null)
            {
                  
                if(Array.isArray(compare_data_item))
                {
                    compare_data_item.forEach(element => {


                        if
                        (
                            element.count > 0 && 
                            element.value == min_value
                        )
                        {
                            g_path_to_value_map.get(k).get("min").add(data_item.record_id)
                        }

                        if
                        (
                            element.count > 0 && 
                            element.value == max_value
                        )
                        {
                            g_path_to_value_map.get(k).get("max").add(data_item.record_id)
                        }

                        if
                        (
                            element.count == 0 ||
                            element.value == "(-)" ||
                            (list_lookup != undefined && element.value == "9999") ||
                            element.value.trim().length == 0
                        )
                        {
                            g_path_to_value_map.get(k).get("missing").add(data_item.record_id)
                        }
                        else
                        {
                            if
                            (
                                data_item.record_id == undefined ||
                                data_item.record_id == null ||
                                data_item.record_id.length == 0
                            )
                            {
                                g_path_to_value_map.get(k).get("count").add(data_item._id)
                            }
                            else
                            {
                                g_path_to_value_map.get(k).get("count").add(data_item.record_id)
                            }
                            
                        }
                    });

                }
                else
                { 
                    console.log("This should NOT happen.")
                }
                
            }
            
        }


        
        
        function stat_n_sort(a, b) 
        {
            var a_is_missing = false;
            var b_is_missing = false;

            if
            (
                a[0] == "(-)" ||
                a[0].trim().length == 0
            )
            a_is_missing = true;


            if
            (
                b[0] == "(-)" ||
                b[0].trim().length == 0
            )
            b_is_missing = true;

            if(a_is_missing && b_is_missing)
                return 0;

            if(a_is_missing && !b_is_missing)
                return -1;

            if(!a_is_missing && b_is_missing)
                return 1;
    
            return parseFloat(a[0]) -parseFloat(b[0]);
            
        }


        if(type == "STAT_N")
        {
            const count = g_report_stat_map.get(k).get("count") - g_report_stat_map.get(k).get("missing");
            const mid_count = count / 2;

            let median = "";
            var mapAsc = new Map([...g_report_map.get(k).entries()].sort(stat_n_sort));
            const array = [];
            let sum = 0;
            let mid_1_entry;
            let mid_2_entry;
            let need_mid_2 = false;
            for(const [k3, v3] of mapAsc)
            {
                if(need_mid_2)
                {
                    mid_2_entry = parseFloat(k2)
                    median = (mid_1_entry + mid_2_entry) / 2;
                    break;
                }

                if
                (
                    k3 == "(-)" || 
                    k3.trim().length == 0
                )
                {
                    continue;
                }
                else if(sum + v3 >= mid_count)
                {

                    mid_1_entry = parseFloat(k3);
                    
                    
                    if(count % 2 == 0)
                    {
                        if((sum + v3) - mid_count >=0)
                        {
                            median = mid_1_entry;
                        }
                        else
                        {
                            need_mid_2 = true;
                            continue;
                        }
                    }
                    else
                    {
                        median = mid_1_entry;
                    }
                    //console.log("here");
                    break;

                    
                }

                sum += v3;
                array.push(k3);
            }

 

            g_report_stat_map.get(k).set("median", median);
            
        }

    }

    for(const [k, l] of g_report_map)
    {
        if(g_report_stat_map.get(k).get("type") != "STAT_N") continue;

        g_report_stat_map.get(k).set("mean", g_report_stat_map.get(k).get("total") / (g_report_stat_map.get(k).get("count") - g_report_stat_map.get(k).get("missing")))
        
        const mean = g_report_stat_map.get(k).get("mean"); 

        let observation_number = 0;
        let sum = 0;

        for(const [n, v] of l)
        {
            if
            (
                n== "(-)" ||
                n.trim().length == 0
            )
            continue;
            const value = parseFloat(n);

            observation_number += v;

            for(var sum_count = 0; sum_count < v; sum_count++)
                sum += (value - mean) * (value - mean);
        }

        g_report_stat_map.get(k).set("variance", sum / observation_number);
        g_report_stat_map.get(k).set("std_dev", Math.sqrt(sum / observation_number));

    }

    for(const [k, v] of g_report_map)
    {

        const current_stat = g_report_stat_map.get(k);
        const type = current_stat.get("type");

        if(type == "FREQ")
        {
            const el = document.getElementById(`${k}-count`);

            if(el != null)
            {
                //el.innerHTML = current_stat.get("count");
                el.innerHTML = render_link
                (
                    k,
                    "count",
                    current_stat.get("count"),
                    "Frequency Distribution"
                );

                for(const [n, v2] of v)
                {
                    if(n == "(-)")
                    {
                        const el2 = document.getElementById(`${k}-missing`);
                        if(el2 != null)
                            /*
                        el2.innerHTML = render_link
                            (
                                k,
                                n,
                                parseInt(el2.innerHTML) + v2
                            );*/
                            el2.innerHTML = render_link
                            (
                                k,
                                "missing",
                                current_stat.get("missing"),
                                "Missing List Values"
                            );

                        const el3 = document.getElementById(`${k}-missing-detail`);
                        if(el3 != null)
                        {
                            el3.innerHTML = render_link
                            (
                                k,
                                "missing",
                                current_stat.get("missing"),
                                "Missing Date Summary"
                            );
                        }   

                    }
                    else
                    {
                        if(Array.isArray(n))
                        {
                            n.forEach(element => {
                                const el2 = document.getElementById(`${k}-${element}`);
                                if(el2 != null)
                                {
                                    el2.innerHTML = render_link
                                    (
                                        k,
                                        n,
                                        v2,
                                        "List Values"
                                    );
                                }
                                
                            });
                        }
                        else
                        {
                            const el2 = document.getElementById(`${k}-${n}`);
                            if(el2 != null)
                            {
                                el2.innerHTML = render_link
                                (
                                    k,
                                    n,
                                    v2,
                                    "List Values"
                                );
                            }
                        }
                        //const el2 = document.getElementById(`${k}-${n}`);
                        //el2.innerHTML = v2;
                    }
                }
            }
            else
            {
                //console.log("here");
            }
        }
        else if(type == "STAT_D")
        {
            let el = document.getElementById(`${k}-count`);

            if(el != null)
            {
                el.innerHTML = render_link
                (
                    k,
                    "count",
                    current_stat.get("count"),
                    "Date Summary Count"
                );

                el = document.getElementById(`${k}-missing`);                
                el.innerHTML = render_link
                (
                    k,
                    "missing",
                    current_stat.get("missing"),
                    "Missing Date Summary"
                );


                el = document.getElementById(`${k}-min`);
                let date = new Date(current_stat.get("min").split(' @')[0]);

                //el.innerHTML = formatDate(date);
                el.innerHTML = render_link(k, "min",  formatDate(date), "Min Date Summary");

                

                el = document.getElementById(`${k}-max`);
                date = new Date(current_stat.get("max").split(' @')[0]);

                //el.innerHTML = formatDate(date);
                el.innerHTML = render_link(k, "max",  formatDate(date), "Max Date Summary");
            }
        }
        else if(type == "STAT_N")
        {
            let el = document.getElementById(`${k}-count`);

            if(el != null)
            {
                el.innerHTML = render_link
                (
                    k,
                    "count",
                    current_stat.get("count"),
                    "Numeric Summary Count"
                );

                el = document.getElementById(`${k}-missing`);                
                el.innerHTML = render_link
                (
                    k,
                    "missing",
                    current_stat.get("missing"),
                    "Missing Numeric Summary"
                );
                /**/

                el = document.getElementById(`${k}-min`);
                el.innerHTML = render_link(k, "min", current_stat.get("min").split(' @')[0], "Min Numeric Summary");

                el = document.getElementById(`${k}-max`);
                el.innerHTML = render_link(k, "max", current_stat.get("max").split(' @')[0], "Max Numeric Summary");

                el = document.getElementById(`${k}-mean`);
                el.innerHTML = current_stat.get("mean").toFixed(2);

                el = document.getElementById(`${k}-std_dev`);
                el.innerHTML = current_stat.get("std_dev").toFixed(2);

                el = document.getElementById(`${k}-median`);
                el.innerHTML = toFixed_2(current_stat.get("median"));

                el = document.getElementById(`${k}-mode`);
                el.innerHTML = current_stat.get("mode").split(' @')[0];
            }
        }

        
    }

    //set_zero();

    window.setTimeout(set_zero,0);
    
}


function formatDate(dateObj)
{

    let result = dateObj;

    if(dateObj instanceof Date)
    {
        var month = dateObj.getUTCMonth() + 1; //months from 1-12
        var day = dateObj.getUTCDate();
        var year = dateObj.getUTCFullYear();

        result = month.toString().padStart(2,"0") + "/" + day.toString().padStart(2,"0") + "/" + year;
    }
    else
    {
        //console.log("here");
    }

    if(result == "NaN/NaN/NaN")
    {
        result = "(-)";
    }

    return result;
}

function search_case_status_onchange(p_value)
{
    if(g_filter.case_status != p_value)
    {
        g_filter.case_status = p_value;

    }

    show_needs_apply_id(true);
}

function search_pregnancy_relatedness_onchange(p_value)
{
    if(g_filter.pregnancy_relatedness != p_value)
    {
        g_filter.pregnancy_relatedness = p_value;

        show_needs_apply_id(true);
    }

    
}

function Can_Pass_Filter(p_value)
{

    let pregnancy_relatedness = true;
    let date_of_review_begin = true;
    let date_of_review_end = true;
    let date_of_death_begin = true;
    let date_of_death_end = true;
    let case_status = true;
    let selected_record_id = true;

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

    if(g_filter.include_blank_date_of_reviews == false)
    {
        date_of_review_begin = is_greater_than_date
        (
            p_value.year_of_case_review,
            p_value.month_of_case_review,
            p_value.day_of_case_review,
            g_filter.date_of_review.begin
        )

        date_of_review_end = is_less_than_date
        (
            p_value.year_of_case_review,
            p_value.month_of_case_review,
            p_value.day_of_case_review,
            g_filter.date_of_review.end
        )
    }

    if(g_filter.include_blank_date_of_deaths == false)
    {
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
    }
/*
    g_filter.pregnancy_relatedness
    g_filter.date_of_review.begin
    g_filter.date_of_review.end
    g_filter.date_of_death.begin
    g_filter.date_of_death.end
    */

    if
    (
        g_filter.pregnancy_relatedness == "all"
    )
    {
        pregnancy_relatedness = true;
    }
    else
    {
        pregnancy_relatedness = g_filter.pregnancy_relatedness == p_value.pregnancy_relatedness;
    }

    if
    (
        g_filter.case_status == "all"
    )
    {
        case_status = true;
    }
    else
    {
        case_status = g_filter.case_status == p_value.case_status;
    }

    if(g_filter.selected_record_id != null && g_filter.selected_record_id.length > 0)
    {
        selected_record_id = p_value.record_id.toLowerCase() == g_filter.selected_record_id.toLowerCase();

    }

    return pregnancy_relatedness && 
        case_status &&
        date_of_review_begin && 
        date_of_review_end && 
        date_of_death_begin && 
        date_of_death_end &&
        selected_record_id;

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

function toFixed_2(p_value)
{
    let result = p_value;
    if
    (
        p_value != "" &&
        ! isNaN(p_value)
    )
    {
        result = p_value.toFixed(2);
    }

    return result;
}


async function on_show_case_list_click
(
    p_path,
    p_value,
    p_count
)
{
    

	

    // shou display value
    // Option Selected: 
    // nan/nan/nan

    const type = g_report_stat_map.get(p_path).get("type");

    const result = [];
    render_search_result2(result, "/" + p_path);
    const result2 = [];

    const json_array = [];

    let sorted_list = [];



    if(type =="STAT_N" && p_value == "count")
    {
        const compare = [ "(-)", "count", "missing", "min", "max", "total" ]
        for(const [k, v] of g_path_to_value_map.get(p_path))
        {

            if(compare.indexOf(k) < 0)
            for(const record_id of v)
            {
                sorted_list.push(record_id)
            }
        
        }

    }
    else if(type =="STAT_D" && p_value == "count")
    {
        const compare = [ "count", "missing", "min", "max", "total" ]
        for(const [k, v] of g_path_to_value_map.get(p_path))
        {

            if(compare.indexOf(k) < 0)
            for(const record_id of v)
            {
                sorted_list.push(record_id)
            }
        
        }

    }
    else if(p_value == "count")
    {
        const compare = [ "(-)", "count", "missing", "min", "max", "total" ]
        for(const [k, v] of g_path_to_value_map.get(p_path))
        {

            if(compare.indexOf(k) < 0)
            for(const record_id of v)
            {
                sorted_list.push(record_id)
            }
        
        }

        

        if
        (
            sorted_list.length != p_count &&
            g_path_to_value_map.get(p_path).get("(-)").size + sorted_list.length == p_count
        )
        {
            
            for(const record_id of g_path_to_value_map.get(p_path).get("(-)"))
            {
                sorted_list.push(record_id)
            }
        }
        
    }
    else
    {
        for(const record_id of g_path_to_value_map.get(p_path).get(p_value))
        {
            sorted_list.push(record_id)
        }
    
    }

    sorted_list.sort();

    result2.push(`<ol>`);
    for(const record_id of sorted_list)
    {
        result2.push(`<li>${record_id}</li>`)
        json_array.push(`${record_id}`);
    }
    result2.push(`</ol>`)   


    let display_value = g_value_to_display_lookup["/" + p_path][p_value];
    if(display_value == undefined) display_value = "";



    let v_delimiter = "-"
    if(p_value == display_value)
    {
        p_value = "N (Count): " ;
        v_delimiter = "";
    }
    else if(p_value == "count")
    {
        p_value = "N (Total Count)";
        v_delimiter = "";  
    }
    else if(p_value == "missing")
    {
        p_value = "Missing Count";
        v_delimiter = "";

    }
    else if(p_value == "min")
    {
        p_value = "Minimum Value";
        v_delimiter = "";
    } 
    else if(p_value == "max")
    {
        p_value = "Maximum Value";
        v_delimiter = "";
    }
    else p_value = "N (Count): " + p_value;

    const link_field_name = `${selected_dictionary_info.form_name} - ${selected_dictionary_info.field_name} ${p_path}`;
    const link_field_summary =  `${p_value} ${v_delimiter} ${display_value}: ${p_count}`;
    const link_field_data = json_array.join(",")

    await data_dictionary_dialog_show
    (
        `${selected_dictionary_info.form_name} - ${selected_dictionary_info.field_name}`,
        result.join(""),
        `<b>${p_value} ${v_delimiter} ${display_value}:</b> ${p_count}`,
        result2.join(""),
        `<input class="btn primary-button" type="button" onclick="download_data_click('${link_field_name}','${link_field_summary}','${link_field_data}')" value="Download"/>`
    );
}

async function data_dictionary_dialog_show
(
    p_title, 
    p_inner_html, 
    p_sub_title, 
    p_detail_html,
    p_download_button
)
{

    let element = document.getElementById("dictionary-lookup-id");
    if(element == null)
    {
        element = document.createElement("dialog");
        element.classList.add('p-0');
        element.classList.add('set-radius');
        element.setAttribute("id", "dictionary-lookup-id");
        element.setAttribute("role", "dialog");

        document.firstElementChild.appendChild(element);
    }

    element.style.maxWidth = "1024px";
    element.style.transform = "translateY(0%)";
    element.style.maxHeight = "600px";
    element.style.overflow = "hidden";

    let html = [];
    html.push(`
        <div aria-modal="true" class="ui-dialog-titlebar modal-header bg-primary ui-widget-header ui-helper-clearfix">
            <span id="ui-id-1" class="ui-dialog-title" style="font-family: 'Open-Sans';">Matching MMRIA ID#'s</span>
            <button type="button" class="ui-button ui-corner-all ui-widget ui-button-icon-only ui-dialog-titlebar-close" title="×" onclick="$mmria.data_dictionary_dialog_click()"><span class="ui-button-icon ui-icon ui-icon-closethick"></span><span class="ui-button-icon-space"> </span>×</button>
        </div>
        <div id="mmria_dialog5" style="overflow-y: scroll;width: 1000; height: 450px;" class="ui-dialog-content ui-widget-content">
            <div class="modal-body">
                <table class="table table-fixed-layout align-cell-top mb-3" style="font-size: 14px"  >
                <caption class="table-caption">
                    Matching MMRIA ID Numbers for ${p_title}.
                </caption>
                <thead>
                    <tr class="header-level-top-black">
                        <th class="th" width="140" scope="col">MMRIA Form</th>
                        <th class="th" width="140" scope="col">Export File Name</th>
                        <th class="th" width="120" scope="col">Export Field</th>
                        <th class="th" width="180" scope="col">Prompt</th>
                        <th class="th" width="380" scope="col">Description</th>
                        <th class="th" width="260" scope="col">Path</th>
                        <th class="th" width="110" scope="col">Data Type</th>
                    </tr>
                </thead>
                
                ${p_inner_html}
                </table>
                ${p_sub_title}<br/><br/>
                ${p_detail_html}
            </div>

        </div>
        <div>
        <footer class="modal-footer">
            ${p_download_button}
            <button id="data_dictionary_dialog_close_button" class="btn secondary-button mr-1" onclick="$mmria.data_dictionary_dialog_click()" style="font-family: 'Open-Sans';">Close</button>
        </footer>
        </div>
    `);

    element.innerHTML = html.join("");

    mmria_pre_modal("dictionary-lookup-id");
    
    window.setTimeout(()=> { const data_dictionary_dialog_close_button = document.getElementById("data_dictionary_dialog_close_button"); data_dictionary_dialog_close_button.focus(); }, 0);
    element.showModal();
}
 
function data_dictionary_dialog_click()
{
    mmria_post_modal();
    let el = document.getElementById("dictionary-lookup-id");
    el.close();
}

function render_link
(
    p_path,
    p_value,
    p_count,
    p_prompt,
)
{

    return `
        <a aria-label="Show ${p_count} ${p_value} case list for ${p_path.split("/").pop()} field" href="javascript:on_show_case_list_click('${p_path}','${p_value.replace(/"/g,"&quot;")}','${p_count}')">${p_count}</a>
    `;

    
}

function render_search_result2(p_result, p_path)
{
	selected_dictionary_info = {};
	render_search_result_item2(p_result, g_metadata, "", null, p_path.toLowerCase(), false);
}


//let last_form = null;
let selected_dictionary_info = {};

function render_search_result_item2(p_result, p_metadata, p_path, p_selected_form, p_search_text, p_is_abstractor_committee)
{

    if(p_metadata.mirror_reference != null && p_metadata.mirror_reference != "")
    {
        return;
    }
	switch(p_metadata.type.toLowerCase())
	{
		case "form":			
			if(p_selected_form== null || p_selected_form=="")
			{
				for(let i = 0; i < p_metadata.children.length; i++)
				{
					let item = p_metadata.children[i];

					render_search_result_item2(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
				}
			}
			else
			{
				if(p_metadata.name.toLowerCase() == p_selected_form.toLowerCase())
				{
					for(let i = 0; i < p_metadata.children.length; i++)
					{
						let item = p_metadata.children[i];

						render_search_result_item2(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
					}
				}
			}
			break;

		case "app":
			let form_filter = "(any form)";
			let el = document.getElementById("form_filter");

			if(el)
			{
				form_filter = el.value;
			}

			for(let i = 0; i < p_metadata.children.length; i++)
			{
				let item = p_metadata.children[i];

				if(form_filter.toLowerCase() == "(any form)")
				{
					render_search_result_item2(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
				}
				else if(item.type.toLowerCase() == "form")
				{
					render_search_result_item2(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
				}
				
			}
			break;

		case "group":
		case "grid":
			for(let i = 0; i < p_metadata.children.length; i++)
			{
				let item = p_metadata.children[i];
				render_search_result_item2(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
			}
			break;

        case "chart":
        case "label":
        case "button":
            break;

		default:
			let file_name = "";
			let field_name = "";
			let file_field_item = g_release_version_specification.path_to_csv_all[p_path];

			if(file_field_item)
			{
				file_name = file_field_item.file_name;
				field_name = file_field_item.field_name;
			}

			if(p_path != p_search_text)
			{
                return;
			}

			let form_name = "(none)";
			let path_array = p_path.split('/');
			let description = "";
			let list_values = [];
			let data_type = p_metadata.type.toLowerCase();

			if
			(
				p_metadata.data_type != null &&
				p_metadata.data_type != ""
			)
			{
				data_type = p_metadata.data_type
			}

			if(path_array.length > 2)
			{
				form_name = path_array[1];
				form_name = convert_form_name(form_name);
			}


            selected_dictionary_info.form_name = form_name;
            selected_dictionary_info.field_name = p_metadata.prompt;

			if(p_metadata.description != null)
			{
				description = p_metadata.description;
			}


			if(p_metadata.type.toLowerCase() == "list")
			{
				let value_list = p_metadata.values;

				if(p_metadata.path_reference && p_metadata.path_reference != "")
				{
					value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
			
					if(value_list == null)	
					{
						value_list = p_metadata.values;
					}
				}




				list_values.push(`
					<tr class="tr">
						<td class="td" width="140"></td>
						<td class="td p-0" colspan="5">
							<table class="table table--standard rounded-0 m-0">
								<thead class="thead">
									<tr class="tr bg-gray-l2">
										<th class="th" colspan="5" width="1080" scope="colgroup">List Values</th>
									</tr>
								</thead>
								<thead class="thead">
									<tr class="tr bg-gray-l2">
										<th class="th" width="140" scope="col">Value</th>
										<th class="th" width="680" scope="col">Display</th>
										<th class="th" width="260" scope="col">Description</th>
									</tr>
								</thead>
								<tbody class="tbody">	
				`);
                    
					for(let i= 0; i < value_list.length; i++)
					{
						list_values.push(`
									<tr class="tr">
										<td class="td" width="140">${value_list[i].value}</td>
										<td class="td" width="680">${value_list[i].display}</td>
										<td class="td" width="260">${value_list[i].description}</td>
									</tr>
						`);
					}
				
				list_values.push(`
								</tbody>
							</table>
						</td>
						<td class="td" colspan="2"></td>
					</tr>
				`);
			}

			// Remove fields who do not have a form_name or if it doesn't exist
			// if (!form_name || form_name == '(none)' || form_name == '(blank)') {
			// if (!form_name || form_name.includes('none') || form_name.includes('blank')) {
			if (
				!form_name ||
				 form_name.indexOf('none') !== -1 ||
				 form_name == '(none)' ||
				 form_name.indexOf('blank') !== -1 ||
				 form_name == '(blank)'
			) 
            {
				return;
			}


			p_result.push(`
				<tr class="tr">
					<td class="td" width="140">${form_name}</td>
					<td class="td" width="140">${file_name}</td>
					<td class="td" width="120">${field_name}</td>
					<td class="td" width="180">${p_metadata.prompt}</td>
					<td class="td" width="380">${description}</td>
					<td class="td" width="260">${p_path}</td>
					<td class="td" width="110">${(data_type.toLowerCase() == "textarea" || data_type.toLowerCase() == "jurisdiction")? "string": data_type}</td>
				</tr>
				
			`);

			break;

	}
}



function set_list_lookup
(
    p_list_lookup, 
    p_name_to_value_lookup, 
    p_value_to_index_number_lookup, 
    p_metadata, 
    p_path,
    p_is_multiform = false,
    p_is_grid = false

)
{
    switch(p_metadata.type.toLowerCase())
    {
        case "app":
        case "group":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                set_list_lookup
                (
                    p_list_lookup, 
                    p_name_to_value_lookup, 
                    p_value_to_index_number_lookup, 
                    child, p_path + "/" + child.name,
                    p_is_multiform,
                    p_is_grid
                );
            }

            break;        
        case "form":

            if(!g_form_field_map.has(p_metadata.name))
            {
                g_form_field_map.set(p_metadata.name, new Map());
            }	

            let is_multiform = p_is_multiform;
            if
            (
                p_metadata.cardinality != null &&
                (
                    p_metadata.cardinality == "+" ||
                    p_metadata.cardinality == "*" 
                )
            )
            {
                is_multiform = true;
                multiform_set.add(p_path);
            }

            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                set_list_lookup
                (
                    p_list_lookup, 
                    p_name_to_value_lookup, 
                    p_value_to_index_number_lookup, 
                    child, p_path + "/" + child.name,
                    is_multiform,
                    p_is_grid
                );
            }

            break;
        case "grid":
            let is_grid = true;

            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                set_list_lookup
                (
                    p_list_lookup, 
                    p_name_to_value_lookup, 
                    p_value_to_index_number_lookup, 
                    child, p_path + "/" + child.name,
                    p_is_multiform,
                    is_grid
                );
            }

            break;

        default:


            if(p_is_multiform && p_is_grid)
            {
                multiform_grid_set.add(p_path);
            }

            if(p_is_multiform)
            {
                multiform_set.add(p_path);
            }
            
            if(p_metadata.type.toLowerCase() == "list")
            {
                let data_value_list = p_metadata.values;

                if(p_metadata.path_reference && p_metadata.path_reference != "")
                {
                    data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
            
                    if(data_value_list == null)	
                    {
                        data_value_list = p_metadata.values;
                    }
                }
    
                p_list_lookup[p_path] = {};
                p_name_to_value_lookup[p_path] = {};
                p_value_to_index_number_lookup[p_path] = {};

                for(let i = 0; i < data_value_list.length; i++)
                {
                    let item = data_value_list[i];
                    p_list_lookup[p_path][item.display.toLowerCase()] = item.value;
                    p_name_to_value_lookup[p_path][item.value] = item.display;
                    p_value_to_index_number_lookup[p_path][item.value] = i;
                }
            }
            else
            {
                p_list_lookup[p_path] = {};
                p_name_to_value_lookup[p_path] = {};
                p_value_to_index_number_lookup[p_path] = {};
            }

            let file_name = "";
			let field_name = "";
			let file_field_item = g_release_version_specification.path_to_csv_all[p_path];

			if(file_field_item)
			{
				file_name = file_field_item.file_name;
				field_name = file_field_item.field_name;
			}

            if(p_metadata.tags.length == 0)
            {
                break;
            } 
                
            if
            (
                !p_metadata.tags.includes("FREQ") &&
                !p_metadata.tags.includes("STAT_N") &&
                !p_metadata.tags.includes("STAT_D") &&
                !p_metadata.tags.includes("CALC_DATE")
            )
            {
                break;
            }

            g_path_to_value_map.set(p_path.substr(1), new Map());

            let form_data_name = "";
            let form_name = "(none)";
            let path_array = p_path.split('/');
            let description = "";
            let list_values = [];
            let data_type = p_metadata.type.toLowerCase();

            if
			(
				p_metadata.data_type != null &&
				p_metadata.data_type != ""
			)
			{
				data_type = p_metadata.data_type
			}

			if(path_array.length > 2)
			{
				form_data_name = path_array[1];
				form_name = convert_form_name(form_data_name);
			}

            if
            (
                g_form_field_map.has(form_data_name) &&
                !g_form_field_map.get(form_data_name).has(field_name)
            )
            {
                const max_length = 40;

                let display_prompt = `${p_metadata.prompt} [${field_name}]`;
                if(display_prompt.length> max_length)
                {
                    display_prompt = p_metadata.prompt.substring(0, max_length);
                }


                let title_prompt = `[${field_name}] ${p_metadata.prompt}`;
                g_form_field_map.get(form_data_name).set
                (
                    field_name, 
                    { 
                        field_name: field_name, 
                        data_name: p_metadata.name,
                        display_prompt: display_prompt,
                        title_prompt: title_prompt
                    }
                );
            }

            break;
    }
}

async function download_data_click
(
    link_field_name,
    link_field_summary,
    link_field_data
)
{
    //console.log("here");
    const rp = {
        "fn" : link_field_name,
        "fs" : link_field_summary,
        "fd" : link_field_data
    }

    myFetch(rp);

   // `<a target="_new" href="steveMMRIA/GetFileResult?FileName=${q.items[i].fileName}"></a>`
   // <a href="view-data-summary/GenerateReport" target="_report" >Excel</a>
}

function myFetch(p_data) 
{
    let file_name = `View Data Summary-${p_data.fn} ${p_data.fs.replace("/","-")}.xlsx`;
    if(file_name.length > 50)
    {
        file_name = `View Data Summary-${p_data.fn.substr(0, 10)}-${p_data.fs.substr(0, 10).replace("/","-")}.xlsx`
    }

    fetch(`${location.protocol}//${location.host}/view-data-summary/GenerateReport`, {
      method: "POST",
      headers: {
        Accept: "application/json, text/plain, */*",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(p_data),
    })
      .then((response) => {
        return response.blob();
      })
      .then((blob) => {
        downloadFile(blob, file_name);
      });
  }

  function downloadFile(blob, name = "file.pdf") {
    const href = URL.createObjectURL(blob);
    const a = Object.assign(document.createElement("a"), {
      href,
      style: "display:none",
      download: name,
    });
    document.body.appendChild(a);
    a.click();
    URL.revokeObjectURL(href);
    a.remove();
  }


  function set_form_enable(p_value)
  {
    
    for(const item of document.querySelectorAll("input"))
    {
        item.disabled = !p_value;
    }

    for(const item of document.querySelectorAll("button"))
    {
        item.disabled = !p_value;
    }

    for(const item of document.querySelectorAll("select"))
    {
        item.disabled = !p_value;
    }
  }