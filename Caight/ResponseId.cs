using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caight
{
    public enum ResponseId : int
    {
        Unknown = -1,

        UnknownEmail = 0,
        RegisteredEmail = 1,
        VerifiedEmail = 2,

        RegisterOk = 3,
        RegisterNo = 4,

        VerifyOkWebOnly = 5,
        VerifyNoWebOnly = 6,

        SignInOk = 7,
        SignInWrongPassword = 8,
        SignInError = 9,
    }
}
