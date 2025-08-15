
## Maternal Mortality Review Information Application

The Maternal Mortality Review Information Application, or  **MMRIA**, is a software tool created, developed, and maintained by CDC to collect, store, analyze, surveillance and summarize information relevant to maternal deaths. The MMRIA system as it is now is a newer, more improved version of the older system known as Maternal Mortality Review and Data System (MMRDS).

The MMRIA system serves 2 purposes:

1. First, to provide complete, detailed, and organized medical and social information that can be used by medical review committees to investigate individual maternal deaths.
2. Second to provide a standardized cumulative database for future research and analysis on maternal mortality.


## Texas A&M Geoservices Geocoding APIs

This application uses the Texas A&M Geoservices Geocoding APIs <a href='http://geoservices.tamu.edu' target='_new'>http://geoservices.tamu.edu</a>


## Environment

+ [ASP.Net Core](https://docs.microsoft.com/en-us/aspnet/core/)
+ [Apache CouchDB](https://couchdb.apache.org/)


## Technology Stack

+ C#
+ Razor Syntax
+ HTML
+ CSS3
+ JavaScript, jQuery


## Running Debug in Terminal to run local version of MMRIA

1. Make sure root folder is: `MMRDS/source-code/mmria`
2. Run Debug by going to top menu "Go" then clicking "Debug" OR click `f5`


## Updating meta-data values for Case Forms

1. Go to the [meta-data editor](http://localhost:5000/editor)
2. Locate and edit data you want to update (Hint: It helps to `ctrl+F` and find it)
3. Once updated, click *save metadata* button at the top twice
  - Ensure value in `_rev` increments for both times you clicked
4. Go to [meta-data version manager](http://localhost:5000/version-manager)
5. Select the version you want under *load version* drop down
6. Click *get version* under *load version*
7. Click *load latest metadata* under *edit attachments*
8. Click *#1 attach metadata* under *edit attachments*
9. If values prefill in form controls under *edit attachments*, then Done


## Case Form Designing

### Changing/Updating case forms

1. Log into user with **Form Designer** role (`sysadmin` is current one)
2. On home page dashboard, under Form Designer Options, click on `Open form designer`
3. Select `default_ui_specification` from dropdown on left sidebar
4. Make necessary changes, once finished, click `Save Specification` on top navigation

### Attaching `ui_specifications`

To see your latest UI changes, this is required. See steps below:

1. Should be already logged in but if not... Log into user with **Form Designer** role (`sysadmin` is current one)
2. On home page dashboard, under Form Designer Options, click on `Open metadata version manager`
3. Under `load version`, click on dropdown and select latest version (usually last on list)
4. Under `edit version` select `draft`
5. Under `edit attachments` click on `load latest metadata` button... Then wait a few seconds
6. Under `edit attachments` click on #4 `ui_specifications` button to attach latest changes

