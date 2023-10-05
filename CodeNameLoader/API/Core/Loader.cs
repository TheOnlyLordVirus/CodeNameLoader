using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using CodeNameLoader.API.Requests;
using CodeNameLoader.API.Responses;
using CodeNameLoader.API.Security;

using Newtonsoft.Json;

namespace CodeNameLoader.API.Core;

internal static class Loader
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<Response<TResponse>?> 
        SendRequest<TResponse, TRequest>
        (Request<TRequest> request, 
        CancellationToken cancellationToken = default)
    {
        EncryptedResponse? encryptedResponse = null;
        Response<TResponse>? response = null;

        try
        {
            EncryptedRequest? encryptedRequest = null;

            if (request.EncryptedData is null)
                encryptedRequest = new EncryptedRequest()
                {
                    EncryptedData = (await request.EncyrptAsync(cancellationToken)
                        .ConfigureAwait(false)).EncryptedData ??
                        throw new NullReferenceException("Failed to encrypt the request!")
                };

            else
                encryptedRequest = new EncryptedRequest()
                {
                    EncryptedData = request.EncryptedData
                };

            if (encryptedRequest is null)
                throw new NullReferenceException("Failed to create encrypted request!");

            HttpResponseMessage httpResponse =
                await _httpClient
                .PostAsJsonAsync(
                    "https://localhost:7179/api/Beta/Input",
                    encryptedRequest!,
                    cancellationToken)
                .ConfigureAwait(false);

            string content = await httpResponse
                .Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            encryptedResponse = JsonConvert.DeserializeObject<EncryptedResponse>(content);

            if (encryptedResponse is null)
                throw new NullReferenceException("Failed to decrypt encrypted response!");

            response = await new Response<TResponse>(encryptedResponse.EncryptedData)
                .DecryptAsync(cancellationToken)
                .ConfigureAwait(false);
        }

#if DEBUG
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
#endif
        return response;
    }

}
