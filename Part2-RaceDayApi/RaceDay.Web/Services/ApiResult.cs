namespace RaceDay.Web.Services;

/// <summary>What came back from an API call: success or a friendly error message.</summary>
public class ApiResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>An API result that also carries data (e.g. a list of events).</summary>
public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }
}
