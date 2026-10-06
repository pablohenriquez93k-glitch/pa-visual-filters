#version 140

// post_hdr_compose.fs

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D PostSourceTexture;
uniform vec4 PostSourceTexture_size;
uniform sampler2D Luminance;
uniform sampler2D Bloom;

uniform sampler2D HdrAvgLuminance;
uniform sampler2D DitherTexture;

in vec2 v_TexCoord;

out vec4 out_FragColor;

#define DITHER 1
#define DITHER_BAYER 0

// uncharted 2 tone mapper
float A = 0.15;
float B = 0.50;
float C = 0.10;
float D = 0.20;
float E = 0.02;
float F = 0.30;
float W = 11.2;

vec3 toneMapper( vec3 x )
{
    return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

#if (DITHER_BAYER == 1)
int pattern[64] = int[](
     0, 32,  8, 40,  2, 34, 10, 42,   /* 8x8 Bayer ordered dithering  */
    48, 16, 56, 24, 50, 18, 58, 26,   /* pattern.  Each input pixel   */
    12, 44,  4, 36, 14, 46,  6, 38,   /* is scaled to the 0..63 range */
    60, 28, 52, 20, 62, 30, 54, 22,   /* before looking in this table */
     3, 35, 11, 43,  1, 33,  9, 41,   /* to determine the action.     */
    51, 19, 59, 27, 49, 17, 57, 25,
    15, 47,  7, 39, 13, 45,  5, 37,
    63, 31, 55, 23, 61, 29, 53, 21
    );
#endif

vec3 dither()
{
#if (DITHER_BAYER == 1)
    ivec2 xy = ivec2(mod(gl_FragCoord.xy, 8.0));
    return vec3(float(pattern[xy.x + xy.y * 8] - 31) / (63.0 * 255.0));
#else
    ivec2 ditherSize = textureSize(DitherTexture, 0);
    ivec2 ditherUV = ivec2(mod(gl_FragCoord.xy, ditherSize));
    vec4 noise = texelFetch(DitherTexture, ditherUV, 0);

    vec2 channelOffsets = gl_FragCoord.xy / ditherSize;
    int offset = int(channelOffsets.x + channelOffsets.y * 2.0);
    vec3 ditherNoise = vec3(
        noise[int(mod(offset,4))],
        noise[int(mod(offset+1,4))],
        noise[int(mod(offset+2,4))]
        );
    if (mod(offset,2) == 0)
        ditherNoise = 1.0 - ditherNoise;

    float divisor = 127.0;
    return (ditherNoise - 0.5) / divisor;
#endif
}

vec3 gamma = vec3(2.2, 2.2, 2.2);
vec3 inv_gamma = 1.0 / gamma;

void main()
{
    // Read the original HDR value
    vec4 hdrValue = texelFetch(PostSourceTexture, ivec2(gl_FragCoord.xy), 0);
    float a = hdrValue.a;

    // Read the blur value.
    vec4 bloom = texture(Bloom, v_TexCoord);

    hdrValue.rgb += 0.25 * bloom.rgb;

    float luminance = max(0.25, texelFetch(HdrAvgLuminance, ivec2(0, 0), 0).x);

    // if (a > 0.0)
    // {
        vec3 toneValue = toneMapper( hdrValue.rgb / luminance );
        vec3 whiteScale = 1.0 / toneMapper(vec3(W, W, W));

        out_FragColor.rgb = toneValue.rgb * whiteScale;
    // }
    // else
        // out_FragColor.rgb = hdrValue.rgb;

    // Add some sub color depth noise to the final image to force dithering to occur.
    // Should be added in sRGB space, and not linear space, but sRGB is kind of expensive.
    // We convert the out color to gamma 2.2 and back to linear; it's silly, but it works.
#if (DITHER == 1)
    out_FragColor.rgb = pow(out_FragColor.rgb, inv_gamma);
    out_FragColor.rgb = max(out_FragColor.rgb + dither(), 0.0);
    out_FragColor.rgb = pow(out_FragColor.rgb, gamma);
#endif

    // Would be cheaper to just add dither with out gamma correction but at low color ranges
    // it starts to add visible noise but at higher colors it stops actually fixing the banding.
}
