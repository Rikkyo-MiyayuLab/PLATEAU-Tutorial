using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Passenger : MonoBehaviour {

    public float WaitTimeSec = 0.0f; // バス停に待機している累計時間
    public GameObject Destination; // 乗客の目的地
    public bool isWaiting = true;
    private float _timer = 0.0f;

    void Update() {
        if (isWaiting) {
            _timer += Time.deltaTime;
            WaitTimeSec += Time.deltaTime;
        }
    }

    public void ResetTimer() {
        _timer = 0.0f;
    }    
}