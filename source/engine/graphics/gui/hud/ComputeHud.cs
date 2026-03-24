using OpenTK.Windowing.GraphicsLibraryFramework;

using Shaders;

namespace Engine;

internal partial class Engine
{
    //Running effect animation (8 frames on one row)
    static float _runningHudAnimTime;
    //Attack slash animation (8 frames on one row)
    static float _attackHudAnimTime;
    static bool _isAttackHudAnimPlaying;
    static bool _attackHudDamageDone;

    //Damage overlay
    const float playerDamageOverlayDuration = 0.2f;
    static float _playerDamageOverlayTimer;

    //Pickup overlay
    const float playerPickupOverlayDuration = 0.15f;
    static float _playerPickupOverlayTimer;

    void TriggerPlayerDamageOverlay()
    {
        _playerDamageOverlayTimer = playerDamageOverlayDuration;
    }

    void TriggerPlayerPickupOverlay()
    {
        _playerPickupOverlayTimer = playerPickupOverlayDuration;
    }

    void ResetHudCombatStates()
    {
        _attackHudAnimTime = 0f;
        _isAttackHudAnimPlaying = false;
        _attackHudDamageDone = false;

        _playerDamageOverlayTimer = 0f;
        ShaderHandler.HudDamageOverlayAlpha = 0f;

        _playerPickupOverlayTimer = 0f;
        ShaderHandler.HudPickupOverlayAlpha = 0f;
    }

    void LoadHudAttribs()
    {
        float x1 = screenHorizontalOffset;
        float x2 = screenHorizontalOffset + minimumScreenSize;
        float y1 = screenVerticalOffset + minimumScreenSize;
        float y2 = screenVerticalOffset;

        if (MouseState.IsButtonPressed(MouseButton.Left) && !_isAttackHudAnimPlaying)
        {
            _isAttackHudAnimPlaying = true;
            _attackHudAnimTime = 0f;
            _attackHudDamageDone = false;
        }

        bool isRunning = isPlayerSprinting;

        //Layer 0 - Running effect (only while running)
        if (isRunning)
        {
            _runningHudAnimTime += deltaTime;

            int frameCount = 8;
            float fps = 20f;
            int frame = (int)(_runningHudAnimTime * fps) % frameCount;

            float u0 = frame / 8f;
            float u1 = (frame + 1) / 8f;

            ShaderHandler.HudVertexAttribList.AddRange(new float[]
            {
                x1, x2, y1, y2,
                0f,
                u0, 0f, u1, 1f
            });
        }
        else
        {
            _runningHudAnimTime = 0f;
        }

        //Layer 1 - Sword / Attack slash
        if (_isAttackHudAnimPlaying)
        {
            _attackHudAnimTime += deltaTime;

            int frameCount = 8;
            float fps = 24f;
            int frame = (int)(_attackHudAnimTime * fps);

            //Middle of slash (between frame 4 and 5) -> apply hit once
            if (frame >= 4 && !_attackHudDamageDone)
            {
                TryDealPlayerSlashDamage();
                _attackHudDamageDone = true;
            }

            if (frame >= frameCount)
            {
                _isAttackHudAnimPlaying = false;
                _attackHudAnimTime = 0f;
                _attackHudDamageDone = false;
            }
            else
            {
                float u0 = frame / 8f;
                float u1 = (frame + 1) / 8f;

                ShaderHandler.HudVertexAttribList.AddRange(new float[]
                {
                    x1, x2, y1, y2,
                    4f,
                    u0, 0f, u1, 1f
                });
            }
        }

        if (!_isAttackHudAnimPlaying)
        {
            ShaderHandler.HudVertexAttribList.AddRange(new float[]
            {
                x1, x2, y1, y2,
                1f,
                0f, 0f, 1f, 1f
            });
        }

        //Layer 2 - Empty layer (future shield)
        ShaderHandler.HudVertexAttribList.AddRange(new float[]
        {
            x1, x2, y1, y2,
            -1f,
            0f, 0f, 1f, 1f
        });

        if (_playerDamageOverlayTimer > 0f)
        {
            _playerDamageOverlayTimer -= deltaTime;

            if (_playerDamageOverlayTimer < 0f)
                _playerDamageOverlayTimer = 0f;
        }

        float damageOverlayAlpha = 0f;
        if (_playerDamageOverlayTimer > 0f)
            damageOverlayAlpha = (_playerDamageOverlayTimer / playerDamageOverlayDuration) * 0.33f;

        ShaderHandler.HudDamageOverlayAlpha = damageOverlayAlpha;

        //Layer 3 - Damage overlay
        ShaderHandler.HudVertexAttribList.AddRange(new float[]
        {
            x1, x2, y1, y2,
            damageOverlayAlpha > 0f ? -2f : -1f,
            0f, 0f, 1f, 1f
        });

        if (_playerPickupOverlayTimer > 0f)
        {
            _playerPickupOverlayTimer -= deltaTime;

            if (_playerPickupOverlayTimer < 0f)
                _playerPickupOverlayTimer = 0f;
        }

        float pickupOverlayAlpha = 0f;
        if (_playerPickupOverlayTimer > 0f)
            pickupOverlayAlpha = (_playerPickupOverlayTimer / playerPickupOverlayDuration) * 0.28f;

        ShaderHandler.HudPickupOverlayAlpha = pickupOverlayAlpha;

        //Layer 4 - Pickup overlay
        ShaderHandler.HudVertexAttribList.AddRange(new float[]
        {
            x1, x2, y1, y2,
            pickupOverlayAlpha > 0f ? -3f : -1f,
            0f, 0f, 1f, 1f
        });

        //Layer 5 - Container
        ShaderHandler.HudVertexAttribList.AddRange(new float[]
        {
            x1, x2, y1, y2,
            3f,
            0f, 0f, 1f, 1f
        });
    }
}
