// Hearth & Hollow — refactor 03: turns a GameContent into C# source (object initialisers, only the fields that
// differ from a type's defaults) and dumps one as text for exact comparison. Used once to collapse the 44 content
// upgrade steps into the current design; kept so content can be snapshotted to code again if it ever drifts.
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using HearthHollow.Core;

namespace HearthHollow.EditorTools
{
    public static class ContentCodegen
    {
        const BindingFlags F = BindingFlags.Public | BindingFlags.Instance;

        // ───── exact text dump (for "is it identical?") ─────
        public static string Dump(object o)
        {
            var sb = new StringBuilder();
            DumpInto(sb, o, "");
            return sb.ToString();
        }

        static void DumpInto(StringBuilder sb, object o, string path)
        {
            if (o == null) { sb.Append(path).Append(" = null\n"); return; }
            var t = o.GetType();
            if (IsLeaf(t)) { sb.Append(path).Append(" = ").Append(Leaf(o)).Append('\n'); return; }
            if (o is IList list)
            {
                sb.Append(path).Append(".Count = ").Append(list.Count).Append('\n');
                for (int i = 0; i < list.Count; i++) DumpInto(sb, list[i], path + "[" + i + "]");
                return;
            }
            foreach (var f in t.GetFields(F)) DumpInto(sb, f.GetValue(o), path + "." + f.Name);
        }

        static bool IsLeaf(Type t) { return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal); }

        static string Leaf(object o)
        {
            if (o is string s) return Str(s);
            if (o is float f) return f.ToString("R", CultureInfo.InvariantCulture) + "f";
            if (o is double d) return d.ToString("R", CultureInfo.InvariantCulture) + "d";
            if (o is bool b) return b ? "true" : "false";
            if (o is Enum e) return EnumLit(e);
            return Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        static string EnumLit(Enum e)
        {
            var t = e.GetType();
            if (Enum.IsDefined(t, e)) return TypeName(t) + "." + e;
            return "(" + TypeName(t) + ")" + Convert.ToInt64(e, CultureInfo.InvariantCulture);
        }

        static string Str(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.Append('"').ToString();
        }

        static string TypeName(Type t)
        {
            if (t == typeof(string)) return "string";
            if (t == typeof(int)) return "int";
            if (t == typeof(float)) return "float";
            if (t == typeof(bool)) return "bool";
            if (t.IsGenericType)
            {
                var args = t.GetGenericArguments();
                var parts = new List<string>();
                foreach (var a in args) parts.Add(TypeName(a));
                return t.Name.Substring(0, t.Name.IndexOf('`')) + "<" + string.Join(", ", parts.ToArray()) + ">";
            }
            return t.DeclaringType != null ? TypeName(t.DeclaringType) + "." + t.Name : t.Name;
        }

        // ───── C# source ─────
        static bool Same(object a, object b) { return Dump(a) == Dump(b); }

        /// `new T { field = value, … }` with only the fields that differ from `new T()`.
        public static string Init(object o, int indent)
        {
            if (o == null) return "null";
            var t = o.GetType();
            if (IsLeaf(t)) return Leaf(o);
            if (o is IList list) return ListInit(list, t, indent);
            object def = Activator.CreateInstance(t);
            var parts = new List<string>();
            foreach (var f in t.GetFields(F))
            {
                object v = f.GetValue(o), dv = f.GetValue(def);
                if (Same(v, dv)) continue;
                parts.Add(f.Name + " = " + Init(v, indent + 1));
            }
            string head = "new " + TypeName(t);
            if (parts.Count == 0) return head + "()";
            string one = head + " { " + string.Join(", ", parts.ToArray()) + " }";
            if (one.Length + indent * 4 <= 150 && one.IndexOf('\n') < 0) return one;
            string pad = new string(' ', (indent + 1) * 4), end = new string(' ', indent * 4);
            return head + "\n" + end + "{\n" + pad + string.Join(",\n" + pad, parts.ToArray()) + ",\n" + end + "}";
        }

        static string ListInit(IList list, Type t, int indent)
        {
            if (list.Count == 0) return "new " + TypeName(t) + "()";
            var items = new List<string>();
            foreach (var e in list) items.Add(Init(e, indent + 1));
            string one = "new " + TypeName(t) + " { " + string.Join(", ", items.ToArray()) + " }";
            if (one.Length + indent * 4 <= 150 && one.IndexOf('\n') < 0) return one;
            string pad = new string(' ', (indent + 1) * 4), end = new string(' ', indent * 4);
            return "new " + TypeName(t) + "\n" + end + "{\n" + pad + string.Join(",\n" + pad, items.ToArray()) + ",\n" + end + "}";
        }

        /// A method that fills `c.<field>` for each named list field (one Add per item), or sets a single object's fields.
        public static string Method(GameContent c, string method, string summary, params string[] fields)
        {
            var sb = new StringBuilder();
            sb.Append("        /// ").Append(summary).Append("\n");
            sb.Append("        static void ").Append(method).Append("(GameContent c)\n        {\n");
            var def = new GameContent();
            foreach (var name in fields)
            {
                var f = typeof(GameContent).GetField(name, F);
                object v = f.GetValue(c);
                if (v is IList list && !(v is string))
                {
                    if (list.Count == 0) continue;
                    sb.Append("            // ── ").Append(name).Append(" (").Append(list.Count).Append(") ──\n");
                    foreach (var e in list) sb.Append("            c.").Append(name).Append(".Add(").Append(Init(e, 3)).Append(");\n");
                }
                else if (v != null && !IsLeaf(v.GetType()))
                {
                    object dv = Activator.CreateInstance(v.GetType());
                    sb.Append("            var ").Append(name.Substring(0, 1)).Append(" = c.").Append(name).Append(";\n");
                    foreach (var sf in v.GetType().GetFields(F))
                    {
                        object x = sf.GetValue(v), dx = sf.GetValue(dv);
                        if (Same(x, dx)) continue;
                        sb.Append("            ").Append(name.Substring(0, 1)).Append('.').Append(sf.Name).Append(" = ").Append(Init(x, 3)).Append(";\n");
                    }
                }
                else if (!Same(v, f.GetValue(def)))
                    sb.Append("            c.").Append(name).Append(" = ").Append(Init(v, 3)).Append(";\n");
            }
            sb.Append("        }\n");
            return sb.ToString();
        }

        public static string File(string title, string body)
        {
            return "// Hearth & Hollow — default content: " + title + ".\n" +
                   "// The current design, written out directly (refactor 03 collapsed the old 44 upgrade steps into this).\n" +
                   "// Edit freely: these files ARE the design now. Pure C#, no UnityEngine.\n" +
                   "using System.Collections.Generic;\n\nnamespace HearthHollow.Core\n{\n    public static partial class DefaultContent\n    {\n" +
                   body + "    }\n}\n";
        }
    }
}
#endif
