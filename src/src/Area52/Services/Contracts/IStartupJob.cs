using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Contracts;

public interface IStartupJob
{
    ValueTask Execute(CancellationToken cancellationToken);
}
