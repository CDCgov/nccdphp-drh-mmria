// offsetRelative (or, if you prefer, positionRelative)
(function ($) {  
  $.fn.offsetRelative = function (top) {
    var $this = $(this);
    var $parent = $this.offsetParent();
    var offset = $this.position();
    if (!top) return offset; // Didn't pass a 'top' element 
    else if ($parent.get(0).tagName == "BODY") return offset; // Reached top of document
    else if ($(top, $parent).length) return offset; // Parent element contains the 'top' element we want the offset to be relative to 
    else if ($parent[0] == $(top)[0]) return offset; // Reached the 'top' element we want the offset to be relative to 
    else { // Get parent's relative offset
      var parent_offset = $parent.offsetRelative(top);
      offset.top += parent_offset.top;
      offset.left += parent_offset.left;
      return offset;
    }
  };
  $.fn.positionRelative = function (top) {
    return $(this).offsetRelative(top);
  };
}(jQuery));

let apiURL = location.protocol + "//" + location.host + "/api/";
let endpointMetaData = 'metadata/';
let endpointUISpecification = 'ui_specification/';

// Declare Http request objects (for global scope)
let jsonMetaData;
let jsonUISpecification;

// Declare Data objects (use when done with requests)
let metaData = {
  fullObject: {},
  forms: {},
  activeForm: '',
};
let uiSpecification = {
  list: '',
  currentID: '',
  currentObject: {},
  localRevision: {},
  localCount: 0,
};
let fdObject = {};

// Declare Drag Select object (assign after elements are in DOM)
let fdDragSelect;

// Global scope for items drag selected
let globalDSelected = [];

