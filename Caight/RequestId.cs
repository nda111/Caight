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

        RequestResetPasswordUri = 9,

        ResetPasswordWebOnly = 10,

        ResetPasswordConfirmWebOnly = 11,

        DeleteAccount = 12,

        JoinGroup = 13,

        DownloadMember = 14,

        UpdateGroup = 15,

        DropGroup = 16,

        WithdrawGroup = 17,

        DropCat = 18,

        EditCat = 19,
    }
}
