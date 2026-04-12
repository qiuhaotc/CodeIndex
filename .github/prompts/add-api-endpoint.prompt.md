---
description: "Add a new REST API endpoint to LuceneController or ManagementController, including FetchResult wrapper, logging with index name prefix, and appropriate authentication."
agent: "agent"
argument-hint: "Endpoint description, e.g.: get statistics for an index (requires auth)"
---

Add a new REST API endpoint to `CodeIndex.Server`: `$SELECTION_OR_INPUT`

## Steps

### 1. Determine the controller

- **LuceneController** (`api/Lucene/`) — search/query operations, no authentication
- **ManagementController** (`api/Management/`) — management operations, requires `[Authorize]`

### 2. Generate the endpoint skeleton

Follow this pattern:

```csharp
/// <summary>Brief description of the endpoint</summary>
[HttpPost("EndpointName")]   // or [HttpGet("EndpointName")]
[Authorize]                  // if authentication is required
public FetchResult<T> EndpointName([FromBody] RequestDto request)
{
    FetchResult<T> result;
    try
    {
        request.RequireNotNull(nameof(request));

        result = new FetchResult<T>
        {
            Result = service.DoWork(request),
            Status = new Status { Success = true }
        };
    }
    catch (Exception ex)
    {
        result = new FetchResult<T>
        {
            Status = new Status { Success = false, StatusDesc = ex.ToString() }
        };
        log.LogError(ex, $"EndpointName {request} failed");
    }
    return result;
}
```

### 3. Create a DTO if needed

- Use C# `record` for simple data transfer objects
- Place in `CodeIndex.Common` (accessible to all layers)

```csharp
// CodeIndex.Common/MyRequest.cs
public record MyRequest(Guid IndexPk, string Filter);
```

### 4. Add a Service method if needed

Add the method to the relevant Service/SearchService, following the same `FetchResult` error handling pattern and validating parameters with `RequireNotNull`/`RequireNotNullOrEmpty`.

### 5. Checklist

- [ ] Response wrapped in `FetchResult<T>`
- [ ] `try/catch` captures all exceptions and populates `StatusDesc`
- [ ] Log messages include the index name prefix (if applicable)
- [ ] Management endpoints decorated with `[Authorize]`
- [ ] DTOs placed in `CodeIndex.Common`
- [ ] Parameters validated with `ArgumentValidation` at method entry

Implement the complete endpoint code including any required DTO classes and Service methods.
