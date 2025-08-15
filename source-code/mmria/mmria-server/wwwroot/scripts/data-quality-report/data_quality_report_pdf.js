var g_current;
var g_writeText;


$(function () {//http://www.w3schools.com/html/html_layout.asp
	'use strict';

	//profile.initialize_profile();

});

async function create_data_quality_report_pdf(p_report_type, p_data, p_quarter, p_headers) {
	let p_ctx = {
		report_type: p_report_type,
		data: p_data,
		quarter: p_quarter,
		headers: p_headers
	};
	// console.log('in create_data_quality_report_pdf: ', p_ctx);

	try {
		// initialize_print_pdf(ctx);
		document.title = p_headers.title;
		await print_pdf(p_ctx);
	}
	catch (ex) {
		// console.log('ERR: ', ex);
		let profile_content_id = document.getElementById("profile_content_id");
		{
			profile_content_id.innerText = `
An error has occurred generating PDF for ${p_headers.title}.
 
Please email mmriasupport@cdc.gov the ERROR DETAILS regarding this Print-PDF issue.

Error Details (Print PDF):

Summary: ${ex}

Stack: ${ex.stack}

            `;
		}

	}
}

async function print_pdf(ctx) {
	g_writeText = '';

	// Get unique PDF name
	let pdfName = createNamePDF();

	// Get the PDF Header Title & Subtitle
	let pdfTitle = ctx.headers.title;
	let pdfSubtitle = ctx.headers.subtitle;

	// Get the logoUrl for Header
	let logoUrl = await getBase64ImageFromURL("/images/mmria-secondary.png");

	// Format the content
	let retContent = formatContent(ctx);

	// console.log('retContent: ', retContent);
	let doc = {
		pageOrientation: 'portrait',
		pageMargins: [10, 50, 10, 50],
		info: {
			title: pdfTitle,
			author: 'MMRIA',
			subject: 'Data Quality Report',
		},
		header: (currentPage, pageCount) => {
			// console.log( 'currentPage: ', currentPage );
			// console.log('pageCount: ', pageCount);
			// console.log( 'doc: ', doc );
			// console.log('header ctx: ', ctx);
			let recLenArr = [];
			let startPage = 0;
			let endPage = 0;
			let header = '***';
			// console.log('doc.content.length: ', doc.content.length);
			// console.log('*** content: ', doc.content);
			for (let i = 0; i < doc.content.length; i++) 
            {
				// console.log('*** pageNumber: ', i, ' - ', doc.content[i].positions[0].pageNumber);
				startPage = doc.content[i].positions[0].pageNumber;
				endPage = doc.content[i].positions[doc.content[i].positions.length - 1].pageNumber;
				header = (doc.content[i].stack[0].table.body[0][0].pageHeaderText != undefined) ? doc.content[i].stack[0].table.body[0][0].pageHeaderText : header;
				recLenArr.push({ s: startPage, e: endPage, p: header });
			}

			// Set the header title
			let index = recLenArr.findIndex(item => ((currentPage >= item.s) && (currentPage <= item.e)));
			g_writeText = recLenArr[index].p;
			let headerObj = [
				{
					margin: 10,
					columns: [
						{
							image: `${logoUrl}`,
							width: 30,
							margin: [0, 0, 0, 10]
						},
						{
							width: '*',
							layout: {
								defaultBorder: false,
							},
							table: {
								widths: '*',
								body: [
									[
										{ text: pdfTitle, alignment: 'center', style: 'pageHeader', },
									],
									[
										{ text: pdfSubtitle, alignment: 'center', style: 'pageSubHeader' },
									],
								],									
							},
						},
						{
							width: 110,
							layout: {
								defaultBorder: false,
							},
							table: {
								widths: [40, 60],
								body: [
									[
										{ text: 'Page:', style: ['headerPageDate', 'isBold'], alignment: 'right' },
										{ text: currentPage + ' of ' + pageCount, style: 'headerPageDate' },
									],
									[
										{ text: 'On:', style: ['headerPageDate', 'isBold'], alignment: 'right' },
										{ text: getTodayFormatted(), style: 'headerPageDate' },
									],
								],
							},
						},
					],
				},
			];
			return headerObj;
		},
		footer: (currentPage, pageCount) => {
			var tbl_footer = '';
			if ( ctx.report_type == 'Summary' )
			{
				tbl_footer = '*The denominator for this indicator is limited to pregnancy-related deaths with a completed ';
				tbl_footer += 'Birth/Fetal Death Certificate - Parent Section according to the Form Status on the Home Record [hrcpr_bcp_secti = 2].'
                tbl_footer += ' DQR report automatically summarizes cases accessible to the logged in user: ' + g_user_name;
			}
			else
			{
				tbl_footer = '';
			}
			if (currentPage < 3) {
				return {
					margin: [20, 0, 20, 0],
					height: 20,
					columns: [
						{ text: tbl_footer, },
					]
				};
			}
		},
		styles: {
			pageHeader: {
				fontSize: 12,
				color: '#000080',
			},
			pageSubHeader: {
				fontSize: 10,
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
				fontSize: 12,
				color: '#0000ff',
				margin: [0, 10, 0, 5]
			},
			isBold: {
				bold: true,
			},
			isItalics: {
				italics: true,
			},
			fgBlue: {
				color: '#000080',
			},
			fgRed: {
				color: '#990000',
			},
			isUnderlined: {
				decoration: 'underline',
			},
			lightFill: {
				fillColor: '#eeeeee',
			},
			blueFill: {
				fillColor: '#cce6ff',
			},
			lightBlueFill: {
				fillColor: '#f0f8ff',
			},
			tableLabel: {
				color: '#000080',
				fontSize: 9,
				bold: true,
			},
			tableLabelSmall: {
				color: '#000080',
				fontSize: 6,
				bold: true,
			},
			tableDetail: {
				color: '#000000',
				fontSize: 9,
			},
			noteLabel: {
				color: '#000080',
				fontSize: 10,
				bold: true,
			},
			noteDetail: {
				color: '#000000',
				fontSize: 10,
			},
		},
		defaultStyle: {
			fontSize: 8,
		},
		content: retContent,
	};
	// Create the pdf file
	// pdfMake.createPdf(doc).download(pdfName);
	pdfMake.createPdf(doc).open();

}

