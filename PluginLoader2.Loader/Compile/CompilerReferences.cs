using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PluginLoader2.Loader.Compile;

class CompilerReferences
{
    private readonly List<MetadataReference> references = new();

    internal IEnumerable<MetadataReference> GetReferences()
    {
        return GlobalReferences.GetReferences().Concat(references);
    }
}
