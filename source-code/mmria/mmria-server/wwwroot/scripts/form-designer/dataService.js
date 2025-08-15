/******************************************************
 * START properties
 *****************************************************/
var urlTestBase = 'http://test.mmria.org/api/';
var urlProdBase = location.protocol + '//' + location.host + '/api/';
var urlMetaData = urlProdBase + "metadata";
var activeSpec;
var initFields = false;
var initFieldSets = false;
var fdDMS;

/******************************************************
 * START logic
 *****************************************************/
$.get(urlMetaData, function(data, status) {
    var metaDataForms = buildFormList(data);

    populateFormDesignerCanvas(metaDataForms);

});


/******************************************************
 * START methods
 *****************************************************/

/**
 * Implements method to build clickable form list based on meta data and returns elements specific to case form
 * @param {Array} data 
 */
function buildFormList(data) {
    var metaDataForms = $.grep(data.children, function (e) { return e.type === 'form'; });

    var inHTML = '';

    $.each(metaDataForms, function (index, value) {
        var newItem = "<li class=\"clickTrigger\" id=\"trigger-" + value.name + "\">" + value.prompt + "</li>";
        inHTML += newItem;
    });

    $("#repeatNavContainer").html(inHTML);

    return metaDataForms;
}

/**
 * Implements method to get form elements and group by field/control type
 * @param {Array} caseForm 
 */
function groupFormElementsByType(caseForm) {

    var elements = {
        'labels': $.grep(caseForm.children, function (e) { return e.type == 'label'; }),
        'strings': $.grep(caseForm.children, function (e) { return e.type == 'string'; }),
        'groups': $.grep(caseForm.children, function (e) { return e.type == 'group'; }),
        'grids': $.grep(caseForm.children, function (e) { return e.type == 'grid'; }),
        'date': $.grep(caseForm.children, function (e) { return e.type == 'date'; }),
        'number': $.grep(caseForm.children, function (e) { return e.type == 'number'; }),
        'textareas': $.grep(caseForm.children, function (e) { return e.type == 'textarea'; }),
        'html_areas': $.grep(caseForm.children, function (e) { return e.type == 'html_area'; }),
    }
    return elements;
}

/**
 * Implements method to get form elements of a group / grid / collection of fields and group by field/control type
 * @param {Object} group 
 */
function groupFormGroupElements(group) {
    var elements = {
        'labels': $.grep(group.children, function (e) { return e.type == 'label'; }),
        'strings': $.grep(group.children, function (e) { return e.type == 'string'; }),
        'date': $.grep(group.children, function (e) { return e.type == 'date'; }),
        'number': $.grep(group.children, function (e) { return e.type == 'number'; }),
        'textareas': $.grep(group.children, function (e) { return e.type == 'textarea'; }),
        'html_areas': $.grep(group.children, function (e) { return e.type == 'html_area'; }),
    }

    return elements;
}

/**
 * Implements method to populate form designer canvas with form elements of chosen form
 * allowing user to set form elements prompt and control metadata via drag and drop
 * @param {Array} metaDataForms 
 */
function populateFormDesignerCanvas(metaDataForms) {
    $(".clickTrigger").click(function () {
        activeForm = this.id.replace('trigger-','');
        var caseForm = metaDataForms.find(x => x.name === activeForm);

        // Set what type of fields you would like
        formElements = groupFormElementsByType(caseForm);
        fd_markup_build_fieldtypes(caseForm);

        buildFormElementPromptControl(formElements);

        $('.resize-drag').on('mouseenter', function () {
            writeActionSpecs("in", activeForm);
        });
        $(".resize-drag").on("mouseleave", function() {
            writeActionSpecs('out', activeForm);
        });

        // Instantiate Form Designer Multi-select
        fdDMS = new DragSelect({
            selectables: document.querySelectorAll(".resize-drag"),
            area: document.getElementById("fd-gs"),
        });

        fdCanvasSnapShot();

        if(initFieldSets == false) {
            bootstrapUISpec("fieldSets");
        }
        if (initFields == false) {
            bootstrapUISpec("fields");
        }
        bootstrapUISpec("fieldSets");
        bootstrapUISpec("fields");

        
    });
}

