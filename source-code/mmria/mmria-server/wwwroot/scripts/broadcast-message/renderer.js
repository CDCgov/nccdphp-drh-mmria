function render()
{
    var result = [];
    var message_one = MESSAGE_ONE_Buffer;
    var message_two = MESSAGE_TWO_Buffer;
    result.push
    (
        `
        <form class="w-95 flex-column pl-1 pr-4" id="message-one-form">
            <div>
                <h2 class="h3" id="message-one-header">Message 1 ${message_one.publish_status == 0 ? '<i>(Unpublished)</i>' : '(Published)'}</h2>            
            </div>
            <div>
                <p id="message-one-title-label" class="h5 mt-4">Title <i>(Limit 250 characters)</i></p>
                <input aria-labelledby="message-one-title-label" class="col form-control" type=text id=message-one-title maxlength="250" value="${message_one.draft.title}" onchange="on_message_one_title_change(this.value)"/>           
            </div>
            <div>
                <p id="message-one-body-label" class="h5">Details <i>(Limit 2000 characters)</i></p>
                <textarea aria-labelledby="message-one-body-label" class="col form-control" id="message-one-body" rows=10 cols=80 maxlength="2000" onchange="on_message_one_body_change(this.value)">${message_one.draft.body}</textarea>
            </div>
            <div id="message_one_type_fieldset">
                ${render_message_one_type_control(message_one.draft.type)}
            </div>
            <div id="message_one_draft_preview">
                ${render_draft_preview(message_one, "one")}
                ${render_published_version(message_one, "one")} 
            </div>
            </div>
            <div class="row">
                <div class="ml-auto pr-3">
                    <input id="message_one_draft_save_button" ${message_one.draft.title.length <= 0 ? "disabled='true' aria-disabled='true'": ""} class="btn btn-primary mr-1" type="button" value="Save Draft" onclick="save_draft_message_one()" />
                    <input id="message_one_publish_button" ${message_one.draft.title.length <= 0 ? "disabled='true' aria-disabled='true'": ""} class="btn btn-primary mr-1" type="button" value="Publish Latest Draft" onclick="publish_message_one()" />
                    <input id="unpublish-message-one" class="btn primary-button mr-1" type="button" value="Unpublish Message" onclick="unpublish_message_one()"  ${message_one.publish_status == 0 ? "disabled" : "" } />
                    <input style="padding-left: 1.5rem !important; padding-right: 1.5rem !important;" class="btn secondary-button" type="button" value="Reset" onclick="reset_message_one()" />
                </div>
            </div>
        </form>
        <form class="w-95 flex-column pl-1 pr-4" id="message-two-form">
            <div>
                <h2 class="h3 mt-4" id="message-two-header">Message 2 ${message_two.publish_status == 0 ? '<i>(Unpublished)</i>' : '(Published)'}</h2>
            </div>
            <div>
                <p id="message-two-title-label" class="h5">Title <i>(Limit 250 characters)</i></p>
                <input aria-labelledby="message-two-title-label" class="col form-control" type=text id=message-two-title maxlength="250" value="${message_two.draft.title}" onchange="on_message_two_title_change(this.value)" />           
            </div>
            <div>
                <p id="message-two-body-label" class="h5">Details <i>(Limit 2000 characters)</i></p>
                <textarea aria-labelledby="message-two-body-label" class="col form-control" id="message-two-body" rows=10 cols=80 maxlength="2000" onchange="on_message_two_body_change(this.value)">${message_two.draft.body}</textarea>
            </div>
            <div id="message_two_type_fieldset">
                ${render_message_two_type_control(message_two.draft.type)}
            </div>
            <div id="message_two_draft_preview">
                ${render_draft_preview(message_two, "two")}
                ${render_published_version(message_two, "two")}
            </div>
            </div>
            <div class="row">
                <div class="ml-auto pr-3">
                    <input id="message_two_draft_save_button" ${message_two.draft.title.length <= 0 ? "disabled='true' aria-disabled='true'": ""} class="btn btn-primary mr-1" type="button" value="Save Draft" onclick="save_draft_message_two()" />
                    <input id="message_two_publish_button" ${message_two.draft.title.length <= 0 ? "disabled='true' aria-disabled='true'": ""} class="btn btn-primary mr-1" type="button" value="Publish Latest Draft" onclick="publish_message_two()" />
                    <input id="unpublish-message-two" class="btn primary-button mr-1" type="button" value="Unpublish Message" onclick="unpublish_message_two()" ${message_two.publish_status == 0 ? "disabled" : "" }/>
                    <input style="padding-left: 1.5rem !important; padding-right: 1.5rem !important;" class="btn secondary-button" type="button" value="Reset" onclick="reset_message_two()" />
                </div>            
            </div>
        </form>
        `
    );
    return result;
}

function createTypePreviewHTML(message, p_message_number)
{
    return render_draft_preview(message, p_message_number) + render_published_version(message, p_message_number);
}

