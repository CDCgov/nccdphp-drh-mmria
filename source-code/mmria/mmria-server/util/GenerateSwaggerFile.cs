using System;

namespace mmria.server;

public sealed class GenerateSwaggerFile
{
    public GenerateSwaggerFile ()
    {
    }

    //http://json-schema.org/
    //http://swagger.io/specification/

    public string generate(mmria.common.metadata.app app)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder ();

        result.Append (pre_fix);
        result.Append ("\"case_item\":{");
        result.Append ("\"title\": \"case_item schema\",");
        result.Append ("\"type\": \"object\",");
        result.Append ("\"properties\": {");
        result.Append(@" 
            ""_id"": {
                    ""description"": ""id"",
                    ""type"": ""string""
            },
            ""_rev"": {
                ""description"": ""revision"",
                ""type"": ""string""
            },");
        
        for(int i = 0; i < app.children.Length; i ++) 
        {
            var child = app.children [i];
            generate (result, child);
            if(i < app.children.Length - 1)
            {
                result.Append (",");
            }
        }


        result.Append (@",
            ""required"": [""_id""]}}
}
}");
        //result.Append (post_fix);
        return result.ToString();
    }


    public void generate(System.Text.StringBuilder result, mmria.common.metadata.node node)
    {
        switch (node.type.ToLower ()) 
        {
        case "form":
            if 
            (
                    node.cardinality != null &&
                    (
                        node.cardinality == "*" ||
                        node.cardinality == "+"
                    )
            ) 
            {
                result.Append ("\"");
                result.Append (node.name); result.Append("\":{");
                result.Append ("\"type\": \"array\",");
                result.Append ("\"items\": { \"type\":\"object\", \"properties\": { ");

                for(int i = 0; i < node.children.Length; i ++) 
                {
                    var child = node.children [i];
                    generate (result, child);
                    if(i < node.children.Length - 1)
                    {
                        result.Append (",");
                    }
                }
                result.Append ("}}}\n");
            } 
            else 
            {
                result.Append ("\"");
                result.Append (node.name); result.Append("\":{");
                //result.Append ("\"title\": \"case_item schema\",");
                result.Append ("\"type\": \"object\",");
                result.Append ("\"properties\": {");

                for(int i = 0; i < node.children.Length; i ++) 
                {
                    var child = node.children [i];
                    generate (result, child);
                    if(i < node.children.Length - 1)
                    {
                        result.Append (",");
                    }
                }
                result.Append ("}}\n");
            }
                
                break;
        case "group":
            result.Append ("\"");
            result.Append (node.name); result.Append("\":{");
            //result.Append ("\"title\": \"case_item schema\",");
            result.Append ("\"type\": \"object\",");
            result.Append ("\"properties\": {");

            for(int i = 0; i < node.children.Length; i ++) 
            {
                var child = node.children [i];
                generate (result, child);
                if(i < node.children.Length - 1)
                {
                    result.Append (",");
                }
            }
            result.Append ("}}\n");
            break;
        case "grid":
            result.Append ("\"");
            result.Append (node.name); result.Append("\":{");
            //result.Append ("\"title\": \"case_item schema\",");
            result.Append ("\"type\": \"array\",");
            result.Append ("\"items\": { \"type\":\"object\", \"properties\": { ");

            for(int i = 0; i < node.children.Length; i ++) 
            {
                var child = node.children [i];
                generate (result, child);
                if(i < node.children.Length - 1)
                {
                    result.Append (",");
                }
            }
            result.Append ("}}}\n");
            break;
            default:
                
                result.Append ("\""); result.Append(node.name); result.Append("\": {");
                //result.Append ("\"description\": \"text item.\",");
                switch(node.type.ToLower())
                {
                    
                    //case "button":
                    case "boolean":
                        result.Append ("\"type\": \"boolean\"");
                        break;
                    case "date":
                        result.Append ("\"type\": \"date\"");
                        break;
                    case "number":
                        result.Append ("\"type\": \"double\"");
                        break;
                    case "time":
                        result.Append ("\"type\": \"dateTime\"");
                        break;
                    case "list":
                        if (node.is_multiselect != null && node.is_multiselect.HasValue && node.is_multiselect.Value) 
                        {
                            result.Append ("\"type\": \"array\"");
                        }
                        else 
                        {
                            result.Append ("\"type\": \"string\"");	
                        }
                        
                        break;
                    case "string":
                    case "label":
                    case "textarea":
                    default:
                        result.Append ("\"type\": \"string\"");
                        break;
                }
                result.Append ("}\n");
            break;

        }

    }




    private static string pre_fix = @"{
