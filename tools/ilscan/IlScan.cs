// IlScan: reads a mod dll's metadata/IL with dnlib (the library tests\CommonTests.cs already uses; no new dependency).
// Standalone: no game or test sources are compiled in. Built by tools\lib\toolchain.ps1 Build-IlScan into tools\ilscan\IlScan.exe
 
//
//   IlScan types   <dll>               -> one full type name per line; compiler-generated types are prefixed "G:"
//   IlScan strings <dll>               -> "<kind>:<value>" per line, kind L = ldstr literal, C = constant string field,
//                                         A = custom attribute string argument, R = manifest resource name (CR/LF escaped)
//   IlScan calls   <dll> <Type::Name>  -> methods whose body references a method Type::Name (any overload), "Type::Method"
//   IlScan callees <dll> <Type::Name>  -> methods/fields referenced by the body of Type::Name (all overloads, direct only)
//   IlScan edges   <dll>               -> "Caller<TAB>Callee" for every method/field reference in every body
//   IlScan harmony <dll>               -> "<PatchType>\t<TargetType>\t<TargetMethod>\tprefix=<return type|->\tpostfix=<yes|no>"
//                                         per type carrying [HarmonyLib.HarmonyPatch]
// Exit: 0 = ok, 1 = input error (missing dll, method not defined for callees), 2 = usage.
// Type names: dnlib FullName ('/' for nested types). "Generated" = the type or a declaring type has a name starting with '<'
// (same rule as tests\CommonTests.cs IsGenType).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

static class IlScan
{
    const string HarmonyPatchAttr = "HarmonyLib.HarmonyPatch";
    const string HarmonyPrefixAttr = "HarmonyLib.HarmonyPrefix";
    const string HarmonyPostfixAttr = "HarmonyLib.HarmonyPostfix";

