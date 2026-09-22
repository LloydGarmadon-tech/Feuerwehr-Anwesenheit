using FireDepartmentMvp.Components;
using FireDepartmentMvp.Data;
using FireDepartmentMvp.Domain;
using FireDepartmentMvp.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<AppState>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<PinService>();
builder.Services.AddScoped<AdminService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

await SeedData.InitializeAsync(app.Services);

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    application = "FireDepartmentMvp",
    time = DateTimeOffset.Now
}));

app.MapPost("/api/alarm", async (
    HttpRequest request,
    ActivityService activities,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    const int maxBytes = 64 * 1024;
    using var memory = new MemoryStream();
    await request.Body.CopyToAsync(memory, cancellationToken);

    if (memory.Length == 0)
        return Results.BadRequest(new { error = "Der Request-Body ist leer." });
    if (memory.Length > maxBytes)
        return Results.BadRequest(new { error = "Der Alarm-Request ist zu groß." });

    var bytes = memory.ToArray();
    IncomingAlarmDto? alarm = null;
    string? encodingWarning = null;

    try
    {
        var strictUtf8 = new UTF8Encoding(false, true);
        var json = strictUtf8.GetString(bytes);
        alarm = JsonSerializer.Deserialize<IncomingAlarmDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
    catch (DecoderFallbackException)
    {
        // Kompatibilitätsweg für ältere Alarmgeber/PowerShell, die Windows-1252/ANSI senden.
        var json = Encoding.Latin1.GetString(bytes);
        try
        {
            alarm = JsonSerializer.Deserialize<IncomingAlarmDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            encodingWarning = "Der Request war nicht UTF-8 und wurde als Latin-1/ANSI interpretiert.";
            logger.LogWarning("Alarm {RemoteIp} wurde mit nicht-UTF-8-Zeichenkodierung empfangen.", request.HttpContext.Connection.RemoteIpAddress);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ungültiges Alarm-JSON nach Encoding-Fallback.");
            return Results.BadRequest(new { error = "Der Request enthält kein gültiges JSON.", detail = ex.Message });
        }
    }
    catch (JsonException ex)
    {
        logger.LogWarning(ex, "Ungültiges Alarm-JSON.");
        return Results.BadRequest(new { error = "Der Request enthält kein gültiges JSON.", detail = ex.Message });
    }

    if (alarm is null || string.IsNullOrWhiteSpace(alarm.Keyword))
        return Results.BadRequest(new { error = "Das Feld 'keyword' ist erforderlich." });

    try
    {
        var result = await activities.ImportMissionAsync(alarm, cancellationToken);
        return Results.Ok(new
        {
            result.Activity.Id,
            result.Activity.Title,
            result.Activity.StartedAt,
            importStatus = result.Status.ToString(),
            warning = encodingWarning
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Alarm konnte nicht verarbeitet werden.");
        return Results.Problem("Der Alarm konnte nicht verarbeitet werden.", statusCode: 500);
    }
});


// Diagnose-Endpunkt für die erste FE2-Anbindung. Er erzeugt bewusst KEINEN Einsatz,
// sondern zeigt nur, welche Query-Parameter tatsächlich von FE2 angekommen sind.
app.MapGet("/api/alarm/fe2/debug", (HttpRequest request, ILogger<Program> logger) =>
{
    var parameters = request.Query.ToDictionary(
        x => x.Key,
        x => x.Value.ToString(),
        StringComparer.OrdinalIgnoreCase);

    logger.LogInformation("FE2-Debug-Aufruf von {RemoteIp}: {ParameterCount} Query-Parameter empfangen.",
        request.HttpContext.Connection.RemoteIpAddress,
        parameters.Count);

    return Results.Ok(new
    {
        received = true,
        receivedAt = DateTimeOffset.Now,
        remoteIp = request.HttpContext.Connection.RemoteIpAddress?.ToString(),
        parameters
    });
});

// Produktiver FE2-Endpunkt für das Ausgangsplugin "URL öffnen".
// Erwartete Query-Namen: externalId, keyword, message, street, houseNumber/house, city, alarmTime/timestamp.
app.MapGet("/api/alarm/fe2", async (
    HttpRequest request,
    ActivityService activities,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    static string? Query(HttpRequest req, params string[] names)
    {
        foreach (var name in names)
        {
            if (req.Query.TryGetValue(name, out var value))
            {
                var text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                    return text.Trim();
            }
        }
        return null;
    }

    var externalId = Query(request, "externalId", "external_id");
    var keyword = Query(request, "keyword", "stichwort");
    var message = Query(request, "message", "pluginmessage", "alarmtext");
    var street = Query(request, "street", "strasse", "straße");
    var houseNumber = Query(request, "houseNumber", "house", "hausnummer", "hsnr");
    var city = Query(request, "city", "stadt", "ort");
    var rawAlarmTime = Query(request, "alarmTime", "timestamp");

    DateTimeOffset? alarmTime = null;
    if (!string.IsNullOrWhiteSpace(rawAlarmTime))
    {
        if (DateTimeOffset.TryParse(rawAlarmTime, out var parsed))
            alarmTime = parsed;
        else
            logger.LogWarning("FE2 lieferte einen nicht interpretierbaren Zeitstempel: {AlarmTime}", rawAlarmTime);
    }

    if (string.IsNullOrWhiteSpace(keyword))
    {
        logger.LogWarning("FE2-Aufruf ohne Stichwort von {RemoteIp}. Query: {QueryString}",
            request.HttpContext.Connection.RemoteIpAddress,
            request.QueryString.Value);

        return Results.BadRequest(new
        {
            success = false,
            error = "Der Query-Parameter 'keyword' ist erforderlich. Nutze zuerst /api/alarm/fe2/debug, um die FE2-Parameter zu prüfen."
        });
    }

    try
    {
        var alarm = new IncomingAlarmDto(
            ExternalId: externalId,
            AlarmTime: alarmTime,
            Keyword: keyword,
            Message: message,
            Street: street,
            HouseNumber: houseNumber,
            City: city);

        var result = await activities.ImportMissionAsync(alarm, cancellationToken);
        var details = result.Activity.MissionDetails;

        // Auch "alreadyClosed" wird mit HTTP 200 beantwortet. Damit läuft FE2 bei einer
        // Wiederholung desselben Alarms nicht in unnötige Retry-/Fehlerzustände.
        return Results.Ok(new
        {
            success = true,
            importStatus = result.Status.ToString(),
            activityId = result.Activity.Id,
            missionNumber = details?.MissionNumber,
            externalId = details?.ExternalId,
            title = result.Activity.Title,
            activityStatus = result.Activity.Status.ToString()
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "FE2-Alarm konnte nicht verarbeitet werden.");
        return Results.Problem("Der FE2-Alarm konnte nicht verarbeitet werden.", statusCode: 500);
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
