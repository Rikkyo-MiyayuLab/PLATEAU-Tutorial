using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 避難者の制御を行うクラス
/// </summary>
public class Evacuee : MonoBehaviour {
    
    [Header("Movement Target")]
    public GameObject Target;
    private NavMeshAgent NavAgent;
    private EnvManager _env;
    private bool isEvacuating = false; // 避難処理中のフラグ。当たり判定により発火するため、複数回避難処理が行われるのを防ぐためのフラグ
    private List<string> excludeTowers; //1度避難したタワーのUUIDを格納するリスト
    void Awake() {
        NavAgent = GetComponent<NavMeshAgent>();    
        excludeTowers = new List<string>(); //初期化

        _env = GetComponentInParent<EnvManager>();
        _env.Agent.OnDidActioned += () => {
            Debug.Log("OnDidActioned");
            // 最短距離の避難所を探す
            if(this != null && this.gameObject.activeSelf) {
                List<GameObject> towers = SearchTowers();
                if(towers.Count > 0) {
                    Target = towers[0]; //最短距離のタワーを目標に設定
                    NavAgent.SetDestination(Target.transform.position);
                }
            }
        };
    }


    private void FixedUpdate() {
        
        if(this != null && this.gameObject.activeSelf && NavAgent.remainingDistance < 1.0f) {
            /*
            Debug.Log("Reached" + NavAgent.remainingDistance);
            Tower tower = Target.GetComponentInParent<Tower>();
            Evacuation(tower);
            */
        }
        
    }



    /// <summary>
    /// タグ名から避難所を検索する。フィールドに存在する全てのタワーを検索し、距離別にソートして返す
    /// </summary>
    /// <param name="excludeTowerUUIDs">除外するタワーのUUID.未指定の場合はnull</param>
    /// <returns>localField内のTowerオブジェクトのリスト</returns>
    private List<GameObject> SearchTowers(List<string> excludeTowerUUIDs = null) {
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Shelter");
        List<GameObject> sortedTowerPoints = new List<GameObject>();
        foreach (var tower in towers) {
            if(excludeTowerUUIDs != null && excludeTowerUUIDs.Contains(tower.GetComponent<Tower>().uuid)) {
                continue;
            }
            GameObject point = tower.transform.GetChild(0).gameObject;
            sortedTowerPoints.Add(point);
        }
        // NOTE: エピソード更新時にgameObjectがnullになることがあるので、nullチェックを行う
        if(this != null) {
            sortedTowerPoints.Sort((a, b) => Vector3.Distance(a.transform.position, transform.position).CompareTo(Vector3.Distance(b.transform.position, transform.position))); 
        }
        return sortedTowerPoints;
    }

    /// <summary>
    /// 避難を行う
    /// </summary>
    public void Evacuation(Tower tower) {
        if(isEvacuating) {
            return;
        }
        isEvacuating = true;
        if(tower.currentCapacity > 0) {
            tower.NowAccCount++;
            //isEvacuate = true;
            gameObject.SetActive(false);
        } else { //キャパシティがいっぱいの場合、次のタワーを探す
            excludeTowers.Add(tower.uuid);
            List<GameObject> towers = SearchTowers(excludeTowers); // 次のタワーを探さないバグあり。
            Debug.Log("TowersCount" + towers.Count);
            if(towers.Count > 0) {
                Target = towers[0]; //最短距離のタワーを目標に設定
                NavAgent.SetDestination(Target.transform.position);
            }
        }
        isEvacuating = false;
    }

}
