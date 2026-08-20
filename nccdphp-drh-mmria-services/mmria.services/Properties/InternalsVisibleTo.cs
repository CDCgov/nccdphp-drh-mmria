using System.Runtime.CompilerServices;

// Story 29.8: expose internal types (VitalImportCaseWriter) to the test project.
[assembly: InternalsVisibleTo("mmria-server.tests")]
