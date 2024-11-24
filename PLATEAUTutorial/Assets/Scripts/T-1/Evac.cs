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
    private List<string> excludeTowers; //1度避難したタワーのUUIDを格納するリスト
    void Start() {
        NavAgent = GetComponent<NavMeshAgent>();    
        excludeTowers = new List<string>(); //初期化
        // 最短距離の避難所を探す
        List<GameObject> towers = SearchTowers();
        if(towers.Count > 0) {
            Target = towers[0]; //最短距離のタワーを目標に設定
            NavAgent.SetDestination(Target.transform.position);
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

        sortedTowerPoints.Sort((a, b) => Vector3.Distance(a.transform.position, transform.position).CompareTo(Vector3.Distance(b.transform.position, transform.position)));
        return sortedTowerPoints;
    }

    /// <summary>
    /// 避難を行う
    /// </summary>
    public void Evacuation(GameObject targetTower) {
        //Towerクラスを取得
        Tower tower = targetTower.GetComponent<Tower>();
        if(tower.currentCapacity > 0) {
            tower.NowAccCount++;
            //isEvacuate = true;
            gameObject.SetActive(false);
        } else { //キャパシティがいっぱいの場合、次のタワーを探す
            excludeTowers.Add(tower.uuid);
            List<GameObject> towers = SearchTowers(excludeTowers);
            if(towers.Count > 0) {
                Target = towers[0]; //最短距離のタワーを目標に設定
            }
        }
    }

}
