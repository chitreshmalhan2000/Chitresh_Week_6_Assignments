using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using System.Text;

namespace az_key_vault_demo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string vaultUrl = "https://at-key-15april.vault.azure.net/";
            string keyName = "at-az-key";

            var credential = new DefaultAzureCredential();
            var keyClient = new KeyClient(new Uri(vaultUrl), credential);


            try
            {
                KeyVaultKey key;

                key = (await keyClient.GetKeyAsync(keyName)).Value;

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
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Key Vault request failed ({ex.Status}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication or runtime error: {ex.Message}");
            }
        }
    }
}
