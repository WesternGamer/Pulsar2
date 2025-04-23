using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace PluginLoader2.Loader.Compile;

class RoslynCompiler
{
    private readonly List<Source> source = new List<Source>();
    private readonly CompilerReferences references;
    private readonly bool debugBuild;
    
    public RoslynCompiler(CompilerReferences references, bool debugBuild = false)
    {
        this.references = references;
        this.debugBuild = debugBuild;
    }

    public void AddSource(Stream stream, string fileName)
    {
        source.Add(new Source(stream, fileName, debugBuild));
    }

    public void Compile(string assemblyName, Stream assemblyOutput, Stream debugSymbolOutput)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: source.Select(x => x.Tree),
            references: references.GetReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: debugBuild ? OptimizationLevel.Debug : OptimizationLevel.Release,
                allowUnsafe: true));

        {
            // write IL code into memory
            EmitResult result;
            if (debugBuild)
            {
                result = compilation.Emit(assemblyOutput, debugSymbolOutput,
                    embeddedTexts: source.Select(x => x.Text),
                    options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: Path.ChangeExtension(assemblyName, "pdb")));
            }
            else
            {
                result = compilation.Emit(assemblyOutput);
            }

            if (!result.Success)
            {
                // handle exceptions
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Location location = diagnostic.Location;
                    Source source = this.source.FirstOrDefault(x => x.Tree == location.SourceTree);
                    LinePosition pos = location.GetLineSpan().StartLinePosition;
                    Log.Error($"{diagnostic.Id}: {diagnostic.GetMessage()} in file:\n{source?.Name ?? "null"} ({pos.Line + 1},{pos.Character + 1})");
                }
                throw new Exception("Compilation failed!");
            }
            else
            {
                if (debugBuild)
                    debugSymbolOutput.Seek(0, SeekOrigin.Begin);
                assemblyOutput.Seek(0, SeekOrigin.Begin);
            }
        }

    }

    private class Source
    {
        public string Name { get; }
        public SyntaxTree Tree { get; }
        public EmbeddedText Text { get; }

        public Source(Stream s, string name, bool includeText)
        {
            Name = name;
            SourceText source = SourceText.From(s, canBeEmbedded: includeText);
            if (includeText)
            {
                Text = EmbeddedText.FromSource(name, source);
                Tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), name);
            }
            else
            {
                Tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
            }
        }
    }
}
