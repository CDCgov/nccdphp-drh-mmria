var x = "";

function diffUsingJS() 
{
    // get the baseText and newText values from the two textboxes, and split them into lines
    var base = difflib.stringAsLines(document.getElementById("baseText").innerText);
    var newtxt = difflib.stringAsLines(document.getElementById("newText").innerText);

    // create a SequenceMatcher instance that diffs the two sets of lines
    var sm = new difflib.SequenceMatcher(base, newtxt);

    // get the opcodes from the SequenceMatcher instance
    // opcodes is a list of 3-tuples describing what changes should be made to the base text
    // in order to yield the new text
    var opcodes = sm.get_opcodes();
    var diffoutputdiv = document.getElementById("diffoutput");
    while (diffoutputdiv.firstChild) diffoutputdiv.removeChild(diffoutputdiv.firstChild);
    var contextSize = "";
    contextSize = contextSize ? contextSize : null;

    // build the diff view and add it to the current DOM
    diffoutputdiv.appendChild(diffview.buildView({
        baseTextLines: base,
        newTextLines: newtxt,
        opcodes: opcodes,
        // set the display titles for each resource
        baseTextName: "Old Value",
        newTextName: "New Value",
        contextSize: contextSize,
        viewType: 0//$("inline").checked ? 1 : 0
    }));

    // scroll down to the diff view window.
    //location = url + "#diff";
}