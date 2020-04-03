using CampAI.BuildTools;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;


[assembly: Tool(Name = "builtin", Description = "Builtin module")]

namespace CampAI.BuildTools {
	public static class BuildTool {
		public static int Main(string[] args) {
			Assembly module;
			MethodInfo action;
			Dictionary<string, ActionArg> named;
			List<ActionArg> positional;
			List<ActionArg> all;
			ActionArg arg;
			object[] va;
			string name;
			string value;
			object info = default;
			GroupCollection gc;
			Group g;
			int i;
			int p;
			bool nologo = default;
			bool nonamed = default;
			AddAssemblyPath(AppDomain.CurrentDomain.BaseDirectory);
			AddAssemblyPath(Path.GetDirectoryName(builtin.Location));
			AddAssemblyPath(Directory.GetCurrentDirectory());
			AssemblyLoadContext.Default.Resolving += ResolveAssembly;
			try {
				if (args.Length == 0 || rexHelp.Match(name = args[0]).Success) { goto help; }
				info = module = LoadModule(name);
				if (args.Length <= 1 || rexHelp.Match(name = args[1]).Success) { goto help; }
				info = action = GetActions(module)[name];
				GetActionArgs(action, all = new List<ActionArg>(), named = new Dictionary<string, ActionArg>(), positional = new List<ActionArg>());
				p = 0;
				for (i = 2; i < args.Length; ++i) {
					if (rexHelp.Match(value = args[i]).Success) { goto help; }
					if (!nonamed) {
						if (value == "-") {
							nonamed = true;
							continue;
						}
						if ((gc = rexNamed.Match(value).Groups)[0].Success) {
							name = gc[1].Value;
							value = (g = gc[2]).Success ? g.Value : null;
							if (name == "nologo") {
								if (value != null) { goto help; }
								nologo = true;
								continue;
							}
							arg = named[name];
							goto addv;
						}
					}
					if (p >= positional.Count) {
						if (p == 0 || !(arg = positional[p - 1]).IsList) { goto help; }
					} else {
						arg = positional[p++];
					}
					addv:
					arg.AddValue(value);
				}
				va = new object[all.Count];
				for (i = 0; i < all.Count; ++i) { va[i] = all[i].GetValue(); }
			} catch (Exception) {
				goto help;
			}
			if (!nologo) { PrintLogo(info); }
			try {
				action.Invoke(null, va);
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
				return 1;
			}
			return 0;
			help:
			PrintHelp(info);
			return 1;
		}

		internal static void AddMetadataReference(IList<MetadataReference> references, string name) {
			Assembly assembly = default;
			try {
				assembly = LoadModule(name);
			} catch { }
			if (assembly != null) { references.Add(MetadataReference.CreateFromFile(assembly.Location)); }
		}

		private static readonly Assembly builtin = Assembly.GetExecutingAssembly();
		private static List<string> assemblyPath = new List<string>();
		private static Regex rexNamed = new Regex("^[-/]([^:=]+)(?:[:=](.*))?$", RegexOptions.CultureInvariant);
		private static Regex rexHelp = new Regex("^(?:[-/][?h]|-{1,2}help)$", RegexOptions.CultureInvariant);

