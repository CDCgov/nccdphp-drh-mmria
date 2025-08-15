/******************************************************
 * START properties
 *****************************************************/
var urlList = location.protocol + '//' + location.host + '/api/ui_specification/list';
var dmWidth;
var specList = [];
var modifySpecId;
var modifySpecRev;

/******************************************************
 * START logic
 *****************************************************/
grapSpecList();


/******************************************************
 * START methods
 *****************************************************/

/**
 * Implements method to add new specification and post it to the database
 * @param {String} name 
 */
function addNewSpec(name) {
  var newSpec = get_new_ui_specification(name);
  
  $.ajax({
    url: location.protocol + '//' + location.host + '/api/ui_specification',
    contentType: 'application/json; charset=utf-8',
    dataType: 'json',
    data: JSON.stringify(newSpec),
    type: "POST"
  }).done(function (response) {
      var response_obj = eval(response);
      if (response_obj.ok) {
        grapSpecList();
      }
  });
}

/**
 * Implements method to delete UI Specificatin by ID
 */
function deleteSpec() {
  $.ajax({
    url: location.protocol + '//' + location.host + '/api/ui_specification/' + modifySpecId + '?rev=' + modifySpecRev,
    contentType: 'application/json; charset=utf-8',
    dataType: 'json',
    data: '',
    type: "DELETE"
  }).done(function (response) {
    //console.log(response);
      var response_obj = eval(response);
      if (response_obj.ok) {
        //console.log(response_obj);
        grapSpecList();
      }
  });
}

/**
 * Implements method to modify specific UI Specification by id
 * Makes a call to grab spec object / modify property(s) and update object with post request
 * Rebuilds list of specifications
 * @param {String} specId 
 */
function modifySpec(modName = null, modHeight = null, modWidth = null) {
  let url = location.protocol + "//" + location.host + "/api/ui_specification/" + modifySpecId;
  $.get(url, function(data, status) {
    let currentSpecObj = data;
    if(modName) {
      currentSpecObj.name = modName;
    }
    if (modHeight) {
      currentSpecObj.dimension.height = modHeight;
    }
    if (modWidth) {
      currentSpecObj.dimension.width = modWidth;
    }
    $.ajax({
      url: location.protocol + "//" + location.host + "/api/ui_specification/" + modifySpecId,
      contentType: 'application/json; charset=utf-8',
      dataType: 'json',
      data: JSON.stringify(currentSpecObj),
      type: "POST"
    }).done(function (response) {
      var response_obj = eval(response);
      if (response_obj.ok) {
        grapSpecList();
      }
    });
  });
}

/**
 * Implements method to set global var modifySpecId and modifySpecRev used for modifySpec and deleteSpec function
 * @param {String} id 
 */
function setModifySpecId(id) {
  modifySpecId = id;
  let url = location.protocol + "//" + location.host + "/api/ui_specification/" + modifySpecId;
  $.get(url, function (data, status) {
    modifySpecRev = data._rev;
  });
}

/**
 * Implements method to get specification list stored in couch DB and build HTML structure for list
 */
function grapSpecList() {
  $.get(urlList, function(data, status) {
    let inHTML = "";
    //console.log(data);
    $.each(data, function(index, value) {
      inHTML += tplSpecTableRow(index, value);
    });
    $("#repeatSpecContainer").html(inHTML);
  });
}

/**
 * Implements method to dynamically build table row template
 * @param {Number} index 
 * @param {Object} value 
 */
function tplSpecTableRow(index, value) {
  let tpl;
  if(value._id === 'default_ui_specification') {
    tpl = `
      <tr>
        <th scope="row">${index + 1}</th>
        <td><a href="/form-designer/app.html?_id=${value._id}">${value.name}</a></td>
        <td>
          Height: ${value.dimension.height} | Width: ${value.dimension.width}
        </td>
        <td>
        </td>
      </tr>
    `;
  } else {
    tpl = `
      <tr>
        <th scope="row">${index + 1}</th>
        <td><a href="/form-designer/app.html?_id=${value._id}">${value.name}</a></td>
        <td>
          Height: ${value.dimension.height} | Width: ${value.dimension.width}
        </td>
        <td>
          <button type="button" class="btn btn-raised btn-success" data-toggle="modal" onClick="setModifySpecId('${value._id}')" data-target="#modifySpecModal">Modify</button>
          <button type="button" class="btn btn-raised btn-danger" data-toggle="modal" onClick="setModifySpecId('${value._id}')" data-target="#deleteSpecModal">Delete</button>
        </td>
      </tr>
    `;
  }
  
  return tpl;
}