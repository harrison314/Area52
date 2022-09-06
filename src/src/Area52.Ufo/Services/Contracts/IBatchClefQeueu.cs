using System.Threading.Tasks;
using System.Threading;

namespace Area52.Ufo.Services.Contracts;

public interface IBatchClefQeueu
{
    void Insert(ClefObject clefObject);

    void Insert(string lines, int count);

    Task<bool> Wait(CancellationToken cancellationToken);

    string GetContent();
}
