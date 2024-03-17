using System;
using System.Collections;
using System.Collections.Generic;
using DPG;
using UnityEngine;

public class PenetratorAudioSlide : MonoBehaviour {
    [SerializeField] private Penetrator penetrator;
    [SerializeField] private AudioClip clip;
    
    private static AnimationCurve audioFalloff = new() {keys=new Keyframe[] { new (0f, 1f, 0, -3.1f), new (1f, 0f, 0f, 0f) } };
    private AudioSource source;
    private float? lastDepth;
    private void OnEnable() {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 1f;
        source.minDistance = 1f;
        source.maxDistance = 25f;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, audioFalloff);
        source.enabled = false;
        penetrator.penetrated += OnPenetrated;
        penetrator.unpenetrated += OnUnpenetrated;
    }

    private void Update() {
        source.volume = Mathf.MoveTowards(source.volume, 0f, Time.deltaTime);
        if (source.volume == 0f && source.enabled) {
            source.enabled = false;
        }
    }

    private void OnUnpenetrated(Penetrator penetrator1, Penetrable penetrable) {
    }

    private void OnPenetrated(Penetrator penetrator1, Penetrable penetrable, Penetrator.PenetrationArgs penetrationArgs, Penetrable.PenetrationResult result) {
        float movement = Mathf.Abs((lastDepth ?? penetrationArgs.penetrationDepth) - penetrationArgs.penetrationDepth);
        if (!source.enabled && movement > Mathf.Epsilon) {
            source.enabled = true;
        }
        source.volume += movement*4f;
        source.volume = Mathf.Clamp01(source.volume);
        lastDepth = penetrationArgs.penetrationDepth;
    }

    private void OnDisable() {
        Destroy(source);
    }
    
}
