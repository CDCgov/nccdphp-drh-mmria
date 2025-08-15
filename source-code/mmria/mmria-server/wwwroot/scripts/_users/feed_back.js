
var change_detection_map = {};
var interval_ids = null;

function monitor_changes()
{
  var url =  location.protocol + '//' + location.host + "/api/current_edit";

  $.ajax({
    url: url,
    method: 'GET'
  }).done(function(response) {
    // this will be run when the AJAX request succeeds

    var initial_setup = false;

    for(var i = 0; i < response.length; i++)
    {
      var item = response[i];


      if(item.edit_type=="json")
      {

        if(!change_detection_map["json"])
        {
          change_detection_map["json"] = item;
          initial_setup = true;
        }
        else if(change_detection_map["json"].id != item.id)
        {

          change_detection_map["json"] = item;
          console.log('metadata item changed');
          console.log(new Date().toISOString());
          console.log(item);

        }
      }
      else if(!change_detection_map[item.metadata])
      {
        change_detection_map[item.metadata] = item;
        initial_setup = true;
      }
      else if(change_detection_map[item.metadata].id != item.id)
      {

        change_detection_map[item.metadata].id = item.id;
        console.log('javascript item changed');
        console.log(new Date().toISOString());
        console.log(item);

        if(item.metadata=="scripts/profile.js")
        {
          var script_name =  location.protocol + '//' + location.host + '/' + item.metadata;
          reload_js(script_name, function(){
            profile.initialize_profile();
          });
        }
        else if(item.metadata=="scripts/_users/navigation_renderer.js")
        {
          var script_name =  location.protocol + '//' + location.host + '/' + item.metadata;
          reload_js(script_name, function(){
            document.getElementById('navigation_id').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
            window.onhashchange ({ isTrusted: true, newURL : window.location.href });
          });
        }
        else if
        (
          item.metadata=="scripts/_users/user_renderer.js" ||
          item.metadata=="scripts/_users/futon.browse.js" ||
          item.metadata=="scripts/_users/jquery.couch.js" ||
          item.metadata=="scripts/_users/index.js"
        )
        {
          var script_name =  location.protocol + '//' + location.host + '/' + item.metadata;
          reload_js(script_name, function(){
            document.getElementById('form_content_id').innerHTML = user_render(g_ui, "", g_ui).join("");
          });
        }
        else if(item.metadata=="styles/mmria.css")
        {
          var script_name =  location.protocol + '//' + location.host + '/' + item.metadata;
          reload_css(script_name, function(){
            //document.getElementById('form_content_id').innerHTML = user_render(g_metadata, "", g_ui).join("");
            return;
          });
        }/*
        else if(item.metadata=="scripts/_users/index.js")
        {
          var script_name =  location.protocol + '//' + location.host + '/' + item.metadata;
          reload_js(script_name, function(){
            //load_metadata();
            return null;
          });
        }*/



      }
    }

    if(initial_setup)
    {
      console.log('initial setup');
      console.log(new Date().toISOString());
      console.log(change_detection_map);
    }

  }).fail(function(response) {

    window.clearInterval(interval_id);
    console.log("failed:", response);}
  );
}

interval_id = window.setInterval(monitor_changes, 3000);
