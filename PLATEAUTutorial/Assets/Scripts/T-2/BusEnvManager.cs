using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class BusEnvManager : MonoBehaviour {

    public List<GameObject> Agents = new List<GameObject>();
    public List<GameObject> BusStops = new List<GameObject>();

    void Start() {
        BusStops = new List<GameObject>(GameObject.FindGameObjectsWithTag("BusStop"));
        Agents = new List<GameObject>(GameObject.FindGameObjectsWithTag("Agent"));
    }
    
}