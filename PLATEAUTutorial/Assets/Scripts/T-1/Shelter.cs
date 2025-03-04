using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 避難所に関するスクリプト（オブジェクト１台分）
/// 現在の収容人数や、受け入れ可否等のデータを用意
/// </summary>
public class Shelter : MonoBehaviour{
    public int MaxCapacity = 10; //最大収容人数
    public int NowAccCount = 0; //現在の収容人数
    public int currentCapacity; //現在の受け入れ可能人数：最大収容人数 - 現在の収容人数

    public string uuid; //タワーの識別子
    /**Events */
    public delegate void AcceptRejected(int NowAccCount) ; //収容定員が超過した時に発火する
    public AcceptRejected onRejected;

    private EnvManager _env;

    void Start() {
        _env = GetComponentInParent<EnvManager>();
        _env.OnEndEpisode += (float _) => {
            // 環境側のエピソード終了時に収容人数をリセット
            NowAccCount = 0;
        };
    }


    /// <summary>
    /// リアルタイムで収容人数を更新
    /// </summary>
    void Update() {
        currentCapacity = MaxCapacity - NowAccCount;
        if (currentCapacity <= 0) {
            onRejected?.Invoke(NowAccCount);
        }
    }

    /// <summary>
    /// 避難者オブジェクトが建物に到達したときに呼び出される。当たり関数
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other) {
        bool isEvacuee = other.CompareTag("Evacuee");
        if (isEvacuee) {
            Evacuee evacuee = other.GetComponent<Evacuee>();
            evacuee.Evacuation(this);
        }
        
        
    }
}
