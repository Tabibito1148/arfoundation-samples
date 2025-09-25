using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NavMeshAgent))]
public class AgentController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float angularSpeed = 120f;

    [Header("Agent Related")]
    [SerializeField] float baseSize = 1f;
    [SerializeField] float sizeToIncrement = 0.1f;

    public GameObject currentTarget;
    private ARTouchManager gameManager;
    private float wanderTimer = 0f;
    public float currentSize;
    private Renderer agentRender;
    private NavMeshAgent navAgent;

    private enum AgentState
    {
        Wandering,
        Chasing,
        Eating
    }

    private AgentState currentState = AgentState.Wandering;

    public void Initialize(ARTouchManager touchManager)
    {
        gameManager = touchManager;
        currentSize = baseSize;
        agentRender = GetComponent<Renderer>();

        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = angularSpeed;
        navAgent.radius = 0.5f;
        navAgent.height = 2f;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        navAgent.enabled = false;

        agentRender.material.color = Color.white;

        StartCoroutine(WaitForNavMesh());
    }

    IEnumerator WaitForNavMesh()
    {
        yield return new WaitForSeconds(1f);

        while (!TryActivateNavMesh())
        {
            yield return new WaitForSeconds(0.5f);
        }

        SetWanderTarget();
    }

    bool TryActivateNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
        {
            navAgent.enabled = true;
            return true;
        }
        return false;
    }

    private void Update()
    {
        if (!navAgent.enabled) return;

        switch (currentState)
        {
            case AgentState.Wandering:
                HandleWandering();
                break;

            case AgentState.Chasing:
                HandleChasing();
                break;
            case AgentState.Eating:
                // Pausa
                break;
        }
    }

    void HandleWandering()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f || HasReachedDestination())
        {
            SetWanderTarget();
        }
    }

    bool HasReachedDestination()
    {
        return !navAgent.pathPending && navAgent.remainingDistance < 0.5f;
    }

    void SetWanderTarget()
    {
        Vector2 randomPointInCircle = Random.insideUnitCircle * 3f;
        Vector3 potentialTarget = transform.position + new Vector3(randomPointInCircle.x, 0, randomPointInCircle.y);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(potentialTarget, out hit, 5f, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
            wanderTimer = Random.Range(2f, 5f);
        }
    }

    void HandleChasing()
    {
        if (currentTarget == null)
        {
            currentState = AgentState.Wandering;
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance < 0.5f)
        {
            EatBall();
        }
        else
        {
            navAgent.SetDestination(currentTarget.transform.position);
        }
    }

    public void SetTarget(GameObject target)
    {
        currentTarget = target;
        currentState = AgentState.Chasing;
    }

    void EatBall()
    {
        if (currentTarget == null) return;

        currentState = AgentState.Eating;
        navAgent.ResetPath();

        Ball ballScript = currentTarget.GetComponent<Ball>();
        Color ballColor = ballScript.GetColor();

        StartCoroutine(ChangeColorAnimation(ballColor));

        currentSize += sizeToIncrement;
        transform.localScale = Vector3.one * currentSize;
        navAgent.radius = 0.5f * currentSize;

        gameManager.AddScore(10);
        StartCoroutine(DestroyBallEffect(currentTarget));

        currentTarget = null;
        StartCoroutine(ReturnToWandering());
    }

    IEnumerator ChangeColorAnimation(Color targetColor)
    {
        Color startColor = agentRender.material.color;
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * 2f;
            agentRender.material.color = Color.Lerp(startColor, targetColor, time);
            yield return null;
        }
    }

    IEnumerator DestroyBallEffect(GameObject ball)
    {
        Vector3 originalScale = ball.transform.localScale;
        float time = 0f;

        while (time < 0.5f)
        {
            time += Time.deltaTime * 4f;
            ball.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, time);
            ball.transform.position = Vector3.Lerp(ball.transform.position, transform.position, time);
            yield return null;
        }
        Destroy(ball);
    }

    IEnumerator ReturnToWandering()
    {
        yield return new WaitForSeconds(0.5f);
        currentState = AgentState.Wandering;
        SetWanderTarget();
    }

    public float GetSize()
    {
        return currentSize;
    }
}