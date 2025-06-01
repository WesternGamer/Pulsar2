using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Build.Tasks;
using Avalonia.Generators.NameGenerator;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Utilities;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualBasic;
using Serilog.Events;

namespace PluginLoader2.Loader.Compile;

// This class is very unpolished and needs to be redone
internal class AvaloniaCompiler
{
    private readonly List<AvaloniaAdditionalText> additionalTexts = [];
    private readonly ResourceGenerator resourceGenerator = new ResourceGenerator();

    public AvaloniaCompiler()
    {

    }

    public void AddXaml(Stream stream, string fileName)
    {
        if(!stream.CanSeek)
        {
            MemoryStream  mem = new MemoryStream();
            stream.CopyTo(mem);
            stream = mem;
        }
        additionalTexts.Add(new AvaloniaAdditionalText(stream, fileName));
    }

    public void Generate(ref CSharpCompilation compilation)
    {
        var generator = CSharpGeneratorDriver.Create([new AvaloniaNameSourceGenerator()], additionalTexts, null, new AvaloniaTextOptionProvider());
        generator.RunGeneratorsAndUpdateCompilation(compilation, out var afterGenCompilation, out var genDiagnostics);
        if (afterGenCompilation is CSharpCompilation afterGenCs)
            compilation = afterGenCs;
    }

    public static void InjectIl(string dll)
    {
        Type t = typeof(XamlIlRuntimeHelpers);
        Assembly.Load(new AssemblyName("System.ComponentModel.TypeConverter, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));

        string dllFolder = Path.GetDirectoryName(dll);
        string outDll = Path.Combine(dllFolder, Path.GetFileNameWithoutExtension(dll) + ".out.dll");
        IEnumerable<string> references = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location)).Select(x => x.Location);
        var result = XamlCompilerTaskExecutor.Compile(new XamlBuildEngine(), dll, outDll, null, null, references.ToArray(), dllFolder, true, true, MessageImportance.High, null, false);
    }


    public void GetResources(List<ResourceDescription> resources)
    {
        resources.Add(new ResourceDescription("!AvaloniaResources", GetResourceStream, true));
    }

    private Stream GetResourceStream()
    {
        MemoryStream mem = new MemoryStream();
        resourceGenerator.Execute(additionalTexts, mem);
        mem.Position = 0;
        return mem;
    }

    private class AvaloniaAdditionalText : AdditionalText
    {
        private readonly SourceText content;

        public override string Path { get; }
        public AnalyzerConfigOptions Options { get; set; } = new AvalonaTextOptions();
        public string Content { get; }

        public AvaloniaAdditionalText(Stream s, string name)
        {
            Path = name;
            content = SourceText.From(s);
            s.Position = 0;
            using StreamReader sr = new StreamReader(s);
            Content = sr.ReadToEnd();
        }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return content;
        }
    }

    #region Resources
    private class ResourceGenerator
    {
        private class Source
        {
            private byte[] data;

            public Source(AvaloniaAdditionalText s) : this(s.Path, Encoding.UTF8.GetBytes(s.Content))
            {
            }

            public Source(string path, byte[] bytes)
            {
                Path = path;
                data = bytes;
                Size = data.Length;
            }

            public string Path { get; }
            public int Size { get; }

            internal Stream Open()
            {
                return new MemoryStream(data, false);
            }
        }

        private bool PreProcessXamlFiles(IEnumerable<AvaloniaAdditionalText> xaml, List<Source> sources)
        {
            Dictionary<string, string> typeToXamlIndex = [];

            foreach (AvaloniaAdditionalText s in xaml)
            {
                sources.Add(new Source(s));
                if (s.Path.ToLowerInvariant().EndsWith(".xaml") || s.Path.ToLowerInvariant().EndsWith(".paml") || s.Path.ToLowerInvariant().EndsWith(".axaml"))
                {
                    XamlFileInfo info;
                    try
                    {
                        info = XamlFileInfo.Parse(s.Content);
                    }
                    catch (Exception e)
                    {
                        //BuildEngine.LogError(BuildEngineErrorCode.InvalidXAML, s.SystemPath, "File doesn't contain valid XAML: " + e);
                        return false;
                    }

                    if (info.XClass != null)
                    {
                        if (typeToXamlIndex.ContainsKey(info.XClass))
                        {

                            //BuildEngine.LogError(BuildEngineErrorCode.DuplicateXClass, s.SystemPath, $"Duplicate x:Class directive, {info.XClass} is already used in {typeToXamlIndex[info.XClass]}");
                            return false;
                        }
                        typeToXamlIndex[info.XClass] = s.Path;
                    }
                }
            }

            AvaloniaResourceXamlInfo xamlInfo = new AvaloniaResourceXamlInfo
            {
                ClassToResourcePathIndex = typeToXamlIndex
            };
            using MemoryStream ms = new MemoryStream();
            new DataContractSerializer(typeof(AvaloniaResourceXamlInfo)).WriteObject(ms, xamlInfo);
            ms.Position = 0;
            sources.Add(new Source("/!AvaloniaResourceXamlInfo", ms.ToArray()));
            return true;
        }

        public bool Execute(IEnumerable<AvaloniaAdditionalText> xaml, Stream output)
        {
            //BuildEngine.LogMessage($"GenerateAvaloniaResourcesTask -> Root: {Root}, {Resources?.Count()} resources, Output:{Output}", _reportImportance < MessageImportance.Low ? MessageImportance.High : _reportImportance);
            List<Source> sources = new List<Source>();
            if (!PreProcessXamlFiles(xaml, sources))
                return false;

            AvaloniaResourcesIndexReaderWriter.WriteResources(
                output,
                [.. sources.Select(source => (source.Path, source.Size, (Func<Stream>)source.Open))]);
            return true;
        }
    }

    [DataContract]
    private class AvaloniaResourceXamlInfo
    {
        [DataMember]
        public Dictionary<string, string> ClassToResourcePathIndex { get; set; } = [];

    }
    #endregion

    #region Compile Xaml
    private class XamlBuildEngine : IBuildEngine
    {
        public bool ContinueOnError { get; } = false;

        public int LineNumberOfTaskNode { get; }

        public int ColumnNumberOfTaskNode { get; }

        public string ProjectFileOfTaskNode { get; }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            Log.Info($"[AvaloniaXaml] {e.Message}");
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            Log.Error($"[AvaloniaXaml] {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            Log.Info($"[AvaloniaXaml] {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            Log.Warn($"[AvaloniaXaml] {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
        }
    }
    #endregion

    #region Source Generator
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

    #endregion

}
