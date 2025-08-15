

//https://www.javascripttutorial.net/web-apis/javascript-drag-and-drop/
/*

event on drag element

    dragstart
    drag
    dragend

drop target events
    dragenter
    dragover
    dragleave or drop

*/

window.onload = main;

const selected = new Set();
let is_dragging = false;

let drag_start_position = null;


function main()
{

    //console.log("main");

    const body = document.body;

    
    //body.addEventListener("mousedown", body_mousedown);
    //body.addEventListener("mousemove", mousemove);
    //body.addEventListener("mouseup", body_mouseup);

    body.addEventListener("drop", body_mouseup);
    


    const el = document.getElementById("id1");
    
    el.style.position = "absolute";
    /*
    el.addEventListener("mouseenter", mouseenter);
    el.addEventListener("mouseleave", mouseleave);
    el.addEventListener("mousedown", mousedown);
    el.addEventListener("mouseup", mouseup);
    */
    //el.addEventListener("drag", drag);
    el.addEventListener("dragstart", drag_start);
    el.addEventListener("dragend", drag_end);


    const el2 = document.getElementById("box");
    //el2.addEventListener("drag", drag);
    el2.addEventListener("dragstart", drag_start);
    el2.addEventListener("dragend", drag_end);

}



function body_mousedown(e)
{
    //is_dragging = true;
    //drag_start_position = { x: e.clientX, y:e.clientY };
}

function body_mouseup(e)
{
    is_dragging = false;
    drag_start_position = null;
}
function mousemove(e)
{
    
    if(is_dragging)
    {
        
        const x = e.clientX - drag_start_position.x;
        const y = e.clientY - drag_start_position.y;

        for (let it = selected.values(), item= null; item=it.next().value; ) 
        {            
            let l = 0;
            let t = 0;

            if(item.style.left)
                l = parseFloat(item.style.left.replace("px", ""));
      
            if(item.style.top)
                t = parseFloat(item.style.top.replace("px", ""));

            item.style.left += (l + x );
            item.style.top += (t + y);
        }
    }
}

function mouseenter(e)
{
    e.currentTarget.classList.add("mouseenter");
}


function mouseleave(e)
{
    e.currentTarget.classList.remove("mouseenter");
}


function mousedown(e)
{
    e.currentTarget.classList.add("mousedown");
    selected.add(e.currentTarget);
}


function mouseup(e)
{
    const item = e.currentTarget;
    item.classList.remove("mousedown");
    if(selected.has(item))
        selected.delete(item);
}

function drag(e)
{
    console.log("drag");
    //console.log(e);
    //e.currentTarget.classList.remove("mousedown");

    
}

function drag_start(e)
{
    console.log("drag start");

    if(!is_dragging)
    {
        is_dragging = true;
        drag_start_position = { x: e.clientX, y:e.clientY };

        selected.add(e.currentTarget);
        //console.log(e);
        //e.currentTarget.classList.remove("mousedown");
    }

    
}

function drag_end(e)
{
    console.log("drag end");

    is_dragging = false;
    
    
    const x = e.clientX - drag_start_position.x;
    const y = e.clientY - drag_start_position.y;


    for (let it = selected.values(), item= null; item=it.next().value; ) 
    {            
        let l = 0;
        let t = 0;

        if(item.style.left)
            l = parseFloat(item.style.left.replace("px", ""));
  
        if(item.style.top)
            t = parseFloat(item.style.top.replace("px", ""));

        item.style.left += parseFloat(l + x ) + "px";
        item.style.top += parseFloat(t + y) + "px";

        selected.delete(e.currentTarget);
    }


    //drag_start_position = { x:x, y:y };

    
}