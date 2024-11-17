using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Passenger : MonoBehaviour {

    public float WaitTimeSec = 0.0f; // バス停に待機している累計時間
    private float _timer = 0.0f;

    void Update() {
        _timer += Time.deltaTime;
        WaitTimeSec += Time.deltaTime;
    }

    
}