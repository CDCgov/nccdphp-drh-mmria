async function get_header(currentPage, pageCount, section_name)
{
    console.log( 'currentPage: ', currentPage );
	console.log( 'pageCount: ', pageCount);
	console.log( 'section_name', section_name);
	let styles = get_style();
	console.log('styles: ', styles);
    // console.log( 'doc: ', doc );
    if (section_name === 'all') 
    {
        let recLenArr = [];
        let startPage = 0;
        let endPage = 0;
        let title = '';
        for (let i = 0; i < doc.content.length; i++) 
        {
            startPage = doc.content[i].positions[0].pageNumber;
            endPage = doc.content[i].positions[doc.content[i].positions.length - 1].pageNumber;
            recLenArr.push({ s: startPage, e: endPage });
        }

        let index = recLenArr.findIndex(item => ((currentPage >= item.s) && (currentPage <= item.e)));
        for (let l = 0; l < doc.content[index].stack.length; l++) 
        {
            writeText = (doc.content[index].stack[l].pageHeaderText !== undefined) ? doc.content[index].stack[l].pageHeaderText : writeText;
        }
    }
    else if (section_name === 'core-summary') 
    {
        writeText = 'CORE SUMMARY';
    } else 
    {
        //writeText = getSectionTitle(section_name);
        writeText = section_name;
    }
    let headerObj = [
        {
            margin: 10,
            columns: [
                // {
                //     image: `${await getBase64ImageFromURL("/images/mmria-secondary.png")}`,
                //     width: 30,
                //     margin: [0, 0, 0, 10]
                // },
                {
                    width: '*',
                    text:  getHeaderName(),
                    alignment: 'center',
                    style: 'pageHeader'
                },
                // {
                //     width: 110,
                //     layout: {
                //         defaultBorder: false,
                //     },
                //     table: {
                //         widths: [40, 60],
                //         body: [
                //             [
                //                 { text: 'Page:', style: ['headerPageDate', 'isBold'], alignment: 'right' },
                //                 { text: currentPage + ' of ' + pageCount, style: 'headerPageDate' },
                //             ],
                //             [
                //                 { text: 'Printed:', style: ['headerPageDate', 'isBold'], alignment: 'right' },
                //                 { text: getTodayFormatted(), style: 'headerPageDate' },
                //             ],
                //         ],
                //     },
                // },
            ],
        },
        {
            margin: [10, 0, 10, 5],
            layout: 'noBorders',
            table: {
                widths: '*',
                body: [
                    [
                        { text: writeText,  }
                    ],
                ],
            },
        },
    ];
    return headerObj;
}


function set_form_start(p_content, p_metadata)
{
    p_content.push({ text: '', pageHeaderText: p_metadata.prompt.toUpperCase() });
    
    let result = [
        {
        layout: {
            defaultBorder: false,
            paddingLeft: function (i, node) { return 1; },
            paddingRight: function (i, node) { return 1; },
            paddingTop: function (i, node) { return 2; },
            paddingBottom: function (i, node) { return 2; },
        },
        id: p_metadata.name,
        width: 'auto',
        table: {
            headerRows: 1,
            widths: [250, 'auto'],
            body: [
                [
                    { text: p_metadata.name, colSpan: '2', },
                    {},
                ]
            ]
            }
        }
    ];

    p_content.push(result)
        
    //return result;
}

function get_group_start(p_name)
{
    let result = [
        { text: p_name, style: ['subHeader'], colSpan: '2', },
        
            {
            layout: {
                defaultBorder: false,
                paddingLeft: function (i, node) { return 1; },
                paddingRight: function (i, node) { return 1; },
                paddingTop: function (i, node) { return 2; },
                paddingBottom: function (i, node) { return 2; },
            },
            id: p_metadata.name,
            width: 'auto',
            table: {
                headerRows: 1,
                widths: [250, 'auto'],
                body: [
                    [
                        { text: p_metadata.name, style: ['subHeader'], colSpan: '2', },
                        {},
                    ]
                ]
                }
            }
    ];


    return result;
}