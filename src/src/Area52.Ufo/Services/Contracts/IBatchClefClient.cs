using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Area52.Ufo.Services.Contracts;

public interface IBatchClefClient
{
    int GetReamingCount();

    Task DirectSend(string clefLines, CancellationToken cancellationToken);
}
