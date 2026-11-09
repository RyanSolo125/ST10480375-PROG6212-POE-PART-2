using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// The ONLY place the MVC app gets data from: every call goes to the Part 2 RaceDay API.
/// The API's session cookie (received at login) is stored in the MVC session and
/// sent with each request, so the API knows which user is making the call.
/// </summary>
public class RaceDayApiClient
{
    private const string ApiSessionCookieName = "RaceDay.Session";
    private const string ServiceDownMessage = "The RaceDay service is not available right now. Please try again in a moment.";

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private ISession Session => _httpContextAccessor.HttpContext!.Session;

    // ---------------------------------------------------------------
    // Authentication
    // ---------------------------------------------------------------

    /// <summary>Logs in through the API and saves the API session cookie + user details in the MVC session.</summary>
    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string password)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<LoginResponse>
                {
                    StatusCode = (int)response.StatusCode,
                    ErrorMessage = await ReadErrorMessageAsync(response)
                };
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            // Grab the API's session cookie, e.g. "RaceDay.Session=CfDJ8...", so later calls are authenticated
            string? apiCookie = null;
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                string? sessionCookie = cookies.FirstOrDefault(c => c.StartsWith(ApiSessionCookieName + "="));
                apiCookie = sessionCookie?.Split(';')[0];
            }

            if (login == null || apiCookie == null)
            {
                return new ApiResult<LoginResponse> { StatusCode = 500, ErrorMessage = "Login failed. Please try again." };
            }

            Session.SetString(SessionKeys.ApiCookie, apiCookie);
            Session.SetInt32(SessionKeys.UserId, login.UserId);
            Session.SetString(SessionKeys.FullName, login.FullName);
            Session.SetString(SessionKeys.Role, login.Role);

            return new ApiResult<LoginResponse> { Success = true, StatusCode = 200, Data = login };
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            _logger.LogError(ex, "Could not reach the RaceDay API at login.");
            return new ApiResult<LoginResponse> { StatusCode = 503, ErrorMessage = ServiceDownMessage };
        }
    }

    public Task<ApiResult<UserDto>> RegisterAsync(object registerRequest) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", JsonContent.Create(registerRequest));

    public Task<ApiResult<object>> LogoutAsync() =>
        SendAsync<object>(HttpMethod.Post, "api/auth/logout");

    // ---------------------------------------------------------------
    // Profile
    // ---------------------------------------------------------------

    public Task<ApiResult<UserDto>> GetProfileAsync() =>
        SendAsync<UserDto>(HttpMethod.Get, "api/users/me");

    public Task<ApiResult<UserDto>> UpdateProfileAsync(string fullName, string? phoneNumber) =>
        SendAsync<UserDto>(HttpMethod.Put, "api/users/me", JsonContent.Create(new { fullName, phoneNumber }));

    public Task<ApiResult<UserDto>> UploadProfilePictureAsync(IFormFile file) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/users/me/profile-picture", ToMultipart(file));

    public Task<ApiResult<List<HistoryDto>>> GetHistoryAsync() =>
        SendAsync<List<HistoryDto>>(HttpMethod.Get, "api/users/me/history");

    // ---------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------

    public Task<ApiResult<List<EventDto>>> GetEventsAsync(string? type = null, string? search = null, bool upcomingOnly = false)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) query.Add("type=" + Uri.EscapeDataString(type));
        if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search));
        if (upcomingOnly) query.Add("upcoming=true");

        string url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return SendAsync<List<EventDto>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<List<EventDto>>> GetMyEventsAsync() =>
        SendAsync<List<EventDto>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResult<EventDto>> GetEventAsync(int id) =>
        SendAsync<EventDto>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResult<EventDto>> CreateEventAsync(object eventRequest) =>
        SendAsync<EventDto>(HttpMethod.Post, "api/events", JsonContent.Create(eventRequest));

    public Task<ApiResult<EventDto>> UpdateEventAsync(int id, object eventRequest) =>
        SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", JsonContent.Create(eventRequest));

    public Task<ApiResult<object>> DeleteEventAsync(int id) =>
        SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResult<EventDto>> UploadBannerAsync(int eventId, IFormFile file) =>
        SendAsync<EventDto>(HttpMethod.Post, $"api/events/{eventId}/banner", ToMultipart(file));

    // ---------------------------------------------------------------
    // Categories
    // ---------------------------------------------------------------

    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryDto>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, object categoryRequest) =>
        SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", JsonContent.Create(categoryRequest));

    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, object categoryRequest) =>
        SendAsync<CategoryDto>(HttpMethod.Put, $"api/categories/{categoryId}", JsonContent.Create(categoryRequest));

    public Task<ApiResult<object>> DeleteCategoryAsync(int categoryId) =>
        SendAsync<object>(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ---------------------------------------------------------------
    // Enrolments
    // ---------------------------------------------------------------

    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId) =>
        SendAsync<EnrolmentDto>(HttpMethod.Post, "api/enrolments", JsonContent.Create(new { categoryId }));

    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/enrolments/me");

    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task<ApiResult<EnrolmentDto>> UpdateEnrolmentStatusAsync(int enrolmentId, string status) =>
        SendAsync<EnrolmentDto>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", JsonContent.Create(new { status }));

    public Task<ApiResult<object>> CancelEnrolmentAsync(int enrolmentId) =>
        SendAsync<object>(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    // ---------------------------------------------------------------
    // Results
    // ---------------------------------------------------------------

    public Task<ApiResult<ResultDto>> CaptureResultAsync(int enrolmentId, string? finishTime, int? position, string status) =>
        SendAsync<ResultDto>(HttpMethod.Post, "api/results", JsonContent.Create(new { enrolmentId, finishTime, position, status }));

    public Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, string? finishTime, int? position, string status) =>
        SendAsync<ResultDto>(HttpMethod.Put, $"api/results/{resultId}", JsonContent.Create(new { finishTime, position, status }));

    public Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/events/{eventId}/results");

    // ---------------------------------------------------------------
    // Images (stored in Azure by the API - this app never talks to Azure)
    // ---------------------------------------------------------------

    public async Task<ApiResult<ImageFile>> GetImageAsync(string fileName)
    {
        try
        {
            using var response = await _http.GetAsync($"api/files/{Uri.EscapeDataString(fileName)}");
            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<ImageFile> { StatusCode = (int)response.StatusCode };
            }

            return new ApiResult<ImageFile>
            {
                Success = true,
                StatusCode = 200,
                Data = new ImageFile
                {
                    Bytes = await response.Content.ReadAsByteArrayAsync(),
                    ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream"
                }
            };
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            _logger.LogError(ex, "Could not load image {FileName} from the API.", fileName);
            return new ApiResult<ImageFile> { StatusCode = 503 };
        }
    }

    // ---------------------------------------------------------------
    // Shared helpers
    // ---------------------------------------------------------------

    /// <summary>Sends a request to the API with the user's API session cookie and reads the reply.</summary>
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        string? apiCookie = Session.GetString(SessionKeys.ApiCookie);
        if (apiCookie != null)
        {
            request.Headers.Add("Cookie", apiCookie);
        }

        try
        {
            using var response = await _http.SendAsync(request);

            var result = new ApiResult<T>
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode
            };

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = await ReadErrorMessageAsync(response);
            }
            else if (response.StatusCode != HttpStatusCode.NoContent && typeof(T) != typeof(object))
            {
                result.Data = await response.Content.ReadFromJsonAsync<T>();
            }

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            _logger.LogError(ex, "Could not reach the RaceDay API: {Method} {Url}", method, url);
            return new ApiResult<T> { StatusCode = 503, ErrorMessage = ServiceDownMessage };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "The RaceDay API sent back data we could not read: {Method} {Url}", method, url);
            return new ApiResult<T> { StatusCode = 500, ErrorMessage = "Something went wrong. Please try again." };
        }
    }

    /// <summary>Wraps an uploaded file so it can be sent to the API as multipart/form-data.</summary>
    private static MultipartFormDataContent ToMultipart(IFormFile file)
    {
        var fileContent = new StreamContent(file.OpenReadStream());
        if (!string.IsNullOrEmpty(file.ContentType))
        {
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        }

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", file.FileName);
        return form;
    }

    /// <summary>Turns an API error into a short message we can show to the user (never a raw error).</summary>
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            string body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;

                // Our API's own errors look like { "message": "..." }
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString()!;
                }

                // Validation errors look like { "errors": { "Field": ["..."] } }
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(field => field.Value.EnumerateArray())
                        .Select(m => m.GetString())
                        .Where(m => !string.IsNullOrWhiteSpace(m));
                    return string.Join(" ", messages);
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON - fall through to a general message
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            _ => "Something went wrong. Please try again."
        };
    }
}
