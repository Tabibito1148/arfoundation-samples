using System.Collections;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;


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


    public GameObject currentTarget; // El que va a seguir (por ahora poner el publico)
    private ARTouchManager gameManager;
    private Vector3 wanderTaget;
    private float wanderTimer = 0f;
    public float currentSize; // Tamaño actual (publico por el momento para evidenciar crecimiento)
    private Renderer agentRender;

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

        agentRender.material.color = Color.white;

        SetWanderTarget();
    }

    private void Update()
    {
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

        if (wanderTimer <= 0f || Vector3.Distance(transform.position, wanderTaget) < 0.5f)
        {
            SetWanderTarget();
        }
        MoveTowards(wanderTaget);
    }

    void SetWanderTarget()
    {
        Vector2 randomPointInCircle = Random.insideUnitCircle * 3f;
        wanderTaget = transform.position + new Vector3(randomPointInCircle.x, 0, randomPointInCircle.y);
        wanderTimer = Random.Range(2f, 5f);
    }

    void ChaseTarget()
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
            MoveTowards(currentTarget.transform.position);
        }
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.position += direction * moveSpeed * Time.deltaTime;
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
