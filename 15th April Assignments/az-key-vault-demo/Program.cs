using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace az_key_vault_demo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration configuration = BuildConfiguration();
            AppSettings settings = LoadSettings(configuration);

            string? storageConnectionString = NormalizeConnectionString(
                configuration["AZURE_STORAGE_CONNECTION_STRING"]);

            if (string.IsNullOrWhiteSpace(storageConnectionString))
            {
                storageConnectionString = await TryGetStorageConnectionStringFromAzCliAsync(settings.StorageAccountName);
            }

            var credential = new DefaultAzureCredential();
            var keyClient = new KeyClient(new Uri(settings.VaultUrl), credential);

            try
            {
                KeyVaultKey key = (await keyClient.GetKeyAsync(settings.KeyName)).Value;

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

                string encryptedBlobName = await EncryptImageAndUploadAsync(
                    credential,
                    key,
                    settings.StorageAccountName,
                    settings.ContainerName,
                    settings.SourceImageFile,
                    storageConnectionString);

                await DecryptImageFromBlobAsync(
                    credential,
                    key,
                    settings.StorageAccountName,
                    settings.ContainerName,
                    encryptedBlobName,
                    settings.DecryptFolder,
                    settings.DecryptedImageFile,
                    storageConnectionString);

                Console.WriteLine("\nDone.");
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                Console.WriteLine($"Key Vault/Blob request failed ({ex.Status}): {ex.Message}");
                Console.WriteLine("Grant Blob data role or set AZURE_STORAGE_CONNECTION_STRING in launchSettings/user-secrets/environment.");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Key Vault/Blob request failed ({ex.Status}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication or runtime error: {ex.Message}");
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
        }

        private static AppSettings LoadSettings(IConfiguration configuration)
        {
            return new AppSettings
            {
                VaultUrl = GetRequired(configuration, "KeyVault:VaultUrl"),
                KeyName = GetRequired(configuration, "KeyVault:KeyName"),
                StorageAccountName = GetRequired(configuration, "Storage:AccountName"),
                ContainerName = GetRequired(configuration, "Storage:ContainerName"),
                SourceImageFile = GetRequired(configuration, "Files:SourceImage"),
                DecryptFolder = GetRequired(configuration, "Files:DecryptFolder"),
                DecryptedImageFile = GetRequired(configuration, "Files:DecryptedImageFile")
            };
        }

        private static string GetRequired(IConfiguration configuration, string key)
        {
            return configuration[key]
                ?? throw new InvalidOperationException($"Missing configuration value: {key}");
        }

        private static async Task<string> EncryptImageAndUploadAsync(
            TokenCredential credential,
            KeyVaultKey key,
            string storageAccountName,
            string containerName,
            string sourceImageFile,
            string? storageConnectionString)
        {
            string imagePath = ResolveSourcePath(sourceImageFile);
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);

            byte[] aesKey = RandomNumberGenerator.GetBytes(32);
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] ciphertext = new byte[imageBytes.Length];
            byte[] tag = new byte[16];

            using (var aes = new AesGcm(aesKey, 16))
            {
                aes.Encrypt(nonce, imageBytes, ciphertext, tag);
            }

            var cryptoClient = new CryptographyClient(key.Id, credential);
            WrapResult wrappedKey = await cryptoClient.WrapKeyAsync(KeyWrapAlgorithm.RsaOaep, aesKey);

            var payload = new EncryptedBlobPayload
            {
                KeyId = key.Id.ToString(),
                WrappedKeyAlgorithm = KeyWrapAlgorithm.RsaOaep.ToString(),
                WrappedAesKeyBase64 = Convert.ToBase64String(wrappedKey.EncryptedKey),
                NonceBase64 = Convert.ToBase64String(nonce),
                TagBase64 = Convert.ToBase64String(tag),
                CiphertextBase64 = Convert.ToBase64String(ciphertext),
                OriginalFileName = Path.GetFileName(imagePath),
                OriginalContentType = "image/png"
            };

            byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            BlobServiceClient blobServiceClient = CreateBlobServiceClient(
                credential,
                storageAccountName,
                storageConnectionString,
                out string authMode);

            Console.WriteLine($"\nBlob auth mode: {authMode}.");

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            string blobName = $"{Path.GetFileNameWithoutExtension(sourceImageFile)}.encrypted.json";
            var blobClient = containerClient.GetBlobClient(blobName);

            await using var uploadStream = new MemoryStream(payloadBytes);
            await blobClient.UploadAsync(uploadStream, overwrite: true);

            Console.WriteLine("\nEncrypted image uploaded successfully.");
            Console.WriteLine($"Source file: {imagePath}");
            Console.WriteLine($"Blob URI: {blobClient.Uri}");

            return blobName;
        }

        private static async Task DecryptImageFromBlobAsync(
            TokenCredential credential,
            KeyVaultKey key,
            string storageAccountName,
            string containerName,
            string encryptedBlobName,
            string decryptFolder,
            string outputFileName,
            string? storageConnectionString)
        {
            BlobServiceClient blobServiceClient = CreateBlobServiceClient(
                credential,
                storageAccountName,
                storageConnectionString,
                out _);

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(encryptedBlobName);

            var downloadResult = await blobClient.DownloadContentAsync();
            var payload = downloadResult.Value.Content.ToObjectFromJson<EncryptedBlobPayload>();
            if (payload is null)
            {
                throw new InvalidOperationException("Encrypted payload is empty or invalid.");
            }

            byte[] wrappedAesKey = Convert.FromBase64String(payload.WrappedAesKeyBase64);
            byte[] nonce = Convert.FromBase64String(payload.NonceBase64);
            byte[] tag = Convert.FromBase64String(payload.TagBase64);
            byte[] ciphertext = Convert.FromBase64String(payload.CiphertextBase64);

            var cryptoClient = new CryptographyClient(key.Id, credential);
            UnwrapResult unwrapResult = await cryptoClient.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep, wrappedAesKey);
            byte[] aesKey = unwrapResult.Key;

            byte[] plaintext = new byte[ciphertext.Length];
            using (var aes = new AesGcm(aesKey, 16))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            string projectRoot = GetProjectRoot();
            string outputFolder = Path.Combine(projectRoot, decryptFolder);
            Directory.CreateDirectory(outputFolder);

            string outputPath = Path.Combine(outputFolder, outputFileName);
            await File.WriteAllBytesAsync(outputPath, plaintext);

            Console.WriteLine("\nImage decrypted successfully.");
            Console.WriteLine($"Decrypted file: {outputPath}");
        }

        private static BlobServiceClient CreateBlobServiceClient(
            TokenCredential credential,
            string storageAccountName,
            string? storageConnectionString,
            out string authMode)
        {
            if (!string.IsNullOrWhiteSpace(storageConnectionString))
            {
                authMode = "connection string (AZURE_STORAGE_CONNECTION_STRING)";
                return new BlobServiceClient(storageConnectionString);
            }

            authMode = "DefaultAzureCredential";
            return new BlobServiceClient(
                new Uri($"https://{storageAccountName}.blob.core.windows.net"),
                credential);
        }

        private static string ResolveSourcePath(string fileName)
        {
            string projectRoot = GetProjectRoot();
            string[] candidates =
            [
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(projectRoot, fileName)
            ];

            foreach (var candidate in candidates)
            {
                string full = Path.GetFullPath(candidate);
                if (File.Exists(full))
                {
                    return full;
                }
            }

            throw new FileNotFoundException(
                $"File '{fileName}' not found. Place it in project root or output folder.");
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        }

        private static string? NormalizeConnectionString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.StartsWith('"') && normalized.EndsWith('"') && normalized.Length > 1)
            {
                normalized = normalized[1..^1];
            }

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static async Task<string?> TryGetStorageConnectionStringFromAzCliAsync(string storageAccountName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c az storage account show-connection-string --name {storageAccountName} --query connectionString -o tsv",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    return null;
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Console.WriteLine($"\nAzure CLI connection string lookup failed: {error.Trim()}");
                    }

                    return null;
                }

                string? value = NormalizeConnectionString(output);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Console.WriteLine("\nBlob connection string loaded from Azure CLI.");
                }

                return value;
            }
            catch
            {
                return null;
            }
        }

        private sealed class AppSettings
        {
            public required string VaultUrl { get; set; }
            public required string KeyName { get; set; }
            public required string StorageAccountName { get; set; }
            public required string ContainerName { get; set; }
            public required string SourceImageFile { get; set; }
            public required string DecryptFolder { get; set; }
            public required string DecryptedImageFile { get; set; }
        }

        private sealed class EncryptedBlobPayload
        {
            public required string KeyId { get; set; }
            public required string WrappedKeyAlgorithm { get; set; }
            public required string WrappedAesKeyBase64 { get; set; }
            public required string NonceBase64 { get; set; }
            public required string TagBase64 { get; set; }
            public required string CiphertextBase64 { get; set; }
            public required string OriginalFileName { get; set; }
            public required string OriginalContentType { get; set; }
        }
    }
}
