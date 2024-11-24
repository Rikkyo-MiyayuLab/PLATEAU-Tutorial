using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private BusEnvManager _env;
    private string uuiid = System.Guid.NewGuid().ToString();

    void Start() {
        _env = GetComponentInParent<BusEnvManager>();
    }

    void Update() {
        PassengerCountText.text = WaitingPassengers.Count.ToString();
        
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

    public List<GameObject> GetPassenger(GameObject targetDestination) {

        List<GameObject> passengers = new List<GameObject>();
        List<GameObject> toRemove = new List<GameObject>(); // forループ中にイテレーションリストの要素を削除するとエラーをスローするので、コピーをつくる

        foreach (var passenger in WaitingPassengers) { 
            GameObject passengerDestination = passenger.GetComponent<Passenger>().Destination;
            Debug.Log("Passenger Destination: " + passengerDestination.name);
            Debug.Log("Next Destination: " + targetDestination.name); // FIXME: 次の行き先がカレントのバス停になっていて、乗客がいない
            if (passengerDestination.name == targetDestination.name) {
                passengers.Add(passenger);
                toRemove.Add(passenger); // 削除対象を別リストに追加
            }
        }

        // 削除対象を元リストから削除
        foreach (var passenger in toRemove) {
            WaitingPassengers.Remove(passenger);
        }

        return passengers;
    }


    public void SpawnPassenger() {
        int spawnCount = Random.Range(MinPassengerSpawnCount, MaxPassengerSpawnCount);
        for (int i = 0; i < spawnCount; i++) {
            var newPassenger = Instantiate(PassengerPrefab, transform.position, Quaternion.identity, transform);
            // 重ならないようにスポーンした乗客を少し横にずらす
            newPassenger.transform.position += new Vector3(Random.Range(-0.5f, 0.5f), 0.0f, Random.Range(-0.5f, 0.5f));
            // 乗客の目的地を環境内のバス停サイズでランダムに設定
            newPassenger.GetComponent<Passenger>().Destination = _env.GetRandomBusStop(this.gameObject);
            WaitingPassengers.Add(newPassenger);
        }
    }
}
