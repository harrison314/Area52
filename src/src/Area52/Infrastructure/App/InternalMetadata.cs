using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Infrastructure.App;

internal static class InternalMetadata
{
    public static string GetAssemblyMetadata(string name)
    {
        foreach (AssemblyMetadataAttribute attr in typeof(InternalMetadata).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attr.Key == name)
            {
                return attr.Value ?? string.Empty;
            }

        }

        throw new InvalidProgramException($"AssemblyMetadataAttribute with name {name} not found.");
    }
}
