#version 140

// post_hdr_compose.fs
// Visual Filters (com.pa.pabloandclaude.visualfilters): game shader + filters after the tone map.
// VF_PARAMS is replaced by #defines built from the player's settings.
//VF_PARAMS

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

// Visual Filters ---------------------------------------------------------
#ifndef VF_DALTON
#define VF_DALTON 0
#endif
#ifndef VF_DALTON_K
#define VF_DALTON_K 1.0
#endif
#ifndef VF_SAT
#define VF_SAT 1.0
#endif
#ifndef VF_CON
#define VF_CON 1.0
#endif
#ifndef VF_BRI
#define VF_BRI 0.0
#endif
#ifndef VF_VIG
#define VF_VIG 0.0
#endif
#ifndef VF_SHARP
#define VF_SHARP 0.0
#endif
#ifndef VF_VIG_ON
#define VF_VIG_ON 0
#endif
#ifndef VF_SHARP_ON
#define VF_SHARP_ON 0
#endif

// Daltonize (Fidaner, Lin, Ozguven 2005): simulate the deficiency in LMS, move the lost
// information to the channels the player can still see.
vec3 vfDaltonize(vec3 c)
{
    float L = 17.8824 * c.r + 43.5161 * c.g + 4.11935 * c.b;
    float M = 3.45565 * c.r + 27.1554 * c.g + 3.86714 * c.b;
    float S = 0.0299566 * c.r + 0.184309 * c.g + 1.46709 * c.b;
#if (VF_DALTON == 1)
    L = 2.02344 * M - 2.52581 * S;
#elif (VF_DALTON == 2)
    M = 0.494207 * L + 1.24827 * S;
#else
    S = -0.395913 * L + 0.801109 * M;
#endif
    vec3 sim = vec3(
        0.0809444479 * L - 0.130504409 * M + 0.116721066 * S,
        -0.0102485335 * L + 0.0540193266 * M - 0.113614708 * S,
        -0.000365296938 * L - 0.00412161469 * M + 0.693511405 * S);
    vec3 err = c - sim;
    vec3 shift = vec3(0.0, 0.7 * err.r + err.g, 0.7 * err.r + err.b);
    return c + VF_DALTON_K * shift;
}

// Filters in display (gamma) space, before the dither.
vec3 vfFilters(vec3 c)
{
#if (VF_DALTON > 0)
    c = clamp(vfDaltonize(c), 0.0, 1.0);
#endif
    float luma = dot(c, vec3(0.2126, 0.7152, 0.0722));
    c = mix(vec3(luma), c, VF_SAT);
    c = (c - 0.5) * VF_CON + 0.5 + VF_BRI;
#if (VF_VIG_ON == 1)
    vec2 d = v_TexCoord - 0.5;
    c *= 1.0 - VF_VIG * smoothstep(0.25, 0.75, length(d) * 1.4142);
#endif
    return clamp(c, 0.0, 1.0);
}
// -------------------------------------------------------------------------

vec3 gamma = vec3(2.2, 2.2, 2.2);
vec3 inv_gamma = 1.0 / gamma;

void main()
{
    // Read the original HDR value
    vec4 hdrValue = texelFetch(PostSourceTexture, ivec2(gl_FragCoord.xy), 0);
    float a = hdrValue.a;
#if (VF_SHARP_ON == 1)
    // Visual Filters: unsharp mask on the HDR source (4 neighbours).
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec3 n = texelFetch(PostSourceTexture, p + ivec2(1, 0), 0).rgb + texelFetch(PostSourceTexture, p - ivec2(1, 0), 0).rgb
           + texelFetch(PostSourceTexture, p + ivec2(0, 1), 0).rgb + texelFetch(PostSourceTexture, p - ivec2(0, 1), 0).rgb;
    hdrValue.rgb = max(hdrValue.rgb * (1.0 + 4.0 * VF_SHARP) - VF_SHARP * n, 0.0);
#endif

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
    out_FragColor.rgb = pow(max(out_FragColor.rgb, 0.0), inv_gamma);
    out_FragColor.rgb = vfFilters(out_FragColor.rgb);   // Visual Filters
#if (DITHER == 1)
    out_FragColor.rgb = max(out_FragColor.rgb + dither(), 0.0);
#endif
    out_FragColor.rgb = pow(out_FragColor.rgb, gamma);

    // Would be cheaper to just add dither with out gamma correction but at low color ranges
    // it starts to add visible noise but at higher colors it stops actually fixing the banding.
}
