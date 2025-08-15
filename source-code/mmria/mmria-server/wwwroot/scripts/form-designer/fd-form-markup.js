/********* Get Global MetaData */
var urlProdBase = location.protocol + "//" + location.host + "/api/";
var urlMetaData = urlProdBase + "metadata";
var globalMetaData;
$.get(urlMetaData, function (data, status) {
    globalMetaData = data;
});

/**
 * Implements method to build a fieldset
 * @param {object} fieldsetInfo 
 */
function fd_markup_build_fieldset(fieldsetInfo) {
    if (fieldsetInfo == undefined || fieldsetInfo.length == 0) { 
        return; 
    } else {
        let fieldsetItems = '';
        $.each(fieldsetInfo.children, function (index, value) {
            let response = fd_markup_build_fieldtypes(value, true);
            if(typeof response !== 'undefined') {
                fieldsetItems += fd_markup_build_fieldtypes(value, true);
            }
        })
        let fd_fieldset = `
        <fieldset id='${fieldsetInfo.name}' class="resize-drag drag-drop yes-drop fd-fieldset">
            <legend>${fieldsetInfo.prompt}</legend>
            ${fieldsetItems}
        </fieldset>`;
        return fd_fieldset;
    }
}

/**
 * Implements method to build a textfield
 * @param {object} data 
 */
function fd_markup_build_string(data) {
    let stringfield = `
    <div class="resize-drag drag-drop yes-drop" id="${data.name}"><label for="${data.name}">${data.prompt}</label></div>
    <div class="resize-drag drag-drop yes-drop" id="${data.name}-control"><input type="text" ></input></div>`;
    return stringfield;
}

/**
 * Implements method to build a textarea
 * @param {object} data
 */
function fd_markup_build_textarea(data) {
    let stringfield = `
    <div class="resize-drag drag-drop yes-drop" id="${data.name}"><label for="${data.name}">${data.prompt}</label></div>
    <div class="resize-drag drag-drop yes-drop" id="${data.name}-control"><textarea rows="7" cols="80"></textarea></div>`;
    return stringfield;
}

/**
 * Implements method to build a html_area
 * @param {object} data
 */
 function fd_markup_build_html_area(data) {
    let stringfield = `
    <div class="resize-drag drag-drop yes-drop" id="${data.name}"><label for="${data.name}">${data.prompt}</label></div>
    <div class="resize-drag drag-drop yes-drop" id="${data.name}-control"><textarea rows="7" cols="80"></textarea></div>`;
    return stringfield;
}


/**
 * Implements method to build a date field
 * @param {object} data
 */
function fd_markup_build_date(data) {
    let stringfield = `
    <div class="resize-drag drag-drop yes-drop" id="${data.name}"><label for="${data.name}">${data.prompt}</label></div>
    <div class="resize-drag drag-drop yes-drop" id="${data.name}-control"><input type="text" ></input></div>`;
    return stringfield;
}

/**
 * Implements method to build a number field
 * @param {object} data
 */
function fd_markup_build_number(data) {
    let stringfield = `
    <div class="resize-drag drag-drop yes-drop" id="${data.name}"><label for="${data.name}">${data.prompt}</label></div>
    <div class="resize-drag drag-drop yes-drop" id="${data.name}-control"><input type="text" ></input></div>`;
    return stringfield;
}

/**
 * Implements method to build a list field
 * @param {object} data
 */
function fd_markup_build_list(data) {
    //console.log('list field', data);
    let listOptions = fd_markup_build_list_options(data);
    let stringfield = `
    <div class="resize-drag drag-drop yes-drop" id="${data.name}"><label for="${data.name}">${data.prompt}</label></div>
    <div class="resize-drag drag-drop yes-drop" id="${data.name}-control">
    <select class="form-control">
        ${listOptions}
    </select>
    </div>`;
    return stringfield;
}

/**
 * Implements method to build list field options
 * @param {object} data 
 */
function fd_markup_build_list_options(data) {    
    let values;
    if (data.path_reference !== undefined) {

        let p = data.path_reference.split("/")[1];
        let apiOptionValues = $.grep(globalMetaData.lookup, function (e) {
            return e.name == p;
        });
        values = apiOptionValues[0].values;
    } else {
        values = data.values;
    }

    let markup = '';
    $.each(values, function (index, value) {
        markup += `<option value="${value.value}">${value.value}</option>`;
    });

    return markup;
}


/**
 * Implements method to build a time field
 * @param {object} data
 */
function fd_markup_build_time(data) {
    let stringfield = `
    <div class="resize-drag drag-drop yes-drop" id="${data.name}"><label for="${data.name}">${data.prompt}</label></div>
    <div class="resize-drag drag-drop yes-drop" id="${data.name}-control"><input type="text" ></input></div>`;
    return stringfield;
}

/**
 * 
 * @param {object} caseForm 
 * @param {boolean} group 
 */
function fd_markup_build_fieldtypes(caseForm, group=false) {

    let inHTML = '';

    // Build field types when coming from a group or collection
    if(group) {
        let value = caseForm;
        let markup;
        switch (value.type.toLowerCase()) {
            case 'string':
                markup = fd_markup_build_string(value)
                break;
            case 'textarea':
                markup = fd_markup_build_textarea(value)
                break;
            case 'html_area':
                markup = fd_markup_build_html_area(value)
                break;
            case 'date':
                markup = fd_markup_build_date(value)
                break;
            case 'number':
                markup = fd_markup_build_number(value);
                break;
            case 'list':
                markup = fd_markup_build_list(value);
                break;
            case 'time':
                markup = fd_markup_build_time(value);
                break;
            case 'group':
                markup = fd_markup_build_fieldset(value);
                break;
            case 'grid':
                markup = fd_markup_build_fieldset(value);
                break;
        }
        return markup;
    }

    // Build field types in general or on init
    let fieldInfo = caseForm.children;

    if (fieldInfo == undefined || fieldInfo.length == 0) {
        console.log('nothing here');
    } else {
        $.each(fieldInfo, function (index, value) {
            switch (value.type.toLowerCase()) {
                case 'string':
                    inHTML += fd_markup_build_string(value)
                    break;
                case 'textarea':
                    inHTML += fd_markup_build_textarea(value);
                    break;
                case 'html_area':
                    inHTML += fd_markup_build_html_area(value);
                    break;
                case 'date':
                    inHTML += fd_markup_build_date(value);
                    break;
                case 'number':
                    inHTML += fd_markup_build_number(value);
                    break;
                case 'group':
                    inHTML += fd_markup_build_fieldset(value);
                    break;
                case 'grid':
                    inHTML += fd_markup_build_fieldset(value);
                    break;
                case 'list':
                    inHTML += fd_markup_build_list(value);
                    break;
                case 'time':
                    inHTML += fd_markup_build_time(value);
                    break;
            }
        });

        $(".form-designer-canvas").html(inHTML);
    }
}