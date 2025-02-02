using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ShelterManagementAgent : Agent {
    
    public GameObject[] ShelterCandidates; //エージェントが操作する避難所の候補リスト
    public Material SelectedMaterial;
    public Material NonSelectMaterial;
    public Action OnDidActioned;
    public List<Tuple<int, int, List<bool>>> ActionLogs = new List<Tuple<int, int, List<bool>>>(); // episode, step, 各避難所候補の選択状況のリスト(true or false)
    private EnvManager _env;
    EnvironmentParameters m_ResetParams;



    void Start() {
        _env = GetComponentInParent<EnvManager>();
        //Academy.Instance.AutomaticSteppingEnabled = true;

    }

    public override void Initialize() {
        Time.timeScale = 100f;
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        if(ShelterCandidates.Length == 0) {
            //Debug.LogError("No shelter candidates");
            // NOTE: 予め候補地は事前に設定させておくこと
            ShelterCandidates = GameObject.FindGameObjectsWithTag("Shelter");
        }
    }

    /// <summary>
    /// Agent.EndEpisode()後に呼ばれる
    /// </summary>
    public override void OnEpisodeBegin() {
        _env.OnStepBegin();
        Debug.Log("Episode begin");
        RequestDecision();
    }

    public void OnEndEpisode() {
        // データの保存とActionLogsの初期化
        string[] shelterIds = new string[ShelterCandidates.Length];
        for(int i = 0; i < ShelterCandidates.Length; i++) {
            shelterIds[i] = ShelterCandidates[i].name;
        }
        string[] headers = new string[ShelterCandidates.Length + 2];
        headers[0] = "Episode";
        headers[1] = "Step";
        Array.Copy(shelterIds, 0, headers, 2, shelterIds.Length);
        Utils.SaveResultCSV(
            headers,
            ActionLogs,
            (data) => new string[] { data.Item1.ToString(), data.Item2.ToString() }.Concat(data.Item3.ConvertAll(x => x ? "1" : "0")).ToArray(),
            $"{_env.recordID}/ActionLog_Episode_{_env.currentEpisodeId}.csv"
        );
        ActionLogs.Clear();
    }

    public void OnStepBegin() {
        _env.OnStepBegin();
        Debug.Log("begin step");
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
            //Debug.Log("ShelterPos?" + shelter.transform.GetChild(0).gameObject.transform.position);
            sensor.AddObservation(shelter.transform.GetChild(0).gameObject.transform.position);
            sensor.AddObservation(shelter.GetComponent<Shelter>().currentCapacity);
        }
        // 観測のタイミングで避難者が避難してGameObjectが消えることがあるので、ここでコピーを作成
        List<GameObject> evacuees = new List<GameObject>(_env.Evacuees);
        sensor.AddObservation(evacuees.Count);

        // 避難者の位置情報を追加
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
        List<bool> selectList = new List<bool>();
        if(Selects.Length != ShelterCandidates.Length) {
            Debug.LogError("Invalid action size : 避難所候補地のサイズとエージェントの選択サイズが不一致です");
            return;
        }

        for(int i = 0; i < Selects.Length; i++) {
            int select = Selects[i]; // 0:非選択、1:選択
            GameObject Shelter = ShelterCandidates[i];
            if(select == 1) {
                _env.CurrentShelters.Add(Shelter);
                Shelter.tag = "Shelter";
                Shelter.GetComponent<MeshRenderer>().material = SelectedMaterial;
                selectList.Add(true);
            } else if(select == 0) {
                _env.CurrentShelters.Remove(Shelter);
                Shelter.tag = "Untagged";
                Shelter.GetComponent<MeshRenderer>().material = NonSelectMaterial;
                selectList.Add(false);
            } else {
                Debug.LogError("Invalid action");
            }
        }

        // 行動ログを記録（episode, step, 各避難所候補の選択状況のリスト(true or false)）
        ActionLogs.Add(new Tuple<int, int, List<bool>>(_env.currentEpisodeId, _env.currentStep, selectList));
        

        OnDidActioned?.Invoke();
    }

    /// <summary>
    ///  ランダムに建物を選択
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut) {
        var Selects = actionsOut.DiscreteActions;
        for(int i = 0; i < Selects.Length; i++) {
            Selects[i] = UnityEngine.Random.Range(0, 2);
        }
    }


}
