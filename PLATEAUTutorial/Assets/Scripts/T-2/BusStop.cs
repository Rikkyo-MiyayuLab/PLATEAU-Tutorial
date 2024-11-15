using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BusStop : MonoBehaviour {
    
    public GameObject PassengerPrefab;
    public float SpawnIntervalSec = 2.0f;
    public int MaxPassengerSpawnCount = 10;
    public int MinPassengerSpawnCount = 1;
    public float AvarageWaitTimeSec = 0.0f; // 乗客の平均待ち時間
    public List<GameObject> WaitingPassengers = new List<GameObject>();
    public TextMeshPro PassengerCountText;
    private float _timer = 0.0f;

    void Update() {
        _timer += Time.deltaTime;
        if (_timer > SpawnIntervalSec) {
            SpawnPassenger();
            _timer = 0.0f;
        }

        // 乗客の平均待ち時間を計算
        if (WaitingPassengers.Count > 0) {
            float totalWaitTime = 0.0f;
            foreach (var passenger in WaitingPassengers) {
                totalWaitTime += passenger.GetComponent<Passenger>().WaitTimeSec;
            }
            AvarageWaitTimeSec = totalWaitTime / WaitingPassengers.Count;
        }
    }

    public void SpawnPassenger() {
        int spawnCount = Random.Range(MinPassengerSpawnCount, MaxPassengerSpawnCount);
        for (int i = 0; i < spawnCount; i++) {
            var newPassenger = Instantiate(PassengerPrefab, transform.position, Quaternion.identity, transform);
            // 重ならないようにスポーンした乗客を少し横にずらす
            newPassenger.transform.position += new Vector3(Random.Range(-0.5f, 0.5f), 0.0f, Random.Range(-0.5f, 0.5f));
            WaitingPassengers.Add(newPassenger);
        }
        PassengerCountText.text = WaitingPassengers.Count.ToString();
    }
}
