let g_md = null;
// let g_metadata;
let g_d = null;
let g_section_name;
let g_current;
let g_writeText;
// let g_metadata_summary = {};
let g_record_number;
let g_show_hidden = false;
let g_type_output;
let g_identified_message = '';

$(function () {//http://www.w3schools.com/html/html_layout.asp
	'use strict';

	//profile.initialize_profile();

	//load_metadata();
});


let TitleMap = {
	"home_record": "Home",
	"death_certificate": "DC",
	"birth_fetal_death_certificate_parent": "BCDC-P",
	"birth_certificate_infant_fetal_section": "BCDC-I",
	"autopsy_report": "Autopsy",
	"prenatal": "PreNatal",
	"er_visit_and_hospital_medical_records": "ER",
	"other_medical_office_visits": "OfficeVisits",
	"medical_transport": "Transport",
	"social_and_environmental_profile": "SEP",
	"mental_health_profile": "MentalHealth",
	"informant_interviews": "Interview",
	"case_narrative": "Narrative",
	"committee_review": "Decision",
	"all": "ALL",
	"core-summary": "Core",
    "tracking": "Tracking",
    "demographic": "Demographic",
    "outcome": "Outcome",
    "cause_of_death": "Cause of Death",
    "preparer_remarks": "Preparer Remarks",
    //
    "vro_case_determination": "VRO Case Determination",
    "ije_dc": "IJE Death Certificate",
    "ije_bc": "IJE Birth Certificate",
    "ije_fetaldc": "IJE Fetal Death Certificate",
    "amss_tracking": "AMSS Tracking",
    "attachments": "Attatchments",


};


function clone(obj) 
{
    if (null == obj || "object" != typeof obj) return obj;
    var copy = obj.constructor();
    for (var attr in obj) 
    {
        if (obj.hasOwnProperty(attr)) copy[attr] = obj[attr];
    }
    return copy;
}



async function create_print_version
(
    p_metadata,
    p_data,
    p_section,
    p_type_output,
    p_number,
    p_metadata_summary,
    p_show_hidden,
    p_is_de_identified = false
) 
{

	g_md = null;
	g_metadata = null;
	g_d = null;
	g_section_name = null;
	g_current = null;
	g_writeText = null;
	g_metadata_summary = {};
	g_record_number = null;
  g_type_output = p_type_output;

	g_md = p_metadata;
	g_metadata = p_metadata;
	g_d = p_data;
	g_section_name = p_section;
	g_metadata_summary = p_metadata_summary;
	g_record_number = p_number;
  console.log(p_is_de_identified);
	if 
    (
        p_show_hidden != null && 
        p_show_hidden
    ) 
    {
		g_show_hidden = true;
	}
  if(p_is_de_identified)
  {
    g_identified_message = '*Graph dates have been altered to preserve confidentiality.';
  }

	let p_ctx = {
		metadata: p_metadata,
		data: p_data,
		lookup: p_metadata.lookup,
		mmria_path: "",
		content: [],
		section_name: g_section_name,
		record_number: p_number,
		is_grid_item: false,
		createdBy: p_data.created_by,
		groupLevel: 0,
		p_data: p_data,
    p_chart_message: g_identified_message
	};

	// console.log(' let p_ctx = ', p_ctx);
	
	try {
		// initialize_print_pdf(ctx);
		document.title = getHeaderName();
		await print_pdf(p_ctx);
	}
	catch (ex) {
		let profile_content_id = document.getElementById("profile_content_id");
		{
			profile_content_id.innerText = `
An error has occurred generating PDF for ${getHeaderName()}.
 
Please email mmriasupport@cdc.gov the ERROR DETAILS regarding this Print-PDF issue.

Error Details (Print PDF):

Summary: ${ex}

Stack: ${ex.stack}

            `;
		}

	}
}


async function create_print_version2
(
    p_metadata,
    p_data,
    p_section,
    p_type_output,
    p_number,
    p_metadata_summary,
    p_show_hidden
) 
{
    p_data = clone(p_data);
	g_md = null;
	g_metadata = null;
	g_d = null;
	g_section_name = null;
	g_current = null;
	g_writeText = null;
	g_metadata_summary = {};
	g_record_number = null;
  g_type_output = p_type_output;

	g_md = p_metadata;
	g_metadata = p_metadata;
	g_d = p_data;
	g_section_name = p_section;
	g_metadata_summary = p_metadata_summary;
	g_record_number = p_number;

	if 
    (
        p_show_hidden != null && 
        p_show_hidden
    ) 
    {
		g_show_hidden = true;
	}

	let p_ctx = {
		metadata: p_metadata,
		data: p_data,
		lookup: p_metadata.lookup,
		mmria_path: "",
		content: [],
		section_name: g_section_name,
		record_number: p_number,
		is_grid_item: false,
		createdBy: p_data.created_by,
		groupLevel: 0,
		p_data: p_data
	};

	// console.log(' let p_ctx = ', p_ctx);
	
	try {
		// initialize_print_pdf(ctx);
		document.title = getHeaderName();
		await print_pdf(p_ctx);
	}
	catch (ex) {
		let profile_content_id = document.getElementById("profile_content_id");
		{
			profile_content_id.innerText = `
An error has occurred generating PDF for ${getHeaderName()}.
 
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

	// Get the PDF Header Title
	let pdfTitle = getHeaderName();

	// Get report tab name
	let reportTabName = ``;


    if(g_d.home_record != null)
        reportTabName = `MMRIA #:  ${g_d.home_record.record_id}/${TitleMap[ctx.section_name]}`;

    if(g_d.tracking != null)
    {
        reportTabName = `PMSS #:  ${g_d.tracking.admin_info.pmssno}/${TitleMap[ctx.section_name]}`;
        TitleMap["committee_review"]= "Committee Review";
    }


	// Get the logoUrl for Header
	let logoUrl = await getBase64ImageFromURL("/images/mmria-secondary.png");

	// Create map of name and index of the g_md array children
	let arrMap = getArrayMap();

	// Format the content
	let retContent = await formatContent(ctx, arrMap);

	let doc = {
		pageOrientation: 'portrait',
		pageMargins: [20, 80, 20, 20],
		// background: () => {
		// 	return {
		// 		canvas: [
		// 			{
		// 				type: 'rect',
		// 				x: 0, y: 0, h: 595.28, w: 841.89,
		// 				color: '#00BFFF'
		// 			},
		// 		],
		// 	};
		// },
		info: {
			title: reportTabName,
		},
		header: (currentPage, pageCount) => {
			// console.log( 'currentPage: ', currentPage );
			// console.log('pageCount: ', pageCount);
			// console.log( 'doc: ', doc );
			// console.log('ctx: ', ctx);
			if (ctx.section_name == 'all') {
				let recLenArr = [];
				let startPage = 0;
				let endPage = 0;
				let header = '***';
				// console.log('doc.content.length: ', doc.content.length);
				// console.log('*** content: ', doc.content);
				for (let i = 0; i < doc.content.length; i++) {
					// console.log('*** pageNumber: ', i, ' - ', doc.content[i].positions[0].pageNumber);
					startPage = doc.content[i].positions[0].pageNumber;
					endPage = doc.content[i].positions[doc.content[i].positions.length - 1].pageNumber;
					header = (doc.content[i].stack[0].table.body[0][0].pageHeaderText != undefined) ? doc.content[i].stack[0].table.body[0][0].pageHeaderText : header;
					recLenArr.push({ s: startPage, e: endPage, p: header });
				}



				// Set the header title
				let index = recLenArr.findIndex(item => ((currentPage >= item.s) && (currentPage <= item.e)));
				if(index > -1)
                {
                    g_writeText = recLenArr[index].p;
                }
                else
                {
                    //debugger;
                }
			}
			else if (g_section_name == 'core-summary') 
            {
				g_writeText = 'CORE SUMMARY';
			} 
            else 
            {
				g_writeText = getSectionTitle(ctx.section_name);
			}

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
							text: pdfTitle,
							alignment: 'center',
							style: 'pageHeader'
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
										{ text: 'Printed:', style: ['headerPageDate', 'isBold'], alignment: 'right' },
										{ text: getTodayFormatted(), style: 'headerPageDate' },
									],
								],
							},
						},
					],
				},
				{
					margin: [10, 0, 10, 5],
					layout: 'noBorders',
					table: {
						widths: '*',
						body: [
							[
								{ text: g_writeText, style: ['formHeader', 'isBold', 'lightFill'], }
							],
						],
					},
				},
			];
			return headerObj;
		},
		styles: {
			pageHeader: {
				fontSize: 12,
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
				fillColor: '#dedede',
			},
			blueFill: {
				fillColor: '#cce6ff',
			},
			lightBlueFill: {
				fillColor: '#f0f8ff',
			},
			subHeader: {
				margin: [0, 10, 0, 0],
				fontSize: 11,
				color: '#000080',
				bold: true,
			},
			gridHeader: {
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
                margin: [4, 0, 0, 0]
			},
			narrativeDetail: {
				color: '#000000',
				fontSize: 8,
			},
			labelDetail: {
				color: '#ff0000',
				fontSize: 9,
				bold: true,
				margin: [0, 5, 0, 0],
			},
		},
		defaultStyle: {
			fontSize: 12,
		},
		content: retContent,
	};

	// Check to see if it is View or Save
	if ( g_type_output == 'save')
	{
		// Create the download
		pdfMake.createPdf( doc ).download( pdfName );

		// Set at timer to close the window once the download is done
		window.setTimeout
		(
			function () { window.close(); }, 
			5000
		);
	}
	else
	{
		window.setTimeout
		(
			async function () { await pdfMake.createPdf(doc).open(window); },
			//async function () { await pdfMake.createPdf(doc).open(); },
			3000
		);
	}

}

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
	let utcDate = new Date().toISOString();
    if(g_d.home_record != null)
	    return `${g_d.home_record.record_id}` + '_' + utcDate + '.pdf';

    if(g_d.tracking != null)
	    return `${g_d.tracking.admin_info.pmssno}` + '_' + utcDate + '.pdf';
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
function fmt2Digits(val) 
{
	if (val == null) return '  ';
    
    const compare_value = parseInt(val);

    if(isNaN(compare_value)) return '  ';

    if(compare_value == 9999) return '  ';

	return ((compare_value < 10) ? '0' : '') + compare_value;
}

