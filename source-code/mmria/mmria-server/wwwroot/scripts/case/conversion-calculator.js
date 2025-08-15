

let cc_cm_input = null;
let cc_in_input = null;

let cc_m_input = null;
let cc_ft_input = null;

let cc_lbs_input = null;
let cc_kg_input = null;

let cc_oz_input = null;
let cc_g_input = null;

let cc_f_input = null;
let cc_c_input = null;

let cc_reset_input = null;
let cc_convert_input = null;

let cc_reset_message = null;

function cc_main()
{
    //if(cc_c_input != null) return;
    
    cc_cm_input = document.getElementById("cc_cm");
    cc_in_input = document.getElementById("cc_in");
    cc_m_input = document.getElementById("cc_m");
    cc_ft_input = document.getElementById("cc_ft");
    cc_lbs_input = document.getElementById("cc_lbs");
    cc_kg_input = document.getElementById("cc_kg");
    cc_oz_input = document.getElementById("cc_oz");
    cc_g_input = document.getElementById("cc_g");
    cc_f_input = document.getElementById("cc_f");
    cc_c_input = document.getElementById("cc_c");
    cc_reset_input = document.getElementById("cc_reset");

    cc_reset_message = document.getElementById("cc_reset_message");
    cc_reset_message.style.display = "none";

    cc_convert_input= document.getElementById("cc_convert")
    cc_convert_input.disabled = true;

    cc_cm_input.addEventListener("keypress",cc_only_numeric_input);
    cc_in_input.addEventListener("keypress",cc_only_numeric_input);
    cc_m_input.addEventListener("keypress",cc_only_numeric_input);
    cc_ft_input.addEventListener("keypress",cc_only_numeric_input);
    cc_lbs_input.addEventListener("keypress",cc_only_numeric_input);
    cc_kg_input.addEventListener("keypress",cc_only_numeric_input);
    cc_oz_input.addEventListener("keypress",cc_only_numeric_input);
    cc_g_input.addEventListener("keypress",cc_only_numeric_input);
    cc_f_input.addEventListener("keypress",cc_only_numeric_input);
    cc_c_input.addEventListener("keypress",cc_only_numeric_input);


    cc_cm_input.addEventListener("keydown",cc_onKeyDown);
    cc_in_input.addEventListener("keydown",cc_onKeyDown);
    cc_m_input.addEventListener("keydown",cc_onKeyDown);
    cc_ft_input.addEventListener("keydown",cc_onKeyDown);
    cc_lbs_input.addEventListener("keydown",cc_onKeyDown);
    cc_kg_input.addEventListener("keydown",cc_onKeyDown);
    cc_oz_input.addEventListener("keydown",cc_onKeyDown);
    cc_g_input.addEventListener("keydown",cc_onKeyDown);
    cc_f_input.addEventListener("keydown",cc_onKeyDown);
    cc_c_input.addEventListener("keydown",cc_onKeyDown);

    cc_cm_input.focus();

}


function cc_reset_clicked()
{

    cc_cm_input.value = "";
    cc_in_input.value = "";
    cc_m_input.value = "";
    cc_ft_input.value = "";
    cc_lbs_input.value = "";
    cc_kg_input.value = "";
    cc_oz_input.value = "";
    cc_g_input.value = "";
    cc_f_input.value = "";
    cc_c_input.value = "";
    
    cc_cm_input.readOnly = false;
    cc_in_input.readOnly = false;
    cc_m_input.readOnly = false;
    cc_ft_input.readOnly = false;
    cc_lbs_input.readOnly = false;
    cc_kg_input.readOnly = false;
    cc_oz_input.readOnly = false;
    cc_g_input.readOnly = false;
    cc_f_input.readOnly = false;
    cc_c_input.readOnly = false;
        
    cc_reset_message.style.display = "none";

    cc_render_convert();
}


