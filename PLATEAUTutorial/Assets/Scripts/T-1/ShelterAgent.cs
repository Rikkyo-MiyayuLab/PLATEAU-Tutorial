using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ShelterManagementAgent : Agent {
    
    public GameObject[] ShelterCandidates;
    public Material SelectedMaterial;
    public Material NonSelectMaterial;
    public Action OnDidActioned;
    private EnvManager _env;


    void Start() {
        _env = GetComponentInParent<EnvManager>();
    }
    public override void Initialize() {
        //_env.OnInitializedEnv?.Invoke();
        if(ShelterCandidates.Length == 0) {
            //Debug.LogError("No shelter candidates");
            // NOTE: 予め候補地は事前に設定させておくこと
            ShelterCandidates = GameObject.FindGameObjectsWithTag("Shelter");
        }
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
            Debug.Log("ShelterPos?" + shelter.transform.GetChild(0).gameObject.transform.position);
            sensor.AddObservation(shelter.transform.GetChild(0).gameObject.transform.position);
            sensor.AddObservation(shelter.GetComponent<Tower>().currentCapacity);
        }
        // 観測のタイミングで避難者が避難してGameObjectが消えることがあるので、ここでコピーを作成
        List<GameObject> evacuees = new List<GameObject>(_env.Evacuees);
        sensor.AddObservation(evacuees.Count);

        // 避��者の位置情報を追加
        foreach(GameObject evacuee in evacuees) {
            if(evacuee != null) {
                sensor.AddObservation(evacuee.transform.position);
            } else {
                sensor.AddObservation(Vector3.zero);
            }
        }
        

    }

    public override void OnActionReceived(ActionBuffers actions) {
        var Selects = actions.DiscreteActions; //エージェントの選択。環境の候補地配列と同じ順序
        if(Selects.Length != ShelterCandidates.Length) {
            Debug.LogError("Invalid action size : 避難所候補地のサイズとエージェントの選択サイズが不一致です");
            return;
        }

        for(int i = 0; i < Selects.Length; i++) {
            int select = Selects[i]; // 0:非選択、1:選択
            GameObject Shelter = ShelterCandidates[i];
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
        OnDidActioned?.Invoke();
    }


}
