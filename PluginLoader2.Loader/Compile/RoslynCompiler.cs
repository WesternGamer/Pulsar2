using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Avalonia.Generators.NameGenerator;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace PluginLoader2.Loader.Compile;

class RoslynCompiler
{
    private readonly List<Source> source = [];
    private readonly bool debugBuild;
    private readonly List<ISourceGenerator> generators = [];
    private readonly List<AdditionalText> avaloniaXaml = [];

    private static readonly string[] ImplicitUsings = [ "System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Net.Http", "System.Threading", "System.Threading.Tasks" ];


    public RoslynCompiler(bool debugBuild = false)
    {
        this.debugBuild = debugBuild;
    }

    public void AddSource(Stream stream, string fileName)
    {
        source.Add(new Source(stream, fileName, debugBuild));
    }
    public void AddAvaloniaXaml(Stream stream, string fileName)
    {
        if(generators.Count == 0)
            generators.Add(new AvaloniaNameSourceGenerator());
        avaloniaXaml.Add(new AvaloniaAdditionalText(stream, fileName));
    }

    public void AddImplicitUsings()
    {
        StringBuilder sb = new StringBuilder();
        foreach (string s in ImplicitUsings)
        {
            sb.Append("global using global::").Append(s).Append(';').AppendLine();
        }
        SourceText sourceText = SourceText.From(sb.ToString());
        source.Add(new Source(sourceText, null));
    }


    public void Compile(string assemblyName, CompilerReferences references, Stream assemblyOutput, Stream debugSymbolOutput = null)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: source.Select(x => x.Tree),
            references: references.GetReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: debugBuild ? OptimizationLevel.Debug : OptimizationLevel.Release,
                allowUnsafe: true));

        if (generators.Count > 0)
        {
            var generator = CSharpGeneratorDriver.Create(generators, avaloniaXaml, null, new AvaloniaTextOptionProvider());
            generator.RunGeneratorsAndUpdateCompilation(compilation, out var afterGenCompilation, out var genDiagnostics);
            if(afterGenCompilation is CSharpCompilation afterGenCs)
                compilation = afterGenCs;
        }

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
        public Source(SourceText source, string name)
        {
            Name = name;
            Tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        }
    }

    private class AvaloniaTextOptionProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new NullTextOptions();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return GlobalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            if (textFile is AvaloniaAdditionalText avaFile)
                return avaFile.Options;
            return GlobalOptions;
        }
    }

    private class NullTextOptions : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
        {
            value = null;
            return false;
        }
    }

    private class AvalonaTextOptions : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
        {
            if (key == "build_metadata.AdditionalFiles.SourceItemGroup")
            {
                value = "AvaloniaXaml";
                return true;
            }

            value = null;
            return false;
        }
    }

    private class AvaloniaAdditionalText : AdditionalText
    {
        private readonly SourceText content;

        public override string Path { get; }
        public AnalyzerConfigOptions Options { get; set; } = new AvalonaTextOptions();

        public AvaloniaAdditionalText(Stream s, string name)
        {
            Path = name;
            content = SourceText.From(s);
        }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return content;
        }
    }
}
