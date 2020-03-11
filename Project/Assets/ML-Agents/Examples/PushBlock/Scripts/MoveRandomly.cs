using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class MoveRandomly : MonoBehaviour {
    
    NavMeshAgent navMeshAgent;  
    public float timeForNewPath;
    bool inCoRoutine;  
    void Start ()  
    {
    navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
      if(!inCoRoutine)
        StartCoroutine(DoSomething());   
    }

    Vector3 getNewRandomPosition ()
    {
        float x = Random.Range(-20f, 20f);
        float z = Random.Range(-20f, 20f);

        Vector3 pos = new Vector3(x, 0, z);
        return pos;
    }

    IEnumerator DoSomething ()
    {
        inCoRoutine = true;
        yield return new WaitForSeconds(timeForNewPath);
        GetNewPath();
        inCoRoutine = false;
    }

    void GetNewPath ()
    {
        navMeshAgent.SetDestination(getNewRandomPosition());
    }
}