function cc_render_convert(p_value, p_length)
{
    let is_able_to_convert = false;

    if
    (
        cc_cm_input.readOnly === true &&
        cc_in_input.readOnly === true &&
        cc_m_input.readOnly === true &&
        cc_ft_input.readOnly === true &&
        cc_lbs_input.readOnly === true &&
        cc_kg_input.readOnly === true &&
        cc_oz_input.readOnly === true &&
        cc_g_input.readOnly === true &&
        cc_f_input.readOnly === true &&
        cc_c_input.readOnly === true 
    )
    {
        // do nothing
    }
    else if(p_value != null)
    {
        if(p_length != null)
        {

            if(p_length == 0)
            {
                if(p_value === -2)
                {
                    is_able_to_convert = false;
                }
                else
                {
                    is_able_to_convert = is_able_to_convert || (p_value !== '' && p_value >= 0);
                }
                
            }
            else 
            {
                is_able_to_convert = is_able_to_convert || 
                (
                    p_value !== '' && 
                    (p_value >= 0 || p_value === -2)
                );
            }
        }
        else
        {
            is_able_to_convert = is_able_to_convert || (p_value !== '' && p_value >= 0);
        }
    } 
    else
    {
        is_able_to_convert = is_able_to_convert || (cc_cm_input.value != null && cc_cm_input.value !== '' && cc_cm_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_in_input.value != null && cc_in_input.value !== '' && cc_in_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_m_input.value != null && cc_m_input.value !== '' && cc_m_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_ft_input.value != null && cc_ft_input.value !== '' && cc_ft_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_lbs_input.value != null && cc_lbs_input.value !== '' && cc_lbs_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_kg_input.value != null && cc_kg_input.value !== '' && cc_kg_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_oz_input.value != null && cc_oz_input.value !== '' && cc_oz_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_g_input.value != null && cc_g_input.value !== '' && cc_g_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_f_input.value != null && cc_f_input.value !== '' && cc_f_input.value !== '.');
        is_able_to_convert = is_able_to_convert || (cc_c_input.value != null && cc_c_input.value !=='' && cc_c_input.value !=='.');
    }


    
    if(cc_cm_input.value != null && cc_cm_input.value !== '')
    {
        cc_in_input.readOnly = true;
    }
    else if(cc_in_input.value != null && cc_in_input.value !== '')
    {
        cc_cm_input.readOnly = true;
    }

    if(cc_m_input.value != null && cc_m_input.value !== '')
    {
        cc_ft_input.readOnly = true;
    }
    else if(cc_ft_input.value != null && cc_ft_input.value !== '')
    {
        cc_m_input.readOnly = true;
    }

    if(cc_lbs_input.value != null && cc_lbs_input.value !== '')
    {
        cc_kg_input.readOnly = true;
    }
    else if(cc_kg_input.value != null && cc_kg_input.value !== '')
    {
        cc_lbs_input.readOnly = true;
    }

    if(cc_oz_input.value != null && cc_oz_input.value !== '')
    {
        cc_g_input.readOnly = true;
    }
    else if(cc_g_input.value != null && cc_g_input.value !== '')
    {
        cc_oz_input.readOnly = true;
    }

    if(cc_f_input.value != null && cc_f_input.value !== '')
    {
        cc_c_input.readOnly = true;
    }
    else if(cc_c_input.value != null && cc_c_input.value !== '')
    {
        cc_f_input.readOnly = true;
    }

    
    


    if(is_able_to_convert)
    {
        cc_convert_input.disabled = false;
    }
    else
    {
        cc_convert_input.disabled = true;
    }
    
}

