using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace flutterwave_sub.JsonModel
{
    public class AuthResponse
    {
        public string suggested_auth { get; set; }
    }

    public class AuthResponseObject
    {
        public string status { get; set; }
        public string message { get; set; }
        public AuthResponse data { get; set; }
    }
}