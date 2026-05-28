using System;
using System.IO;
using System.Net;
using System.Linq;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace mmria.services;

public sealed class CleanPath {

    public static String execute(String aString) 
    {
        if (aString == null) return null;
        String cleanString = "";
        for (int i = 0; i < aString.Length; ++i) 
        {
            cleanString += cleanChar(aString[i]);
        }
        return cleanString;
    }

    private static char cleanChar(char aChar) 
    {

        // 0 - 9
        for (int i = 48; i < 58; ++i) 
        {
            if (aChar == i) return (char) i;
        }

        // 'A' - 'Z'
        for (int i = 65; i < 91; ++i) 
        {
            if (aChar == i) return (char) i;
        }

        // 'a' - 'z'
        for (int i = 97; i < 123; ++i) 
        {
            if (aChar == i) return (char) i;
        }

        // other valid characters
        switch (aChar) 
        {
            case '/':
                return '/';
            case '.':
                return '.';
            case '-':
                return '-';
            case '_':
                return '_';
            case ' ':
                return ' ';
        }
        return '%';
    }
}
