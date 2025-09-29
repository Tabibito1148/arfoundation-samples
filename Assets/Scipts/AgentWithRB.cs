using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class AgentControllerWithRB : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float angularSpeed = 120f;

    [Header("Agent Related")]
    [SerializeField] float baseSize = 1f;
    [SerializeField] float sizeToIncrement = 0.1f;

    [Header("Animation")]
    [SerializeField] float colorChangeSpeed = 2f;

    private const float DESTINATION_THRESHOLD = 0.5f;
    private const float WANDER_RADIUS = 3f;

    public GameObject currentTarget { get; private set; }
    private ARTouchManager gameManager;
    private float wanderTimer = 0f;
    private Renderer agentRender;
    private Rigidbody rb;

    public float CurrentSize => currentSize;
    private float currentSize;

    private Vector3 currentDestination;
    private bool hasDestination = false;

    private enum AgentState
    {
        Wandering,
        Chasing,
        Eating
    }

    private AgentState currentState = AgentState.Wandering;

    private void Awake()
    {
        agentRender = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();

        if (agentRender == null)
            Debug.LogError("Renderer no encontrado en el agente.");
        if (rb == null)
            Debug.LogError("Rigidbody no encontrado en el agente.");
    }

    public void Initialize(ARTouchManager touchManager)
    {
        gameManager = touchManager;
        currentSize = baseSize;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        if (agentRender != null)
            agentRender.material.color = Color.white;

        SetWanderTarget();
    }

    private void Update()
    {
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
                rb.linearVelocity = Vector3.zero;
                break;
        }
    }

    private void HandleWandering()
    {
        wanderTimer -= Time.deltaTime;

        if (!hasDestination || wanderTimer <= 0f || HasReachedDestination())
        {
            SetWanderTarget();
        }
        else
        {
            MoveTowards(currentDestination);
        }
    }

    private bool HasReachedDestination()
    {
        return Vector3.Distance(transform.position, currentDestination) < DESTINATION_THRESHOLD;
    }

    private void SetWanderTarget()
    {
        Vector2 randomPointInCircle = Random.insideUnitCircle * WANDER_RADIUS;
        currentDestination = transform.position + new Vector3(randomPointInCircle.x, 0, randomPointInCircle.y);
        hasDestination = true;
        wanderTimer = Random.Range(2f, 5f);
    }

    private void HandleChasing()
    {
        if (currentTarget == null)
        {
            currentState = AgentState.Wandering;
            hasDestination = false;
            return;
        }

        Vector3 targetPos = currentTarget.transform.position;
        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance < DESTINATION_THRESHOLD)
        {
            EatBall();
        }
        else
        {
            MoveTowards(targetPos);
        }
    }

    private void MoveTowards(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
        }
    }

    public void SetTarget(GameObject target)
    {
        currentTarget = target;
        currentState = AgentState.Chasing;
        hasDestination = false;
    }

    private void EatBall()
    {
        if (currentTarget == null) return;

        currentState = AgentState.Eating;
        rb.linearVelocity = Vector3.zero;

        Ball ballScript = currentTarget.GetComponent<Ball>();
        Color ballColor = ballScript.GetColor();

        StartCoroutine(ChangeColorAnimation(ballColor));

        currentSize += sizeToIncrement;
        transform.localScale = Vector3.one * currentSize;

        gameManager.AddScore(10);
        StartCoroutine(DestroyBallEffect(currentTarget));

        currentTarget = null;
        StartCoroutine(ReturnToWandering());
    }

    private IEnumerator ChangeColorAnimation(Color targetColor)
    {
        if (agentRender == null) yield break;

        Color startColor = agentRender.material.color;
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * colorChangeSpeed;
            agentRender.material.color = Color.Lerp(startColor, targetColor, time);
            yield return null;
        }
    }

    private IEnumerator DestroyBallEffect(GameObject ball)
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

    private IEnumerator ReturnToWandering()
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
