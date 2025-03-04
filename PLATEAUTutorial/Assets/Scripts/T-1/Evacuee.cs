using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 避難者の制御を行うクラス
/// </summary>
public class Evacuee : MonoBehaviour {
    
    [Header("Movement Target")]
    public GameObject Target; // 現在の移動目標
    private NavMeshAgent NavAgent; // NavMeshAgentコンポーネント
    private EnvManager _env; // ShelterEnvManagerの参照
    private bool isEvacuating = false; // 避難処理中のフラグ。当たり判定により発火するため、複数回避難処理が行われるのを防ぐためのフラグ
    private List<string> excludeShelters; //1度避難したタワーのUUIDを格納するリスト
    void Awake() {
        NavAgent = GetComponent<NavMeshAgent>();    
        excludeShelters = new List<string>(); 

        _env = GetComponentInParent<EnvManager>();
        _env.Agent.OnDidActioned += () => {
            // エージェントが建物を選択したことを検知して最短距離の避難所を探す
            if(this != null && this.gameObject.activeSelf) {
                List<GameObject> towers = SearchShelters();
                if(towers.Count > 0) {
                    Target = towers[0]; //最短距離のタワーを目標に設定
                    NavAgent.SetDestination(Target.transform.position);
                }
            }
        };
    }


    
    /// <summary>
    /// タグ名から避難所を検索する。フィールドに存在する全てのタワーを検索し、距離別にソートして返す
    /// </summary>
    /// <param name="excludeTowerUUIDs">除外するタワーのUUID.未指定の場合はnull</param>
    /// <returns>localField内のTowerオブジェクトのリスト</returns>
    private List<GameObject> SearchShelters(List<string> excludeTowerUUIDs = null) {
        // タグ名から避難所を検索する
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Shelter");
        GameObject[] constShelters = GameObject.FindGameObjectsWithTag("ConstShelter");
        List<GameObject> Iterates = new List<GameObject>();
        foreach (var shelter in towers) {
            Iterates.Add(shelter);
        }
        foreach (var shelter in constShelters) {
            Iterates.Add(shelter);
        }
        // 訪れたことのない避難所を探す
        List<GameObject> sortedTowerPoints = new List<GameObject>();
        foreach (var tower in Iterates) {
            if(excludeTowerUUIDs != null && excludeTowerUUIDs.Contains(tower.GetComponent<Shelter>().uuid)) {
                continue;
            }
            GameObject point = tower.transform.GetChild(0).gameObject; // 避難所に設置した目印オブジェクトを取得
            sortedTowerPoints.Add(point);
        }
        // NOTE: エピソード更新時にgameObjectがnullになることがあるので、nullチェックを行う
        if(this != null) {
            // 距離別にソート
            sortedTowerPoints.Sort((a, b) => Vector3.Distance(a.transform.position, transform.position).CompareTo(Vector3.Distance(b.transform.position, transform.position))); 
        }
        return sortedTowerPoints;
    }

    /// <summary>
    /// 避難を行う処理
    /// 避難所のオブジェクトにアタッチされ、当たり判定により呼び出される 
    /// </summary>
    public void Evacuation(Shelter shelter) {
        if(isEvacuating) {
            return;
        }
        isEvacuating = true;
        // キャパシティーがある場合、避難処理を行う
        if(shelter.currentCapacity > 0) {
            shelter.NowAccCount++;
            gameObject.SetActive(false);
        } else { //キャパシティがいっぱいの場合、次の避難所を探す
            excludeShelters.Add(shelter.uuid);
            List<GameObject> shelters = SearchShelters(excludeShelters);
            if(shelters.Count > 0) {
                Target = shelters[0]; //最短距離のタワーを目標に設定
                NavAgent.SetDestination(Target.transform.position);
            }
        }
        isEvacuating = false;
    }

}
