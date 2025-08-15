function textarea_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
    if
    (
        p_metadata.name == "notes_about_key_circumstances_surrounding_death" &&
        (
            p_data == null ||
            p_data == ""
        )
    )
    {
        // Do something
    }
    else
    {
        p_result.push("<div class='textarea' id='");
        p_result.push(convert_object_path_to_jquery_id(p_object_path));
        p_result.push("'");
        p_result.push(" mpath='");
        p_result.push(p_metadata_path);
        p_result.push("' ");
        p_result.push(">");

        p_result.push(`<label for="${convert_object_path_to_jquery_id(p_object_path)}_control" `);
        if(p_metadata.description && p_metadata.description.length > 0)
        {
            p_result.push("rel='tooltip' data-original-title='");
            p_result.push(p_metadata.description.replace(/'/g, "\\'"));
            p_result.push("'");
        }

        var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

        if(style_object && p_metadata.name != "case_opening_overview")
        {
            p_result.push(" style='");
            p_result.push(get_style_string(style_object.prompt.style));
            p_result.push("'");
        }
        p_result.push(">");

        let prompt = p_metadata.prompt;
        if
        (
            p_metadata.type.toLowerCase() == 'textarea' &&
            p_metadata.max_length != null &&
            parseInt(p_metadata.max_length) > 0 &&
            p_metadata.is_display_field_length != null &&
            p_metadata.is_display_field_length == true
        )
        {

            let is_highlight_border = false;

            if
            (

                p_data != null &&
                p_data.toString().length >= parseInt(p_metadata.max_length) 
            )
            {
                is_highlight_border = true;
                prompt += ` <span style='color: #BB6C49;'>(Max ${p_metadata.max_length} characters)</span>`
            }
            else
            {
                prompt += ` (Max ${p_metadata.max_length} characters)`
            }


            
        }

            p_result.push(prompt);
            p_result.push
            (`
                ${render_data_analyst_dictionary_link
                (
                    p_metadata, 
                    p_dictionary_path
                )}
            `);
        p_result.push("</label>");



        

        if(p_metadata.name == "case_opening_overview")
        {
            page_render_create_textarea(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path);

            let opts = {
                btns: [
                    ['viewHTML'],
                    ['undo', 'redo'],
                    ['strong', 'em', 'underline', 'del'],
                    ['fontsize'],
                    ['foreColor', 'backColor'],
                    ['justifyLeft', 'justifyCenter', 'justifyRight'],
                    ['unorderedList', 'orderedList'],
                    ['horizontalRule'],
                    ['removeformat'],
                    ['fullscreen'],
                ],
                plugins: {
                    // Add font sizes manually
                    fontsize: {
                        sizeList: [
                            '14px',
                            '16px',
                            '18px',
                            '24px',
                            '32px',
                            '48px'
                        ],
                        allowCustomSize: false
                    },
                    // Add colors manually
                    // Currently utilizing all primary, secondary, tertiary colors in color wheel
                    colors: {
                        colorList: [
                            'FFFFFF',
                            'CCCCCC',
                            '777777',
                            '333333',
                            '000000',
                            'FF0000',
                            '00FF00',
                            '0000FF',
                            'FFFF00',
                            'FF00FF',
                            '00FFFF',
                            'FF7F00',
                            'FF007F',
                            '7FFF00',
                            '7F00FF',
                            '00FF7F',
                            '007FFF'
                        ]
                    }
                },
                semantic: true
            }

            p_post_html_render.push(`$('#case_narrative_editor').trumbowyg(${JSON.stringify(opts)});`);
            
            p_post_html_render.push(`
                $('#case_narrative_editor')
                .trumbowyg()
                .on('tbwchange', function ()
                {
                    tbw_onchange("${p_object_path}","${p_metadata_path}","${p_dictionary_path}");
                })
                .on('tbwpaste', function ()
                {
                    tbw_change_paste("${p_object_path}","${p_metadata_path}","${p_dictionary_path}");
                });
            `);

        }
        else
        {
            page_render_create_textarea(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path);
        }

        p_result.push("</div>");
    }
}



function tbw_change_paste(p_object_path, p_metadata_path, p_dictionary_path)
{
    let data = $('.trumbowyg-editor').html();

    //g_textarea_oninput(p_object_path, p_metadata_path,p_dictionary_path, data);
    //return;

    let crlf_regex = /\n/g;

    if(data!= null)
    {
        data = data.replace(crlf_regex, " ");
    }
 

    let new_text = textarea_control_strip_html_attributes(data);

    if
    (
        new_text == null && data.length > 0 ||
        new_text.length == 0 && data.length != 0
    )
    {
        console.log("tbw_change_paste null error");
        new_text = data;
    }

    g_textarea_oninput(p_object_path, p_metadata_path,p_dictionary_path, new_text);
}

function tbw_onchange(p_object_path, p_metadata_path, p_dictionary_path)
{
    let data = $('.trumbowyg-editor').html();

    //g_textarea_oninput(p_object_path, p_metadata_path,p_dictionary_path, data);
    //return;

    let new_text = textarea_control_strip_html_attributes(data);

    if
    (
        new_text == null && data.length > 0 ||
        new_text.length == 0 && data.length != 0
    )
    {
        console.log("tbw_change_paste null error");
        new_text = data;
    }

    g_textarea_oninput(p_object_path, p_metadata_path,p_dictionary_path, new_text);
}


function textarea_control_replace_return_with_br(p_value)
{
    let crlf_regex = /\n/g;

    let result = p_value;

    if(p_value!= null)
    {
        result = p_value.replace(crlf_regex, "<br/>");
    }

    return result;
}

function textarea_control_strip_html_attributes(p_value)
{

    let CommentRegex = /<!--\[[^>]+>/gi;

    let Strip5PlusBr = /<br\><br\><br\><br\>+/gi;

    let StripTrailingBR = /<br><br>(<br>|<br>.?)+/gi;

    const Replace1 = /<br><\/p>/gi;
    const Replace2 = /<br><\/span>/gi;
    const Replace3 = /<p><span [^>]+><\/span><\/p>/gi;
    const Replace4 = /<span [^>]+><\/span>/gi;

    let PseudoTagRegex = /<\/?[a-z]:[^>]+>/gi;

    let crlf_regex = /\n/g;

    let node = document.createElement("body");
    node.innerHTML = p_value.replace(CommentRegex,"")
        .replace(crlf_regex," ")
        .replace(Strip5PlusBr,"<br><br>")
        .replace(StripTrailingBR,"")
        .replace(PseudoTagRegex,"")
        .replace(Replace1, "</p>")
        .replace(Replace2, "</span>")
        .replace(Replace3,"")
        .replace(Replace4,"").trim();

    DOMWalker(node);

    return node.innerHTML;
    
}

const AcceptableTag = {
    "body":true,
    "div":true,
    "p":true,
    "em":true,
    "strong":true,
    "u":true,
    "ul":true,
    "ol":true,
    "li":true,
    "br":true,
    "del":true,
    "hr":true,
    "span":true,
    "table":true,
    "th":true,
    "td":true,
    "tbody":true
}

function DOMWalker(p_node)
{
    //console.log(`${p_node.nodeType} = ${p_node.nodeName}`);

    if
    (
        AcceptableTag[p_node.nodeName.toLowerCase()] == null
    )
    {
        if(p_node.nodeName.toLowerCase() != "#text")
        {
            //console.log(`${p_node.nodeType} = ${p_node.nodeName}`);
        }
    }

    if(p_node.attributes != null)
    {
        let remove_list = [];

        for(let i = 0; i < p_node.attributes.length; i++)
        {
            let attr = p_node.attributes[i];
           
            if(attr.name != "style")
            {
                //console.log(`${attr.name} = ${attr.value}`);
                remove_list.push(attr.name);
            }

            if(attr.value.trim() != "")
            {
                let VarRegex =/var.*\(--([a-z ]+)\)/gi;
                let array = attr.value.split(";")
                let new_array = [];
                for(let array_index = 0; array_index < array.length; array_index++)
                {
                    let att_value = array[array_index];
                    let name_value = att_value.split(":");
                    if(att_value.match(VarRegex)!= null)
                    {
                        let new_att_value = colourNameToHex(name_value[1].replace(VarRegex,"$1").trim());
                        
                        new_array.push(`${name_value[0].trim()}:${new_att_value}`);

                    }
                    else if(att_value.trim().indexOf("color")== 0)
                    {
                        if(name_value[1].trim().indexOf("#") != 0)
                        {
                            let new_att_value = colourNameToHex(name_value[1].trim());
                        
                            new_array.push(`${name_value[0].trim()}:${new_att_value}`);
                        }
                        else
                        {
                            new_array.push(att_value);
                        }
                    }
                    else if(att_value.trim().indexOf("font-size")== 0)
                    {
                        if(name_value[1].trim().endsWith("rem"))
                        {
                            new_array.push(`font-size:12`);
                        }
                        else
                        {
                            new_array.push(att_value);
                        }
                    }
                    else
                    {
                        new_array.push(att_value);
                    }
                    
                }
                attr.value = new_array.join(";")
            }


        }

        remove_list.reverse ();
        for(let i = 0; i < remove_list.length; i++)
        {
            
            p_node.removeAttribute(remove_list[i]);
        }
    }

    for(let i = 0; i < p_node.childNodes.length; i++)
    {
        let child = p_node.childNodes[i];

        DOMWalker(child);
    }


}

function colourNameToHex(colour)
{
    var colours = {
    "aliceblue":"#f0f8ff","antiquewhite":"#faebd7","aqua":"#00ffff","aquamarine":"#7fffd4","azure":"#f0ffff",
    "beige":"#f5f5dc","bisque":"#ffe4c4","black":"#000000","blanchedalmond":"#ffebcd","blue":"#0000ff","blueviolet":"#8a2be2","brown":"#a52a2a","burlywood":"#deb887",
    "cadetblue":"#5f9ea0","chartreuse":"#7fff00","chocolate":"#d2691e","coral":"#ff7f50","cornflowerblue":"#6495ed","cornsilk":"#fff8dc","crimson":"#dc143c","cyan":"#00ffff",
    "darkblue":"#00008b","darkcyan":"#008b8b","darkgoldenrod":"#b8860b","darkgray":"#a9a9a9","darkgreen":"#006400","darkkhaki":"#bdb76b","darkmagenta":"#8b008b","darkolivegreen":"#556b2f",
    "darkorange":"#ff8c00","darkorchid":"#9932cc","darkred":"#8b0000","darksalmon":"#e9967a","darkseagreen":"#8fbc8f","darkslateblue":"#483d8b","darkslategray":"#2f4f4f","darkturquoise":"#00ced1",
    "darkviolet":"#9400d3","deeppink":"#ff1493","deepskyblue":"#00bfff","dimgray":"#696969","dodgerblue":"#1e90ff",
    "firebrick":"#b22222","floralwhite":"#fffaf0","forestgreen":"#228b22","fuchsia":"#ff00ff",
    "gainsboro":"#dcdcdc","ghostwhite":"#f8f8ff","gold":"#ffd700","goldenrod":"#daa520","gray":"#808080","green":"#008000","greenyellow":"#adff2f",
    "honeydew":"#f0fff0","hotpink":"#ff69b4",
    "indianred ":"#cd5c5c","indigo":"#4b0082","ivory":"#fffff0","khaki":"#f0e68c",
    "lavender":"#e6e6fa","lavenderblush":"#fff0f5","lawngreen":"#7cfc00","lemonchiffon":"#fffacd","lightblue":"#add8e6","lightcoral":"#f08080","lightcyan":"#e0ffff","lightgoldenrodyellow":"#fafad2",
    "lightgrey":"#d3d3d3","lightgreen":"#90ee90","lightpink":"#ffb6c1","lightsalmon":"#ffa07a","lightseagreen":"#20b2aa","lightskyblue":"#87cefa","lightslategray":"#778899","lightsteelblue":"#b0c4de",
    "lightyellow":"#ffffe0","lime":"#00ff00","limegreen":"#32cd32","linen":"#faf0e6",
    "magenta":"#ff00ff","maroon":"#800000","mediumaquamarine":"#66cdaa","mediumblue":"#0000cd","mediumorchid":"#ba55d3","mediumpurple":"#9370d8","mediumseagreen":"#3cb371","mediumslateblue":"#7b68ee",
    "mediumspringgreen":"#00fa9a","mediumturquoise":"#48d1cc","mediumvioletred":"#c71585","midnightblue":"#191970","mintcream":"#f5fffa","mistyrose":"#ffe4e1","moccasin":"#ffe4b5",
    "navajowhite":"#ffdead","navy":"#000080",
    "oldlace":"#fdf5e6","olive":"#808000","olivedrab":"#6b8e23","orange":"#ffa500","orangered":"#ff4500","orchid":"#da70d6",
    "palegoldenrod":"#eee8aa","palegreen":"#98fb98","paleturquoise":"#afeeee","palevioletred":"#d87093","papayawhip":"#ffefd5","peachpuff":"#ffdab9","peru":"#cd853f","pink":"#ffc0cb","plum":"#dda0dd","powderblue":"#b0e0e6","purple":"#800080",
    "rebeccapurple":"#663399","red":"#ff0000","rosybrown":"#bc8f8f","royalblue":"#4169e1",
    "saddlebrown":"#8b4513","salmon":"#fa8072","sandybrown":"#f4a460","seagreen":"#2e8b57","seashell":"#fff5ee","sienna":"#a0522d","silver":"#c0c0c0","skyblue":"#87ceeb","slateblue":"#6a5acd","slategray":"#708090","snow":"#fffafa","springgreen":"#00ff7f","steelblue":"#4682b4",
    "tan":"#d2b48c","teal":"#008080","thistle":"#d8bfd8","tomato":"#ff6347","turquoise":"#40e0d0",
    "violet":"#ee82ee",
    "wheat":"#f5deb3","white":"#ffffff","whitesmoke":"#f5f5f5",
    "yellow":"#ffff00","yellowgreen":"#9acd32"};

    if (typeof colours[colour.toLowerCase()] != 'undefined')
        return colours[colour.toLowerCase()];

    return false;
}


function textarea_control_strip_html_attributes2(p_value)
{
    let AttributeRegEx = /[a-zA-Z]+='[^']+'|[a-zA-Z]+=\"[^\"]+\"/gi;
    

    let PseudoTagRegex = /<\/?[a-z]:[^>]+>/gi;

    let CommentRegex = /<!--\[[^>]+>/gi;


    //let StripTrailBlankSpaceExp = /(<\/?([ ])+[^>]+>)/gi;

    let Strip5PlusBr = /<br\><br\><br\><br\>+/gi;

    let StripTrailBlankSpaceExp = /<\/?[a-zA-Z]+([ ]+)[^>]+>/gi;

    let result = p_value.replace(AttributeRegEx,"")
        .replace(PseudoTagRegex, "").
        replace(CommentRegex,"")
        .replace(StripTrailBlankSpaceExp, "")
        .replace(Strip5PlusBr,"<br>");

    return result;

}