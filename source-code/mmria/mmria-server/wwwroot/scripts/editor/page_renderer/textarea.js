function textarea_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
    if
    (
        p_metadata.name == "notes_about_key_circumstances_surrounding_death" &&
        (
            p_data == null ||
            p_data == ""
        )
    )
    {
        // Do something
    }
    else
    {
        const textareaControlId =
            p_metadata.name == "case_opening_overview"
                ? "case_narrative_editor"
                : `${convert_object_path_to_jquery_id(p_object_path)}_control`;

        p_result.push("<div class='textarea' id='");
        p_result.push(convert_object_path_to_jquery_id(p_object_path));
        p_result.push("'");
        p_result.push(" mpath='");
        p_result.push(p_metadata_path);
        p_result.push("' ");
        p_result.push(">");

        p_result.push(`<label for="${textareaControlId}" `);
        if(p_metadata.description && p_metadata.description.length > 0)
        {
            p_result.push("rel='tooltip' data-original-title='");
            p_result.push(p_metadata.description.replace(/'/g, "\\'"));
            p_result.push("'");
        }

        var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

        if(style_object && p_metadata.name != "case_opening_overview")
        {
            p_result.push(" style='");
            p_result.push(get_style_string(style_object.prompt.style));
            p_result.push("'");
        }
        p_result.push(">");

        let prompt = p_metadata.prompt;
        if
        (
            p_metadata.type.toLowerCase() == 'textarea' &&
            p_metadata.max_length != null &&
            parseInt(p_metadata.max_length) > 0 &&
            p_metadata.is_display_field_length != null &&
            p_metadata.is_display_field_length == true
        )
        {

            let is_highlight_border = false;

            // Check for paste truncation markers
            const elementId = convert_object_path_to_jquery_id(p_object_path) + '_control';
            const element = document.getElementById(elementId);
            const hasPasteTruncation = element && element.getAttribute("data-paste-truncated") === "true";

            if
            (

                p_data != null &&
                p_data.toString().length >= parseInt(p_metadata.max_length) 
            )
            {
                is_highlight_border = true;
                prompt += ` <span style='color: #BB6C49;'>(Max ${p_metadata.max_length} characters)</span>`
            }
            else if (hasPasteTruncation)
            {
                is_highlight_border = true;
                prompt += ` <span style='color: #BB6C49;'>(Max ${p_metadata.max_length} characters)</span>`
            }            
            else
            {
                prompt += ` (Max ${p_metadata.max_length} characters)`
            }


            
        }

            p_result.push(prompt);
            p_result.push
            (`
                ${render_data_analyst_dictionary_link
                (
                    p_metadata, 
                    p_dictionary_path
                )}
            `);
        p_result.push("</label>");



        

        if(p_metadata.name == "case_opening_overview")
        {
            page_render_create_textarea(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path);

            let opts = {
                btns: [
                    ['viewHTML'],
                    ['undo', 'redo'],
                    ['strong', 'em', 'underline', 'del'],
                    ['fontsize'],
                    ['foreColor', 'backColor'],
                    ['justifyLeft', 'justifyCenter', 'justifyRight'],
                    ['unorderedList', 'orderedList'],
                    ['horizontalRule'],
                    ['removeformat'],
                    ['fullscreen'],
                ],
                plugins: {
                    // Add font sizes manually
                    fontsize: {
                        sizeList: [
                            '14px',
                            '16px',
                            '18px',
                            '24px',
                            '32px',
                            '48px'
                        ],
                        allowCustomSize: false
                    },
                    // Add colors manually
                    // Currently utilizing all primary, secondary, tertiary colors in color wheel
                    colors: {
                        colorList: [
                            'FFFFFF',
                            'CCCCCC',
                            '777777',
                            '333333',
                            '000000',
                            'FF0000',
                            '00FF00',
                            '0000FF',
                            'FFFF00',
                            'FF00FF',
                            '00FFFF',
                            'FF7F00',
                            'FF007F',
                            '7FFF00',
                            '7F00FF',
                            '00FF7F',
                            '007FFF'
                        ]
                    }
                },
                semantic: true
            }

            if(g_data_is_checked_out)
            {
                p_post_html_render.push(`$('#case_narrative_editor').trumbowyg(${JSON.stringify(opts)});`);
                p_post_html_render.push(`apply_case_narrative_editor_accessibility();`);
                
                p_post_html_render.push(`
                    $('#case_narrative_editor')
                    .trumbowyg()
                    .on('tbwchange', function ()
                    {
                        tbw_onchange("${p_object_path}","${p_metadata_path}","${p_dictionary_path}");
                    })
                    .on('tbwpaste', function ()
                    {
                        tbw_change_paste("${p_object_path}","${p_metadata_path}","${p_dictionary_path}");
                    });
                `);
                p_post_html_render.push(`attach_narrative_paste_handler("${p_object_path}","${p_metadata_path}","${p_dictionary_path}");`);
            }
            else
            {
                const readOnlyOpts = Object.assign({}, opts, { disabled: true });
                p_post_html_render.push(`$('#case_narrative_editor').trumbowyg(${JSON.stringify(readOnlyOpts)});`);
                p_post_html_render.push(`apply_case_narrative_editor_accessibility();`);
            }

        }
        else
        {
            page_render_create_textarea(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path);
        }

        p_result.push("</div>");
    }
}



/**
 * Strips browser-computed noise from style attributes on pasted content.
 * When copying from a browser page (including from the Trumbowyg editor itself),
 * the clipboard carries the full computed CSS of every element — font-family stacks,
 * orphans, widows, letter-spacing, etc. These are not user-intentional formatting;
 * preserving them causes deeply nested styled spans that accumulate on each paste cycle.
 *
 * Only three properties are considered meaningful to keep on <span> elements:
 *   font-size  — set explicitly by the Trumbowyg font-size toolbar
 *   color      — set explicitly by the Trumbowyg color toolbar
 *   background-color — set explicitly by the Trumbowyg background-color toolbar
 *
 * All other style properties on <span> elements are dropped.
 * Style attributes on <p> and <div> elements are removed entirely (Trumbowyg does
 * not set inline styles on block elements).
 */
function strip_paste_noise_styles(node)
{
    // Browser-computed default values that carry no user intent.
    // When copying from the editor, Chrome/Edge include the full computed CSS;
    // these values represent "no formatting" and must be stripped along with
    // the other noise properties so spans don't accumulate on each paste cycle.
    var DEFAULT_BG    = { 'rgb(255, 255, 255)':true, 'white':true, '#ffffff':true, '#fff':true, 'transparent':true };
    var DEFAULT_COLOR = { 'rgb(0, 0, 0)':true, 'rgb(51, 51, 51)':true, 'rgb(33, 33, 33)':true, 'black':true, '#000000':true, '#000':true };
    // 16px, 1rem, 12px, and the legacy unit-less "12" are the base body font sizes
    // in this app; 12 (no unit) is DOMWalker's old invalid rem-to-px output.
    var DEFAULT_FSIZE = { '16px':true, '1rem':true, '12px':true, '12':true };

    var spans = node.querySelectorAll('span');
    for (var si = 0; si < spans.length; si++)
    {
        var span = spans[si];
        if (!span.hasAttribute('style')) continue;
        var parts = span.getAttribute('style').split(';');
        var kept = [];
        for (var pi = 0; pi < parts.length; pi++)
        {
            var trimmed = parts[pi].trim();
            if (!trimmed) continue;
            var colonIdx = trimmed.indexOf(':');
            if (colonIdx === -1) continue;
            var prop = trimmed.substring(0, colonIdx).trim().toLowerCase();
            var val  = trimmed.substring(colonIdx + 1).trim().toLowerCase();
            // Keep only the three Trumbowyg-toolbar properties, and only when
            // their value is not a browser-default (which would mean the user
            // never intentionally set this property).
            if (prop === 'background-color' && !DEFAULT_BG[val])    { kept.push(trimmed); continue; }
            if (prop === 'color'            && !DEFAULT_COLOR[val]) { kept.push(trimmed); continue; }
            if (prop === 'font-size'        && !DEFAULT_FSIZE[val]) { kept.push(trimmed); continue; }
            // All other properties: drop
        }
        if (kept.length > 0)
        {
            span.setAttribute('style', kept.join('; '));
        }
        else
        {
            span.removeAttribute('style');
        }
    }

    // Unwrap spans with no remaining style attribute — replace the span with its
    // children so the default-value spans don't accumulate across paste cycles.
    // querySelectorAll returns document order; iterating in reverse processes
    // deepest descendants first, keeping parent references valid.
    var unstyledSpans = node.querySelectorAll('span:not([style])');
    for (var ui = unstyledSpans.length - 1; ui >= 0; ui--)
    {
        var uspan  = unstyledSpans[ui];
        var parent = uspan.parentNode;
        if (!parent) continue;
        while (uspan.firstChild) { parent.insertBefore(uspan.firstChild, uspan); }
        parent.removeChild(uspan);
    }

    // Strip all inline styles from semantic inline formatting tags.
    // <strong>, <em>, <u> etc. convey their formatting via the tag name itself;
    // any style attribute is purely browser-computed noise from the clipboard.
    var inlineTags = node.querySelectorAll('strong, em, u, b, i, s, strike');
    for (var it = 0; it < inlineTags.length; it++)
    {
        inlineTags[it].removeAttribute('style');
    }

    // Flatten redundant nested same-tag inline elements.
    // Two cases:
    // (A) Pure nesting — outer has only same-tag children: <strong><strong>text</strong></strong>
    //     → flatten to <strong>text</strong> by lifting inner's children to outer.
    // (B) Mixed-content outer — outer has both a same-tag child AND bare text (or other
    //     element) siblings: <strong><strong>The&nbsp;</strong>decedent</strong>
    //     This is a browser "context wrapper" added when copy starts inside <strong>.
    //     It makes plain text siblings bold/italic when they shouldn't be.
    //     → unwrap the outer entirely; the inner <strong> stays intact.
    var FLAT_TAGS = ['strong', 'b', 'em', 'i', 'u', 's', 'strike'];
    for (var fti = 0; fti < FLAT_TAGS.length; fti++)
    {
        var nested = node.querySelectorAll(FLAT_TAGS[fti] + ' ' + FLAT_TAGS[fti]);
        for (var ni = nested.length - 1; ni >= 0; ni--)
        {
            var innerTag = nested[ni];
            var outerTag = innerTag.parentNode;
            if (!outerTag || outerTag.nodeName.toLowerCase() !== FLAT_TAGS[fti]) continue;
            // Does the outer have mixed content? (a non-whitespace text node, or a child
            // element that is NOT the same inline tag — either makes it a context wrapper)
            var outerHasMixed = false;
            for (var fci = 0; fci < outerTag.childNodes.length; fci++)
            {
                var fch = outerTag.childNodes[fci];
                if (fch.nodeType === Node.TEXT_NODE && fch.nodeValue.trim() !== '')
                    { outerHasMixed = true; break; }
                if (fch.nodeType === Node.ELEMENT_NODE && fch.nodeName.toLowerCase() !== FLAT_TAGS[fti])
                    { outerHasMixed = true; break; }
            }
            if (outerHasMixed)
            {
                // Case (B): spurious context wrapper — unwrap outer, children stay intact.
                var outerParent = outerTag.parentNode;
                if (outerParent)
                {
                    while (outerTag.firstChild) { outerParent.insertBefore(outerTag.firstChild, outerTag); }
                    outerParent.removeChild(outerTag);
                }
            }
            else
            {
                // Case (A): pure nesting — lift inner's children to outer, remove inner.
                while (innerTag.firstChild) { outerTag.insertBefore(innerTag.firstChild, innerTag); }
                outerTag.removeChild(innerTag);
            }
        }
    }

    // Clean blank paragraphs — strip any inline wrappers from empty <p> elements.
    // Pressing Enter after bold text creates <p><strong><br></strong></p> instead of
    // <p><br></p>. When copied and pasted, those inline wrappers come along and can
    // accumulate. Setting innerHTML = '<br>' here produces clean spacer paragraphs.
    var pastedPs = node.querySelectorAll('p');
    for (var ppi = 0; ppi < pastedPs.length; ppi++)
    {
        if (pastedPs[ppi].textContent.trim() === '')
        {
            pastedPs[ppi].innerHTML = '<br>';
        }
    }

    var blocks = node.querySelectorAll('p, div');
    for (var bi = 0; bi < blocks.length; bi++)
    {
        blocks[bi].removeAttribute('style');
    }
}

/**
 * Recursively removes clipboard-metadata nodes from a DOM subtree:
 * - Comment nodes (<!--StartFragment-->, <!--EndFragment-->, and any other comments)
 * - <br class="Apple-interchange-newline"> (Mac WebKit clipboard line-break artifact)
 */
function strip_clipboard_artifacts(node)
{
    for (var i = node.childNodes.length - 1; i >= 0; i--)
    {
        var child = node.childNodes[i];
        if (child.nodeType === Node.COMMENT_NODE)
        {
            node.removeChild(child);
        }
        else if (child.nodeName === 'BR' && child.className === 'Apple-interchange-newline')
        {
            node.removeChild(child);
        }
        else
        {
            strip_clipboard_artifacts(child);
        }
    }
}

/**
 * Attaches a Range API paste handler to the Trumbowyg narrative editor div.
 * Uses the capture phase to intercept paste before Trumbowyg's built-in handler,
 * ensuring content is inserted at the active cursor position.
 * XSS-vector attributes (on*, javascript: hrefs) are stripped via DOMWalker;
 * all structural HTML tags are preserved.
 */
function attach_narrative_paste_handler(p_object_path, p_metadata_path, p_dictionary_path)
{
    var editorElement = document.querySelector('.case-narrative-trumbowyg .trumbowyg-editor');
    if (!editorElement) return;

    editorElement.addEventListener('paste', function(event)
    {
        // Step 1: Capture selection synchronously at TOP — before any DOM manipulation
        var selection = window.getSelection();
        if (!selection || selection.rangeCount === 0) return;
        var range = selection.getRangeAt(0);

        // Prevent browser default paste and Trumbowyg's bubble-phase paste handler
        event.preventDefault();
        event.stopImmediatePropagation();

        // Step 2: Get clipboard data — prefer HTML, fall back to plain text
        var clipboardData = event.clipboardData || window.clipboardData;
        var pastedHtml = clipboardData ? clipboardData.getData('text/html') : '';
        var pastedText = (!pastedHtml && clipboardData) ? (clipboardData.getData('text/plain') || '') : '';

        // Extract only the fragment content between StartFragment/EndFragment markers.
        // Browser clipboard HTML wraps the selection in a full <html><head><body> document
        // and uses these markers to identify the actual copied content. Slicing to the
        // fragment eliminates the document wrapper and the primary marker comments.
        if (pastedHtml)
        {
            var sfStart = pastedHtml.indexOf('<!--StartFragment-->');
            var sfEnd   = pastedHtml.indexOf('<!--EndFragment-->');
            if (sfStart !== -1 && sfEnd !== -1 && sfEnd > sfStart)
            {
                pastedHtml = pastedHtml.substring(sfStart + '<!--StartFragment-->'.length, sfEnd);
            }
            else if (sfStart !== -1)
            {
                pastedHtml = pastedHtml.substring(sfStart + '<!--StartFragment-->'.length);
            }
        }

        // Step 3: Delete currently selected content (if any)
        range.deleteContents();

        // Step 4: Build DocumentFragment from XSS-cleaned paste content
        var fragment = document.createDocumentFragment();
        if (pastedHtml)
        {
            var cleanNode = document.createElement('div');
            cleanNode.innerHTML = pastedHtml;
            // DOMWalker strips on* attributes and javascript: hrefs; preserves all structural tags
            DOMWalker(cleanNode);
            // Remove residual clipboard metadata that survived fragment extraction:
            // Comment nodes (embedded StartFragment/EndFragment from prior paste cycles)
            // and Mac WebKit Apple-interchange-newline <br> elements.
            strip_clipboard_artifacts(cleanNode);
            // Strip browser-computed noise styles; keep only font-size, color, background-color
            // on spans, and remove all inline styles from block elements.
            strip_paste_noise_styles(cleanNode);
            while (cleanNode.firstChild)
            {
                fragment.appendChild(cleanNode.firstChild);
            }
        }
        else if (pastedText)
        {
            fragment.appendChild(document.createTextNode(pastedText));
        }

        // Step 5: Insert at captured range — with block-level safety for block content.
        // When the fragment contains <p>/<div> elements and the cursor is inside an inline
        // element (e.g. <strong>), range.insertNode places the <p> inside that <strong>.
        // The <strong> then bleeds into the pasted paragraph, making the entire paragraph
        // bold/italic/etc. even though only one word in the source was formatted.
        // Fix: detect this case and insert after the nearest block ancestor instead.
        var PASTE_BLOCK_TAGS = { 'p':true, 'div':true, 'ul':true, 'ol':true, 'table':true };
        var fragmentHasBlocks = false;
        for (var _fci = 0; _fci < fragment.childNodes.length; _fci++)
        {
            var _fcn = fragment.childNodes[_fci];
            if (_fcn.nodeType === Node.ELEMENT_NODE && PASTE_BLOCK_TAGS[_fcn.nodeName.toLowerCase()])
            {
                fragmentHasBlocks = true;
                break;
            }
        }

        var _insertedLastNode = null;
        if (fragmentHasBlocks)
        {
            // For block-level paste content, NEVER use range.insertNode unless the cursor is
            // at the editor root level (directly between paragraphs). Using range.insertNode
            // when the cursor is inside ANY element — including inside a <p> at text level —
            // creates <p><p>…</p></p> invalid nesting. The browser's resolution of that invalid
            // structure can leave surrounding <strong>/<em> formatting bleeding into the pasted
            // paragraph. The block-safe path (insert after nearest block ancestor) is always
            // correct: pasted paragraphs become siblings, never children of existing blocks.
            var _ctxEl = range.startContainer.nodeType === Node.ELEMENT_NODE
                         ? range.startContainer
                         : range.startContainer.parentElement;
            // Only skip block-safe insert when cursor is literally AT the editor root div
            // (between paragraphs) — that position is already at block boundary.
            var _isInsideStructure = _ctxEl && !_ctxEl.classList.contains('trumbowyg-editor');

            if (_isInsideStructure)
            {
                // Walk up to the nearest <p> or <div> ancestor
                var _blockAncestor = _ctxEl;
                while (_blockAncestor
                       && !PASTE_BLOCK_TAGS[_blockAncestor.nodeName.toLowerCase()]
                       && !_blockAncestor.classList.contains('trumbowyg-editor'))
                {
                    _blockAncestor = _blockAncestor.parentElement;
                }
                if (_blockAncestor && PASTE_BLOCK_TAGS[_blockAncestor.nodeName.toLowerCase()])
                {
                    // Insert pasted blocks after the block ancestor — safe from inline bleed
                    // and from <p>-inside-<p> invalid nesting.
                    var _nextRef  = _blockAncestor.nextSibling;
                    var _parentRef = _blockAncestor.parentNode;
                    while (fragment.firstChild)
                    {
                        var _nodeToInsert = fragment.firstChild;
                        if (_nextRef) { _parentRef.insertBefore(_nodeToInsert, _nextRef); }
                        else          { _parentRef.appendChild(_nodeToInsert); }
                        _insertedLastNode = _nodeToInsert;
                    }
                }
                else
                {
                    range.insertNode(fragment);
                }
            }
            else
            {
                range.insertNode(fragment);
            }
        }
        else
        {
            range.insertNode(fragment);
        }

        // Step 5b: Post-insert nested inline cleanup.
        // Run the same mixed-content-outer-unwrap + pure-nesting-flatten pass on the
        // entire editor div. This catches any nesting that was created *after* the
        // fragment cleanup ran — e.g. by browser DOM normalization when the insertion
        // point was near an inline element boundary, or by Trumbowyg's semanticCode
        // converting <b> tags.
        var _postEditorEl = document.querySelector('.trumbowyg-editor');
        if (_postEditorEl)
        {
            var _POST_FLAT = ['strong', 'b', 'em', 'i', 'u', 's', 'strike'];
            for (var _pfi = 0; _pfi < _POST_FLAT.length; _pfi++)
            {
                var _pfTag = _POST_FLAT[_pfi];
                var _pfNested = _postEditorEl.querySelectorAll(_pfTag + ' ' + _pfTag);
                for (var _pfni = _pfNested.length - 1; _pfni >= 0; _pfni--)
                {
                    var _pfInner = _pfNested[_pfni];
                    var _pfOuter = _pfInner.parentNode;
                    if (!_pfOuter || _pfOuter.nodeName.toLowerCase() !== _pfTag) continue;
                    var _pfMixed = false;
                    for (var _pfci = 0; _pfci < _pfOuter.childNodes.length; _pfci++)
                    {
                        var _pfch = _pfOuter.childNodes[_pfci];
                        if (_pfch.nodeType === Node.TEXT_NODE && _pfch.nodeValue.trim() !== '')
                            { _pfMixed = true; break; }
                        if (_pfch.nodeType === Node.ELEMENT_NODE && _pfch.nodeName.toLowerCase() !== _pfTag)
                            { _pfMixed = true; break; }
                    }
                    if (_pfMixed)
                    {
                        var _pfPar = _pfOuter.parentNode;
                        if (_pfPar)
                        {
                            while (_pfOuter.firstChild) { _pfPar.insertBefore(_pfOuter.firstChild, _pfOuter); }
                            _pfPar.removeChild(_pfOuter);
                        }
                    }
                    else
                    {
                        while (_pfInner.firstChild) { _pfOuter.insertBefore(_pfInner.firstChild, _pfInner); }
                        _pfOuter.removeChild(_pfInner);
                    }
                }
            }
        }

        // Step 6: Collapse range to end of inserted content and restore selection
        if (_insertedLastNode)
        {
            range.selectNodeContents(_insertedLastNode);
        }
        range.collapse(false);
        selection.removeAllRanges();
        selection.addRange(range);

        // Step 7: Save updated narrative content via the standard change path
        tbw_onchange(p_object_path, p_metadata_path, p_dictionary_path);

    }, true); // capture phase — runs before and suppresses Trumbowyg's bubble-phase handler
}

function tbw_change_paste(p_object_path, p_metadata_path, p_dictionary_path)
{
    let data = $('.trumbowyg-editor').html();

    //g_textarea_oninput(p_object_path, p_metadata_path,p_dictionary_path, data);
    //return;

    let crlf_regex = /\n/g;

    if(data!= null)
    {
        data = data.replace(crlf_regex, " ");
    }
 

    let new_text = textarea_control_strip_html_attributes(data);

    if
    (
        new_text == null && data.length > 0 ||
        new_text.length == 0 && data.length != 0
    )
    {
        console.log("tbw_change_paste null error");
        new_text = data;
    }

    g_textarea_oninput(p_object_path, p_metadata_path,p_dictionary_path, new_text);
}

function tbw_onchange(p_object_path, p_metadata_path, p_dictionary_path)
{
    let data = $('.trumbowyg-editor').html();

    //g_textarea_oninput(p_object_path, p_metadata_path,p_dictionary_path, data);
    //return;

    let new_text = textarea_control_strip_html_attributes(data);

    if
    (
        new_text == null && data.length > 0 ||
        new_text.length == 0 && data.length != 0
    )
    {
        console.log("tbw_change_paste null error");
        new_text = data;
    }

    g_textarea_oninput(p_object_path, p_metadata_path,p_dictionary_path, new_text);
}


function textarea_control_replace_return_with_br(p_value)
{
    let crlf_regex = /\n/g;

    let result = p_value;

    if(p_value!= null)
    {
        result = p_value.replace(crlf_regex, "<br/>");
    }

    return result;
}

function textarea_control_strip_html_attributes(p_value)
{

    let CommentRegex = /<!--\[[^>]+>/gi;

    let Strip5PlusBr = /<br\><br\><br\><br\>+/gi;

    let StripTrailingBR = /<br><br>(<br>|<br>.?)+/gi;

    const Replace1 = /<br><\/p>/gi;
    const Replace2 = /<br><\/span>/gi;
    const Replace3 = /<p><span [^>]+><\/span><\/p>/gi;
    const Replace4 = /<span [^>]+><\/span>/gi;

    let PseudoTagRegex = /<\/?[a-z]:[^>]+>/gi;

    let crlf_regex = /\n/g;

    let node = document.createElement("body");
    node.innerHTML = p_value.replace(CommentRegex,"")
        .replace(crlf_regex," ")
        .replace(Strip5PlusBr,"<br><br>")
        .replace(StripTrailingBR,"")
        .replace(PseudoTagRegex,"")
        .replace(Replace1, "</p>")
        .replace(Replace2, "</span>")
        .replace(Replace3,"")
        .replace(Replace4,"").trim();

    DOMWalker(node);

    // Flatten redundant nested same-tag inline elements in stored HTML.
    // Same two-case logic as strip_paste_noise_styles: mixed-content outers are context
    // wrappers and are unwrapped; pure same-tag nesting is flattened.
    var SAVE_FLAT_TAGS = ['strong', 'b', 'em', 'i', 'u', 's', 'strike'];
    for (var sfti = 0; sfti < SAVE_FLAT_TAGS.length; sfti++)
    {
        var sfNested = node.querySelectorAll(SAVE_FLAT_TAGS[sfti] + ' ' + SAVE_FLAT_TAGS[sfti]);
        for (var sfni = sfNested.length - 1; sfni >= 0; sfni--)
        {
            var sfInner = sfNested[sfni];
            var sfOuter = sfInner.parentNode;
            if (!sfOuter || sfOuter.nodeName.toLowerCase() !== SAVE_FLAT_TAGS[sfti]) continue;
            var sfHasMixed = false;
            for (var sfci = 0; sfci < sfOuter.childNodes.length; sfci++)
            {
                var sfch = sfOuter.childNodes[sfci];
                if (sfch.nodeType === Node.TEXT_NODE && sfch.nodeValue.trim() !== '')
                    { sfHasMixed = true; break; }
                if (sfch.nodeType === Node.ELEMENT_NODE && sfch.nodeName.toLowerCase() !== SAVE_FLAT_TAGS[sfti])
                    { sfHasMixed = true; break; }
            }
            if (sfHasMixed)
            {
                var sfPar = sfOuter.parentNode;
                if (sfPar)
                {
                    while (sfOuter.firstChild) { sfPar.insertBefore(sfOuter.firstChild, sfOuter); }
                    sfPar.removeChild(sfOuter);
                }
            }
            else
            {
                while (sfInner.firstChild) { sfOuter.insertBefore(sfInner.firstChild, sfInner); }
                sfOuter.removeChild(sfInner);
            }
        }
    }

    // Progressive save-path span cleanup — mirrors paste-path strip_paste_noise_styles.
    // Old stored content (pre-4.0) has accumulated <span> wrappers with browser-computed
    // styles. Clean them on every save so old data is healed over time. Uses the same
    // default-value sets to avoid stripping intentional user formatting.
    var SPSV_BG    = { 'rgb(255, 255, 255)':true, 'white':true, '#ffffff':true, '#fff':true, 'transparent':true };
    var SPSV_COLOR = { 'rgb(0, 0, 0)':true, 'rgb(51, 51, 51)':true, 'rgb(33, 33, 33)':true, 'black':true, '#000000':true, '#000':true };
    var SPSV_FSIZE = { '16px':true, '1rem':true, '12px':true, '12':true };
    var saveSpans = node.querySelectorAll('span');
    for (var ssi = 0; ssi < saveSpans.length; ssi++)
    {
        var ssEl = saveSpans[ssi];
        if (!ssEl.hasAttribute('style')) continue;
        var ssParts = ssEl.getAttribute('style').split(';');
        var ssKept = [];
        for (var sspi = 0; sspi < ssParts.length; sspi++)
        {
            var ssT = ssParts[sspi].trim();
            if (!ssT) continue;
            var ssCi = ssT.indexOf(':');
            if (ssCi === -1) continue;
            var ssProp = ssT.substring(0, ssCi).trim().toLowerCase();
            var ssVal  = ssT.substring(ssCi + 1).trim().toLowerCase();
            if (ssProp === 'background-color' && !SPSV_BG[ssVal])    { ssKept.push(ssT); continue; }
            if (ssProp === 'color'            && !SPSV_COLOR[ssVal]) { ssKept.push(ssT); continue; }
            if (ssProp === 'font-size'        && !SPSV_FSIZE[ssVal]) { ssKept.push(ssT); continue; }
        }
        if (ssKept.length > 0) { ssEl.setAttribute('style', ssKept.join('; ')); }
        else                   { ssEl.removeAttribute('style'); }
    }
    var saveUnstyled = node.querySelectorAll('span:not([style])');
    for (var sui = saveUnstyled.length - 1; sui >= 0; sui--)
    {
        var suEl = saveUnstyled[sui];
        var suPar = suEl.parentNode;
        if (!suPar) continue;
        while (suEl.firstChild) { suPar.insertBefore(suEl.firstChild, suEl); }
        suPar.removeChild(suEl);
    }

    // Strip all style attributes from semantic inline formatting tags in stored content.
    // Old data may have <strong style="font-size: 1rem;"> (or similar) from prior paste
    // accumulation or browser execCommand behavior. The tag name carries the formatting;
    // any inline style on it is noise that must be cleaned on save.
    var saveInlineTags = node.querySelectorAll('strong, em, u, b, i, s, strike');
    for (var sit = 0; sit < saveInlineTags.length; sit++)
    {
        saveInlineTags[sit].removeAttribute('style');
    }

    // Restore <br> in <p> elements that have no text content so blank-line spacers
    // survive save/reload. Using textContent.trim() recurses into nested empty spans
    // (e.g. <p><span></span></p>) which the old child-node walk counted as visible.
    var allP = node.querySelectorAll('p');
    for (var pi = 0; pi < allP.length; pi++)
    {
        if (allP[pi].textContent.trim() === '')
        {
            allP[pi].innerHTML = '<br>';
        }
    }

    return node.innerHTML;
    
}

const AcceptableTag = {
    "body":true,
    "div":true,
    "p":true,
    "em":true,
    "strong":true,
    "u":true,
    "ul":true,
    "ol":true,
    "li":true,
    "br":true,
    "del":true,
    "hr":true,
    "span":true,
    "table":true,
    "th":true,
    "td":true,
    "tbody":true
}

function DOMWalker(p_node)
{
    //console.log(`${p_node.nodeType} = ${p_node.nodeName}`);

    if
    (
        AcceptableTag[p_node.nodeName.toLowerCase()] == null
    )
    {
        if(p_node.nodeName.toLowerCase() != "#text")
        {
            //console.log(`${p_node.nodeType} = ${p_node.nodeName}`);
        }
    }

    if(p_node.attributes != null)
    {
        let remove_list = [];

        for(let i = 0; i < p_node.attributes.length; i++)
        {
            let attr = p_node.attributes[i];
            const lname = attr.name.toLowerCase();
            const lvalue = attr.value.trim().toLowerCase();

            // Remove only XSS-vector attributes: event handlers (on*) and javascript: scheme values.
            // Structural attributes such as <font size="...">, <a href="...">, etc. are preserved.
            if(lname.startsWith('on') || lvalue.startsWith('javascript:'))
            {
                remove_list.push(attr.name);
                continue;
            }

            // Normalize style attribute values (color names → hex, rem font-sizes → px equivalent)
            if(lname === 'style' && attr.value.trim() !== '')
            {
                let VarRegex =/var.*\(--([a-z ]+)\)/gi;
                let array = attr.value.split(";")
                let new_array = [];
                for(let array_index = 0; array_index < array.length; array_index++)
                {
                    let att_value = array[array_index];
                    let name_value = att_value.split(":");
                    if(att_value.match(VarRegex)!= null)
                    {
                        let new_att_value = colourNameToHex(name_value[1].replace(VarRegex,"$1").trim());
                        if (new_att_value !== false)
                        {
                            new_array.push(`${name_value[0].trim()}:${new_att_value}`);
                        }
                        else
                        {
                            new_array.push(att_value);
                        }

                    }
                    else if(att_value.trim().indexOf("color")== 0)
                    {
                        if(name_value[1].trim().indexOf("#") != 0)
                        {
                            let new_att_value = colourNameToHex(name_value[1].trim());
                            if (new_att_value !== false)
                            {
                                new_array.push(`${name_value[0].trim()}:${new_att_value}`);
                            }
                            else
                            {
                                new_array.push(att_value);
                            }
                        }
                        else
                        {
                            new_array.push(att_value);
                        }
                    }
                    else if(att_value.trim().indexOf("font-size")== 0)
                    {
                        if(name_value[1].trim().endsWith("rem"))
                        {
                            new_array.push(`font-size:12px`);
                        }
                        else
                        {
                            new_array.push(att_value);
                        }
                    }
                    else
                    {
                        new_array.push(att_value);
                    }
                    
                }
                attr.value = new_array.join(";")
            }


        }

        remove_list.reverse ();
        for(let i = 0; i < remove_list.length; i++)
        {
            
            p_node.removeAttribute(remove_list[i]);
        }
    }

    for(let i = 0; i < p_node.childNodes.length; i++)
    {
        let child = p_node.childNodes[i];

        DOMWalker(child);
    }


}

function colourNameToHex(colour)
{
    var colours = {
    "aliceblue":"#f0f8ff","antiquewhite":"#faebd7","aqua":"#00ffff","aquamarine":"#7fffd4","azure":"#f0ffff",
    "beige":"#f5f5dc","bisque":"#ffe4c4","black":"#000000","blanchedalmond":"#ffebcd","blue":"#0000ff","blueviolet":"#8a2be2","brown":"#a52a2a","burlywood":"#deb887",
    "cadetblue":"#5f9ea0","chartreuse":"#7fff00","chocolate":"#d2691e","coral":"#ff7f50","cornflowerblue":"#6495ed","cornsilk":"#fff8dc","crimson":"#dc143c","cyan":"#00ffff",
    "darkblue":"#00008b","darkcyan":"#008b8b","darkgoldenrod":"#b8860b","darkgray":"#a9a9a9","darkgreen":"#006400","darkkhaki":"#bdb76b","darkmagenta":"#8b008b","darkolivegreen":"#556b2f",
    "darkorange":"#ff8c00","darkorchid":"#9932cc","darkred":"#8b0000","darksalmon":"#e9967a","darkseagreen":"#8fbc8f","darkslateblue":"#483d8b","darkslategray":"#2f4f4f","darkturquoise":"#00ced1",
    "darkviolet":"#9400d3","deeppink":"#ff1493","deepskyblue":"#00bfff","dimgray":"#696969","dodgerblue":"#1e90ff",
    "firebrick":"#b22222","floralwhite":"#fffaf0","forestgreen":"#228b22","fuchsia":"#ff00ff",
    "gainsboro":"#dcdcdc","ghostwhite":"#f8f8ff","gold":"#ffd700","goldenrod":"#daa520","gray":"#808080","green":"#008000","greenyellow":"#adff2f",
    "honeydew":"#f0fff0","hotpink":"#ff69b4",
    "indianred ":"#cd5c5c","indigo":"#4b0082","ivory":"#fffff0","khaki":"#f0e68c",
    "lavender":"#e6e6fa","lavenderblush":"#fff0f5","lawngreen":"#7cfc00","lemonchiffon":"#fffacd","lightblue":"#add8e6","lightcoral":"#f08080","lightcyan":"#e0ffff","lightgoldenrodyellow":"#fafad2",
    "lightgrey":"#d3d3d3","lightgreen":"#90ee90","lightpink":"#ffb6c1","lightsalmon":"#ffa07a","lightseagreen":"#20b2aa","lightskyblue":"#87cefa","lightslategray":"#778899","lightsteelblue":"#b0c4de",
    "lightyellow":"#ffffe0","lime":"#00ff00","limegreen":"#32cd32","linen":"#faf0e6",
    "magenta":"#ff00ff","maroon":"#800000","mediumaquamarine":"#66cdaa","mediumblue":"#0000cd","mediumorchid":"#ba55d3","mediumpurple":"#9370d8","mediumseagreen":"#3cb371","mediumslateblue":"#7b68ee",
    "mediumspringgreen":"#00fa9a","mediumturquoise":"#48d1cc","mediumvioletred":"#c71585","midnightblue":"#191970","mintcream":"#f5fffa","mistyrose":"#ffe4e1","moccasin":"#ffe4b5",
    "navajowhite":"#ffdead","navy":"#000080",
    "oldlace":"#fdf5e6","olive":"#808000","olivedrab":"#6b8e23","orange":"#ffa500","orangered":"#ff4500","orchid":"#da70d6",
    "palegoldenrod":"#eee8aa","palegreen":"#98fb98","paleturquoise":"#afeeee","palevioletred":"#d87093","papayawhip":"#ffefd5","peachpuff":"#ffdab9","peru":"#cd853f","pink":"#ffc0cb","plum":"#dda0dd","powderblue":"#b0e0e6","purple":"#800080",
    "rebeccapurple":"#663399","red":"#ff0000","rosybrown":"#bc8f8f","royalblue":"#4169e1",
    "saddlebrown":"#8b4513","salmon":"#fa8072","sandybrown":"#f4a460","seagreen":"#2e8b57","seashell":"#fff5ee","sienna":"#a0522d","silver":"#c0c0c0","skyblue":"#87ceeb","slateblue":"#6a5acd","slategray":"#708090","snow":"#fffafa","springgreen":"#00ff7f","steelblue":"#4682b4",
    "tan":"#d2b48c","teal":"#008080","thistle":"#d8bfd8","tomato":"#ff6347","turquoise":"#40e0d0",
    "violet":"#ee82ee",
    "wheat":"#f5deb3","white":"#ffffff","whitesmoke":"#f5f5f5",
    "yellow":"#ffff00","yellowgreen":"#9acd32"};

    if (typeof colours[colour.toLowerCase()] != 'undefined')
        return colours[colour.toLowerCase()];

    return false;
}


function textarea_control_strip_html_attributes2(p_value)
{
    let AttributeRegEx = /[a-zA-Z]+='[^']+'|[a-zA-Z]+=\"[^\"]+\"/gi;
    

    let PseudoTagRegex = /<\/?[a-z]:[^>]+>/gi;

    let CommentRegex = /<!--\[[^>]+>/gi;


    //let StripTrailBlankSpaceExp = /(<\/?([ ])+[^>]+>)/gi;

    let Strip5PlusBr = /<br\><br\><br\><br\>+/gi;

    let StripTrailBlankSpaceExp = /<\/?[a-zA-Z]+([ ]+)[^>]+>/gi;

    let result = p_value.replace(AttributeRegEx,"")
        .replace(PseudoTagRegex, "").
        replace(CommentRegex,"")
        .replace(StripTrailBlankSpaceExp, "")
        .replace(Strip5PlusBr,"<br>");

    return result;

}

function apply_case_narrative_editor_accessibility()
{
    const narrativeEditor = $("#case_narrative_editor");

    if(narrativeEditor.length === 0)
    {
        return;
    }

    narrativeEditor.attr("aria-labelledby", "case-narrative-heading");

    const trumbowygBox = narrativeEditor.closest(".trumbowyg-box");

    if(trumbowygBox.length > 0)
    {
        trumbowygBox.addClass("case-narrative-trumbowyg");
    }

    const trumbowygEditor = trumbowygBox.find(".trumbowyg-editor");

    if(trumbowygEditor.length > 0)
    {
        trumbowygEditor.attr(
            {
                "aria-labelledby": "case-narrative-heading",
                "role": "textbox",
                "aria-multiline": "true"
            }
        );
    }

    narrativeEditor.attr("aria-hidden", "true");
}
