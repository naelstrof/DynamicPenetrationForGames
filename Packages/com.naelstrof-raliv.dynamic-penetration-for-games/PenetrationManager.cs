using System;
using UnityEngine;

public class PenetrationManager : MonoBehaviour {
    static PenetrationManager instance;
    public static int frame;

    [RuntimeInitializeOnLoadMethod]
    public static void Initialize() {
        instance = null;
    }
    
    public static void SubscribeToPenetratorUpdates(Action read, Action<float> write) {
        Instance.penetratorRead += read;
        Instance.penetratorWrite += write;
    }
    public static void UnsubscribeToPenetratorUpdates(Action read, Action<float> write) {
        if (!instance) return;
        instance.penetratorRead -= read;
        instance.penetratorWrite -= write;
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

    event Action penetratorRead;
    event Action<float> penetratorWrite;
    
    void LateUpdate() {
        frame++;
        penetratorRead?.Invoke();
        penetratorWrite?.Invoke(Time.deltaTime);
    }
    
}