// Form Designer Object Handler
formDesigner = {
  dataHandler: {
    newBuild: function() {
      jsonMetaData = $.get(apiURL + endpointMetaData);
      jsonMetaData
        .done(function (data, status) {
          metaData.fullObject = data;
          metaData.forms = formDesigner.dataHandler.getForms(data);
          formDesigner.canvasHandler.displayFormList(metaData.forms);
        })
        .fail(function (xhr, status, error) {
          console.log(xhr);
        });
    },
    getForms: function (metaData) {
      return $.grep(metaData.children, function (n) {
        return n.type === 'form';
      });
    },
    getFormFields: function (formName) {
      let currentForm;
      $.grep(metaData.forms, function (n) {
        if (n.name === formName) { currentForm = n };
      });
      return currentForm.children;
    }
  },
  fdObjectHandler: {
    snapShot: function(undo = false, init = false, unwrap = false) {
      if(undo) { $('.fd-path-object').removeAttr('style'); return true }
      let elems = document.getElementsByClassName("fd-path-object");
      let newElems = [];
      let target;
      let parent;
      let promptVcontrol;
      let path;

      // Loop through all elements of current form and prep
      $.each
      (
        elems,
        function(index, value) 
        {
          parent = $(value).parent();
          parentID = parent[0].id;
          if (value.tagName === 'LABEL') 
          {
            targetID = $(value)[0].htmlFor;
            target = `label[for='${targetID}']`;
            promptVcontrol = 'prompt';
            path = targetID.replace(/--/g, '/');
          } 
          else 
          {
            target = `#${value.id}`;
            promptVcontrol = 'control';
            path = value.id.replace(/--/g, '/');
          }

          let elemPos = $(target).position();;

          if(parentID === 'temp-wrap' && unwrap) 
          {
            elemPos = $(target).offsetRelative('.form-designer-canvas');
          }

          let elemHeight = $(target).outerHeight();
          let elemWidth = $(target).outerWidth();
          let fontWeight = $(target).css('font-weight');
          let fontSize = $(target).css('font-size');
          let fontStyle = $(target).css('font-style');
          let fontColor = $(target).css('color');
          let cssSytle = {
              position: 'absolute',
              top: elemPos.top,
              left: elemPos.left,
              height: elemHeight,
              width: elemWidth,
              'font-weight': fontWeight,
              'font-size': fontSize,
              'font-style': fontStyle,
              color: fontColor
          };
          
          // Add current element/path to fdObject
          if(init === false) 
          {
            formDesigner.fdObjectHandler.addPath(path, cssSytle, promptVcontrol);
          }

          // Add current element/path to array for later inline style setting
          newElems.push({target: target, style: cssSytle, promptVcontrol: promptVcontrol, path: path});
        }
      );

      // Update specification info (includes writing to modal).
      formDesigner.fdObjectHandler.mapToSpec();

      // Take quicksnap for local revision

      // Set element positions via inline style
      $.each
      (
        newElems,
        function (index, value) 
        {
          if(uiSpecification.currentObject.form_design[value.path] !== undefined) 
          {
            $(value.target).css(JSON.parse(uiSpecification.currentObject.form_design[value.path][value.promptVcontrol].style));
          }

          $(value.target).css('transform','');
          $(value.target).removeAttr('data-x data-y');
        }
      );     
    },
    quickSnap: function() {
      uiSpecification.localCount++;
      uiSpecification.localRevision[uiSpecification.localCount] = JSON.parse(JSON.stringify(uiSpecification.currentObject));
      $('#local-rev-container').html(fdTemplates.uiSpecification.dashboard.localRev());
    },
    rollBackRevision: function() {
      console.log('first local', uiSpecification.localCount);
      let index = uiSpecification.localCount - 1;
      uiSpecification.localCount = index;
      $('#local-rev-container').html(fdTemplates.uiSpecification.dashboard.localRev());
      console.log('local count', uiSpecification.localCount);
      uiSpecification.currentObject = JSON.parse(JSON.stringify(uiSpecification.localRevision[uiSpecification.localCount]));
      formDesigner.canvasHandler.formFields.display(metaData.activeForm);
      
    },
    addPath: function(path, style, promptVcontrol) {
      if(path in fdObject) {
        fdObject[path][promptVcontrol] = {
          style: JSON.stringify(style),
          x: null,
          y: null,
          height: null,
          width: null,
        };
      } else {
        fdObject[path] = {};
        fdObject[path][promptVcontrol] = { 
          style: JSON.stringify(style),
          x: null,
          y: null,
          height: null,
          width: null,
        };
      };
      
    },
    mapToSpec: function(data = null) {
      formDesigner.uiSpecHandler.writeToModal();
    }
  },
  uiSpecHandler: {
    newBuild: function() {
      jsonUISpecification = $.get(apiURL + endpointUISpecification + "list");
      jsonUISpecification
        .done(function(data, status) {
          uiSpecification.list = data;
          formDesigner.uiSpecHandler.displayOptions(uiSpecification);
        })
        .fail(function(xhr, status, error) {
          console.log(xhr);
        });
    },
    newSpecObject: function(name) {
      return {
        "name": name,
        "_id": $mmria.get_new_guid(),
        "data_type": "ui-specification",
        "date_created": new Date().toISOString(),
        "created_by": g_uid,
        "date_last_updated": new Date().toISOString(),
        "last_updated_by": g_uid,
        "dimension": {
          "width": 8.5,
          "height": null
        },
        "form_design": {}
      };
    },
    writeToModal: function() {
      let modalSpecObject = JSON.stringify(uiSpecification.currentObject, undefined, 4);
      $("#fd-modal-specs-printed").html(modalSpecObject);
    },
    addSpecToDB: function(name) {
      let newSpec = formDesigner.uiSpecHandler.newSpecObject(name);

      // Clear the spec name field
      $("#ui-spec-name-field").val('');
      $.ajax
      (
        {
        url: apiURL + endpointUISpecification,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(newSpec),
        type: "POST"
        }
      ).done
      (
        function(response) 
        {
          let response_obj = eval(response);
          if (response_obj.ok) 
          {
            formDesigner.uiSpecHandler.newBuild();
          }
        }
      );
    },
    deleteSpec: function() {
      $.ajax({
        url: apiURL + endpointUISpecification + uiSpecification.currentID + '?rev=' + uiSpecification.currentObject._rev,
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: '',
        type: "DELETE"
      }).done(function (response) {
        let response_obj = eval(response);
        if (response_obj.ok) {
          formDesigner.uiSpecHandler.newBuild();
          let selector = { value: 'default_ui_specification' };
          formDesigner.uiSpecHandler.setCurrent(selector); // Reset current spec in case user deleted current (which is always)
        }
      });
    },
    modifySpec: function(name = null, height = null, width = null) 
    {
      var ma = document.getElementById("fd-messages");
      ma.innerHTML = "";

      if (name) 
      { 
        uiSpecification.currentObject.name = name; 
      }

      if (height) 
      {
        uiSpecification.currentObject.dimension.height = height;
      }

      if (width) 
      { 
        uiSpecification.currentObject.dimension.width = width;
      }

      $.ajax
      (
        {
          url: apiURL + endpointUISpecification + uiSpecification.currentID,
          contentType: "application/json; charset=utf-8",
          dataType: "json",
          data: JSON.stringify(uiSpecification.currentObject),
          type: "POST"
        }
      ).done
      (
        function(response) 
        {
          let response_obj = eval(response);
          if (response_obj.ok) 
          {
            formDesigner.uiSpecHandler.newBuild();
            let selector = { value: uiSpecification.currentID };
            formDesigner.uiSpecHandler.setCurrent(selector); // Reset current spec in case user deleted current
            formDesigner.uiSpecHandler.dashboard.infoDisplay(); // Rebuild infoDisplay to reflect changes
            formDesigner.canvasHandler.setDimensions();

            
            ma.innerHTML = "save completed SUCCESSFULLY @ " + new Date().toISOString();

          }
        }
      )
      .fail
      (
        function (xhr, status, error) 
        {
          
          var ma = document.getElementById("fd-messages");
          ma.innerHTML = "save ERROR " + status + " @ " + new Date().toISOString() + "\n" + error;
        }
      );
    },
    getSpec: function(currentID) {
      $.get(apiURL + endpointUISpecification + currentID)
        .done(function(data, status) {
          uiSpecification.currentObject = data;
          uiSpecification.localRevision[0] = JSON.parse(JSON.stringify(uiSpecification.currentObject));;
          fdObject = uiSpecification.currentObject.form_design;

          // Map fdObject to Spec
          formDesigner.fdObjectHandler.mapToSpec();

          // Hide delete / modify controls for null or default UI
          if(uiSpecification.currentObject._id === null || uiSpecification.currentObject._id === 'default_ui_specification') {
            $("#ui-spec-delete-modify-container").hide();
          } else {
            $("#ui-spec-delete-modify-container").show();
          }

          // Show form list and canvas heads up display
          $("#fd-canvas-heads-up-display-wrapper, #form-list-container").show();

          // Hide select spec alert and show select formtype alert
          $("#alert-select-spec").hide();
          $("#alert-select-formtype").show();

          // Update modal for printing specs
          let modalSpecObject = JSON.stringify(uiSpecification.currentObject, undefined, 4);
          $("#fd-modal-specs-printed").html(modalSpecObject);

          // Update uiSpecDashboard
          formDesigner.uiSpecHandler.dashboard.infoDisplay();

          // Update canvas wrapper dimensions
          formDesigner.canvasHandler.setDimensions();

          // Rebuild canvas from active form if applicable
          if(metaData.activeForm !== '') {
            formDesigner.canvasHandler.formFields.display(metaData.activeForm);
          }
          
        });
    },
    displayOptions: function (uiSpecification) {
      let tpl = `<option value="">Select a specification</option>`;
      $.each(uiSpecification.list, function(index, value) {
        tpl += fdTemplates.uiSpecification.options(value);
      });
      $('#ui-spec-selector').html(tpl);
    },
    dashboard: {
      infoDisplay: function() {
        let tpl = fdTemplates.uiSpecification.dashboard.info();
        $("#ui-spec-display-container").html(tpl);
      },
    },
    setCurrent: function(selector) {
      if(selector === undefined || selector.value === '') {
        // Sometimes a user may delete the current spec, so we should set to default when this happens
        formDesigner.uiSpecHandler.getSpec("default_ui_specification");
      } else {
        uiSpecification.currentID = selector.value;
        formDesigner.uiSpecHandler.getSpec(selector.value);
      }
    }
  },
  canvasHandler: {
    displayFormList: function (forms) {
      let tpl = '';
      $.each(forms, function(index, value) {
        tpl += fdTemplates.formListItem(value);
      });
      $('#form-list-container').html(tpl);
    },
    setDimensions() {

      $(".form-designer-wrapper").css("width", uiSpecification.currentObject.dimension.width + "px");

    },
    formFields: {
      widestInFormGroup: function() {
        let w;
        $(".form-group-wrapper").each(function(index, value) {
          w = Math.max.apply(null, $(value).find('label, input, select').map(function () {
            return $(this).outerWidth(true);
          }).get());
          $(value).css('width',w+'px');
        });        
      },
      setFormGroupPosition: function(targetID) {
        let fgp = {
          top: Math.min.apply(null, [$(`label[for= '${targetID}']`).offset().top, $(`#${targetID}`).offset().top]),
          left: Math.min.apply(null, [$(`label[for= '${targetID}']`).offset().left, $(`#${targetID}`).offset().left])
        };
        return fgp;
        
      },
      wrapLabelControl: function(stacked = true) {
        // Set dom back to static with undo snapshot
        formDesigner.fdObjectHandler.snapShot(true);

        let elems = document.getElementsByClassName("fd-path-object");
        $(".form-group-wrapper").contents().unwrap();
        $.each(elems, function(index, value) {
          if (value.tagName === 'LABEL') {
            targetID = $(value)[0].htmlFor;
            let fgp = formDesigner.canvasHandler.formFields.setFormGroupPosition(targetID);
            $(`label[for= '${targetID}'], #${targetID}`).removeAttr('style');
            $(`label[for= '${targetID}'], #${targetID}`).wrapAll(`<div class="form-group form-group-wrapper" />`);
          }
        });
        if (stacked) { formDesigner.canvasHandler.formFields.widestInFormGroup(); }
        formDesigner.fdObjectHandler.snapShot();
        formDesigner.fdObjectHandler.quickSnap();
        $(".form-group-wrapper").contents().unwrap();

      },
      display: function (formName) {
        let fields = formDesigner.dataHandler.getFormFields(formName);
        let tpl = [];
        $.each
        (
          fields,
          function(index, value) 
          {
            if(value.is_hidden != null && value.is_hidden == true)
            {
                // do nothing
                console.log("here");
            }
            else switch(value.type.toLowerCase())
            {
              case 'group':
              case 'grid':
                tpl.push(fdTemplates.formFields.controls.group(formName, value));
                break;

              case 'hidden':
                // do nothing
                return;
                break;
              // tpl += `<div class="form-group form-group-wrapper form-field-item resize-drag drag-drop yes-drop item">`;
              case 'label':
                  tpl.push(fdTemplates.formFields.prompt(formName, value));
                break;
              case 'list':
                  tpl.push(fdTemplates.formFields.prompt(formName, value));
                  tpl.push(fdTemplates.formFields.controls.list(formName, value));
                  break;
              case 'textarea': 
                tpl.push(fdTemplates.formFields.prompt(formName, value));
                tpl.push(fdTemplates.formFields.controls.textarea(formName, value));
                break;
            case 'html_area': 
                tpl.push(fdTemplates.formFields.prompt(formName, value));
                tpl.push(fdTemplates.formFields.controls.html_area(formName, value));
                break;
            case 'always_enabled_button':
              case 'button':
                tpl.push(fdTemplates.formFields.prompt(formName, value));
                tpl.push(fdTemplates.formFields.controls.button(formName, value));
                break;
              

              case 'chart':
                tpl.push(fdTemplates.formFields.controls.chart(formName, value));
                break;
              case 'date':
                    tpl.push(fdTemplates.formFields.prompt(formName, value));
                    tpl.push(fdTemplates.formFields.controls.date(formName, value));                
                    break;
              case 'string':
              default:
                  tpl.push(fdTemplates.formFields.prompt(formName, value));
                  tpl.push(fdTemplates.formFields.controls.string(formName, value));
                break;
            
            }
          }
        );
        $('#fd-canvas').html(tpl.join(""));

        // Must initialize Drag Select functionality for fields after they have been loaded to the DOM
        fdDragSelect = new DragSelect({
          selectables: document.querySelectorAll(".resize-drag"),
          area: document.getElementById("fd-canvas-wrapper")
        });

        // On display we should pass true as 2nd paramater for snapshot so that we can read what is already saved.
        formDesigner.fdObjectHandler.snapShot(false, true);

        // Initialize FD Object for form
        formDesigner.fdObjectHandler.mapToSpec();

        // Mark active form
        metaData.activeForm = formName;

        // Set form-list button active states
        $('.form-list-item').removeClass('active');
        $(`#${formName}`).addClass('active')
      },
    },
    wysiwyg: {
      checkHierarchy: function() {

      },
      wrap: function() {

        $('.ds-selected').each(function(index, obj) {
          $(this).parents().removeClass('ds-selected');
        })

        if (globalDSelected.length > 0) {
          $('#btn-multiDrag').removeClass('multiDrag');
          globalDSelected = [];
          $(".multiDrag").removeClass("multiDrag");
        } else {
          $("#btn-multiDrag").addClass('multiDrag');
          globalDSelected = [...$(".ds-selected")];
          $(".ds-selected").addClass("multiDrag");
        }
      },
      unwrap: function() {
        globalDSelected = [];
        $(".multiDrag").removeClass("multiDrag");
      },
      inline: function() {
        $(".ds-selected").removeAttr("style");
        $(".ds-selected").wrapAll(`<div id='temp-wrap' />`);
        $("#temp-wrap").contents().unwrap();
        formDesigner.fdObjectHandler.snapShot();
        formDesigner.fdObjectHandler.quickSnap();
      },
      bold: function() {
        let elems = fdDragSelect.getSelection();
        $.each(elems, function (index, value) {
          let fontWeight = $(value).css("font-weight");
          if(fontWeight === 'normal' || fontWeight <= 400) {
            $(value).css('font-weight', 'bold');
          } else {
            $(value).css("font-weight", "");
          }
        });
        formDesigner.fdObjectHandler.snapShot();
        formDesigner.fdObjectHandler.quickSnap();
      },
      italic: function() {
        let elems = fdDragSelect.getSelection();
        $.each(elems, function (index, value) {
          let fontStyle = $(value).css("font-style");
          if (fontStyle === 'italic') {
            $(value).css('font-style', '');
          } else {
            $(value).css("font-style", "italic");
          }
        });
        formDesigner.fdObjectHandler.snapShot();
        formDesigner.fdObjectHandler.quickSnap();
      },
      color: function() {
        let color = prompt("Enter your color in hex ex:#f1f233");
        $(".ds-selected").css('color', color);
        formDesigner.fdObjectHandler.snapShot();
        formDesigner.fdObjectHandler.quickSnap();
      },
      fontSize: function(control) {
        let elems = fdDragSelect.getSelection();
        $.each(elems, function(index, value) {
          let fontSize = parseInt($(value).css('font-size'));
          if(control === 'increase') {
            fontSize = fontSize + 1 + 'px';
          } else {
            fontSize = fontSize - 1 + 'px';
          }
          $(value).css({ 'font-size': fontSize });
        });
        formDesigner.fdObjectHandler.snapShot();
        formDesigner.fdObjectHandler.quickSnap();
      },
      stackLabelControl: function(stacked = true) {
        formDesigner.canvasHandler.formFields.wrapLabelControl(stacked);
      }
    }
  }
}

