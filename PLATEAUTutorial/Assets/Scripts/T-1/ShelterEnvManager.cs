using System;
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
    /**シミュレーションモードの選択を定義*/
    public enum SimulateMode {
        Train, // モデル訓練
        Inference // モデル推論
    }

    public enum SpawnMode {
        Random, // 一定の範囲内でランダムに出現
        Custom, // 自身でスポーン位置・範囲を設定
    }

    [Header("Environment Settings")]
    public SimulateMode Mode = SimulateMode.Train; 
    public SpawnMode EvacSpawnMode = SpawnMode.Random; 
    public float TimeScale = 1.0f; // 推論時のシミュレーションの時間スケール
    public bool IsRecordData = false;
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
    public TextMeshProUGUI evacRateCounter;

    // Event Listeners
    public delegate void EndEpisodeHandler(float evacueeRate);
    public EndEpisodeHandler OnEndEpisode;
    public delegate void StartEpisodeHandler();
    public StartEpisodeHandler OnStartEpisode;
    [Header("Parameters")]
    public float EvacuationRate; // 全体の避難率
    public bool EnableEnv = false; // 環境の準備が完了したか否か（利用不可の場合はfalse）
    public int currentStep;
    private float currentTimeSec;
    private List<Tuple<float, float>> evaRatePerSec = new List<Tuple<float, float>>();
    public int currentEpisodeId = 0;
    public string recordID;
    void Start() {
        if(Mode == SimulateMode.Inference) {
            Time.timeScale = TimeScale; // 推論時のみシミュレーションの時間スケールを設定
        }

        if(AccSimulateScale > 1.0f) {
            Debug.LogError("AccSimulateScale is greater than 1.0f. Please set the value between 0.0f and 1.0f.");
        }
        // 日付-時間-分-秒を組み合わせた記録用IDを生成
        recordID = System.DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss");

        NavMesh.pathfindingIterationsPerFrame = 1000000; // パス検索の上限値を設定

        Agent = AgentObj.GetComponent<ShelterManagementAgent>();
        Evacuees = new List<GameObject>(); // 避難者のリストを初期化
        CurrentShelters = new List<GameObject>(); // 避難所のリストを初期化
        Shelters = new List<GameObject>(); // 避難所のリストを初期化
        currentStep = Agent.StepCount;

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

        /** エピソード終了時の処理*/
        OnEndEpisode += OnEndEpisodeHandler;
    }

    void OnDrawGizmos() {
        if(EvacSpawnMode == SpawnMode.Random) {
            Gizmos.color = Color.red;
            DrawWireCircle(spawnCenter, SpawnRadius);
        }
    }

    void FixedUpdate() {
        currentTimeSec += Time.deltaTime;
        EvacuationRate = GetCurrentEvacueeRate();
        evaRatePerSec.Add(new Tuple<float, float>(currentTimeSec, EvacuationRate));
        UpdateUI();
        if (currentTimeSec >= MaxSeconds || IsEvacuatedAll()) { // 制限時間 or 全避難者が避難完了した場合
            OnEndEpisode?.Invoke(EvacuationRate); // 制限時間を超えた場合、エピソード終了のイベントを発火
        }
    }

    private void OnEndEpisodeHandler(float evacuateRate) {
        // 1. 避難率による報酬
        float evacuationRateReward = GetCurrentEvacueeRate();

        // 2. 経過時間によりボーナスを与える
        float timeBonus = (MaxSeconds - currentTimeSec) / MaxSeconds;

        // 総合報酬
        float totalReward = evacuationRateReward + timeBonus;
        Debug.Log("Total Reward: " + totalReward);
        Agent.AddReward(totalReward);

        if(IsRecordData) {
            Utils.SaveResultCSV(
                new string[] { "Time", "EvacuationRate" }, 
                evaRatePerSec, 
                (data) => new string[] { data.Item1.ToString(), data.Item2.ToString() },
                $"{recordID}/EvaRatesPerSec_Episode_{currentEpisodeId}.csv"
            );
        }
        Agent.OnEndEpisode();

        Agent.EndEpisode();
        currentEpisodeId++;
    }

    /// <summary>
    /// エピソード開始時の初期化処理
    /// この関数はエージェントのイベント関数から参照されます 
    /// </summary>
    public void OnEpisodeBegin() {
        EnableEnv = false;
        Dispose();
        Create();
        OnStartEpisode?.Invoke();
        EnableEnv = true;
    }

    /// <summary>
    /// 環境をリセット,破棄をする関数。
    /// - 避難者のクリア
    /// - 避難所のクリア
    /// </summary>
    public void Dispose() {
        foreach (var evacuee in Evacuees) {
            Destroy(evacuee);
        }
        // 避難者スポーン地点の表示を非表示にする
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPos");
        foreach (var spawnPoint in spawnPoints) {
            var point = spawnPoint.GetComponent<EvacueeSpawnPoint>();
            point.ShowRangeOff();
        }
        Evacuees = new List<GameObject>(); // 新しいリストを作成
        CurrentShelters = new List<GameObject>(); // 新しいリストを作成
        currentTimeSec = 0;
        evaRatePerSec.Clear();
    }

    /// <summary>
    /// 環境の生成を行う関数.
    /// - 避難者のスポーン 処理
    /// </summary>
    public void Create() {

        if(Mode == SimulateMode.Train) {
            if(EvacSpawnMode == SpawnMode.Custom) {
                // Custom Spawnエリアの中からランダムに1つ選択し、避難者をスポーンさせ、避難者位置に分布を持たせる
                GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPos");
                GameObject selectSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                var point = selectSpawnPoint.GetComponent<EvacueeSpawnPoint>();
                point.ShowRangeOn();
                float radius = point.SpawnRadius;
                Vector3 spawnCenter = selectSpawnPoint.transform.position;
                Vector3 spawnPos = GetRandomPositionOnNavMesh(radius, spawnCenter);
                for (int i = 0; i < SpawnEvacueeSize; i++) {
                    SpawnEvacuee(spawnPos);
                }
            } else {
                for (int i = 0; i < SpawnEvacueeSize; i++) {
                    Vector3 spawnPos = GetRandomPositionOnNavMesh(SpawnRadius, spawnCenter);
                    if (spawnPos != Vector3.zero) {
                        SpawnEvacuee(spawnPos);
                    }
                }
            }
            

        } else if(Mode == SimulateMode.Inference) {
            if(EvacSpawnMode == SpawnMode.Custom) {
                GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPos");
                foreach (var spawnPoint in spawnPoints) {
                    var point = spawnPoint.GetComponent<EvacueeSpawnPoint>();
                    float radius = point.SpawnRadius;
                    Vector3 spawnCenter = spawnPoint.transform.position;
                    Vector3 spawnPos = GetRandomPositionOnNavMesh(radius, spawnCenter);
                    for (int i = 0; i < point.SpawnSize; i++) {
                        SpawnEvacuee(spawnPos);
                    }
                }
            } else {
                for (int i = 0; i < SpawnEvacueeSize; i++) {
                    Vector3 spawnPos = GetRandomPositionOnNavMesh(SpawnRadius, spawnCenter);
                    if (spawnPos != Vector3.zero) {
                        SpawnEvacuee(spawnPos);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 避難者１体を生成、登録する関数
    /// </summary>
    /// <param name="spawnPos"></param>
    private void SpawnEvacuee(Vector3 spawnPos) {
        GameObject evacuee = Instantiate(SpawnEvacueePref, spawnPos, Quaternion.identity, transform);
        evacuee.tag = "Evacuee";
        Evacuees.Add(evacuee);
    }

    /// <summary>
    /// 範囲内のナビメッシュ上の任意の座標を取得する。
    /// </summary>
    /// <returns>ランダムなナビメッシュ上の座標 or Vector3.zero</returns>
    private static Vector3 GetRandomPositionOnNavMesh(float radius, Vector3 center) {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius; // 半径内のランダムな位置を取得
        randomDirection += center; // 中心位置を加算
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas)) {
            return hit.position;
        }
        return Vector3.zero; // ナビメッシュが見つからなかった場合
    }

    private void UpdateUI() {
        stepCounter.text = $"Remain Seconds : {MaxSeconds - currentTimeSec:F2}";
        evacRateCounter.text = $"Evacuation Rate : {EvacuationRate:F2}";
    }

    /// <summary>
    /// 現在の避難完了率を取得する
    /// </summary>
    /// <returns>現在の避難完了率: 0～1</returns>
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
    private static void DrawWireCircle(Vector3 center, float radius, int segments = 36) {
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


    private bool IsEvacuatedAll() {
        foreach (var evacuee in Evacuees) {
            if (evacuee.activeSelf) {
                return false;
            }
        }
        return true;
    }
}
