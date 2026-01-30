using System;

namespace mmria.common.util;

public static class TextAreaField
{
    public static string CleanUp(string value)
    {
        // This function can be used to correct any problems we find in the future
        // Currently it will do nothing and return the value passed in

        System.Text.RegularExpressions.Regex oRegEx = new 
        (
            "<!*[^<>]*>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |System.Text.RegularExpressions.RegexOptions.Multiline
        );

        var result = oRegEx.Replace
        (
            value.Replace("â€¢", "•")
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
                .TrimEnd('\r', '\n'),
                ""
        );

        // *** Removed the following from the  list
        // 	.Replace("\n", " ")
        // 	.Replace("\t", " ")
        //	.Replace("\\s+", " ")
        // 	.Replace("&nbsp;", " ")
        // 	.Replace("&amp;", "&")
        // 	.Replace("&quot;", "\"")
        // 	.Replace("&pos;", "'")
        // 	.Replace("&lt;", "<")
        // 	.Replace("&gt;", ">")
        // 	.Replace("&reg;", "Â®")
        // 	.Replace("&copy;", "Â©")
        // 	.Replace("&bull;", "•")
        // 	.Replace("&#39;", "\'")
        //	.Replace("<br>", "\n<br>")
        //	.Replace("<br", "\n<br")
        //	.Replace("<p", "\n<p")


        return result;
    }
}

