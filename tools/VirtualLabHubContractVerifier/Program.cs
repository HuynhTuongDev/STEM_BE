using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using STEM.Core.Entities.Users;

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

try
{
    var settings = VerifierSettings.Load(args);
    Console.WriteLine($"BaseUrl: {settings.BaseUrl}");
    Console.WriteLine($"LabId: {settings.LabId}");
    Console.WriteLine($"ClassId: {settings.ClassId}");
    Console.WriteLine($"StudentId: {settings.StudentId}");
    Console.WriteLine($"TeacherId: {settings.TeacherId}");
    Console.WriteLine($"NonMatchingStudentId: {settings.NonMatchingStudentId}");

    await using var fixture = await TestFixture.CreateAsync(settings, jsonOptions);
    try
    {
        await ContractVerifier.RunAsync(settings, fixture, jsonOptions);
    }
    finally
    {
        await fixture.CleanupAsync();
    }

    Console.WriteLine("PASS: VirtualLabHub contract verifier completed.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.ExitCode = 1;
}

internal sealed record VerifierSettings(
    Uri BaseUrl,
    Guid LabId,
    int ClassId,
    int StudentId,
    int NonMatchingStudentId,
    int TeacherId,
    string StudentToken,
    string NonMatchingStudentToken,
    string TeacherToken,
    string ConnectionString,
    TimeSpan Timeout)
{
    public static VerifierSettings Load(string[] args)
    {
        var values = ParseArgs(args);
        var baseUrl = new Uri(GetValue(values, "base-url", "STEM_SIGNALR_BASE_URL") ?? "http://localhost:55459");
        var classId = GetInt(values, "class-id", "STEM_SIGNALR_CLASS_ID", 3);
        var labId = GetGuid(
            values,
            "lab-id",
            "STEM_SIGNALR_LAB_ID",
            Guid.Parse("e82937a8-fb12-49d2-ab3a-b780e44556f8"));
        var teacherId = GetInt(values, "teacher-id", "STEM_SIGNALR_TEACHER_ID", 45);
        var timeoutSeconds = GetInt(values, "timeout-seconds", "STEM_SIGNALR_TIMEOUT_SECONDS", 10);
        var appsettingsPath = ResolveAppSettingsPath(GetValue(values, "appsettings", "STEM_API_APPSETTINGS"));
        var config = LoadAppSettings(appsettingsPath);
        var jwt = config.RootElement.GetProperty("JwtSettings");
        var secret = jwt.GetProperty("Secret").GetString() ?? throw new InvalidOperationException("JwtSettings:Secret is missing.");
        var issuer = jwt.GetProperty("Issuer").GetString() ?? throw new InvalidOperationException("JwtSettings:Issuer is missing.");
        var audience = jwt.GetProperty("Audience").GetString() ?? throw new InvalidOperationException("JwtSettings:Audience is missing.");
        var connectionString = config.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString()
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

        var studentId = GetOptionalInt(values, "student-id", "STEM_SIGNALR_STUDENT_ID")
            ?? ResolveFirstStudentInClass(connectionString, classId)
            ?? 910101;
        var nonMatchingStudentId = GetInt(
            values,
            "non-matching-student-id",
            "STEM_SIGNALR_NON_MATCHING_STUDENT_ID",
            studentId + 500000);

        var studentToken = GetValue(values, "student-token", "STEM_SIGNALR_STUDENT_TOKEN")
            ?? JwtFactory.Create(studentId, "Hub Contract Student", RoleNames.Student, secret, issuer, audience);
        var nonMatchingStudentToken = GetValue(values, "non-matching-student-token", "STEM_SIGNALR_NON_MATCHING_STUDENT_TOKEN")
            ?? JwtFactory.Create(nonMatchingStudentId, "Hub Contract Other Student", RoleNames.Student, secret, issuer, audience);
        var teacherToken = GetValue(values, "teacher-token", "STEM_SIGNALR_TEACHER_TOKEN")
            ?? JwtFactory.Create(teacherId, "Hub Contract Teacher", RoleNames.Teacher, secret, issuer, audience);

        return new VerifierSettings(
            baseUrl,
            labId,
            classId,
            studentId,
            nonMatchingStudentId,
            teacherId,
            studentToken,
            nonMatchingStudentToken,
            teacherToken,
            connectionString,
            TimeSpan.FromSeconds(timeoutSeconds));
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var keyValue = arg[2..].Split('=', 2);
            if (keyValue.Length == 2)
            {
                values[keyValue[0]] = keyValue[1];
                continue;
            }

            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for argument {arg}.");
            }

            values[keyValue[0]] = args[++i];
        }

        return values;
    }

    private static string? GetValue(Dictionary<string, string> values, string key, string environmentVariable)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : Environment.GetEnvironmentVariable(environmentVariable);
    }

    private static int GetInt(Dictionary<string, string> values, string key, string environmentVariable, int fallback)
    {
        var raw = GetValue(values, key, environmentVariable);
        return int.TryParse(raw, out var value) ? value : fallback;
    }

    private static int? GetOptionalInt(Dictionary<string, string> values, string key, string environmentVariable)
    {
        var raw = GetValue(values, key, environmentVariable);
        return int.TryParse(raw, out var value) ? value : null;
    }

    private static Guid GetGuid(Dictionary<string, string> values, string key, string environmentVariable, Guid fallback)
    {
        var raw = GetValue(values, key, environmentVariable);
        return Guid.TryParse(raw, out var value) ? value : fallback;
    }

    private static int? ResolveFirstStudentInClass(string connectionString, int classId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = new NpgsqlCommand(
            """
            SELECT "StudentId"
            FROM "Enrollments"
            WHERE "ClassId" = @classId
            ORDER BY "StudentId"
            LIMIT 1
            """,
            connection);
        command.Parameters.Add("classId", NpgsqlDbType.Integer).Value = classId;
        var result = command.ExecuteScalar();
        return result is int studentId ? studentId : null;
    }

    private static string ResolveAppSettingsPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return Path.GetFullPath(explicitPath);
        }

        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "STEM.Api", "appsettings.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate STEM.Api/appsettings.json.");
    }

    private static JsonDocument LoadAppSettings(string path)
    {
        Console.WriteLine($"AppSettings: {path}");
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}

