using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;

using Newtonsoft.Json;
using CodeNameLoader.API.Requests;
using CodeNameLoader.API.Responses;

namespace CodeNameLoader.API.Security;

internal static class NetworkObfuscation
{
    private static readonly Aes _aes = Aes.Create();
    private static readonly SHA256 _sha256 = SHA256.Create();

    private static readonly string _decryptionKey = "Cba321";
    private static readonly byte[] _decryptIV = new byte[16]
    {
            0x19, 0xe7, 0x22, 0xd4, 0xc5, 0x54, 0x92, 0xd3,
            0xd8, 0xd1, 0xc2, 0xca, 0x05, 0x5b, 0x45, 0xdc
    };

    private static readonly string _encryptionKey = "Abc123";
    private static readonly byte[] _encryptIV = new byte[16]
    {
            0x86, 0xc0, 0x71, 0x97, 0x16, 0x9a, 0x25, 0x7b,
            0x70, 0xf0, 0x9c, 0x3e, 0x8f, 0x61, 0x4d, 0xb4
    };

    public static async Task<Request<TRequest>> EncyrptAsync<TRequest>(
            this Request<TRequest> unencryptedRequest,
            CancellationToken cancellationToken = default)
    {
        Request<TRequest> encryptedRequest = unencryptedRequest;
        string encryptedRequestData = string.Empty;
        string[] errors = Array.Empty<string>();

        try
        {
            string serializedRequest = JsonConvert.SerializeObject(unencryptedRequest.RequestObject);
            encryptedRequestData = await Internal_AesEncryptStringAsync(serializedRequest, cancellationToken)
                .ConfigureAwait(false);

            if (encryptedRequest.Equals(string.Empty))
                throw new Exception("Failed to serialize and encrypt this request!");

            encryptedRequest = new Request<TRequest>(encryptedRequestData);
        }

#if DEBUG
        catch (Exception ex)
        {
            errors = new string[]
            {
                    string.Concat("Message: ", ex.Message),
                    string.Concat("Inner Exception: ", ex.InnerException)
            };

            encryptedRequest = new Request<TRequest>(errors);
        }
#endif

        finally
        {
            // Log request and log any errors in the database to monitor what data people are sending.
        }

        return encryptedRequest;
    }

    public static async Task<Response<TResponse>> DecryptAsync<TResponse>(
        this Response<TResponse> encryptedResponse,
        CancellationToken cancellationToken = default)
    {
        TResponse? unecryptedResponseObject = default;
        Response<TResponse>? unecryptedRequest = default;
        string[] errors = Array.Empty<string>();

        try
        {
            if (string.IsNullOrEmpty(encryptedResponse?.EncryptedData))
                throw new Exception("The EncryptedResponse data was null or empty!");

            string unencryptedString = await Internal_AesDecryptStringAsync(encryptedResponse.EncryptedData!, cancellationToken)
                .ConfigureAwait(false);

            unecryptedResponseObject = JsonConvert.DeserializeObject<TResponse?>(unencryptedString!);

            if (unecryptedResponseObject is null)
                throw new Exception("Failed to deserialize and decrypt this Response!");

            unecryptedRequest = new Response<TResponse>(unecryptedResponseObject);
        }

#if DEBUG
        catch (Exception ex)
        {
            errors = new string[]
            {
                    string.Concat("Message: ", ex.Message),
                    string.Concat("Inner Exception: ", ex.InnerException)
            };

            unecryptedRequest = new Response<TResponse>(errors);
        }
#endif

        finally
        {
            // Log request and log any errors in the database to monitor what data people are sending.
        }

        return unecryptedRequest;
    }

    private static async Task<string> Internal_AesDecryptStringAsync(string encryptedText, CancellationToken cancellationToken = default)
    {
        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        string decryptedText = string.Empty;

        byte[] key = _sha256.ComputeHash(Encoding.ASCII.GetBytes(_decryptionKey));
        _aes.Mode = CipherMode.CBC;
        _aes.Key = key;
        _aes.IV = _decryptIV;
        _aes.Padding = PaddingMode.PKCS7;

        using MemoryStream memoryStream = new MemoryStream();
        using CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateDecryptor(), CryptoStreamMode.Write);

        try
        {
            await cryptoStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length, cancellationToken);
            await cryptoStream.FlushFinalBlockAsync(cancellationToken);

            byte[] decryptedBytes = memoryStream.ToArray();

            decryptedText = Encoding.UTF8.GetString(decryptedBytes, 0, decryptedBytes.Length);
        }

        finally
        {
            memoryStream.Close();
            cryptoStream.Close();
        }

        return decryptedText;
    }

    private static async Task<string> Internal_AesEncryptStringAsync(string plainText, CancellationToken cancellationToken = default)
    {
        string encryptedText;

        byte[] key = _sha256.ComputeHash(Encoding.ASCII.GetBytes(_encryptionKey));
        _aes.Mode = CipherMode.CBC;
        _aes.Key = key;
        _aes.IV = _encryptIV;
        _aes.Padding = PaddingMode.PKCS7;

        using MemoryStream memoryStream = new MemoryStream();
        using CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateEncryptor(), CryptoStreamMode.Write);

        try
        {
            byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);
            await cryptoStream.WriteAsync(plainBytes, 0, plainBytes.Length, cancellationToken);
            await cryptoStream.FlushFinalBlockAsync(cancellationToken);

            byte[] cipherBytes = memoryStream.ToArray();

            encryptedText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);
        }

        finally
        {
            memoryStream.Close();
            cryptoStream.Close();
        }

        return encryptedText;
    }
}
