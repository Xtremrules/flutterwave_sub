﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace flutterwave_sub.JsonModel
{
    public class CardDetails
    {
        [Required]
        public string cardno { get; set; }
        [Required]
        public string cvv { get; set; }
        [Required]
        public string expirymonth { get; set; }
        [Required]
        public string expiryyear { get; set; }
        [Required]
        public string pin { get; set; }

        public int serviceId { get; set; }
    }
    public class CardPayDetails : CardDetails
    {
        public string PBFPubKey { get; set; }
        public const string suggested_auth = "PIN";
        public string amount { get; set; }
        public string email { get; set; }
        public string phonenumber { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string IP { get; set; }
        /// <summary>
        /// Generated by the server
        /// </summary>
        public string txRef { get; set; }
        /// <summary>
        /// device fingerprint of the user's device
        /// </summary>
        public string device_fingerprint { get; set; }
        /// <summary>
        /// This helps identify the frequency of the recurring debit it can be set to the follow possible values:
        /// 
        /// recurring-daily, recurring-weekly, recurring-monthly, recurring-quarterly, recurring-bianually, recurring-anually
        /// </summary>
        public string charge_type { get; set; }
        /// <summary>
        /// Plan ID
        /// </summary>
        public string payment_plan { get; set; }
    }
}