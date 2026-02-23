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
     */

    /* Buttons ID Translator
     * 0. Campaing
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

    internal static int[] buttonIds;

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
        buttonIds = new int[] { 7 };

        LoadMenuAttribs(1);
        LoadButtonAttribs(buttonIds);
    }

    void HandleCustomsMenu()
    {
        buttonIds = new int[] { 7, 5, 11, 12 };

        Vector3 mapNameClr = (
            232f / 255f,
            225f / 255f,
            218f / 255f);

        Vector3 descrClr = (
            96f / 255f,
            89f / 255f,
            82f / 255f);

        //Counter
        LoadTextAttribs(
            $"{customsCurrentId + 1}/{Level.CustomMaps.Count}",
            screenHorizontalOffset + (minimumScreenSize / 1.75f),
            screenVerticalOffset + (minimumScreenSize / 1.3f),
            2f,
            new Vector3(mapNameClr)
        );

        //Author
        LoadTextAttribs(
            $"by:{Level.CustomMaps[customsCurrentId].Author}",
            screenHorizontalOffset + (minimumScreenSize / 2.3f),
            screenVerticalOffset + (minimumScreenSize / 1.45f),
            1.5f,
            new Vector3(mapNameClr)
        );

        //Created at
        LoadTextAttribs(
            $"at:{Level.CustomMaps[customsCurrentId].CreatedAt}",
            screenHorizontalOffset + (minimumScreenSize / 2.3f),
            screenVerticalOffset + (minimumScreenSize / 1.85f),
            1.5f,
            new Vector3(mapNameClr)
        );

        //Map name
        LoadTextAttribs(
            $"{Level.CustomMaps[customsCurrentId].MapName}",
            screenHorizontalOffset + (minimumScreenSize / 8.5f),
            screenVerticalOffset + (minimumScreenSize / 2.5f),
            2f,
            new Vector3(mapNameClr)
        );

        LoadMenuAttribs(2);
        LoadButtonAttribs(buttonIds);
    }

    void HandleSettingsMenu()
    {
        buttonIds = new int[] { 7 };

        LoadMenuAttribs(3);
        LoadButtonAttribs(buttonIds);
    }

    void HandleStatisticsMenu()
    {
        buttonIds = new int[] { 7 };

        LoadMenuAttribs(4);
        LoadButtonAttribs(buttonIds);

        Vector3 aliasFontColor = (
            232f / 255f,
            225f / 255f,
            218f / 255f);

        Vector3 statFontColor = (
            96f / 255f,
            89f / 255f,
            82f / 255f);

        //Statistics
        //Alias
        LoadTextAttribs(
            $"Recskas",
            screenHorizontalOffset + (minimumScreenSize / 2.72f),
            screenVerticalOffset + (minimumScreenSize / 1.49f),
            2f,
            new Vector3(aliasFontColor)
        );

        //Joined
        LoadTextAttribs(
            $"Joined:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 1.83f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Joined value
        LoadTextAttribs(
            $"26-03-17",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 1.87f),
            1f,
            new Vector3(statFontColor)
        );

        //Time played
        LoadTextAttribs(
            $"Time played:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.1f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Time played value
        LoadTextAttribs(
            $"2h 34m",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 2.16f),
            1f,
            new Vector3(statFontColor)
        );

        //Story done
        LoadTextAttribs(
            $"Story done:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.46f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Story done value
        LoadTextAttribs(
            $"True",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 2.55f),
            1f,
            new Vector3(statFontColor)
        );

        //Enemy kill
        LoadTextAttribs(
            $"Enemy kill:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.97f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Enemy kill value
        LoadTextAttribs(
            $"120",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 3.1f),
            1f,
            new Vector3(statFontColor)
        );

        //Deaths
        LoadTextAttribs(
            $"Deaths:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 3.75f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Deaths value
        LoadTextAttribs(
            $"2",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 3.9f),
            1f,
            new Vector3(statFontColor)
        );

        //Score
        LoadTextAttribs(
            $"Score:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 5.13f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Score value
        LoadTextAttribs(
            $"1230",
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