internal static class JwtFactory
{
    public static string Create(int userId, string displayName, string roleName, string secret, string issuer, string audience)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object?>
        {
            ["iss"] = issuer,
            ["aud"] = audience,
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddHours(2).ToUnixTimeSeconds(),
            ["sub"] = userId.ToString(),
            ["email"] = $"hub-contract-{userId}@local.test",
            ["name"] = displayName,
            [ClaimTypes.NameIdentifier] = userId.ToString(),
            [ClaimTypes.Name] = displayName,
            [ClaimTypes.Role] = roleName
        };

        var headerJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        });
        var payloadJson = JsonSerializer.Serialize(payload);
        var unsigned = $"{Base64Url(Encoding.UTF8.GetBytes(headerJson))}.{Base64Url(Encoding.UTF8.GetBytes(payloadJson))}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned)));
        return $"{unsigned}.{signature}";
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

internal sealed class TestFixture : IAsyncDisposable
{
    private readonly VerifierSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly HttpClient _httpClient;
    private readonly List<(Guid ProjectId, int StudentId)> _createdProjects = new();
    private readonly List<Guid> _createdLabClassAssignments = new();
    private readonly List<int> _createdEnrollmentIds = new();

    private TestFixture(VerifierSettings settings, JsonSerializerOptions jsonOptions, Guid projectId)
    {
        _settings = settings;
        _jsonOptions = jsonOptions;
        ProjectId = projectId;
        _httpClient = new HttpClient { BaseAddress = settings.BaseUrl };
        _createdProjects.Add((projectId, settings.StudentId));
    }

    public Guid ProjectId { get; }

    public static async Task<TestFixture> CreateAsync(VerifierSettings settings, JsonSerializerOptions jsonOptions)
    {
        using var httpClient = new HttpClient { BaseAddress = settings.BaseUrl };
        await WaitForApiAsync(httpClient, settings.Timeout);
        var projectId = await CreateProjectAsync(
            httpClient,
            settings.StudentToken,
            settings.LabId,
            jsonOptions);

        Console.WriteLine($"Created test project: {projectId:N}");
        return new TestFixture(settings, jsonOptions, projectId);
    }

    public async Task<Guid> CreateProjectForStudentAsync(string studentToken, int studentId)
    {
        var projectId = await CreateProjectAsync(
            _httpClient,
            studentToken,
            _settings.LabId,
            _jsonOptions);

        _createdProjects.Add((projectId, studentId));
        Console.WriteLine($"Created unmatched-class test project: {projectId:N}");
        return projectId;
    }

