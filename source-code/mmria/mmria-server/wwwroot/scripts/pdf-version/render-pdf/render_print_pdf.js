var g_size = {};
var g_layout = {};
var g_post_render = [];


function get_simple_ctx(
	p_metadata, 
	p_data, 
	p_path, 
	p_content, 
	p_section_name,
	p_record_number,
	p_is_grid_item 
)
{
	console.log('get_simple_ctx: ', p_section_name, ' - ', p_path);
    return { 
		metadata: p_metadata, 
		data:p_data, 
		mmria_path: p_path,  
		content: p_content,
		p_section_name,
		p_record_number,
		p_is_grid_item,
	}
}

function get_print_pdf_context(
	p_result, 
	p_post_html_render, 
	p_metadata,
	p_data, 
	p_path, 
	p_metadata_path, 
	p_object_path, 
	p_search_text, 
	p_is_read_only, 
	p_form_index, 
	p_grid_index, 
	p_valid_date_or_datetime, 
	p_entered_date_or_datetime_value
)
{
    let result = {
        result : p_result,
        post_html_render: p_post_html_render,
        metadata:p_metadata, 
        
        data:p_data, 
        mmria_path:p_path,
        metadata_path:p_metadata_path,
        object_path:p_object_path,
        search_text:p_search_text,
        form_index: p_form_index,
        grid_index: p_grid_index,
        is_read_only: p_is_read_only,

        is_valid_date_or_datetime: p_valid_date_or_datetime,
        entered_date_or_datetime_value: p_entered_date_or_datetime_value

    };

    return result;
}

async function initialize_print_pdf(p_ctx) 
{
	console.log('in initialize_print_pdf: ', p_ctx);
    print_pdf_calc_size(p_ctx);
    print_pdf_calc_layout(p_ctx);
    p_ctx.content = [];
    print_pdf_render_content(p_ctx);
    
    pdfMake.createPdf(await dd(p_ctx)).open();
}

async function dd(ctx)
{
	console.log('in dd: ', ctx);
	let p_items = ctx.content;
    let result = {
    pageOrientation: 'landscape',
    pageMargins: [20, 80, 20, 20],
    info: { title: getHeaderName() },
    header: await get_header(1,1, ctx.p_section_name),
    styles: get_style(),
    defaultStyle: { fontSize: 12  },
	content: p_items	
    };

    return result;
}


function get_style()
{
    return {
        pageHeader: {
            fontSize: 18,
            color: '#000080',
        },
        headerPageDate: {
            fontSize: 9,
        },
        formHeader: {
            fontSize: 12,
            color: '#000080',
            margin: [2, 2, 2, 2],
        },
        coreHeader: {
            fontSize: 14,
            color: '#0000ff',
            margin: [0, 10, 0, 5]
        },
        isBold: {
            bold: true,
        },
        fgBlue: {
            color: '#000080',
        },
        isUnderlined: {
            decoration: 'underline',
        },
        lightFill: {
            fillColor: '#dedede',
        },
        blueFill: {
            fillColor: '#cce6ff',
        },
        subHeader: {
            fontSize: 11,
            color: '#000080',
            bold: true,
        },
        tableLargeLabel: {
            color: '#000000',
            fontSize: 10,
        },
        tableLabel: {
            color: '#1010dd',
            fontSize: 9,
            bold: true,
        },
        tableDetail: {
            color: '#000000',
            fontSize: 9,
        },
    };
}

