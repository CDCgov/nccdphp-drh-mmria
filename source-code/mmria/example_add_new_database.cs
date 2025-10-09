// Example: How to add a new CouchDB database configuration to MMRIA

using mmria.common.couchdb;

public class AddNewDatabaseExample
{
    // Method 1: Add to ConfigurationSet programmatically
    public void AddDatabaseToConfigurationSet(ConfigurationSet configSet, string jurisdiction)
    {
        var newDbConfig = new DBConfigurationDetail
        {
            prefix = "new_db_",  // Database prefix
            url = "http://localhost:5984",  // CouchDB server URL
            user_name = "admin_user",  // CouchDB username
            user_value = "admin_password"  // CouchDB password
        };
        
        // Add to the detail_list with jurisdiction as key
        configSet.detail_list[jurisdiction] = newDbConfig;
    }

    // Method 2: Add to OverridableConfiguration (preferred approach)
    public void AddDatabaseToOverridableConfig(OverridableConfiguration config, string jurisdiction)
    {
        // Ensure the jurisdiction key exists in string_keys
        if (!config.string_keys.ContainsKey(jurisdiction))
        {
            config.string_keys[jurisdiction] = new Dictionary<string, string>();
        }

        // Add database configuration keys
        config.string_keys[jurisdiction]["couchdb_url"] = "http://localhost:5984";
        config.string_keys[jurisdiction]["db_prefix"] = "new_db_";
        config.string_keys[jurisdiction]["timer_user_name"] = "admin_user";
        config.string_keys[jurisdiction]["timer_value"] = "admin_password";
    }

    // Method 3: Example of how the system retrieves database config
    public DBConfigurationDetail GetDatabaseConfig(OverridableConfiguration config, string jurisdiction)
    {
        // This is how the system gets DB config using GetDBConfig method
        return config.GetDBConfig(jurisdiction);
    }

    // Method 4: Example of using the database configuration
    public async Task UseDatabaseConfig(DBConfigurationDetail dbConfig)
    {
        // Construct database URL for a specific database
        string mmrdsUrl = dbConfig.Get_Prefix_DB_Url("mmrds");
        string metadataUrl = dbConfig.Get_Prefix_DB_Url("metadata");
        string jurisdictionUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction");
        
        // Example: Create new database
        string newDatabaseUrl = dbConfig.Get_Prefix_DB_Url("my_new_database");
        
        // Use cURL to create the database
        var createDbCurl = new mmria.server.cURL(
            "PUT", 
            null, 
            newDatabaseUrl, 
            null, 
            dbConfig.user_name, 
            dbConfig.user_value
        );
        
        string result = await createDbCurl.executeAsync();
        Console.WriteLine($"Database creation result: {result}");
    }
}

// Configuration in appsettings.json would look like:
/*
{
  "mmria_settings": {
    "couchdb_url": "http://localhost:5984",
    "db_prefix": "new_db_",
    "timer_user_name": "admin_user",
    "timer_value": "admin_password"
  }
}
*/

// Example configuration document that would be stored in CouchDB:
/*
{
  "_id": "configuration_document_id",
  "_rev": "1-abc123",
  "data_type": "configuration-set",
  "detail_list": {
    "new_jurisdiction": {
      "prefix": "new_db_",
      "url": "http://localhost:5984",
      "user_name": "admin_user", 
      "user_value": "admin_password"
    }
  },
  "name_value": {
    "metadata_version": "25.06.16"
  }
}
*/
