using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Linq;
using mmria.common.functional;
using mmria.server;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server.Controllers;

public sealed class Audit_View
{
    public Audit_View(){}
    public string id {get;set;} 
    public string user  {get;set;} = "all"; 
    public string search_text  {get;set;} = "all";
    public bool showAll {get;set;} = false;
    public mmria.common.model.couchdb.case_view_sortable_item cv {get;set;}
    public List<mmria.common.model.couchdb.Change_Stack> ls {get;set;}

    public int page_size {get;set;} 
    public int page {get;set;} 
    public int total {get;set;} 
}

public sealed class Audit_Detail_View
{
    public Audit_Detail_View(){}
    public string id {get;set;} 
    public string change_id  {get;set;}
    public int change_item  {get;set;}
    public bool showAll {get;set;} = false;
    public mmria.common.model.couchdb.case_view_sortable_item cv {get;set;}
    public mmria.common.model.couchdb.Change_Stack cs {get;set;}

    public mmria.common.metadata.node MetadataNode {get;set;}

    public Dictionary<string, string> value_to_display {get;set;}
    public Dictionary<string, string> display_to_value {get;set;}
            

}

[Authorize(Roles = "abstractor")]
public sealed class _auditController : Controller
{

    public struct Change_Stack_Result_Struct
    {
        public mmria.common.model.couchdb.Change_Stack[] docs;
    }

    struct Selector_Struc
    {
        //public System.Dynamic.ExpandoObject selector;
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> selector;
        public string[] fields;

        public string use_index;

        public int limit;
    }


    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private Dictionary<string, mmria.common.metadata.value_node[]> lookup;
    public _auditController
    (
        IHttpContextAccessor httpContextAccessor,
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    (string url, string post) get_find_url(string p_id)
    {
        var selector_struc = new Selector_Struc();
        selector_struc.selector = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        selector_struc.limit = 1_000_000;
        selector_struc.selector.Add("case_id", new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        selector_struc.selector["case_id"].Add("$eq", p_id);
        selector_struc.use_index = "case-id-date-last-updated-index";

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string selector_struc_string = Newtonsoft.Json.JsonConvert.SerializeObject(selector_struc, settings);

        string result = $"{db_config.url}/{db_config.prefix}audit/_find";
        return (result, selector_struc_string);
    }

    [Route("_audit/{p_id}/{page?}")]
    public async Task<IActionResult> Index(System.Threading.CancellationToken cancellationToken, string p_id, int page = -1, string user = "all", string search_text = "all", bool showAll = false)
    {

        var case_view_request_string = $"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/by_id?key=\"{p_id}\"";

        var case_view_curl = new cURL("GET", null, case_view_request_string, null, db_config.user_name, db_config.user_value);
        string responseFromServer = await case_view_curl.executeAsync();

        cancellationToken.ThrowIfCancellationRequested();

        var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);


        mmria.common.model.couchdb.case_view_sortable_item case_view_item =
            case_view_response.rows.Where(i => i.id == p_id).FirstOrDefault().value;


        //var request_string = $"{configuration["mmria_settings:couchdb_url}/{configuration["mmria_settings:db_prefix}audit/_all_docs?include_docs=true";
        var (request_string, post_data) = get_find_url(p_id);
        var audit_view_curl = new cURL("POST", null, request_string, post_data, db_config.user_name, db_config.user_value);
        responseFromServer = await audit_view_curl.executeAsync();

        cancellationToken.ThrowIfCancellationRequested();

        var view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<Change_Stack_Result_Struct>(responseFromServer);

        List<mmria.common.model.couchdb.Change_Stack> result = new();

        foreach (var item in view_response.docs)
        {

            for (var i = 0; i < item.items.Count; i++)
            {
                item.items[i].temp_index = i;
            }

            for (var subitem_index = 0; subitem_index < item.items.Count; subitem_index++)
            {
                var subitem = item.items[subitem_index];



            }

            item.items.Sort(new Change_Stack_Item_DescendingDate());

            if (showAll)
            {
                result.Add(DebounceDateTimeField(item));
            }
            else if (item.items.Count > 0 && item.case_id == p_id)
            {

                result.Add(DebounceDateTimeField(item));
            }
        }

        const int page_size = 50;

        result.Sort(new Change_Stack_DescendingDate());
        return View
        (
            new Audit_View()
            {
                id = p_id,
                user = user,
                search_text = search_text,
                showAll = showAll,
                cv = case_view_item,
                ls = page == -1 ? result : result.Skip((page - 1) * page_size).Take(page_size).ToList(),
                page_size = page_size,
                page = page,
                total = result.Count
            });
    }

    [Route("_audit/{p_id}/detail/{change_id}/{change_item}")]
    public async Task<IActionResult> MoreDetail(System.Threading.CancellationToken cancellationToken, string p_id, string change_id, int change_item)
    {

        var case_view_request_string = $"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/by_id?key=\"{p_id}\"";

        var case_view_curl = new cURL("GET", null, case_view_request_string, null, db_config.user_name, db_config.user_value);
        string responseFromServer = await case_view_curl.executeAsync();

        cancellationToken.ThrowIfCancellationRequested();

        var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);


        mmria.common.model.couchdb.case_view_sortable_item case_view_item =
            case_view_response.rows.Where(i => i.id == p_id).FirstOrDefault().value;


        //var request_string = $"{configuration["mmria_settings:couchdb_url}/{configuration["mmria_settings:db_prefix}audit/_all_docs?include_docs=true";
        var request_string = $"{db_config.url}/{db_config.prefix}audit/{change_id}";
        var audit_view_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
        responseFromServer = await audit_view_curl.executeAsync();

        cancellationToken.ThrowIfCancellationRequested();


        var cs = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.Change_Stack>(responseFromServer);


        for (var i = 0; i < cs.items.Count; i++)
        {
            cs.items[i].temp_index = i;
        }

        string metadata_url = $"{db_config.url}/metadata/version_specification-{cs.metadata_version}/metadata";

        cURL metadata_curl = new cURL("GET", null, metadata_url, null, null, null);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());