// Application initializations without user prompting
formDesigner.dataHandler.newBuild();
formDesigner.uiSpecHandler.newBuild();


$('#custom-fd-commands').bind("enterKey",function(e){
  execute_command_click();
});
$('#custom-fd-commands').keyup(function(e){
   if(e.keyCode == 13)
   {
       $(this).trigger("enterKey");
   }
});

function execute_command_click()
{
  //
  var message_area = document.getElementById("fd-messages");
  var cmd_test = document.getElementById("custom-fd-commands").value;

  var valid_command_regex = /^[a-z\+]{2,6}( -?\d+)?( -?\d+)?$/

  if(valid_command_regex.test(cmd_test.trim()))
  {

    var cmd_text = cmd_test.trim().split(' ');
    var message = "execute command clicked\n\n" + cmd_text;

    
    switch(cmd_text[0].toLowerCase())
    {
        case "ls":
          message += "\n\nlist selection";
          message += list_selection();
          break;
        case "all":
          message += "\n\nselect all";
          message += select_all_canvas_child_nodes();
          break;    
        case "none":
            message += "\n\nremove all selections";
            message += remove_all_selections_on_canvas();
            break;   
        case "al":
            message += "\n\nalign left selection";
            formDesigner.fdObjectHandler.quickSnap(true);
            message += align_left_selection();
            break;
        case "al+":
          message += "\n\nalign left+ selection";
          formDesigner.fdObjectHandler.quickSnap(true);
          message += align_left_selection(true);
          break;          
        case "nx":
            message += "\n\nudge x";
            formDesigner.fdObjectHandler.quickSnap(true);
            if(cmd_text.length > 1)
            {
              message += nudge_left_selection(cmd_text[1])
            }
            else
            {
              message += nudge_left_selection(5);
            }
            break;
        case "ny":
          message += "\n\nnudge y";
          formDesigner.fdObjectHandler.quickSnap(true);
          if(cmd_text.length > 1)
          {
            message += nudge_top_selection(cmd_text[1])
          }
          else
          {
            message += nudge_top_selection(5);
          }
          break;  
        case "at":
            message += "\n\nalign top selection";

            formDesigner.fdObjectHandler.quickSnap(true);
            message += align_top_selection();
            break;
        case "at+":
          message += "\n\nalign top+ selection";

          formDesigner.fdObjectHandler.quickSnap(true);
          message += align_top_selection(true);
          break;          
        case "aw":
            message += "\n\nalign width selection";
            formDesigner.fdObjectHandler.quickSnap(true);
            message += align_width_selection()
            break;
        case "aw+":
          message += "\n\nalign width+ selection";
          formDesigner.fdObjectHandler.quickSnap(true);
          message += align_width_selection(true);
          break;          
        case "ah":
            message += "\n\nalign height selection";
            formDesigner.fdObjectHandler.quickSnap(true);
            message += align_height_selection()
            break;
        case "ah+":
          message += "\n\nalign height+ selection";
          formDesigner.fdObjectHandler.quickSnap(true);
          message += align_height_selection(true);
          break;   
        case "height":
            message += "\n\set height on selection";
            formDesigner.fdObjectHandler.quickSnap(true);
            if(cmd_text.length > 1)
            {
                message += set_height_on_selection(cmd_text[1])
            }
            else
            {
                message += set_height_on_selection(24);
            }
            break;    
        case "ahs":
            message += "\n\nalign height space of selection";
            formDesigner.fdObjectHandler.quickSnap(true);
            if(cmd_text.length > 1)
            {
              if(cmd_text.length > 2)
              {
                message += align_height_space_of_selection(cmd_text[1], cmd_text[2])
              }
              else
              {
                message += align_height_space_of_selection(cmd_text[1])
              }
            }
            else
            {
              message += align_height_space_of_selection(15);
            }
            
            break;
        case "aws":
            message += "\n\nalign width space of selection";
            formDesigner.fdObjectHandler.quickSnap(true);
            if(cmd_text.length > 1)
            {
              message += align_width_space_of_selection(cmd_text[1])
            }
            else
            {
              message += align_width_space_of_selection(15);
            }
            
            break;

        case "st":
        case "stack":
          message += "\n\nstack selected fields";
          formDesigner.fdObjectHandler.quickSnap(true);

          if(cmd_text.length > 1)
          {
            if(cmd_text.length > 2)
            {
              message += stack_selected_fields(cmd_text[1], cmd_text[2]);
            }
            else
            {
              message += stack_selected_fields(cmd_text[1]);
            }
          }
          else
          {
            message += stack_selected_fields();
          }
          
          break;          

        case "ro":
        case "row":
          message += "\n\nmake row of selected fields";
          formDesigner.fdObjectHandler.quickSnap(true);
          message += make_row_of_selected_fields();
          break;  
        case "sac" :
            message += "\n\nselect all container child nodes";
            formDesigner.fdObjectHandler.quickSnap(true);
            message += select_container_child_nodes();
        break;
        case "help":
            message += "ls list selection\n";
            message += "all select all\n";
            message += "none remove all selections\n";
            message += "al align left selection\n";
            message += "al+ nalign left+ selection\n";
            message += "at align top selection (align to lowest)\n";
            message += "at+ align top+ selection (align to highest)\n";
            message += "aw align width selection (align to smallest)\n";
            message += "aw+ align width+ selection (align to biggest)\n";
            message += "ah align height selection (align to smallest)\n";
            message += "ah+ align height selection (align to biggest)\n";
            
            message += "ahs ## ## ## align height space of selection\n";
            message += "aws ## ## ## align width space of selection\n";
            message += "height ## set height default = 24 number is px unit\n";
            message += "st or stack stack controls\n"
            message += "ro or row - make single row of controls\n"
            message += "sac - select all container child nodes\n"
            message += "help get list of commands\n"
        break;
          
            
    }

    message_area.innerHTML = sanitizeHTML(message);
  }
  else
  {
    message_area.innerHTML = "invalid command";
  }
}