// ************************************************************************
// ************************************************************************
//
// Begin - Generic Functions
//
// ************************************************************************
// ************************************************************************

// getBase64ImageFromURL
function getBase64ImageFromURL(url) {
	return new Promise((resolve, reject) => {
		let img = new Image();
		img.setAttribute("crossOrigin", "anonymous");
		img.onload = () => {
			let canvas = document.createElement("canvas");
			canvas.width = img.width;
			canvas.height = img.height;
			let ctx = canvas.getContext("2d");
			ctx.drawImage(img, 0, 0);
			let dataURL = canvas.toDataURL("image/png");
			resolve(dataURL);
		};
		img.onerror = error => {
			reject(error);
		};
		img.src = url;
	});
}

// create a unique PDF name based on datetime
function createNamePDF() {
	let today = new Date();
	let yy = today.getFullYear() + ':';
	let mm = fmt2Digits(today.getMonth() + 1) + ':';
	let dd = fmt2Digits(today.getDate()) + '_';
	let hh = fmt2Digits(today.getHours()) + ':';
	let mn = fmt2Digits(today.getMinutes()) + ':';
	let ss = fmt2Digits(today.getSeconds()) + '.pdf';

	return 'DQR_mmria_' + yy + mm + dd + hh + mn + ss;
}

// check field for null
function chkNull(val) {
	if (val == null) return '';
	return val;
}

// create a unique PDF name based on datetime
function getTodayFormatted() {
	let today = new Date();
	let mm = today.toLocaleString('default', { month: 'short' });
	let yy = today.getFullYear();
	let dd = fmt2Digits(today.getDate());

	return `${dd} ${mm} ${yy}`;
}

// Format the month or day to always have 2 digits
function fmt2Digits(val) {
	if (val == null || val == '9999') return '  ';
	return ((val < 10) ? '0' : '') + val;
}

// Format the year to always have 4 digits, check for 9999
function fmtYear(val) {
	return (val == null || val == '9999') ? '    ' : val;
}

// Reformat date - from YYYY/MM/DD to MM/DD/YYYY
function reformatDate(dt) {
	if (dt == null || dt.length == 0 || dt == '0001-01-01T00:00:00') return '  /  /    ';
	let date = new Date(dt);
	return (!isNaN(date.getTime())) ? `${fmt2Digits(date.getMonth() + 1)}/${fmt2Digits(date.getDate())}/${fmtYear(date.getFullYear())}` : '';
}

// Format date from data and return mm/dd/yyyy or blank if it contains 9999's
function fmtDataDate(dt) {
	if (dt.year == null || dt.year == '9999' || dt.year == '') {
		return '  /  /  ';
	}
	return `${fmt2Digits(dt.month)}/${fmt2Digits(dt.day)}/ {fmtYear(dt.year)}`;
}

// Format date by field (day, month, year)
function fmtDateByFields(dt) {
	let mm = (dt.month == null || dt.month == '9999' || dt.month == '') ? '  ' : fmt2Digits(dt.month);
	let dd = (dt.day == null || dt.day == '9999' || dt.day == '') ? '  ' : fmt2Digits(dt.day);
	let yy = (dt.year == null || dt.year == '9999' || dt.year == '') ? '    ' : dt.year;
	return `${mm}/${dd}/${yy}`;
}

