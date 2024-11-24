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
            navMeshAgent.velocity = Vector3.zero;
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
        Debug.Log("OnEpisodeBegin");
        // 各種パラメータの初期化
        passengers.Clear();
        RequestDecision();

    }

    public override void CollectObservations(VectorSensor sensor) {
        // 自身の位置・速度情報
        sensor.AddObservation(transform.position);
        sensor.AddObservation(navMeshAgent.velocity);
        //sensor.AddObservatList<ion>(passengers);
        // 目的地の位置情報
        //sensor.AddObservation(target.transform.position);
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
            //Debug.Log("Arrived" + target.name);
            GetOffPassengers();
            GameObject CurrentBusstop = target;
            target = _env.BusStops[Select]; // 行き先更新
            GetRidePassengers(CurrentBusstop.GetComponent<BusStop>(), target);
        }
        navMeshAgent.SetDestination(target.transform.position);
        navMeshAgent.isStopped = false;

    }

    private void GetOffPassengers() {
        // 降車対象のリストを準備
        List<GameObject> passengersToRemove = new List<GameObject>();

        foreach (var passenger in passengers) {
            // 乗客の目的地が現在のバス停と一致する場合
            if (passenger.GetComponent<Passenger>().Destination == target) {
                passengersToRemove.Add(passenger);
            }
        }

        // 降車対象をリストから削除
        foreach (var passenger in passengersToRemove) {
            passengers.Remove(passenger);
        }
    }


    private void GetRidePassengers(BusStop currentBusStop, GameObject nextBusStop) {
        List<GameObject> ridePassengers = currentBusStop.GetPassenger(nextBusStop);
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