    public async Task<int?> TryCreateSecondMatchingClassAsync()
    {
        await using var connection = new NpgsqlConnection(_settings.ConnectionString);
        await connection.OpenAsync();

        var secondClassId = await ExecuteScalarAsync<int?>(
            connection,
            """
            SELECT "Id"
            FROM "Classes"
            WHERE "Id" > @classId
            ORDER BY "Id"
            LIMIT 1
            """,
            command => command.Parameters.Add("classId", NpgsqlDbType.Integer).Value = _settings.ClassId);
        if (!secondClassId.HasValue)
        {
            return null;
        }

        var existingAssignmentId = await ExecuteScalarAsync<Guid?>(
            connection,
            """
            SELECT "Id"
            FROM "LabClassAssignments"
            WHERE "LabId" = @labId AND "ClassId" = @classId
            LIMIT 1
            """,
            command =>
            {
                command.Parameters.Add("labId", NpgsqlDbType.Uuid).Value = _settings.LabId;
                command.Parameters.Add("classId", NpgsqlDbType.Integer).Value = secondClassId.Value;
            });
        if (!existingAssignmentId.HasValue)
        {
            var assignmentId = Guid.NewGuid();
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO "LabClassAssignments" ("Id", "LabId", "ClassId", "CreatedAt")
                VALUES (@id, @labId, @classId, @createdAt)
                """,
                connection);
            command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = assignmentId;
            command.Parameters.Add("labId", NpgsqlDbType.Uuid).Value = _settings.LabId;
            command.Parameters.Add("classId", NpgsqlDbType.Integer).Value = secondClassId.Value;
            command.Parameters.Add("createdAt", NpgsqlDbType.TimestampTz).Value = DateTime.UtcNow;
            await command.ExecuteNonQueryAsync();
            _createdLabClassAssignments.Add(assignmentId);
        }

        var existingEnrollmentId = await ExecuteScalarAsync<int?>(
            connection,
            """
            SELECT "Id"
            FROM "Enrollments"
            WHERE "ClassId" = @classId AND "StudentId" = @studentId
            LIMIT 1
            """,
            command =>
            {
                command.Parameters.Add("classId", NpgsqlDbType.Integer).Value = secondClassId.Value;
                command.Parameters.Add("studentId", NpgsqlDbType.Integer).Value = _settings.StudentId;
            });
        if (!existingEnrollmentId.HasValue)
        {
            var now = DateTime.UtcNow;
            var enrollmentId = await ExecuteScalarAsync<int>(
                connection,
                """
                INSERT INTO "Enrollments" ("ClassId", "StudentId", "CreatedAt", "UpdatedAt")
                VALUES (@classId, @studentId, @createdAt, @updatedAt)
                RETURNING "Id"
                """,
                command =>
                {
                    command.Parameters.Add("classId", NpgsqlDbType.Integer).Value = secondClassId.Value;
                    command.Parameters.Add("studentId", NpgsqlDbType.Integer).Value = _settings.StudentId;
                    command.Parameters.Add("createdAt", NpgsqlDbType.TimestampTz).Value = now;
                    command.Parameters.Add("updatedAt", NpgsqlDbType.TimestampTz).Value = now;
                });
            _createdEnrollmentIds.Add(enrollmentId);
        }

        return secondClassId;
    }

    private static async Task<Guid> CreateProjectAsync(
        HttpClient httpClient,
        string studentToken,
        Guid labId,
        JsonSerializerOptions jsonOptions)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentToken);
        var diagram = JsonSerializer.Deserialize<JsonElement>(
            """
            {
              "version": 1,
              "author": "VirtualLabHubContractVerifier",
              "parts": [],
              "connections": []
            }
            """);

        var payload = new
        {
            labId,
            name = $"hub-contract-{Guid.NewGuid():N}"[..32],
            board = "esp32",
            language = "arduino",
            code = "void setup() {}\nvoid loop() {}",
            diagram
        };

        using var response = await httpClient.PostAsJsonAsync("api/virtual-lab/projects", payload, jsonOptions);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create project failed: {(int)response.StatusCode} {body}");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("id", out var idElement) ||
            !Guid.TryParse(idElement.GetString(), out var projectId))
        {
            throw new InvalidOperationException($"Create project response did not include a GUID id: {body}");
        }

        return projectId;
    }

    private static async Task WaitForApiAsync(HttpClient httpClient, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        Exception? lastException = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await httpClient.GetAsync("");
                Console.WriteLine($"API ready probe: {(int)response.StatusCode}");
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;
                await Task.Delay(500);
            }
        }

        throw new TimeoutException($"API was not reachable within {timeout.TotalSeconds:N0}s.", lastException);
    }

    public async Task<ProjectState> GetProjectStateAsync()
    {
        await using var connection = new NpgsqlConnection(_settings.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT "Status", "SimulationEventsJson"::text
            FROM "VirtualLabProjects"
            WHERE "Id" = @projectId
            """,
            connection);
        command.Parameters.Add("projectId", NpgsqlDbType.Uuid).Value = ProjectId;

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException($"Project {ProjectId:N} was not found.");
        }