// Format the year to always have 4 digits, check for 9999
function fmtYear(val) {
	return (val == null || val == '9999') ? '    ' : val;
}

// Reformat date - from YYYY/MM/DD to MM/DD/YYYY
function reformatDate(dt) 
{
	if 
    (
        dt == null || 
        dt.length == 0 || 
        dt == '0001-01-01T00:00:00'
    ) 
    {
        return '  /  /    ';
    }

    function pad(n) 
    {
        const width = 2;
        const z = '0';
        n = n + '';
        return n.length >= width ? n : new Array(width - n.length + 1).join(z) + n;
      }

	const date = new Date(dt);
	if(!isNaN(date.getTime())) 
    {
        if(dt.indexOf("T") > -1)
        {
            return `${fmt2Digits(date.getMonth() + 1)}/${fmt2Digits(date.getDate())}/${fmtYear(date.getFullYear())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
        }
        else if(dt.indexOf("-") > -1)
        {
            const arr = dt.split('-');
            if(arr.length == 3)
            {
                return `${fmt2Digits(arr[1])}/${fmt2Digits(arr[2])}/${fmtYear(arr[0])}`
            }
            else
            {
                return `${fmt2Digits(date.getMonth() + 1)}/${fmt2Digits(date.getDate())}/${fmtYear(date.getFullYear())}`    
            }
        }
        else
        {
            return `${fmt2Digits(date.getMonth() + 1)}/${fmt2Digits(date.getDate())}/${fmtYear(date.getFullYear())}`
        }
    }
    else
    {
        return '';
    } 
}

function parseDateString(str) 
{
    const arr = str.split("-");
    if(arr.length == 3)
    {
        var y = arr[0],
            m = parseInt(arr[1]) - 1,
            d = arr[2];
        return new Date(y,m,d);
    }
    else
    {
        return '';
    }
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
function fmtDateTime(dt) 
{
	if (dt == null || dt.length == 0 || dt == '0001-01-01T00:00:00') return '  /  /    ';
	
    let fDate = new Date(dt);
    if(dt.indexOf("T") < 0)
    {
        fDate = parseDateString(dt);
    }
	const hh = fDate.getHours();
	const mn = fDate.getMinutes();
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

// Get the header name
function getHeaderName() 
{
    if(g_d.home_record != null)
    {	
        let headerStr = `MMRIA Record ID#:  ${g_d.home_record.record_id}\t--\t` +
        `Agency ID#: ${g_d.home_record.agency_case_id}`;
        return headerStr;

    }

    if(g_d.tracking != null)
    {	
        let headerStr = `PMSS #:  ${g_d.tracking.admin_info.pmssno}\t--\t` +
        `Jurisdiction: ${g_d.tracking.admin_info.jurisdiction}`;
        return headerStr;

    }

}

// Get Report Tab Name
function getReportTabName(section) {
	let nm = '';
	switch (section) {
		case 'home_record':
			nm = 'Home Record'
			break;
		case 'death_certificate':
			nm = 'Death Certificate';
			break;
		case 'birth_fetal_death_certificate_parent':
			nm = 'Birth/Fetal Death Certificate - Parent Section';
			break;
		case 'birth_certificate_infant_fetal_section':
			nm = 'Birth/Fetal Death Certificate - Infant/Fetal Section';
			break;
		case 'autopsy_report':
			nm = 'Autopsy Report';
			break;
		case 'prenatal':
			nm = 'Prenatal Care Record';
			break;
		case 'er_visit_and_hospital_medical_records':
			nm = 'ER Visits and Hospitalizations';
			break;
		case 'other_medical_office_visits':
			nm = 'Other Medical Office Visits';
			break;
		case 'medical_transport':
			nm = 'Medical Transport';
			break;
		case 'social_and_environmental_profile':
			nm = 'Social and Environmental Profile';
			break;
		case 'mental_health_profile':
			nm = 'Mental Health Profile';
			break;
		case 'informant_interviews':
			nm = 'Informant Interviews';
			break;
		case 'case_narrative':
			nm = 'Case Narrative';
			break;
		case 'committee_review':
			nm = 'Committee Review';
			break;
		case 'core-summary':
			nm = 'Core Elements Only';
			break;
		case 'all':
			nm = 'All Case Forms';
			break;
        default:
            nm = section;
            /*
            /tracking	
            /deographic	
            /outcome	
            /cause_of_death	
            /preparer_remarks	
            /committee_review	
            /vro_case_determination	
            /ije_dc	
            /ije_bc	
            /ije_fetaldc	
            /amss_tracking	
            /attachments
            */
	}

	return nm;
}
// Get the array for record selected
function getArrayMap() {
	let arr = [];

	for (let i = 0; i < g_md.children.length; i++) {
		arr.push({ name: `${g_md.children[i].name}`, index: i });
	}

	return arr;
}

// Generic Find from global lookup array
function lookupGlobalArr(lookup, val, pathReference) {
	// See if val is null
	if (val == undefined || val == null) return '';
	// Make sure val is a string
	let valStr = `${val}`;
	// See if val is blank
	if (valStr == '') return valStr;

	// Get the path_reference name
	let luIdx = pathReference.indexOf('/') + 1;
	let lookupName = pathReference.substr(luIdx);

	// Find the correct lookup table index
	let lookupIndex = lookup.findIndex((s) => s.name == lookupName);

	// Return the display value from the lookup array
	let arr = lookup[lookupIndex].values;
	let idx = arr.findIndex((s) => s.value == valStr);
	idx = (idx == -1) ? 0 : idx;   // This fixes bad data coming in
	return (arr[idx].display == '(blank)') ? ' ' : arr[idx].display;
}

// Generic Look up display by value
function lookupFieldArr(val, arr) {
	// See if val is null
	if (val == undefined || val == null) return '';
	// Make sure val is a string
	let valStr = `${val}`;

	// See if val is blank or array is empty
	if (valStr == '' || arr.length == 0) return valStr;

	let idx = arr.findIndex((s) => s.value == valStr);
	idx = (idx == -1) ? 0 : idx;   // This fixes bad data coming in
	return (arr[idx].display == '(blank)') ? ' ' : arr[idx].display;
}

// Return all global choices
function lookupGlobalMultiChoiceArr(lookup, val, pathReference) {
	// See if val is null
	if (val == undefined || val == null) return '';

	// Get the path_reference name
	let luIdx = pathReference.indexOf('/') + 1;
	let lookupName = pathReference.substr(luIdx);

	// Find the correct lookup table index
	let lookupIndex = lookup.findIndex((s) => s.name == lookupName);

	// Return field with all choices
	let strChoice = '';
	let arr = lookup[lookupIndex].values;
	let idx;
	for (let i = 0; i < val.length; i++) {
		idx = arr.findIndex((s) => s.value == val[i]);
		if (idx != -1 && arr[idx].display != null) {
			strChoice += arr[idx].display + ', ';
		}
	}
	idx = strChoice.lastIndexOf(', ');
	strChoice = (idx == -1) ? strChoice : strChoice.substring(0, idx);

	return strChoice;
}

// Return all field choices
function lookupFieldMultiChoiceArr(val, arr) {
	// See if val is null
	if (val == undefined || val == null) return '';

	// Return field with all choices
	let strChoice = '';
	let idx;
	for (let i = 0; i < val.length; i++) {
		idx = arr.findIndex((s) => s.value == val[i]);
		if (idx != -1 && arr[idx].display != null) {
			strChoice += arr[idx].display + ', ';
		}


	}
	idx = strChoice.lastIndexOf(', ');
	strChoice = (idx == -1) ? strChoice : strChoice.substring(0, idx);

	return strChoice;
}

function getLookupField(lookup, data, metadata) {

	let retStr = '';

	if (metadata.hasOwnProperty('is_multiselect') && metadata.is_multiselect == true) {
		if (metadata.hasOwnProperty('path_reference') && metadata.path_reference.length > 0) {
			retStr = lookupGlobalMultiChoiceArr(lookup, data, metadata.path_reference);
		} else if (metadata.values.length > 0) {
			retStr = lookupFieldMultiChoiceArr(data, metadata.values);
		} else {
			retStr = chkNull(data);
		}
	} else if (metadata.hasOwnProperty('path_reference') && metadata.path_reference.length > 0) {
		retStr = lookupGlobalArr(lookup, data, metadata.path_reference);
	} else if (metadata.values.length > 0) {
		retStr = lookupFieldArr(data, metadata.values);
	} else {
		retStr = chkNull(data);
	}

	return retStr;
}

// Find section prompt name
function getSectionTitle(name) {
	if (name == 'all') {
	}

	let idx = g_md.children.findIndex((s) => s.name == name);
	idx = (idx == -1) ? 0 : idx;		// fixes bad data coming in
	return g_md.children[idx].prompt.toUpperCase();
}



const chart_start_increment_map = new Map();

chart_start_increment_map.set("blood_pressure_graph", { start: 40, increment: 20});
chart_start_increment_map.set("weight_gain_graph", { start: 100, increment: 20});
chart_start_increment_map.set("hematocrit_graph", { start: 10, increment: 2});
chart_start_increment_map.set("temperature_graph", { start: 90, increment: 2});
chart_start_increment_map.set("pulse_graph", { start: 0, increment: 10});
chart_start_increment_map.set("respiration_graph", { start: 0, increment: 2});


function doChart2
(
    p_id_prefix, 
    chartData, 
    chartTitle, 
    chartMessage,
    p_metadata
) 
{

    let minimum_graph_value = 0;
    let increment_graph_value = 10;
    let y_is_beginAtZero = true;
    
    if
    (
        chart_start_increment_map.has(p_metadata.name)
    )
    {
        const key_value = chart_start_increment_map.get(p_metadata.name);

        minimum_graph_value = key_value.start;
        increment_graph_value = key_value.increment;

        if (minimum_graph_value != 0)
        {
            y_is_beginAtZero = false;
        }

    }

	let wrapper_id = `${p_id_prefix}chartWrapper`;
	let container = document.getElementById(wrapper_id);

	if (container != null) {
		container.remove();
	}

	container = document.createElement('div')
	container.id = wrapper_id;
	document.body.appendChild(container);

	let canvas_id = `${p_id_prefix}myChart`;
	let canvas = document.createElement('canvas');
	canvas.id = canvas_id;
	// canvas.setAttribute('height', '300');
	canvas.setAttribute('width', '800');
	container.appendChild(canvas);

	const config = {
		type: 'line',
		data: chartData,
		options: {
			plugins: {
				title: {
					display: true,
					text: chartTitle,
					color: '#1010dd',
					font: {
						weight: 'bold',
						size: 36
					}
				}
			},
			maintainAspectRatio: false,
			responsive: true,
			animation: null,
			animate: false,
			scales: {
				y: {
					beginAtZero: y_is_beginAtZero,
                    min: minimum_graph_value,
                    //max: 450,
                    stepSize: increment_graph_value,
					ticks: {
						font: {
							size: 20,
						}
					}
				},
				x: {
          title: {
            display: true,
            text: chartMessage
          },
					ticks: {
						font: {
							size: 26,
						}
					}
				}
			},
		},
	};


	let myImgChart = new Chart(canvas.getContext('2d'), config);
	//const width = 300; //px
	//const height = 150; //px
	//const canvasRenderService = new CanvasRenderService(width, height);
	myImgChart.draw();
	myImgChart.render();

	return myImgChart.toBase64Image();
	// return canvas.toDataURL();
}
async function formatContent(p_ctx, arrMap) {
	let retContent = [];
	let body = [];
	let arrIndex;
	let sectionName = p_ctx.section_name;
	let ctx;

	switch (sectionName) {
		// Show all reports
		case 'all':
			if (p_ctx.metadata.children) {
				for (let i = 0; i < p_ctx.metadata.children.length; i++) {
					let child = p_ctx.metadata.children[i];
					if (child.type.toLowerCase() == 'form' && p_ctx.data[child.name] != null) {
						// Setup the correct ctx information to run the correct report
						let new_content = [];
						let new_context = {
							...p_ctx,
							metadata:
								child,
							content: new_content,
							data: p_ctx.data[child.name]
						};
						body = await print_pdf_render_content(new_context);

						// If not the Home Record and is all records, then do a page break
						if 
                        (
                            p_ctx.section_name == 'all' && 
                            child.name != 'home_record' &&
                            child.name != 'tracking'
                            
                        ) 
                        {
							body.unshift([{ text: '', pageBreak: 'before', colSpan: '2', }, {},]);
						}
						// Get the page header
						body.unshift([{ text: '', pageHeaderText: child.prompt, colSpan: '2', }, {},]);
						
						// console.log('**** child name: ',  child.name);
						// console.log('**** body: ', body);
						
						// Push the form to the stack
						retContent.push([
							{
								layout: {
									defaultBorder: false,
									paddingLeft: function (i, node) { return 1; },
									paddingRight: function (i, node) { return 1; },
									paddingTop: function (i, node) { return 2; },
									paddingBottom: function (i, node) { return 1; },
								},
								id: child.name,
								width: 'auto',
								table: {
									headerRows: 0,
									widths: [250, 'auto'],
									body: body,
								},
							},
						]);
					}
				}
			}
			break;

		// Core Summary
		case 'core-summary':
			retContent.push(await core_summary());
			break;

		// Show selected report
		default:
			if (p_ctx.metadata.children) {
				arrIndex = arrMap.findIndex((s) => s.name == sectionName);
				let child = p_ctx.metadata.children[arrIndex];
				if (child.type.toLowerCase() == 'form' && p_ctx.data[child.name] != null) {
					// Setup the ctx information to run the correct report
					ctx = { ...p_ctx, metadata: child, data: p_ctx.data[child.name] };
					body = await print_pdf_render_content(ctx);
					retContent.push([
						{
							layout: {
								defaultBorder: false,
								paddingLeft: function (i, node) { return 1; },
								paddingRight: function (i, node) { return 1; },
								paddingTop: function (i, node) { return 2; },
								paddingBottom: function (i, node) { return 1; },
							},
							id: child.name,
							width: 'auto',
							table: {
								headerRows: 0,
								widths: [250, 'auto'],
								body: body,
							},
						},
					]);
					body = [];
				}
			}
			break;
	}

	return retContent;
}


function convert_html_to_pdf(p_value) {
	//{ text: d.case_opening_overview, style: ['tableDetail'], },
	let result = [];
	let CommentRegex = /<!--\[[^>]+>/gi;

	let node = document.createElement("body");
	node.innerHTML = p_value.replace(CommentRegex, "");

	ConvertHTMLDOMWalker(result, node);

	return result;

}

function convert_attribute_to_pdf(p_node, p_result) 
{
	//{ text: d.case_opening_overview, style: ['tableDetail'], },
	let result = {};


	if (p_result != null) 
    {
		result = p_result;
	}

	/*
	font: string: name of the font
	fontSize: number: size of the font in pt
	fontFeatures: string[]: array of advanced typographic features supported in TTF fonts (supported features depend on font file)
	lineHeight: number: the line height (default: 1)
	bold: boolean: whether to use bold text (default: false)
	italics: boolean: whether to use italic text (default: false)
	alignment: string: (‘left’ or ‘center’ or ‘right’ or ‘justify’) the alignment of the text
	characterSpacing: number: size of the letter spacing in pt
	color: string: the color of the text (color name e.g., ‘blue’ or hexadecimal color e.g., ‘#ff5500’)
	background: string the background color of the text
	markerColor: string: the color of the bullets in a buletted list
	decoration: string: the text decoration to apply (‘underline’ or ‘lineThrough’ or ‘overline’)
	decorationStyle: string: the style of the text decoration (‘dashed’ or ‘dotted’ or ‘double’ or ‘wavy’)
	decorationColor: string: the color of the text decoration, see color
	
	text-align: right;
	font-size: 18px; 
	color: rgb(255, 0, 0); 
	background-color: rgb(0, 255, 0);
	*/


	if (p_node.attributes != null) 
    {

		for (let i = 0; i < p_node.attributes.length; i++) 
        {
			let attr = p_node.attributes[i];

			if (attr.name == "style") 
            {
				let style_array = attr.value.split(';');
				for (let style_index = 0; style_index < style_array.length; style_index++) 
                {
					let kvp = style_array[style_index].split(":");
					switch (kvp[0].trim()) 
                    {
						case "text-align":
							result['alignment'] = kvp[1].trim();
							break;
						case "font-size":
							result['fontSize'] = kvp[1].trim().replace("px", "").replace("pt", "").replace("rem", "");
							break;
							//case "bold": 
							//    result['bold']  = kvp[1];
							break;
						case "color":
							result['color'] = `${rgb_to_hex(kvp[1].trim())}`;
							break;
						case "background-color":
							result['background'] = `${rgb_to_hex(kvp[1].trim())}`;
							break;
						default:
							// console.log(`missing style: ${attr.name} = ${attr.value}`);
							break;
					}
				}

				break;
			}
		}
	}

	return result;

}

function rgb_to_hex(p_value) 
{
	if (p_value.split("(").length < 2) 
    {
		return p_value;
	}

	let a = p_value.split("(")[1].split(")")[0];
	a = a.split(",");
	let b = a.map
    (
        function (x) 
        {
		    x = parseInt(x).toString(16);
		    return (x.length == 1) ? "0" + x : x;
        }
    );

	return "#" + b.join("");
}

function GetTableHeader(p_result, p_node) 
{
	//if(p_result.length > 0) return;

	switch (p_node.nodeName.toUpperCase()) 
    {
		case "TH":
			p_result.push(p_node.textContent.trim());
			break;
		case "TR":
		default:
			for (let i = 0; i < p_node.childNodes.length; i++) 
            {
				let child = p_node.childNodes[i];

				GetTableHeader(p_result, child);
			}
			break;

	}
}

function GetTableDetailRow(p_result, p_node) 
{
	//if(p_result.length > 0) return;

	switch (p_node.nodeName.toUpperCase()) 
    {
		case "TD":
			p_result.push(p_node.textContent.trim());
			break;
		default:
			for (let i = 0; i < p_node.childNodes.length; i++) 
            {
				let child = p_node.childNodes[i];

				GetTableDetailRow(p_result, child);
			}
			break;

	}
}

function ConvertHTMLDOMWalker(p_result, p_node) 
{
	//let crlf_regex = /\n/g;

	switch (p_node.nodeName.toUpperCase()) 
    {
		case "TABLE":


			let header = [];
			let widths = [];
			let number_of_header_rows = 2;


			for (let i = 0; i < 1; i++) 
            {
				let child = p_node.childNodes[i];

				GetTableHeader(header, child);
			}

			let body = [];

			body.push(header);

			let tbody = null;
			//let tbody_index = 0;
			for (let i = 0; i < p_node.childNodes.length; i++) 
            {
				if (p_node.childNodes[i].nodeName.toUpperCase() == "TBODY") 
                {
					tbody = p_node.childNodes[i];
					//tbody_index = i;
					break;
				}
			}

			if (tbody != null) 
            {
				for (let i = 0; i < tbody.childNodes.length; i++) 
                {
					let child = tbody.childNodes[i];
					let detail_row = [];
					GetTableDetailRow(detail_row, child);

					if (detail_row.length > 0) 
                    {
						if (widths.length == 0) 
                        {
							if (header.length > 0) 
                            {

								header = detail_row;

								number_of_header_rows = 1;

								for (let col_count = 0; col_count < detail_row.length; col_count++) 
                                {
									widths.push("auto");
								}
							}
							else 
                            {
								for (let col_count = 0; col_count < detail_row.length; col_count++) 
                                {
									widths.push("auto");
								}

								while (header.length < widths.length) 
                                {
									header.push("");
								}

								body.push(detail_row);
							}
						}
						else 
                        {
							body.push(detail_row);
						}

					}
				}
			}

			if (header.length != widths.length && header.length != max_detail) 
            {
				//console.log("here");
			}

			let table = {
				layout: 'lightHorizontalLines', // optional
				table: {
					headerRows: number_of_header_rows,
					widths: widths,

					body: body
				}
			};

			p_result.push(table);
			return;
			break;
		case "#TEXT":
			p_result.push({ text: p_node.textContent.trim().replace("<br>", "\n") });
			return;
			break;
		case "P":
		case "DIV":
			let text_array = [];
			for (let i = 0; i < p_node.childNodes.length; i++) {
				let child = p_node.childNodes[i];
				ConvertHTMLDOMWalker(text_array, child);
			}
			text_array.push({ text: "\n" });
			p_result.push({ text: text_array, style: convert_attribute_to_pdf(p_node)});
			return;
        case "SPAN":
            let span_text_array = [];
            for (let i = 0; i < p_node.childNodes.length; i++) {
                let child = p_node.childNodes[i];
                ConvertHTMLDOMWalker(span_text_array, child);
            }

            if(span_text_array.length == 1)
            {
                p_result.push({ text: ' ' + span_text_array[0].text,  style: convert_attribute_to_pdf(p_node) });
            }
            else
            {
                p_result.push({ text: span_text_array,  style: convert_attribute_to_pdf(p_node) });
            }
            return;
			break;
		case "STRONG":
        case "B":
			let strong_attr = { bold: true };
			p_result.push({ text: p_node.textContent.trim(), style: convert_attribute_to_pdf(p_node, strong_attr) });
			return;
			break;
		case "BR":
			p_result.push({ text: "\n" });
			return;
			break;
		case "EM":
			let em_attr = { italics: true };
			p_result.push({ text: p_node.textContent.trim(), style: convert_attribute_to_pdf(p_node, em_attr) });
			return;
			break;
		case "UL":
			let ul_array = [];
			for (let i = 0; i < p_node.childNodes.length; i++) {
				let child = p_node.childNodes[i];

				ConvertHTMLDOMWalker(ul_array, child);
			}
			p_result.push({ ul: ul_array });
			return;
			break;

		case "OL":
			let ol_array = [];
			for (let i = 0; i < p_node.childNodes.length; i++) {
				let child = p_node.childNodes[i];

				ConvertHTMLDOMWalker(ol_array, child);
			}
			p_result.push({ ol: ol_array });
			return;
			break;
		case "LI":
			if (p_node.childNodes.length > 1) 
            {
				let li_array = [];
				for (let i = 0; i < p_node.childNodes.length; i++) 
                {
					let child = p_node.childNodes[i];

					ConvertHTMLDOMWalker(li_array, child);
				}
				const text_only = [];
                process_li_array(text_only, li_array);

                p_result.push({ text: text_only });

			} 
            else 
            {
				let li_node = { text: p_node.textContent.trim() }
				p_result.push(convert_attribute_to_pdf(p_node, li_node));
			}
			return;
			break;

	}


	for (let i = 0; i < p_node.childNodes.length; i++) {
		let child = p_node.childNodes[i];

		ConvertHTMLDOMWalker(p_result, child);
	}


}

function process_li_array(p_result, p_array)
{
    for (let i = 0; i < p_array.length; i++) 
    {
        const item = p_array[i];

        if(Array.isArray(item.text))
        {
            process_li_array(p_result, item.text);
        }
        else
        {
            p_result.push(item.text);
        }

    }
}

// Core Summary - display all of the core summary fields
async function core_summary() {
	let body = [];
	// let arrMap = getArrayMap();

	// Record Core Fields
	let retPage = [];

	// let arrIndex = arrMap.findIndex((s) => s.name == 'home_record');
	body = core_pdf_summary(g_md, g_d, '/', false, '');

	// Show the table
	retPage.push([
		{
			layout: {
				defaultBorder: false,
				paddingLeft: function (i, node) { return 1; },
				paddingRight: function (i, node) { return 1; },
				paddingTop: function (i, node) { return 2; },
				paddingBottom: function (i, node) { return 2; },
			},
			table: {
				headerRows: 0,
				widths: [250, '*'],
				body: body,
			},
		},
	]);

	return retPage;
}

function core_pdf_summary(p_metadata, p_data, p_path, p_is_core_summary, p_metadata_path) {
	let is_core_summary = false;

	if (p_is_core_summary) {
		is_core_summary = true;
	}

	let result = [];
	switch (p_metadata.type.toLowerCase()) {
		case 'group':
			if (g_metadata_summary[p_metadata_path].is_core_summary > 0 ||
				p_metadata.is_core_summary && p_metadata.is_core_summary == true) {
				result.push([
					{
						text: `${p_metadata.prompt}: `,
						style: ['subHeader', 'isUnderLine'],
						colspan: '2',
						margin: [5, 0, 0, 0],
					},
					{},
				]);
				if (p_metadata.is_core_summary || p_metadata.is_core_summary == true) {
					is_core_summary = true;
				}

				if (p_metadata.children) {
					for (let i = 0; i < p_metadata.children.length; i++) {
						let child = p_metadata.children[i];
						if (p_data[child.name] != null)
							Array.prototype.push.apply(result, core_pdf_summary(child, p_data[child.name], p_path + "." + child.name, is_core_summary, p_metadata_path + ".children[" + i + "]"));
					}
				}
			}
			break;
		case 'form':
			if (
				g_metadata_summary[p_metadata_path].is_core_summary > 0 &&
				(
					p_metadata.cardinality == "+" || p_metadata.cardinality == "*"
				)
			) {
				result.push([
					{
						text: p_metadata.prompt,
						style: ['coreHeader'],
						colSpan: '2',
						margin: [0, 5, 0, 0],
					},
					{},
				]);
				for (let form_index = 0; form_index < p_data.length; form_index++) {
					let form_item = p_data[form_index];
					result.push([
						{ text: `Record: ${form_index + 1}`.toUpperCase(), style: ['tableLabel'], colSpan: '2', },
						{},
					]);
					if (p_metadata.children) {
						for (let i = 0; i < p_metadata.children.length; i++) {
							let child = p_metadata.children[i];
							if (form_item[child.name] != null)
								Array.prototype.push.apply(result, core_pdf_summary(child, form_item[child.name], p_path + "." + child.name, is_core_summary, p_metadata_path + ".children[" + i + "]"));
						}
					}
				}
			}
			else if (g_metadata_summary[p_metadata_path].is_core_summary > 0) {
				result.push([
					{ text: `${p_metadata.prompt}: `, style: ['coreHeader'], colSpan: '2', },
					{},
				]);
				if (p_metadata.children) {
					for (let i = 0; i < p_metadata.children.length; i++) {
						let child = p_metadata.children[i];
						if (p_data[child.name] != null)
							Array.prototype.push.apply(result, core_pdf_summary(child, p_data[child.name], p_path + "." + child.name, is_core_summary, p_metadata_path + ".children[" + i + "]"));
					}
				}
			}
			break;
		case "grid":
			let body = [];
			let row;
			let colWidths;
			let colspan = 0;
            if(p_metadata.is_hidden && !g_show_hidden)
            {
                break;
            }

			if (p_metadata.is_core_summary && p_metadata.is_core_summary == true) {
				// Get the number of columns
				colspan = p_metadata.children.length;
				row = new Array();
				row.push({ text: p_metadata.prompt, style: ['tableLabel', 'blueFill'], colSpan: colspan, },);

				// Add the extra {} for the columns so it doesn't break
				for (let j = 1; j < colspan; j++) {
					row.push({},);
				}
				body.push(row);

				// Add the appropriate number of column widths so it will be at 100% across the page
				colWidths = new Array();
				colWidths.push('auto',);
				for (let j = 1; j < colspan; j++) {
					colWidths.push('*',);
				}

				row = new Array();
				for (let j = 0; j < p_metadata.children.length; j++) {
					let child = p_metadata.children[j];
					row.push({ text: child.prompt, style: ['tableLabel', 'blueFill'], },);
				}
				body.push(row);
				row = new Array();
				for (let i = 0; i < p_data.length; i++) {
					if (p_data[i] != null) {
						for (let j = 0; j < p_metadata.children.length; j++) {
							let child = p_metadata.children[j];
							if (p_data[i][child.name] != null) {
								if (child.type == 'list') {
									let textStr;
									if (child.values.length == 0) {
										textStr = lookupGlobalArr(g_md.lookup, p_data[i][child.name], child.path_reference.substring(child.path_reference.indexOf('/') + 1));
									}
									else {
										textStr = lookupFieldArr(p_data[i][child.name], child.values);
									}
									row.push({ text: textStr, style: ['tableDetail'], },);
								}
								else {
									row.push({ text: p_data[i][child.name], style: ['tableDetail'], },);
								}
							}
							else {
								row.push({ text: '', style: ['tableDetail'], },);
							}
						}
						body.push(row);
						row = new Array();
					}
				}
			}
			else if (g_metadata_summary[p_metadata_path].is_core_summary > 0) {
				for (let j = 0; j < p_metadata.children.length; j++) {
					let child = p_metadata.children[j];
					if (child.is_core_summary && child.is_core_summary == true) {
						colspan = colspan + 1;
					}
				}

				// Add the 1st line of the header
				row = new Array();
				row.push({ text: p_metadata.prompt, style: ['tableLabel', 'blueFill'], colSpan: colspan, },);

				// Add the extra {} for the columns so it doesn't break
				for (let j = 1; j < colspan; j++) {
					row.push({},);
				}
				body.push(row);

				// Add the appropriate number of column widths so it will be at 100% across the page
				colWidths = new Array();
				colWidths.push('auto',);
				for (let j = 1; j < colspan; j++) {
					colWidths.push('*',);
				}

				row = new Array();
				for (let j = 0; j < p_metadata.children.length; j++) {
					let child = p_metadata.children[j];
					if (child.is_core_summary && child.is_core_summary == true) {
						row.push({ text: child.prompt, style: ['tableLabel', 'blueFill'], })
					}
				}
				body.push(row);
				row = new Array();
				for (let i = 0; i < p_data.length; i++) {
					if (p_data[i] != null) {
						for (let j = 0; j < p_metadata.children.length; j++) {
							let child = p_metadata.children[j];
							if (child.is_core_summary && child.is_core_summary == true) {
								if (p_data[i][child.name] != null) {
									if (child.type == 'list') {
										let textStr;
										if (child.values.length == 0) {
											textStr = lookupGlobalArr(g_md.lookup, p_data[i][child.name], child.path_reference.substring(child.path_reference.indexOf('/') + 1));
										}
										else {
											textStr = lookupFieldArr(p_data[i][child.name], child.values);
										}
										row.push({ text: textStr, style: ['tableDetail'], },);
									}
									else {
										row.push({ text: p_data[i][child.name], style: ['tableDetail'], },);
									}
								}
								else {
									row.push({ text: '', style: ['tableDetail'], },);
								}
							}
						}
						body.push(row);
						row = new Array();
					}
				}
			}
			// Display the grid table
			if (body.length > 0) {
				result.push([
					{
						table: {
							widths: colWidths,
							headerRows: 2,
							body: body
						},
						colSpan: '2',
					},
					{},
				]);
			}
			break;
		case 'app':
			if 
            (
				(
                    p_metadata.is_core_summary || p_metadata.is_core_summary == true) ||
				is_core_summary == true
			) {
				result.push([
					{ text: `${p_metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
					{ text: p_data[p_metadata.name], style: ['tableDetail'], },
				]);
			}

			if (p_metadata.children) 
            {
				for (let i = 0; i < p_metadata.children.length; i++) 
                {
					let child = p_metadata.children[i];
					if (child.type.toLowerCase() == "form" && p_data[child.name] != null)
						Array.prototype.push.apply(result, core_pdf_summary(child, p_data[child.name], p_path + "." + child.name, is_core_summary, "g_metadata.children[" + i + "]"));
				}
			}
			break;
		case 'list':
			if 
            (
				(
                    p_metadata.is_core_summary || 
                    p_metadata.is_core_summary == true
                ) ||
				is_core_summary == true
			) {

				let data_value_list = p_metadata.values;
				let list_lookup = {};

				if (p_metadata.path_reference && p_metadata.path_reference != "") {
					data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));

					if (data_value_list == null) {
						data_value_list = p_metadata.values;
					}
				}

				for (let list_index = 0; list_index < data_value_list.length; list_index++) {
					let list_item = data_value_list[list_index];
					list_lookup[list_item.value] = list_item.display;
				}

				if (Array.isArray(p_data)) {
					let choiceList = '';
					for (let i = 0; i < p_data.length; i++) {
						if
							(
							(p_data[i] == 9999 || p_data[i] == "9999") &&
							p_data.length > 1
						) {
							continue;
						}
						choiceList += `${list_lookup[p_data[i]]}, `;
					}
					if (choiceList.length > 0) {
						choiceList = choiceList.slice(0, -2);
					}
					result.push([
						{ text: `${p_metadata.prompt}: `, style: ['tableLabel'], alignment: 'right' },
						{ text: `${choiceList}`, style: ['tableDetail'], },
					]);
				}
				else {
					result.push([
						{ text: `${p_metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
						{ text: list_lookup[p_data], style: ['tableDetail'], },
					]);
				}
			}
			break;
		default:
			if 
            (
				(
                    p_metadata.is_core_summary || 
                    p_metadata.is_core_summary == true
                ) ||
				is_core_summary == true
			) 
            {
				result.push([
					{ text: `${p_metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
					{ text: p_data, style: ['tableDetail'], },
				]);
			}

			if (p_metadata.children) 
            {
				for (let i = 0; i < p_metadata.children.length; i++) 
                {
					let child = p_metadata.children[i];
					if (p_data[child.name] != null)
						Array.prototype.push.apply(result, core_pdf_summary(child, p_data[child.name], p_path + "." + child.name, is_core_summary));
				}
			}
			break;
	}

	return result;
}

function convert_dictionary_path_to_lookup_object(p_path) {
	//g_data.prenatal.routine_monitoring.systolic_bp
	let result = null;
	let temp_result = []
	let temp = "g_metadata." + p_path.replace(new RegExp('/', 'gm'), ".").replace(new RegExp('\\.(\\d+)\\.', 'gm'), "[$1].").replace(new RegExp('\\.(\\d+)$', 'g'), "[$1]");
	let index = temp.lastIndexOf('.');
	temp_result.push(temp.substr(0, index));
	temp_result.push(temp.substr(index + 1, temp.length - (index + 1)));

	let lookup_list = eval(temp_result[0]);

	for (let i = 0; i < lookup_list.length; i++) {
		if (lookup_list[i].name == temp_result[1]) {
			result = lookup_list[i].values;
			break;
		}
	}

	return result;
}

// Recursive function to traverse the metadata
function print_pdf_render_content(ctx) {
	// Find the correct type
	// console.log('ctx: ', ctx);
	switch (ctx.metadata.type.toLowerCase()) {
		case "app":
			console.log('in APP');
			break;
		case "form":
			// console.log('*************** type: ', ctx.metadata.type);
			// multiform 
			if (ctx.metadata.cardinality == '*' || ctx.metadata.cardinality == '+') {
				if (ctx.metadata.children && ctx.data.length > 0) {
					// See if we do all records or just one
					let startArr = 0;
					let endArr = ctx.data.length;
					if (typeof ctx.record_number != 'undefined') {
						startArr = ctx.record_number - 1;
						endArr = startArr + 1;
					}
					for (startArr; startArr < endArr; startArr++) {
						if (typeof ctx.record_number == 'undefined' && startArr > 0) {
							ctx.content.push([
								{ text: '', pageBreak: 'before', colSpan: '2', },
								{},
							]);
						}

						// Display the record number
						ctx.content.push([
							{ text: `Record #${startArr + 1}`.toUpperCase(), style: ['subHeader'], colSpan: '2', },
							{},
						]);

						let formItem = ctx.data[startArr];
						ctx.metadata.children.forEach((child) => {
							if (formItem[child.name] || child.type == 'chart') {
								let new_content = [];
								let new_context = {
									...ctx,
									metadata: child,
									data: formItem[child.name],
									mmria_path: ctx.mmria_path + "/" + child.name,
									content: new_content,
									multiFormIndex: startArr,
								};
								let ret = print_pdf_render_content(new_context);
								ctx.content.push(...ret);
							}
						});
					}
				} else {
					ctx.content.push([
						{ text: 'No records entered', style: ['tableDetail'], colSpan: '2' },
						{},
					]);
				}
			} else {
				if (ctx.metadata.children) {
					ctx.metadata.children.forEach((child, index) => {
						if ((ctx.data && ctx.data[child.name]) || child.type == 'chart') {
							let new_content = [];
							let new_context = {
								...ctx,
								metadata: child,
								data: ctx.data[child.name],
								mmria_path: ctx.mmria_path + "/" + child.name,
								content: new_content,
							};
							let ret = print_pdf_render_content(new_context);
							ctx.content.push(...ret);
						}
					});
				}
			}
			// Check to see if any records were add, if not, then push a no records line
			if (ctx.content.length == 0) {
				ctx.content.push([
					{ text: 'No records entered', style: ['tableDetail'], colSpan: '2' },
					{},
				]);
			}
			break;
		case "group":
			// console.log('*************** type: ', ctx.metadata.type);
			// ****************************************************************
			// *** The 1st if statement will see if the group has 3 children 
			// *** and they are month, day and year
			// ***
			// *** The 2nd if statement will check to see if there are more
			// *** than 3 children and if the first 3 are month, day and year.
			// *** It will then do the date and any other fields that are there.
			// *** 
			// *** The last else statement will just process thru the children
			// *** 
			// ****************************************************************
			if (ctx.metadata.children.length == 3
				&& ctx.metadata.children[0].name == 'month'
				&& ctx.metadata.children[1].name == 'day'
				&& ctx.metadata.children[2].name == 'year'
			) {
				ctx.content.push([
					{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
					{ text: fmtDateByFields(ctx.data), style: ['tableDetail'], },
				]);
			} else if (ctx.metadata.children.length > 3
				&& ctx.metadata.children[0].name == 'month'
				&& ctx.metadata.children[1].name == 'day'
				&& ctx.metadata.children[2].name == 'year'
			) {
				ctx.content.push([
					{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
					{ text: fmtDateByFields(ctx.data), style: ['tableDetail'], },
				]);
				for (let i = 3; i < ctx.metadata.children.length; i++) {
					let child = ctx.metadata.children[i];
					if (ctx.data) {
						let new_content = [];
						let new_context = {
							...ctx,
							metadata: child,
							data: ctx.data[child.name],
							mmria_path: ctx.mmria_path + "/" + child.name,
							content: new_content,
							groupLevel: ctx.groupLevel + 1,
						};
						let ret = print_pdf_render_content(new_context);
						if (ret.length > 0) {
							ctx.content.push(...ret);
						}
					}
				}
			} else {
				// See if the record was imported by CDC Automated Vitals
				// 	If the automated_vitals_group child is NOT the automated user
				//	Then skip it
				let showIt = true;
				if (ctx.metadata.name == 'automated_vitals_group' ||
					ctx.metadata.name == 'vitals_import_group') {
					showIt = ctx.createdBy == 'vitals-import' ? true : false;
				}
				if (showIt) {
					if (ctx.groupLevel == 0) {
						ctx.content.push([
							{ text: ctx.metadata.prompt, style: ['subHeader'], colSpan: '2', },
							{},
						]);
					}
					ctx.metadata.children.forEach((child, index) => {
						if (ctx.data && showIt) {
							let new_content = [];
							let new_context = {
								...ctx,
								metadata: child,
								data: ctx.data[child.name],
								mmria_path: ctx.mmria_path + "/" + child.name,
								content: new_content,
								groupLevel: ctx.groupLevel + 1,
							};
							let ret = print_pdf_render_content(new_context);
							if (ret.length > 0) {
								ctx.content.push(...ret);
							}
						}
					});
				}
			}
			break;
		case "grid":
			// console.log('*************** type: ', ctx.metadata.type);
			let gridBody = [];
			let row;
			let colWidths;
			let colspan = 0;

            if(ctx.metadata.is_hidden && !g_show_hidden)
            {
                break;
            }

			// Check to see if Committee Decisions / Recommendations of the Committee is blank
			if (ctx.metadata.name == 'recommendations_of_committee' && ctx.data.length == 0) {
				break;
			}

			// Get the number of columns in the grid 
			colspan = ctx.metadata.children.length;

			// If grid is over a certain size, then do something different
            if 
            (
                ctx.metadata.name == 'cvs_grid' 
			) 
            {

                ctx.content.push([{ text: '', margin: [0, 10, 0, 0], colSpan: '2', }, {},],);

				colWidths = new Array();
				colWidths = [30, 450];
				row = new Array();
				row.push
                (
					{ text: ctx.metadata.prompt, style: ['gridHeader', 'blueFill'], colSpan: '2', },
					{}
                );
				gridBody.push(row);
				row = new Array();
				row.push({ text: 'Rec #', style: ['tableLabel', 'blueFill'], alignment: 'center', },);
				//row.push({ text: 'Date', style: ['tableLabel', 'blueFill'], },);
				row.push({ text: 'Community Vital Signs Data', style: ['tableLabel', 'blueFill'], },);
				//row.push({ text: 'Comment(s)', style: ['tableLabel', 'blueFill'], },);
				gridBody.push(row);

				// Are there any records?
				if (ctx.data.length == 0) 
                {
					row = new Array();
					row.push
                    (
                        { text: 'No records entered', style: ['tableDetail'], colSpan: '2', },
						{}
                    );
					gridBody.push(row);
				} 
                else 
                {
					// Build the table detail
					let metaChild = ctx.metadata.children;
					ctx.data.forEach((dataChild, dataIndex) => {
						row = new Array();
						row.push({ text: `${dataIndex + 1}`, style: ['tableDetail', 'isItalics', 'isBold'], alignment: 'center', },);
						
						// Create a two column table for the Medical Info column - exclude the first (datetime) and last (comments)  
						let colPrompt = new Array();
						let colData = new Array();
						for (let i = 1; i < metaChild.length - 1; i++) 
                        {
							switch (metaChild[i].type.toLowerCase()) 
                            {
								case 'list':
									colPrompt.push({ text: `${metaChild[i].prompt}:  `, style: ['tableLabel'], alignment: 'right', },);
									colData.push({ text: getLookupField(ctx.lookup, dataChild[metaChild[i].name], metaChild[i]), style: ['tableDetail'], },);
									break;
								case 'string':
								case 'number':
								case 'time':
								case 'hidden':
									colPrompt.push({ text: `${metaChild[i].prompt}:  `, style: ['tableLabel'], alignment: 'right', },);
									colData.push({ text: dataChild[metaChild[i].name] || '-', style: ['tableDetail'], },);
									break;
								default:
									colPrompt.push({ text: `${metaChild[i].prompt}:  `, style: ['tableLabel'], alignment: 'right', },);
									colData.push({ text: ' DEFAULT', style: ['tableDetail'], },);
									break;
							}
						}

						// Put it into a table
						row.push({ columns: [colPrompt, colData], },);
						
						gridBody.push(row)
					});
				}
			} 
            else if 
            (
                ctx.metadata.name == 'transport_vital_signs' ||
				ctx.metadata.name == 'vital_signs' ||
				ctx.metadata.name == 'laboratory_tests' ||
				ctx.metadata.name == 'routine_monitoring' 
			) 
            {
				colWidths = new Array();
				colWidths = [30, 100, 200, '*'];
				row = new Array();
				row.push(
					{ text: ctx.metadata.prompt, style: ['gridHeader', 'blueFill'], colSpan: '4', },
					{}, {}, {});
				gridBody.push(row);
				row = new Array();
				row.push({ text: 'Rec #', style: ['tableLabel', 'blueFill'], alignment: 'center', },);
				row.push({ text: 'Date', style: ['tableLabel', 'blueFill'], },);
				row.push({ text: 'Medical Information', style: ['tableLabel', 'blueFill'], },);
				row.push({ text: 'Comment(s)', style: ['tableLabel', 'blueFill'], },);
				gridBody.push(row);

				// Are there any records?
				if (ctx.data.length == 0) 
                {
					row = new Array();
					row.push({ text: 'No records entered', style: ['tableDetail'], colSpan: '4', },
						{}, {}, {});
					gridBody.push(row);
				} 
                else 
                {
					// Build the table detail
					let metaChild = ctx.metadata.children;
					ctx.data.forEach((dataChild, dataIndex) => {
						row = new Array();
						row.push({ text: `${dataIndex + 1}`, style: ['tableDetail', 'isItalics', 'isBold'], alignment: 'center', },);
						row.push({ text: fmtDateTime(dataChild[metaChild[0].name]), style: ['tableDetail'], },);
						// Create a two column table for the Medical Info column - exclude the first (datetime) and last (comments)  
						let colPrompt = new Array();
						let colData = new Array();
						for (let i = 1; i < metaChild.length - 1; i++) {
							switch (metaChild[i].type.toLowerCase()) {
								case 'list':
									colPrompt.push({ text: `${metaChild[i].prompt}: `, style: ['tableLabel'], alignment: 'right', },);
									colData.push({ text: getLookupField(ctx.lookup, dataChild[metaChild[i].name], metaChild[i]), style: ['tableDetail'], },);
									break;
								case 'string':
								case 'number':
								case 'time':
								case 'hidden':
									colPrompt.push({ text: `${metaChild[i].prompt.replace(" - ", "-")}: `, style: ['tableLabel'], alignment: 'right', },);
									colData.push({ text: dataChild[metaChild[i].name] || '-', style: ['tableDetail'], },);
									break;
								default:
									colPrompt.push({ text: `${metaChild[i].prompt}: `, style: ['tableLabel'], alignment: 'right', },);
									colData.push({ text: ' DEFAULT', style: ['tableDetail'], },);
									break;
							}
						}

						// Put it into a table
						row.push({ columns: [colPrompt, colData], },);
						row.push({ text: chkNull(dataChild[metaChild[metaChild.length - 1].name]), style: ['tableDetail'], },);
						gridBody.push(row)
					});
				}
			} 
            else if (ctx.metadata.children[ctx.metadata.children.length - 1].name == 'comments' ||
				ctx.metadata.children[ctx.metadata.children.length - 1].name == 'comment' ||
				ctx.metadata.children[ctx.metadata.children.length - 1].name == 'pregrid_comments') {
				// Save the comment field name
				let commentFieldName = ctx.metadata.children[ctx.metadata.children.length - 1].name;
				// Get the number of fields
				colWidths = new Array();
				// The 30 is for the record number and the auto is to make it use the whole width
				colWidths.push(30, 'auto',);
				let adjColspan = colspan - 1;
				for (let j = 1; j < adjColspan; j++) {
					colWidths.push('auto',);
				};

				// Do the header row
				row = new Array();
				row.push({ text: ctx.metadata.prompt, style: ['gridHeader', 'blueFill'], colSpan: `${colWidths.length}`, },);
				// Add the extra {} for the columns so it doesn't break;
				for (let j = 1; j < colWidths.length; j++) {
					row.push({},);
				}
				gridBody.push(row);

				// Do header columns
				row = new Array();
				row.push({ text: 'Rec #', style: ['tableLabel', 'blueFill'], alignment: 'center', },);
				for (let j = 0; j < adjColspan; j++) {
					row.push({ text: ctx.metadata.children[j].prompt, style: ['tableLabel', 'blueFill'], },);
				}
				gridBody.push(row);

				// Do the detail lines
				// Check to see if there are records, if not then tell them so
				if (ctx.data.length == 0) {
					row = new Array();
					row.push({ text: 'No records entered', style: ['tableDetail'], colSpan: `${colWidths.length}`, },);
					for (let i = 1; i < colWidths.length; i++) {
						row.push({},);
					}
					gridBody.push(row);
				} else {
					ctx.data.forEach((dataChild, dataIndex) => {
						// Add the record number
						let gridBorder = ( chkNull(dataChild[commentFieldName]).length > 0 ) 
							? [true, true, true, false] 
							: [true, true, true, true];
						row = new Array();
						row.push(
							{
								text: `${dataIndex + 1}`,
								style: ['tableDetail', 'isItalics', 'isBold'],
								alignment: 'center',
								border: gridBorder,
							});
						for (let i = 0; i < adjColspan; i++) {
							let metaChild = ctx.metadata.children[i];
							switch (metaChild.type.toLowerCase()) {
								case 'list':
									row.push({ text: getLookupField(ctx.lookup, dataChild[metaChild.name], metaChild), style: ['tableDetail'], },);
									break;
								case 'string':
								case 'number':
								case 'time':
									row.push({ text: chkNull(dataChild[metaChild.name]), style: ['tableDetail'], },);
									break;
								case 'textarea':
									row.push({ text: chkNull(dataChild[metaChild.name]), style: ['tableDetail'], },);
									break;
								case 'date':
									row.push({ text: reformatDate(dataChild[metaChild.name]), style: ['tableDetail'], },);
									break;
								case 'datetime':
									row.push({ text: fmtDateTime(dataChild[metaChild.name]), style: ['tableDetail'], },);
									break;
								case 'hidden':
								default:
									break;
							}
						}
						gridBody.push(row);
						// See if we need to add the comment header and value
						if ( chkNull(dataChild[commentFieldName]).length > 0 ) {
							row = new Array();
							// Allow for first column that has been row spanned (*** Tried to use the row span but it doesn't work, so I am faking it)
							row.push({ text: '', border: [true, false, true, false], },);
							row.push({ text: ctx.metadata.children[adjColspan].prompt, style: ['tableLabel', 'lightBlueFill'], colSpan: `${adjColspan}` },);
							for (let i = 1; i < adjColspan; i++) {
								row.push({},);
							}
							gridBody.push(row);
							row = new Array();
							row.push({ text: '', border: [true, false, true, true], },);
							row.push({ text: chkNull(dataChild[commentFieldName]), style: ['tableDetail'], colSpan: `${adjColspan}` });
							for (let i = 1; i < adjColspan; i++) {
								row.push({},);
							}
							gridBody.push(row);
						}
					});
				}
			} else {
				// Get the number of fields
				colWidths = new Array();
				// The 30 is for the record number and the auto is to make it use the whole width
				colWidths.push(30, 'auto',);
				// Do this to make sure auto is the 1st column width and we bypass it in the count
				let jStart = ctx.metadata.children[0].type == 'label' ? 2 : 1;
				for (let j = jStart; j < colspan; j++) {
					// Only add a column if it is NOT a label
					if (ctx.metadata.children[j].type != 'label') {
						colWidths.push('auto',);
					};
				}

				// Do the header row
				row = new Array();
				row.push({ text: ctx.metadata.prompt, style: ['gridHeader', 'blueFill'], colSpan: `${colWidths.length}`, },);
				// Add the extra {} for the columns so it doesn't break
				for (let j = 1; j < colWidths.length; j++) {
					row.push({},);
				}
				gridBody.push(row);

				row = new Array();
				row.push({ text: 'Rec #', style: ['tableLabel', 'blueFill'], alignment: 'center', },);
				ctx.metadata.children.forEach((child, index) => {
					// Only add a column if it is NOT a label
					if (child.type != 'label') {
						row.push({ text: child.prompt, style: ['tableLabel', 'blueFill'], },);
					}
				});
				gridBody.push(row);

				// Do the detail lines
				row = new Array();
				// Check to see if there are records, if not then tell them so
				if (ctx.data.length == 0) {
					row.push({ text: 'No records entered', style: ['tableDetail'], colSpan: `${colWidths.length}`, },);
					for (let i = 1; i < colWidths.length; i++) {
						row.push({},);
					}
					gridBody.push(row);
				} else {
					// Get the fields for each row
					ctx.data.forEach((dataChild, dataIndex) => {
						// Add the record number
						row.push({ text: `${dataIndex + 1}`, style: ['tableDetail', 'isItalics', 'isBold'], alignment: 'center', },);
						// get metadata info for each row
						ctx.metadata.children.forEach((metaChild, metaIndex) => {
							switch (metaChild.type.toLowerCase()) {
								case 'list':
									row.push({ text: getLookupField(ctx.lookup, dataChild[metaChild.name], metaChild), style: ['tableDetail'], },);
									break;
								case 'string':
								case 'number':
								case 'time':
									row.push({ text: chkNull(dataChild[metaChild.name]), style: ['tableDetail'], },);
									break;
								case 'textarea':
									row.push({ text: chkNull(dataChild[metaChild.name]), style: ['tableDetail'], },);
									break;
								case 'date':
									row.push({ text: reformatDate(dataChild[metaChild.name]), style: ['tableDetail'] },);
									break;
								case 'datetime':
									row.push({ text: fmtDateTime(dataChild[metaChild.name]), style: ['tableDetail'] },);
									break;
								case 'hidden':
								default:
									break;
							}
						});
						if (row.length > 0) {
							gridBody.push(row);
						}
						row = new Array();
					});
				}
			}

			// Display the grid table
			if (gridBody.length > 0) {
				ctx.content.push([
					{
						colSpan: '2',
						table: {
							widths: colWidths,
							headerRows: 2,
							body: gridBody,
						},
					}, {},
				]);
				// Add a little space before the next thing
				ctx.content.push([{ text: '', margin: [0, 10, 0, 0], colSpan: '2', }, {},],);
			}
			break;
		case "string":
		case "number":
		case "time":
		case "jurisdiction":
			// console.log('*************** type: ', ctx.metadata.type);
			ctx.content.push([
				{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
				{ text: chkNull(ctx.data), style: ['tableDetail'], },
			]);
			break;
		case "date":
			// console.log('*************** type: ', ctx.metadata.type);
			ctx.content.push([
				{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
				{ text: reformatDate(ctx.data), style: ['tableDetail'] },
			]);
			break;
		case "datetime":
			// console.log('*************** type: ', ctx.metadata.type);
			ctx.content.push([
				{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
				{ text: fmtDateTime(ctx.data), style: ['tableDetail'] },
			]);
			break;
		case "list":
			// console.log('*************** type: ', ctx.metadata.type);
			// **************************************************************************
			// *** The 1st if will see if is_multiselect exists and set to true
			// ***		Then check to see if  path_reference exist and if it exists
			// ***		then call lookupGlobalMultiChoiceArr else call
			// ***		lookupFieldMultiChoiceArr - both will return a list of
			// ***		display values or just return what is in the field in case
			// ***		there is a problem
			// ***
			// *** The 2nd if will check to see if path_reference exists and then call
			// *** lookupGlobalArr and return the display value
			// ***
			// *** The 3rd if will check to see if the values array has length and then
			// *** call lookupFieldArr and return the display value
			// ***
			// *** Else just return the data value - just in case there is a problem
			// **************************************************************************

			if (ctx.metadata.hasOwnProperty('is_multiselect') && ctx.metadata.is_multiselect == true) {
				if (ctx.metadata.hasOwnProperty('path_reference') && ctx.metadata.path_reference.length > 0) {
					ctx.content.push([
						{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
						{ text: lookupGlobalMultiChoiceArr(ctx.lookup, ctx.data, ctx.metadata.path_reference), style: ['tableDetail'] },
					]);
				} else if (ctx.metadata.values.length > 0) {
					ctx.content.push([
						{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
						{ text: lookupFieldMultiChoiceArr(ctx.data, ctx.metadata.values), style: ['tableDetail'] },
					]);
				} else {
					ctx.content.push([
						{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
						{ text: chkNull(ctx.data), style: ['tableDetail'] },
					]);
				}
			} else if (ctx.metadata.hasOwnProperty('path_reference') && ctx.metadata.path_reference.length > 0) {
				ctx.content.push([
					{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
					{ text: lookupGlobalArr(ctx.lookup, ctx.data, ctx.metadata.path_reference), style: ['tableDetail'] },
				]);
			} else if (ctx.metadata.values.length > 0) {
				ctx.content.push([
					{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
					{ text: lookupFieldArr(ctx.data, ctx.metadata.values), style: ['tableDetail'] },
				]);
			} else {
				ctx.content.push([
					{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
					{ text: chkNull(ctx.data), style: ['tableDetail'] },
				]);
			}
			break;
		case "textarea":
			// console.log('*************** type: ', ctx.metadata.type);
			if (ctx.metadata.name == 'case_opening_overview') {
				let narrative = convert_html_to_pdf(pdf_version_index_to_string(ctx));
				// Loop thru and handle the ul (bullet list) & ol (ordered list) differently
				for (let i = 0; i < narrative.length; i++) {
					if (narrative[i].hasOwnProperty('ul') == true) {
						// Found a record with the ul: key
						narrative[i].ul.forEach((u) => {
							let ulRet = '' + u.text;

                            if(Array.isArray(u.text))
                            {
                                ulRet = '' + u.text.join("");
                            }
							// bullet list removed -  removed style: ['narrativeDetail'], 
							ctx.content.push([
								{ ul: [ulRet,], colSpan: '2', },
								{},
							]);
						});
					} else if (narrative[i].hasOwnProperty('ol') == true) {
						// Found a record with the ol: key
						narrative[i].ol.forEach((o) => {
							let olRet = '' + o.text;

                            if(Array.isArray(o.text))
                            {
                                ulRet = '' + o.text.join("");
                            }
							// ordered list -  removed style: ['narrativeDetail'], 
							ctx.content.push([
								{ ol: [olRet,], colSpan: '2', },
								{},
							]);
						});
					} else if (narrative[i].hasOwnProperty('table') == true) {
						// Found a table record
						let myHeaderRows = narrative[i].table.headerRows;
						let myBody = [];
						narrative[i].table.body.forEach((b) => {
							myBody.push(b);
						});
						let myWidths = narrative[i].table.widths;

						// table removed -  - removed style: ['narrativeDetail'], 
						ctx.content.push([
							{
								layout: 'lightHorizontalLines',
								table: {
									headerRows: myHeaderRows,
									widths: myWidths,
									body: myBody,
								}, colSpan: '2',
							}, {},
						]);
					} else {
						// Regular default - removed style: ['narrativeDetail'], 
						ctx.content.push([
							{ text: narrative[i], colSpan: '2' },
							{},
						]);
					}
				}
			} else {
				ctx.content.push([
					{ text: `${ctx.metadata.prompt}: `, style: ['subHeader'], colSpan: '2', },
					{},
				],
					[
						{ 
							text: `${pdf_version_index_to_string(ctx)}`, 
							style: ['tableDetail'], 
							colSpan: '2', 
						},
						{},
					]);
			}
			break;
		case "chart":
			let chartBody = [];

			let x_axis_path = ctx.metadata.x_axis.split(',');
			let y_axis_path = ctx.metadata.y_axis.split(',');
			let y_axis_field_cnt = y_axis_path.length;

			let x_axis_parts = [];
			x_axis_path.forEach((p) => {
                if
                (
                    p!=null &&
                    p!= ""
                )
                {
				    x_axis_parts.push(p.split('/'));
                }
			});

			let y_axis_parts = [];
			y_axis_path.forEach((p) => {
                if
                (
                    p!=null &&
                    p!=""
                )
                {
				    y_axis_parts.push(p.split('/'));
                }
			});

			let thereBeRecords = true;

			if (typeof ctx.multiFormIndex == 'undefined') 
            {
				if 
                (
                    ctx.p_data.hasOwnProperty([x_axis_parts[0][0]]) == false ||
					ctx.p_data[x_axis_parts[0][0]].hasOwnProperty([x_axis_parts[0][1]]) == false
				) 
                {
					chartBody.push([
						{
							text: 'No Graph Records', style: ['tableDetail'], alignment: 'center',
						},
					]);
					thereBeRecords = false;
				} 
                else if (ctx.p_data[x_axis_parts[0][0]].length == 0) 
                {
					chartBody.push([
						{
							text: 'No Graph Records', style: ['tableDetail'], alignment: 'center',
						},
					]);
					thereBeRecords = false;
				}
			}

			if (thereBeRecords) 
            {
                const x_is_valid = [];
                const y_is_valid = [];

				let xLabels = [];
				let xRec = (typeof ctx.multiFormIndex == 'undefined')
					? ctx.p_data[x_axis_parts[0][0]][x_axis_parts[0][1]]
					: ctx.p_data[x_axis_parts[0][0]][ctx.multiFormIndex][x_axis_parts[0][1]];


                xRec.forEach((x) => {
                    if
                    (
                        x[x_axis_parts[0][2]]!= null &&
                        x[x_axis_parts[0][2]] != ''
                    )
                    {
                        x_is_valid.push(true);
                    }
                    else
                    {
                        x_is_valid.push(false);
                    }
                });

    
                let y_is_valid_one = [];
				let y_is_valid_two = [];
                let yRec = (typeof ctx.multiFormIndex == 'undefined')
					? ctx.p_data[y_axis_parts[0][0]][y_axis_parts[0][1]]
					: ctx.p_data[y_axis_parts[0][0]][ctx.multiFormIndex][y_axis_parts[0][1]];

                    if (y_axis_field_cnt == 1) 
                    {
                        yRec.forEach((y) => {
                            if
                            (
                                y[y_axis_parts[0][2]] != null &&
                                y[y_axis_parts[0][2]] != '' &&
                                y[y_axis_parts[0][2]] != 'null'
                            )
                            {
                                y_is_valid.push(true);
                            }
                            else
                            {
                                y_is_valid.push(false);
                            }
                        });
                    }
                    else
                    {
        
                        yRec.forEach
                        (
                            (y) => {
        
                                    if
                                    (
                                        y[y_axis_parts[0][2]] != null &&
                                        y[y_axis_parts[0][2]] != '' &&
                                        y[y_axis_parts[0][2]] != 'null' 
                                    )
                                    {
                                        y_is_valid_one.push(true);
                                    }
                                    else
                                    {
                                        y_is_valid_one.push(false);
                                    }
          
        
        
                                    if
                                    (
                                        y[y_axis_parts[1][2]] != null &&
                                        y[y_axis_parts[1][2]] != '' &&
                                        y[y_axis_parts[1][2]] != 'null'
                                    )
                                    {
                                        y_is_valid_two.push(true);
                                    }
                                    else
                                    {
                                        y_is_valid_two.push(false);
                                    }
                            }
                        );
                    }
                



				
				let yDataOne = [];
				let yDataTwo = [];
				let colorOne = 'rgb(0, 0, 255)';
				let colorTwo = 'rgb(255, 0, 0)';
				let optData;


				if (y_axis_field_cnt == 1) 
                {


                    xRec.forEach((x, index) => {
                        if
                        (
                            x[x_axis_parts[0][2]]!= null &&
                            x[x_axis_parts[0][2]] != '' &&
                            x_is_valid[index] &&
                            y_is_valid[index]
                        )
                        {
                            xLabels.push(reformatDate(x[x_axis_parts[0][2]]));
                        }
                    });
    

					yRec.forEach((y, index) => {
                        if
                        (
                            y[y_axis_parts[0][2]] != null &&
                            y[y_axis_parts[0][2]] != '' &&
                            y_is_valid[index] &&
                            x_is_valid[index]
                        )
                        {
                            yDataOne.push(y[y_axis_parts[0][2]]);
                        }
					});

					optData = {
						labels: xLabels,
						datasets: [
							{
								label: y_axis_parts[0][2],
								fill: false,
								backgroundColor: colorOne,
								borderColor: colorOne,
								data: yDataOne,
							},
						]
					};
				} 
                else 
                {

                    xRec.forEach((x, index) => {
                        if
                        (
                            x[x_axis_parts[0][2]]!= null &&
                            x[x_axis_parts[0][2]] != '' &&
                            x_is_valid[index] &&
                            (
                                y_is_valid_one[index] || 
                                y_is_valid_two[index]
                            )
                        )
                        {
                            xLabels.push(reformatDate(x[x_axis_parts[0][2]]));
                        }
                    });

					yRec.forEach
                    (
                        (y, index) => {

                                if
                                (
                                    y[y_axis_parts[0][2]] != null &&
                                    y[y_axis_parts[0][2]] != '' &&
                                    y_is_valid_one[index] &&
                                    x_is_valid[index] &&
                                    y_is_valid_one[index]
                                )
                                {
                                    yDataOne.push(y[y_axis_parts[0][2]]);
                                    //yDataTwo.push(y[y_axis_parts[1][2]]);
                                }


                                if
                                (
                                    y_axis_parts[1][2] != null &&
                                    y_axis_parts[1][2] != '' &&
                                    y_is_valid_one[index] &&
                                    x_is_valid[index] &&
                                    y_is_valid_two[index]
                                )
                                {
                                    //yDataOne.push(y[y_axis_parts[0][2]]);
                                    yDataTwo.push(y[y_axis_parts[1][2]]);
                                }
             
					    }
                    );

					optData = {
						labels: xLabels,
						datasets: [
							{
								label: y_axis_parts[0][2],
								fill: false,
								backgroundColor: colorOne,
								borderColor: colorOne,
								data: yDataOne,
							},
							{
								label: y_axis_parts[1][2],
								fill: false,
								backgroundColor: colorTwo,
								borderColor: colorTwo,
								data: yDataTwo,
							},
						]
					};
				}

				let retImg = '';
				let imgName = (typeof ctx.multiFormIndex == 'undefined')
					? `${y_axis_parts[0][0]}_${y_axis_parts[0][1]}_${y_axis_parts[0][2]}_`
					: `${y_axis_parts[0][0]}_${y_axis_parts[0][1]}_${y_axis_parts[0][2]}_0${ctx.multiFormIndex}_`;
				retImg = doChart2(imgName, optData, ctx.metadata.prompt, ctx.p_chart_message, ctx.metadata);

				chartBody.push
                (
                    [
					    { image: retImg, width: 550, alignment: 'center', }
				    ]
                );
			}
			
			// Now push it to the context
			ctx.content.push([
				{
					layout: {
						defaultBorder: false,
						paddingLeft: function (i, node) { return 1; },
						paddingRight: function (i, node) { return 1; },
						paddingTop: function (i, node) { return 2; },
						paddingBottom: function (i, node) { return 2; },
					},
					width: 'auto',
					margin: [0, 10, 0, 0],
					colSpan: '2',
					table: {
						headerRows: 0,
						widths: ['*'],
						body: chartBody,
					},
				},
				{},
			]);
			break;
		case "button":
			break;
		case "hidden":
			// console.log('*************** type: ', ctx.metadata.type);
			// console.log('g_show_hidden: ', g_show_hidden);
			if (g_show_hidden) {
				ctx.content.push([
					{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
					{ text: chkNull(ctx.data), style: ['tableDetail', 'fgRed'], },
				]);
			}
			break;
		case "label":
			// console.log('*************** type: ', ctx.metadata.type);
			// ctx.content.push([
			// 	{ text: `${ctx.metadata.prompt}`, style: ['labelDetail'], alignment: 'center', colSpan: '2', },
			// 	{},
			// ]);
			break;
		default:
			// console.log('*************** type: ', ctx.metadata.type);
			// console.log('*** in DEFAULT', ctx.metadata.prompt);
			ctx.content.push([
				{ text: `${ctx.metadata.prompt}: `, style: ['tableLabel'], alignment: 'right', },
				{ text: chkNull(ctx.data), style: ['tableDetail'] },
			]);
			break;
	}

	return ctx.content;
}


function pdf_version_index_to_string(ctx)
{
    if(ctx == null)
    {
        return '';
    }
    else if(ctx.data == null)
    {
        return '';
    }
    else if(typeof ctx.data == 'string') 
    {
        return ctx.data;
    }
    else
    {
        
        if (typeof ctx.data.toString === "function") 
        { 
            return ctx.data.toString();
        }
        else
        {
            return '';
        }
    }
}

/*
function safe_decodeURI(value)
{
    let result = value;

    try
    {
        result = decodeURI(value);
    }
    catch(e)
    {
        result = decodeURI(encodeURI(value));
    }

    return result;
}
*/
