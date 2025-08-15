## Developing using NPM

**Before you start the project in debug mode:**
`cd` into the `mmria-server` directory
_if running for the first time_ run `npm install`
run `npm start`

This runs a webpack build which will watch for changes in the `/Components` directory. If a change is detected on save it will rebuild the components and place the build in the `/wwwroot/dist` directory. _NOTE: you will still need to refresh the browser after a change is made_