        return new ProjectState(reader.GetString(0), reader.GetString(1));
    }

    public async Task CleanupAsync()
    {
        await using var connection = new NpgsqlConnection(_settings.ConnectionString);
        await connection.OpenAsync();
        var totalAffected = 0;
        foreach (var project in _createdProjects)
        {
            await using var command = new NpgsqlCommand(
                """
                DELETE FROM "VirtualLabProjects"
                WHERE "Id" = @projectId AND "UserId" = @studentId
                """,
                connection);
            command.Parameters.Add("projectId", NpgsqlDbType.Uuid).Value = project.ProjectId;
            command.Parameters.Add("studentId", NpgsqlDbType.Integer).Value = project.StudentId;
            totalAffected += await command.ExecuteNonQueryAsync();
        }

        foreach (var enrollmentId in _createdEnrollmentIds)
        {
            await using var command = new NpgsqlCommand(
                """
                DELETE FROM "Enrollments"
                WHERE "Id" = @id
                """,
                connection);
            command.Parameters.Add("id", NpgsqlDbType.Integer).Value = enrollmentId;
            totalAffected += await command.ExecuteNonQueryAsync();
        }

        foreach (var assignmentId in _createdLabClassAssignments)
        {
            await using var command = new NpgsqlCommand(
                """
                DELETE FROM "LabClassAssignments"
                WHERE "Id" = @id
                """,
                connection);
            command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = assignmentId;
            totalAffected += await command.ExecuteNonQueryAsync();
        }

        Console.WriteLine($"Cleaned test project rows: {totalAffected}");
    }

    private static async Task<T?> ExecuteScalarAsync<T>(
        NpgsqlConnection connection,
        string sql,
        Action<NpgsqlCommand> configure)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configure(command);
        var result = await command.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
        {
            return default;
        }

        return (T)result;
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await Task.CompletedTask;
    }
}

internal sealed record ProjectState(string Status, string SimulationEventsJson);

