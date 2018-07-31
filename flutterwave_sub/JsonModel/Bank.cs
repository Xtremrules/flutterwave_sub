using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace flutterwave_sub.JsonModel
{
    public class Bank
    {
        public string name { get; set; }
        public string code { get; set; }
        public string country { get; set; }
    }

    public class BankObject
    {
        public string status { get; set; }
        public string message { get; set; }
        public List<Bank> data { get; set; }
    }
}