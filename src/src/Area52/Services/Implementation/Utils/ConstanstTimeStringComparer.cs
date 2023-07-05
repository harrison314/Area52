using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Area52.Services.Implementation.Utils;

internal class ConstanstTimeStringComparer : IEqualityComparer<string>
{
    public ConstanstTimeStringComparer()
    {

    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public bool Equals(string? x, string? y)
    {
        if (object.ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;
        if (x.Length != y.Length) return false;

        int result = 0;
        for (int i = 0; i < x.Length; i++)
        {
            result |= x[i] ^ y[i];
        }

        return result == 0;
    }

    public int GetHashCode([DisallowNull] string obj)
    {
        return obj.GetHashCode();
    }
}
