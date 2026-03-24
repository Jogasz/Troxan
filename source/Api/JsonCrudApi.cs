using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Sources;

internal sealed class JsonCrudApi : IDisposable
{
    // If constructed with a baseUrl the API will send HTTP requests to that server.
    // If constructed without a baseUrl it will operate in local demo mode (useful for offline dev).

    readonly HttpClient? httpClient;
    readonly bool remoteMode;
    // If the configured BaseUrl is a full routed endpoint (contains api.php or query string),
    // store it here so we can request the absolute URI directly instead of relying on BaseAddress.
    readonly string? configuredAbsoluteUrl;

    // Simple local "auth database" used only when remoteMode == false
    static readonly Dictionary<string, string> DemoUsers = new()
    {
        ["alice"] = "password123",
        ["bob"] = "hunter2",
        ["guest"] = "guest"
    };

    // After successful login this will contain the logged in username (null otherwise)
    public string? LoggedInUser { get; private set; }

    // Optional auth token returned by server (e.g. JWT or session token)
    string? authToken;
    public string? AuthToken => authToken;

    // Default: demo/local mode
    public JsonCrudApi()
    {
        httpClient = null;
        remoteMode = false;
    }

    // Remote mode: send requests to given baseUrl (e.g. "https://api.example.com/")
    public JsonCrudApi(string baseUrl)
    {
        // If the provided baseUrl already contains a routed endpoint (api.php / path= / ?),
        // we prefer to keep it as an absolute request URI and not set HttpClient.BaseAddress.
        if (!string.IsNullOrEmpty(baseUrl) && (baseUrl.Contains("api.php") || baseUrl.Contains("path=") || baseUrl.Contains('?')))
        {
            httpClient = new HttpClient();
            configuredAbsoluteUrl = baseUrl;
        }
        else
        {
            httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            configuredAbsoluteUrl = null;
        }
        remoteMode = true;
        // If a token was persisted in settings, attach it
        if (!string.IsNullOrEmpty(Sources.Settings.Api.Token))
        {
            authToken = Sources.Settings.Api.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }
    }

    // Authenticate: in remote mode POST credentials to /auth/login and accept server response.
    // In demo mode validate against DemoUsers.
    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        if (!remoteMode || httpClient is null)
        {
            bool ok = DemoUsers.TryGetValue(username, out var existing) && existing == password;
            if (ok) LoggedInUser = username;
            return ok;
        }

        // Remote authentication flow. The server contract isn't fixed yet so be permissive:
        // - POST /auth/login with JSON { username, password }
        // - treat 2xx as potentially successful; attempt to parse { success: bool } or { username: string }

        var payload = new { username, password };
        // Determine configured base (could be absolute routed URL stored separately)
        string? configured = configuredAbsoluteUrl ?? httpClient.BaseAddress?.ToString();
        // If the configured BaseUrl already contains the full routed endpoint (e.g. api.php?path=...)
        // then use it as the request URI. Otherwise use the default relative endpoint.
        string requestUri = "auth/login";
        if (!string.IsNullOrEmpty(configured) && (configured.Contains("api.php") || configured.Contains("path=") || configured.Contains('?')))
        {
            requestUri = configured!;
        }
        try
        {
            Console.WriteLine($"Attempting remote auth at {configured}... (request: {requestUri})");
            var resp = await httpClient.PostAsJsonAsync(requestUri, payload);

            // If server returned 5xx or similar, treat as unavailable and fall back to local demo
            if (!resp.IsSuccessStatusCode)
            {
                int code = (int)resp.StatusCode;
                if (code >= 500)
                {
                    Console.WriteLine($"Remote auth endpoint returned {resp.StatusCode}. Falling back to local auth.");
                    // fall through to local fallback below
                }
                else
                {
                    // 4xx - authentication failed on server (bad creds)
                    return false;
                }
            }

            // Try to read JSON body and print raw body for debug
            var rawBody = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"Login response body: {rawBody}");

            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            // helper to extract token from an element (root or nested data)
            static string? ExtractToken(JsonElement el)
            {
                if (el.ValueKind != JsonValueKind.Object) return null;
                if (el.TryGetProperty("token", out var t) && t.ValueKind == JsonValueKind.String)
                    return t.GetString();
                if (el.TryGetProperty("user_token", out var ut) && ut.ValueKind == JsonValueKind.String)
                    return ut.GetString();
                return null;
            }