// Format date and time string with mm/dd/yyyy hh:mm (military time)
function fmtDateTime(dt) {
	if (dt == null || dt.length == 0 || dt == '0001-01-01T00:00:00') return '  /  /    ';
	let fDate = new Date(dt);
	let hh = fDate.getHours();
	let mn = fDate.getMinutes();
	let strTime = `${fmt2Digits(hh)}:${fmt2Digits(mn)}`;
	strTime = (strTime == '00:00') ? '' : strTime;
	return `${fmt2Digits(fDate.getMonth() + 1)}/${fmt2Digits(fDate.getDate())}/${fmtYear(fDate.getFullYear())} ${strTime}`;
}

// Reformat date from data string and return mm/dd/yyyy 
function fmtStrDate(dt) {
	if (dt == null || dt.length == 0) {
		return ' / / ';
	}
	let dtParts = dt.split('-');
	return `${fmt2Digits(dtParts[1])}/${fmt2Digits(dtParts[2])}/${fmtYear(dtParts[0])}`;
}

// Format number
function fmtNumber( val ) 
{
	if ( val == null || val == '' ) 
    {
		return '0';
	}
	return val;
}

// Format percentage
function fmtPercent( val ) 
{
	if ( val == null || val == '' ) 
    {
		return '0.0%';
	}

    const result = val.toFixed(1);

    if(result == 100)
    {
        return '100%';
    }
    else
    {
        return result + '%';
    }
	
}

// ************************************************************************
// ************************************************************************
//
// End - Generic Functions
//
// ************************************************************************
// ************************************************************************

// ************************************************************************
// ************************************************************************
//
// Start - Build the record based on what kind it is
//
// ************************************************************************
// ************************************************************************
function formatContent(ctx) {
	let retContent = [];
	let body = [];

	if ( ctx.report_type == 'Summary' )
	{
		// Build summary page 1 & 2
		body = dqr_summary(ctx);

		retContent.push([
			{
				layout: {
					defaultBorder: false,
					paddingLeft: function (i, node) { return 1; },
					paddingRight: function (i, node) { return 1; },
					paddingTop: function (i, node) { return 2; },
					paddingBottom: function (i, node) { return 1; },
				},
				width: '*',
				table: {
					headerRows: 0,
					widths: ['*'],
					body: body,
				},
			},
		]);

		body = [];

		// Add the notes
		body = dqr_notes(ctx);

		retContent.push([
			{
				layout: {
					defaultBorder: false,
					paddingLeft: function (i, node) { return 1; },
					paddingRight: function (i, node) { return 1; },
					paddingTop: function (i, node) { return 2; },
					paddingBottom: function (i, node) { return 1; },
				},
				width: '*',
				table: {
					headerRows: 0,
					widths: ['*'],
					body: body,
				},
			},
		]);
	}
	else
	{
		// Build the detail report
		body = dqr_detail(ctx);

		retContent.push([
			{
				layout: {
					defaultBorder: false,
					paddingLeft: function (i, node) { return 1; },
					paddingRight: function (i, node) { return 1; },
					paddingTop: function (i, node) { return 2; },
					paddingBottom: function (i, node) { return 1; },
				},
				width: '*',
				table: {
					headerRows: 0,
					widths: ['*'],
					body: body,
				},
			},
		]);

	}

	// console.log('retContent: ', retContent);
	return retContent;
}

// Add a Page Break
function add_page_break() {
	let row = new Array();

	row.push([{ text: '', pageBreak: 'before' }]);

	return row;
}