    static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2) return Usage();
            string cmd = args[0], dll = args[1];
            if (!File.Exists(dll)) { Console.Error.WriteLine("IlScan: dll not found: " + dll); return 1; }
            using (var module = ModuleDefMD.Load(File.ReadAllBytes(dll)))
            {
                switch (cmd)
                {
                    case "types": if (args.Length != 2) return Usage(); foreach (var l in Types(module)) Console.WriteLine(l); return 0;
                    case "strings": if (args.Length != 2) return Usage(); foreach (var l in Strings(module)) Console.WriteLine(l); return 0;
                    case "calls": if (args.Length != 3) return Usage(); foreach (var l in Calls(module, args[2])) Console.WriteLine(l); return 0;
                    case "callees":
                        {
                            if (args.Length != 3) return Usage();
                            var r = Callees(module, args[2]);
                            if (r == null) { Console.Error.WriteLine("IlScan: method not defined in " + dll + ": " + args[2]); return 1; }
                            foreach (var l in r) Console.WriteLine(l);
                            return 0;
                        }
                    case "edges": if (args.Length != 2) return Usage(); foreach (var l in Edges(module)) Console.WriteLine(l); return 0;
                    case "harmony": if (args.Length != 2) return Usage(); foreach (var l in Harmony(module)) Console.WriteLine(l); return 0;
                    default: return Usage();
                }
            }
        }
        catch (Exception e) { Console.Error.WriteLine("IlScan: " + e); return 1; }
    }

    static int Usage()
    {
        Console.Error.WriteLine("usage: IlScan (types|strings|edges|harmony) <dll> | IlScan (calls|callees) <dll> <Type::Method>");
        return 2;
    }

    static bool IsGen(string name) { return name != null && name.StartsWith("<", StringComparison.Ordinal); }
    static bool IsGenType(TypeDef t) { for (var d = t; d != null; d = d.DeclaringType) if (IsGen(d.Name)) return true; return false; }

    static IEnumerable<string> Types(ModuleDef module)
    {
        foreach (var t in module.GetTypes())
        {
            if (t.IsGlobalModuleType) continue;
            yield return (IsGenType(t) ? "G:" : "") + t.FullName;
        }
    }

    static string Esc(string s) { return s == null ? "<null>" : s.Replace("\r", "\\r").Replace("\n", "\\n"); }

    static IEnumerable<string> Strings(ModuleDef module)
    {
        var outp = new List<string>();
        Action<CustomAttributeCollection> attrs = cas =>
        {
            foreach (var ca in cas)
            {
                foreach (var a in ca.ConstructorArguments) AttrValue(a, outp);
                foreach (var na in ca.NamedArguments) AttrValue(na.Argument, outp);
            }
        };
        attrs(module.Assembly == null ? new CustomAttributeCollection() : module.Assembly.CustomAttributes);
        attrs(module.CustomAttributes);
        foreach (var t in module.GetTypes())
        {
            attrs(t.CustomAttributes);
            foreach (var f in t.Fields)
            {
                attrs(f.CustomAttributes);
                if (f.HasConstant && f.Constant.Value is string) outp.Add("C:" + Esc((string)f.Constant.Value));
            }
            foreach (var p in t.Properties) attrs(p.CustomAttributes);
            foreach (var ev in t.Events) attrs(ev.CustomAttributes);
            foreach (var m in t.Methods)
            {
                attrs(m.CustomAttributes);
                foreach (var pd in m.ParamDefs) attrs(pd.CustomAttributes);
                if (!m.HasBody) continue;
                foreach (var i in m.Body.Instructions)
                    if (i.OpCode.Code == Code.Ldstr) outp.Add("L:" + Esc((string)i.Operand));
            }
        }
        foreach (var r in module.Resources) outp.Add("R:" + Esc(r.Name));
        return outp;
    }

    static void AttrValue(CAArgument a, List<string> outp)
    {
        if (a.Value is UTF8String) outp.Add("A:" + Esc(((UTF8String)a.Value).String));
        else if (a.Value is string) outp.Add("A:" + Esc((string)a.Value));
        else if (a.Value is IList<CAArgument>) foreach (var x in (IList<CAArgument>)a.Value) AttrValue(x, outp);
        else if (a.Value is CAArgument) AttrValue((CAArgument)a.Value, outp);
    }

    static string Name(IMemberRef m) { return (m.DeclaringType == null ? "" : m.DeclaringType.FullName) + "::" + m.Name.String; }

    // Splits "Type::Method" at the last "::" (type names never contain "::").
    static void Split(string spec, out string type, out string name)
    {
        int k = spec.LastIndexOf("::", StringComparison.Ordinal);
        if (k <= 0 || k + 2 >= spec.Length) throw new ArgumentException("expected <Type::Method>, got '" + spec + "'");
        type = spec.Substring(0, k); name = spec.Substring(k + 2);
    }

    static IEnumerable<string> Calls(ModuleDef module, string spec)
    {
        string type, name; Split(spec, out type, out name);
        var hits = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var t in module.GetTypes())
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (var i in m.Body.Instructions)
                {
                    var im = i.Operand as IMethod;
                    if (im == null || im.DeclaringType == null || !im.IsMethod) continue;
                    if (im.DeclaringType.FullName == type && im.Name.String == name) { hits.Add(Name(m)); break; }
                }
            }
        return hits;
    }

    static IEnumerable<string> Callees(ModuleDef module, string spec)
    {
        string type, name; Split(spec, out type, out name);
        var td = module.Find(type, false);
        if (td == null) return null;
        var ms = td.Methods.Where(m => m.Name.String == name).ToList();
        if (ms.Count == 0) return null;
        var hits = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var m in ms)
        {
            if (!m.HasBody) continue;
            foreach (var i in m.Body.Instructions)
            {
                var mr = i.Operand as IMemberRef;
                if (mr == null || mr is ITypeDefOrRef || mr.DeclaringType == null) continue;
                if (mr is IMethod || mr is IField) hits.Add(Name(mr));
            }
        }
        return hits;
    }

    static IEnumerable<string> Edges(ModuleDef module)
    {
        var hits = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var t in module.GetTypes())
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (var i in m.Body.Instructions)
                {
                    var mr = i.Operand as IMemberRef;
                    if (mr == null || mr.DeclaringType == null) continue;
                    if (mr is ITypeDefOrRef) hits.Add(Name(m) + "\t" + ((ITypeDefOrRef)mr).FullName);
                    else if (mr is IMethod || mr is IField) hits.Add(Name(m) + "\t" + Name(mr));
                }
            }
        return hits;
    }

    static bool HasAttr(IHasCustomAttribute x, string full) { return x.CustomAttributes.Any(ca => ca.TypeFullName == full); }

    static IEnumerable<string> Harmony(ModuleDef module)
    {
        var outp = new List<string>();
        foreach (var t in module.GetTypes())
        {
            var pas = t.CustomAttributes.Where(ca => ca.TypeFullName == HarmonyPatchAttr).ToList();
            if (pas.Count == 0) continue;
            string target = null, method = null;
            foreach (var ca in pas)
                foreach (var a in ca.ConstructorArguments)
                {
                    if (target == null && a.Value is TypeSig) target = ((TypeSig)a.Value).FullName;
                    if (method == null && (a.Value is UTF8String || a.Value is string)) method = a.Value.ToString();
                }
            if (method == null && t.Methods.Any(m => m.Name.String == "TargetMethod" || m.Name.String == "TargetMethods")) method = "<TargetMethod()>";
            var prefix = t.Methods.FirstOrDefault(m => m.Name.String == "Prefix" || HasAttr(m, HarmonyPrefixAttr));
            bool postfix = t.Methods.Any(m => m.Name.String == "Postfix" || HasAttr(m, HarmonyPostfixAttr));
            outp.Add(t.FullName + "\t" + (target ?? "-") + "\t" + (method ?? "-") + "\tprefix=" + (prefix == null ? "-" : prefix.ReturnType.FullName) + "\tpostfix=" + (postfix ? "yes" : "no"));
        }
        return outp;
    }
}
