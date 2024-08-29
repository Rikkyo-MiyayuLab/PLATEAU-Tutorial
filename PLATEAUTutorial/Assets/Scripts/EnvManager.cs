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
    public int nowStep;
    public int SpawnEvacueeSize;
    public GameObject SpawnEvacueePref; // 避難者のプレハブ
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
    public delegate void EndEpisodeHandler(float evacueeRate);
    public EndEpisodeHandler OnEndEpisode;
    public delegate void StartEpisodeHandler();
    public StartEpisodeHandler OnStartEpisode;
    public delegate void OnInitHandler();
    public OnInitHandler OnInitializedEnv;
    [Header("Parameters")]
    public float EvacuationRate; // 全体の避難率

    protected int m_Timer;

    private Color gizmoColor = Color.red; // Gizmoの色

    void Start() {
        nowStep = 0;
        m_Timer = 0;
        Agent = AgentObj.GetComponent<ShelterManagementAgent>();
        OnEndEpisode += (float _) => {
            Agent.EndEpisode();
            Reset();
        };
    }

    void OnDrawGizmos() {
        Gizmos.color = gizmoColor; // Gizmoの色を設定
        Gizmos.DrawWireSphere(spawnCenter, SpawnRadius); // 中心から半径のワイヤーフレームの球体を描画
    }

    void FixedUpdate() {
        m_Timer += 1;
        UpdateUI();
        if ((m_Timer >= MaxSteps && MaxSteps > 0)) {
            OnEndEpisode?.Invoke(EvacuationRate);
        }

    }

    /// <summary>
    /// エピソード開始時の処理
    /// </summary>
    public void OnEpisodeBegin() {

        Evacuees = new List<GameObject>(GameObject.FindGameObjectsWithTag("Evacuee"));
        Shelters = new List<GameObject>(GameObject.FindGameObjectsWithTag("Shelter"));

        for (int i = 0; i < SpawnEvacueeSize; i++) {
            Vector3 spawnPos = GetRandomPositionOnNavMesh();
            if (spawnPos != Vector3.zero) {
                GameObject evacuee = Instantiate(SpawnEvacueePref, spawnPos, Quaternion.identity);
                evacuee.tag = "Evacuee";
                Evacuees.Add(evacuee);
            }
        }
        OnStartEpisode?.Invoke();
    }

    public void Reset() {
        // 避難者の削除
        foreach (var evacuee in Evacuees) {
            Destroy(evacuee);
        }
        Evacuees.Clear();
        Shelters.Clear();
        m_Timer = 0;
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
        stepCounter.text = $"Remain Steps : {MaxSteps - m_Timer}";
    }

}
