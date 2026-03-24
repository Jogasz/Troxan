using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Threading.Tasks;

using Sources;

namespace Engine;

internal partial class Engine
{
    const float sprintSpeedMultiplier = 1.7f;
    const float sprintStaminaDrainPerSecond = 30f;
    const float sprintStaminaRegenPerSecond = 18f;
    const float sprintStaminaRegenDelay = 0.9f;

    void UpdateSprintStamina(KeyboardState keyboard)
    {
        bool isShiftHeld = keyboard.IsKeyDown(Keys.LeftShift);

        if (!isShiftHeld)
            sprintNeedsShiftRelease = false;

        if (isPlayerSprinting)
        {
            playerCurrentStaminaRuntime -= sprintStaminaDrainPerSecond * deltaTime;
            sprintRegenDelayTimer = sprintStaminaRegenDelay;

            if (playerCurrentStaminaRuntime <= 0f)
            {
                playerCurrentStaminaRuntime = 0f;
                sprintNeedsShiftRelease = true;
                isPlayerSprinting = false;
            }
        }
        else
        {
            if (sprintRegenDelayTimer > 0f)
            {
                sprintRegenDelayTimer -= deltaTime;

                if (sprintRegenDelayTimer < 0f)
                    sprintRegenDelayTimer = 0f;
            }
            else
            {
                playerCurrentStaminaRuntime += sprintStaminaRegenPerSecond * deltaTime;

                if (playerCurrentStaminaRuntime > tempPlayerMaxStamina)
                    playerCurrentStaminaRuntime = tempPlayerMaxStamina;
            }
        }

        tempPlayerCurrentStamina = (int)MathF.Ceiling(playerCurrentStaminaRuntime);
    }

    bool IsPlayerTouchingEnemy()
    {
        float minDistance = playerCollisionRadius + enemyCollisionRadius;
        float minDistanceSq = minDistance * minDistance;

        for (int i = 0; i < Level.Sprites.Count; i++)
        {
            var sprite = Level.Sprites[i];
            if (!sprite.State || sprite.Type != 2) continue;

            Vector2 enemyPosPx = (
                (sprite.Position.X + 0.5f) * tileSize,
                (sprite.Position.Y + 0.5f) * tileSize);

            float dx = enemyPosPx.X - playerPosition.X;
            float dy = enemyPosPx.Y - playerPosition.Y;

            if ((dx * dx + dy * dy) <= minDistanceSq)
                return true;
        }

        return false;
    }

    void CheckPlayerWallBlock(
        Vector2 playerPos,
        Vector2 rotatedVector,
        float playerDeltaMovementSpeed,
        out bool IsXBlocked,
        out bool IsYBlocked)
    {
        //Temporary variables to reduce calculations
        //XBlocked
        int tempXBlocked_Y_P = (int)((playerPos.Y + playerCollisionRadius) / tileSize);
        int tempXBlocked_Y_M = (int)((playerPos.Y - playerCollisionRadius) / tileSize);
        int tempXBlocked_X_P = (int)(((int)(playerPos.X + playerCollisionRadius + rotatedVector.X * playerDeltaMovementSpeed)) / tileSize);
        int tempXBlocked_X_M = (int)(((int)(playerPos.X - playerCollisionRadius + rotatedVector.X * playerDeltaMovementSpeed)) / tileSize);
        //YBlocked
        int tempYBlocked_Y_P = (int)(((int)(playerPos.Y + playerCollisionRadius + rotatedVector.Y * playerDeltaMovementSpeed)) / tileSize);
        int tempYBlocked_Y_M = (int)(((int)(playerPos.Y - playerCollisionRadius + rotatedVector.Y * playerDeltaMovementSpeed)) / tileSize);
        int tempYBlocked_X_P = (int)((playerPos.X + playerCollisionRadius) / tileSize);
        int tempYBlocked_X_M = (int)((playerPos.X - playerCollisionRadius) / tileSize);

        //Collision checking
        IsXBlocked =
                playerPos.X - playerCollisionRadius + rotatedVector.X * playerDeltaMovementSpeed <= 0f ||
                playerPos.X + playerCollisionRadius + rotatedVector.X * playerDeltaMovementSpeed >= (mapWalls.GetLength(1) * tileSize) ||
                mapWalls[tempXBlocked_Y_P, tempXBlocked_X_P] > 0 ||
                mapWalls[tempXBlocked_Y_P, tempXBlocked_X_M] > 0 ||
                mapWalls[tempXBlocked_Y_M, tempXBlocked_X_P] > 0 ||
                mapWalls[tempXBlocked_Y_M, tempXBlocked_X_M] > 0;

        IsYBlocked =
                playerPos.Y - playerCollisionRadius + rotatedVector.Y * playerDeltaMovementSpeed <= 0f ||
                playerPos.Y + playerCollisionRadius + rotatedVector.Y * playerDeltaMovementSpeed >= (mapWalls.GetLength(0) * tileSize) ||
                mapWalls[tempYBlocked_Y_P, tempYBlocked_X_P] > 0 ||
                mapWalls[tempYBlocked_Y_M, tempYBlocked_X_P] > 0 ||
                mapWalls[tempYBlocked_Y_P, tempYBlocked_X_M] > 0 ||
                mapWalls[tempYBlocked_Y_M, tempYBlocked_X_M] > 0;
    }