function cc_convert_clicked()
{

    cc_cm_input.readOnly = true;
    cc_in_input.readOnly = true;
    cc_m_input.readOnly = true;
    cc_ft_input.readOnly = true;
    cc_lbs_input.readOnly = true;
    cc_kg_input.readOnly = true;
    cc_oz_input.readOnly = true;
    cc_g_input.readOnly = true;
    cc_f_input.readOnly = true;
    cc_c_input.readOnly = true;

    if
    (
        cc_cm_input.value != null && 
        cc_cm_input.value != '' &&
        (
            cc_in_input.value == null || 
            cc_in_input.value == ''
        )
    )
    {

        if(cc_cm_input.value < 0) cc_cm_input.value = 0.00;
        if(cc_cm_input.value > 1000) cc_cm_input.value = 1000.00;

        cc_in_input.value = cc_cm_to_in(cc_cm_input.value).toFixed(2);
    }
    else if
    (
        cc_in_input.value != null && 
        cc_in_input.value != '' &&
        (
            cc_cm_input.value == null || 
            cc_cm_input.value == '' 
        )
    )
    {
        if(cc_in_input.value < 0) cc_in_input.value = 0.00;
        if(cc_in_input.value > 1000) cc_in_input.value = 1000.00;

        cc_cm_input.value = cc_in_to_cm(cc_in_input.value).toFixed(2);
    }

    if
    (
        cc_m_input.value != null && 
        cc_m_input.value != '' &&
        (
            cc_ft_input.value == null || 
            cc_ft_input.value == ''
        )
    )
    {
        if(cc_m_input.value < 0) cc_m_input.value = 0.00;
        if(cc_m_input.value > 1000) cc_m_input.value = 1000.00;

        cc_ft_input.value = cc_m_to_ft(cc_m_input.value).toFixed(2);
    }
    else if
    (
        cc_ft_input.value != null && 
        cc_ft_input.value != '' &&
        (
            cc_m_input.value == null || 
            cc_m_input.value == ''
        )
    )
    {
        if(cc_ft_input.value < 0) cc_ft_input.value = 0.00;
        if(cc_ft_input.value > 1000) cc_ft_input.value = 1000.00;

        cc_m_input.value = cc_ft_to_m(cc_ft_input.value).toFixed(2);
    }

    if
    (
        cc_lbs_input.value != null && 
        cc_lbs_input.value != '' &&
        (
            cc_kg_input.value == null ||
            cc_kg_input.value == ''
        )
    )
    {
        if(cc_lbs_input.value < 0) cc_lbs_input.value = 0.00;
        if(cc_lbs_input.value > 1000) cc_lbs_input.value = 1000.00;

        cc_kg_input.value = cc_lbs_to_kg(cc_lbs_input.value).toFixed(2);
    }
    else if
    (
        cc_kg_input.value != null && 
        cc_kg_input.value != '' &&
        (
            cc_lbs_input.value == null ||
            cc_lbs_input.value == ''
        )
    )
    {
        if(cc_kg_input.value < 0) cc_kg_input.value = 0.00;
        if(cc_kg_input.value > 1000) cc_kg_input.value = 1000.00;

        cc_lbs_input.value = cc_kg_to_lbs(cc_kg_input.value).toFixed(2);
    }

    if
    (
        cc_oz_input.value != null && 
        cc_oz_input.value != '' &&
        (
            cc_g_input.value == null ||
            cc_g_input.value == ''
        )
    )
    {
        if(cc_oz_input.value < 0) cc_oz_input.value = 0.00;
        if(cc_oz_input.value > 1000) cc_oz_input.value = 1000.00;

        cc_g_input.value = cc_oz_to_g(cc_oz_input.value).toFixed(2);
    }
    else if
    (
        cc_g_input.value != null && 
        cc_g_input.value != '' &&
        (
            cc_oz_input.value == null || 
            cc_oz_input.value == '' 
        )
    )
    {
        if(cc_g_input.value < 0) cc_g_input.value = 0.00;
        if(cc_g_input.value > 1000) cc_g_input.value = 1000.00;

        cc_oz_input.value = cc_g_to_oz(cc_g_input.value).toFixed(2);
    }

    if
    (
        cc_f_input.value != null && 
        cc_f_input.value != '' &&
        (
            cc_c_input.value == null ||
            cc_c_input.value == ''
        )
    )
    {
        if(cc_f_input.value < 0) cc_f_input.value = 0.00;
        if(cc_f_input.value > 1000) cc_f_input.value = 1000.00;

        cc_c_input.value = cc_f_to_c(cc_f_input.value).toFixed(2);
    }
    else if
    (
        cc_c_input.value != null && 
        cc_c_input.value != '' &&
        (
            cc_f_input.value == null ||
            cc_f_input.value == '' 
        )
    )
    {
        if(cc_c_input.value < 0) cc_c_input.value = 0.00;
        if(cc_c_input.value > 1000) cc_c_input.value = 1000.00;

        cc_f_input.value = cc_c_to_f(cc_c_input.value).toFixed(2);
    }
    
    cc_reset_message.style.display = "block";
    cc_convert_input.disabled = true;
    
    //cc_render_convert();
    
}

function cc_cm_to_in(p_value)
{
    //return p_value  * 0.39;
    return p_value  * 0.3937007874;
}
function cc_in_to_cm(p_value)
{
    return p_value * 2.54;
}



function cc_m_to_ft(p_value)
{
    return p_value * 3.28; //3.28084
}

function cc_ft_to_m(p_value)
{
    return p_value * 0.304; //0.3048
}



function cc_lbs_to_kg(p_value)
{
    return p_value * 0.45; //0.4535924
}
function cc_kg_to_lbs(p_value)
{
    return p_value * 2.20; //2.204623
}


function cc_oz_to_g(p_value)
{
    return p_value * 28.35; //28.34952
}
function cc_g_to_oz(p_value)
{
    //return p_value * 0.04; //0.03527396
    return p_value * 0.03527396;
}



function cc_f_to_c(p_value)
{
    return (p_value - 32) / 1.8000
}

function cc_c_to_f(p_value)
{
    return p_value * 1.8000 + 32
}


