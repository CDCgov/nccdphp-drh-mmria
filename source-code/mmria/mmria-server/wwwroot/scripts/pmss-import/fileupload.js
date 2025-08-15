var g_file_stat_list = [];
var g_content_list = [];
var g_validation_errors = [];
var g_cdc_identifier_set = {};
var g_is_other_file = false;

const mor_max_length = 5001;
const nat_max_length = 4001;
const fet_max_length = 6001;

let data = null;

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

    //show spinner
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
}


function process_button_click() 
{
    send_ije_set();
}

function setup_file_list() 
{
    var el = document.getElementById("files");
    el.disabled = "disabled";

    let result = false;

    let is_mor = false;
    let is_nat = false;
    let is_fet = false;

    let temp = [];
    let temp_contents = [];

    if (g_file_stat_list.length < 2) 
    {
        //g_validation_errors.push("need at least 2 IJE files. MOR and NAT or FET");
    }

    // process mor file 1st
    for (let i = 0; i < g_file_stat_list.length; i++) 
    {
        let item = g_file_stat_list[i];
        if (typeof item !== "undefined") 
        {
            if (item.name.toLowerCase().endsWith(".csv")) 
            {
                g_cdc_identifier_set = {};
                
                is_mor = true;
                temp[0] = item;
                temp_contents[0] = g_content_list[i];

                data = parseCSV(temp_contents[0]);

                // other 150 columns

                // all 555 columns




                if 
                (
                    data.length < 2
                ) 
                {
                    g_validation_errors.push("csv file is empty" + data.length);
                }
                else if 
                (
                    data[0].length != 156 &&
                    data[0].length != 562
                ) 
                {
                    g_validation_errors.push("csv columns != 150 or 555" + data.length);
                }
                else 
                {

                    if(data[0].length == 156) g_is_other_file = true;
                    /*
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

                        g_validation_errors.push("Duplicate CDC Identifier detected " + duplicatesMessage);
                    }
                    */
                }
            }
        }
    }


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
    if (p_val.length > 15) 
    {
        return p_val.substr(11, p_val.length - 15);
    }
    else 
    {
        return p_val;
    }
}


function render_file_list() {
    let bag = document.getElementById('bag');

    let ul_list = [];
    ul_list.push("<ul>");
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

            if (item.name.toLowerCase().endsWith(".csv")) 
            {
                let out = document.getElementById('mor_output');

                const label_element = document.getElementById('csv-label');
                if(g_is_other_file)
                {
                    label_element.innerText = `CSV File Content: Other Version`;
                }
                else
                {
                    label_element.innerText = `CSV File Content: All Version`;
                }
                

                const csv = [];

                for(const item of data)
                {
                    csv.push(`${item[0]}, ${item[1]}, ${item[2]}, ${item[3]}, ${item[4]}, ${item[5]}, ${item[6]}, ${item[7]}, ${item[8]}, ${item[9]}`)
                }

                out.value = csv.join("\n");
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

            ul_list.push("<li>");
            ul_list.push(item.name);
            ul_list.push(" number of records ");
            ul_list.push(" (");
            ul_list.push(data.length -1);
            ul_list.push(")</li>");
        }
    }
    ul_list.push("</ul>");

    bag.innerHTML = ul_list.join("");

    let out = document.getElementById('output');
    let button = document.getElementById('process');

    if (g_validation_errors.length > 0) 
    {
        out.value = g_validation_errors.join("\n");
    }
    else 
    {
        out.value = " ready to process";
        button.disabled = false;
    }

    //hide the spinner when things are done
    $('.spinner-inline').fadeOut();
}

function send_ije_set() 
{
    var filename1 = "";
    var filename2 = ""

    if (typeof g_file_stat_list[1] !== "undefined") 
    {
        filename1 = g_file_stat_list[1].name;
    }

    if (typeof g_file_stat_list[2] !== "undefined") 
    {
        filename2 = g_file_stat_list[2].name;
    }

    let data = {
        mor: g_content_list[0],
        nat: g_content_list[1],
        fet: g_content_list[2],
        mor_file_name: g_file_stat_list[0].name,
        nat_file_name: filename1,
        fet_file_name: filename2
    };

    $.ajax({
        url: location.protocol + '//' + location.host + '/api/pmss_csv_import',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify(data),
        type: "POST"
    }).done(function (response) {
        const buttonNext = document.getElementById('process_next');
        let out = document.getElementById('output');
        var response_obj = eval(response);

        if (response_obj.ok) 
        {
            out.value = `CSV File successfully sent.\n\nBatch Id = ${response.batch_id}\n\nCheck the CSV Import Notification Report in a few minutes to get the results of the process.`;

            let button = document.getElementById('process_next');
            buttonNext.style.display = 'inline-block';
        }
        else 
        {
            out.value = `CSV File error while sending\nError Detail\n = ${response.detail}`;
            buttonNext.style.display = 'none';
        }

        let button = document.getElementById('process');
        button.disabled = true;
    });
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


/*
private List<string> GetAssociatedNat(string[] p_nat_list, string p_cdc_unique_id)
{
    var result = new List<string>();
    int mom_ssn_start = 2000-1;
    if (p_nat_list != null)
        foreach (var item in p_nat_list)
        {
            if (item.Length > mom_ssn_start + 9)
            {
                var mom_ssn = item.Substring(mom_ssn_start, 9)?.Trim();
                if (mom_ssn == p_cdc_unique_id)
                {
                    result.Add(item);
                }
            }
        }

    return result;
}
*/

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



/*
private List<string> GetAssociatedFet(string[] p_fet_list, string p_cdc_unique_id)
{
    var result = new List<string>();
    int mom_ssn_start = 4039-1;
    if(p_fet_list != null)
        foreach(var item in p_fet_list)
        {
            if(item.Length > mom_ssn_start + 9)
            {
                var mom_ssn = item.Substring(mom_ssn_start, 9)?.Trim();
                if(mom_ssn == p_cdc_unique_id)
                {
                    result.Add(item);
                }
            }
        }

    return result;
}
*/

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


function parseCSV(str) 
{
    const arr = [];
    let quote = false;  // 'true' means we're inside a quoted field

    // Iterate over each character, keep track of current row and column (of the returned array)
    for (let row = 0, col = 0, c = 0; c < str.length; c++) {
        let cc = str[c], nc = str[c+1];        // Current character, next character
        arr[row] = arr[row] || [];             // Create a new row if necessary
        arr[row][col] = arr[row][col] || '';   // Create a new column (start with empty string) if necessary

        // If the current character is a quotation mark, and we're inside a
        // quoted field, and the next character is also a quotation mark,
        // add a quotation mark to the current column and skip the next character
        if (cc == '"' && quote && nc == '"') { arr[row][col] += cc; ++c; continue; }

        // If it's just one quotation mark, begin/end quoted field
        if (cc == '"') { quote = !quote; continue; }

        // If it's a comma and we're not in a quoted field, move on to the next column
        if (cc == ',' && !quote) { ++col; continue; }

        // If it's a newline (CRLF) and we're not in a quoted field, skip the next character
        // and move on to the next row and move to column 0 of that new row
        if (cc == '\r' && nc == '\n' && !quote) { ++row; col = 0; ++c; continue; }

        // If it's a newline (LF or CR) and we're not in a quoted field,
        // move on to the next row and move to column 0 of that new row
        if (cc == '\n' && !quote) { ++row; col = 0; continue; }
        if (cc == '\r' && !quote) { ++row; col = 0; continue; }

        // Otherwise, append the current character to the current column
        arr[row][col] += cc;
    }
    return arr;
}
