
// enable draggables to be dropped into this
interact('.dropzone').dropzone({
    // only accept elements matching this CSS selector
    accept: '.yes-drop',
    // Require a 75% element overlap for a drop to be possible
    overlap: 0.75,

    // listen for drop related events:

    ondropactivate: function (event) {
        // add active dropzone feedback
        event.target.classList.add('drop-active');

        // Stop Multi-select functionality when user may be moving forward with drag-n-drop
        fdDMS.stop();
    },
    ondragenter: function (event) {
        var draggableElement = event.relatedTarget,
            dropzoneElement = event.target;

        // feedback the possibility of a drop
        dropzoneElement.classList.add('drop-target');
        // draggableElement.classList.add('can-drop');
        // draggableElement.textContent = 'Dragged in';
    },
    ondragleave: function (event) {
        // remove the drop feedback style
        event.target.classList.remove('drop-target');
        // event.relatedTarget.classList.remove('can-drop');
        // event.relatedTarget.textContent = 'Dragged out';
    },
    ondrop: function (event) {
        // event.relatedTarget.textContent = 'Dropped';

        event.relatedTarget.setAttribute('data-t', $('#' + event.relatedTarget.id).position().top);
        event.relatedTarget.setAttribute("data-l", $("#" + event.relatedTarget.id).position().left);

        var pc;
        var fid = event.relatedTarget.id;
        if(event.relatedTarget.id.includes('-control')) {
            pc = 'control';
            var fid = event.relatedTarget.id.split("-")[0];
        } else {
            pc = 'prompt';
        }
        
        createOrUpdateFormElements(
            activeForm, 
            fid, 
            $("#" + event.relatedTarget.id).position().top, 
            $("#" + event.relatedTarget.id).position().left, 
            event.relatedTarget.getAttribute('data-w'), 
            event.relatedTarget.getAttribute('data-h'), 
            pc
        );
        
        writeFormSpecs(true);
        writeActionSpecs("in", event.relatedTarget.id);
    },
    ondropdeactivate: function (event) {
        // remove active dropzone feedback
        event.target.classList.remove('drop-active');
        event.target.classList.remove('drop-target');
    }
});

interact('.drag-drop')
    .draggable({
        inertia: false,
        restrict: {
            restriction: "parent",
            endOnly: true,
            elementRect: { top: 0, left: 0, bottom: 1, right: 1 }
        },
        autoScroll: true,
        // dragMoveListener from the dragging demo above
        onmove: dragMoveListener,
    })
    .resizable({
        // resize from all edges and corners
        edges: { left: true, right: true, bottom: true, top: true },

        // keep the edges inside the parent
        restrictEdges: {
            outer: 'parent',
            endOnly: true,
        },

        // minimum size
        restrictSize: {
            min: { width: 100, height: 20 },
        },

        inertia: false,
    })
    .on('resizemove', function (event) {

        fdDMS.stop();

        var target = event.target,
            x = (parseFloat(target.getAttribute('data-x')) || 0),
            y = (parseFloat(target.getAttribute('data-y')) || 0);

        // update the element's style
        target.style.width = event.rect.width + 'px';
        target.style.height = event.rect.height + 'px';

        // translate when resizing from top or left edges
        x += event.deltaRect.left;
        y += event.deltaRect.top;

        target.style.webkitTransform = target.style.transform =
            'translate(' + x + 'px,' + y + 'px)';

        target.setAttribute('data-x', x);
        target.setAttribute('data-y', y);
        // target.textContent = Math.round(event.rect.width) + '\u00D7' + Math.round(event.rect.height);

        if (event.button == 0) {
            target.setAttribute('data-w', event.rect.width);
            target.setAttribute('data-h', event.rect.height);

            var pc;
            var fid = target.id;
            if (target.id.includes('-control')) {
                pc = 'control';
                fid = target.id.split('-')[0];
            } else {
                pc = 'prompt';
            }

            createOrUpdateFormElements(
                activeForm,
                fid,
                parseFloat(target.getAttribute('data-t')),
                parseFloat(target.getAttribute('data-l')),
                event.rect.width,
                event.rect.height,
                pc
            );

            writeFormSpecs(true);
            writeActionSpecs('in', target.id);
        }
    });