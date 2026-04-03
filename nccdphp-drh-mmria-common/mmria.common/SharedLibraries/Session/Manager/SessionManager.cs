using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Session.DAL;
using mmria.common.SharedLibraries.Session.Model;

namespace mmria.common.SharedLibraries.Session.Manager;

public sealed class SessionManager
{
    private readonly SessionDAL _dal;

    public SessionManager
    (
        SessionDAL dal
    )
    {
        _dal = dal;
    }

    public void RecordSessionEvent(Session_Event_Message sem, mmria.common.couchdb.DBConfigurationDetail db_config)
    {
        var se = new mmria.common.model.couchdb.session_event();
            se.data_type = "session-event";
            se._id =sem._id;
            se.date_created  = sem.date_created;
            se.user_id  = sem.user_id;
            se.ip  = sem.ip;

            switch(sem.action_result)
            {
                case Session_Event_Message.Session_Event_Message_Action_Enum.successful_login:
                    se.action_result = mmria.common.model.couchdb.session_event.session_event_action_enum.successful_login;
                    break;
                case Session_Event_Message.Session_Event_Message_Action_Enum.password_changed:
                    se.action_result = mmria.common.model.couchdb.session_event.session_event_action_enum.password_changed;
                    break;                            
                case Session_Event_Message.Session_Event_Message_Action_Enum.failed_login:
                default:
                    se.action_result = mmria.common.model.couchdb.session_event.session_event_action_enum.failed_login;
                    break;

            }


            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            _ = settings;
            _ = _dal.SaveSessionEventAsync(se, db_config);

    }

    public async Task PostSessionAsync(Session_Message session_message, mmria.common.couchdb.DBConfigurationDetail db_config)
    {
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
            string request_string = db_config.url + $"/{db_config.prefix}session/{session_message._id}";

            try 
            {
                var check_document_expando_object = await _dal.GetSessionDocumentAsync(session_message._id, db_config);

                if(!string.IsNullOrWhiteSpace(check_document_expando_object.user_id) && !session_message.user_id.Equals(check_document_expando_object.user_id, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write($"unauthorized PUT {session_message._id} by: {session_message.user_id}");
                    return;
                }
            } 
            catch (Exception) 
            {
                // do nothing for now document doesn't exsist.
            }

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(session_message, settings);

            try
            {
                var sessionDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<session>(object_string);
                result = await _dal.SaveSessionAsync(sessionDocument, db_config);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
    }

    public async Task<get_sortable_view_reponse_header<session>> GetSessionListAsync(
        int skip,
        int take,
        string sort,
        string search_key,
        bool descending,
        DBConfigurationDetail db_config)
    {
        string sort_view = sort.ToLower();
        switch (sort_view)
        {
            case "by_date_created":
            case "by_date_created_user_id":
            case "by_session_event_id":
            case "by_user_id":
            case "by_ip":
                break;
            default:
                sort_view = "by_date_created";
                break;
        }

        var session_view_response = await _dal.GetSessionSortableViewAsync(
            skip,
            take,
            sort_view,
            !string.IsNullOrWhiteSpace(search_key),
            descending,
            db_config);

        if (string.IsNullOrWhiteSpace(search_key))
        {
            return session_view_response;
        }

        string key_compare = search_key.ToLower().Trim(new char[] { '"' });

        var result = new get_sortable_view_reponse_header<session>();
        result.offset = session_view_response.offset;
        result.total_rows = session_view_response.total_rows;

        foreach (get_sortable_view_response_item<session> cvi in session_view_response.rows)
        {
            bool add_item = false;
            if (cvi.value.ip != null && cvi.value.ip.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
            {
                add_item = true;
            }

            if (bool.TryParse(key_compare, out bool is_active))
            {
                if (cvi.value.is_active == is_active)
                {
                    add_item = true;
                }
            }

            if (cvi.value.user_id != null && cvi.value.user_id.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
            {
                add_item = true;
            }

            if (DateTime.TryParse(key_compare, out DateTime is_date))
            {
                if (cvi.value.date_created == is_date)
                {
                    add_item = true;
                }
            }

            if (DateTime.TryParse(key_compare, out is_date))
            {
                if (cvi.value.date_last_updated == is_date)
                {
                    add_item = true;
                }
            }

            if (cvi.value.session_event_id != null && cvi.value.session_event_id.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
            {
                add_item = true;
            }

            if (add_item)
            {
                result.rows.Add(cvi);
            }
        }

        result.total_rows = result.rows.Count;
        result.rows = result.rows.Skip(skip).Take(take).ToList();

        return result;
    }

    public async Task<IEnumerable<session_response>> GetSessionDatabaseAsync(DBConfigurationDetail db_config)
    {
        session_response json_result = await _dal.GetSessionDatabaseAsync(db_config);
        return new session_response[] { json_result };
    }

    public async Task<session> GetSessionDocumentAsync(string id, DBConfigurationDetail db_config)
    {
        return await _dal.GetSessionDocumentAsync(id, db_config);
    }

    public async Task<document_put_response> PostSessionDocumentAsync(session post_request, ClaimsPrincipal user, DBConfigurationDetail db_config)
    {
        document_put_response result = new document_put_response();
        string request_string = db_config.url + $"/{db_config.prefix}session/{post_request._id}";
        _ = result;
        _ = request_string;

        try
        {
            try
            {
                var check_document_expando_object = await _dal.GetSessionDocumentAsync(post_request._id, db_config);

                var userName = user.Identities.First(
                    u => u.IsAuthenticated &&
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;

                if (!userName.Equals(check_document_expando_object.user_id, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write($"unauthorized PUT {post_request._id} by: {userName}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"err caseController.Post\n{ex}");
            }

            try
            {
                result = await _dal.SaveSessionAsync(post_request, db_config);
                _ = result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    public async Task<IEnumerable<session_response>> GetCouchDbSessionAsync(string authSessionValue, DBConfigurationDetail db_config)
    {
        session_response json_result = await _dal.GetCouchDbSessionAsync(authSessionValue, db_config);
        return new session_response[] { json_result };
    }

    public async Task<IEnumerable<login_response>> LoginToCouchDbSessionAsync(DBConfigurationDetail db_config)
    {
        login_response json_result = await _dal.LoginToCouchDbSessionAsync(db_config);
        return new login_response[] { json_result };
    }

    public async Task<int> GetDaysUntilPasswordExpirationAsync(
        string userName,
        int? password_days_before_expires,
        DBConfigurationDetail db_config)
    {
        int days_til_expiration = -1;

        if (!password_days_before_expires.HasValue || password_days_before_expires.Value <= 0)
        {
            return days_til_expiration;
        }

        var session_event_response = await _dal.GetSessionEventsByUserIdAsync(userName, db_config);
        session_event_response.rows.Sort(new Compare_Session_Event_By_DateCreated<session_event>());

        DateTime date_of_last_password_change = DateTime.MinValue;
        foreach (var session_event in session_event_response.rows)
        {
            if (session_event.value.action_result == mmria.common.model.couchdb.session_event.session_event_action_enum.password_changed)
            {
                date_of_last_password_change = session_event.value.date_created;
                break;
            }
        }

        if (date_of_last_password_change != DateTime.MinValue)
        {
            days_til_expiration = password_days_before_expires.Value - (int)(DateTime.Now - date_of_last_password_change).TotalDays;
        }

        return days_til_expiration;
    }

}
