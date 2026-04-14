using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;

using Shaders;
using Sources;

namespace Engine;

internal partial class Engine
{
    /* Menu background ID translator
     * 0. Main Menu
     * 1. Campaign
     * 2. Customs
     * 3. Settings Menu
     * 4. Statistics Menu
     * 5. Pause Menu
     * 6. Level Completed Menu
     * 
     */

    /* Buttons ID Translator
     * 0. Campaign
     * 1. Customs
     * 2. Settings
     * 3. Statistics
     * 4. Continue
     * 5. New Game
     * 6. Exit
     * 7. Back
     * 8. Back to Game
     * 9. Main Menu
     * 10. Next Level
     * 11. 'Left Arrow'
     * 12. 'Right Arrow'
     * 13. 'Up Arrow'
     * 14. 'Down Arrow'
     */

    Dictionary<MenuId, Action> menuHandlers;

    //Title, Map name
    Vector3 firstTextClr = (255f / 255f, 255f / 255f, 255f / 255f);
    //Author, Descripton/ Created at
    Vector3 secondTextClr = (96f / 255f, 89f / 255f, 82f / 255f);

    // Normalized button centers (X,Y) in menu-space: 0..1 inside the square viewport
    readonly Vector2[] campaignButtonCenters =
    {
        new(0.25f, 0.18f), // Left arrow
        new(0.75f, 0.18f), // Right arrow
        new(0.50f, 0.23f), // Play
        new(0.50f, 0.15f), // Back
    };

    readonly Vector2[] customsButtonCenters =
    {
        new(0.25f, 0.18f), // Left arrow
        new(0.75f, 0.18f), // Right arrow
        new(0.50f, 0.22f), // Play
        new(0.50f, 0.15f), // Back
    };

    readonly Vector2[] settingsButtonCenters =
    {
        new(0.50f, 0.15f), // Back
    };

    readonly Vector2[] statisticsButtonCenters =
    {
        new(0.50f, 0.12f), // Back
    };

    void InitMenuHandlers()
    {
        menuHandlers = new()
        {
            [MenuId.Main] = HandleMainMenu,
            [MenuId.Campaign] = HandleCampaignMenu,
            [MenuId.Customs] = HandleCustomsMenu,
            [MenuId.Statistics] = HandleStatisticsMenu,
            [MenuId.Settings] = HandleSettingsMenu,
            [MenuId.Pause] = HandlePauseMenu,
            //[MenuId.LvlCompleted] = HandleLvlCompletedMenu,
        };
    }

    internal static int[]? buttonIds;

    void HandleAllMenus()
    {
        if (currentMenu == MenuId.None) return;

        menuHandlers[currentMenu]();
    }
    void HandleMainMenu()
    {
        buttonIds = new int[] { 0, 1, 2, 3, 6};

        LoadMenuAttribs(0);
        LoadButtonAttribs(buttonIds);
    }

    void HandleCampaignMenu()
    {
        buttonIds = new int[] { 12, 13, 11, 7 };

        //Map name
        LoadTextAttribs(
            $"Level {storyCurrentId + 1}",
            screenHorizontalOffset + (minimumScreenSize / 8.3f),
            screenVerticalOffset + (minimumScreenSize / 1.49f),
            2.5f,
            new Vector3(firstTextClr)
        );

        LoadMenuAttribs(1);
        LoadButtonAttribs(buttonIds, campaignButtonCenters);
    }

    void HandleCustomsMenu()
    {
        buttonIds = new int[] { 12, 13, 11, 7 };

        Vector3 mapNameClr = (
            232f / 255f,
            225f / 255f,
            218f / 255f);

        //============================???

        //Counter
        LoadTextAttribs(
            $"{customsCurrentId + 1}/{Level.CustomMetaDatas.Count}",
            screenHorizontalOffset + (minimumScreenSize / 1.75f),
            screenVerticalOffset + (minimumScreenSize / 1.3f),
            2f,
            new Vector3(mapNameClr)
        );

        //Author
        LoadTextAttribs(
            $"by:{Level.CustomMetaDatas[customsCurrentId].Author}",
            screenHorizontalOffset + (minimumScreenSize / 2.3f),
            screenVerticalOffset + (minimumScreenSize / 1.45f),
            1.5f,
            new Vector3(mapNameClr)
        );

        //Created at
        LoadTextAttribs(
            $"at:{Level.CustomMetaDatas[customsCurrentId].CreatedAt}",
            screenHorizontalOffset + (minimumScreenSize / 2.3f),
            screenVerticalOffset + (minimumScreenSize / 1.85f),
            1.5f,
            new Vector3(mapNameClr)
        );

        //Map name
        LoadTextAttribs(
            $"{Level.CustomMetaDatas[customsCurrentId].MapName}",
            screenHorizontalOffset + (minimumScreenSize / 8.5f),
            screenVerticalOffset + (minimumScreenSize / 2.5f),
            2f,
            new Vector3(mapNameClr)
        );

        LoadMenuAttribs(2);
        LoadButtonAttribs(buttonIds, customsButtonCenters);
    }

