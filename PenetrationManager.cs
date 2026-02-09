using System;
using UnityEngine;

public class PenetrationManager : MonoBehaviour {
    
    static PenetrationManager instance;
    public static void SubscribeToPenetratorUpdates(Action callback) {
        Instance.UpdatePenetrators += callback;
    }
    
    public static void SubscribeToPenetratorFixedUpdates(Action callback) {
        Instance.FixedUpdatePenetrators += callback;
    }
    
    public static PenetrationManager Instance {
        get {
            if (instance != null) return instance;
            instance = FindObjectOfType<PenetrationManager>();
            if (instance != null) return instance;
            var go = new GameObject("PenetrationManager");
            instance = go.AddComponent<PenetrationManager>();
            return instance;
        }
    }

    event Action UpdatePenetrators;
    event Action FixedUpdatePenetrators;
    
    void FixedLateUpdate() {
        FixedUpdatePenetrators?.Invoke();
    }
    
    void LateUpdate() {
        UpdatePenetrators?.Invoke();
    }
    
}
