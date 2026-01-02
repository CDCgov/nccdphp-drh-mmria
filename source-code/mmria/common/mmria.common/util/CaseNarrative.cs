using System;
using System.Text.RegularExpressions;



namespace mmria.common.util;

public static class CaseNarrative
{
    
    public static string StripHTML(string value)
    {
        System.Text.RegularExpressions.Regex oRegEx = new 
        (
            "<!*[^<>]*>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |System.Text.RegularExpressions.RegexOptions.Multiline
        );

        var result = oRegEx.Replace
        (
            value.Replace("\n", " ")
                .Replace("\t", " ")
                .Replace("\\s+", " ")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&quot;", "\"")
                .Replace("&pos;", "'")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&reg;", "Â®")
                .Replace("&copy;", "Â©")
                .Replace("&bull;", "•")
                .Replace("â€¢", "•")
                .Replace("&trade;", "â„¢")
                .Replace("â€™", "’")
                .Replace("â€“", "–")
                .Replace("â€”", "—")
                .Replace("â€¦", "…")
                .Replace("â€œ", "“")
                .Replace("â€š", "‚")
                .Replace("â€ž", "„")
                .Replace("â€¡", "‡")
                .Replace("â€º", "›")
                .Replace("â€¹", "‹")
                .Replace("Â¢", "¢")
                .Replace("Â£", "£")
                .Replace("Â¤", "¤")
                .Replace("Â¦", "¦")
                .Replace("Â§", "§")
                .Replace("Â¯", "¯")
                .Replace("Â±", "±")
                .Replace("Ã·", "÷")
                .Replace("Â¨", "¨")
                .Replace("Â«", "«")
                .Replace("Â»", "»")
                .Replace("Â©", "©")
                .Replace("Â®", "®")
                .Replace("Â´", "´")
                .Replace("Â¸", "¸")
                .Replace("Âº", "º")
                .Replace("Â¹", "¹")
                .Replace("Â²", "²")
                .Replace("Â³", "³")
                .Replace("Â¼", "¼")
                .Replace("Â½", "½")
                .Replace("Â¾", "¾")
                .Replace("â„¢", "™")
                .Replace("Ë†", "ˆ")
                .Replace("Ëœ", "~")
                .Replace("Æ’", "ƒ")
                .Replace("â€°", "‰")
                .Replace("â€", "”")
                .Replace("Â¥", "¥")
                .Replace("Ã¡", "á")
                .Replace("Ã¢", "â")
                .Replace("Ã£", "xã")
                .Replace("Ã¤", "ä")
                .Replace("Ã¥", "å")
                .Replace("Ã¨", "è")
                .Replace("Ã©", "é")
                .Replace("Ãª", "ê")
                .Replace("Ã«", "ë")
                .Replace("Ã¬", "ì")
                .Replace("Ã®", "î")
                .Replace("Ã¯", "ï")
                .Replace("Ã°", "ð")
                .Replace("Ã±", "ñ")
                .Replace("Ã²", "ò")
                .Replace("Ã³", "ó")
                .Replace("Ã´", "ô")
                .Replace("Ãµ", "õ")
                .Replace("Ã¶", "ö")
                .Replace("Ã¸", "ø")
                .Replace("Ã¹", "ù")
                .Replace("Ãº", "ú")
                .Replace("Ã»", "û")
                .Replace("Ã¼", "ü")
                .Replace("Ã½", "ý")
                .Replace("Ã¿", "ÿ")
                .Replace("Ã€", "À")
                .Replace("Ã‚", "Â")
                .Replace("Ãƒ", "Ã")
                .Replace("Ã„", "Ä")
                .Replace("Ã…", "Å")
                .Replace("Ãˆ", "È")
                .Replace("Ã‰", "É")
                .Replace("ÃŠ", "Ê")
                .Replace("Ã‹", "Ë")
                .Replace("ÃŒ", "Ì")
                .Replace("ÃŽ", "Î")
                .Replace("Ã‘", "Ñ")
                .Replace("Ã’", "Ò")
                .Replace("Ã“", "Ó")
                .Replace("Ã”", "Ô")
                .Replace("Ã•", "Õ")
                .Replace("Ã–", "Ö")
                .Replace("Ã—", "×")
                .Replace("Ã™", "Ù")
                .Replace("Ãš", "Ú")
                .Replace("Ã›", "Û")
                .Replace("Ãœ", "Ü")
                .Replace("â€˜", "’")
                .Replace("Â", "")
                .Replace("&#39;", "\'")
                .Replace("<br>", "\n<br>")
                .Replace("<br", "\n<br")
                .Replace("<p", "\n<p")
                .TrimEnd('\r', '\n'),
                ""
        );



