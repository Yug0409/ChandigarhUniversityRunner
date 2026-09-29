using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private float scorePerSecond = 10f;
    [SerializeField] private int coinScore = 500;

    [Header("Score UI")]
    [SerializeField] private GameObject scoreBoardUI;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Canvas scoreCanvas;
    [SerializeField] private Vector2 scorePosition = new Vector2(28f, -24f);

    private float currentScore;
    private bool scoring;
    private Text fallbackScoreText;

    public int CurrentScore => Mathf.FloorToInt(currentScore);

    private void Awake()
    {
        if (scoreBoardUI == null && scoreText != null)
        {
            scoreBoardUI = scoreText.gameObject;
        }

        CreateScoreUI();
        UpdateScoreUI();

        if (scoreBoardUI != null)
        {
            scoreBoardUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!scoring)
            return;

        currentScore += scorePerSecond * Time.deltaTime;
        UpdateScoreUI();
    }

    public void StartScoring()
    {
        scoring = true;

        if (scoreBoardUI != null)
        {
            scoreBoardUI.SetActive(true);
        }
    }

    public void StopScoring()
    {
        scoring = false;
    }

    public void AddCoins(int coinCount)
    {
        if (scoring && coinCount > 0)
        {
            currentScore += coinCount * coinScore;
            UpdateScoreUI();
        }
    }

    public void ResetScore()
    {
        scoring = false;
        currentScore = 0f;
        UpdateScoreUI();

        if (scoreBoardUI != null)
        {
            scoreBoardUI.SetActive(false);
        }
    }

    private void CreateScoreUI()
    {
        if (scoreText != null)
            return;

        if (scoreBoardUI == null)
        {
            if (scoreCanvas == null)
                return;

            scoreBoardUI = new GameObject(
                "ScoreBoard",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            RectTransform boardRect =
                scoreBoardUI.GetComponent<RectTransform>();

            boardRect.SetParent(scoreCanvas.transform, false);
            boardRect.anchorMin = new Vector2(0f, 1f);
            boardRect.anchorMax = new Vector2(0f, 1f);
            boardRect.pivot = new Vector2(0f, 1f);
            boardRect.anchoredPosition = scorePosition;
            boardRect.sizeDelta = new Vector2(360f, 80f);

            scoreBoardUI.GetComponent<Image>().color =
                new Color(0.06f, 0.1f, 0.09f, 0.82f);
        }

        GameObject scoreObject = new GameObject(
            "ScoreText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text)
        );

        RectTransform scoreRect = scoreObject.GetComponent<RectTransform>();
        scoreRect.SetParent(scoreBoardUI.transform, false);
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.offsetMin = new Vector2(14f, 8f);
        scoreRect.offsetMax = new Vector2(-14f, -8f);

        fallbackScoreText = scoreObject.GetComponent<Text>();
        fallbackScoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fallbackScoreText.fontSize = 34;
        fallbackScoreText.fontStyle = FontStyle.Bold;
        fallbackScoreText.alignment = TextAnchor.MiddleLeft;
        fallbackScoreText.color = Color.white;
        fallbackScoreText.raycastTarget = false;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = CurrentScore.ToString();
        }

        if (fallbackScoreText != null)
        {
            fallbackScoreText.text = CurrentScore.ToString();
        }
    }
}