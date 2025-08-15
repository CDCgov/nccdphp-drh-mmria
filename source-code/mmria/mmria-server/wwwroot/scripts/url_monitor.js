var url_monitor = {
get_url_state: function(url)
{
  var result = null;
  var form = null;
  var selected_id = null;
  var selected_child_id = null;

  var url_array = url.split('#');
  if(url_array.length > 1)
  {
    var trimmed_string = url_array[1].replace(/^\/+|\/+$/g, '');

    var path_array = trimmed_string.split('/');

    if(path_array.length > 0)
    {
      form = path_array[0];
      //console.log("selected form: " + form);
    }

    if(path_array.length > 1)
    {
      selected_id = path_array[1];
      //console.log("selected id: " + selected_id);
    }

    if(path_array.length > 2)
    {
      selected_child_id = path_array[2];
      //console.log("selected child id: " + selected_child_id);
    }

    result = {
      selected_form_name: form,
      "selected_id": selected_id,
      "selected_child_id": selected_child_id,
      "path_array" : path_array
    };
  }
  else {

    result = {
      selected_form_name: null,
      "selected_id": null,
      "selected_child_id": null,
      "path_array" : []
    };
  }

  return result;
},
url_has_changed: function(e)
{
  /*
  e = HashChangeEvent
  {
    isTrusted: true,
    oldURL: "http://localhost:12345/react-test/#/",
    newURL: "http://localhost:12345/rea
  }*/
  if(e.isTrusted)
  {
    var url_state = this.get_url_state(e.newURL);


    if(
      url_state.selected_id != null &&
      url_state.selected_id != this.state.selected_id
    )
    {
      if(this.state.record_data.length > 0)
      {
        var data_access = new Data_Access("http://localhost:5984");
        data_access.get_data(url_state.selected_id,
          function(doc)
          {
            this.load_record(doc);
            this.setState(url_state);
          }.bind(this)
        );
      }
      else
      {
        this.setState(url_state);
      }
    }
    else
    {
      this.setState(url_state);
    }
  }
  else
  {
    // do nothing for now
  }
}
};
