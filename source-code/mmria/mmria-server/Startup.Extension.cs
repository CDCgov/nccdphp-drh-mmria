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
            if (string.IsNullOrWhiteSpace(@this.Host))
            {
                return null;
            }

            // In single-tenant deployments, internal requests often arrive with a pod IP
            // or service name instead of the public route host. Always prefer the configured
            // single-tenant config ID in that mode so DB/config resolution stays stable.
            string multiTenantJurisdictions = System.Environment.GetEnvironmentVariable("multi_tenant_jurisdictions");
            string multiTenantTemplateUrl = System.Environment.GetEnvironmentVariable("multi_tenant_shared_config_id_template_couchdb_url");
            string multiTenantRebuildSource = System.Environment.GetEnvironmentVariable("multi_tenant_re_build_src");
            bool isSingleTenantMode =
                string.IsNullOrWhiteSpace(multiTenantJurisdictions) &&
                string.IsNullOrWhiteSpace(multiTenantTemplateUrl) &&
                string.IsNullOrWhiteSpace(multiTenantRebuildSource);

            if (isSingleTenantMode)
            {
                string singleTenantPrefix =
                    System.Environment.GetEnvironmentVariable("config_id") ??
                    System.Environment.GetEnvironmentVariable("app_instance_name");

                if (!string.IsNullOrWhiteSpace(singleTenantPrefix))
                {
                    return singleTenantPrefix;
                }
            }

            return @this.Host.ToString().Split("-")[0];
        }
    }