function list_selection()
{

  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");
  var selected_item_list = [];
  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }

  result += "\n number of items selected: " + selected_item_list.length;

  for(var i = 0; i < selected_item_list.length; i++)
  {
    let item = selected_item_list[i];
    let rect = null;

    if(item.localName)
    switch(item.localName.toLowerCase())
    {
      case "label":
          rect = item.getBoundingClientRect();
          result += "\n\t" + item.localName;
          result += ": [" + item.getAttribute("for") + "]";
          result += " left: " + item.offsetLeft
          result += " top: " + item.offsetTop
          break;
  

      case "input":
        rect = item.getBoundingClientRect();
        result += "\n\t" + item.localName;
        result += ": [" + item.getAttribute("id") + "]";
        result += " left: " + item.offsetLeft
        result += " top: " + item.offsetTop

        break;
    }
  }


  return result;
}


function select_all_canvas_child_nodes()
{

  var canvas_element = document.querySelector(".form-designer-canvas");
  var result = "";

  let canvas_children = canvas_element.children;

  var html_list = document.getElementsByClassName("ds-selected");
  var selected_item_list = [];

  for(let i = 0; i < canvas_children.length; i++)
  {
    selected_item_list.push(canvas_children[i]);
  }


  for(var i = 0; i < html_list.length; i++)
  {
    html_list[i].classList.remove("ds-selected");
  }


  result += "\n number of items selected: " + selected_item_list.length;

  for(var i = 0; i < selected_item_list.length; i++)
  {
    let item = selected_item_list[i];
    let rect = null;

    if(item.localName)
    {
      item.classList.add("ds-selected");
      switch(item.localName.toLowerCase())
      {
        case "label":
            rect = item.getBoundingClientRect();
            result += "\n\t" + item.localName;
            result += ": [" + item.getAttribute("for") + "]";
            result += " left: " + item.offsetLeft
            result += " top: " + item.offsetTop
            break;
    

        
        case "input":
        case "select":
        case "textarea":
        case "html_area":
        case "fieldset":
          rect = item.getBoundingClientRect();
          result += "\n\t" + item.localName;
          result += ": [" + item.getAttribute("id") + "]";
          result += " left: " + item.offsetLeft
          result += " top: " + item.offsetTop

          break;
        default:
          result += " missing: localname " + item.localName
          break;

      }
    }
    else
    {
      result += " missing: localname " + item;
    }

  }


  return result;
}

