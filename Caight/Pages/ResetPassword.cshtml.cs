using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Caight
{
    public class ResetPasswordModel : PageModel
    {
        public string Hash { get; private set; } = null;
        public string Password { get; private set; } = null;

        public void OnGet(string hash)
        {
            Hash = hash;
            Password = Request.Query["p"];
        }
    }
}