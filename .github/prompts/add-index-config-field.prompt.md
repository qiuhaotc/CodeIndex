---
description: "Add a new index configuration field to IndexConfig record and propagate the change through the entire stack: storage, UI, API, and validation."
agent: "agent"
argument-hint: "New field description, e.g.: maximum file size limit (int, MB, default 10)"
---

Add a new configuration field to the `IndexConfig` record: `$SELECTION_OR_INPUT`

## Steps

### 1. Modify `IndexConfig` record

File: `src/CodeIndex.Common/IndexConfig.cs`

```csharp
// Add the new property with a default value
public int NewField { get; init; } = defaultValue;
```

### 2. Check persistence

`IndexConfig` is JSON-serialized to disk (under the `Configuration/` directory). The new field must be backward-compatible with existing config files by providing a default value for deserialization.

### 3. Check `IndexConfigForView`

File: `src/CodeIndex.Common/IndexConfigForView.cs`
If `IndexConfigForView` is a display DTO for `IndexConfig`, add the corresponding field there as well.

### 4. Update the Blazor UI

File: index management page under `src/CodeIndex.Server/Pages/`
Add the matching input control (e.g. `<InputNumber>` / `<InputText>`) and label to the form component.

### 5. Add validation if needed

If the new field has range constraints, add validation in `ArgumentValidation` or inside `IndexConfig` itself.

### 6. Update `ManagementController`

If `AddIndex` / `EditIndex` endpoints need to accept the new field, confirm `IndexConfig` is deserialized correctly from the request body (the framework handles this automatically; just ensure field names match).

### 7. Update consumers

Search for usages of `IndexConfig` in `IndexMaintainer`, `CodeIndexBuilder`, and `SearchService`, and read the new field wherever it is needed.

## Checklist

- [ ] `IndexConfig` record has the new property (with default value)
- [ ] `IndexConfigForView` updated (if applicable)
- [ ] Blazor form has the new input control
- [ ] Consumer logic uses the new field
- [ ] Unit tests added to verify the new field behavior

Implement all of the above changes step by step.