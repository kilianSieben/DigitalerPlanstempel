using System.Text;
using System.Security.Cryptography;


namespace DigitalerPlanstempel.Utility
{
    public static class Hashing
    {
        //Folgende Funktion wurde von https://docs.microsoft.com/de-de/dotnet/api/system.security.cryptography.hashalgorithm.computehash?view=netcore-3.1 übernommen
        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public static byte[] GetHashBytes(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            return data;
        }
    }
}
