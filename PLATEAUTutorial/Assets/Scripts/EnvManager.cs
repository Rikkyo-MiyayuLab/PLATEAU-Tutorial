using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

/// <summary>
/// シミュレータ環境全般の制御を行うクラス
/// TODO: このクラスは最終的に、以下２つの親クラスとして変更する予定
/// ・T-1: 避難所配置最適化シミュレーションクラス
/// ・T-2: 観光地バス自動運転シミュレーションクラス  
/// </summary>
public class EnvManager : MonoBehaviour {

    [Header("Environment Settings")]
    public int MaxSteps;
    public int SpawnEvacueeSize;
    public GameObject SpawnEvacueePref; // 避難者のプレハブ
    public GameObject ExMarkPref;
    public float SpawnRadius = 10f; // スポーンエリアの半径
    public Vector3 spawnCenter = Vector3.zero; // スポーンエリアの中心位置

    public GameObject AgentObj;
    public ShelterManagementAgent Agent;

    [Header("Objects")]
    public List<GameObject> Evacuees; // 避難者のリスト
    public List<GameObject> Shelters; // 現在の避難所のリスト

    [Header("UI Elements")]
    public TextMeshProUGUI stepCounter;

    // Event Listeners
    public delegate void EndEpisodeHandler(float evacueeRate, int endStep);
    public EndEpisodeHandler OnEndEpisode;
    public delegate void StartEpisodeHandler();
    public StartEpisodeHandler OnStartEpisode;
    public delegate void OnInitHandler();
    public OnInitHandler OnInitializedEnv;
    [Header("Parameters")]
    public float EvacuationRate; // 全体の避難率
    public bool EnableEnv = false; // 環境の準備が完了したか否か（利用不可の場合はfalse）

    protected int currentStep;

    private Color gizmoColor = Color.red; // Gizmoの色

    void Start() {
        Agent = AgentObj.GetComponent<ShelterManagementAgent>();
        currentStep = Agent.StepCount;
        MaxSteps = Agent.MaxStep;
        OnEndEpisode += (float evacuateRate, int endStep) => {
            //Dispose();
            //エージェントに避難率と終了までにかかったステップ数に基づいて報酬を与える
            Agent.SetReward(evacuateRate);
            // かかったステップ数が少ないほど報酬が高い
            if(endStep > MaxSteps) { //超える場合があるので、ここで補正
                endStep = MaxSteps;
            }
            Agent.AddReward(1.0f - (float)endStep / MaxSteps);
            Agent.EndEpisode();
        };
    }

    void OnDrawGizmos() {
        Gizmos.color = gizmoColor; // Gizmoの色を設定
        Gizmos.DrawWireSphere(spawnCenter, SpawnRadius); // 中心から半径のワイヤーフレームの球体を描画
    }

    void FixedUpdate() {
        currentStep += Agent.StepCount;
        EvacuationRate = GetCurrentEvacueeRate();
        UpdateUI();
        if ((currentStep >= MaxSteps && MaxSteps > 0)) {
            OnEndEpisode?.Invoke(EvacuationRate, currentStep);
        }

    }

    /// <summary>
    /// エピソード開始時の処理
    /// </summary>
    public void OnEpisodeBegin() {
        EnableEnv = false;
        Create();
        OnStartEpisode?.Invoke();
        EnableEnv = true;
    }

    public void Dispose() {
        // 避難者の削除
        foreach (var evacuee in Evacuees) {
            Destroy(evacuee);
        }
        Evacuees.Clear();
        Shelters.Clear();
        currentStep = 0;

        Evacuees = new List<GameObject>();
        Shelters = new List<GameObject>();
    }

    public void Create() {

        Dispose();

        for (int i = 0; i < SpawnEvacueeSize; i++) {
            Vector3 spawnPos = GetRandomPositionOnNavMesh();
            spawnPos.y = 1.2f;
            if (spawnPos != Vector3.zero) {
                GameObject evacuee = Instantiate(SpawnEvacueePref, spawnPos, Quaternion.identity, transform);
                evacuee.tag = "Evacuee";
                Evacuees.Add(evacuee);
            }
        }

        Shelters = new List<GameObject>(GameObject.FindGameObjectsWithTag("Shelter"));
        foreach (var shelter in Shelters) {
            if(shelter.GetComponent<Tower>() == null) {
                Tower tower = shelter.AddComponent<Tower>();
                tower.uuid = System.Guid.NewGuid().ToString();
                tower.MaxCapacity = 10;
                tower.NowAccCount = 0;
            }
            // ExMarkの追加（存在しない場合のみ）
            if(shelter.transform.Find("ExMark") == null) {
                // GameObjectをshlterを親にして生成
                /* NOTE : エピソード終了毎に無限生成されてしまう
                GameObject exMark = Instantiate(ExMarkPref, shelter.transform);
                exMark.transform.localPosition = Vector3.zero;
                exMark.transform.parent = shelter.transform;
                exMark.GetComponent<MeshRenderer>().enabled = false; // 非表示にする
                */
            }
        }
    }


    /// <summary>
    /// ナビメッシュ上の任意の座標を取得する。
    /// </summary>
    /// <returns>ランダムなナビメッシュ上の座標 or Vector3.zero</returns>
    private Vector3 GetRandomPositionOnNavMesh() {
        Vector3 randomDirection = Random.insideUnitSphere * SpawnRadius; // 半径内のランダムな位置を取得
        randomDirection += spawnCenter; // 中心位置を加算
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, SpawnRadius, NavMesh.AllAreas)) {
            return hit.position;
        }
        return Vector3.zero; // ナビメッシュが見つからなかった場合
    }

    private void UpdateUI() {
        stepCounter.text = $"Remain Steps : {MaxSteps - currentStep}";
    }


    private float GetCurrentEvacueeRate() {
        int evacueeSize = Evacuees.Count;
        // 避難済みの避難者はgameObjectがfalseになっているので、それで判定
        int evacuatedSize = Evacuees.RemoveAll(e =>!e.activeSelf);
        return (float)evacuatedSize / evacueeSize;
    }

}