            // Check for various server conventions: boolean "success", string "status" == "success", or presence of username
            if (root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True)
            {
                LoggedInUser = username;
                // Prefer any token the server just returned (overwrite persisted)
                var newTok = ExtractToken(root);
                if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object)
                    newTok ??= ExtractToken(d);
                if (!string.IsNullOrEmpty(newTok))
                {
                    authToken = newTok.Trim();
                    if (httpClient is not null)
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    }
                    Console.WriteLine("Auth token received and set on HttpClient.");
                    try { Sources.Settings.Api.Token = authToken; Sources.Settings.Save("settings.json"); } catch { }
                }
                return true;
            }

            if (root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String && string.Equals(status.GetString(), "success", StringComparison.OrdinalIgnoreCase))
            {
                // server returns { status: "success", data: { username, token, ... } }
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    if (data.TryGetProperty("username", out var du) && du.ValueKind == JsonValueKind.String)
                        LoggedInUser = du.GetString();
                    // Prefer server token if present
                    var newTok2 = ExtractToken(data);
                    if (!string.IsNullOrEmpty(newTok2))
                    {
                        authToken = newTok2.Trim();
                    }
                    // Also extract optional username from data; numeric stats are merged later by GetPlayerStatsAsync
                    try
                    {
                        if (data.TryGetProperty("username", out var nameVal) && nameVal.ValueKind == JsonValueKind.String)
                            Sources.Settings.Player.Username = nameVal.GetString();
                    }
                    catch { }
                }
                // fallback to top-level username
                if (string.IsNullOrEmpty(LoggedInUser) && root.TryGetProperty("username", out var ru) && ru.ValueKind == JsonValueKind.String)
                    LoggedInUser = ru.GetString();

                if (authToken is not null && httpClient is not null)
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    Console.WriteLine("Auth token received and set on HttpClient.");
                    try { Sources.Settings.Api.Token = authToken; Sources.Settings.Save("settings.json"); } catch { }
                }
                return true;
            }

            if (root.TryGetProperty("success", out s) && s.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (root.TryGetProperty("username", out var u) && u.ValueKind == JsonValueKind.String)
            {
                LoggedInUser = u.GetString();
                // Prefer server token if present
                var newTok3 = ExtractToken(root);
                if (root.TryGetProperty("data", out var d2) && d2.ValueKind == JsonValueKind.Object)
                    newTok3 ??= ExtractToken(d2);
                if (!string.IsNullOrEmpty(newTok3))
                {
                    authToken = newTok3.Trim();
                    if (httpClient is not null)
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    Console.WriteLine("Auth token received and set on HttpClient.");
                    try { Sources.Settings.Api.Token = authToken; Sources.Settings.Save("settings.json"); } catch { }
                }
                return !string.IsNullOrEmpty(LoggedInUser);
            }

            // If server returned any JSON and status was success-like via HTTP status code, accept it.
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Remote auth attempt failed ({ex.Message}). Falling back to local demo auth.");
            // fall through to local fallback
        }

        // Local fallback when remote unreachable
        bool okLocal = DemoUsers.TryGetValue(username, out var existingLocal) && existingLocal == password;
        if (okLocal)
        {
            LoggedInUser = username;
            Console.WriteLine("Local demo auth succeeded (used fallback).");
            authToken = null;
        }
        else
        {
            Console.WriteLine("Local demo auth failed.");
            authToken = null;
        }

        return okLocal;
    }

    // Placeholder: fetch player data from server when in remote mode; otherwise return demo payload.
    public async Task<string?> GetPlayerDataAsync()
    {
        if (LoggedInUser is null) return null;

        if (!remoteMode || httpClient is null)
        {
            var obj = new { Username = LoggedInUser, Coins = 0, Level = 1 };
            return JsonSerializer.Serialize(obj);
        }

        try
        {
            // If BaseAddress already contains a routed endpoint, try to use it for stats
            string? baseUrl = configuredAbsoluteUrl ?? httpClient.BaseAddress?.ToString();
            string statsUri = "player/me";
            if (!string.IsNullOrEmpty(baseUrl) && (baseUrl.Contains("api.php") || baseUrl.Contains("path=") || baseUrl.Contains('?')))
            {
                // Common pattern: if login is routed with ?path=game_login then stats might be ?path=game_stats
                if (baseUrl.Contains("game_login"))
                    statsUri = baseUrl.Replace("game_login", "game_stats");
                else
                    statsUri = baseUrl;
            }

            // Build explicit request so we can ensure Authorization header is present on the outgoing request
            var request = new HttpRequestMessage(HttpMethod.Get, statsUri);
            // Prefer the most-recent token (authToken field) to avoid sending an old persisted header
            Console.WriteLine($"GetPlayerDataAsync: persisted token (settings)='{Sources.Settings.Api.Token}' authToken='{authToken}' httpClientHeader='{httpClient.DefaultRequestHeaders.Authorization?.Parameter}'");
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                Console.WriteLine($"GetPlayerDataAsync: sending Authorization: Bearer {authToken}");
            }
            else if (httpClient.DefaultRequestHeaders.Authorization is not null)
            {
                request.Headers.Authorization = httpClient.DefaultRequestHeaders.Authorization;
                Console.WriteLine($"GetPlayerDataAsync: sending Authorization from HttpClient.DefaultRequestHeaders: Bearer {httpClient.DefaultRequestHeaders.Authorization.Parameter}");
            }
            Console.WriteLine($"GetPlayerDataAsync: requesting {statsUri}");
            var resp = await httpClient.SendAsync(request);
            if (!resp.IsSuccessStatusCode)
            {
                // Debug: print response to help diagnose why no stats returned
                var body = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"GetPlayerDataAsync: remote returned {(int)resp.StatusCode} {resp.StatusCode}. Body: {body}");
                if ((int)resp.StatusCode == 401)
                    throw new UnauthorizedAccessException("Unauthorized when fetching player stats");
                return null;
            }
            return await resp.Content.ReadAsStringAsync();
        }
        catch (UnauthorizedAccessException)
        {
            // propagate unauthorized so caller can handle re-login
            throw;
        }
        catch
        {
            return null;
        }
    }

    // Higher level helper: get parsed player stats (coins, level, username)
    public async Task<PlayerStats?> GetPlayerStatsAsync()
    {
        var json = await GetPlayerDataAsync();
        if (json is null) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Determine payload location: prefer data object when present
            JsonElement payload = root;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                payload = data;

            // helper to read int/string safely
            static int ReadInt(JsonElement el, string name)
            {
                if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number) return v.GetInt32();
                return 0;
            }
            static string? ReadString(JsonElement el, string name)
            {
                if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();
                return null;
            }

            // Basic fields
            string? username = ReadString(payload, "username") ?? ReadString(root, "username");
            int coins = ReadInt(payload, "coins");
            int level = ReadInt(payload, "level");

            // Advanced statistics: could be direct or inside a "statistics" / "statistics_file" object
            string? joinTime = null;
            string? timePlayed = null;
            int numStories = 0;
            int numEnemies = 0;
            int numDeaths = 0;
            int score = 0;

            if (payload.TryGetProperty("statistics", out var statsObj) && statsObj.ValueKind == JsonValueKind.Object)
            {
                joinTime = ReadString(statsObj, "join_time");
                timePlayed = ReadString(statsObj, "time_played");
                numStories = ReadInt(statsObj, "num_of_story_finished");
                numEnemies = ReadInt(statsObj, "num_of_enemies_killed");
                numDeaths = ReadInt(statsObj, "num_of_deaths");
                score = ReadInt(statsObj, "score");
            }
            else if (payload.TryGetProperty("statistics_file", out var statsFile) && statsFile.ValueKind == JsonValueKind.String)
            {
                try
                {
                    using var statsDoc = JsonDocument.Parse(statsFile.GetString()!);
                    var sroot = statsDoc.RootElement;
                    joinTime = ReadString(sroot, "join_time");
                    timePlayed = ReadString(sroot, "time_played");
                    numStories = ReadInt(sroot, "num_of_story_finished");
                    numEnemies = ReadInt(sroot, "num_of_enemies_killed");
                    numDeaths = ReadInt(sroot, "num_of_deaths");
                    score = ReadInt(sroot, "score");
                }
                catch { }
            }
            else
            {
                // Try top-level fields as fallback
                joinTime = ReadString(root, "join_time");
                timePlayed = ReadString(root, "time_played");
                numStories = ReadInt(root, "num_of_story_finished");
                numEnemies = ReadInt(root, "num_of_enemies_killed");
                numDeaths = ReadInt(root, "num_of_deaths");
                score = ReadInt(root, "score");
            }

            // Populate Settings runtime fields so UI can read them
            if (username is not null) Sources.Settings.Player.Username = username;
            // Use server-provided stats as authoritative baseline for runtime values (overwrite local)
            Sources.Settings.Player.Coins = coins;
            Sources.Settings.Player.Level = level;
            Sources.Settings.Player.NumOfStoryFinished = numStories;
            Sources.Settings.Player.NumOfEnemiesKilled = numEnemies;
            Sources.Settings.Player.NumOfDeaths = numDeaths;
            Sources.Settings.Player.Score = score;

            // JoinTime and TimePlayed stored as strings everywhere; accept server values directly
            if (!string.IsNullOrEmpty(joinTime))
                Sources.Settings.Player.JoinTime = joinTime;
            if (!string.IsNullOrEmpty(timePlayed))
                Sources.Settings.Player.TimePlayed = timePlayed;

            // Persist fetched runtime stats
            try { Sources.Settings.Save("settings.json"); } catch { }

            return new PlayerStats(username ?? LoggedInUser, coins, level);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        httpClient?.Dispose();
    }

    // Push current player statistics to server. Returns true on success.
    public async Task<bool> UpdatePlayerStatsAsync()
    {
        if (!remoteMode || httpClient is null)
            return false;

        // Build payload from Settings.Player runtime values
        var payload = new
        {
            username = Sources.Settings.Player.Username,
            coins = Sources.Settings.Player.Coins,
            level = Sources.Settings.Player.Level,
            statistics = new
            {
                join_time = Sources.Settings.Player.JoinTime ?? string.Empty,
                time_played = Sources.Settings.Player.TimePlayed ?? string.Empty,
                num_of_story_finished = Sources.Settings.Player.NumOfStoryFinished,
                num_of_enemies_killed = Sources.Settings.Player.NumOfEnemiesKilled,
                num_of_deaths = Sources.Settings.Player.NumOfDeaths,
                score = Sources.Settings.Player.Score
            }
        };

        // Determine request URI: if BaseAddress contains routed api.php use game_update_stats, otherwise use player/update
        string? baseUrl = configuredAbsoluteUrl ?? httpClient.BaseAddress?.ToString();
        string requestUri = "player/update";
        if (!string.IsNullOrEmpty(baseUrl) && (baseUrl.Contains("api.php") || baseUrl.Contains("path=") || baseUrl.Contains('?')))
        {
            if (baseUrl.Contains("game_login"))
                requestUri = baseUrl.Replace("game_login", "game_update_stats");
            else
                requestUri = baseUrl;
        }

        try
        {
            Console.WriteLine($"Updating player stats to {requestUri} ...");
            var resp = await httpClient.PostAsJsonAsync(requestUri, payload);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"UpdatePlayerStatsAsync: server returned {(int)resp.StatusCode} {resp.StatusCode}. Body: {body}");
                return false;
            }
            Console.WriteLine("UpdatePlayerStatsAsync: success.");
            try { Sources.Settings.Save("settings.json"); } catch { }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdatePlayerStatsAsync: exception: {ex.Message}");
            return false;
        }
    }
}

internal sealed record PlayerStats(string? Username, int Coins, int Level);
