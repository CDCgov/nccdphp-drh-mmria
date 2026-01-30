namespace mmria.common.steve;

public sealed class AuthRequestBody
{
    public AuthRequestBody(){}
    public string clientName {get;set;}
    public string clientSecretKey {get;set;}
    public string seaBucketKMSKey  {get;set;}
}

public sealed class AuthResponse
{
    public AuthResponse(){}
    public string id {get;set;}
    public bool? authenticated {get;set;}
    public string token {get;set;}

    public string message{get;set;}

    public bool? success {get;set;}

}