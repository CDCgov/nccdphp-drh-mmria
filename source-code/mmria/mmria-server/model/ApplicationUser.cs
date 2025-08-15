public sealed class ApplicationUser 
{
    public string UserName { get; set; }
    public string Value { get; set; }

    public ApplicationUser() { }
    public ApplicationUser(string username, string value) 
    {
        this.UserName = username;
        this.Value = value;
    }
}