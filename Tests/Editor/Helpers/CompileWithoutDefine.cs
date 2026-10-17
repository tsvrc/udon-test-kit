using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Tsvrc.UdonTestKit.Editor.Tests.Helpers
{
    // Compiles the sources of an assembly of this project with the references and defines Unity
    // compiles it with, but one define.
    internal static class CompileWithoutDefine
    {
        internal static CSharpCompilation Of(string assemblyName, string define)
        {
            UnityEditor.Compilation.Assembly assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Single(candidate => candidate.name == assemblyName);
            Assume.That(assembly.defines, Has.Member(define), $"{assemblyName} compiles without {define} already.");
            var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: assembly.defines.Where(other => other != define));
            return CSharpCompilation.Create(assemblyName,
                assembly.sourceFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), options, file)),
                assembly.allReferences.Select(reference => MetadataReference.CreateFromFile(reference)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        internal static string[] Errors(CSharpCompilation compilation)
        {
            return compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString()).ToArray();
        }
    }
}