function select_container_child_nodes()
{

  var canvas_element = document.getElementsByClassName("ds-selected")[0];
  var result = "";

  let canvas_children = canvas_element.children;

  //var html_list = document.getElementsByClassName("ds-selected");
  //var selected_item_list = [];

  for(let i = 0; i < canvas_element.children.length; i++)
  {
    if(canvas_children[i].localName != "legend")
    {
      canvas_children[i].classList.add("ds-selected");
    }
    
  }


  canvas_element.classList.remove("ds-selected");

  result += "\n number of items selected: " + canvas_element.children.length;


  return result;
}





function remove_all_selections_on_canvas()
{

  window.DragSelect.selected = [];

  
  var html_list = document.getElementsByClassName("ds-selected");


  for(var i = 0; i < html_list.length; i++)
  {
    html_list[i].classList.remove("ds-selected");
  }

  return "\n unselected all items";
}


function align_left_selection(p_is_max_left)
{

  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");
  var selected_item_list = [];
  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }


  result += "\n number of items selected: " + selected_item_list.length;

  if(selected_item_list.length > 0)
  {

    selected_item_list.sort(align_left_compare);

    if(p_is_max_left)
    {
      selected_item_list.reverse();
    }

    var lowest_left = selected_item_list[0].offsetLeft;

    for(var i = 1; i < selected_item_list.length; i++)
    {
      selected_item_list[i].style.left = lowest_left + "px";
    }
  }
  
  return result;
}


