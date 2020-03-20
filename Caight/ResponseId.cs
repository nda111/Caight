using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caight
{
    public enum ResponseId : int
    {
        Unknown = -1,

        /*
         * EvaluateEmail = 0
         */
        UnknownEmail = 0,
        RegisteredEmail = 1,
        VerifiedEmail = 2,

        /*
         * RegisterEmail = 1
         */
        RegisterOk = 3,
        RegisterNo = 4,

        /*
         * VerifyEmailWebOnly = 2
         */
        VerifyOkWebOnly = 5,
        VerifyNoWebOnly = 6,

        /*
         * SignIn = 3
         */
        SignInOk = 7,
        SignInWrongPassword = 8,
        SignInError = 9,

        /*
         * NewGroup = 4
         * NewCat = 5
         */
        AddEntityOk = 10,
        AddEntityNo = 11,
        AddEntityNotPw = 12,
        AddEntityError = 13,

        /*
         * DownloadEntity = 7
         */
        EntityGroup = 14,
        EntityCat = 15,
        EndOfEntity = 16,
    }
}
