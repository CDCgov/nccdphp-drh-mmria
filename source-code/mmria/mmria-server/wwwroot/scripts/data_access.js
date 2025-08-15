function Data_Access(p_remote_db_url)
{ 
	this.remote_db_url = p_remote_db_url; 
	this.db = new PouchDB('mmrds');
}

//https://www.sitepoint.com/building-offline-first-app-pouchdb/
Data_Access.prototype.set_data = function(object_json, p_error_reponse)
{
	
	if(p_error_reponse)
	{
		this.db.put(object_json, p_error_reponse);
	}
	else
	{
		this.db.put(object_json);	
	}
	
}

Data_Access.prototype.get_data = function(p_query, p_then_function)
{
	var result =  null;
	
	if(p_query)
	{
		if(p_then_function)
		{
			this.db.get(p_query).then(p_then_function);
		}
		else
		{
			result = this.db.get(p_query)
		}
	}
	else
	{
		result = this.db.get({_all_doc: ""})	
	}
	return result;
}

Data_Access.prototype.sync = function()
{
	var local_db = new PouchDB('mmrds');
	var remoteDB = new PouchDB(this.remote_db_url);
	
	PouchDB.sync(local_db, remoteDB);
}


Data_Access.prototype.error_response = function(error, response) 
{
    if (error) 
	{
      console.log(error);
      return;
    }
	else if(response && response.ok) 
	{
      /* Do something with the response. */
    }
}