using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace mmria.console;

public sealed class cURL
{
    string method;
    System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,string>> headers;
    System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,string>> cookies;
    string url;
    string pay_load;
    string name;
    string value;

    public cURL (string p_method, string p_headers, string p_url, string p_pay_load, string p_username = null,
    string p_value = null)
    {
        this.headers = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,string>> ();
        this.cookies = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,string>> ();

        this.name = p_username;
        this.value = p_value;
        this.AllowRedirect = true;

        switch (p_method.ToUpper ()) 
        {
            case "PUT":
                this.method = "PUT";
                break;
            case "POST":
                this.method = "POST";
                break;
            case "DELETE":
                this.method = "DELETE";
                break;
            case "HEAD":
                this.method = "HEAD";
            break;					
            case "GET":
            default:
                this.method = "GET";
                break;
        }

        url = p_url;
        pay_load = p_pay_load;
        if (p_headers != null) 
        {
            string[] name_value_list = p_headers.Split ('|');

            foreach (string name_value in name_value_list) 
            {
                string[] n_v = name_value.Split (' ');
                this.headers.Add (new System.Collections.Generic.KeyValuePair<string,string> (n_v [0], n_v [1]));
            }

        }
    }


    public bool AllowRedirect { get; set; }

    public cURL AddHeader(string p_name, string p_value)
    {
        this.headers.Add(new System.Collections.Generic.KeyValuePair<string,string>(p_name, p_value));
        return this;
    }

    public cURL AddCookie(string p_name, string p_value)
    {
        this.cookies.Add(new System.Collections.Generic.KeyValuePair<string,string>(p_name, p_value));
        return this;
    }

    public string execute ()
    {
        string result = null;

        var httpWebRequest = (HttpWebRequest)WebRequest.Create(this.url);
        httpWebRequest.ReadWriteTimeout = 100000; //this can cause issues which is why we are manually setting this
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.PreAuthenticate = false;
        httpWebRequest.Accept = "*/*";
        httpWebRequest.Method = this.method;
        httpWebRequest.AllowAutoRedirect = this.AllowRedirect;

        if (!string.IsNullOrWhiteSpace(this.name) && !string.IsNullOrWhiteSpace(this.value))
        {
            string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.name + ":" + this.value));
            httpWebRequest.Headers.Add("Authorization", "Basic " + encoded);
        }


        foreach (System.Collections.Generic.KeyValuePair<string,string> kvp in this.headers) 
        {
            httpWebRequest.Headers.Add (kvp.Key, kvp.Value);
        }

        foreach (System.Collections.Generic.KeyValuePair<string,string> kvp in this.cookies) 
        {
            if (httpWebRequest.CookieContainer == null)
            {
                httpWebRequest.CookieContainer = new CookieContainer();
            }

            httpWebRequest.CookieContainer.Add(new Cookie(kvp.Key,kvp.Value) { Domain = httpWebRequest.Host.Split(":")[0] });
        }
    


        if (this.pay_load != null) 
        {
            //httpWebRequest.ContentLength = this.pay_load.Length;

            using (var streamWriter = new StreamWriter (httpWebRequest.GetRequestStream ())) 
            {
                streamWriter.Write (this.pay_load);
                streamWriter.Flush ();
                streamWriter.Close ();
            }
        }

        //try
        //{
            HttpWebResponse resp = (HttpWebResponse)httpWebRequest.GetResponse();
            result = new StreamReader(resp.GetResponseStream()).ReadToEnd();
            //Console.WriteLine("Response : " + respStr); // if you want see the output
        //}
        //catch(Exception ex)
        //{
            //process exception here   
        //	result = ex.ToString();
        //	Console.Write(ex); // if you want see the output
        //	throw ex;
        //}

        return result;
    }


    public async System.Threading.Tasks.Task<string> executeAsync()
    {
        string result = null;

        var httpWebRequest = (HttpWebRequest)WebRequest.Create (this.url);
        httpWebRequest.ReadWriteTimeout = 100000; //this can cause issues which is why we are manually setting this
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.PreAuthenticate = false;
        httpWebRequest.Accept = "*/*";
        httpWebRequest.Method = this.method;
        httpWebRequest.AllowAutoRedirect = this.AllowRedirect;

        if (!string.IsNullOrWhiteSpace (this.name) && !string.IsNullOrWhiteSpace (this.value))
        {
            string encoded = System.Convert.ToBase64String (System.Text.Encoding.GetEncoding ("ISO-8859-1").GetBytes (this.name + ":" + this.value));
            httpWebRequest.Headers.Add ("Authorization", "Basic " + encoded);
        }


        foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in this.headers) 
        {
            httpWebRequest.Headers.Add (kvp.Key, kvp.Value);
        }


        foreach (System.Collections.Generic.KeyValuePair<string,string> kvp in this.cookies) 
        {
            if (httpWebRequest.CookieContainer == null)
            {
                httpWebRequest.CookieContainer = new CookieContainer();
            }

            httpWebRequest.CookieContainer.Add(new Cookie(kvp.Key,kvp.Value) { Domain = httpWebRequest.Host.Split(":")[0] });
        }

        if (this.pay_load != null) 
        {
            //httpWebRequest.ContentLength = this.pay_load.Length;

            using (var streamWriter = new StreamWriter (httpWebRequest.GetRequestStream ()))
            {
                streamWriter.Write (this.pay_load);
                streamWriter.Flush ();
                streamWriter.Close ();
            }
        }

        try
        {
            WebResponse resp = await httpWebRequest.GetResponseAsync ();
            result = new StreamReader (resp.GetResponseStream ()).ReadToEnd ();
        //Console.WriteLine("Response : " + respStr); // if you want see the output
        }
        catch(Exception ex)
        {
        //process exception here   
        //  result = ex.ToString();
            Console.Write(ex); // if you want see the output
            throw ex;
        }

        return result;
    }


    public cURL add_authentication_header(string p_name, string p_value)
    {

        this.name = p_name;
        this.value = p_value;

        return this;
    }
}


