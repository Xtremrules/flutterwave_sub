using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace flutterwave_sub.JsonModel
{
    public class PaymentPlan
    {
        public int id { get; set; }
        public string name { get; set; }
        public string amount { get; set; }
        public string interval { get; set; }
        public int duration { get; set; }
        public string status { get; set; }
        public string currency { get; set; }
        public string plan_token { get; set; }
        public DateTime date_created { get; set; }
    }

    public class PaymentPlanObject
    {
        public string status { get; set; }
        public string message { get; set; }
        public PaymentPlan data { get; set; }
    }
}