async function print_pdf_render_content(p_ctx) 
{
	// Find the correct type
	console.log('in print_pdf_render_content');
	console.log('p_ctx: ', p_ctx);
	let result = [];
    switch(p_ctx.metadata.type.toLocaleLowerCase())
    {
        case "app":
			console.log('in app');
			result.push([ { text: 'in app', }, ]);
			// Get the Header
			// get_header(0, 1, p_ctx.p_section_name);
            // for(let i = 0; i < p_ctx.metadata.children.length; i++)
            // {
            //     let child = p_ctx.metadata.children[i];
            //     if(p_ctx.data && child.type.toLocaleLowerCase() == "form")
            //     {
            //         //let new_context = get_simple_ctx(child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
            //         let new_context = get_simple_ctx(child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.content);
            //         print_pdf_render_content(new_context);
                    
            //         return;
            //     }
            // }
            break;
        case "form":
			console.log('in form');
			result.push([ [ { text: 'in form', }, ], ]);
			// if(p_ctx.metadata.cardinality == "1" || p_ctx.metadata.cardinality == "?")
            // {
            //     set_form_start(p_ctx.content, p_ctx.metadata);
            //     let form_start = p_ctx.content[p_ctx.content.length -1];
       
            //     for(let i = 0; i < p_ctx.metadata.children.length; i++)
            //     {
            //         let child = p_ctx.metadata.children[i];

            //         if(p_ctx.data && p_ctx.data[child.name])
            //         {

            //             //let new_context = get_simple_ctx(p_ctx.result, p_ctx.post_html_render, child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
            //             let new_content = []
            //             let new_context = get_simple_ctx(child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, new_content);
            //             print_pdf_render_content(new_context);

            //             form_start.table.body.push(new_content);
            //         }

                    
                    
            //     }
            //     return;
            // }
            // else // multiform
            // {

            //     for(let row = 0; row < p_ctx.data.length; row++)
            //     {
            //         let row_data = p_ctx.data[row]
            //         for(let i = 0; i < p_ctx.metadata.children.length; i++)
            //         {
            //             let child = p_ctx.metadata.children[i];
    
            //             if(row_data)
            //             {
            //                 //let new_context = get_simple_ctx(p_ctx.result, p_ctx.post_html_render, child, row_data[child.name], p_ctx.mmria_path + "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "[" + row + "]." + child.name, p_ctx.search_text, p_ctx.is_read_only, row, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
            //                 let new_context = get_simple_ctx(child, row_data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.content);
            //                 new_context.multiform_index = row;
            //                 print_pdf_render_content(new_context);
            //             }
                        
            //         }
            //     }
            // }
            break;
        case "group":
			console.log('in group');
			result.push([ { text: 'in group', }, ]);
			// for(let i = 0; i < p_ctx.metadata.children.length; i++)
            // {
            //     //p_ctx.content.push(get_group_start(p_ctx.metadata.prompt));

            //     p_ctx.content.push({ text: p_ctx.metadata.name, style: ['subHeader'], colSpan: '2', });
               
            //     let group_body = {
            //     layout: {
            //         defaultBorder: false,
            //         paddingLeft: function (i, node) { return 1; },
            //         paddingRight: function (i, node) { return 1; },
            //         paddingTop: function (i, node) { return 2; },
            //         paddingBottom: function (i, node) { return 2; },
            //     },
            //     id: p_ctx.mmria_path,
            //     width: 'auto',
            //     table: {
            //         headerRows: 1,
            //         widths: [250, 'auto'],
            //         body: [
            //             [
            //                 { text: p_ctx.metadata.name, style: ['subHeader'], colSpan: '2', },
            //                 {},
            //             ]
            //         ]
            //         }
            //     };
        

                

            //     let child = p_ctx.metadata.children[i];
            //     if(p_ctx.data)
            //     {
            //         //let new_context = get_simple_ctx(p_ctx.result, p_ctx.post_html_render, child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
            //         let new_context = get_simple_ctx(child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, group_body.table.body);
            //         print_pdf_render_content(new_context);
                    
            //     }
            //     p_ctx.content.push(group_body);

            // }
            break;
        case "grid":
			console.log('in grid');
			result.push([ { text: 'in grid', }, ]);
			// return;
            // for(let i = 0; i < p_ctx.data.length; i++)
            // {
            //     let row_item = p_ctx.data[i];
            //     for(let j in p_ctx.metadata.children)
            //     {
            //         let child = p_ctx.metadata.children[j];
                    
            //         let new_context = get_simple_ctx(p_ctx.result, p_ctx.post_html_render, child, row_item[child.name], p_ctx.mmria_path + "/" + child.name, p_ctx.metadata_path  + ".children[" +j + "]", p_ctx.object_path + "[" + i + "]." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, i, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
            //         print_pdf_render_content(new_context);
            //     }
            
            // }
            break;
        case "string":
        case "number":
        case "time":
			console.log('in string, number, time');
			result.push([ { text: 'in string, number, time', }, ]);

                // //p_ctx.content.push({ text: p_ctx.metadata.prompt });
                // p_ctx.content.push
                // (
                //     [
                //         { text: p_ctx.metadata.prompt, style: ['tableLabel'], alignment: 'right', }
                //         ,{ text: p_ctx.data, style: ['tableDetail'], }
                // ]
                // );
                // //p_ctx.content.push({ text: data });


            
            break;
        case "date":
			console.log('in date');
			result.push([ { text: 'in date', }, ]);

            // p_ctx.content.push
            // (
            //     [
            //         { text: p_ctx.metadata.prompt, style: ['tableLabel'], alignment: 'right', }
            //         ,{ text: p_ctx.data, style: ['tableDetail'], }
            // ]
            // );
            
            break;
        case "datetime":
			console.log('in datetime');
			result.push([ { text: 'in datetime', }, ]);

            // p_ctx.content.push
            // (
            //     [
            //         { text: p_ctx.metadata.prompt, style: ['tableLabel'], alignment: 'right', }
            //         ,{ text: p_ctx.data, style: ['tableDetail'], }
            // ]
            // );
            
            break;
        case "list":
			console.log('in list');
			result.push([ { text: 'in list', }, ]);

            // p_ctx.content.push
            // (
            //     [
            //         { text: p_ctx.metadata.prompt, style: ['tableLabel'], alignment: 'right', }
            //         ,{ text: p_ctx.data, style: ['tableDetail'], }
            // ]
            // );
            
            break;
        case "textarea":
			console.log('in textarea');
			result.push([ { text: 'in textarea', }, ]);
			// p_ctx.content.push
            // (
            //     [
            //         { text: p_ctx.metadata.prompt, style: ['tableLabel'], alignment: 'right', }
            //         ,{ text: p_ctx.data, style: ['tableDetail'], }
            // ]
            // );
            
            break;
		default:
			console.log('in default');
			result.push([ { text: 'in default', }, ]);
			break;
    }
	console.log('result: ', result);
	return result;
}