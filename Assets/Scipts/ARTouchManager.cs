using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARTouchManager : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Prefabs")]
    public GameObject ballPrefab;
    public GameObject agentPrefab;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI sizeText;

    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private AgentController agent;
    private int score = 0;
    private bool agentSpawned = false;

    private Color[] ballColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan, Color.white };

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                HandleTouch(touch.position);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleTouch(Input.mousePosition);
        }
    }

    void HandleTouch(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = raycastHits[0].pose;

            if (!agentSpawned)
            {
                SpawnAgent(hitPose.position);
            }
            else
            {
                SpawnBall(hitPose.position);
            }
        }
    }

    void SpawnAgent(Vector3 position)
    {
        GameObject agentObj = Instantiate(agentPrefab, position, Quaternion.identity);
        agent = agentObj.GetComponent<AgentController>();
        agent.Initialize(this);
        agentSpawned = true;

        StartCoroutine(BounceAnimation(agentObj.transform));
    }

    void SpawnBall(Vector3 position)
    {
        GameObject ball = Instantiate(ballPrefab, position, Quaternion.identity);

        Color randomColor = ballColors[Random.Range(0, ballColors.Length)];
        ball.GetComponent<Renderer>().material.color = randomColor;

        Ball ballScript = ball.GetComponent<Ball>();
        ballScript.Initialize(randomColor);

        if (agent != null)
        {
            agent.SetTarget(ball);
        }
        StartCoroutine(BounceAnimation(ball.transform));
    }

    IEnumerator BounceAnimation(Transform target)
    {
        Vector3 originalScale = target.localScale;
        target.localScale = Vector3.zero;

        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime * 3f;
            float bounce = Mathf.Sin(time * Mathf.PI);
            target.localScale = originalScale * bounce;
            yield return null;
        }

        target.localScale = originalScale;
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }

    void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        if (agent != null)
        {
            sizeText.text = "Size: " + agent.GetSize().ToString("F1");
        }
    }
}
