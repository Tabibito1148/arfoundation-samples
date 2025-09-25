using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NavMeshAgent))]
public class AgentController : MonoBehaviour
{
    [Header("Movement")]
    [Space(10)]
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float rotationSpeed = 5f;

    [Header("Agent Related")]
    [Space(10)]
    [SerializeField] float baseSize = 1f;
    [SerializeField] float sizeToIncrement = 0.1f;

    public GameObject currentTarget;
    private ARTouchManager gameManager;
    private Vector3 wanderTarget;
    private float wanderTimer = 0f;
    public float currentSize;
    private Renderer agentRender;

    // ← NUEVO: Referencias NavMesh
    private NavMeshAgent navAgent;
    private bool navMeshReady = false;

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

        // ← NUEVO: Configurar NavMeshAgent
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = rotationSpeed * 57.3f; // Convertir a grados
        navAgent.radius = 0.5f;
        navAgent.height = 2f;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Desactivar NavMesh hasta que esté listo
        navAgent.enabled = false;

        agentRender.material.color = Color.white;

        // Esperar un poco para que NavMesh se genere
        StartCoroutine(WaitForNavMesh());
    }

    // ← NUEVO: Esperar a que NavMesh esté listo
    IEnumerator WaitForNavMesh()
    {
        yield return new WaitForSeconds(1f); // Esperar un segundo

        // Intentar activar NavMesh
        if (TryActivateNavMesh())
        {
            SetWanderTarget();
        }
        else
        {
            // Si no está listo, intentar cada medio segundo
            StartCoroutine(RetryNavMeshActivation());
        }
    }

    IEnumerator RetryNavMeshActivation()
    {
        while (!navMeshReady)
        {
            yield return new WaitForSeconds(0.5f);
            if (TryActivateNavMesh())
            {
                SetWanderTarget();
                break;
            }
        }
    }

    bool TryActivateNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
        {
            navAgent.enabled = true;
            navMeshReady = true;
            return true;
        }
        return false;
    }

    private void Update()
    {
        // Solo actuar si NavMesh está listo
        if (!navMeshReady) return;

        switch (currentState)
        {
            case AgentState.Wandering:
                Wander();
                break;

            case AgentState.Chasing:
                ChaseTarget();
                break;
            case AgentState.Eating:
                // Pausa
                break;
        }
    }

    void Wander()
    {
        wanderTimer -= Time.deltaTime;

        // ← CAMBIADO: Usar NavMesh para verificar si llegó
        if (wanderTimer <= 0f || !navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            SetWanderTarget();
        }
    }

    void SetWanderTarget()
    {
        Vector2 randomPointInCircle = Random.insideUnitCircle * 3f;
        Vector3 potentialTarget = transform.position + new Vector3(randomPointInCircle.x, 0, randomPointInCircle.y);

        // ← CAMBIADO: Verificar que el punto esté en NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(potentialTarget, out hit, 5f, NavMesh.AllAreas))
        {
            wanderTarget = hit.position;
            navAgent.SetDestination(wanderTarget);
        }

        wanderTimer = Random.Range(2f, 5f);
    }

    void ChaseTarget()
    {
        if (currentTarget == null)
        {
            currentState = AgentState.Wandering;
            return;
        }

        // ← CAMBIADO: Usar NavMesh distance
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance < 0.5f)
        {
            EatBall();
        }
        else
        {
            // ← CAMBIADO: Usar NavMesh para moverse
            navAgent.SetDestination(currentTarget.transform.position);
        }
    }

    // ← ELIMINADO: MoveTowards ya no se necesita, NavMesh lo hace automáticamente

    public void SetTarget(GameObject target)
    {
        currentTarget = target;
        currentState = AgentState.Chasing;
    }

    void EatBall()
    {
        if (currentTarget == null) return;

        currentState = AgentState.Eating;

        // ← NUEVO: Detener NavMesh mientras come
        navAgent.ResetPath();

        Ball ballScript = currentTarget.GetComponent<Ball>();
        Color ballColor = ballScript.GetColor();

        StartCoroutine(ChangeColorAnimation(ballColor));

        currentSize += sizeToIncrement;
        transform.localScale = Vector3.one * currentSize;

        // ← NUEVO: Actualizar NavMesh agent size
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