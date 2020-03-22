using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caight
{
    public enum RequestId : int
    {
        Unknown = -1,

        EvaluateEmail = 0,

        RegisterEmail = 1,

        VerifyEmailWebOnly = 2,

        SignIn = 3,

        NewGroup = 4,
        NewCat = 5,

        DownloadEntity = 6,

        ChangeName = 7,

        Logout = 8,
    }
}
