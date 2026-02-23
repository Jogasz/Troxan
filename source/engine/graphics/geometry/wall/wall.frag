//Fragment shader is calculating the color output for the pixels

//Declaration of shader version and core profile functionality
//==============================================================
#version 330 core
//==============================================================

//Incoming and outgoing variables, uniforms
//==============================================================
    //Wall's height (in)
in float vWallHeight;
    //Ray's length (in)
in float vRayLength;
    //Ray's pos in tile (in)
in float vRayTilePos;
    //Texture's index (in)
in float vTexIndex;
    //Wall's side (in)
in float vWallSide;

    //Final outgoing RGBA of the quad (out)
out vec4 FragColor;

//OnRenderFrame uniforms
//======================
    //Textures array (uIn)
uniform sampler2D uTextures[4];
    //Player's pitch (uIn)
uniform float uPitch;

//OnLoad / OnFramebufferResize uniforms
//=====================================
    //Minimum window's size (uIn)
uniform float uMinimumScreenSize;
    //Offsets for ,inimum window (uIn)
uniform vec2 uScreenOffset;
    //TileSize (uIn)
uniform float uTileSize;
    //Distance shade value (uIn)
uniform float uDistanceShade;

//==============================================================

//Entry point
//==============================================================
void main()
{
        //Strip quad's height
    float stripQuadHeight = max(0.0, vWallHeight);
        //Wall's true top value
    float wallRelTop = uScreenOffset.y + (uMinimumScreenSize / 2) + (vWallHeight / 2) - uPitch;
        //Current pixel's Y from strip quad's top (Clamp to avoid weird values on borders)
    float pixelYInStrip = clamp(wallRelTop - gl_FragCoord.y, 0.0, stripQuadHeight);
        //Strength of shading by distance
    float distanceShade = uDistanceShade / 255;

        //Shade strenght
    float shade = vRayLength * distanceShade;
    //Horizontal texture pixel position
        //Right side (no flip)
    float u = clamp(vRayTilePos / uTileSize, 0.0, 1.0);
        //Wrong side (flip)
    if (vWallSide == 1 ||
        vWallSide == 3) u = clamp(1 - (vRayTilePos / 50), 0.0, 1.0);
        //Vertical texture pixel position
    float v = clamp(1 - (pixelYInStrip / stripQuadHeight), 0.0, 1.0);
        //Taking out the color from the selected texture
    vec4 tex = texture(uTextures[int(vTexIndex - 1)], vec2(u, v));
    if (distance(tex.rgb, vec3(255,0,220) /255) < 0.1) discard;
        //Returning the correct color
    FragColor = tex - shade;
}
//==============================================================