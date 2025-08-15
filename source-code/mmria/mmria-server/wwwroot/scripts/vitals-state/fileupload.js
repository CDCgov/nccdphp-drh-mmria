var g_fileg_validation_errors_stat_list = [];
var g_content_list = [];
var g_validation_errors = new Set();
var g_host_state = null;
var g_cdc_identifier_set = {};

var g_jurisdiction_tree = null;

var host_prefix = null;

var g_my_roles = null;

const g_case_folder_list = [];

var highest_folder = null;

var g_is_setup_started = false;


const mor_max_length = 5001;
const nat_max_length = 4001;
const fet_max_length = 6001;

/*

file length
mor 5000
nat 4000
fet 6000

*/

var openFile = function (event) 
{

    var input = event.target;


    var reader = new FileReader();
    reader.onload = function () {
        var dataURL = reader.result;
        var output = document.getElementById('output');
        output.value = dataURL;
    };
    reader.readAsText(input.files[0]);
};

function readmultifiles(event, files) 
{
    const self = $(event.target);
    let ul_list = [];
    g_file_stat_list = [];

    self.next('.spinner-inline').fadeIn();

    for (let i = 0; i < files.length; i++) 
    {
        let item = files[i];
        readFile(i);
        g_file_stat_list.push({ name: item.name, index: i })

    }

    function readFile(index) 
    {
        if (index >= files.length) return;

        var file = files[index];
        var reader = new FileReader();
        reader.onload = function (e) {
            // get file content  
            g_content_list[index] = e.target.result;
            // do sth with bin
            readFile(index + 1)
        }
        reader.readAsText(file);

        window.setTimeout(setup_file_list, 9000);
    }
}


window.onload = function () 
{
    let process_button = document.getElementById("process");
    process_button.onclick = process_button_click;
    process_button.disabled = true;

    host_prefix = window.location.host.split("-")[0].toUpperCase();
    if(host_prefix.indexOf(":") > -1)
    {
        const colon_split = host_prefix.split(":");
        host_prefix = colon_split[0];
    }

    const header = document.getElementById('upload-header-id');
    header.innerText = `Upload IJE Files for Jurisdiction: ${host_prefix}`;
}


async function process_button_click() 
{
    await send_ije_set();
}

