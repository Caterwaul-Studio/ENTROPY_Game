#ifndef HOLOMAP_WRIST_VIEW_INCLUDED
#define HOLOMAP_WRIST_VIEW_INCLUDED

// Set by MapViewport for the wrist map's camera only. 0 everywhere else, which turns these off
float _HoloMapWristView;
float _HoloMapCutStart;
float _HoloMapCutEnd;

static const half BayerMatrix[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

// Dithers away what's between the camera and the player: gone before CutStart, fully shown after CutEnd
void NearCut(float depth, float4 positionCS)
{
    if (_HoloMapCutEnd <= 0) return;
    half shown = saturate((depth - _HoloMapCutStart) / max(_HoloMapCutEnd - _HoloMapCutStart, 0.001));
    uint2 pixel = uint2(positionCS.xy) % 4;
    clip(shown - (BayerMatrix[pixel.y * 4 + pixel.x] + 0.5) / 16.0);
}

#endif