function fd_fire_multiselect() {
    new Selectables({
        elements: ".resize-drag",
        zone: "div.form-designer-canvas",
        selectedClass: "active",
        moreUsing: 'ctrlKey',
        onSelect: function (element) {
            fd_multiselect_prompt(element);
        },
        onDeselect: function (el) {
            fd_multiselect_clear();
        },
    });
}

function fd_multiselect_prompt(element) {
    console.log(element.id);
    let inHTML = `<div><h3>What would you like to do with the selected items?</h3></div>`;
    $(".wysiwyg-container").html(inHTML);
}

function fd_multiselect_clear() {
  let inHTML = ``;
  $(".wysiwyg-container").html(inHTML);
}

/**
 * Implements method to compile form element markup and inject into form designer canvas
 * @param {Array} fe 
 */
function buildFormElementPromptControl(fe) {
    var inHTML = '';

    // String fields
    $.each(fe.strings, function(index, value) {
        var newItem = `<div id="` + value.name + `" class="resize-drag drag-drop yes-drop">` + value.prompt + `</div>`;
        newItem += `<div id="` + value.name + `-control" class="resize-drag drag-drop yes-drop"> <input type="text" rows="1" placeholder="` + value.prompt + `"></input></div>`;
        inHTML += newItem;
    });

    // Label fields
    $.each(fe.labels, function (index, value) {
        var newItem = `<div id="` + value.name + `" class="resize-drag drag-drop yes-drop">` + value.prompt + `</div>`;
        inHTML += newItem;
    });

    // Field groups
    $.each(fe.groups, function(index, value) {
        var newItem = `<fieldset id="` + value.name + `" class="resize-drag drag-drop yes-drop fd-fieldset">`
        newItem += '<legend>' + value.prompt + '</legend>';
        var groupName = value.name;
        var stringSet = $.grep(value.children, function(e) {
          return e.type == "string";
        });

        if (stringSet.length > 0) {
            $.each(stringSet, function(index, value) {
                var eid = groupName + '__' + value.name;
                newItem += `<div id="` + eid + `" class="resize-drag drag-drop yes-drop">` + value.prompt + `</div>`;
                newItem += `<div id="` + eid + `-control" class="resize-drag drag-drop yes-drop"> <input type="text" rows="1" placeholder="` + value.prompt + `"></textarea></div>`;
            })
        }
        newItem += `</fieldset>`;
        inHTML += newItem;
    });

    // Grids
    // $.each(fe.grids, function (index, value) {
    //     var groupName = value.name;
    //     var stringSet = $.grep(value.children, function (e) {
    //         return e.type == "string";
    //     });

    //     if (stringSet.length > 0) {
    //         $.each(stringSet, function (index, value) {
    //             var eid = groupName + '__' + value.name;
    //             var newItem = `<div id="` + eid + `" class="resize-drag drag-drop yes-drop">` + value.prompt + `</div>`;
    //             newItem += `<div id="` + eid + `-control" class="resize-drag drag-drop yes-drop"> <input type="text" rows="1" placeholder="` + value.prompt + `"></input></div>`;
    //             inHTML += newItem;
    //         })
    //     }
    // });

    // Write elements to canvas
    //$(".form-designer-canvas").html(inHTML);

    // Add style to written elements
    styleElementsPerDefinition(fe.strings, fe.groups);
}

/**
 * Implements method to apply styles to form elements per saved form definitions
 * @param {Array} fe 
 */
