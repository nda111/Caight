using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caight
{
    internal enum ResponseId : int
    {
        Unknown = -1,

        UnknownEmail = 0,
        RegisteredEmail = 1,
        CertifiedEmail = 2,
    }
}