        this.lookup = get_look_up(metadata);
        var node = get_metadata_node(metadata, cs.items[change_item].dictionary_path.Trim().TrimStart('/'));
        if (node == null)
        {
            return View(new Audit_Detail_View()
            {
                id = p_id,
                change_id = change_id,
                cv = case_view_item,
                cs = cs,
                change_item = change_item,
                MetadataNode = metadata.AsNode()


            });
        }
        else
        {
            var x = convert(node);

            return View(new Audit_Detail_View()
            {
                id = p_id,
                change_id = change_id,
                cv = case_view_item,
                cs = cs,
                change_item = change_item,
                MetadataNode = node,
                value_to_display = x.value_to_display,
                display_to_value = x.display_to_value

            });
        }
    }
    public sealed class Change_Stack_DescendingDate : IComparer<mmria.common.model.couchdb.Change_Stack>
    {
        public int Compare(mmria.common.model.couchdb.Change_Stack x, mmria.common.model.couchdb.Change_Stack y)
        {
            // Compare x and y in reverse order.
            return y.date_created.Value.CompareTo(x.date_created.Value);
        }
    }

    public sealed class Change_Stack_Item_DescendingDate : IComparer<mmria.common.model.couchdb.Change_Stack_Item>
    {
        public int Compare(mmria.common.model.couchdb.Change_Stack_Item x, mmria.common.model.couchdb.Change_Stack_Item y)
        {
            // Compare x and y in reverse order.
            return y.date_created.Value.CompareTo(x.date_created.Value);
        }
    }

    public sealed class Metadata_Node
    {
        public Metadata_Node() { }
        public bool is_multiform { get; set; }
        public bool is_grid { get; set; }

        public string path { get; set; }

        public string sass_export_name { get; set; }
        public mmria.common.metadata.node Node { get; set; }

        public Dictionary<string, string> display_to_value { get; set; }
        public Dictionary<string, string> value_to_display { get; set; }
    }


    private List<Metadata_Node> get_metadata_node_by_type(mmria.common.metadata.app p_metadata, string p_type)
    {
        var result = new List<Metadata_Node>();
        foreach (var node in p_metadata.children)
        {
            var current_type = node.type.ToLowerInvariant();
            if (current_type == p_type)
            {
                result.Add(new Metadata_Node()
                {
                    is_multiform = false,
                    is_grid = false,
                    path = node.name,
                    Node = node,
                    sass_export_name = node.sass_export_name
                });
            }
            else if (current_type == "form")
            {
                if
                (
                    node.cardinality == "+" ||
                    node.cardinality == "*"
                )
                {
                    get_metadata_node_by_type(ref result, node, p_type, true, false, node.name);
                }
                else
                {
                    get_metadata_node_by_type(ref result, node, p_type, false, false, node.name);
                }
            }
        }
        return result;
    }

    private void get_metadata_node_by_type(ref List<Metadata_Node> p_result, mmria.common.metadata.node p_node, string p_type, bool p_is_multiform, bool p_is_grid, string p_path)
    {
        var current_type = p_node.type.ToLowerInvariant();
        if (current_type == p_type)
        {
            var value_to_display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var display_to_value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if
            (
                current_type == "list"
            )
            {

                if (!string.IsNullOrWhiteSpace(p_node.path_reference))
                {
                    //var key = "lookup/" + p_node.name;
                    var key = p_node.path_reference;
                    if (this.lookup.ContainsKey(key))
                    {
                        var values = this.lookup[key];

                        p_node.values = values;
                    }
                }

                foreach (var value_item in p_node.values)
                {
                    var value = value_item.value;
                    var display = value_item.display;

                    if (!value_to_display.ContainsKey(value))
                    {
                        value_to_display.Add(value, display);
                    }

                    if (!display_to_value.ContainsKey(display))
                    {
                        display_to_value.Add(display, value);
                    }
                }
            }

            p_result.Add(new Metadata_Node()
            {
                is_multiform = p_is_multiform,
                is_grid = p_is_grid,
                path = p_path,
                Node = p_node,
                value_to_display = value_to_display,
                display_to_value = display_to_value,
                sass_export_name = p_node.sass_export_name
            });
        }
        else if (p_node.children != null)
        {
            foreach (var node in p_node.children)
            {
                if (current_type == "grid")
                {
                    get_metadata_node_by_type(ref p_result, node, p_type, p_is_multiform, true, p_path + "/" + node.name);
                }
                else
                {
                    get_metadata_node_by_type(ref p_result, node, p_type, p_is_multiform, p_is_grid, p_path + "/" + node.name);
                }
            }
        }
    }

    private mmria.common.metadata.node get_metadata_node(mmria.common.metadata.app p_metadata, string p_path)
    {
        mmria.common.metadata.node result = null;

        mmria.common.metadata.node current = null;

        string[] path = p_path.Split("/");

        for (int i = 0; i < path.Length; i++)
        {
            string current_name = path[i];
            if (i == 0)
            {
                foreach (var child in p_metadata.children)
                {
                    if (child.name.Equals(current_name, StringComparison.OrdinalIgnoreCase))
                    {
                        current = child;
                        break;
                    }
                }
            }

            else
            {

                if (current.children != null)
                {
                    foreach (var child2 in current.children)
                    {
                        if (child2.name.Equals(current_name, StringComparison.OrdinalIgnoreCase))
                        {
                            current = child2;
                            break;
                        }
                    }
                }
                else
                {
                    return result;
                }

                if (i == path.Length - 1)
                {
                    result = current;
                }
            }

        }

        return result;
    }

    (Dictionary<string, string> display_to_value, Dictionary<string, string> value_to_display) convert(mmria.common.metadata.node value)
    {
        Dictionary<string, string> display_to_value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> value_to_display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);


        if (!string.IsNullOrWhiteSpace(value.path_reference))
        {
            //var key = "lookup/" + p_node.name;
            var key = value.path_reference;
            if (this.lookup.ContainsKey(key))
            {
                var values = this.lookup[key];

                value.values = values;
            }
        }
        if (value.values != null)
            foreach (var value_item in value.values)
            {
                var v = value_item.value;
                var display = value_item.display;

                if (!value_to_display.ContainsKey(v))
                {
                    value_to_display.Add(v, display);
                }

                if (!display_to_value.ContainsKey(display))
                {
                    display_to_value.Add(display, v);
                }
            }

        return (display_to_value, value_to_display);
    }


    private Dictionary<string, mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
    {
        var result = new Dictionary<string, mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in p_metadata.lookup)
        {
            result.Add("lookup/" + node.name, node.values);
        }
        return result;
    }


    mmria.common.model.couchdb.Change_Stack DebounceDateTimeField(mmria.common.model.couchdb.Change_Stack value)
    {
        var result = new mmria.common.model.couchdb.Change_Stack()
        {
            _id = value._id,
            _rev = value._rev,
            case_id = value.case_id,
            case_rev = value.case_rev,
            user_name = value.user_name,
            note = value.note,
            metadata_version = value.metadata_version,
            date_created = value.date_created
        };

        string found_path = "";
        int found_index = -1;
        int last_index = -1;
        int target_index = -1;
        for (var subitem_index = 0; subitem_index < value.items.Count; subitem_index++)
        {
            var subitem = value.items[subitem_index];


            if (subitem.metadata_type.ToUpper() == "DATETIME")
            {
                if (subitem.dictionary_path == found_path)
                {
                    last_index = subitem_index;
                    continue;
                }
                else
                {
                    found_path = subitem.dictionary_path;
                    found_index = subitem_index;
                    target_index = result.items.Count;
                    result.items.Add(subitem);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(found_path))
                {
                    if (last_index > -1)
                        result.items[target_index].old_value = value.items[last_index].old_value;
                    result.items[target_index].new_value = value.items[found_index].new_value;

                    found_path = "";
                    found_index = -1;
                    last_index = -1;
                    target_index = -1;
                }
                result.items.Add(subitem);
            }

        }

        if
        (
            !string.IsNullOrWhiteSpace(found_path) &&
            last_index > -1
        )
        {
            result.items[target_index].old_value = value.items[last_index].old_value;
            result.items[target_index].new_value = value.items[found_index].new_value;


            found_path = "";
            found_index = -1;
            last_index = -1;
            target_index = -1;
        }

        return result;
    }
    
    [HttpPost]
    [Route("api/_audit/audit-manage-user")]
    public async Task<IActionResult> PostAuditHistory([FromBody] mmria.common.model.couchdb.audit.Audit_Manage_User master_audit_document)
    {
        if (master_audit_document == null || master_audit_document.items == null || !master_audit_document.items.Any())
        {
            return BadRequest("No audit items provided");
        }
        try
        {
            // Save the updated master document to CouchDB
            var db_save_result = await SaveAuditDocument(master_audit_document);
            
            if (db_save_result.ok)
            {
                // Return success response
                var response = new
                {
                    ok = true,
                    _id = master_audit_document._id,
                    _rev = db_save_result.rev
                };
                return Ok(response);
            }
            else
            {
                return StatusCode(500, "Failed to save audit history to database");
            }
        }
        catch (Exception ex)
        {
            // Log the exception (you might want to use a proper logging framework)
            Console.WriteLine($"Error saving audit history: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    
    [HttpGet]
    public async Task<mmria.common.model.couchdb.audit.Audit_Manage_User> GetAuditDocument()
    {
        try
        {
            var request_string = $"{db_config.url}/{db_config.prefix}audit/audit-manage-user";
            var audit_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);

            string responseFromServer = await audit_curl.executeAsync();

            // Check if document exists (not a 404 error)
            if (responseFromServer.Contains("\"error\":\"not_found\""))
            {
                return null; // Document doesn't exist yet
            }

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.audit.Audit_Manage_User>(responseFromServer);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting master audit document: {ex.Message}");
            return null; // Return null if document doesn't exist or error occurred
        }
    }

    private async Task<mmria.common.model.couchdb.document_put_response> SaveAuditDocument(mmria.common.model.couchdb.audit.Audit_Manage_User auditDocument)
    {
        try
        {
            // Configure JSON serialization to exclude null values
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };
            
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(auditDocument, settings);
            var request_string = $"{db_config.url}/{db_config.prefix}audit/{auditDocument._id}";
            
            Console.WriteLine($"SaveAuditDocument URL: {request_string}");
            Console.WriteLine($"SaveAuditDocument _rev: {auditDocument._rev ?? "null"}");
            Console.WriteLine($"SaveAuditDocument Data: {object_string}");
            
            var audit_curl = new mmria.server.cURL("PUT", null, request_string, object_string, db_config.user_name, db_config.user_value);
            
            string responseFromServer = await audit_curl.executeAsync();
            Console.WriteLine($"SaveAuditDocument Response: {responseFromServer}");
            
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SaveAuditDocument: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return new mmria.common.model.couchdb.document_put_response { ok = false };
        }
    }

}
