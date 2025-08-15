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
};
let fdObject = {};

// Declare Drag Select object (assign after elements are in DOM)
let fdDragSelect;