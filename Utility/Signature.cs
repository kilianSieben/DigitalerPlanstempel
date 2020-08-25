using System;
using System.Security.Cryptography;

namespace DigitalerPlanstempel.Utility
{
    public static class Signature
    {
        //Folgende Funktionen wurden von https://docs.microsoft.com/de-de/dotnet/standard/security/cryptographic-signatures übernommen
        public static (byte[], RSACryptoServiceProvider) CreateSignature(byte[] hashValue)
        {

            //The value to hold the signed value.
            byte[] signedHashValue;

            //Generate a public/private key pair.
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            //Create an RSAPKCS1SignatureFormatter object and pass it the
            //RSACryptoServiceProvider to transfer the private key.
            RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);

            //Set the hash algorithm to SHA1.
            rsaFormatter.SetHashAlgorithm("SHA256");
            

            //Create a signature for hashValue and assign it to
            //signedHashValue.
            signedHashValue = rsaFormatter.CreateSignature(hashValue);

            return (signedHashValue,rsa);
        }

        public static bool VerifySignature(byte[] hashValue,byte[] signedHashValue, RSACryptoServiceProvider rsa)
        {

            RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
            rsaDeformatter.SetHashAlgorithm("SHA256");
            if (rsaDeformatter.VerifySignature(hashValue, signedHashValue))
            {
                Console.WriteLine("The signature is valid.");
                return true;
            }
            else
            {
                Console.WriteLine("The signature is not valid.");
                return false;
            }
        }
    }
}