async function setup_file_list() 
{
    if(g_is_setup_started) return;

    g_is_setup_started = true;

    var el = document.getElementById("files");
    el.disabled = "disabled";

    g_host_state = null;

    let result = false;

    let is_mor = false;
    let is_nat = false;
    let is_fet = false;

    let temp = [];
    let temp_contents = [];

    if (g_file_stat_list.length < 2) 
    {
        //g_validation_errors.add("need at least 2 IJE files. MOR and NAT or FET");
    }

    // process mor file 1st
    for (let i = 0; i < g_file_stat_list.length; i++) 
    {
        let item = g_file_stat_list[i];
        if (typeof item !== "undefined") 
        {
            if (item.name.toLowerCase().endsWith(".mor")) 
            {
                g_cdc_identifier_set = {};
                
                is_mor = true;
                temp[0] = item;
                temp_contents[0] = g_content_list[i];

                var patt = new RegExp("^20[0-9]{2}_20[0-9]{2}_[0-2][0-9]_[0-3][0-9]_[A-Z,a-z]{2,9}.[mM][oO][rR]$");

                if (!patt.test(item.name.toLowerCase())) 
                {
                    g_validation_errors.add(`mor file name format incorrect.\nFile name must be in 20##_Year_Month_Day_StateCode[2-9] format.\n(e.g. 2000_2021_01_01_${host_prefix}.mor)\nfound ${item.name}`);
                }


                const remove_extension = item.name.split(".");
                const split_on_underscore = remove_extension[0].split("_");
        
                const state_name = split_on_underscore[split_on_underscore.length -1];

                if (state_name.toUpperCase() != host_prefix) 
                {
                    g_validation_errors.add(`mor file name format incorrect. State Section doesn't match host ${state_name} != ${host_prefix}`);
                }


                if (!validate_length(g_content_list[i].split("\n"), mor_max_length)) 
                {
                    g_validation_errors.add("mor File Length !=" + mor_max_length);
                }
                else 
                {
                    var copy = g_content_list[i];
                    var morRows = copy.split("\n");
                    var listOfCdcIdentifier = [];
                    

                    for (var j = 0; morRows.length > j; j++) 
                    {
                        var cdcIdentifier = morRows[j].substring(190, 199);

                        listOfCdcIdentifier.push(cdcIdentifier.trim());
                        g_cdc_identifier_set[cdcIdentifier.trim()] = true;
                    }

                    let findDuplicates = arr => arr.filter((item, index) => arr.indexOf(item) != index)
                    var duplicates = findDuplicates(listOfCdcIdentifier)



                    if (duplicates.length > 0) 
                    {
                        var counts = {};
                        var duplicatesMessage = "";
                        duplicates.forEach(function (x) { counts[x] = (counts[x] || 0) + 1; });

                        for (var k = 0; k < Object.keys(counts).length; k++) 
                        {
                            duplicatesMessage += "\n" + Object.keys(counts)[k] + ', ' + Object.values(counts)[k]
                        }

                        g_validation_errors.add("Duplicate CDC Identifier detected " + duplicatesMessage);
                    }
                }
            }
        }
    }

    for (let i = 0; i < g_file_stat_list.length; i++) 
    {
        let item = g_file_stat_list[i];
        if (typeof item !== "undefined") 
        {
            if (item.name.toLowerCase().endsWith(".mor")) 
            {
                continue;
            }
            else if (item.name.toLowerCase().endsWith(".nat")) 
            {
                is_nat = true;
                temp[1] = item;
                temp_contents[1] = g_content_list[i];

                g_content_list_array = g_content_list[i].split("\n");
                if (!validate_length(g_content_list_array, nat_max_length)) 
                {
                    g_validation_errors.add("nat File Length !=" + nat_max_length);
                }

               let Nat_Ids = validate_AssociatedNAT(g_content_list_array);
               for(let _i = 0;_i < Nat_Ids.length; _i++)
               {
                    let text = Nat_Ids[_i];

                    g_validation_errors.add(text);
                    
               }
            }
            else if (item.name.toLowerCase().endsWith(".fet")) 
            {
                is_fet = true;
                temp[2] = item;
                temp_contents[2] = g_content_list[i];

                g_content_list_array = g_content_list[i].split("\n");

                if (!validate_length(g_content_list_array, fet_max_length)) 
                {
                    g_validation_errors.add("fet File Length !=" + fet_max_length);
                }

                let Fet_Ids = validate_AssociatedFET(g_content_list_array);
                for(let _i = 0;_i < Fet_Ids.length; _i++)
                {
                    let text = Fet_Ids[_i];

                    g_validation_errors.add(text);
                    
                }
            }
        }
    }

    if (is_mor && (is_nat || is_fet)) 
    {
        if
            (!(

                get_state_from_file_name(g_file_stat_list[0].name) &&
                (typeof g_file_stat_list[1] === "undefined" || get_state_from_file_name(g_file_stat_list[1].name)) &&
                (typeof g_file_stat_list[2] === "undefined" || get_state_from_file_name(g_file_stat_list[2].name))
            )

        ) 
        {
            g_validation_errors.add("all IJE files must have same state");
        }
        else 
        {
            g_host_state = get_state_from_file_name(g_file_stat_list[0].name);
        }


    }
    else
    {
        g_host_state = get_state_from_file_name(g_file_stat_list[0].name);
        //g_validation_errors.add("need at least 2 IJE files. MOR and NAT or FET");
    }

    if (!is_mor) 
    {
        g_validation_errors.add("missing .MOR IJE file")
    }

    //if(!is_nat)
    //{
    //    g_validation_errors.add("missing .NAT IJE file")
    //}

    //if(!is_fet)
    //{
    //    g_validation_errors.add("missing .FET IJE file")
    //}

	g_my_roles = await $.ajax
    (
        {
            url: location.protocol + '//' + location.host + '/api/user_role_jurisdiction_view/my-roles',//&search_key=' + g_uid,
            headers: {          
            Accept: "text/plain; charset=utf-8",         
            "Content-Type": "text/plain; charset=utf-8"   
            } 
	    }
    )

    for(const i of g_my_roles.rows)
    {
        const role = i.value;
        if(role.is_active !== true) continue;
        if(role.role_name != "vital_importer_state") continue;
        
        highest_folder = role.jurisdiction_id;
       
        break;
    }


    g_jurisdiction_tree = await $.ajax
    (
        {
            url: location.protocol + '//' + location.host + '/api/jurisdiction_tree',
            type: "GET"
        }
    );


    const case_folder_el = document.getElementById("case-folder");
    case_folder_el.remove(0);
    case_folder_el.removeAttribute("disabled");
    case_folder_el.removeAttribute("aria-disabled");


    if(highest_folder == "/")
    {
        g_case_folder_list.push("Top Folder")
        let newOption = new Option('Top Folder','/', true);
        case_folder_el.add(newOption);
    }

    for (let i = 0; i < g_jurisdiction_tree.children.length; i++) 
    {
        const val = g_jurisdiction_tree.children[i].name;
        if(val.startsWith(highest_folder))
        {
            g_case_folder_list.push(val);
            newOption = new Option(val,val);
            case_folder_el.add(newOption);
        }

    }


    g_file_stat_list = temp;
    g_content_list = temp_contents;

    render_file_list()

    return result;

}


function validate_length(p_array, p_max_length) 
{
    let result = true;

    for (let i = 0; i < p_array.length; i++) 
    {
        let item = p_array[i];
        if (item.length > 0 && item.length != p_max_length) 
        {
            result = false;
            break;
        }
    }

    return result;
}

function get_state_from_file_name(p_val) 
{
    const remove_extension = p_val.split(".")
    const split_on_underscore = remove_extension[0].split("_");

    return split_on_underscore[split_on_underscore.length -1];
}


function case_folder_changed(value)
{
    highest_folder = value;
    render_file_list();
}

