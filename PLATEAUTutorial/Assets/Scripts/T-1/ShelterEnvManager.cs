using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using PLATEAU.CityInfo;
using PLATEAU.Util;
using Newtonsoft.Json;

/// <summary>
/// シミュレータ環境全般の制御を行うクラス
/// </summary>
public class EnvManager : MonoBehaviour {
    public enum SimulateMode {
        Train,
        Inference
    }

    [Header("Environment Settings")]
    public SimulateMode Mode = SimulateMode.Train; // 実行モード（Train or Inference）
    public float TimeScale = 1.0f; // シミュレーションの時間スケール
    /// <summary>
    /// 生成する避難者の人数に合わせて避難所の収容人数をスケーリングします.
    /// </summary>
    /// <example>
    /// スケーリング例:
    /// <list type="bullet">
    /// <item>
    /// <description>1.0f: 通常 → 収容人数算出式に合わせて避難所の収容人数を設定</description>
    /// </item>
    /// <item>
    /// <description>0.5f: 避難者の人数が半分 → 避難所の収容人数も半分</description>
    /// </item>
    /// </list>
    /// </example>
    public float AccSimulateScale = 1.0f; 
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
        if(Mode == SimulateMode.Inference) {
            Time.timeScale = TimeScale; // 推論時のみシミュレーションの時間スケールを設定
        }

        if(AccSimulateScale > 1.0f) {
            Debug.LogError("AccSimulateScale is greater than 1.0f. Please set the value between 0.0f and 1.0f.");
        }
        NavMesh.pathfindingIterationsPerFrame = 1000000;

        Agent = AgentObj.GetComponent<ShelterManagementAgent>();
        Evacuees = new List<GameObject>(); // 避難者のリストを初期化
        CurrentShelters = new List<GameObject>(); // 避難所のリストを初期化
        Shelters = new List<GameObject>(); // 避難所のリストを初期化
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
                tower.MaxCapacity = GetAccSize(shelter);
                tower.NowAccCount = 0;
            }
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = gizmoColor; // Gizmoの色を設定
        DrawWireCircle(spawnCenter, SpawnRadius);
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



    /// <summary>
    /// 避難者のランダムスポーン範囲を描画する
    /// </summary>
    private void DrawWireCircle(Vector3 center, float radius, int segments = 36) {
        float angle = 0f;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0); // 初期点

        for (int i = 1; i <= segments; i++) {
            angle += angleStep;
            float rad = Mathf.Deg2Rad * angle;

            Vector3 newPoint = center + new Vector3(Mathf.Cos(rad) * radius, 5, Mathf.Sin(rad) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);

            prevPoint = newPoint; // 次の線を描画するために現在の点を更新
        }
    }


    /// <summary>
    /// 属性情報から避難所の収容人数を取得する
    /// 【計算式】
    /// 収容可能人数＝ 床総面積㎡×0.8÷1.65㎡
    /// ※出典：https://manboukama.ldblog.jp/archives/50540532.html
    /// </summary>
    /// <param name="shelterBldg">避難所のGameObject</param>
    /// <returns>避難所の収容人数(設定パラメータによりスケーリングされます)</returns>
    private int GetAccSize(GameObject shelterBldg) {
        double? totalFloorSize = null;
        // PLATEAU City Objectから、建物の高さを取得し、避難所の収容人数を動的に設定する
        var cityObjectGroup = shelterBldg.GetComponent<PLATEAUCityObjectGroup>();
        var rootCityObject = cityObjectGroup.CityObjects.rootCityObjects[0];

        // Newtonsoft.Jsonを使用して、CityObjectの属性情報クラスにデシリアライズして取得
        var cityObjectJsonStr = JsonConvert.SerializeObject(rootCityObject);
        var attributes = JsonConvert.DeserializeObject<RootObject>(cityObjectJsonStr).Attributes;
        // 属性値リストを巡回し、床総面積から収容人数を算出
        foreach(var attribute in attributes) {
            if(attribute.Key == "uro:buildingDetailAttribute") {
                foreach(var uroAttr in attribute.AttributeSetValue) { 
                    if(uroAttr.Key == "uro:totalFloorArea") {
                        if(double.TryParse(uroAttr.Value.ToString(), out double parsedValue)) {
                            totalFloorSize = parsedValue;
                        }
                    }
                }
            }
        }

        // 結果が取得できなかった場合は0を返す
        if(totalFloorSize == null) {
            Debug.LogError("Failed to get the total floor size of the shelter building.");
            return 0;
        } else {
            // 収容可能人数＝総面積×0.8÷1.65㎡とする
            return (int)((totalFloorSize * 0.8 / 1.65) * AccSimulateScale);
        }
    }
}