""swagger"": ""2.0"",
""info"": {
""version"": ""0.0.3"",
""title"": ""MMRIA data import API"",
""description"": ""An API for use in electronic importing of case data using OpenAPISpecification -swagger-2.0"",
""termsOfService"": ""http://swagger.io/terms/"",
""contact"": {
    ""name"": ""admin at mmria.org""
},
""license"": {
    ""name"": ""GPLv3""
}
},
""host"": ""test.mmria.org"",
""basePath"": ""/api"",
""schemes"": [
""http""
],
""consumes"": [
""application/json""
],
""produces"": [
""application/json""
],
""paths"": {
""/queue"": {
    ""post"": {
    ""operationId"": ""setQueueRequest"",
    ""description"": ""Adds new and existing case records into processing queue."",
    ""produces"": [
        ""application/json""
    ],
    ""parameters"": [ 
    {
        ""name"": ""set_queue_request"",
        ""in"": ""body"",
        ""description"": ""A list of case items to be imported."",
        ""required"": true,
        ""schema"": { ""$ref"": ""#/definitions/set_queue_request""   }
}
    ],
    ""responses"": {
        ""200"": {
        ""description"": ""Sets a data import queue."",
        ""schema"": {""$ref"": ""#/definitions/set_queue_response"" }
        }
    }
    }
}
},
""definitions"": {
""set_queue_request"": {
            ""title"": ""Example Schema"",
            ""type"": ""object"",
            ""properties"": {
            ""security_token"": {
                ""type"": ""string""
            },
            ""action"": {
                ""type"": ""string""
            },
            ""case_list"": {
                ""description"": ""Array of cases to be processed"",
                ""type"": ""array"",
                ""items"": {
                    ""$ref"": ""#/definitions/case_item""
                }
            }
        }              ,
            ""required"": [""secutiry_token"", ""case_list""]
        },
    ""set_queue_response"":{
            ""title"": ""Example Schema"",
            ""type"": ""object"",
            ""properties"": {
            ""ok"": {
                ""description"": ""Error indication if request is accepted."",
                ""type"": ""boolean""
            },
            ""message"": {
                ""description"": ""Message from server."",
                ""type"": ""string""
            },
            ""queue_id"": {
                ""description"": ""queue item id."",
                ""type"": ""string""
                
            }
            }
    },";



    private static string post_fix = @"
}
};";


    private static string sample = @"{
""swagger"": ""2.0"",
""info"": {
""version"": ""0.0.3"",
""title"": ""MMRIA data import API"",
""description"": ""An API for use in electronic importing of case data using OpenAPISpecification -swagger-2.0"",
""termsOfService"": ""http://swagger.io/terms/"",
""contact"": {
    ""name"": ""admin at mmria.org""
},
""license"": {
    ""name"": ""GPLv3""
}
},
""host"": ""test.mmria.org"",
""basePath"": ""/api"",
""schemes"": [
""http""
],
""consumes"": [
""application/json""
],
""produces"": [
""application/json""
],
""paths"": {
""/queue"": {
    ""get"": {
    ""operationId"": ""setQueueRequest"",
    ""description"": ""Adds new and existing case records into processing queue."",
    ""produces"": [
        ""application/json""
    ],
    ""parameters"": [ 
    {
        ""name"": ""set_queue_request"",
        ""in"": ""body"",
        ""description"": ""How many items to return at one time (max 100)"",
        ""required"": true,
        ""schema"": { ""$ref"": ""#/definitions/set_queue_request""   }
}
    ],
    ""responses"": {
        ""200"": {
        ""description"": ""Sets a data import queue."",
        ""schema"": {""$ref"": ""#/definitions/set_queue_response"" }
        }
    }
    }
}
},
""definitions"": {
""set_queue_request"": {
            ""title"": ""Example Schema"",
            ""type"": ""object"",
            ""properties"": {
            ""security_token"": {
                ""type"": ""string""
            },
            ""action"": {
                ""type"": ""string""
            },
            ""case_list"": {
                ""description"": ""Array of cases to be processed"",
                ""type"": ""array"",
                ""items"": {
                    ""$ref"": ""#/definitions/case_item""
                }
            },
            ""attribute1"":{
            ""title"": ""attribute 1"",
            ""type"": ""object"",
            ""properties"": {
            ""street"": {
                ""type"": ""string""
            },
            ""city"": {
                ""type"": ""string""
            },
            ""order"": {
                ""description"": ""Age in years"",
                ""type"": ""integer"",
                ""minimum"": 0
            }
            },
            ""required"": [""street"", ""city""]
            }
        }              ,
            ""required"": [""secutiry_token"", ""case_list""]
        },
    ""set_queue_response"":{
            ""title"": ""Example Schema"",
            ""type"": ""object"",
            ""properties"": {
            ""ok"": {
                ""description"": ""Error indication if request is accepted."",
                ""type"": ""boolean""
            },
            ""message"": {
                ""description"": ""Message from server."",
                ""type"": ""string""
            },
            ""queue_id"": {
                ""description"": ""queue item id."",
                ""type"": ""string""
                
            }
            }
    },
    ""case_item"":{
            ""title"": ""case_item schema"",
            ""type"": ""object"",
            ""properties"": {
            ""_id"": {
                ""description"": ""id"",
                ""type"": ""boolean""
            },
            ""_rev"": {
                ""description"": ""revision"",
                ""type"": ""string""
            },
            ""text_item"": {
                ""description"": ""text item."",
                ""type"": ""string""
                
            }
            }
    }
}
}";
}


