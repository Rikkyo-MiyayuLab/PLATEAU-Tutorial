using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class BusAgent : Agent {

    public NavMeshAgent navMeshAgent;
    public GameObject target; //バス停

    void Start() {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public override void Initialize() {}

    public override void OnEpisodeBegin() {}

    public override void CollectObservations(VectorSensor sensor) {}


    /// <summary>
    /// 【行動情報】
    /// - 連続系行動
    /// 1. 移動スピード
    /// - 離散系行動  
    /// 1. バス停のID選択
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions) {}


    
}