// Format detail pages
function dqr_detail(ctx) {
	let retPage = [];
	let body = [];

	// Check to see if Detail Report has no cases
	if ( ctx.data.questions.length == 0 && ctx.data.cases.length == 0 )
	{
		body = format_detail_no_data();

		retPage.push([
			{
				layout: {
					defaultBorder: false,
					paddingLeft: function (i, node) { return 1; },
					paddingRight: function (i, node) { return 1; },
					paddingTop: function (i, node) { return 2; },
					paddingBottom: function (i, node) { return 1; },
				},
				width: '*',
				table: {
					headerRows: 0,
					widths: ['*'],
					body: body,
				},
			},
		]);
	}
	else
	{

        


        

		// Add detail records
		body = format_detail_questions(ctx);

		retPage.push([
			{
				layout: {
					defaultBorder: false,
					paddingLeft: function (i, node) { return 1; },
					paddingRight: function (i, node) { return 1; },
					paddingTop: function (i, node) { return 2; },
					paddingBottom: function (i, node) { return 1; },
				},
				width: '*',
				table: {
					headerRows: 0,
					widths: ['*'],
					body: body,
				},
			},
		]);

		body = [];
		body = format_detail_cases(ctx);

		retPage.push([
			{
				layout: {
					defaultBorder: false,
					paddingLeft: function (i, node) { return 1; },
					paddingRight: function (i, node) { return 1; },
					paddingTop: function (i, node) { return 2; },
					paddingBottom: function (i, node) { return 1; },
				},
				width: '*',
				table: {
					headerRows: 0,
					widths: ['*'],
					body: body,
				},
			},
		]);

		body = [];
		body = format_detail_total(ctx);

		retPage.push([
			{
				layout: {
					defaultBorder: false,
					paddingLeft: function (i, node) { return 1; },
					paddingRight: function (i, node) { return 1; },
					paddingTop: function (i, node) { return 2; },
					paddingBottom: function (i, node) { return 1; },
				},
				width: '*',
				table: {
					headerRows: 0,
					widths: ['*'],
					body: body,
				},
			},
		]);
	}

	return retPage;
}

// Case List by Data Quality Question
function format_detail_no_data() {
	let retPage = [];
	let rows = new Array();

	// Header
	rows.push([
		{
			text: 'No cases identified with issues in DQR',
			style: ['pageSubHeader', 'fgRed', 'isBold'],
			colSpan: '5',
			alignment: 'center'
		},
		{},{},{},{}
	],);

	retPage.push([
		{
			layout: {
				defaultBorder: false,
			},
			width: '*',
			table: {
				headerRows: 0,
				widths: ['*', '*', '*', '*', 'auto'],
				body: rows,
			},
		},
	]);

	return retPage;
}

// Case List by Data Quality Question
function format_detail_questions(ctx) 
{
    let v_44_48 = new Set();
    v_44_48.add("n44");
    v_44_48.add("n45");
    v_44_48.add("n46");
    v_44_48.add("n47");
    v_44_48.add("n48");

	let retPage = [];
	// ***
	// *** q - the question constants array
	// *** d - the data array
	// ***
	let q = g_dqr_questions;
	let d = ctx.data.questions;
	let rows = new Array();
	let fld = '';

	// Header
	rows.push([
		{
			text: 'Case List by Data Quality Question',
			style: ['pageSubHeader', 'blueFill', 'isBold'],
			colSpan: '5',
			alignment: 'center'
		},
		{},{},{},{}
	],);

	// Sub Header
	rows.push([
		{ text: '#', style: ['tableLabel', 'blueFill'], alignment: 'center', },
		{ text: 'MMRIA Record-ID#', style: ['tableLabel', 'blueFill'], },
		{ text: 'Date of Death', style: ['tableLabel', 'blueFill'], },
		{ text: 'ComRev Date', style: ['tableLabel', 'blueFill'], },
		{ text: 'MMRIA Internal Analysis ID#', style: ['tableLabel', 'blueFill'], },
	],);

	// Questions loop
	if ( d.length == 0 )
	{
		// No questions
		rows.push([
			{ text: 'No Data Quality Questions to Display', style: ['tableDetail'], colSpan: '5', },
			{},{},{},{},
		],);
	}
	else
	{
		// Loop thru the questions
		for ( let i = 0; i < d.length; i++ )
		{
			// Build the index to question constants
			fld = ( d[i].qid < 10 ) ? 'n0' + d[i].qid : 'n' + d[i].qid;


            if
            (
                v_44_48.has(fld) &&
                d[i].typ.indexOf("Failed Logic Check") < 0
            )
            {
                continue;
            }
		
			// Show the question and type identifier
			rows.push([
				{ text: `${q[fld]} - ${d[i].typ}`, style: ['tableLabel'], colSpan: '5', border: [false, false, false, true], },
				{},{},{},{},
			],);

			// Loop thru the detail lines for this question
			if ( d[i].detail.length == 0 )
			{
				rows.push([
					{ text: 'No cases to report', style: ['tableDetail', 'lightFill'], colSpan: '5', },
					{},{},{},{},
				],);
			}
			else
			{
				for ( let j = 0; j < d[i].detail.length; j++ )
				{
					rows.push([
						{ text: `${d[i].detail[j].num}`, style: ['tableDetail', `${ (j % 2 == 0) ? 'lightFill' : ''}`], alignment: 'center', },
						{ text: `${d[i].detail[j].rec_id}`, style: ['tableDetail', `${ (j % 2 == 0) ? 'lightFill' : ''}`], },
						{ text: `${d[i].detail[j].dt_death}`, style: ['tableDetail', `${ (j % 2 == 0) ? 'lightFill' : ''}`], },
						{ text: `${d[i].detail[j].dt_com_rev}`, style: ['tableDetail', `${ (j % 2 == 0) ? 'lightFill' : ''}`], },
						{ text: `${d[i].detail[j].ia_id}`, style: ['tableDetail', `${ (j % 2 == 0) ? 'lightFill' : ''}`], },
					],);	
				}
			}

			// Add a line at end of section
			rows.push([
				{ text: ' ', colSpan: '5', border: [false, true, false, false], },{},{},{},{},
			],);
		}
	}

	retPage.push([
		{
			layout: {
				defaultBorder: false,
			},
			width: '*',
			table: {
				headerRows: 2,
				widths: [30, '*', '*', '*', 'auto'],
				body: rows,
			},
		},
	]);

	return retPage;
}

