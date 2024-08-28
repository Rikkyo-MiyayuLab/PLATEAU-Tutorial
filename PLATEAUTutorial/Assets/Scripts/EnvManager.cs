using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シミュレータ環境全般の制御を行うクラス
/// </summary>
public class EnvManager : MonoBehaviour {

    [Header("Environment Settings")]
    public int MaxSteps;
    public int nowStep;
    public int SpawnEvacueeSize;
    public GameObject SpawnEvacueePref; // 避難者のプレハブ

    [Header("Objects")]
    public List<GameObject> Evacuees; // 避難者のリスト
    public List<GameObject> Shelters; // 現在の避難所のリスト

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


    void Start() {}

    void FixedUpdate() {
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

        OnStartEpisode?.Invoke();
    }

}
