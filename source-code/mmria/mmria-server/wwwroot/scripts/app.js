/*
Copyright (c) 2015 The Polymer Project Authors. All rights reserved.
This code may only be used under the BSD style license found at http://polymer.github.io/LICENSE.txt
The complete set of authors may be found at http://polymer.github.io/AUTHORS.txt
The complete set of contributors may be found at http://polymer.github.io/CONTRIBUTORS.txt
Code distributed by Google as part of the polymer project is also
subject to an additional IP rights grant found at http://polymer.github.io/PATENTS.txt
*/

//https://easyusedev.wordpress.com/2015/11/06/dynamically-loading-polymer-elements-on-the-fly/

(function(document) {
  'use strict';

	// Grab a reference to our auto-binding template
	// and give it some initial binding values
	// Learn more about auto-binding templates at http://goo.gl/Dx1u2g
	var app = Polymer.dom(document).querySelector('#app');
	
  
  // profile --- start
  window.addEventListener('profile_login_changed', function(e) 
  {
    var profile = app.querySelector('#mmrds_profile');
	
        profile.isLoggedIn = e.detail.is_logged_in;
		profile.auth_session = e.detail.auth_session;
		profile.user_name = e.detail.user_name;
		profile.user_roles = e.detail.user_roles;
		if(profile.isLoggedIn)
		{
			var minutes_14 = 14;
			var current_date_time = new Date();
			var new_date_time = new Date(current_date_time.getTime() + minutes_14 * 60000);
			
			document.cookie = "AuthSession=" + profile.auth_session + "; expires=" + new_date_time.toGMTString() + "; path=/";
			app.route.path = '/summary';
		}
		else
		{
			document.cookie = "AuthSession=" + profile.auth_session + "; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";		
			app.route.path = '/home';
		}
		
		
		
		var abstractor_menu = app.querySelector('#abstractor_menu')
		abstractor_menu.style.display = "block";
			

		
    });/**/
  
  // profile --- end
  
	window.addEventListener('onSelectedRecordIdChanged', function(e)
		{
			if
			(
				//(app.IndentifierData && app.IndentifierData.id && app.IndentifierData.id.length > 0) ||
				(e && e.detail && e.detail.length > 0)
			)
			{
				app.$.abstractor_selected_item_menu.style.display = 'block';
				app.$.tool_bar_hud_id.style.display = 'block';
			}
			else
			{
				app.$.abstractor_selected_item_menu.style.display = 'none';
				app.$.tool_bar_hud_id.style.display = 'none';
			}

			/*
			if(e && e.currentTarget)
			{
				console.log('onSelectedRecordIdChanged' + e.currentTarget.dataValue)
				this.selected_record_id = e.currentTarget.dataValue;
			}*/
		}
	);
  
  // Sets app default base URL
  app.baseUrl = '/';
  if (window.location.port === '') {  // if production
    // Uncomment app.baseURL below and
    // set app.baseURL to '/your-pathname/' if running from folder in production
    // app.baseUrl = '/polymer-starter-kit/';
  }

  // Listen for template bound event to know when bindings
  // have resolved and content has been stamped to the page
  app.addEventListener('dom-change', function() 
  {
    console.log('Our app is ready to rock!');
  });

  // See https://github.com/Polymer/polymer/issues/1381
  window.addEventListener('WebComponentsReady', function() 
  {
    // imports are loaded and elements have been registered
	//document.querySelector("#master_record_call").generateRequest();
  });

  // Main area's paper-scroll-header-panel custom condensing transformation of
  // the appName in the middle-container and the bottom title in the bottom-container.
  // The appName is moved to top and shrunk on condensing. The bottom sub title
  // is shrunk to nothing on condensing.
  window.addEventListener('paper-header-transform', function(e) {
    var appName = app.querySelector('#mainToolbar .app-name');
    var middleContainer = app.querySelector('#mainToolbar .middle-container');
    var bottomContainer = app.querySelector('#mainToolbar .bottom-container');
    var detail = e.detail;
    var heightDiff = detail.height - detail.condensedHeight;
    var yRatio = Math.min(1, detail.y / heightDiff);
    // appName max size when condensed. The smaller the number the smaller the condensed size.
    var maxMiddleScale = 0.50;
    var auxHeight = heightDiff - detail.y;
    var auxScale = heightDiff / (1 - maxMiddleScale);
    var scaleMiddle = Math.max(maxMiddleScale, auxHeight / auxScale + maxMiddleScale);
    var scaleBottom = 1 - yRatio;

    // Move/translate middleContainer
    Polymer.Base.transform('translate3d(0,' + yRatio * 100 + '%,0)', middleContainer);

    // Scale bottomContainer and bottom sub title to nothing and back
    Polymer.Base.transform('scale(' + scaleBottom + ') translateZ(0)', bottomContainer);

    // Scale middleContainer appName
    Polymer.Base.transform('scale(' + scaleMiddle + ') translateZ(0)', appName);
  });

  // Scroll page to top and expand header
  app.scrollPageToTop = function() {
    app.$.headerPanelMain.scrollToTop(true);
  };

  app.closeDrawer = function() {
    app.$.paperDrawerPanel.closeDrawer();
  };
  



})(document);