function render_draft_preview(message, p_message_number)
{
    var draftPreviewHTML = ``;
    var draftAlertTypeStylings= [];

    if (message.draft.type == "information")
        draftAlertTypeStylings = ["info-banner", "info-icon", "icon_info", "bg-primary"]
    else if (message.draft.type == "warning")
        draftAlertTypeStylings = ["warning-banner", "warning-icon", "icon_warning", "btn-primary"]
    else
        draftAlertTypeStylings = ["error-banner", "error-icon", "icon_error", "btn-primary"]
        draftPreviewHTML = `
        <p class="h5">Draft Preview</p>
        <div class="ml-2 pl-2 w-100">
            <div class="${draftAlertTypeStylings[0]} col-md-12">
              <img class="${draftAlertTypeStylings[1]}" src="./img/${draftAlertTypeStylings[2]}.svg">
              <span class="margin-pagealert mb-0 col-md-9">
                ${message.draft.title}
              </span>
              <span class="ml-auto">${message.draft.body.length > 0 ? `<div class="col flex-grow-0"><input class="btn ${draftAlertTypeStylings[3]}" type="button" onclick="draft_message_detail_button_${p_message_number}_click()" value="Details" /></div>` : ``}</span>
            </div>
        </div>
    `;

    return draftPreviewHTML;
}

        // <div>
        //     <div class="alert border-top-0 ${draftAlertTypeStylings[0]} col-md-12">
        //         <div class="row d-flex padding-pagealert align-items-center">
        //             <div class="flex-grow-0 col">
        //                 <span class="fi ${draftAlertTypeStylings[1]} " aria-hidden="true"></span>                        
        //             </div>
        //             <div class="col">
        //                 <p class="margin-pagealert">
        //                 ${message.draft.title}
        //                 </p>		
        //             </div>
        //             ${message.draft.body.length > 0 ? `<div class="col flex-grow-0"><input class="btn ${draftAlertTypeStylings[2]}" type="button" onclick="draft_message_detail_button_${p_message_number}_click()" value="Details" /></div>` : ``}
        //         </div>
        //     </div>
        // </div>

function render_published_version(message, p_message_number)
{
    // Mirror the simplified banner structure used in render_draft_preview
    // Array layout: [bannerClass, iconImgClass, iconImgFileBaseName, detailsButtonClass]
    let publishedAlertTypeStylings = [];

    if (message.published.type == "information")
        publishedAlertTypeStylings = ["info-banner", "info-icon", "icon_info", "bg-primary"];
    else if (message.published.type == "warning")
        publishedAlertTypeStylings = ["warning-banner", "warning-icon", "icon_warning", "btn-primary"];
    else
        publishedAlertTypeStylings = ["error-banner", "error-icon", "icon_error", "btn-primary"];

    if (message.publish_status == 0) {
        return `
            <p class="h5">Published Version</p>
            <i>No message published.</i>
        `;
    }

    return `
        <p class="h5">Published Version</p>
        <div class="ml-2 pl-2 w-100">
            <div class="${publishedAlertTypeStylings[0]} col-md-12">
                <img class="${publishedAlertTypeStylings[1]}" src="./img/${publishedAlertTypeStylings[2]}.svg" alt="">
                <span class="margin-pagealert mb-0 col-md-9">
                    ${message.published.title}
                </span>
                <span class="ml-auto">
                    ${
                        message.published.body.length > 0
                        ? `<div class="col flex-grow-0"><input class="btn ${publishedAlertTypeStylings[3]}" type="button" onclick="published_message_detail_button_${p_message_number}_click()" value="Details" /></div>`
                        : ``
                    }
                </span>
            </div>
        </div>
    `;
}

function render_message_one_type_control(value)
{
    return `
    <fieldset>
        <legend class="h5 mb-2 mt-3">Type</legend>
        <div class="form-check ml-4">
            <input class="big-radio form-check-input" type="radio" id="message-one-information" name="message-one-type" value="information" aria-label="Information" ${ value == "information" ? "checked" : "" } onchange="on_message_one_type_change(this.value)">
            <label class="form-check-label" for="message-one-information"> Information (purple)</label>
        </div>
        <div class="form-check ml-4">
            <input class="big-radio form-check-input" type="radio" id="message-one-warning" name="message-one-type" value="warning" aria-label="Information" ${ value == "warning" ? "checked" : "" } onchange="on_message_one_type_change(this.value)">
            <label class="form-check-label" for="message-one-warning"> Warning (yellow)</label>
        </div>
        <div class="form-check ml-4">
            <input class="big-radio form-check-input" type="radio" id="message-one-error" name="message-one-type" value="error" aria-label="Error" ${ value == "error" ? "checked" : "" } onchange="on_message_one_type_change(this.value)">
            <label class="form-check-label" for="message-one-error"> Error (red)</label>
        </div>  
    </fieldset>
    `;
}

function render_message_two_type_control(value)
{
    return `
    <fieldset>
        <legend class="h5 mb-2 mt-3">Type</legend>
        <div class="form-check ml-4">
            <input class="big-radio form-check-input" type="radio" id="message-two-information" name="message-two-type" value="information" aria-label="Information" ${ value == "information" ? "checked" : "" } onchange="on_message_two_type_change(this.value)">
            <label class="form-check-label" for="message-two-information"> Information (purple)</label>
        </div>
        <div class="form-check ml-4">
            <input class="big-radio form-check-input" type="radio" id="message-two-warning" name="message-two-type" value="warning" aria-label="Information" ${ value == "warning" ? "checked" : "" } onchange="on_message_two_type_change(this.value)">
            <label class="form-check-label" for="message-two-warning"> Warning (yellow)</label>
        </div>
        <div class="form-check ml-4">
            <input class="big-radio form-check-input" type="radio" id="message-two-error" name="message-two-type" value="error" aria-label="Error" ${ value == "error" ? "checked" : "" } onchange="on_message_two_type_change(this.value)">
            <label class="form-check-label" for="message-two-error"> Error (red)</label>
        </div> 
    </fieldset>
    `;
}