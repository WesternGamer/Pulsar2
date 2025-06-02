using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pulsar2.Loader.Compile;

class CompilerReferences
{
    private readonly List<MetadataReference> references = new();

    internal IEnumerable<MetadataReference> GetReferences()
    {
        return GlobalReferences.GetReferences().Concat(references);
    }

    public void TryAddDependency(string dll) // TODO: Handle native references
    {
        if (Path.HasExtension(dll)
            && Path.GetExtension(dll).Equals(".dll", StringComparison.OrdinalIgnoreCase)
            && File.Exists(dll))
        {
            try
            {
                MetadataReference reference = MetadataReference.CreateFromFile(dll);
                if (reference != null)
                {
                    Log.Info("Custom compiler reference: " + (reference.Display ?? dll));
                    references.Add(reference);
                }
            }
            catch
            { }
        }
    }
}
