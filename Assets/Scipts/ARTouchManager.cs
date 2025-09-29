using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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
    public GameObject readyToSpawn;
    public TextMeshProUGUI sizeText;

    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private AgentControllerWithRB agent;
    private int score = 0;
    private bool agentSpawned = false;
    public float beforeBallSpawn = 0f;

    public Color[] ballColors = {
        Color.red, Color.blue, Color.green, Color.yellow,
        Color.magenta, Color.cyan, Color.white
    };

    private void Update()
    {
        beforeBallSpawn += Time.deltaTime;

        if (beforeBallSpawn >= 1f)
        {
            readyToSpawn.SetActive(true);
        }
        else
        {
            readyToSpawn.SetActive(false);
        }
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                Vector2 touchPosition = touch.position.ReadValue();
                HandleTouch(touchPosition);
            }
        }
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                HandleTouch(mousePosition);
            }
        }
    }

    private void HandleTouch(Vector2 screenPosition)
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

    private void SpawnAgent(Vector3 position)
    {
        GameObject agentObj = Instantiate(agentPrefab, position, Quaternion.identity);
        agent = agentObj.GetComponent<AgentControllerWithRB>();
        agent.Initialize(this);
        agentSpawned = true;

        StartCoroutine(BounceAnimation(agentObj.transform));
    }

    private void SpawnBall(Vector3 position)
    {
        if (beforeBallSpawn >= 1f)
        {
            Vector3 adjustedPosition = position;
            if (arCamera != null)
            {
                adjustedPosition.y = arCamera.transform.position.y;
            }

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

            beforeBallSpawn = 0f;
        }
        else return;

    }

    private IEnumerator BounceAnimation(Transform target)
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

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (sizeText != null && agent != null)
            sizeText.text = "Size: " + agent.GetSize().ToString("F1");


    }
}