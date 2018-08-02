using flutterwave_sub.Encryption;
using flutterwave_sub.JsonModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace flutterwave_sub.Tests
{
    [TestClass]
    public class FlutterwaveTest
    {
        public CardPayDetails_NotComplete Get_Card_Details()
        {

            var CardPayDetail = new CardPayDetails_NotComplete
            {
                amount = "10000",
                cardno = "5438898014560229",
                charge_type = "normal",
                country = "NG",
                currency = "NGN",
                cvv = "787",
                device_fingerprint = "787787677665IU9",
                email = "email@email.com",
                expirymonth = "09",
                expiryyear = "19",
                firstname = "Test",
                IP = "0.0.0.0",
                lastname = "Me Test",
                payment_plan = "209",
                PBFPubKey = Credentials.API_Public_Key,
                phonenumber = "080999808899",
                txRef = "FLWK-8997878787UHY8998009"
            };
            return CardPayDetail;
        }

        [DataTestMethod]
        public void Can_Encrypt_And_Decrypt()
        {
            // Arrange
            var en = new RavePaymentDataEncryption();
            var cardDetails = Get_Card_Details();
            var seckey = Credentials.API_Secret_Key;

            // Act
            var key = en.GetEncryptionKey(seckey);
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(cardDetails);
            var cipher = en.EncryptData(key, data);

            var test = en.DecryptData(cipher, key);

            // Assert
            Assert.AreEqual(data, test);
            Assert.IsTrue(!key.Equals(seckey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
