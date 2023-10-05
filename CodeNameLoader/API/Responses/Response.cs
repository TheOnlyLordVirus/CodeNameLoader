using System.Linq;

namespace CodeNameLoader.API.Responses;

public class EncryptedResponse
{
    public string EncryptedData { get; set; } = string.Empty;
}

public class Response<TResponse>
{
    public TResponse? ResponseObject { get; init; }

    public string? EncryptedData { get; init; } = null;

    public bool HasErrors
    {
        get =>
            Errors is not null &&
            Errors?.Count() > 0;
    }

    public string[]? Errors { get; init; } = null;

    public Response(TResponse responseData) =>
        ResponseObject = responseData;

    public Response(string encryptedData) =>
        EncryptedData = encryptedData;

    public Response(string[] errors) =>
        Errors = errors;
}
