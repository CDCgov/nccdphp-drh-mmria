/******************************************************
 * START properties
 *****************************************************/
var activeForm;
var formElements; 
var uiSpecification;
var undoManager = new UndoManager();
var localFDRev = {};
var localFDRevCount = 0;
var localFDRevCountCurrent;
var specId = getUrlParam();

/******************************************************
 * START logic
 *****************************************************/

getSpecById(specId);

/******************************************************
 * START methods
 *****************************************************/

/**
 * Implements method to create or edit form specific elements/objects
 * @param {string} activeForm 
 * @param {Array} formElements 
 * @param {Float32Array} t 
 * @param {Float32Array} l
 * @param {Float32Array} w
 * @param {Float32Array} h
 * @param {string} promptVcontrol 
 */
function createOrUpdateFormElements(activeForm, formElement, t, l, w, h, promptVcontrol) 
{
	formElement = formElement.replace('__', '/');
	var prop = activeForm + '/' + formElement;
	if (prop in uiSpecification.form_design) 
	{
		uiSpecification.form_design[prop][promptVcontrol] = {
			'x': t,
			'y': l,
			'height': h,
			'width': w
		}
	} 
	else 
	{
		uiSpecification.form_design[prop] = new element(t, l, w, h, promptVcontrol);
	}
}

/**
 * Implements constructor method to create prompt or control objects for form specific elements
 * @param {Float32Array} t 
 * @param {Float32Array} l 
 * @param {Float32Array} h 
 * @param {Float32Array} w 
 * @param {string} e 
 */
function element(t = null, l = null, h = null, w = null, e = 'prompt') {
	this[e] = {
		'x': t,
		'y': l,
		'height': h,
		'width': w
	}
}

/**
 * Implements method to write form designer specs to screen for debugging
 */
function writeFormSpecs(logAction = false) {
	if(logAction) {
		createLocalRevision();
	}

	var html = JSON.stringify(uiSpecification, undefined, 4)
	$(".formDesignSpecsPre").html(html);

}


/**
 * Implements global method to get URL parameters variables
 */
function getUrlVars() {
	var vars = {};
	var parts = window.location.href.replace(/[?&]+([^=&]+)=([^&]*)/gi, function (m, key, value) {
		vars[key] = value;
	});
	return vars;
}


/**
 * Implements method to grab parameter (UI specification) value from URL
 * @param {String} parameter 
 */
function getUrlParam(parameter = "_id") {
	if (window.location.href.indexOf(parameter) > -1) {
		urlparameter = getUrlVars()[parameter];
	} else {
		urlparameter = 'default_ui_specification';
	}
	return urlparameter;
}

/**
 * Implements method to get UI Spec by id and set main var uiSpecification. Also write form design object.
 * @param {String} id 
 */
function getSpecById(id) {
	var url = location.protocol + "//" + location.host + "/api/ui_specification/" + id;
	$.get(url, function(data, status) {
		uiSpecification = data;
		writeFormSpecs();
	})
}

/**
 * Implements method to save current specification to database
 */
function saveSpec() {
	// createOrUpdateFormElements(
	// 	activeForm,
	// 	fid,
	// 	$("#" + event.relatedTarget.id).position().top,
	// 	$("#" + event.relatedTarget.id).position().left,
	// 	event.relatedTarget.getAttribute('data-w'),
	// 	event.relatedTarget.getAttribute('data-h'),
	// 	pc
	// );

	// writeFormSpecs();
	$.ajax({
		url: location.protocol + "//" + location.host + "/api/ui_specification/" + specId,
		contentType: 'application/json; charset=utf-8',
		dataType: 'json',
		data: JSON.stringify(uiSpecification),
		type: "POST"
	}).done(function (response) {
		var response_obj = eval(response);
		if (response_obj.ok) {
			getSpecById(specId);
		}
	});
}

/**
 * Implements method to add a local specification revision to memory (Future proof for redo functionality)
 */
function addLocalRevision() {
	localFDRevCountCurrent = localFDRevCount;
	localFDRev[localFDRevCountCurrent] = JSON.parse(JSON.stringify(uiSpecification));
	localFDRevCount++;
	writeLocalRevision();
}

/**
 * Implements method to undo last action in form designer, falling back to last local revision
 */
function deleteLocalRevision() {
	if(localFDRevCountCurrent <= 0) {
		localFDRevCount = 1;
		localFDRevCountCurrent = 0;
		alert('You cannot undo any further, this is the last saved revision.');
	} else {
		delete localFDRev[localFDRevCountCurrent];
		localFDRevCountCurrent--;
		localFDRevCount--;
	}
	uiSpecification = JSON.parse(JSON.stringify(localFDRev[localFDRevCountCurrent]));
	$.get(urlMetaData, function (data, status) {
		var metaDataForms = buildFormList(data);

		var caseForm = metaDataForms.find(x => x.name === activeForm);

		// Set what type of fields you would like
		formElements = groupFormElementsByType(caseForm);

		buildFormElementPromptControl(formElements);

		// Rebuild on caseForm click
		$(".clickTrigger").click(function () {
			console.log(activeForm);
			activeForm = this.id;
			var caseForm = metaDataForms.find(x => x.name === this.id);

			// Set what type of fields you would like
			formElements = groupFormElementsByType(caseForm);

			buildFormElementPromptControl(formElements);
		});
	});
	writeLocalRevision();
}

/**
 * Implements method to create local revision object into memory and Initialize undo functionality
 */
function createLocalRevision() {
	addLocalRevision();

	undoManager.add({
		undo: function () {
			deleteLocalRevision();
		},
		redo: function () {
			addLocalRevision();
		}
	});
}

/**
 * Implements method to write local revision to html view
 */
function writeLocalRevision() {
	var html = localFDRevCountCurrent;
  	$("#local-rev").html(html);
}