function align_top_selection(p_is_max_top)
{

  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");
  var selected_item_list = [];
  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }


  result += "\n number of items selected: " + selected_item_list.length;

  if(selected_item_list.length > 0)
  {

    selected_item_list.sort(align_top_compare);

    if(p_is_max_top)
    {
      selected_item_list.reverse();
    }

    var lowest_top = selected_item_list[0].offsetTop;

    for(var i = 1; i < selected_item_list.length; i++)
    {
      selected_item_list[i].style.top = lowest_top + "px";
    }
  }

  return result;
}

//**
function nudge_left_selection(p_value)
{
  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");
  var selected_item_list = [];
  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }

  result += "\n number of items selected: " + selected_item_list.length;

  if(selected_item_list.length > 0)
  {
    for(let i = 0; i < selected_item_list.length; i++)
    {
        let current_left = selected_item_list[i].offsetLeft;
        let new_left = current_left + parseInt(p_value);
      selected_item_list[i].style.left = new_left + "px";
    }
  }
  
  return result;
}


function nudge_top_selection(p_value)
{
    var result = "";

    var html_list = document.getElementsByClassName("ds-selected");
    var selected_item_list = [];
    for(var i = 0; i < html_list.length; i++)
    {
      selected_item_list.push(html_list[i]);
    }
  
    result += "\n number of items selected: " + selected_item_list.length;
  
    if(selected_item_list.length > 0)
    {
      for(let i = 0; i < selected_item_list.length; i++)
      {
          let current_top = selected_item_list[i].offsetTop;
          let new_top = current_top + parseInt(p_value);
        selected_item_list[i].style.top = new_top + "px";
      }
    }
    
    return result;
}

