using System;
using System.Collections.Generic;
using DPG;
using UnityEngine;

namespace GatorDragonGames {

[Serializable]
public static class PenetratorCollisionSpheresAuto {
    public static void Render(IList<Renderer> renderers, GirthFrame girthFrameWorld, float inflation, float factor, int subdivisions) {
        for (int i=0;i<subdivisions;i++) {
            var alpha = (float)i/(subdivisions-1);
            var girthSlice = girthFrameWorld.SampleGirth(alpha);
            if (girthSlice.radius == 0f) continue;
            var sphereCast = new SphereCasterRenderer.SphereCast(girthSlice.position, Vector3.one * (girthSlice.radius+inflation), Quaternion.identity, factor, 0f, true, true, true);
            sphereCast.DrawDebug(Color.red);
            foreach (var renderer in renderers) {
                SphereCasterRenderer.Render(renderer, sphereCast);
            }
        }
    }
}
}