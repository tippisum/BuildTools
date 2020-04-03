using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;

namespace CampAI.BuildTools {
	public static class Builtin {
		[Tool(Name = "csc", Description = "C# compiler")]
		public static void Csc(
			[Arg(Description = "Output file", ValueName = "FILE")]
			string @out,
			[Arg(Description = "Source files", Flags = ActionArgFlags.Required | ActionArgFlags.Positional)]
			params string[] sources
		) {
			CSharpCodeProvider csp;
			CompilerResults result;
			CompilerParameters options;
			Assembly asm;
			List<string> rc;
			CompilerErrorCollection cec;
			CompilerError ce;
			object[] oa;
			int i;
			csp = new CSharpCodeProvider();
			rc = new List<string>();
			rc.Add(Assembly.GetExecutingAssembly().Location);
			asm = Assembly.LoadWithPartialName("Mono.Cecil.dll");
			if (asm != null) { rc.Add(asm.Location); }
			options = new CompilerParameters(rc.ToArray(), @out);
			result = csp.CompileAssemblyFromFile(options, sources);
			cec = result.Errors;
			oa = new object[5];
			for (i = 0; i < cec.Count; ++i) {
				ce = cec[i];
				oa[0] = ce.FileName;
				oa[1] = ce.Line;
				oa[2] = ce.Column;
				oa[3] = ce.ErrorNumber;
				oa[4] = ce.ErrorText;
				Console.Write("{0} ({1},{2}): {3}: {4}\n", oa);
			}
		}
	}
}
