namespace flutterwave_sub
{
    public static class EndPoints
    {
        /// <summary>
        /// Call this point to Charge card.
        /// You need to encrpt your payload
        /// Only this endpoint require encryption
        /// </summary>
        public const string charge = "https://ravesandboxapi.flutterwave.com/flwv3-pug/getpaidx/api/charge";
        /// <summary>
        /// Call this to validate charge
        /// </summary>
        public const string validateCharge = "https://ravesandboxapi.flutterwave.com/flwv3-pug/getpaidx/api/validatecharge";
        /// <summary>
        /// Call this to verify charge
        /// </summary>
        public const string verifyCharge = "https://ravesandboxapi.flutterwave.com/flwv3-pug/getpaidx/api/v2/verify";
        /// <summary>
        /// Call this to create payment plan
        /// </summary>
        public const string paymentPlan = "https://ravesandboxapi.flutterwave.com/v2/gpx/paymentplans/create";
        /// <summary>
        /// Call this for card token charge
        /// </summary>
        public const string tokenCharge = "https://ravesandboxapi.flutterwave.com/flwv3-pug/getpaidx/api/tokenized/charge";
    }
}