using Engine;
using Sources;

namespace Root;

public class Program
{
    static void Main()
    {
        //Loading settings (More exception handling is needed)
        try
        {
            Settings.Load("settings.json");
            Console.WriteLine("Settings loaded!");

        }
        catch (FileNotFoundException NoFileEx)
        {
            Console.WriteLine(NoFileEx);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Settings:\n{ex}");
        }

        //Loading needed level infos, NOT LEVELS (More exception handling is needed)
        try
        {
            //Story directory
            string storyRootDir = "assets/maps/story";
            //Customs directory
            string customsRootDir = "assets/maps/customs";
            Level.FirstLoad(storyRootDir, customsRootDir);
        }
        catch (FileNotFoundException NoFileEx)
        {
            Console.WriteLine(NoFileEx);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Level:\n{ex}");
        }

        Level.Load(0, 1);

        //Console.WriteLine("Breakpoint");

        //CLI login: block until user authenticates
        var apiBase = Sources.Settings.Api.BaseUrl;
        using var api = string.IsNullOrEmpty(apiBase) ? new Sources.JsonCrudApi() : new Sources.JsonCrudApi(apiBase!);

        bool authenticated = false;
        while (!authenticated)
        {
            Console.Write("Username: ");
            string? user = Console.ReadLine();

            Console.Write("Password: ");
            string? pass = ReadPassword();

            if (string.IsNullOrEmpty(user) || pass is null)
            {
                Console.WriteLine("Invalid input.\n");
                continue;
            }

            authenticated = api.AuthenticateAsync(user, pass).GetAwaiter().GetResult();
            if (!authenticated)
            {
                Console.WriteLine("Login failed. Try again.\n");
            }
            else
            {
                Console.WriteLine("Login successful. Fetching player data...\n");
                try
                {
                    var stats = api.GetPlayerStatsAsync().GetAwaiter().GetResult();
                    if (stats is not null)
                    {
                        // JsonCrudApi already merged runtime stats into Settings.Player.
                        Console.WriteLine($"Player stats loaded: Coins={Sources.Settings.Player.Coins}, Level={Sources.Settings.Player.Level}\n");
                    }
                    else
                    {
                        Console.WriteLine("No player stats received; using defaults.\n");
                    }
                    Console.WriteLine("Starting game...\n");
                }
                catch (UnauthorizedAccessException)
                {
                    // Token invalid for stats - clear saved token and force re-login
                    Console.WriteLine("Authorization failed when fetching stats. Please login again.\n");
                    Sources.Settings.Api.Token = null;
                    try { Sources.Settings.Save("settings.json"); } catch { }
                    authenticated = false; // restart loop
                }
            }
        }

        Engine.Engine engine = new Engine.Engine(800, 800, "Troxan");
        engine.Run();
    }

    static string? ReadPassword()
    {
        var pass = new System.Text.StringBuilder();
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                if (pass.Length > 0)
                {
                    pass.Length--;
                    Console.Write('\b');
                    Console.Write(' ');
                    Console.Write('\b');
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                pass.Append(key.KeyChar);
                Console.Write('*');
            }
        }
        Console.WriteLine();
        return pass.ToString();
    }
}