
// target elements with the "draggable" class
interact('.draggable')
    .draggable({
        // enable inertial throwing
        inertia: false,
        // keep the element within the area of it's parent
        restrict: {
            restriction: "parent",
            endOnly: true,
            elementRect: { top: 0, left: 0, bottom: 1, right: 1 }
        },
        // enable autoScroll
        autoScroll: true,

        // call this function on every dragmove event
        onmove: dragMoveListener,
        // call this function on every dragend event
        onend: function (event) {
            var textEl = event.target.querySelector('p');

            textEl && (textEl.textContent =
                'moved a distance of '
                + (Math.sqrt(Math.pow(event.pageX - event.x0, 2) +
                    Math.pow(event.pageY - event.y0, 2) | 0))
                    .toFixed(2) + 'px');

        }
    });

function dragMoveListener(event) {
    if (globalDSelected === undefined || globalDSelected.length === 0) {
      // User should explicity select the move/drag tool in wysiwyg
      return;
    }
    // Stop drag-n-select when using drag-n-drop
    fdDragSelect.stop();

    globalDSelected.forEach(function(element) {

      var target = element,
        // keep the dragged position in the data-x/data-y attributes
        x = (parseFloat(target.getAttribute("data-x")) || 0) + event.dx,
        y = (parseFloat(target.getAttribute("data-y")) || 0) + event.dy;

      // console.log(target);

      // translate the element
      target.style.webkitTransform = target.style.transform =
        "translate(" + x + "px, " + y + "px)";

      // update the posiion attributes
      target.setAttribute("data-x", x);
      target.setAttribute("data-y", y);
    });

    // Take a quickSnap when done moving
    if (event.button == 0) {
      formDesigner.fdObjectHandler.snapShot();
      formDesigner.fdObjectHandler.quickSnap();
    }

    fdDragSelect.start();
}

function multiDragMove() {
  globalDSelected.forEach(function(element) {
    var target = element,
      // keep the dragged position in the data-x/data-y attributes
      x = (parseFloat(target.getAttribute("data-x")) || 0) + event.dx,
      y = (parseFloat(target.getAttribute("data-y")) || 0) + event.dy;

    // console.log(target);

    // translate the element
    target.style.webkitTransform = target.style.transform =
      "translate(" + x + "px, " + y + "px)";

    // update the posiion attributes
    target.setAttribute("data-x", x);
    target.setAttribute("data-y", y);
  });
}

// this is used later in the resizing
window.dragMoveListener = dragMoveListener;

// enable draggables to be dropped into this
interact('.dropzone').dropzone({
    // only accept elements matching this CSS selector
    accept: '#yes-drop',
    // Require a 75% element overlap for a drop to be possible
    overlap: 0.75,

    // listen for drop related events:

    ondropactivate: function (event) {
        // add active dropzone feedback
        event.target.classList.add('drop-active');
    },
    ondragenter: function (event) {
        var draggableElement = event.relatedTarget,
            dropzoneElement = event.target;

        // feedback the possibility of a drop
        dropzoneElement.classList.add('drop-target');
        draggableElement.classList.add('can-drop');
        draggableElement.textContent = 'Dragged in';
    },
    ondragleave: function (event) {
        // remove the drop feedback style
        event.target.classList.remove('drop-target');
        event.relatedTarget.classList.remove('can-drop');
        event.relatedTarget.textContent = 'Dragged out';
    },
    ondrop: function (event) {
        event.relatedTarget.textContent = 'Dropped';
    },
    ondropdeactivate: function (event) {
        // remove active dropzone feedback
        event.target.classList.remove('drop-active');
        event.target.classList.remove('drop-target');
    }
});

interact(".drag-drop")
  .draggable({
    inertia: false,
    restrict: {
      restriction: "parent",
      endOnly: true,
      elementRect: { top: 0, left: 0, bottom: 1, right: 1 }
    },
    autoScroll: true,
    // dragMoveListener from the dragging demo above
    onmove: dragMoveListener
  })
  .resizable({
    // resize from all edges and corners
    edges: { left: true, right: true, bottom: true, top: true },

    // keep the edges inside the parent
    restrictEdges: {
      outer: "parent",
      endOnly: true
    },

    // minimum size
    restrictSize: {
      min: { width: 100, height: 50 }
    },

    inertia: false
  })
  .on("resizemove", function(event) {
    // Stop drag-n-select when using drag-n-drop (re-size)
    fdDragSelect.stop();
    
    var target = event.target,
      x = parseFloat(target.getAttribute("data-x")) || 0,
      y = parseFloat(target.getAttribute("data-y")) || 0;

    // update the element's style
    target.style.width = event.rect.width + "px";
    target.style.height = event.rect.height + "px";

    // translate when resizing from top or left edges
    x += event.deltaRect.left;
    y += event.deltaRect.top;

    target.style.webkitTransform = target.style.transform =
      "translate(" + x + "px," + y + "px)";

    target.setAttribute("data-x", x);
    target.setAttribute("data-y", y);
    //target.textContent = Math.round(event.rect.width) + "\u00D7" + Math.round(event.rect.height);

    // Take a quickSnap when done re-sizing
    if(event.button == 0) {
      formDesigner.fdObjectHandler.snapShot();
      formDesigner.fdObjectHandler.quickSnap();
    }
  });