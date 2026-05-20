using System;
using UnityEngine;

public class PenetrationManager : MonoBehaviour {
    static PenetrationManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Initialize() {
        instance = null;
    }
    
    public delegate void PenetratorReadAction();
    public delegate void PenetratorWriteAction(float dt);
    
    public static void SubscribeToPenetratorUpdates(PenetratorReadAction readAction, PenetratorWriteAction writeAction) {
        Instance.penetratorRead += readAction;
        Instance.penetratorWrite += writeAction;
    }
    public static void UnsubscribeToPenetratorUpdates(PenetratorReadAction readAction, PenetratorWriteAction writeAction) {
        if (!instance) return;
        instance.penetratorRead -= readAction;
        instance.penetratorWrite -= writeAction;
    }
    
    public static PenetrationManager Instance {
        get {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<PenetrationManager>();
            if (instance != null) return instance;
            var go = new GameObject("PenetrationManager");
            instance = go.AddComponent<PenetrationManager>();
            return instance;
        }
    }

    event PenetratorReadAction penetratorRead;
    event PenetratorWriteAction penetratorWrite;
    
    void LateUpdate() {
        penetratorRead?.Invoke();
        penetratorWrite?.Invoke(Time.deltaTime);
    }
    
}
