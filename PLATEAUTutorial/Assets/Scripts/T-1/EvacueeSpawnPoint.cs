using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// カスタムスポーン用のポイント
/// </summary>
public class EvacueeSpawnPoint : MonoBehaviour {
    
    public GameObject EvacueePrefab;
    public float SpawnRadius = 10f;
    public int SpawnSize = 50;

    void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Utils.DrawWireCircle(transform.position, SpawnRadius);
    }


    public void SpawnEvacuee() {
        Vector3 spawnPos = transform.position + Random.insideUnitSphere * SpawnRadius;
        GameObject evacuee = Instantiate(EvacueePrefab, spawnPos, Quaternion.identity);
        evacuee.transform.parent = transform.parent;
        evacuee.tag = "Evacuee";
    }

}
