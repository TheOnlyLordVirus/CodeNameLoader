using System.Linq;

namespace CodeNameLoader.API.Requests;

public sealed class EncryptedRequest
{
    public string EncryptedData { get; set; } = string.Empty;
}

public sealed class Request<TRequest>
{
    public TRequest? RequestObject { get; init; }

    public string? EncryptedData { get; init; } = null;

    public bool HasErrors
    {
        get =>
            Errors is not null &&
            Errors.Count() > 0;
    }

    public string[]? Errors { get; init; } = null;

    public Request(TRequest requestObject) =>
        RequestObject = requestObject;

    public Request(string encryptedData) =>
        EncryptedData = encryptedData;

    public Request(string[] errors) =>
        Errors = errors;
}