function styleElementsPerDefinition(fe, group = null) {
    $.each(fe, function (index, value) {
        var tid = activeForm + '/' + value.name;
        if (tid in uiSpecification.form_design) {
            var el = uiSpecification.form_design[tid];
            if ('prompt' in uiSpecification.form_design[tid]) {
                if(el.prompt) {
                    // set style for element prompt
                    // $('#' + value.name).css({ "position": 'absolute', "top": el.prompt.x, "left": el.prompt.y, "width": el.prompt.width, "height": el.prompt.height });
                    // $('#' + value.name).attr({ "data-t": el.prompt.x, "data-l": el.prompt.y, "data-w": el.prompt.width, "data-h": el.prompt.height });
                }
            }
            if ('control' in uiSpecification.form_design[tid]) {
                if (el.control) {
                    // set style for element control
                    // $('#' + value.name + '-control').css({ "position": 'absolute', "top": el.control.x, "left": el.control.y, "width": el.control.width, "height": el.control.height });
                    // $('#' + value.name + '-control').attr({ "data-t": el.control.x, "data-l": el.control.y, "data-w": el.control.width, "data-h": el.control.height });
                }
            }
        }
    });

    if (group) {
        $.each(group, function (index, value) {
            var tid = activeForm + '/' + value.name;
            var gn = value.name;
            if (tid in uiSpecification.form_design) {
                var el = uiSpecification.form_design[tid];
                if ('prompt' in uiSpecification.form_design[tid]) {
                    if (el.prompt) {
                        if (el.prompt.width == null) { el.prompt.width = 1037.75 };
                        // set style for element prompt
                        // $('#' + value.name).css({ "position": 'absolute', "top": el.prompt.x, "left": el.prompt.y, "width": el.prompt.width, "height": el.prompt.height });
                        // $('#' + value.name).attr({ "data-t": el.prompt.x, "data-l": el.prompt.y, "data-w": el.prompt.width, "data-h": el.prompt.height });
                    }
                }
                if ('control' in uiSpecification.form_design[tid]) {
                    if (el.control) {
                        if (el.control.width == null) { el.control.width = 1037.75 };
                        // set style for element control
                        // $('#' + value.name + '-control').css({ "position": 'absolute', "top": el.control.x, "left": el.control.y, "width": el.control.width, "height": el.control.height });
                        // $('#' + value.name + '-control').attr({ "data-t": el.control.x, "data-l": el.control.y, "data-w": el.control.width, "data-h": el.control.height });
                    }
                }
            }

            var stringSet = $.grep(value.children, function (e) {
                return e.type == "string";
            });

            if (stringSet.length > 0) {
                $.each(stringSet, function (index, value) {
                    var eid = tid + '/' + value.name;
                    var jeid = gn + '__' + value.name;
                    if (eid in uiSpecification.form_design) {
                        var el = uiSpecification.form_design[eid];
                        if ('prompt' in uiSpecification.form_design[eid]) {
                            if (el.prompt) {
                                // set style for element prompt
                                // $('#' + jeid).css({ "position": 'absolute', "top": el.prompt.x, "left": el.prompt.y, "width": el.prompt.width, "height": el.prompt.height });
                                // $('#' + jeid).attr({ "data-t": el.prompt.x, "data-l": el.prompt.y, "data-w": el.prompt.width, "data-h": el.prompt.height });
                            }
                        }
                        if ('control' in uiSpecification.form_design[eid]) {
                            if (el.control) {
                                // set style for element control
                                // $('#' + jeid + '-control').css({ "position": 'absolute', "top": el.control.x, "left": el.control.y, "width": el.control.width, "height": el.control.height });
                                // $('#' + jeid + '-control').attr({ "data-t": el.control.x, "data-l": el.control.y, "data-w": el.control.width, "data-h": el.control.height });
                            }
                        }
                    }
                })
            }
        });
    }

}
function writeActionSpecs(val, obj) {
    let prop;
    obj = obj.replace('__', '/');
    if(obj.includes('control')) {
        obj = obj.replace('-control', '');
        prop = activeForm + "/" + obj;
        activeSpec = uiSpecification.form_design[prop];
    } else {
        prop = activeForm + "/" + obj;
        activeSpec = uiSpecification.form_design[prop];
    }
    if(val === 'in') {
        if (activeSpec === undefined) {
            var html = "Default specifications";
        } else {
            var html = JSON.stringify(activeSpec, undefined, 4);
        }
        $(".liveSpecHeading").html(prop);
        $(".liveSpec").html(html);
        $('.liveSpec').show('slow');
    }
}