            // OLD STUFF - Save for now
            // value.Replace("â€™","")
            // 	.Replace("<br>", "\n")
            // 	.Replace("&quot;", "")
            // 	.Replace("&pos;", "'")
            // 	.Replace("&nbsp;", " ")
            // 	.Replace("&amp;", "&")
            // 	.Replace("&lt;", "<")
            // 	.Replace("&gt;", ">"), 
            // ""
        
        return result;
    }

    public static string StripHtmlAttributes(string value)
    {

        Regex AttributeRegEx = new Regex
        (
            "[a-zA-Z]+='[^']+'|[a-zA-Z]+=\"[^\"]+\"",
            
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        Regex PseudoTagRegex = new Regex
        (
            @"<\/?[a-z]:[^>]+>",
            
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        Regex CommentRegex = new Regex
        (
            @"<!--\[[^>]+>",
            
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );


        Regex StripTrailBlankSpaceExp = new Regex
        (
            @"<\/?[a-zA-Z]+([ ]+)[^>]+>",
            
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        Regex StripHTMLExp = new Regex
        (
            @"(<\/?[^>]+>)",
            
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );


        return StripTrailBlankSpaceExp.Replace
        (
            value,
            ""
        )
        .Replace(CommentRegex.Replace(PseudoTagRegex.Replace(AttributeRegEx.Replace(value,""), ""),""), "");
    }
    public static string StripHtmlAttributes2(string value)
    {
        //string result = "bubba";
        
        //return result;


        int startindex = value.IndexOf('<');
        int endindex = value.IndexOf('>');
        string tag = value.Substring((startindex + 1), (endindex - startindex - 1));
        int spaceindex = tag.IndexOf(' ');
        if (spaceindex > 0)
            tag = tag.Substring(0, spaceindex);
        return String.Concat('<', tag, value.Substring(endindex));
    }

    public static string FnRemoveHTML(string sString)
    {
        System.Text.RegularExpressions.Regex oRegEx = new 
        (
            "<!*[^<>]*>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |System.Text.RegularExpressions.RegexOptions.Multiline
        );



        /*
            .Pattern = 
            .Global = True
            .IgnoreCase = True
            .MultiLine = True
            */
        var result = oRegEx.Replace(sString, "");
        result = result.Replace("&nbsp;", "\n").Replace("&quot;", "'").Replace("\\", "<\\");
            result = result.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&");
            
        //result = FnRemoveLineBreak(result);

        return result;
    /* 
        FnRemoveHTML = oRegEx.Replace(sString, "");
        FnRemoveHTML = Replace(Replace(Replace(FnRemoveHTML, "&nbsp;", vbCrLf, 1, -1), "&quot;", "'", 1, -1), "\\", "<\\", 1, -1);
        FnRemoveHTML = Replace(Replace(Replace(FnRemoveHTML, "&gt;", ">", 1, -1), "&lt;", "<", 1, -1), "&amp;", "&", 1, -1);
        FnRemoveHTML = FnRemoveLineBreak(FnRemoveHTML);
    
        Error_Handler_Exit:
        On Error Resume Next
        Set oRegEx = Nothing*/
    }

}

