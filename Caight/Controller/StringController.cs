using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Caight.Controller
{
    public class StringController : Microsoft.AspNetCore.Mvc.Controller
    {
        public string Index()
        {
            return "This is default action...";
        }

        public string Reverse(string text)
        {
            return new string(text.Reverse().ToArray());
        }
    }
}