function bootstrapUISpec(itemType) {
    if(itemType === 'fields') {
        var fields = document.getElementsByClassName("resize-drag");
        initFields = true;
        for (index = 0; index < fields.length; ++index) {
            let itemID1 = fields[index].id;
            let itemID = fields[index].id;
            let pc;
            if ($('#' + itemID1).is('fieldset')) { continue; }
            if (itemID.includes('-control')) {
                pc = 'control';
                itemID = itemID1.split('-')[0];
            } else {
                pc = 'prompt';
            }
            let top = $('#' + itemID1).position().top;
            let left = $("#" + itemID1).position().left;

            createOrUpdateFormElements(activeForm, itemID, top, left, null, null, pc);

            writeFormSpecs();
            //console.log(item.getBoundingClientRect());
        }
    } else {
        var fieldSets = document.getElementsByClassName("fd-fieldset");
        initFieldSets = true;
        for (index = 0; index < fieldSets.length; ++index) {
            let itemID1 = fieldSets[index].id;
            let itemID = fieldSets[index].id;
            let pc;
            if (itemID.includes('-control')) {
                pc = 'control';
                itemID = itemID1.split('-')[0];
            } else {
                pc = 'prompt';
            }
            let top = $("#" + itemID1).position().top;
            let left = $("#" + itemID1).position().left;
            let height = $("#" + itemID1).height();

            //console.log(itemID1);


            createOrUpdateFormElements(activeForm, itemID, top, left, 1037.75, height, 'prompt');
            writeFormSpecs();
        }
    }
}

function fdControlStack() {
   let elems = fdDMS.getSelection();
   let promptId;
   let promptHeight;
   let promptPos;
   let controlId;
   let controlPos;
   let controlHeight;
   let controlTopPos;
    $.each(elems, function (index, value) {
        // Check to see if we are dealing with control or prompt
        if(value.id.includes('-control')) {
            // This is a control put the prompt above
            promptId = value.id.replace('-control', '');
            controlId = value.id;
            promptPos = $('#'+promptId).position();
            promptHeight = $('#'+promptId).outerHeight();
            controlTopPos = promptPos.top + promptHeight;
            $('#'+value.id).css({ position: 'absolute', top: controlTopPos, left: promptPos.left });

        } else {
            controlId = value.id + '-control';
            promptId = value.id;
            promptPos = $('#' + promptId).position();
            promptHeight = $('#' + promptId).outerHeight();
            controlTopPos = promptPos.top + promptHeight;
            $('#' + controlId).css({ position: 'absolute', top: controlTopPos, left: promptPos.left });
        }
        // let fontSize = parseInt($("label[for="+value.id+"]").css("font-size"));
        // fontSize = fontSize + 1 + 'px';
        // $("label[for=" + value.id + "]").css("font-size",fontSize);
    })
}

function fdChangeFontSize(o) {
    let elems = fdDMS.getSelection();

    $.each(elems, function(index, value) {
        if(o == 'larger') {
            let fontSize = parseInt($("label[for=" + value.id + "]").css("font-size"));
            fontSize = fontSize + 1 + 'px';
            $("label[for=" + value.id + "]").css("font-size", fontSize);
        } else {
            let fontSize = parseInt($("label[for=" + value.id + "]").css("font-size"));
            fontSize = fontSize - 1 + 'px';
            $("label[for=" + value.id + "]").css("font-size", fontSize);
        }
    })
}

function fdCanvasSnapShot() {
    let elems = document.getElementById('fd-gs').getElementsByClassName('resize-drag');
    let newElems = [];

    $.each(elems, function(index, value) {
        console.log(value);
        let elemPos = $("#" + value.id).position();
        let elemHeight = $("#" + value.id).outerHeight();
        let elemWidth = $("#" + value.id).outerWidth();
        let cssSytle = {
            position: 'absolute',
            top: elemPos.top,
            left: elemPos.left,
            height: elemHeight,
            width: elemWidth
        };
        newElems.push({id: value.id, style: cssSytle})
        // console.log({id: value.id, top: elemPos.top, left: elemPos.left, width: elemWidth, height: elemHeight});
    })

    console.log(newElems);
    $.each(newElems, function(index, value) {
        $('#'+value.id).css(value.style);
    })
}