function cc_only_numeric_input(evt)
{
    const cr_enter = 13;
    const zero = 48;
    const dot = 46;
    const nine = 57

    if // preventDefault if
    (
        evt.srcElement.value.length > 12 ||
        (
            evt.which == dot &&
            evt.srcElement.value.indexOf('.') > -1
        )
        ||
        evt.srcElement.readOnly == true
        ||
        /*(
            evt.srcElement.value.length == 0 &&
            (evt.which == dot || evt.which == zero)
        )
        ||
        (
            evt.srcElement.value.length > 0 &&
            parseInt(evt.srcElement.value) == 0 &&
            evt.which == dot 
        )
        ||
        (
            evt.which < zero || 
            evt.which > nine
            
        ) 
        &&*/
        (
            evt.which != dot &&
            evt.which != cr_enter &&
            
            (
                evt.which < zero || 
                evt.which > nine
            )
        )
    )
    {
        //console.log(evt.which);
        evt.preventDefault();
    }
    else if 
    (
        evt.which == cr_enter &&
        evt.srcElement.value.length == 0 
    )
    {

    }
    else 
    {
        cc_render_convert(evt.which - zero, evt.srcElement.value.length);
        if (evt.srcElement.value.readOnly == false)
        switch(evt.srcElement.id)
        {

            case "cc_cm":
                cc_in_input.readOnly = true;
            break;
            case "cc_in":
                cc_cm_input.readOnly = true;
            break;
            case "cc_m":
                cc_ft_input.readOnly = true;
            break;
            case "cc_ft":
                cc_m_input.readOnly = true;
            break;
            case "cc_lbs":
                cc_kg_input.readOnly = true;
            break;
            case "cc_kg":
                cc_lbs_input.readOnly = true;
            break;
            case "cc_oz":
                cc_g_input.readOnly = true;
            break;
            case "cc_g":
                cc_oz_input.readOnly = true;
            break;
            case "cc_f":
                cc_c_input.readOnly = true;
            break;
            case "cc_c":
                cc_f_input.readOnly = true;
            break;     
        }
    }
    
}


function cc_onKeyDown(event) 
{
    const BACKSPACE = 8;
    const TAB = 9;
    var key = event.keyCode || event.charCode;

    

    if( key == BACKSPACE)
    {
        if(event.srcElement.value.length <= 2)
        {
            //console.log(event.srcElement.id)
            if(event.srcElement.value.length == 0)
            {
                cc_render_convert(-2, 0);
            }
            else if(event.srcElement.value.length == 2)
            {
                const val = event.srcElement.value[0];
                if(val === ".")
                {
                    cc_render_convert(-2, 0);
                }
                else
                {
                    cc_render_convert(val, 0);
                }
            }
            else
            {
                
                cc_render_convert('', event.srcElement.value.length);
            }
            switch(event.srcElement.id)
            {

                case "cc_cm":
                    cc_in_input.readOnly = false;
                break;
                case "cc_in":
                    cc_cm_input.readOnly = false;
                break;
                case "cc_m":
                    cc_ft_input.readOnly = false;
                break;
                case "cc_ft":
                    cc_m_input.readOnly = false;
                break;
                case "cc_lbs":
                    cc_kg_input.readOnly = false;
                break;
                case "cc_kg":
                    cc_lbs_input.readOnly = false;
                break;
                case "cc_oz":
                    cc_g_input.readOnly = false;
                break;
                case "cc_g":
                    cc_oz_input.readOnly = false;
                break;
                case "cc_f":
                    cc_c_input.readOnly = false;
                break;
                case "cc_c":
                    cc_f_input.readOnly = false;
                break;     
            }
        }
        else
        {
            switch(event.srcElement.id)
            {

                case "cc_cm":
                    cc_in_input.readOnly = true;
                break;
                case "cc_in":
                    cc_cm_input.readOnly = true;
                break;
                case "cc_m":
                    cc_ft_input.readOnly = true;
                break;
                case "cc_ft":
                    cc_m_input.readOnly = true;
                break;
                case "cc_lbs":
                    cc_kg_input.readOnly = true;
                break;
                case "cc_kg":
                    cc_lbs_input.readOnly = true;
                break;
                case "cc_oz":
                    cc_g_input.readOnly = true;
                break;
                case "cc_g":
                    cc_oz_input.readOnly = true;
                break;
                case "cc_f":
                    cc_c_input.readOnly = true;
                break;
                case "cc_c":
                    cc_f_input.readOnly = true;
                break;     
            }
        }
    }
    else if( key == TAB)
    {
        cc_render_convert();
    }
};