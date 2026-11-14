using System.Collections.Generic;
using System.Text;

namespace Localization
{
    public static class ArabicShaper
    {
        enum JoinKind
        {
            None,
            Right,
            Dual,
            Transparent
        }

        struct Form
        {
            public JoinKind kind;
            public char isolated;
            public char final;
            public char initial;
            public char medial;

            public Form(JoinKind kind, char isolated, char final, char initial, char medial)
            {
                this.kind = kind;
                this.isolated = isolated;
                this.final = final;
                this.initial = initial;
                this.medial = medial;
            }
        }

        static readonly Dictionary<char, Form> Forms = BuildForms();

        public static bool ContainsArabic(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c >= 0x0600 && c <= 0x06FF)
                    return true;
            }
            return false;
        }

        public static string Shape(string value)
        {
            if (string.IsNullOrEmpty(value) || !ContainsArabic(value))
                return value;

            char[] source = value.ToCharArray();
            StringBuilder shaped = new StringBuilder(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                char current = source[i];
                if (!Forms.TryGetValue(current, out Form form) || form.kind == JoinKind.Transparent)
                {
                    shaped.Append(current);
                    continue;
                }

                int nextIndex = NextJoiner(source, i + 1);
                if (current == '\u0644' && nextIndex >= 0 && IsAlef(source[nextIndex]))
                {
                    bool previous = ConnectsForward(source, PreviousJoiner(source, i - 1));
                    shaped.Append(LamAlef(source[nextIndex], previous));
                    i = nextIndex;
                    continue;
                }

                bool joinPrev = form.kind != JoinKind.None && ConnectsForward(source, PreviousJoiner(source, i - 1));
                bool joinNext = form.kind == JoinKind.Dual && ConnectsBackward(source, nextIndex);
                if (joinPrev && joinNext && form.medial != '\0')
                    shaped.Append(form.medial);
                else if (joinPrev && form.final != '\0')
                    shaped.Append(form.final);
                else if (joinNext && form.initial != '\0')
                    shaped.Append(form.initial);
                else
                    shaped.Append(form.isolated == '\0' ? current : form.isolated);
            }

            return ReversePreservingRuns(shaped.ToString());
        }

        static string ReversePreservingRuns(string value)
        {
            char[] chars = value.ToCharArray();
            System.Array.Reverse(chars);
            int i = 0;
            while (i < chars.Length)
            {
                if (!IsLtrRun(chars[i]))
                {
                    i++;
                    continue;
                }
                int start = i;
                while (i < chars.Length && IsLtrRun(chars[i]))
                    i++;
                System.Array.Reverse(chars, start, i - start);
            }
            return new string(chars);
        }

        static bool IsLtrRun(char c)
        {
            return (c >= '0' && c <= '9')
                || (c >= 'A' && c <= 'Z')
                || (c >= 'a' && c <= 'z')
                || c == '%'
                || c == '+'
                || c == '#'
                || c == '@';
        }

        static bool IsAlef(char c)
        {
            return c == '\u0622' || c == '\u0623' || c == '\u0625' || c == '\u0627';
        }

        static char LamAlef(char alef, bool previous)
        {
            if (alef == '\u0622') return previous ? '\uFEF6' : '\uFEF5';
            if (alef == '\u0623') return previous ? '\uFEF8' : '\uFEF7';
            if (alef == '\u0625') return previous ? '\uFEFA' : '\uFEF9';
            return previous ? '\uFEFC' : '\uFEFB';
        }

        static int NextJoiner(char[] source, int index)
        {
            for (int i = index; i < source.Length; i++)
            {
                if (KindOf(source[i]) != JoinKind.Transparent)
                    return i;
            }
            return -1;
        }

        static int PreviousJoiner(char[] source, int index)
        {
            for (int i = index; i >= 0; i--)
            {
                if (KindOf(source[i]) != JoinKind.Transparent)
                    return i;
            }
            return -1;
        }

        static bool ConnectsForward(char[] source, int index)
        {
            if (index < 0)
                return false;
            return KindOf(source[index]) == JoinKind.Dual;
        }

        static bool ConnectsBackward(char[] source, int index)
        {
            if (index < 0)
                return false;
            JoinKind kind = KindOf(source[index]);
            return kind == JoinKind.Dual || kind == JoinKind.Right;
        }

        static JoinKind KindOf(char c)
        {
            if (IsTransparent(c))
                return JoinKind.Transparent;
            if (Forms.TryGetValue(c, out Form form))
                return form.kind;
            return JoinKind.None;
        }

        static bool IsTransparent(char c)
        {
            return (c >= '\u064B' && c <= '\u065F')
                || c == '\u0670'
                || (c >= '\u06D6' && c <= '\u06ED')
                || c == '\u200D';
        }

        static Dictionary<char, Form> BuildForms()
        {
            Dictionary<char, Form> map = new Dictionary<char, Form>();
            Add(map, '\u0621', JoinKind.None, '\uFE80', '\0', '\0', '\0');
            Add(map, '\u0622', JoinKind.Right, '\uFE81', '\uFE82', '\0', '\0');
            Add(map, '\u0623', JoinKind.Right, '\uFE83', '\uFE84', '\0', '\0');
            Add(map, '\u0624', JoinKind.Right, '\uFE85', '\uFE86', '\0', '\0');
            Add(map, '\u0625', JoinKind.Right, '\uFE87', '\uFE88', '\0', '\0');
            Add(map, '\u0626', JoinKind.Dual, '\uFE89', '\uFE8A', '\uFE8B', '\uFE8C');
            Add(map, '\u0627', JoinKind.Right, '\uFE8D', '\uFE8E', '\0', '\0');
            Add(map, '\u0628', JoinKind.Dual, '\uFE8F', '\uFE90', '\uFE91', '\uFE92');
            Add(map, '\u0629', JoinKind.Right, '\uFE93', '\uFE94', '\0', '\0');
            Add(map, '\u062A', JoinKind.Dual, '\uFE95', '\uFE96', '\uFE97', '\uFE98');
            Add(map, '\u062B', JoinKind.Dual, '\uFE99', '\uFE9A', '\uFE9B', '\uFE9C');
            Add(map, '\u062C', JoinKind.Dual, '\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0');
            Add(map, '\u062D', JoinKind.Dual, '\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4');
            Add(map, '\u062E', JoinKind.Dual, '\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8');
            Add(map, '\u062F', JoinKind.Right, '\uFEA9', '\uFEAA', '\0', '\0');
            Add(map, '\u0630', JoinKind.Right, '\uFEAB', '\uFEAC', '\0', '\0');
            Add(map, '\u0631', JoinKind.Right, '\uFEAD', '\uFEAE', '\0', '\0');
            Add(map, '\u0632', JoinKind.Right, '\uFEAF', '\uFEB0', '\0', '\0');
            Add(map, '\u0633', JoinKind.Dual, '\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4');
            Add(map, '\u0634', JoinKind.Dual, '\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8');
            Add(map, '\u0635', JoinKind.Dual, '\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC');
            Add(map, '\u0636', JoinKind.Dual, '\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0');
            Add(map, '\u0637', JoinKind.Dual, '\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4');
            Add(map, '\u0638', JoinKind.Dual, '\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8');
            Add(map, '\u0639', JoinKind.Dual, '\uFEC9', '\uFECA', '\uFECB', '\uFECC');
            Add(map, '\u063A', JoinKind.Dual, '\uFECD', '\uFECE', '\uFECF', '\uFED0');
            Add(map, '\u0641', JoinKind.Dual, '\uFED1', '\uFED2', '\uFED3', '\uFED4');
            Add(map, '\u0642', JoinKind.Dual, '\uFED5', '\uFED6', '\uFED7', '\uFED8');
            Add(map, '\u0643', JoinKind.Dual, '\uFED9', '\uFEDA', '\uFEDB', '\uFEDC');
            Add(map, '\u0644', JoinKind.Dual, '\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0');
            Add(map, '\u0645', JoinKind.Dual, '\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4');
            Add(map, '\u0646', JoinKind.Dual, '\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8');
            Add(map, '\u0647', JoinKind.Dual, '\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC');
            Add(map, '\u0648', JoinKind.Right, '\uFEED', '\uFEEE', '\0', '\0');
            Add(map, '\u0649', JoinKind.Right, '\uFEEF', '\uFEF0', '\0', '\0');
            Add(map, '\u064A', JoinKind.Dual, '\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4');
            Add(map, '\u0671', JoinKind.Right, '\uFB50', '\uFB51', '\0', '\0');
            Add(map, '\u067E', JoinKind.Dual, '\uFB56', '\uFB57', '\uFB58', '\uFB59');
            Add(map, '\u0686', JoinKind.Dual, '\uFB7A', '\uFB7B', '\uFB7C', '\uFB7D');
            Add(map, '\u06A9', JoinKind.Dual, '\uFB8E', '\uFB8F', '\uFB90', '\uFB91');
            Add(map, '\u06AF', JoinKind.Dual, '\uFB92', '\uFB93', '\uFB94', '\uFB95');
            Add(map, '\u06CC', JoinKind.Dual, '\uFBFC', '\uFBFD', '\uFBFE', '\uFBFF');
            return map;
        }

        static void Add(Dictionary<char, Form> map, char c, JoinKind kind, char isolated, char final, char initial, char medial)
        {
            map[c] = new Form(kind, isolated, final, initial, medial);
        }
    }
}
