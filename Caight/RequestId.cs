using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caight
{
    internal enum RequestId : int
    {
        Unknown = -1,

        EvaluateEmail = 0,
        RegisterEmail = 1,

        CertifyEmail = 2,
    }
}