// Case List by MMRIA-ID#
function format_detail_cases(ctx) {
	let retPage = [];
	// ***
	// *** q - the question constants array
	// *** d - the data array
	// ***
	let q = g_dqr_questions;
	let d = ctx.data.cases;
	let rows = new Array();
	let fld = '';

	// Header
	rows.push([
		{
			text: 'Case List by MMRIA-ID#',
			style: ['pageSubHeader', 'blueFill', 'isBold'],
			colSpan: '5',
			alignment: 'center'
		},
		{},{},{},{},
	],);

	// Sub Header
	rows.push([
		{ text: 'MMRIA Record-ID#', style: ['tableLabel', 'blueFill'], },
		{ text: 'Agency-Based Case ID#', style: ['tableLabel', 'blueFill'], },
		{ text: 'Date of Death', style: ['tableLabel', 'blueFill'], },
		{ text: 'ComRev Date', style: ['tableLabel', 'blueFill'], },
		{ text: 'MMRIA Internal Analysis ID#', style: ['tableLabel', 'blueFill'], },
	],);

	// case loop
	if ( d.length == 0 )
	{
		// No cases
		rows.push([
			{ text: 'No Case List Data to Display', style: ['tableDetail'], colSpan: '5', },
			{},{},{},{},
		],);
	}
	else
	{
		// Loop thru the questions
		for ( let i = 0; i < d.length; i++ )
		{
			// Show the question and type identifier
			rows.push([
				{ text: `${d[i].rec_id}`, style: ['tableLabel'], border: [false, false, false, true], },
				{ text: `${d[i].ab_case_id}`, style: ['tableLabel'], border: [false, false, false, true], },
				{ text: `${d[i].dt_death}`, style: ['tableLabel'], border: [false, false, false, true], },
				{ text: `${d[i].dt_com_rev}`, style: ['tableLabel'], border: [false, false, false, true], },
				{ text: `${d[i].ia_id}`, style: ['tableLabel'], border: [false, false, false, true], },
			],);

			// Loop thru the detail lines for this question
			for ( let j = 0; j < d[i].detail.length; j++ )
			{
				// Build the index to question constants
				fld = ( d[i].detail[j].qid < 10 ) ? 'n0' + d[i].detail[j].qid : 'n' + d[i].detail[j].qid;
		
				rows.push([
					{ 
						text: [
							{ text: `${fmt2Digits(j + 1)} `, style: ['tableDetail', 'isItalics'], },
							{ text: ` ${q[fld]} - ${d[i].detail[j].typ}`, },
						], 
						style: ['tableDetail', `${ (j % 2 == 0) ? 'lightFill' : ''}`], 
						colSpan: '5', 
					},
					{},{},{},{},
				],);	
			}

			// Add a line at end of section with underline
			rows.push([ { text: ' ', colSpan: '5', border: [false, true, false, false], },{},{},{},{}, ],);
		}
	}

	retPage.push([
		{
			layout: {
				defaultBorder: false,
			},
			width: '*',
			table: {
				headerRows: 2,
				widths: [80, 100, 80, 80, '*'],
				body: rows,
			},
		},
	]);

	return retPage;
}

// Total # of cases with issues
function format_detail_total(ctx) {
	let retPage = [];
	let d = ctx.data.total;
	let rows = new Array();

	// Add blank line
	rows.push([ { text: ' ', }, ],);

	// Total
	rows.push([
		{ text: `Total # of cases identified with issues in DQR: ${ d }`, style: ['tableLabel'], },
	],);

	retPage.push([
		{
			layout: {
				defaultBorder: false,
			},
			width: '*',
			table: {
				headerRows: 0,
				widths: ['*'],
				body: rows,
			},
		},
	]);

	return retPage;
}

// Format summary pages
function dqr_summary(ctx) {
	let retPage = [];
	let body = [];

	body = format_summary_pages(ctx);

	retPage.push([
		{
			layout: {
				defaultBorder: false,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 1; },
			},
			width: '*',
			table: {
				headerRows: 0,
				widths: ['*'],
				body: body,
			},
		},
	]);

	return retPage;
}