internal static class ContractVerifier
{
    public static async Task RunAsync(VerifierSettings settings, TestFixture fixture, JsonSerializerOptions jsonOptions)
    {
        var projectId = fixture.ProjectId.ToString("N");
        await using var classWatcher = await HubClient.ConnectAsync("class-watcher", settings.BaseUrl, settings.TeacherToken, settings.Timeout, jsonOptions);
        await using var staleClassWatcher = await HubClient.ConnectAsync("stale-class-watcher", settings.BaseUrl, settings.TeacherToken, settings.Timeout, jsonOptions);
        await using var projectWatcher = await HubClient.ConnectAsync("project-watcher", settings.BaseUrl, settings.TeacherToken, settings.Timeout, jsonOptions);
        await using var staleProjectWatcher = await HubClient.ConnectAsync("stale-project-watcher", settings.BaseUrl, settings.TeacherToken, settings.Timeout, jsonOptions);
        await using var student = await HubClient.ConnectAsync("student", settings.BaseUrl, settings.StudentToken, settings.Timeout, jsonOptions);

        await classWatcher.InvokeAsync("WatchClass", settings.ClassId);
        await staleClassWatcher.InvokeAsync("WatchClass", settings.ClassId);
        await staleClassWatcher.InvokeAsync("UnwatchClass", settings.ClassId);
        await projectWatcher.InvokeAsync("WatchStudent", projectId);
        await staleProjectWatcher.InvokeAsync("WatchStudent", projectId);
        await staleProjectWatcher.InvokeAsync("UnwatchStudent", projectId);
        Console.WriteLine("PASS: WatchClass/UnwatchClass/WatchStudent/UnwatchStudent invocations completed.");

        ClearAll(classWatcher, staleClassWatcher, projectWatcher, staleProjectWatcher, student);
        await student.InvokeAsync("JoinSession", projectId);
        await classWatcher.ExpectEventAsync("StudentJoined", args => ArgString(args, 0) == projectId && ArgInt(args, 1) == settings.StudentId);
        await staleClassWatcher.ExpectNoEventAsync("StudentJoined");
        await projectWatcher.ExpectNoEventAsync("StudentJoined");
        Console.WriteLine("PASS: JoinSession routed StudentJoined only to active class watchers.");

        await VerifyJoinSessionWithMultipleMatchingClassesAsync(settings, fixture, jsonOptions, classWatcher);
        await VerifyJoinSessionWithoutMatchingClassAsync(settings, fixture, jsonOptions, classWatcher);

        ClearAll(classWatcher, projectWatcher, staleProjectWatcher, student);
        const string diagramJson = "{\"parts\":[],\"connections\":[]}";
        await student.InvokeAsync("DiagramUpdated", projectId, diagramJson);
        await projectWatcher.ExpectEventAsync("StudentDiagramUpdated", args => ArgString(args, 0) == projectId && ArgString(args, 1) == diagramJson);
        await classWatcher.ExpectNoEventAsync("StudentDiagramUpdated");
        await staleProjectWatcher.ExpectNoEventAsync("StudentDiagramUpdated");
        Console.WriteLine("PASS: DiagramUpdated routed only to active project watchers.");

        ClearAll(classWatcher, projectWatcher, staleProjectWatcher, student);
        const string sourceCode = "void setup() { Serial.begin(115200); }";
        await student.InvokeAsync("CodeUpdated", projectId, sourceCode);
        await projectWatcher.ExpectEventAsync("StudentCodeUpdated", args => ArgString(args, 0) == projectId && ArgString(args, 1) == sourceCode);
        await classWatcher.ExpectNoEventAsync("StudentCodeUpdated");
        await staleProjectWatcher.ExpectNoEventAsync("StudentCodeUpdated");
        Console.WriteLine("PASS: CodeUpdated routed only to active project watchers.");

        ClearAll(classWatcher, projectWatcher, staleClassWatcher, student);
        await student.InvokeAsync("CompileStarted", projectId);
        await classWatcher.ExpectEventAsync("StudentCompileStarted", args => ArgString(args, 0) == projectId);
        await projectWatcher.ExpectEventAsync("StudentCompileStarted", args => ArgString(args, 0) == projectId);
        await staleClassWatcher.ExpectNoEventAsync("StudentCompileStarted");
        Console.WriteLine("PASS: CompileStarted routed to class and project groups.");

        ClearAll(classWatcher, projectWatcher, student);
        await student.InvokeAsync("CompileFinished", projectId, true, null);
        await classWatcher.ExpectEventAsync("StudentCompileFinished", args => ArgString(args, 0) == projectId && ArgBool(args, 1) && args[2].ValueKind == JsonValueKind.Null);
        await projectWatcher.ExpectEventAsync("StudentCompileFinished", args => ArgString(args, 0) == projectId && ArgBool(args, 1) && args[2].ValueKind == JsonValueKind.Null);
        Console.WriteLine("PASS: CompileFinished routed to class and project groups.");

        ClearAll(classWatcher, projectWatcher, student);
        await student.InvokeAsync("RunStarted", projectId);
        await projectWatcher.ExpectEventAsync("StudentRunStarted", args => ArgString(args, 0) == projectId);
        await classWatcher.ExpectNoEventAsync("StudentRunStarted");
        var state = await fixture.GetProjectStateAsync();
        AssertEqual("running", state.Status, "RunStarted should set Status=running.");
        AssertEventCount(state.SimulationEventsJson, 0, "RunStarted should reset SimulationEventsJson.");
        Console.WriteLine("PASS: RunStarted routed to project group and reset DB events.");

        ClearAll(classWatcher, projectWatcher, student);
        var firstEvent = JsonSerializer.Deserialize<JsonElement>(
            """
            { "type": "serial", "time": 1, "payload": { "message": "one" } }
            """);
        await student.InvokeAsync("SimulationEvent", projectId, firstEvent);
        await projectWatcher.ExpectEventAsync("StudentSimulationEvent", args => ArgString(args, 0) == projectId && ArgString(args[1], "type") == "serial");
        await classWatcher.ExpectNoEventAsync("StudentSimulationEvent");
        state = await fixture.GetProjectStateAsync();
        AssertEventCount(state.SimulationEventsJson, 1, "SimulationEvent should append one DB event.");
        Console.WriteLine("PASS: SimulationEvent routed to project group and appended to DB.");

        await VerifyConcurrentSimulationEventsAsync(settings, fixture, jsonOptions, projectId);

        ClearAll(classWatcher, projectWatcher, student);
        await student.InvokeAsync("Stopped", projectId);
        await classWatcher.ExpectEventAsync("StudentStopped", args => ArgString(args, 0) == projectId);
        await projectWatcher.ExpectEventAsync("StudentStopped", args => ArgString(args, 0) == projectId);
        state = await fixture.GetProjectStateAsync();
        AssertEqual("stopped", state.Status, "Stopped should set Status=stopped.");
        Console.WriteLine("PASS: Stopped routed to class/project groups and persisted DB status.");

        ClearAll(classWatcher, projectWatcher, student);
        const int submissionId = 123456;
        await student.InvokeAsync("Submitted", projectId, submissionId);
        await classWatcher.ExpectEventAsync("StudentSubmitted", args => ArgString(args, 0) == projectId && ArgInt(args, 1) == submissionId);
        await projectWatcher.ExpectEventAsync("StudentSubmitted", args => ArgString(args, 0) == projectId && ArgInt(args, 1) == submissionId);
        Console.WriteLine("PASS: Submitted routed to class/project groups.");

        ClearAll(projectWatcher, staleProjectWatcher, student);
        await projectWatcher.InvokeAsync("UnwatchStudent", projectId);
        await student.InvokeAsync("CodeUpdated", projectId, "after-unwatch");
        await projectWatcher.ExpectNoEventAsync("StudentCodeUpdated");
        await staleProjectWatcher.ExpectNoEventAsync("StudentCodeUpdated");
        Console.WriteLine("PASS: UnwatchStudent removed project watcher from detail stream.");

        ClearAll(student);
        const string guidance = "Check your GND connection.";
        await classWatcher.InvokeAsync("SendGuidance", projectId, guidance);
        await student.ExpectEventAsync("ReceiveGuidance", args => ArgString(args, 0) == guidance && ArgString(args, 1) == "Hub Contract Teacher");
        Console.WriteLine("PASS: SendGuidance routed ReceiveGuidance to project group.");
    }

