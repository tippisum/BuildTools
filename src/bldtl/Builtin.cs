using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;

namespace CampAI.BuildTools {
	public static class Builtin {
		[Tool(Name = "csc", Description = "C# compiler")]
		public static void Csc(
			[Arg(Description = "Output file", ValueName = "FILE")]
			string @out,
			[Arg(Description = "Source files", Flags = ActionArgFlags.Required | ActionArgFlags.Positional)]
			params string[] sources
		) {
			int i;
			List<MetadataReference> references;
			EmitResult result;
			IReadOnlyList<Diagnostic> diagnostics;
			SyntaxTree[] trees;
			Diagnostic diagnostic;
			trees = new SyntaxTree[sources.Length];
			for (i = 0; i < sources.Length; ++i) {
				trees[i] = CSharpSyntaxTree.ParseText(File.ReadAllText(sources[i]));
			}
			references = new List<MetadataReference>();
			BuildTool.AddMetadataReference(references, "System.Private.CoreLib");
			BuildTool.AddMetadataReference(references, "System.Runtime");
			BuildTool.AddMetadataReference(references, "netstandard");
			BuildTool.AddMetadataReference(references, "System.Console");
			BuildTool.AddMetadataReference(references, "Mono.Cecil");
			BuildTool.AddMetadataReference(references, "builtin");
			result = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(@out), trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).Emit(@out);
			if (!result.Success) {
				diagnostics = result.Diagnostics;
				for (i = 0; i < diagnostics.Count; ++i) {
					diagnostic = diagnostics[i];
					if (diagnostic.Severity == DiagnosticSeverity.Error || diagnostic.IsWarningAsError) {
						Console.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
					}
				}
			}
		}
	}
}
