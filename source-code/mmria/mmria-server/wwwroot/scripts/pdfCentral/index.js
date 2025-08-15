const g_file_stat_list = []
const g_file_content_list = []
const g_is_file_valid_list = []
let g_is_setup_started = false;


window.onload = main;

async function main()
{
    const el = document.getElementById("attachment_file_info_list");
    
    el.innerHTML =  render_file_info_list();

    //console.log(q);    
    
}



var attachment_openFile = function (event) 
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

async function attachment_readmultifiles(event, files) 
{
    const self = $(event.target);
    let ul_list = [];
    g_file_stat_list.length = 0;

    self.next('.spinner-inline').fadeIn();

    function readFile(index) 
    {
        if (index >= files.length) return;

        var file = files[index];
        var reader = new FileReader();
        reader.onload = function (e) {
            // get file content  
            g_file_content_list[index] = e.target.result;
            // do sth with bin
            readFile(index + 1)
        }
        reader.readAsDataURL(file);
    }

    for (let i = 0; i < files.length; i++) 
    {
        const item = files[i];
        readFile(i);
        g_file_stat_list.push({ name: item.name, index: i })

    }

    const el = document.getElementById("files");
    el.disabled = "disabled";
    const attachment_reset_button = document.getElementById("attachment_reset_button");
    attachment_reset_button.disabled = false;



    await attachment_setup_file_list();
}

async function attachment_setup_file_list() 
{

    if(g_is_setup_started) return;

    g_is_setup_started = true;



    g_host_state = null;

    let result = false;

    let is_pdf = false;


    let temp = [];
    let temp_contents = [];

    if (g_file_stat_list.length < 2) 
    {
        //g_validation_errors.add("need at least 2 IJE files. MOR and NAT or FET");
    }

    result = true;

    const file_html = [];
    const valid_file_format = /\d\d\d\d\d\d\d\d.pdf/;
    for (let i = 0; i < g_file_stat_list.length; i++) 
    {
        const item = g_file_stat_list[i];
        if (typeof item !== "undefined") 
        {
            let is_valid = "Valid File Name: "
            if (valid_file_format.test(item.name.toLowerCase())) 
            {
                //g_cdc_identifier_set = {};
                
                result = result && true;

                g_is_file_valid_list.push(true)

            }
            else 
            {
                g_is_file_valid_list.push(false)
                is_valid = "X invalid File Name: "
                result = false;
            }



            file_html.push(`<li>${is_valid}${item.name}</li>`)
        }
    }
/*
    if (!is_pdf) 
    {
        g_validation_errors.add("missing .MOR IJE file")
    }
*/


    //g_file_stat_list = temp;
    //g_content_list = temp_contents;

    const el2 = document.getElementById("attachment_upload_button")
    const el3 = document.getElementById("attachment_upload_list")

    const el4 = document.getElementById("override-existing-files")
    

    if(result)
    {
        el2.disabled = false;
        el4.disabled = false;
    }
    else
    {
        el2.disabled = true;
        el4.disabled = true;
    }



    el3.innerHTML = file_html.join("");

    return result;

}

let g_file_info_list = []
async function Attachment_GetFileList(p_id)
{
    const url = `${location.protocol}//${location.host}/attachment/GetDocumentList?id=${p_id}`;
    const response = await $.ajax
    (
        {
            url: url,
            type: "GET"
        }
    );

    g_file_info_list = response;




}

async function attachment_reset_button_click()
{
    const el = document.getElementById("attachment_upload_button")
    el.disabled = true;
    
    const el2 = document.getElementById("override-existing-files")
    el2.disabled = true;

    const files_el = document.getElementById("files")
    files_el.disabled = false
    files_el.value= null;

    g_file_stat_list.length = 0;
    g_is_setup_started = false;

    const attachment_upload_list_el = document.getElementById("attachment_upload_list")
    attachment_upload_list_el.innerHTML = "";

    const attachment_reset_button = document.getElementById("attachment_reset_button");
    attachment_reset_button.disabled = true;

    const output_area = document.getElementById("output_area_id");
    output_area.innerHTML = "";


}

function render_file_info_list()
{
    const result = [];
/*
    if(g_file_info_list.length == 0)
    {
        result.push
        (`
            <li> Select pdf you want to upload.  PDF should have PMSNO as name.</li>    
        `);
    }
    else for( const i in g_file_info_list)
    {
        const item = g_file_info_list[i];
        
        result.push
        (`
            <li>${item.name} <a target="attachement" href="${location.protocol}//${location.host}/attachment/GetFileResult?f=${item.path}">download</a>  | <a href="javascript:on_delete_button_clicked('${location.protocol}//${location.host}/attachment/DeleteFile?f=${item.path}')">delete</a></li>    
        `);
    }
*/

    result.push
    (`
        <li> Select pdf you want to upload.  PDF should have PMSSNO as name (JJYY####.pdf).</li> 
        <li style="text-align:center;">
            <label for="files" class="sr-only">Upload files</label>
            <input type="file" id="files" class="form-control p-1 h-auto" name="files[]" onchange="attachment_readmultifiles(event, this.files)" multiple />
        </li>
        <li>
            <ul id="attachment_upload_list">
            </ul>
        </li>
        <li style="text-align:center;">
            <label><input type="checkbox" id="override-existing-files" value="override_existing" disabled>Override Existing PDF(s)</label> <input type="button" value="upload" id="attachment_upload_button" disabled onclick="send_pdf_set()" /> | 
            <input type="button" value="reset" id="attachment_reset_button" onclick="attachment_reset_button_click()" disabled="true" />
        </li>

        
    `);

    return result.join("");
}

async function send_pdf_set() 
{

    

    const override_control = document.getElementById("override-existing-files")
    

    const post_central_file_resquest = {
        
        "override_existing": override_control.checked,
        "file_name_list": [],
        "file_data_list": []
    };

    for(let i = 0; i < g_file_stat_list.length; i++)
    {
        post_central_file_resquest.file_name_list.push(g_file_stat_list[i].name);
        post_central_file_resquest.file_data_list.push(g_file_content_list[i]);
    }

    const response = await $.ajax({
        url: `${location.protocol}//${location.host}/attachment/CentralFileUpload`,
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify(post_central_file_resquest),
        type: "POST"
    });

    //console.log(response);
    if(response.ok)
    {
        await attachment_reset_button_click();


    }

    const output_area = document.getElementById("output_area_id");

    output_area.innerHTML= `<textarea cols=80 rows=10>${JSON.stringify(response, null, 2)}</textarea>`
}

async function on_delete_button_clicked(p_path)
{
    //console.log(p_path)

    const response = await $.ajax({
        url: p_path,
        contentType: 'application/json; charset=utf-8',
        type: "GET"
    });

   if(response.ok)
   {
        await Attachment_GetFileList(g_data._id);

        const el = document.getElementById("attachment_file_info_list")
        el.innerHTML = render_file_info_list();


   }

}