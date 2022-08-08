using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if DEBUG
[assembly: InternalsVisibleTo("Area52.UnitTests")]
[assembly: InternalsVisibleTo("Area52.RavenDbTests")]
#endif
