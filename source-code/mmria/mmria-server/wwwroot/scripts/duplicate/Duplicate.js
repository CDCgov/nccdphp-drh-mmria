/*
jQuery plugin to check if current window is duplicate window
BASED ON https://stackoverflow.com/a/45717724/223752
*/

(function ($) {
    $.fn.DuplicateWindow = function () {
        var localStorageTimeout = (5) * 1000; // 15,000 milliseconds = 15 seconds.
        var localStorageResetInterval = (1/2) * 1000; // 10,000 milliseconds = 10 seconds.
        var localStorageTabKey = 'mmria-application-browser-tab';
        var sessionStorageGuidKey = 'browser-tab-guid';

        var ItemType = {
            Session: 1,
            Local: 2
        };

        function getSessionValue() {
            return sessionStorage.getItem(sessionStorageGuidKey) || "";
        }

        function setSessionValue(value) {
            sessionStorage.setItem(sessionStorageGuidKey, value || "");
        }

        function getLocalValue() {
            return localStorage.getItem(localStorageTabKey) || "";
        }

        function setLocalValue(value) {
            if (value == null || value === "") {
                localStorage.removeItem(localStorageTabKey);
                return;
            }

            localStorage.setItem(localStorageTabKey, value);
        }

        function GetItem(itemtype) {
            var val = "";
            switch (itemtype) {
                case ItemType.Session:
                    val = getSessionValue();
                    break;
                case ItemType.Local:
                    val = getLocalValue();
                    if (val == undefined)
                        val = "";
                    break;
            }
            return val;
        }

        function SetItem(itemtype, val) {
            switch (itemtype) {
                case ItemType.Session:
                    setSessionValue(val);
                    break;
                case ItemType.Local:
                    setLocalValue(val);
                    break;
            }
        }

        function createGUID() {
            // Use cryptographically secure random number generator
            const array = new Uint8Array(16);
            crypto.getRandomValues(array);
            
            // Convert to hex string with proper GUID format
            const hex = Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
            return hex.substr(0, 8) + '-' + hex.substr(8, 4) + '-' + hex.substr(12, 4) + '-' + hex.substr(16, 4) + '-' + hex.substr(20, 12);
        }

        /**
         * Compare our tab identifier associated with this session (particular tab)
         * with that of the shared localStorage record for this browser.
         * This browser tab is good if any of the following are true:
         * 1.  There is no shared tab record yet (first browser tab).
         * 2.  The sessionStorage Guid matches the shared tab Guid. Same tab, refreshed.
         * 3.  The shared tab timeout period has ended.
         *
         * If our current session is the correct active one, an interval will continue
         * to re-insert the shared tab value with an updated timestamp.
         *
         * Another thing, that should be done (so you can open a tab within 15 seconds of closing it) would be to do the following (or hook onto an existing onunload method):
         *      window.onunload = () => {
                        localStorage.removeItem(localStorageTabKey);
              };
         */
        function TestIfDuplicate() {
            //console.log("In testTab");
            var sessionGuid = GetItem(ItemType.Session) || createGUID();
            SetItem(ItemType.Session, sessionGuid);

            var val = GetItem(ItemType.Local);
            var tabObj = (val == "" ? null : JSON.parse(val)) || null;
            //console.log(val);
            //console.log(sessionGuid);
            //console.log(tabObj);

            // If no or stale tab object, our session is the winner.  If the guid matches, ours is still the winner
            if (tabObj === null || (tabObj.timestamp < (new Date().getTime() - localStorageTimeout)) || tabObj.guid === sessionGuid) {
                function setTabObj() {
                    //console.log("In setTabObj");
                    var newTabObj = {
                        guid: sessionGuid,
                        timestamp: new Date().getTime()
                    };
                    SetItem(ItemType.Local, JSON.stringify(newTabObj));
                }
                setTabObj();
                setInterval(setTabObj, localStorageResetInterval);//refresh timestamp in localStorage
                return false;
            } else {
                // An active tab is already open that does not match our session guid.
                return true;
            }
        }

        window.IsDuplicate = function () {
            var duplicate = TestIfDuplicate();
            //console.log("Is Duplicate: "+ duplicate);
            return duplicate;
        };

        $(window).on("beforeunload", function () {
            if (TestIfDuplicate() == false) {
                SetItem(ItemType.Local, "");
            }
        })
    }
    $(window).DuplicateWindow();
}(jQuery));
