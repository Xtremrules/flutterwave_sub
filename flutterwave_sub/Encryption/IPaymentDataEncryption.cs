using System;

namespace flutterwave_sub.Encryption
{
    public interface IPaymentDataEncryption
    {
        string GetEncryptionKey(string secretKey);
        string EncryptData(string encryptionKey, String data);
        string DecryptData(string encryptedData, string encryptionKey);
    }
}