    void HandleSettingsMenu()
    {
        buttonIds = new int[] { 7 };

        LoadMenuAttribs(3);
        LoadButtonAttribs(buttonIds, settingsButtonCenters);
    }

    void HandleStatisticsMenu()
    {
        buttonIds = new int[] { 7 };

        LoadMenuAttribs(4);
        LoadButtonAttribs(buttonIds, statisticsButtonCenters);

        Vector3 aliasFontColor = (
            232f / 255f,
            225f / 255f,
            218f / 255f);

        Vector3 statFontColor = (
            96f / 255f,
            89f / 255f,
            82f / 255f);

        //Statistics (dynamic from Settings.Player)
        // Alias (username)
        LoadTextAttribs(
            $"{Sources.Settings.Player.Username ?? "Player"}",
            screenHorizontalOffset + (minimumScreenSize / 2.72f),
            screenVerticalOffset + (minimumScreenSize / 1.49f),
            2f,
            new Vector3(aliasFontColor)
        );

        // Joined
        LoadTextAttribs(
            $"Joined:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 1.83f),
            1.5f,
            new Vector3(statFontColor)
        );
        LoadTextAttribs(
            $"{Sources.Settings.Player.JoinTime ?? "-"}",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 1.87f),
            1f,
            new Vector3(statFontColor)
        );

        // Time played
        LoadTextAttribs(
            $"Time played:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.1f),
            1.5f,
            new Vector3(statFontColor)
        );
        LoadTextAttribs(
            $"{Sources.Settings.Player.TimePlayed ?? "0h 0m"}",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 2.16f),
            1f,
            new Vector3(statFontColor)
        );

        // Story done
        LoadTextAttribs(
            $"Story done:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.46f),
            1.5f,
            new Vector3(statFontColor)
        );
        LoadTextAttribs(
            $"{(Sources.Settings.Player.NumOfStoryFinished > 0 ? Sources.Settings.Player.NumOfStoryFinished.ToString() : "0")}",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 2.55f),
            1f,
            new Vector3(statFontColor)
        );

        // Enemy kills
        LoadTextAttribs(
            $"Enemy kill:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.97f),
            1.5f,
            new Vector3(statFontColor)
        );
        LoadTextAttribs(
            $"{(Sources.Settings.Player.NumOfEnemiesKilled > 0 ? Sources.Settings.Player.NumOfEnemiesKilled.ToString() : "0")}",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 3.1f),
            1f,
            new Vector3(statFontColor)
        );

        // Deaths
        LoadTextAttribs(
            $"Deaths:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 3.75f),
            1.5f,
            new Vector3(statFontColor)
        );
        LoadTextAttribs(
            $"{(Sources.Settings.Player.NumOfDeaths > 0 ? Sources.Settings.Player.NumOfDeaths.ToString() : "0")}",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 3.9f),
            1f,
            new Vector3(statFontColor)
        );

        // Score
        LoadTextAttribs(
            $"Score:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 5.13f),
            1.5f,
            new Vector3(statFontColor)
        );
        LoadTextAttribs(
            $"{Sources.Settings.Player.Coins}",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 5.45f),
            1f,
            new Vector3(statFontColor)
        );

    }

    void HandlePauseMenu()
    {
        buttonIds = new int[] { 8, 9 };

        LoadMenuAttribs(5);
        LoadButtonAttribs(buttonIds);
    }

    void LoadMenuAttribs(int backgroundIndex)
    {
        // Provide aPos as x1, x2, yTop, yBottom (y1,y2) to match vertex shader
        ShaderHandler.MenusVertexAttribList.AddRange(new float[]
        {
            screenHorizontalOffset,
            screenHorizontalOffset + minimumScreenSize,
            screenVerticalOffset + minimumScreenSize, // top
            screenVerticalOffset, // bottom
            backgroundIndex
        });
    }
}
