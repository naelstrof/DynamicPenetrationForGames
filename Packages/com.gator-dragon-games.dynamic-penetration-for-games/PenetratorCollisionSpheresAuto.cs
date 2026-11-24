using System;
using UnityEngine;

namespace GatorDragonGames.DPG {

[Serializable]
public static class PenetratorCollisionSpheresAuto {
    public static void Render(Renderer[] renderers, GirthFrame girthFrameWorld, float inflation, float factor, int subdivisions) {
        var rendererCount = renderers.Length;
        for (int i=0;i<subdivisions;i++) {
            var alpha = (float)i/(subdivisions-1);
            var girthSlice = girthFrameWorld.SampleGirth(alpha);
            if (girthSlice.radius == 0f) continue;
            var sphereCast = new SphereCasterRenderer.SphereCast(girthSlice.position, Vector3.one * (girthSlice.radius+inflation), Quaternion.identity, factor, 0f, true, true, true);
            for(int j=0;j<rendererCount;j++) {
                SphereCasterRenderer.Render(renderers[j], sphereCast);
            }
        }
    }
}
}