    private static async Task VerifyConcurrentSimulationEventsAsync(
        VerifierSettings settings,
        TestFixture fixture,
        JsonSerializerOptions jsonOptions,
        string projectId)
    {
        const int eventCount = 6;
        var clients = new List<HubClient>();
        try
        {
            for (var i = 0; i < eventCount; i++)
            {
                var client = await HubClient.ConnectAsync($"concurrent-student-{i}", settings.BaseUrl, settings.StudentToken, settings.Timeout, jsonOptions);
                clients.Add(client);
                await client.InvokeAsync("JoinSession", projectId);
            }

            var tasks = clients.Select((client, index) =>
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(
                    $$"""
                    { "type": "serial", "time": {{index + 2}}, "payload": { "index": {{index}} } }
                    """);
                return client.InvokeAsync("SimulationEvent", projectId, payload);
            });

            await Task.WhenAll(tasks);
            var state = await fixture.GetProjectStateAsync();
            AssertEventCount(state.SimulationEventsJson, eventCount + 1, "Concurrent SimulationEvent calls should not lose DB events.");
            AssertConcurrentIndexes(state.SimulationEventsJson, eventCount);
            Console.WriteLine($"PASS: Concurrent SimulationEvent append kept all {eventCount} events.");
        }
        finally
        {
            foreach (var client in clients)
            {
                await client.DisposeAsync();
            }
        }
    }

    private static async Task VerifyJoinSessionWithoutMatchingClassAsync(
        VerifierSettings settings,
        TestFixture fixture,
        JsonSerializerOptions jsonOptions,
        HubClient classWatcher)
    {
        var projectId = (await fixture.CreateProjectForStudentAsync(
            settings.NonMatchingStudentToken,
            settings.NonMatchingStudentId)).ToString("N");
        await using var student = await HubClient.ConnectAsync(
            "non-matching-student",
            settings.BaseUrl,
            settings.NonMatchingStudentToken,
            settings.Timeout,
            jsonOptions);

        ClearAll(classWatcher, student);
        await student.InvokeAsync("JoinSession", projectId);
        await classWatcher.ExpectNoEventAsync("StudentJoined");
        Console.WriteLine("PASS: JoinSession without a matching LabClassAssignment/Enrollment joined project only and did not broadcast StudentJoined.");
    }

    private static async Task VerifyJoinSessionWithMultipleMatchingClassesAsync(
        VerifierSettings settings,
        TestFixture fixture,
        JsonSerializerOptions jsonOptions,
        HubClient classWatcher)
    {
        var secondClassId = await fixture.TryCreateSecondMatchingClassAsync();
        if (!secondClassId.HasValue)
        {
            Console.WriteLine("SKIP: JoinSession multiple matching classes edge case because no second class was available.");
            return;
        }

        await using var student = await HubClient.ConnectAsync(
            "multi-class-student",
            settings.BaseUrl,
            settings.StudentToken,
            settings.Timeout,
            jsonOptions);

        var projectId = fixture.ProjectId.ToString("N");
        ClearAll(classWatcher, student);
        await student.InvokeAsync("JoinSession", projectId);
        await classWatcher.ExpectEventAsync("StudentJoined", args => ArgString(args, 0) == projectId && ArgInt(args, 1) == settings.StudentId);
        Console.WriteLine($"PASS: JoinSession with multiple matching classes selected class {settings.ClassId} before class {secondClassId.Value}.");
    }

    private static void ClearAll(params HubClient[] clients)
    {
        foreach (var client in clients)
        {
            client.ClearEvents();
        }
    }

    private static string? ArgString(IReadOnlyList<JsonElement> args, int index)
    {
        return args.Count > index && args[index].ValueKind == JsonValueKind.String
            ? args[index].GetString()
            : null;
    }

    private static string? ArgString(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(property, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? ArgInt(IReadOnlyList<JsonElement> args, int index)
    {
        return args.Count > index && args[index].ValueKind == JsonValueKind.Number && args[index].TryGetInt32(out var value)
            ? value
            : null;
    }

    private static bool ArgBool(IReadOnlyList<JsonElement> args, int index)
    {
        return args.Count > index && args[index].ValueKind == JsonValueKind.True;
    }

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
        }
    }

    private static void AssertEventCount(string eventsJson, int expectedCount, string message)
    {
        using var document = JsonDocument.Parse(eventsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"{message} SimulationEventsJson is not an array: {eventsJson}");
        }

        var actual = document.RootElement.GetArrayLength();
        if (actual != expectedCount)
        {
            throw new InvalidOperationException($"{message} Expected {expectedCount}, got {actual}: {eventsJson}");
        }
    }

    private static void AssertConcurrentIndexes(string eventsJson, int expectedCount)
    {
        using var document = JsonDocument.Parse(eventsJson);
        var indexes = document.RootElement
            .EnumerateArray()
            .Where(item =>
                item.TryGetProperty("payload", out var payload) &&
                payload.TryGetProperty("index", out var index) &&
                index.TryGetInt32(out _))
            .Select(item => item.GetProperty("payload").GetProperty("index").GetInt32())
            .OrderBy(value => value)
            .ToArray();

        var expected = Enumerable.Range(0, expectedCount).ToArray();
        if (!indexes.SequenceEqual(expected))
        {
            throw new InvalidOperationException($"Concurrent event indexes mismatch. Expected [{string.Join(",", expected)}], got [{string.Join(",", indexes)}].");
        }
    }
}

