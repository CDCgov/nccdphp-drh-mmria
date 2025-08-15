namespace mmria.server.extension;
public static class StartupExtension
{
    public static void SetIfIsNotNullOrWhiteSpace(this string @this, ref bool that)
    {
        if (!string.IsNullOrWhiteSpace(@this))
        {
            bool.TryParse(@this, out that);
        }
    }

    public static void SetIfIsNotNullOrWhiteSpace(this string @this, ref bool that, bool defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(@this))
        {
            if(!bool.TryParse(@this, out that))
                that = defaultValue;
        }
        else that = defaultValue;


    }
    public static void SetIfIsNotNullOrWhiteSpace(this string @this, ref string that)
    {
        if (!string.IsNullOrWhiteSpace(@this))
        {
            that = @this;
        }
    }





    public static void SetIfIsNotNullOrWhiteSpace(this string @this, ref string that, string defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(@this))
        {
            that = @this;
        }
        else that = defaultValue;
    }


    public static void SetIfIsNotNullOrWhiteSpace(this string @this, ref int that)
    {
        if (!string.IsNullOrWhiteSpace(@this))
        {
            int.TryParse(@this, out that);
        }
    }

    public static void SetIfIsNotNullOrWhiteSpace(this string @this, ref int that, int defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(@this))
        {
            if(!int.TryParse(@this, out that))
                that = defaultValue;
        
        }
        else that = defaultValue;
    }

    public static void SetIfIsNotNullOrWhiteSpace(this int? @this, ref int that)
    {
        if (@this.HasValue)
        {
            that = @this.Value;
        }
    }

    public static void SetIfIsNotNullOrWhiteSpace(this bool? @this, ref bool that)
    {
        if (@this.HasValue)
        {
            that = @this.Value;
        }
    }
}

    public static class HostString
    {
        public static string GetPrefix(this Microsoft.AspNetCore.Http.HostString @this)
        {
            if (!string.IsNullOrWhiteSpace(@this.Host))
            {
                return @this.Host.ToString().Split("-")[0];
            
            }

            return null;
        }
    }