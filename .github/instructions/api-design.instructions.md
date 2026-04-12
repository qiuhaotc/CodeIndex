---
description: "Use when adding REST API endpoints, controllers, or working with LuceneController and ManagementController. Covers FetchResult pattern, authentication, NSwag, and request/response design."
applyTo: "src/CodeIndex.Server/Controllers/**/*.cs"
---

# REST API Design Conventions

## Controller Structure

All controllers inherit from `ControllerBase` and use `[ApiController]` + `[Route("api/[controller]")]`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class LuceneController : ControllerBase
{
    readonly SearchService searchService;
    readonly ILogger<LuceneController> log;

    public LuceneController(SearchService searchService, ILogger<LuceneController> log)
    {
        this.searchService = searchService;
        this.log = log;
    }
}
```

## Endpoint Authentication Rules

| Controller | Endpoint Type | Attribute |
|------------|---------------|-----------|
| `LuceneController` | All search endpoints | None (anonymous) |
| `ManagementController` | Management endpoints | `[Authorize]` |
| `ManagementController` | Login / GenerateCaptcha | None (anonymous) |

## Response Format — FetchResult

All endpoints with business logic must return `FetchResult<T>`, which contains `Status` (with a `Success` boolean and `StatusDesc` error description):

```csharp
[HttpPost("GetCodeSources")]
public FetchResult<IEnumerable<CodeSource>> GetCodeSources([FromBody] SearchRequest searchRequest)
{
    FetchResult<IEnumerable<CodeSource>> result;
    try
    {
        result = new FetchResult<IEnumerable<CodeSource>>
        {
            Result = searchService.SearchCodeSource(searchRequest),
            Status = new Status { Success = true }
        };
    }
    catch (Exception ex)
    {
        result = new FetchResult<IEnumerable<CodeSource>>
        {
            Status = new Status { Success = false, StatusDesc = ex.ToString() }
        };
        log.LogError(ex, $"GetCodeSources {searchRequest} failed");
    }
    return result;
}
```

## CAPTCHA Authentication (Login Endpoint)

The Login endpoint in `ManagementController` must compare the submitted CAPTCHA against the value stored in `HttpContext.Session`:

```csharp
var captchaInSession = HttpContext.Session.GetString(SessionKeys.Captcha);
if (string.IsNullOrEmpty(captchaInSession) || !captchaInSession.Equals(loginRequest.Captcha, StringComparison.OrdinalIgnoreCase))
{
    return new FetchResult<bool> { Status = new Status { Success = false, StatusDesc = "Invalid CAPTCHA" } };
}
```

## Endpoint Naming

- Query operations: `Get` prefix (`GetCodeSources`, `GetIndexLists`)
- Create operations: `Add` prefix (`AddIndex`)
- Update operations: `Edit` prefix (`EditIndex`)
- Delete/Stop/Start: use verbs (`DeleteIndex`, `StopIndex`, `StartIndex`)

## Parameter Binding

- Complex objects: POST + `[FromBody]` (e.g. `SearchRequest`, `IndexConfig`)
- Simple scalars: GET + query string (e.g. `?indexPk=...`)

## OpenAPI / NSwag

NSwag is configured to generate Swagger docs automatically. No additional `[SwaggerOperation]` attributes are needed. XML comments are picked up automatically.