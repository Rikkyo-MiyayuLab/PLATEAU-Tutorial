using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ShelterManagementAgent : Agent {
    
    public GameObject[] ShelterCandidates;
    public Material SelectedMaterial;
    public Material NonSelectMaterial;
    private EnvManager _env;

    void Start() {
        _env = GetComponentInParent<EnvManager>();
    }
    public override void Initialize() {
        //_env.OnInitializedEnv?.Invoke();
    }

    public override void OnEpisodeBegin() {
        _env.OnEpisodeBegin();
        Debug.Log("Episode begin");
        RequestDecision();
    }

    /// <summary>
    /// 1. 各避難所候補地の位置情報
    /// 2. 各候補地が収容できる避難者の数。
    /// 3. 避難者の現在位置 
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor) {

        foreach(GameObject shelter in ShelterCandidates) {
            Debug.Log("ShelterPos?" + shelter.transform.position);
            sensor.AddObservation(shelter.transform.position);
            sensor.AddObservation(shelter.GetComponent<Tower>().currentCapacity);
        }
        foreach(GameObject evacuee in _env.Evacuees) {
            sensor.AddObservation(evacuee.transform.position);
        }
        

    }

    public override void OnActionReceived(ActionBuffers actions) {
        var Selects = actions.DiscreteActions; //エージェントの選択。環境の候補地配列と同じ順序
        foreach(int select in Selects) {
            GameObject Shelter = ShelterCandidates[select];
            if(select == 1) {
                _env.Shelters.Add(Shelter);
                Shelter.tag = "Shelter";
                Shelter.GetComponent<MeshRenderer>().material = SelectedMaterial;
            } else if(select == 0) {
                _env.Shelters.Remove(Shelter);
                Shelter.tag = "Untagged";
                Shelter.GetComponent<MeshRenderer>().material = NonSelectMaterial;
            } else {
                Debug.LogError("Invalid action");
            }
        }
    }


}