//**



function align_width_selection(p_is_max_width)
{

  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");
  var selected_item_list = [];

  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }

  result += "\n number of items selected: " + selected_item_list.length;

  if(selected_item_list.length > 0)
  {

    selected_item_list.sort(align_width_compare);

    if(p_is_max_width)
    {
      selected_item_list.reverse();
    }

    var lowest_width = selected_item_list[0].offsetWidth;

    for(var i = 1; i < selected_item_list.length; i++)
    {
      selected_item_list[i].style.width = lowest_width + "px";
    }
  }

  return result;
}


function set_height_on_selection(p_height_in_px)
{

  var result = "";


  var html_list = document.getElementsByClassName("ds-selected");
  var selected_item_list = [];

  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }

  result += "\n number of items selected: " + selected_item_list.length;

  if(selected_item_list.length == 1)
  {

    selected_item_list[0].style.height = p_height_in_px + "px";
/*
    for(var i = 1; i < selected_item_list.length; i++)
    {
      selected_item_list[i].style.height = p_height_in_px + "px";
    }*/
  }

  return result;
}

function align_height_selection(p_is_max_height)
{

  var result = "";


  var html_list = document.getElementsByClassName("ds-selected");
  var selected_item_list = [];

  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }

  result += "\n number of items selected: " + selected_item_list.length;

  if(selected_item_list.length > 0)
  {

    selected_item_list.sort(align_height_compare);

    if(p_is_max_height)
    {
      selected_item_list.reverse();
    }

    var lowest_height = selected_item_list[0].offsetHeight;

    for(var i = 1; i < selected_item_list.length; i++)
    {
      selected_item_list[i].style.height = lowest_height + "px";
    }
  }

  return result;
}


function align_height_space_of_selection(p_pixels, p_field_pixels)
{

  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");

  var selected_item_list = [];

  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }
  
  

  result += "\n number of items selected: " + selected_item_list.length;

  
  if(selected_item_list.length > 1)
  {
    selected_item_list.sort(align_top_compare);

    for(var i = 1; i < selected_item_list.length; i++)
    {
      var previous_item = selected_item_list[i - 1];
      if
      (
        previous_item.localName && 
        previous_item.localName == "label" && 
        p_field_pixels

      )
      {
        selected_item_list[i].style.top = (previous_item.offsetTop + previous_item.offsetHeight + new Number(p_field_pixels))  + "px";
      }
      else
      {
        selected_item_list[i].style.top = (previous_item.offsetTop + previous_item.offsetHeight + new Number(p_pixels))  + "px";
      }
      
    }
  }

  return result;
}


