using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

/// <summary>
/// シミュレータ環境全般の制御を行うクラス
/// </summary>
public class EnvManager : MonoBehaviour {

    [Header("Environment Settings")]
    public float MaxSeconds = 60.0f; // シミュレーションの最大時間（秒）
    public int SpawnEvacueeSize;
    public GameObject SpawnEvacueePref; // 避難者のプレハブ
    public float SpawnRadius = 10f; // スポーンエリアの半径
    public Vector3 spawnCenter = Vector3.zero; // スポーンエリアの中心位置

    public GameObject AgentObj;
    public ShelterManagementAgent Agent;

    [Header("Objects")]
    [System.NonSerialized]
    public List<GameObject> Evacuees; // 避難者のリスト
    [System.NonSerialized]
    public List<GameObject> CurrentShelters; // 現在のアクティブな避難所のリスト
    public List<GameObject> Shelters; // 全避難所のリスト

    [Header("UI Elements")]
    public TextMeshProUGUI stepCounter;

    // Event Listeners
    public delegate void EndEpisodeHandler(float evacueeRate);
    public EndEpisodeHandler OnEndEpisode;
    public delegate void StartEpisodeHandler();
    public StartEpisodeHandler OnStartEpisode;
    public delegate void OnInitHandler();
    public OnInitHandler OnInitializedEnv;
    [Header("Parameters")]
    public float EvacuationRate; // 全体の避難率
    public bool EnableEnv = false; // 環境の準備が完了したか否か（利用不可の場合はfalse）
    private int currentStep;
    private float currentTimeSec;
    private Color gizmoColor = Color.red; // Gizmoの色

    void Start() {
        NavMesh.pathfindingIterationsPerFrame = 1000000;
        Agent = AgentObj.GetComponent<ShelterManagementAgent>();
        Evacuees = new List<GameObject>(); // 避難者のリストを初期化
        CurrentShelters = new List<GameObject>(); // 避難所のリストを初期化
        currentStep = Agent.StepCount;

        OnEndEpisode += (float evacuateRate) => {
            currentTimeSec = 0;
            //Dispose();
            //エージェントに避難率と終了までにかかったステップ数に基づいて報酬を与える
            Agent.SetReward(evacuateRate * 100);
            Agent.EndEpisode();
        };

        // 避難所登録
        Shelters = new List<GameObject>(GameObject.FindGameObjectsWithTag("Shelter"));
        // 固定値の避難所を追加
        GameObject[] constSheleters = GameObject.FindGameObjectsWithTag("ConstShelter");
        foreach (var shelter in constSheleters) {
            Shelters.Add(shelter);
        }
        // コンポーネントの初期化
        foreach (var shelter in Shelters) {
            if(shelter.GetComponent<Shelter>() == null) {
                Shelter tower = shelter.AddComponent<Shelter>();
                tower.uuid = System.Guid.NewGuid().ToString();
                tower.MaxCapacity = 10;
                tower.NowAccCount = 0;
            }
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = gizmoColor; // Gizmoの色を設定
        Gizmos.DrawWireSphere(spawnCenter, SpawnRadius); // 中心から半径のワイヤーフレームの球体を描画
    }

    void FixedUpdate() {
        currentTimeSec += Time.deltaTime;
        if (currentTimeSec >= MaxSeconds) {
            OnEndEpisode?.Invoke(EvacuationRate);
        }
        EvacuationRate = GetCurrentEvacueeRate();
        UpdateUI();
    }

    /// <summary>
    /// エピソード開始時の処理
    /// </summary>
    public void OnEpisodeBegin() {
        EnableEnv = false;
        Dispose();
        Create();
        OnStartEpisode?.Invoke();
        EnableEnv = true;
    }

    public void Dispose() {
        foreach (var evacuee in Evacuees) {
            Destroy(evacuee);
        }
        Evacuees = new List<GameObject>(); // 新しいリストを作成
        CurrentShelters = new List<GameObject>(); // 新しいリストを作成
    }


    public void Create() {


        for (int i = 0; i < SpawnEvacueeSize; i++) {
            Vector3 spawnPos = GetRandomPositionOnNavMesh();
            spawnPos.y = 1.2f;
            if (spawnPos != Vector3.zero) {
                GameObject evacuee = Instantiate(SpawnEvacueePref, spawnPos, Quaternion.identity, transform);
                evacuee.tag = "Evacuee";
                Evacuees.Add(evacuee);
            }
        }

        /*
        CurrentShelters = new List<GameObject>(GameObject.FindGameObjectsWithTag("Shelter"));
        foreach (var shelter in CurrentShelters) {
            if(shelter.GetComponent<Tower>() == null) {
                Tower tower = shelter.AddComponent<Tower>();
                tower.uuid = System.Guid.NewGuid().ToString();
                tower.MaxCapacity = 10;
                tower.NowAccCount = 0;
            }
        }
        */
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
        stepCounter.text = $"Remain Seconds : {MaxSeconds - currentTimeSec:F2}";
    }


    private float GetCurrentEvacueeRate() {
        int evacueeSize = Evacuees.Count;
        int evacuatedSize = 0;
        foreach (var evacuee in Evacuees) {
            if (!evacuee.activeSelf) {
                evacuatedSize++;
            }
        }
        return (float)evacuatedSize / evacueeSize;
    }


}
