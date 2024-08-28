using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ShelterManagementAgent : Agent {
    
    private EnvManager _env;

    void Start() {
        _env = GetComponentInParent<EnvManager>();
        
    }
    public override void Initialize() {
        _env.OnInitializedEnv?.Invoke();
    }

    public override void OnEpisodeBegin() {
        _env.OnEpisodeBegin();
    }

    public override void CollectObservations(VectorSensor sensor) {}

    public override void OnActionReceived(ActionBuffers actions) {}


}
