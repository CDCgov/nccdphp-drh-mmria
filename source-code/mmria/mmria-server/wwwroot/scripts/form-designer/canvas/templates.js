let fdTemplates = {
    formListItem: function(value) 
    {
        return `<button id="${value.name}" type="button" class="btn btn-outline-primary btn-block form-list-item" onclick="javascript: formDesigner.canvasHandler.formFields.display(this.id);">${value.prompt}</button>`;
        //return `<div id="${value.name}" class="form-list-item" onclick="javascript: formDesigner.canvasHandler.formFields.display(this.id);">${value.prompt}</div>`;
    },
    uiSpecification: 
    {
        options: function(value) 
        {
            if(uiSpecification.currentID == value._id) 
            {
                return `<option value="${value._id}" selected="selected">${value.name}</option>`;
            } 
            else 
            {
                return `<option value="${value._id}">${value.name}</option>`;
            }
        },
        dashboard: 
        {
            info: function() 
            {
                return `<span>
                        <strong>Current Specification: ${uiSpecification.currentObject.name}</strong> 
                        <button type="button" class="btn btn-secondary" data-toggle="modal" data-target="#specModal">
                            More Info
                        </button> 
                        <button type="button" class="btn btn-primary" onclick="javascript: formDesigner.uiSpecHandler.modifySpec()">
                            Save Specification
                        </button>
                        </span>`;
            },
            localRev: function() 
            {
                return `Local Revision # ${uiSpecification.localCount } 
                        <button type="button" class="btn btn-warning" onclick="javascript: formDesigner.fdObjectHandler.rollBackRevision()">
                            Undo
                        </button>`;
            }
        },
    },
    formFields: 
    {
        prompt: function(formName, value, p_index) 
        {

            if 
            (
                (
                    value.is_multiselect && 
                    value.is_multiselect == true
                ) ||
                value.control_style && 
                (
                    value.control_style.indexOf("checkbox") > -1 ||
                    value.control_style.indexOf("radio") > -1
                )
            )
            {
                return '';
            } 
            else if 
            (
                value.type.toLowerCase() === 'hidden' || 
                value.type.toLowerCase() === 'button' ||
                value.type.toLowerCase() === 'always_enabled_button' ||
                (value.is_hidden != null && value.is_hidden == true)
            ) 
            {
                return '';
            }
            else
            {
                let prompt = value.prompt;
                if
                (
                    (
                        value.type.toLowerCase() == 'string' ||
                        value.type.toLowerCase() == 'textarea'
                    ) &&
                    value.max_length != null &&
                    parseInt(value.max_length) > 0 &&
                    value.is_display_field_length != null &&
                    value.is_display_field_length == true
                )
                {
                    prompt += ` (Max ${value.max_length} characters)`
                }
                return `<label for="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" data-order="${p_index}">${prompt}</label>`;
            }
            
        },
        controls: 
        {
            string: function(formName, value) 
            {
                return `<input id="${formName}--${value.name}" class="form-control form-field-item resize-drag drag-drop yes-drop item fd-path-object" type="text" ></input>`;
            },
            textarea: function (formName, value) 
            {
                return `<textarea id="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" row="7" cols="80">Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Dolor sed viverra ipsum nunc aliquet bibendum enim. In massa tempor nec feugiat. Nunc aliquet bibendum enim facilisis gravida. Nisl nunc mi ipsum faucibus vitae aliquet nec ullamcorper. Amet luctus venenatis lectus magna fringilla. Volutpat maecenas volutpat blandit aliquam etiam erat velit scelerisque in. Egestas egestas fringilla phasellus faucibus scelerisque eleifend. Sagittis orci a scelerisque purus semper eget duis. Nulla pharetra diam sit amet nisl suscipit. Sed adipiscing diam donec adipiscing tristique risus nec feugiat in. Fusce ut placerat orci nulla. Pharetra vel turpis nunc eget lorem dolor. Tristique senectus et netus et malesuada.
                
                Etiam tempor orci eu lobortis elementum nibh tellus molestie. Neque egestas congue quisque egestas. Egestas integer eget aliquet nibh praesent tristique. Vulputate mi sit amet mauris. Sodales neque sodales ut etiam sit. Dignissim suspendisse in est ante in. Volutpat commodo sed egestas egestas. Felis donec et odio pellentesque diam. Pharetra vel turpis nunc eget lorem dolor sed viverra. Porta nibh venenatis cras sed felis eget. Aliquam ultrices sagittis orci a. Dignissim diam quis enim lobortis. Aliquet porttitor lacus luctus accumsan. Dignissim convallis aenean et tortor at risus viverra adipiscing at.
                </textarea>`;
            },
            html_area: function (formName, value) 
            {
                return `<textarea id="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" row="7" cols="80">Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Dolor sed viverra ipsum nunc aliquet bibendum enim. In massa tempor nec feugiat. Nunc aliquet bibendum enim facilisis gravida. Nisl nunc mi ipsum faucibus vitae aliquet nec ullamcorper. Amet luctus venenatis lectus magna fringilla. Volutpat maecenas volutpat blandit aliquam etiam erat velit scelerisque in. Egestas egestas fringilla phasellus faucibus scelerisque eleifend. Sagittis orci a scelerisque purus semper eget duis. Nulla pharetra diam sit amet nisl suscipit. Sed adipiscing diam donec adipiscing tristique risus nec feugiat in. Fusce ut placerat orci nulla. Pharetra vel turpis nunc eget lorem dolor. Tristique senectus et netus et malesuada.
                
                Etiam tempor orci eu lobortis elementum nibh tellus molestie. Neque egestas congue quisque egestas. Egestas integer eget aliquet nibh praesent tristique. Vulputate mi sit amet mauris. Sodales neque sodales ut etiam sit. Dignissim suspendisse in est ante in. Volutpat commodo sed egestas egestas. Felis donec et odio pellentesque diam. Pharetra vel turpis nunc eget lorem dolor sed viverra. Porta nibh venenatis cras sed felis eget. Aliquam ultrices sagittis orci a. Dignissim diam quis enim lobortis. Aliquet porttitor lacus luctus accumsan. Dignissim convallis aenean et tortor at risus viverra adipiscing at.
                </textarea>`;
            },
            button: function (formName, value) 
            {
                return `<button id="${formName}--${value.name}" type="button" class="btn btn-raised btn-primary form-field-item resize-drag drag-drop yes-drop item fd-path-object">
                            ${value.prompt}
                        </button>`
            },
            list: function (formName, value, p_index) 
            { 
                let listOptions = fdTemplates.formFields.controls.listOptions(value, formName + "--" + value.name);
                let listField;

                /*
                if (value.hasOwnProperty('is_multiselect')) {
                    listField = `
                                <fieldset id="${formName}--${value.name}" class="resize-drag drag-drop yes-drop fd-path-object list-fieldset">
                                    <legend style="width:auto; padding: 8px">${value.prompt}</legend>
                                    ${listOptions}
                                </fieldset>`;
                } else {
                    */

                var list_display_size= '';
                if(value.list_display_size)
                {

                    list_display_size = ' size=' + value.list_display_size;
                }


                /*
                if(value.control_style && value.control_style.indexOf("editable") > -1)
                {
                    listField = `
                                <div id="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" ${list_display_size}>
                                <select style="width:98%;height:49%;">
                                    ${listOptions}
                                </select>
                                </div>
                                
                                `;

                }
                else*/
                if
                (
                    (value.is_multiselect && value.is_multiselect == true) ||
                    value.control_style && 
                    (
                        value.control_style.indexOf("checkbox") > -1 ||
                        value.control_style.indexOf("radio") > -1
                    )
                )
                {

                    listField = `
                    <fieldset id="${formName}--${value.name}" class="resize-drag drag-drop yes-drop fd-path-object"> 
                      <legend data-order="${p_index}">${value.prompt}</legend>
                      ${listOptions}
                    </fieldset>`;


                }
                else
                {
                    listField = `
                      <select id="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" ${list_display_size} >
                          ${listOptions}
                      </select>
                    `;
                }
                //}
                return listField;
            },
            listOptions: function (data, formName) 
            { 
                let values;
                if (data.path_reference != null && data.path_reference != "" && data.path_reference !== undefined) 
                {

                    let p = data.path_reference.split("/")[1];
                    let apiOptionValues = $.grep
                    (
                        metaData.fullObject.lookup, 
                        function (e) 
                        {
                            return e.name == p;
                        }
                    );
                    values = apiOptionValues[0].values;
                } 
                else 
                {
                    values = data.values;
                }

                let markup = '';
 
                if
                (
                    (data.control_style && data.control_style.indexOf("checkbox") > -1) ||
                    (data.is_multiselect && data.is_multiselect == true)
                )
                {
                    $.each
                    (
                        values,
                            function (index, value) 
                        {
                         
                            let control_id = formName + "--" + value.value.replace(/\//g, "--").replace(/ /g, "--").replace(/'/g, "-");
                            if(value.display == '')
                            {
                                markup += `<label for="${control_id}" class="choice-control form-field-item resize-drag drag-drop yes-drop item fd-path-object" data-order="${index}"><input type="checkbox" /><span class="choice-control-info"> (blank)</span></label>`;
                            }
                            else 
                            {
                                markup += `<label for="${control_id}" class="choice-control form-field-item resize-drag drag-drop yes-drop item fd-path-object" data-order="${index}"><input type="checkbox" /><span class="choice-control-info"> ${value.display}</span></label>`;
                            }
                        }
                    );
                }
                else if(data.control_style && data.control_style.indexOf("radio") > -1)
                {
                    $.each
                    (
                        values,
                            function (index, value) 
                        {

                            let control_id = formName + "--" + value.value.replace(/\//g, "--").replace(/ /g, "--").replace(/'/g, "-"); //.replace(/[\/ ]/g, "--");

                            if(value.display == '')
                            {
                                markup += `<label for="${control_id}" class="choice-control form-field-item resize-drag drag-drop yes-drop item fd-path-object" data-order="${index}"><input name="${control_id}" type="radio" ></input><span class="choice-control-info"> (blank)</span></label>`;
                            }
                            else 
                            {
                                markup += `<label for="${control_id}" class="choice-control form-field-item resize-drag drag-drop yes-drop item fd-path-object" data-order="${index}"><input name="${control_id}" type="radio" ></input><span class="choice-control-info">  ${value.display}</span></label>`;
                            }
                        }
                    );
                }
                else
                {
                    $.each
                    (
                        values,
                            function (index, value) 
                        {
                            if (value.value === '') 
                            {
                                markup += `<option selected data-order="${index}">- select -</option>`;
                            }
                            else 
                            {
                                markup += `<option value="${value.value}" data-order="${index}">${value.display}</option>`;
                            }
                        }
                    );
                }
 
                return markup;
            },
            date: function (formName, value) 
            { 
                if(value.is_hidden != null && value.is_hidden == true)
                {
                    return '';
                }
                else return `<input id="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" type="text" ></input>`;
            },
            number: function (formName, value) 
            { 
                return `<input id="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" type="text" ></input>`;
            },
            label: function (formName, value, p_index) 
            { 
                //return `<div id="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" type="text" data-order="${p_index}" >${value.prompt}</div>`;
                return '';
            },
            chart: function (formName, value, p_index) 
            { 
                return `<div id="${formName}--${value.name}" class="form-field-item resize-drag drag-drop yes-drop item fd-path-object" type="text" data-order="${p_index}" ><p>chart</p><h3>${value.prompt}</h3></div>`;
            },
            group: function (parentName, value, p_index) 
            {
                let newGroupName = `${parentName}--${value.name}`;
                let groupFields = '';
                $.each
                (
                    value.children, 
                    function(index, value) 
                    {
                        if(value.type.toLowerCase() === 'group' || value.type.toLowerCase() == 'grid')
                        {
                            //let newSubGroupName = `${newGroupName}--${value.name}`;
                            groupFields += fdTemplates.formFields.controls.group(newGroupName, value, index);
                        }
                        else
                        {
                            groupFields += fdTemplates.formFields.prompt(newGroupName, value, index);
                            
                            if 
                            (
                                value.type.toLowerCase() === 'hidden' ||
                                (value.is_hidden != null && value.is_hidden == true)
                            ) 
                            {
                                return; // hide do nothing
                            }
                            else if(value.type.toLowerCase() === 'list') 
                            {
                                groupFields += fdTemplates.formFields.controls.list(newGroupName, value, index);
                            }
                            else if 
                            (
                                value.type.toLowerCase() === 'button' ||
                                value.type.toLowerCase() === 'always_enabled_button'
                            ) 
                            {
                                groupFields += fdTemplates.formFields.controls.button(newGroupName, value, index)
                            } 
                            else if (value.type.toLowerCase() === 'date') 
                            {
                                groupFields += fdTemplates.formFields.controls.date(newGroupName, value, index)
                            }
                            else if (value.type.toLowerCase() === 'label') 
                            {
                                groupFields += fdTemplates.formFields.controls.label(newGroupName, value, index)
                            } 
                            else if (value.type.toLowerCase() === 'textarea') 
                            {
                                groupFields += fdTemplates.formFields.controls.textarea(newGroupName, value, index)
                            }
                            else if (value.type.toLowerCase() === 'html_area') 
                            {
                                groupFields += fdTemplates.formFields.controls.html_area(newGroupName, value, index)
                            }
                            else if (value.type.toLowerCase() === 'chart') 
                            {
                                groupFields += fdTemplates.formFields.controls.chart(newGroupName, value, index)
                            }
                            else 
                            {
                                groupFields += fdTemplates.formFields.controls.string(newGroupName, value, index);
                            }
                        }
                        // groupFields += `</div>`;
                    }
                )
                
                let group = null;

                if(value.type.toLowerCase() == 'grid')
                {
                    group = `
                    <fieldset id="${newGroupName}" class="resize-drag drag-drop yes-drop fd-path-object"> 
                        <legend data-order="${p_index}">${value.prompt} - 4 item(s)</legend>
                        <div style="height: 100%;">
                            <div class="grid-control-action-icn row no-gutters"">
                                <button type="button" class="grid-control-action-btn mr-1" title="delete">
                                <span class="x24 fill-p text-secondary cdc-icon-close"></span><span class="sr-only">Close</span></button><span> item 1 of 4</span>
                            </div> 
                        
                            ${groupFields}
                        
                        </div>
                        <button type="button" class="grid-control-btn btn d-flex align-items-center">

                        <span class="x24 cdc-icon-plus"></span> Add Item</button>
                        </fieldset>`;
                }
                else
                {
                    group = `
                    <fieldset id="${newGroupName}" class="resize-drag drag-drop yes-drop fd-path-object"> 
                        <legend data-order="${p_index}">${value.prompt}</legend>
                        ${groupFields}
                        </fieldset>`;

                }

                return group;
            }
        }
    }
}