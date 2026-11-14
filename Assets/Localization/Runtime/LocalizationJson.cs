using System.Collections.Generic;
using System.Text;

namespace Localization
{
    public static class LocalizationJson
    {
        public static Dictionary<string, string> Parse(string json)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json))
                return result;

            int i = 0;
            Skip(json, ref i);
            if (i >= json.Length || json[i] != '{')
                return result;
            i++;

            while (i < json.Length)
            {
                Skip(json, ref i);
                if (i >= json.Length || json[i] == '}')
                    break;
                string key = ReadString(json, ref i);
                Skip(json, ref i);
                if (i < json.Length && json[i] == ':')
                    i++;
                Skip(json, ref i);
                string value = ReadString(json, ref i);
                if (!string.IsNullOrEmpty(key))
                    result[key] = value;
                Skip(json, ref i);
                if (i < json.Length && json[i] == ',')
                    i++;
            }

            return result;
        }

        static void Skip(string json, ref int i)
        {
            while (i < json.Length && (json[i] == ' ' || json[i] == '\n' || json[i] == '\r' || json[i] == '\t'))
                i++;
        }

        static string ReadString(string json, ref int i)
        {
            if (i >= json.Length || json[i] != '"')
                return string.Empty;
            i++;
            StringBuilder builder = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i++];
                if (c == '"')
                    break;
                if (c == '\\' && i < json.Length)
                {
                    char esc = json[i++];
                    switch (esc)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (i + 4 <= json.Length)
                            {
                                int code = 0;
                                for (int n = 0; n < 4; n++)
                                {
                                    code <<= 4;
                                    char h = json[i++];
                                    if (h >= '0' && h <= '9') code += h - '0';
                                    else if (h >= 'a' && h <= 'f') code += h - 'a' + 10;
                                    else if (h >= 'A' && h <= 'F') code += h - 'A' + 10;
                                }
                                builder.Append((char)code);
                            }
                            break;
                        default:
                            builder.Append(esc);
                            break;
                    }
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
    }
}
