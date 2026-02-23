using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static OpenTK.Windowing.Common.Input.MouseCursor;

using Shaders;
using Sources;

namespace Engine;

internal partial class Engine
{
    /* Buttons ID Translator
     * 0. Campaing
     * 1. Customs
     * 2. Setings
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

    static int[] buttonsWidth = new int[]
    {
        114,
        98,
        91,
        102,
        103,
        122,
        57,
        63,
        149,
        128,
        121,
        32,
        32,
        32,
        32
    };

    static int originalButtonsHeight = 32;

    const float buttonsAtlasWidth = 447f;
    const float buttonsAtlasHeight = 480f;

    static int customsCurrentId = 0;

    private void CustomsCurrentIdChanger(int buttonId)
    {
        int minLimit = 0;
        int maxLimit = Level.CustomMaps.Count - 1;

        switch (buttonId)
        {
            case 11:
                if (customsCurrentId != minLimit) customsCurrentId -= 1;
                break;
            case 12:
                if (customsCurrentId != maxLimit) customsCurrentId += 1;
                break;
        }
    }

    //Validating hover
    static bool IsPointInQuad(float px, float py, float x1, float x2, float y1, float y2)
    {
        float minX = Math.Min(x1, x2);
        float maxX = Math.Max(x1, x2);
        float minY = Math.Min(y1, y2);
        float maxY = Math.Max(y1, y2);
        return px >= minX && px <= maxX && py >= minY && py <= maxY;
    }

    void HandleClickActions(int id)
    {
        _menuClickConsumed = true;

        //Main menu
        if (currentMenu == MenuId.Main)
        {
            switch (id)
            {
                //Campaign
                case 0:
                    currentMenu = MenuId.None;
                    break;
                //Customs
                case 1:
                    currentMenu = MenuId.Customs;
                    break;
                //Settings
                case 2:
                    currentMenu = MenuId.Settings;
                    break;
                //Statistics
                case 3:
                    currentMenu = MenuId.Statistics;
                    break;
                //Exit
                case 6:
                    Close();
                    break;
            }
        }

        //Campaign menu
        else if (currentMenu == MenuId.Campaign)
        {
            switch (id)
            {
                case 7:
                    currentMenu = MenuId.Main;
                    break;
            }
        }

        //Customs menu
        else if (currentMenu == MenuId.Customs)
        {
            switch (id)
            {
                //Back
                case 7:
                    currentMenu = MenuId.Main;
                    break;
                //Left arrow
                case 11:
                //Right arrow
                case 12:
                    CustomsCurrentIdChanger(id);
                    Console.WriteLine(customsCurrentId);
                    break;
            }
        }

        //Settings menu
        else if (currentMenu == MenuId.Settings)
        {
            switch (id)
            {
                case 7:
                    currentMenu = MenuId.Main;
                    break;
            }
        }

        //Statistics menu
        else if (currentMenu == MenuId.Statistics)
        {
            switch (id)
            {
                case 7:
                    currentMenu = MenuId.Main;
                    break;
            }
        }

        //Pause menu
        else if (currentMenu == MenuId.Pause)
        {
            switch (id)
            {
                case 8:
                    currentMenu = MenuId.None;
                    break;
                case 9:
                    currentMenu = MenuId.Main;
                    break;
            }
        }
    }

    //Click debounce for menu buttons
    bool _menuClickConsumed;

    //Previous mouse state tracking
    bool _prevMouseDown;

    void LoadButtonAttribs(int[] buttonIds)
    {
        float buttonHeight = minimumScreenSize / 15f;
        float buttonsGap = minimumScreenSize / 100f;
        float horizontalHalfScreen = screenHorizontalOffset + minimumScreenSize / 2f;
        float verticalHalfScreen = screenVerticalOffset + minimumScreenSize / 2f;

        //If hover
        bool anyHover = false;

        //Mouse position (Ortho coord)
        float mouseX = MouseState.X;
        float mouseY = ClientSize.Y - MouseState.Y;

        //Is mouse down
        bool mouseDown = MouseState.IsButtonDown(MouseButton.Left);

        //Action registers on release
        bool mouseReleased = _prevMouseDown && !mouseDown;

        //Release debounce
        if (!mouseDown)
            _menuClickConsumed = false;

        // Normal menu behavior: multiple buttons stacked vertically centered
        for (int i = 0; i < buttonIds.Length; i++)
        {
            int id = buttonIds[i];

            float buttonWidth = (buttonHeight / originalButtonsHeight) * buttonsWidth[id];

            float quadX1 = horizontalHalfScreen - buttonWidth / 2f;
            float quadX2 = horizontalHalfScreen + buttonWidth / 2f;
            float quadY1 = verticalHalfScreen - (i + 1f) * buttonHeight - i * buttonsGap;
            float quadY2 = verticalHalfScreen - (i + 2f) * buttonHeight - i * buttonsGap;

            bool isHover = IsPointInQuad(mouseX, mouseY, quadX1, quadX2, quadY1, quadY2);
            bool isClick = isHover && mouseDown;

            anyHover |= isHover;

            if (anyHover) Cursor = MouseCursor.PointingHand;
            else Cursor = MouseCursor.Default;

            //Action registers on release on the button
            if (isHover && mouseReleased && !_menuClickConsumed)
                HandleClickActions(id);

            //V: pick correct row in the sheet
            float pyTop = id * originalButtonsHeight;
            float pyBottom = (id + 1) * originalButtonsHeight;

            //X: choose state (Requirement: shift by button's own width)
            float px0 = 0f;
            if (isClick) px0 = 2f * buttonsWidth[id];
            else if (isHover) px0 = 1f * buttonsWidth[id];
            float px1 = px0 + buttonsWidth[id];

            float u0 = px0 / buttonsAtlasWidth;
            float u1 = px1 / buttonsAtlasWidth;

            float vTop = 1f - (pyTop / buttonsAtlasHeight);
            float vBottom = 1f - (pyBottom / buttonsAtlasHeight);

            // normal menu: unflipped
            float v0 = vBottom;
            float v1 = vTop;

            ShaderHandler.ButtonsVertexAttribList.AddRange(new float[]
            {
                quadX1,
                quadX2,
                quadY1,
                quadY2,
                id,
                u0,
                v0,
                u1,
                v1
            });
        }

        //Update previous mouse state
        _prevMouseDown = mouseDown;
    }
}
