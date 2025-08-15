using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace migrate;

public sealed class cURL
{
    string method;
    System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,string>> headers;
    string url;
    string pay_load;
    string user_id;
    string password;

    string content_type;
    public cURL (string p_method, string p_headers, string p_url, string p_pay_load, string p_username = null,
    string p_password = null, string p_content_type = "application/json")
    {
        this.headers = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,string>> ();

        this.content_type = p_content_type;

        this.user_id = p_username;
        this.password = p_password;
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

    public string execute ()
    {
        string result = null;

        var httpWebRequest = (HttpWebRequest)WebRequest.Create(this.url);
        httpWebRequest.ReadWriteTimeout = 100000; //this can cause issues which is why we are manually setting this
        httpWebRequest.ContentType = this.content_type;
        httpWebRequest.PreAuthenticate = false;
        httpWebRequest.Accept = "*/*";
        httpWebRequest.Method = this.method;
        httpWebRequest.AllowAutoRedirect = this.AllowRedirect;

        if (!string.IsNullOrWhiteSpace(this.user_id) && !string.IsNullOrWhiteSpace(this.password))
        {
            string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.user_id + ":" + this.password));
            httpWebRequest.Headers.Add("Authorization", "Basic " + encoded);
        }


        foreach (System.Collections.Generic.KeyValuePair<string,string> kvp in this.headers) 
        {
            httpWebRequest.Headers.Add (kvp.Key, kvp.Value);
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
        //}

        return result;
    }


    public async System.Threading.Tasks.Task<string> executeAsync()
    {
        string result = null;

        var httpWebRequest = (HttpWebRequest)WebRequest.Create (this.url);
        httpWebRequest.ReadWriteTimeout = 100000; //this can cause issues which is why we are manually setting this
        httpWebRequest.ContentType = this.content_type;
        httpWebRequest.PreAuthenticate = false;
        httpWebRequest.Accept = "*/*";
        httpWebRequest.Method = this.method;
        httpWebRequest.AllowAutoRedirect = this.AllowRedirect;

        if (!string.IsNullOrWhiteSpace (this.user_id) && !string.IsNullOrWhiteSpace (this.password))
        {
            string encoded = System.Convert.ToBase64String (System.Text.Encoding.GetEncoding ("ISO-8859-1").GetBytes (this.user_id + ":" + this.password));
            httpWebRequest.Headers.Add ("Authorization", "Basic " + encoded);
        }


        foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in this.headers) 
        {
            httpWebRequest.Headers.Add (kvp.Key, kvp.Value);
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
        WebResponse resp = await httpWebRequest.GetResponseAsync ();
        result = new StreamReader (resp.GetResponseStream ()).ReadToEnd ();
        //Console.WriteLine("Response : " + respStr); // if you want see the output
        //}
        //catch(Exception ex)
        //{
        //process exception here   
        //  result = ex.ToString();
        //}

        return result;
    }


    public cURL add_authentication_header(string p_username,
    string p_password)
    {

        this.user_id = p_username;
        this.password = p_password;

        return this;
    }
}


