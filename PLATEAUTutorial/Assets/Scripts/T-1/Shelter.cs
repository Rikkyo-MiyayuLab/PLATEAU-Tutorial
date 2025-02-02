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

    private string LogPrefix = "shelter: ";

    /**Events */
    public delegate void AcceptRejected(int NowAccCount) ; //収容定員が超過した時に発火する
    public AcceptRejected onRejected;

    private EnvManager _env;
    void Start() {
        _env = GetComponentInParent<EnvManager>();
        _env.OnEndStep += (float _) => {
            NowAccCount = 0;
        };
    }

    void Update() {
        currentCapacity = MaxCapacity - NowAccCount;
        if (currentCapacity <= 0) {
            onRejected?.Invoke(NowAccCount);
        }
    }

    void OnTriggerEnter(Collider other) {
        
        //Debug.Log("OnTriggerEnter Tower");
        bool isEvacuee = other.CompareTag("Evacuee");
        //Debug.Log("isEvacuee?" + isEvacuee);
        if (isEvacuee) {
            Evacuee evacuee = other.GetComponent<Evacuee>();
            evacuee.Evacuation(this);
        }
        
        
    }
}
