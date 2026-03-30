using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Sources;

internal sealed class JsonCrudApi : IDisposable
{
    //HTTP or local mode

    readonly HttpClient? httpClient;
    readonly bool remoteMode;
    //Full routed endpoint cache
    readonly string? configuredAbsoluteUrl;

    //Local demo users
    static readonly Dictionary<string, string> DemoUsers = new()
    {
        ["alice"] = "password123",
        ["bob"] = "hunter2",
        ["guest"] = "guest"
    };

    //Current user
    public string? LoggedInUser { get; private set; }

    //Current token
    string? authToken;
    public string? AuthToken => authToken;

    //Demo mode
    public JsonCrudApi()
    {
        httpClient = null;
        remoteMode = false;
    }

    //Remote mode
    public JsonCrudApi(string baseUrl)
    {
        //Routed endpoint handling
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
        //Load persisted token
        if (!string.IsNullOrEmpty(Sources.Settings.Api.Token))
        {
            authToken = Sources.Settings.Api.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }
    }

    //Login
    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        if (!remoteMode || httpClient is null)
        {
            bool ok = DemoUsers.TryGetValue(username, out var existing) && existing == password;
            if (ok) LoggedInUser = username;
            return ok;
        }

        var payload = new { username, password };
        //Configured endpoint
        string? configured = configuredAbsoluteUrl ?? httpClient.BaseAddress?.ToString();
        //Request target
        string requestUri = "auth/login";
        if (!string.IsNullOrEmpty(configured) && (configured.Contains("api.php") || configured.Contains("path=") || configured.Contains('?')))
        {
            requestUri = configured!;
        }
        try
        {
            Console.WriteLine($"Attempting remote auth at {configured}... (request: {requestUri})");
            var resp = await httpClient.PostAsJsonAsync(requestUri, payload);

            if (!resp.IsSuccessStatusCode)
            {
                int code = (int)resp.StatusCode;
                if (code >= 500)
                {
                    Console.WriteLine($"Remote auth endpoint returned {resp.StatusCode}. Falling back to local auth.");
                }
                else
                {
                    return false;
                }
            }

            var rawBody = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"Login response body: {rawBody}");

            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            //Token parser
            static string? ExtractToken(JsonElement el)
            {
                if (el.ValueKind != JsonValueKind.Object) return null;
                if (el.TryGetProperty("token", out var t) && t.ValueKind == JsonValueKind.String)
                    return t.GetString();
                if (el.TryGetProperty("user_token", out var ut) && ut.ValueKind == JsonValueKind.String)
                    return ut.GetString();
                return null;
            }

            if (root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True)
            {
                LoggedInUser = username;
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
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    if (data.TryGetProperty("username", out var du) && du.ValueKind == JsonValueKind.String)
                        LoggedInUser = du.GetString();
                    var newTok2 = ExtractToken(data);
                    if (!string.IsNullOrEmpty(newTok2))
                    {
                        authToken = newTok2.Trim();
                    }
                    try
                    {
                        if (data.TryGetProperty("username", out var nameVal) && nameVal.ValueKind == JsonValueKind.String)
                            Sources.Settings.Player.Username = nameVal.GetString();
                    }
                    catch { }
                }
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

            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Remote auth attempt failed ({ex.Message}). Falling back to local demo auth.");
        }

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

    //Get raw player data
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
            string? baseUrl = configuredAbsoluteUrl ?? httpClient.BaseAddress?.ToString();
            string statsUri = "player/me";
            if (!string.IsNullOrEmpty(baseUrl) && (baseUrl.Contains("api.php") || baseUrl.Contains("path=") || baseUrl.Contains('?')))
            {
                if (baseUrl.Contains("game_login"))
                    statsUri = baseUrl.Replace("game_login", "game_stats");
                else
                    statsUri = baseUrl;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, statsUri);
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
            throw;
        }
        catch
        {
            return null;
        }
    }

    //Get parsed player stats
    public async Task<PlayerStats?> GetPlayerStatsAsync()
    {
        var json = await GetPlayerDataAsync();
        if (json is null) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement payload = root;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                payload = data;

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

            string? username = ReadString(payload, "username") ?? ReadString(root, "username");
            int coins = ReadInt(payload, "coins");
            int level = ReadInt(payload, "level");

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
                joinTime = ReadString(root, "join_time");
                timePlayed = ReadString(root, "time_played");
                numStories = ReadInt(root, "num_of_story_finished");
                numEnemies = ReadInt(root, "num_of_enemies_killed");
                numDeaths = ReadInt(root, "num_of_deaths");
                score = ReadInt(root, "score");
            }

            if (username is not null) Sources.Settings.Player.Username = username;
            Sources.Settings.Player.Coins = coins;
            Sources.Settings.Player.Level = level;
            Sources.Settings.Player.NumOfStoryFinished = numStories;
            Sources.Settings.Player.NumOfEnemiesKilled = numEnemies;
            Sources.Settings.Player.NumOfDeaths = numDeaths;
            Sources.Settings.Player.Score = score;

            if (!string.IsNullOrEmpty(joinTime))
                Sources.Settings.Player.JoinTime = joinTime;
            if (!string.IsNullOrEmpty(timePlayed))
                Sources.Settings.Player.TimePlayed = timePlayed;

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

    //Push player stats
    public async Task<bool> UpdatePlayerStatsAsync()
    {
        if (!remoteMode || httpClient is null)
            return false;

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