internal sealed class HubClient : IAsyncDisposable
{
    private readonly ClientWebSocket _webSocket = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement?>> _pendingInvocations = new();
    private readonly ConcurrentQueue<HubEvent> _events = new();
    private readonly Uri _baseUrl;
    private readonly string _token;
    private readonly TimeSpan _timeout;
    private readonly JsonSerializerOptions _jsonOptions;
    private Task? _receiveTask;
    private int _nextInvocationId;

    private HubClient(string name, Uri baseUrl, string token, TimeSpan timeout, JsonSerializerOptions jsonOptions)
    {
        Name = name;
        _baseUrl = baseUrl;
        _token = token;
        _timeout = timeout;
        _jsonOptions = jsonOptions;
    }

    public string Name { get; }

    public static async Task<HubClient> ConnectAsync(
        string name,
        Uri baseUrl,
        string token,
        TimeSpan timeout,
        JsonSerializerOptions jsonOptions)
    {
        var client = new HubClient(name, baseUrl, token, timeout, jsonOptions);
        await client.ConnectAsync();
        return client;
    }

    public async Task InvokeAsync(string target, params object?[] arguments)
    {
        var invocationId = Interlocked.Increment(ref _nextInvocationId).ToString();
        var completion = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingInvocations[invocationId] = completion;

        await SendAsync(new
        {
            type = 1,
            invocationId,
            target,
            arguments
        });

        using var timeout = new CancellationTokenSource(_timeout);
        await using (timeout.Token.Register(() => completion.TrySetCanceled(timeout.Token)))
        {
            try
            {
                await completion.Task;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"{Name}: invocation {target} did not complete within {_timeout.TotalSeconds:N0}s.");
            }
        }
    }

    public async Task ExpectEventAsync(string target, Func<IReadOnlyList<JsonElement>, bool> predicate)
    {
        var deadline = DateTimeOffset.UtcNow.Add(_timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var match = _events.ToArray().Any(item => item.Target == target && predicate(item.Arguments));
            if (match)
            {
                return;
            }

            await Task.Delay(50, _cts.Token);
        }

        var seen = string.Join(", ", _events.ToArray().Select(item => item.Target));
        throw new TimeoutException($"{Name}: expected event {target} was not observed. Seen: [{seen}]");
    }

    public async Task ExpectNoEventAsync(string target)
    {
        await Task.Delay(350, _cts.Token);
        if (_events.ToArray().Any(item => item.Target == target))
        {
            throw new InvalidOperationException($"{Name}: unexpected event {target} was observed.");
        }
    }

    public void ClearEvents()
    {
        while (_events.TryDequeue(out _))
        {
        }
    }

    private async Task ConnectAsync()
    {
        using var httpClient = new HttpClient { BaseAddress = _baseUrl };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        using var response = await httpClient.PostAsync("hubs/virtual-lab/negotiate?negotiateVersion=1", content: null);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{Name}: negotiate failed: {(int)response.StatusCode} {body}");
        }

        using var document = JsonDocument.Parse(body);
        var tokenProperty = document.RootElement.TryGetProperty("connectionToken", out var connectionToken)
            ? connectionToken
            : document.RootElement.GetProperty("connectionId");
        var connectUrl = BuildWebSocketUrl(_baseUrl, tokenProperty.GetString() ?? throw new InvalidOperationException($"{Name}: negotiate response has no connection token."));

        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_token}");
        using var connectTimeout = new CancellationTokenSource(_timeout);
        await _webSocket.ConnectAsync(connectUrl, connectTimeout.Token);
        await SendRawAsync($$"""{"protocol":"json","version":1}{{SignalRProtocol.RecordSeparator}}""");
        await ReceiveHandshakeAsync(connectTimeout.Token);
        _receiveTask = Task.Run(ReceiveLoopAsync);
    }

    private async Task SendAsync(object message)
    {
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        await SendRawAsync(json + SignalRProtocol.RecordSeparator);
    }

    private async Task SendRawAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, _cts.Token);
    }

    private async Task ReceiveHandshakeAsync(CancellationToken cancellationToken)
    {
        var message = await ReceiveRawMessageAsync(cancellationToken);
        var records = SplitRecords(message).ToArray();
        if (records.Length == 0)
        {
            throw new InvalidOperationException($"{Name}: empty SignalR handshake response.");
        }

        using var document = JsonDocument.Parse(records[0]);
        if (document.RootElement.TryGetProperty("error", out var error))
        {
            throw new InvalidOperationException($"{Name}: SignalR handshake error: {error.GetString()}");
        }
    }

    private async Task ReceiveLoopAsync()
    {
        while (!_cts.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
        {
            string raw;
            try
            {
                raw = await ReceiveRawMessageAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (WebSocketException)
            {
                break;
            }

            foreach (var record in SplitRecords(raw))
            {
                ProcessRecord(record);
            }
        }
    }

    private async Task<string> ReceiveRawMessageAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await _webSocket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return string.Empty;
            }

            stream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private void ProcessRecord(string record)
    {
        using var document = JsonDocument.Parse(record);
        var root = document.RootElement;
        if (!root.TryGetProperty("type", out var typeElement))
        {
            return;
        }

        var type = typeElement.GetInt32();
        if (type == 1 && root.TryGetProperty("target", out var targetElement))
        {
            var arguments = root.TryGetProperty("arguments", out var argsElement) && argsElement.ValueKind == JsonValueKind.Array
                ? argsElement.EnumerateArray().Select(item => item.Clone()).ToArray()
                : [];
            _events.Enqueue(new HubEvent(targetElement.GetString() ?? string.Empty, arguments));
            return;
        }

        if (type == 3 && root.TryGetProperty("invocationId", out var invocationElement))
        {
            var invocationId = invocationElement.GetString();
            if (invocationId != null && _pendingInvocations.TryRemove(invocationId, out var completion))
            {
                if (root.TryGetProperty("error", out var error))
                {
                    completion.TrySetException(new InvalidOperationException($"{Name}: hub invocation failed: {error.GetString()}"));
                }
                else if (root.TryGetProperty("result", out var result))
                {
                    completion.TrySetResult(result.Clone());
                }
                else
                {
                    completion.TrySetResult(null);
                }
            }
        }
    }

    private static Uri BuildWebSocketUrl(Uri baseUrl, string connectionToken)
    {
        var builder = new UriBuilder(baseUrl)
        {
            Scheme = baseUrl.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws",
            Path = CombinePath(baseUrl.AbsolutePath, "hubs/virtual-lab"),
            Query = $"id={Uri.EscapeDataString(connectionToken)}"
        };

        return builder.Uri;
    }

    private static string CombinePath(string first, string second)
    {
        return $"{first.TrimEnd('/')}/{second.TrimStart('/')}";
    }

    private static IEnumerable<string> SplitRecords(string raw)
    {
        return raw.Split(SignalRProtocol.RecordSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
            catch (WebSocketException)
            {
            }
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _webSocket.Dispose();
        _cts.Dispose();
    }
}

internal sealed record HubEvent(string Target, IReadOnlyList<JsonElement> Arguments);

internal static class SignalRProtocol
{
    public const char RecordSeparator = '\u001e';
}
