using System;
using System.Net;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Base;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? Content { get; }

    public ApiException(HttpStatusCode statusCode, string message, string? content = null)
        : base(message)
    {
        StatusCode = statusCode;
        Content = content;
    }
}
