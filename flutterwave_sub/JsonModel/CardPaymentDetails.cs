﻿using System.ComponentModel.DataAnnotations;

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
    }

    /// <summary>
    /// This will require pin and suggested_auth
    /// </summary>
    public class CardPayDetails_NotComplete : CardDetails
    {
        public string PBFPubKey { get; set; }
        public string amount { get; set; }
        public string email { get; set; }
        public string phonenumber { get; set; }
        public string firstname { get; set; }
        public string country { get; set; }// = "NG";
        public string currency { get; set; }// = "NGN";
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

    public class CardPayDetails_Pin : CardPayDetails_NotComplete
    {
        public string pin { get; set; }
        public string suggested_auth { get; set; }// "PIN";
    }

    public class CardPayDetails_Billing : CardPayDetails_NotComplete
    {
        public string suggested_auth { get; set; } //"AVS_VBVSECURECODE" || "NOAUTH_INTERNATIONAL";
        public string billingzip { get; set; }
        public string billingcity { get; set; }
        public string billingaddress { get; set; }
        public string billingstate { get; set; }
        public string billingcountry { get; set; }
    }

    public class BillingDetails
    {
        [Required]
        public string billingzip { get; set; }
        [Required]
        public string billingcity { get; set; }
        [Required]
        public string billingaddress { get; set; }
        [Required]
        public string billingstate { get; set; }
        [Required]
        public string billingcountry { get; set; }
    }
}