    void Controls(KeyboardState keyboard, MouseState mouse)
    {
        //Checking if mouse was moved
        bool IsMouseMoving = mouse.X != mouse.PreviousX || mouse.Y != mouse.PreviousY;

        //If there's no keyboard or mouse input, stop
        if (!keyboard.IsAnyKeyDown && !IsMouseMoving)
        {
            return;
        }
        else
        {
            //Movement + collison
            // - W, A, S, D + Left Shift
            if (keyboard.IsKeyDown(Keys.W) ||
                keyboard.IsKeyDown(Keys.LeftShift) ||
                keyboard.IsKeyDown(Keys.A) ||
                keyboard.IsKeyDown(Keys.S) ||
                keyboard.IsKeyDown(Keys.D))
            {
                HandleMovement(keyboard);
            }

            //Jump
            // - Space
            if (keyboard.IsKeyPressed(Keys.Space))
            {
                HandleJump();
            }

            //Mouse
            if (IsMouseMoving && CursorState == CursorState.Grabbed)
            {
                HandleMouse(mouse);
            }

            //Fullscreen
            // - F11
            if (keyboard.IsKeyPressed(Keys.F11))
            {
                HandleFullscreen();
            }

            //Cursor grab
            // - F1
            if (keyboard.IsKeyPressed(Keys.F1))
            {
                HandleCursorGrab();
            }
        }
    }

    void HandleMovement(KeyboardState keyboard)
    {
        float playerDeltaMovementSpeed = playerMovementSpeed * deltaTime;
        var _PlayerPosition = playerPosition;
        Vector2 movementVector;
        Vector2 rotatedVector;
        bool IsXBlocked = false;
        bool IsYBlocked = false;

        //Checking the movement input
        movementVector.X = (keyboard.IsKeyDown(Keys.A) ? 1f : 0f) + (keyboard.IsKeyDown(Keys.D) ? -1f : 0f);
        movementVector.Y = (keyboard.IsKeyDown(Keys.W) ? 1f : 0f) + (keyboard.IsKeyDown(Keys.S) ? -1f : 0f);

        //Checking if the movement vector's magnitude is higher than 1
        if (MathX.Hypotenuse(movementVector.X, movementVector.Y) > 1f)
        {
            movementVector.Normalize();
        }

        //Rotating the movement vector to the angle of the player
        rotatedVector.X = (float)(movementVector.X * Math.Cos(playerAngle - MathX.Quadrant1)) - (float)(movementVector.Y * Math.Sin(playerAngle - MathX.Quadrant1));
        rotatedVector.Y = (float)(movementVector.X * Math.Sin(playerAngle - MathX.Quadrant1)) + (float)(movementVector.Y * Math.Cos(playerAngle - MathX.Quadrant1));

        //Sprint
        bool wantsSprint = keyboard.IsKeyDown(Keys.W) && keyboard.IsKeyDown(Keys.LeftShift);
        bool canSprintByStamina = !sprintNeedsShiftRelease && playerCurrentStaminaRuntime > 0f;
        bool canSprintByEnemy = !IsPlayerTouchingEnemy();

        bool sprintWallBlockedX = false;
        bool sprintWallBlockedY = false;

        if (wantsSprint && canSprintByStamina && canSprintByEnemy)
        {
            float sprintDeltaMovementSpeed = deltaTime * playerMovementSpeed * sprintSpeedMultiplier;

            CheckPlayerWallBlock(
                _PlayerPosition,
                rotatedVector,
                sprintDeltaMovementSpeed,
                out sprintWallBlockedX,
                out sprintWallBlockedY);

            if (!sprintWallBlockedX && !sprintWallBlockedY)
            {
                playerDeltaMovementSpeed = sprintDeltaMovementSpeed;
                FOV = (int)(Settings.Graphics.FOV / 1.15f);
                isPlayerSprinting = true;
            }
            else
            {
                playerDeltaMovementSpeed = deltaTime * playerMovementSpeed;
                FOV = Settings.Graphics.FOV;
            }
        }
        else
        {
            playerDeltaMovementSpeed = deltaTime * playerMovementSpeed;
            FOV = Settings.Graphics.FOV;
        }

        CheckPlayerWallBlock(
            _PlayerPosition,
            rotatedVector,
            playerDeltaMovementSpeed,
            out IsXBlocked,
            out IsYBlocked);

        //Allowing player to move if the collision checker gave permission
        if (!IsXBlocked)
        {
            _PlayerPosition.X += rotatedVector.X * playerDeltaMovementSpeed;
        }
        if (!IsYBlocked)
        {
            _PlayerPosition.Y += rotatedVector.Y * playerDeltaMovementSpeed;
        }

        playerPosition = _PlayerPosition;
    }

    void HandleJump()
    {
        Console.WriteLine(" - Jump!");
    }

    void HandleFullscreen()
    {
        WindowState = WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
    }

    void HandleMouse(MouseState mouse)
    {
        float offsetX = (mouse.X - mouse.PreviousX) * mouseSensitivity;
        float offsetY = (mouse.Y - mouse.PreviousY) * (mouseSensitivity * 1000);

        //Rotating X
        playerAngle = Utils.NormalizeAngle(playerAngle + offsetX);

        //Pitch/yaw (Y)
        pitch = Math.Clamp(pitch - offsetY, -1000, 1000);
    }

    void HandleCursorGrab()
    {
        CursorState = CursorState == CursorState.Normal ? CursorState.Grabbed : CursorState.Normal;
    }
}
