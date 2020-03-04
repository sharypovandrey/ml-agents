//Put this script on your blue cube.

using System.Collections;
using System;
using UnityEngine;
using MLAgents;
using Random=UnityEngine.Random;

public class PushAgentBasic : Agent
{
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;

    public GameObject area;

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;

    PushBlockSettings m_PushBlockSettings;

    /// <summary>
    /// The goal to push the block to.
    /// </summary>
    public GameObject goal;

    /// <summary>
    /// The block to be pushed to the goal.
    /// </summary>
    public GameObject block;
    public GameObject block_1;

    /// <summary>
    /// Detects when the block touches the goal.
    /// </summary>

    public bool useVectorObs;

    Rigidbody m_BlockRb;  //cached on initialization
    Rigidbody m_AgentRb;  //cached on initialization
    Material m_GroundMaterial; //cached on Awake()

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    private float area_size_x; 
    private float area_size_z; 

    void Awake()
    {
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        Monitor.SetActive(true);
    }

    public override void InitializeAgent()
    {
        base.InitializeAgent();

        // Cache the agent rigidbody
        m_AgentRb = GetComponent<Rigidbody>();
        // Cache the block rigidbody
        m_BlockRb = block.GetComponent<Rigidbody>();
        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;
        
        area_size_x = areaBounds.size.x;
        area_size_z = areaBounds.size.z;
        
        SetResetParameters();
    }

    public override void CollectObservations()
    {
        // float g_x = Normalize( areaBounds.center.x - goal.GetComponent<Collider>().bounds.center.x, 0, area_size_x / 2); 
        // float g_z = Normalize( areaBounds.center.z - goal.GetComponent<Collider>().bounds.center.z, 0, area_size_z / 2);
        // float a_x = Normalize( areaBounds.center.x - this.GetComponent<Collider>().bounds.center.x, 0, area_size_x / 2); 
        // float a_z = Normalize( areaBounds.center.z - this.GetComponent<Collider>().bounds.center.z, 0, area_size_z / 2);
        float g_x = goal.GetComponent<Collider>().bounds.center.x; 
        float g_z = goal.GetComponent<Collider>().bounds.center.z;
        float a_x = this.GetComponent<Collider>().bounds.center.x; 
        float a_z = this.GetComponent<Collider>().bounds.center.z;        
        float d_x = g_x - a_x;
        float d_z = g_z - a_z;

        // AddVectorObs(g_x);
        // AddVectorObs(g_z);
        // AddVectorObs(a_x);
        // AddVectorObs(a_z);
        AddVectorObs(d_x);
        AddVectorObs(d_z);

        // Debug.Log(string.Format("goal x: {0} z: {1}, agent x: {2}, z: {3}, distance x: {4}, z: {5}", g_x, g_z, a_x, a_z, d_x, d_z));
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    public Vector3 GetRandomSpawnPosForObj(GameObject obj)
    {
        Vector3 p = GetRandomSpawnPos();

        bool canReturn = true;

        foreach (Transform child in area.transform)
        {
            if (child.tag == "block")
            {
                GameObject b = child.gameObject;
                float x_diff = Math.Abs(b.GetComponent<Collider>().bounds.center.x - obj.GetComponent<Collider>().bounds.center.x);
                float z_diff = Math.Abs(b.GetComponent<Collider>().bounds.center.z - obj.GetComponent<Collider>().bounds.center.z);
                Debug.Log(string.Format("dist {0} {1}", x_diff, z_diff ));
                if (x_diff < 2f || z_diff < 2f)
                    canReturn = false;
            }
        }

        if (canReturn)
            return p;
        // Debug.Log("good pos");
        // return p;
        return GetRandomSpawnPosForObj(obj); //Recursive call   
    }

    /// <summary>
    /// Called when the agent moves the block into the goal.
    /// </summary>
    public void ScoredAGoal()
    {
        // We use a reward of 5.
        AddReward(5f);

        // By marking an agent as done AgentReset() will be called automatically.
        Done();

        // Swap ground material for a bit to indicate we scored.
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(float[] act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = Mathf.FloorToInt(act[0]);

        // Goalies and Strikers have slightly different action spaces.
        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void AgentAction(float[] vectorAction)
    {
        // Move the agent using the action.
        MoveAgent(vectorAction);

        // Penalty given each step to encourage agent to finish task quickly.
        AddReward(-1f / maxStep);
    }

    public override float[] Heuristic()
    {
        if (Input.GetKey(KeyCode.D))
        {
            return new float[] { 3 };
        }
        if (Input.GetKey(KeyCode.W))
        {
            return new float[] { 1 };
        }
        if (Input.GetKey(KeyCode.A))
        {
            return new float[] { 4 };
        }
        if (Input.GetKey(KeyCode.S))
        {
            return new float[] { 2 };
        }
        return new float[] { 0 };
    }

    /// <summary>
    /// Resets the block position and velocities.
    /// </summary>
    void ResetBlock()
    {
        // Get a random position for the block.
        block.transform.position = GetRandomSpawnPos();

        // Reset block velocity back to zero.
        m_BlockRb.velocity = Vector3.zero;

        // Reset block angularVelocity back to zero.
        m_BlockRb.angularVelocity = Vector3.zero;
    }

    void AppendBlocks()
    {
        // for (int i = 0; i < 3; i++)
        // {
        //     block_1 = Instantiate(block, block.transform.position, block.transform.rotation, area.transform);
        //     block_1.transform.position = GetRandomSpawnPos();
        // }
    }

    void ResetGoal()
    {
        // Get a random position for the block.
        // goal.transform.position = GetRandomSpawnPosForObj(goal);
        goal.transform.position = GetRandomSpawnPos();
    }

    /// <summary>
    /// In the editor, if "Reset On Done" is checked then AgentReset() will be
    /// called automatically anytime we mark done = true in an agent script.
    /// </summary>
    public override void AgentReset()
    {
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        area.transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        ResetBlock();
        // AppendBlocks();
        ResetGoal();
        // transform.position = GetRandomSpawnPosForObj(this.gameObject);
        transform.position = GetRandomSpawnPos();
        m_AgentRb.velocity = Vector3.zero;
        m_AgentRb.angularVelocity = Vector3.zero;

        SetResetParameters();
    }

    public void SetGroundMaterialFriction()
    {
        var resetParams = Academy.Instance.FloatProperties;

        var groundCollider = ground.GetComponent<Collider>();

        groundCollider.material.dynamicFriction = resetParams.GetPropertyWithDefault("dynamic_friction", 0);
        groundCollider.material.staticFriction = resetParams.GetPropertyWithDefault("static_friction", 0);
    }

    public void SetBlockProperties()
    {
        var resetParams = Academy.Instance.FloatProperties;

        var scale = resetParams.GetPropertyWithDefault("block_scale", 2);
        //Set the scale of the block
        m_BlockRb.transform.localScale = new Vector3(scale, 0.75f, scale);

        // Set the drag of the block
        m_BlockRb.drag = resetParams.GetPropertyWithDefault("block_drag", 0.5f);
    }

    public void SetResetParameters()
    {
        SetGroundMaterialFriction();
        SetBlockProperties();
    }

    void OnCollisionEnter(Collision col)
    {
        // Touched goal.
        if (col.gameObject.CompareTag("goal"))
        {
            ScoredAGoal();
        } else if (col.gameObject.CompareTag("block") || col.gameObject.CompareTag("wall"))
        {
            AddReward(-2.5f);
            Debug.Log("NEGATIVE REWARD!!!");
        }
    }

    float Normalize(float current, float min, float max)
    {
        return (current - min) / (max - min);
    }


}
