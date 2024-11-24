using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class BusAgent : Agent {

    public NavMeshAgent navMeshAgent;
    public int MaxAccommodationCount = 50;
    public float MinimumSpeed = 0.5f;
    public GameObject target; //現在の目的地となっているバス停
    public List<GameObject> passengers = new List<GameObject>();
    public TextMeshPro passengersCounter;
    private BusEnvManager _env;

    private bool isArrived = false;

    void Start() {
        navMeshAgent = GetComponent<NavMeshAgent>();
        _env = GetComponentInParent<BusEnvManager>();
    }

    void Update() {

        if(navMeshAgent.remainingDistance < 1.0f) {
            // バス停に到着した場合
            navMeshAgent.isStopped = true;
            RequestDecision();
            /*
            if (target != null) {
                GetRidePassengers(target.GetComponent<BusStop>());
                GetOffPassengers();
            }
            */
        }

        // 乗客の数を更新
        passengersCounter.text = passengers.Count.ToString();

        
    }

    public override void Initialize() {}

    public override void OnEpisodeBegin() {
        // 各種パラメータの初期化
        passengers.Clear();
        
        // 初回の目的地を要求: NOTE: 
        int randomIndex = Random.Range(0, _env.BusStops.Count);
        target = _env.BusStops[randomIndex];
        RequestDecision();

    }

    public override void CollectObservations(VectorSensor sensor) {
        // 自身の位置・速度情報
        sensor.AddObservation(transform.position);
        sensor.AddObservation(navMeshAgent.velocity);
        //sensor.AddObservatList<ion>(passengers);
        // 目的地の位置情報
        sensor.AddObservation(target.transform.position);
    }


    /// <summary>
    /// 【行動情報】
    /// - 連続系行動
    /// 1. 移動スピード
    /// - 離散系行動  
    /// 1. バス停のID選択
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions) {
        var Select = actions.DiscreteActions[0]; //エージェントの選択目的地のID群
        //var Speed = actions.ContinuousActions[0]; //移動スピード
        // 現在と同じ停留所を選択した場合、再度決定を要求
        if (target == _env.BusStops[Select]) {
            RequestDecision();
            return;
        }

        // バス停に到着している場合
        if(navMeshAgent.isStopped) {
            Debug.Log("Arrived" + target.name);
            target = _env.BusStops[Select]; // 行き先更新
            GetRidePassengers(target.GetComponent<BusStop>());
            GetOffPassengers();
        }
        navMeshAgent.SetDestination(target.transform.position);
        navMeshAgent.isStopped = false;

    }

    private void GetOffPassengers() {
        // 目的地に到着した場合、乗客を降ろす
        foreach (var passenger in passengers) {
            // 乗客の目的地が自身の目的地と一致する場合
            if (passenger.GetComponent<Passenger>().Destination == target) {
                // 乗客を降ろす
                passengers.Remove(passenger);
            }
        }
    }

    private void GetRidePassengers(BusStop busStop) {
        Debug.Log("Next" + target.name);
        List<GameObject> ridePassengers = busStop.GetPassenger(target);
        foreach (var passenger in ridePassengers) {
            if(passengers.Count + 1 > MaxAccommodationCount){
                break; // バスの最大収容人数を超えたら中止
            }
            passengers.Add(passenger);
            // その乗客を非表示にする
            passenger.SetActive(false);
        }
    }
}