function format_summary_pages(ctx) {
	let retPage = [];
	// ***
	// *** q - the question array
	// *** d - the data array
	// ***
	let q = g_dqr_questions;
	let d = ctx.data;
	let rows = new Array();
	let fld = '';
	let fldSub = '';
	let startLoop;
	let endLoop;

	// First table - 01) thru 05)
	// Header
	rows.push([
		{
			text: `Summary of ALL Deaths Entered into MMRIA as of ${ctx.quarter}`,
			style: ['tableLabel', 'blueFill'],
			border: [true, true, false, true],
		},
		{
			text: `N (as of ${ctx.quarter})`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [false, true, true, true],
		},
	],);

	// Add the lines for 01) & 02)
	startLoop = 1;
	endLoop = 2;
	for ( let i = startLoop; i <= endLoop; i++ )
	{
		// Create the field name
		fld = 'n0' + i;
		rows.push([
			{
				text: q[fld],
				style: ['tableDetail'],
			},
			{
				text: d[fld],
				style: ['tableDetail'],
				alignment: 'center',
			},
		],);
	}

	// Add line 03) header
	fld = 'n03';
	rows.push([
		{
			text: q[fld],
			style: ['tableDetail'],
			border: [true, true, true, false],
		},
		{
			text: '',
			style: ['tableDetail'],
			alignment: 'center',
			border: [true, true, true, false],
		},
	],);
	// Add 03) detail lines
	startLoop = 0;
	endLoop = 7;
	for ( let i = startLoop; i <= endLoop; i++ )
	{
		fldSub = fld + i;
		rows.push([
			{
				text: q[fldSub],
				style: ['tableDetail', `${ (i % 2 == 0) ? 'lightFill' : ''}`],
				border: [true, false, true, false],
				preserveLeadingSpaces: true,
			},
			{
				text: d[fld][i],
				style: ['tableDetail', `${ (i % 2 == 0) ? 'lightFill' : ''}`],
				alignment: 'center',
				border: [true, false, true, false],
			},
		],);
	}

	// Add lines 04) & 05)
	startLoop = 4;
	endLoop = 5;
	for ( let i = startLoop; i <= endLoop; i++ )
	{
		fld = 'n0' + i;
		rows.push([
			{
				text: q[fld],
				style: ['tableDetail'],
			},
			{
				text: d[fld],
				style: ['tableDetail'],
				alignment: 'center',
			},
		],);
	}
	
	retPage.push([
		{
			layout: {
				defaultBorder: true,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 1; },
			},
			width: '*',
			table: {
				headerRows: 0,
				widths: [350, '*'],
				body: rows,
			},
		},
	]);

	// Re-initialize
	rows = new Array();

	// Second table - 06) thru 09)
	// Header
	rows.push([
		{
			text: `Summary of Deaths included in this ${ctx.quarter} Report`,
			style: ['tableLabel', 'blueFill'],
			border: [true, true, false, true],
		},
		{
			text: `N (as of ${ctx.quarter})`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [false, true, true, true],
		},
	],);

	// Add lines 06) thru 09)
	startLoop = 6;
	endLoop = 9;
	for ( let i = startLoop; i <= endLoop; i++ )
	{
		fld = 'n0' + i;
		rows.push([
			{
				text: q[fld] + ` ${ ( i == 6 ) ? ctx.quarter : ''}`,
				style: ['tableDetail'],
			},
			{
				text: d[fld],
				style: ['tableDetail'],
				alignment: 'center',
			},
		],);
	}

	retPage.push([
		{
			layout: {
				defaultBorder: true,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 1; },
			},
			width: '*',
			table: {
				headerRows: 0,
				widths: [350, '*'],
				body: rows,
			},
		},
	]);

	// Re-initialize
	rows = new Array();

	// Third table - 10) thru 34)
	// Header lines
	// ***
	// Header line 1
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, true, false, false],
		},
		{
			text: 'Deaths Reviewed in',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, true, true, false],
		},
		{
			text: 'Deaths Reviewed in',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, true, true, false],
		},
	],);
	// Header line 2
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: `${ctx.quarter}, N = ${ctx.data.n06}`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: `Previous 4 Qtrs, N = ${ctx.data.n08}`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 3
	rows.push([
		{
			text: '',
			style: ['tableLabelSmall', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: 'How your data looks',
			style: ['tableLabelSmall', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: 'How your data looked',
			style: ['tableLabelSmall', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 4
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: 'Missing          Unknown',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: 'Missing          Unknown',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 5
	rows.push([
		{
			text: 'Key Characteristics of Reviewed Pregnancy-Related Deaths',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, true],
		},
		{
			text: 'N       %            N          %',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, true],
		},
		{
			text: 'N       %            N          %',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, true],
		},
	],);

	// Add line 10) thru 34)
	startLoop = 10;
	endLoop = 34;
	for ( let i = startLoop; i <= endLoop; i++ )
	{
		fld = 'n' + i;
		rows.push([
			{
				text: q[fld],
				style: ['tableDetail'],
			},
			{
				columns:
					[
						{ width: 20, text: fmtNumber( d[fld].s.mn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 30, text: fmtPercent( d[fld].s.mp ), style: ['tableDetail'], alignment: 'right', },
						{ width: 20, text: fmtNumber( d[fld].s.un ), style: ['tableDetail'], alignment: 'right', },
						{ width: 30, text: fmtPercent( d[fld].s.up ), style: ['tableDetail'], alignment: 'right', },
						{ width: 4, text: '', style: ['tableDetail'], },
					],
			},
			{
				columns:
					[
						{ width: 20, text: fmtNumber( d[fld].p.mn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 30, text: fmtPercent( d[fld].p.mp ), style: ['tableDetail'], alignment: 'right', },
						{ width: 20, text: fmtNumber( d[fld].p.un ), style: ['tableDetail'], alignment: 'right', },
						{ width: 30, text: fmtPercent( d[fld].p.up ), style: ['tableDetail'], alignment: 'right', },
						{ width: 4, text: '', style: ['tableDetail'], },
					],
			},
		],);
	}

	retPage.push([
		{
			layout: {
				defaultBorder: true,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 1; },
			},
			width: '*',
			table: {
				headerRows: 5,
				widths: [350, '*', '*'],
				body: rows,
			},
		},
	]);

	// Re-initialize
	rows = new Array();

	// Fourth table - 35) thru 43)
	// Header
	// ***
	// Header line 1
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, true, false, false],
		},
		{
			text: 'Deaths Reviewed in',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, true, true, false],
		},
		{
			text: 'Deaths Reviewed in',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, true, true, false],
		},
	],);
	// Header line 2
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: `${ctx.quarter}, N = ${ctx.data.n06}`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: `Previous 4 Qtrs, N =  ${ctx.data.n08}`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 3
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: 'Missing          Unknown',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: 'Missing          Unknown',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 4
	rows.push([
		{
			text: 'Committee Decisions for Reviewed Pregnancy-Related Deaths',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, true],
		},
		{
			text: 'N       %            N        %',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, true],
		},
		{
			text: 'N       %            N        %',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, true],
		},
	],);

	// Add line 35) thru 43)
	startLoop = 35;
	endLoop = 43;
	for ( let i = startLoop; i <= endLoop; i++ )
	{
		fld = 'n' + i;
		rows.push([
			{
				text: q[fld],
				style: ['tableDetail'],
			},
			{
				columns:
					[
						{ width: 20, text: fmtNumber( d[fld].s.mn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 30, text: fmtPercent( d[fld].s.mp ), style: ['tableDetail'], alignment: 'right', },
						{ width: 20, text: fmtNumber( d[fld].s.un ), style: ['tableDetail'], alignment: 'right', },
						{ width: 30, text: fmtPercent( d[fld].s.up ), style: ['tableDetail'], alignment: 'right', },
					],
			},
			{
				columns:
					[
						{ width: 20, text: fmtNumber( d[fld].p.mn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 30, text: fmtPercent( d[fld].p.mp ), style: ['tableDetail'], alignment: 'right', },
						{ width: 20, text: fmtNumber( d[fld].p.un ), style: ['tableDetail'], alignment: 'right', },
						{ width: 30, text: fmtPercent( d[fld].p.up ), style: ['tableDetail'], alignment: 'right', },
					],
			},
		],);
	}

	retPage.push([
		{
			layout: {
				defaultBorder: true,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 1; },
			},
			width: '*',
			table: {
				headerRows: 4,
				widths: [350, '*', '*'],
				body: rows,
			},
		},
	]);

	// Re-initialize
	rows = new Array();

	// Fifth table - 44) thru 45)
	// Header
	// ***
	// Header line 1
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, true, false, false],
		},
		{
			text: 'Deaths Reviewed in',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, true, true, false],
		},
		{
			text: 'Deaths Reviewed in',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, true, true, false],
		},
	],);
	// Header line 2
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: `${ctx.quarter}, N = ${ctx.data.n06}`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: `Previous 4 Qtrs, N = ${ctx.data.n08}`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 3
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: 'Total    Passed Logic Chk',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: 'Total    Passed Logic Chk',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 4
	rows.push([
		{
			text: 'Logic Checks for Reviewed Pregnancy-Related Deaths',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, true],
		},
		{
			text: '  N            N          %',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, true],
			preserveLeadingSpaces: true,
		},
		{
			text: '  N            N          %',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, true],
			preserveLeadingSpaces: true,
		},
	],);

	// Add line 44) & 45)
	startLoop = 44;
	endLoop = 45;
	for ( let i = startLoop; i <= endLoop; i++ )
	{
		fld = 'n' + i;
		rows.push([
			{
				text: q[fld],
				style: ['tableDetail'],
			},
			{
				columns:
					[
						{ width: 30, text: fmtNumber( d[fld].s.tn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 33, text: fmtNumber( d[fld].s.pn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 37, text: fmtPercent( d[fld].s.pp ), style: ['tableDetail'], alignment: 'right', },
					],
			},
			{
				columns:
					[
						{ width: 30, text: fmtNumber( d[fld].p.tn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 33, text: fmtNumber( d[fld].p.pn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 37, text: fmtPercent( d[fld].p.pp ), style: ['tableDetail'], alignment: 'right', },
					],
			},
		],);
	}

	retPage.push([
		{
			layout: {
				defaultBorder: true,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 1; },
			},
			width: '*',
			table: {
				headerRows: 4,
				widths: [350, '*', '*'],
				body: rows,
			},
		},
	]);

	// Re-initialize
	rows = new Array();

	// Sixth table - 46) thru 49)
	// Header
	// ***
	// Header line 1
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, true, false, false],
		},
		{
			text: 'Preventable Deaths',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, true, true, false],
		},
		{
			text: 'Preventable Deaths',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, true, true, false],
		},
	],);
	// Header line 2
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: 'Reviewed in',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: 'Reviewed in',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 3
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: `${ctx.quarter}, N = ${ctx.data.current_is_preventable_death}`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: `Previous 4 Qtrs, N = ${ctx.data.previous_is_preventable_death}`,
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 4
	rows.push([
		{
			text: '',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, false],
		},
		{
			text: 'Total    Passed Logic Chk',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
		{
			text: 'Total    Passed Logic Chk',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, false],
		},
	],);
	// Header line 5
	rows.push([
		{
			text: 'Logic Checks for Reviewed Preventable Pregnancy-Related Deaths',
			style: ['tableLabel', 'blueFill'],
			border: [true, false, true, true],
		},
		{
			text: '  N            N          %',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, true],
			preserveLeadingSpaces: true,
		},
		{
			text: '  N            N          %',
			style: ['tableLabel', 'blueFill'],
			alignment: 'center',
			border: [true, false, true, true],
			preserveLeadingSpaces: true,
		},
	],);

	// Add lines 46) thru 49)
	startLoop = 46;
	endLoop = 49;
	for ( let i = startLoop; i <= endLoop; i++ )
	{
		fld = 'n' + i;
		rows.push([
			{
				text: q[fld],
				style: ['tableDetail'],
			},
			{
				columns:
					[
						{ width: 30, text: fmtNumber( d[fld].s.tn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 33, text: fmtNumber( d[fld].s.pn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 37, text: fmtPercent( d[fld].s.pp ), style: ['tableDetail'], alignment: 'right', },
					],
			},
			{
				columns:
					[
						{ width: 30, text: fmtNumber( d[fld].p.tn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 33, text: fmtNumber( d[fld].p.pn ), style: ['tableDetail'], alignment: 'right', },
						{ width: 37, text: fmtPercent( d[fld].p.pp ), style: ['tableDetail'], alignment: 'right', },
					],
			},
		],);
	}

	retPage.push([
		{
			layout: {
				defaultBorder: true,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 1; },
			},
			width: '*',
			table: {
				headerRows: 5,
				widths: [350, '*', '*'],
				body: rows,
			},
		},
	]);

	return retPage;
}

// Build note pages
function dqr_notes(ctx) {
	let retPage = [];
	let body = [];
	let row = new Array();

	// Add a page break
	body.push(add_page_break());

	// Add the notes from data_quality_report_constants.js file
	g_dqr_notes_list.map((note, index) => {
		row = new Array();
		row.push([
			{
				text: [
					{
						text: `${note.num} `,
						style: ['noteDetail'],
					},
					{
						text: `${note.title} `,
						style: ['noteLabel'],
					},
					{
						text: `${note.desc}`,
						style: ['noteDetail'],
					},
				],
			},
		]);
		body.push(row);
	});

	retPage.push([
		{
			layout: {
				defaultBorder: false,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 1; },
			},
			width: '*',
			table: {
				headerRows: 0,
				widths: ['*'],
				body: body,
			},
		},
	]);

	return retPage;
}
