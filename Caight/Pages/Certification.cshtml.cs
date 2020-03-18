using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Caight.Pages
{
    public class CertificationModel : PageModel
    {
        public string Hash { get; set; } = null;

        public void OnGet()
        {
            Hash = Request.Query["q"];
        }
    }
}
