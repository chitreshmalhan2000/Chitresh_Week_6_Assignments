using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using System.Text;

namespace chitreshapp68
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            string tenantId = "23f0a6db-5131-4dee-842b-e1e5d0703e7b";
            string clientId = "41ac63fe-41bd-4993-930c-754f8eb1ee4d";
            string clientSecret = "tlw8Q~JWv-uKnbYGVZpklyu1ET.p731kzorSEbN8";


            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            string vaultUrl = "https://chitreshkeyvault.vault.azure.net/";
            string keyName = "chitreshkey";


            var keyClient = new KeyClient(new Uri(vaultUrl), credential);

            KeyVaultKey key;

            key = await keyClient.GetKeyAsync(keyName);

            string originalText = "Sensitive order data for CloudXeus Technology Services";
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(originalText);

            var cryptoClient = new CryptographyClient(key.Id, credential);

            EncryptResult encryptResult = await cryptoClient.EncryptAsync(
                EncryptionAlgorithm.RsaOaep,
                plaintextBytes);

            Console.WriteLine("Encrypted text (Base64):");
            Console.WriteLine(Convert.ToBase64String(encryptResult.Ciphertext));

            DecryptResult decryptResult = await cryptoClient.DecryptAsync(
                EncryptionAlgorithm.RsaOaep,
                encryptResult.Ciphertext);

            string decryptedText = Encoding.UTF8.GetString(decryptResult.Plaintext);

            Console.WriteLine("\nDecrypted text:");
            Console.WriteLine(decryptedText);

            Console.ReadLine();
        }
    }
}