function render_file_list() 
{
    let bag = document.getElementById('bag');

    let case_folder_name = highest_folder;
    if(highest_folder == "/")
    {
        case_folder_name = "Top Folder";
    }

    let ul_list = [];
    ul_list.push("<ul id='file-detail-listing'>");
    for (let i = 0; i < g_file_stat_list.length; i++) 
    {
        let item = g_file_stat_list[i];
        let number_of_lines = 0;

        if (typeof item !== "undefined") 
        {
            if (g_content_list.length > i && g_content_list[i]) 
            {

                let lines = g_content_list[i].split('\n');
                for (let j = 0; j < lines.length; j++) 
                {
                    let line = lines[j];
                    if (line.trim().length > 0) number_of_lines += 1;
                }

            }

            if (item.name.toLowerCase().endsWith(".mor")) 
            {
                let out = document.getElementById('mor_output')
                out.value = g_content_list[i];
            }
            else if (item.name.toLowerCase().endsWith(".nat")) 
            {
                let out = document.getElementById('nat_output')
                out.value = g_content_list[i];
            }
            else if (item.name.toLowerCase().endsWith(".fet")) 
            {
                let out = document.getElementById('fet_output')
                out.value = g_content_list[i];
            }

            ul_list.push
            (
                `
                <li>
                    ${item.name} 
                    number of records 
                    (${number_of_lines}).
                    Case Folder: ${case_folder_name}
                </li>
                `
            );
        }
    }
    ul_list.push("</ul>");

    bag.innerHTML = ul_list.join("");

    let out = document.getElementById('output');
    let button = document.getElementById('process');

    if (g_validation_errors.size > 0) 
    {
        const list = []
        g_validation_errors.forEach(i => list.push(i));
        out.value = list.join("\n");
    }
    else 
    {
        
        out.value = g_host_state + " ready to process.   Case Folder: " + case_folder_name;
        button.disabled = false;
    }

    $('.spinner-inline').fadeOut();
}

async function send_ije_set() 
{
    let filename1 = "";
    let filename2 = ""

    if (typeof g_file_stat_list[1] !== "undefined") 
    {
        filename1 = g_file_stat_list[1].name;
    }

    if (typeof g_file_stat_list[2] !== "undefined") 
    {
        filename2 = g_file_stat_list[2].name;
    }

    const case_folder_el = document.getElementById("case-folder");
    let selected_case_folder = case_folder_el.value;

    if(selected_case_folder == null || selected_case_folder == "")
    {
        selected_case_folder = case_folder_el.options[0].value;
    }

    let data = {
        mor: g_content_list[0],
        nat: g_content_list[1],
        fet: g_content_list[2],
        mor_file_name: g_file_stat_list[0].name,
        nat_file_name: filename1,
        fet_file_name: filename2,
        case_folder: selected_case_folder
    };

    const response = await $.ajax({
        url: location.protocol + '//' + location.host + '/api/ije_message',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify(data),
        type: "POST"
    });

    const buttonNext = document.getElementById('process_next');
    let out = document.getElementById('output');

    if (response.ok) 
    {
        out.value = `IJE File Set for host state ${g_host_state} successfully sent.\n\nBatch Id = ${response.batch_id}\n\nCheck the Vitals Notification Report in a few minutes to get the results of the process.`;

        let button = document.getElementById('process_next');
        buttonNext.style.display = 'inline-block';
    }
    else 
    {
        out.value = `IJE File error while sending for host state ${g_host_state}\nError Detail\n = ${response.detail}`;
        buttonNext.style.display = 'none';
    }

    let button = document.getElementById('process');
    button.disabled = true;
    
}

function hasDuplicates(arr) 
{
    var counts = [];

    for (var i = 0; i <= arr.length; i++) 
    {
        if (counts[arr[i]] === undefined) 
        {
            counts[arr[i]] = 1;
        } 
        else 
        {
            return true;
        }
    }
    return false;
}

function validate_AssociatedNAT(p_array) 
{
    let result = [];

    let mom_ssn_start = 2000-1;

    for (let i = 0; i < p_array.length; i++) 
    {
        let item = p_array[i];
        if (item.length > mom_ssn_start + 9) 
        {

            var mom_ssn = item.substring(mom_ssn_start, mom_ssn_start + 9).trim();
            if (g_cdc_identifier_set[mom_ssn] == null || g_cdc_identifier_set[mom_ssn] == false)
            {
                result.push(`NAT file Line: ${i+1}  id: ${mom_ssn} not found in .mor file`);
            }
            
        }
    }

    return result;
}



function validate_AssociatedFET(p_array) 
{
    let result = [];

    let mom_ssn_start = 4039-1;

    for (let i = 0; i < p_array.length; i++) 
    {
        let item = p_array[i];
        if (item.length > mom_ssn_start + 9) 
        {
            var mom_ssn = item.substring(mom_ssn_start, mom_ssn_start + 9).trim();
            if (g_cdc_identifier_set[mom_ssn] == null || g_cdc_identifier_set[mom_ssn] == false)
            {
                result.push(`FET file Line: ${i+1}  id: ${mom_ssn} not found in .mor file`);
            }
        }
    }

    return result;
}