		private static void AddAssemblyPath(string path) {
			string full;
			full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
			if (Directory.Exists(full)) {
				if (!assemblyPath.Contains(full)) {
					assemblyPath.Add(full);
				}
			}
		}
		private static void FormatColumn(string left, string right) {
			string al;
			string bl;
			int len;
			al = null;
			bl = null;
			if (!String.IsNullOrEmpty(left)) {
				len = left.Length;
				if (len > 20) { len = 20; }
				al = left.Substring(0, len);
				left = left.Substring(len);
			}
			if (!String.IsNullOrEmpty(right)) {
				len = right.Length;
				if (len > 40) { len = 40; }
				bl = right.Substring(0, len);
				right = right.Substring(len);
			}
			Console.Write("{0,-25}{1}\n", al, bl);
			while (!String.IsNullOrEmpty(left) || !String.IsNullOrEmpty(right)) {
				al = null;
				bl = null;
				if (!String.IsNullOrEmpty(left)) {
					len = left.Length;
					if (len > 16) { len = 16; }
					al = left.Substring(0, len);
					left = left.Substring(len);
				}
				if (!String.IsNullOrEmpty(right)) {
					len = right.Length;
					if (len > 40) { len = 40; }
					bl = right.Substring(0, len);
					right = right.Substring(len);
				}
				Console.Write("    {0,-21}{1}\n", al, bl);
			}
		}
		private static void GetActionArgs(MethodInfo action, IList<ActionArg> all, IDictionary<string, ActionArg> named, IList<ActionArg> positional) {
			ParameterInfo[] pc;
			ActionArg arg;
			int i;
			pc = action.GetParameters();
			for (i = 0; i < pc.Length; ++i) {
				arg = ActionArg.CreateArg(pc[i]);
				if ((arg.Flags & ActionArgFlags.Positional) != 0) { positional.Add(arg); } else { named[arg.Name] = arg; }
				all.Add(arg);
			}
		}
		private static bool GetActionInfo(MethodInfo action, out string name, out string description) {
			ToolAttribute ta;
			name = null;
			description = null;
			ta = action.GetCustomAttribute<ToolAttribute>();
			if (ta == null) { return false; }
			name = ta.Name ?? action.Name;
			description = ta.Description;
			return true;
		}
		private static IDictionary<string, MethodInfo> GetActions(Assembly module) {
			Dictionary<string, MethodInfo> r;
			Type[] types;
			MethodInfo[] methods;
			MethodInfo method;
			ToolAttribute attr;
			string name;
			int i;
			int j;
			r = new Dictionary<string, MethodInfo>();
			types = module.GetExportedTypes();
			for (i = 0; i < types.Length; ++i) {
				methods = types[i].GetMethods(BindingFlags.Public | BindingFlags.Static);
				for (j = 0; j < methods.Length; ++j) {
					method = methods[j];
					attr = method.GetCustomAttribute<ToolAttribute>();
					if (attr == null) { continue; }
					name = attr.Name;
					if (name == null) { name = method.Name; }
					r.Add(name, method);
				}
			}
			return r;
		}
		private static void GetModuleInfo(Assembly module, out string name, out string description) {
			ToolAttribute attr;
			name = null;
			description = null;
			attr = module.GetCustomAttribute<ToolAttribute>();
			if (attr != null) {
				name = attr.Name;
				description = attr.Description;
			}
			if (name == null) { name = Path.GetFileName(module.Location); }
		}
		private static Assembly LoadModule(string module) {
			return module == "builtin"
				? builtin
				: !module.EndsWith(".dll") && !module.EndsWith(".exe") && module == Path.GetFileName(module)
				? Assembly.Load(module)
				: Assembly.LoadFrom(module);
		}
		private static void PrintHelp(object info) {
			Assembly module;
			MethodInfo action;
			SortedDictionary<string, ActionArg> named;
			List<ActionArg> positional;
			List<ActionArg> all;
			string s0;
			string s1;
			string s2;
			string s3;
			named = null;
			positional = null;
			s1 = null;
			s2 = null;
			PrintLogo(info);
			s0 = Path.GetFileName(builtin.Location);
			action = info as MethodInfo;
			if (action != null) {
				module = action.DeclaringType.Assembly;
				if (GetActionInfo(action, out s2, out s3)) { GetActionArgs(action, all = new List<ActionArg>(), named = new SortedDictionary<string, ActionArg>(), positional = new List<ActionArg>()); }
			} else {
				module = info as Assembly;
			}
			if (module != null) { GetModuleInfo(module, out s1, out s3); }
			if (s1 == null) { s1 = "<module>"; }
			if (s2 == null) { s2 = "<action>"; }
			Console.Write("usage: {0} {1} {2}", s0, s1, s2);
			if (named == null || named.Count != 0) { Console.Write(" [options]"); }
			if (positional != null && positional.Count != 0) {
				foreach (ActionArg arg in positional) {
					Console.Write((arg.Flags & ActionArgFlags.Required) != 0 ? " {0}" : " [{0}]", arg.Name);
					if (arg.IsList) { Console.Write(" ..."); }
				}
				Console.Write("\n\nPositional arguments:\n");
				foreach (ActionArg arg in positional) { FormatColumn(arg.Name, arg.Description); }
			} else {
				Console.Write("\n");
			}
			if (named != null && named.Count != 0) {
				Console.Write("\nOptions:\n");
				foreach (KeyValuePair<string, ActionArg> kv in named) {
					s0 = kv.Key;
					s1 = kv.Value.ValueName;
					FormatColumn(String.Format(String.IsNullOrEmpty(s1) ? "-{0}" : "-{0}:{1}", s0, s1), kv.Value.Description);
				}
			}
			if (action == null && module != null) {
				Console.Write("\nAvailable actions:\n");
				foreach (KeyValuePair<string, MethodInfo> kv in GetActions(module)) { if (GetActionInfo(kv.Value, out s0, out s1)) { FormatColumn(kv.Key, s1); } }
			}
			Console.Write("\n");
		}
		private static void PrintLogo(object info) {
			Assembly module;
			MethodInfo action;
			string name;
			string description;
			module = builtin;
			Console.Write("{0} {1}\n{2}\n", module.GetCustomAttribute<AssemblyTitleAttribute>().Title, module.GetName().Version, module.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
			if ((action = info as MethodInfo) == null) {
				if ((module = info as Assembly) == null) { return; }
				GetModuleInfo(module, out name, out description);
			} else {
				if (!GetActionInfo(action, out name, out description)) { return; }
			}
			Console.Write(description == null ? "\n{0}\n\n" : "\n{0}: {1}\n\n", name, description);
		}
		private static Assembly ResolveAssembly(AssemblyLoadContext context, AssemblyName name) {
			Assembly assembly = default;
			int i;
			if (assemblyPath != null) {
				for (i = 0; i < assemblyPath.Count; ++i) {
					try {
						assembly = context.LoadFromAssemblyPath(Path.GetFullPath(name.Name + ".dll", assemblyPath[i]));
						break;
					} catch { }
				}
			}
			return assembly;
		}
	}
}
