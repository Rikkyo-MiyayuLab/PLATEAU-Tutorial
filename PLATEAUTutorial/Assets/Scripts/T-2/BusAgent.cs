using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class BusAgent : Agent {

    public NavMeshAgent navMeshAgent;
    public float MinimumSpeed = 0.5f;
    public GameObject target; //バス停
    public int passengerCount = 0;
    private BusEnvManager _env;

    private bool isArrived = false;

    void Start() {
        navMeshAgent = GetComponent<NavMeshAgent>();
        _env = GetComponentInParent<BusEnvManager>();
    }

    void Update() {
        if(navMeshAgent.remainingDistance < 1.0f) {
            RequestDecision();
        }

        /*
        // 移動中かどうか確認
        if (navMeshAgent.velocity.sqrMagnitude > 0.1f) {
            // 移動方向を取得
            Vector3 direction = navMeshAgent.velocity.normalized;

            // 移動方向に向けて回転
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * navMeshAgent.angularSpeed);
        }
        */
    }

    public override void Initialize() {}

    public override void OnEpisodeBegin() {
        // 初回の目的地を要求
        int randomIndex = Random.Range(0, _env.BusStops.Count);
        target = _env.BusStops[randomIndex];
        RequestDecision();

    }

    public override void CollectObservations(VectorSensor sensor) {
        // 自身の位置・速度情報
        sensor.AddObservation(transform.position);
        sensor.AddObservation(navMeshAgent.velocity);
        sensor.AddObservation(passengerCount);
        //目的地の位置情報
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
        var Selects = actions.DiscreteActions[0]; //エージェントの選択目的地のID群
        var Speed = actions.ContinuousActions[0]; //移動スピード
        if(Speed < MinimumSpeed) {
            Speed = MinimumSpeed;
        }
        target = _env.BusStops[Selects];

        navMeshAgent.speed = Speed * 10;
        navMeshAgent.SetDestination(target.transform.position);

        // 進行方向正面に向ける

        
    }


    
}