function align_width_space_of_selection(p_pixels)
{

  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");

  var selected_item_list = [];

  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }
  
  

  result += "\n number of items selected: " + selected_item_list.length;

  
  if(selected_item_list.length > 1)
  {
    selected_item_list.sort(align_left_compare);

    for(var i = 1; i < selected_item_list.length; i++)
    {
      var previous_item = selected_item_list[i - 1];

      selected_item_list[i].style.left = (previous_item.offsetLeft + previous_item.offsetWidth + new Number(p_pixels))  + "px";
    }
  }

  return result;
}

function align_left_compare(a, b)
{
  if (a.offsetLeft > b.offsetLeft) return 1;
  if (b.offsetLeft > a.offsetLeft) return -1;

  return 0;
}

function align_top_compare(a, b)
{
  if (a.offsetTop > b.offsetTop) return 1;
  if (b.offsetTop > a.offsetTop) return -1;

  return 0;
}

function align_height_compare(a, b)
{
  if (a.offsetHeight > b.offsetHeight) return 1;
  if (b.offsetHeight > a.offsetHeight) return -1;

  return 0;
}

function align_width_compare(a, b)
{
  if (a.offsetWidth > b.offsetWidth) return 1;
  if (b.offsetWidth > a.offsetWidth) return -1;

  return 0;
}

function prompt_and_control_compare(a, b)
{


  if(a.localName && b.localName)
  {
    let a_value;
    let b_value;

    
    switch(a.localName.toLowerCase())
    {
      case "label":
        a_value = a.getAttribute("for").toLowerCase() + "__1";
        break;
      case "input":
      case "textarea":
        case "html_area":
      case "select":
        a_value = a.getAttribute("id").toLowerCase() + "__2";
        break;
    }

    switch(b.localName.toLowerCase())
    {
      case "label":
        b_value = b.getAttribute("for").toLowerCase() + "__1";
        break;
      case "input":
      case "textarea":
        case "html_area":
      case "select":        
        b_value = b.getAttribute("id").toLowerCase() + "__2";
        break;
    }

    if (a_value > b_value) return 1;
    if (a_value > b_value) return -1;
  
    return 0;
  }
  else return 0;

  
}


function stack_selected_fields(p_pixels)
{

  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");

  var selected_item_list = [];

  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }
  
  

  result += "\n number of items selected: " + selected_item_list.length;

  
  if(selected_item_list.length > 1)
  {
    selected_item_list.sort(prompt_and_control_compare);

    for(var i = 1; i < selected_item_list.length; i+=2)
    {
      var previous_item = selected_item_list[i - 1];
      var current_item = selected_item_list[i];
      if
      (
        previous_item.localName && 
        previous_item.localName == "label" && 
        current_item.localName &&
        (
          current_item.localName == "input" ||
          current_item.localName == "textarea" ||
          current_item.localName == "html_area" ||
          current_item.localName == "select"
        )
      )
      {
        //previous_item[i].style.top = (previous_item.offsetTop + previous_item.offsetHeight + new Number(p_field_pixels))  + "px";
        if(p_pixels == null)
        {
          p_pixels = 15;
        }
        current_item.style.top = (previous_item.offsetTop + previous_item.offsetHeight + new Number(p_pixels))  + "px";
        current_item.style.left = previous_item.offsetLeft + "px";
      }      
    }
  }

  return result;
}


function make_row_of_selected_fields()
{

  var result = "";

  var html_list = document.getElementsByClassName("ds-selected");

  var selected_item_list = [];

  for(var i = 0; i < html_list.length; i++)
  {
    selected_item_list.push(html_list[i]);
  }
  
  

  result += "\n number of items selected: " + selected_item_list.length;

  
  if(selected_item_list.length > 1)
  {
    selected_item_list.sort(prompt_and_control_compare);

    for(var i = 1; i < selected_item_list.length; i+=2)
    {
      var previous_item = selected_item_list[i - 1];
      var current_item = selected_item_list[i];
      if
      (
        previous_item.localName && 
        previous_item.localName == "label" && 
        current_item.localName &&
        (
          current_item.localName == "input" ||
          current_item.localName == "textarea" ||
          current_item.localName == "html_area" ||
          current_item.localName == "select"
        )
      )
      {
        //previous_item[i].style.top = (previous_item.offsetTop + previous_item.offsetHeight + new Number(p_field_pixels))  + "px";
        current_item.style.top = previous_item.offsetTop + "px";
        current_item.style.left = (previous_item.offsetLeft + previous_item.offsetWidth + 15)  + "px";
      }      
    }
  }

  return result;
}

function sanitizeHTML(str) 
{
	var temp = document.createElement('div');
	temp.textContent = str;
	return temp.innerHTML;
}
