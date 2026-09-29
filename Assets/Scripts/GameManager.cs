using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        WaitingToStart,
        Playing,
        GameOver,
        Completed
    }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.WaitingToStart;

    [Header("Main References")]
    [SerializeField] private CharacterMovement player;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private SoundEffectsManager soundEffectsManager;
    [SerializeField] private GameStart gameStart;
    [SerializeField] private Canvas finishCanvas;
    [SerializeField] private GameObject finishUI;

    [Header("Game Over UI")]
    [SerializeField] private GameObject retryUI;

    [Header("Finish Settings")]
    [SerializeField] private float distancePastLastRow = 2f;

    [Header("Lane Centers")]
    [Tooltip("Element 0 = Left, Element 1 = Center, Element 2 = Right")]
    [SerializeField] private Transform[] laneCenters = new Transform[3];

    [Header("Starting Lane")]
    [SerializeField] private int startingLane = 1;

    [Header("Keyboard Input")]
    [SerializeField] private bool useKeyboardInput = true;

    [Header("Joystick Input")]
    [SerializeField, Range(0f, 1f)] private float joystickDeadzone = 0.2f;
    [SerializeField, Range(0f, 1f)] private float joystickPressThreshold = 0.5f;

    private int currentLane;
    private Vector3 initialPlayerPosition;
    private Quaternion initialPlayerRotation;
    private bool joystickInputArmed = true;

    public GameState CurrentState => currentState;
    public int CurrentLane => currentLane;

    private void Awake()
    {
        ValidateSetup();

        currentLane = Mathf.Clamp(
            startingLane,
            0,
            2
        );

        if (player != null)
        {
            initialPlayerPosition = player.transform.position;
            initialPlayerRotation = player.transform.rotation;
        }

        CreateFinishUIIfNeeded();
        CreateRetryUIIfNeeded();
    }

    private void Start()
    {
        PrepareGame();
    }

    private void Update()
    {
        if (currentState != GameState.Playing)
            return;

        if (obstacleSpawner != null && player != null)
        {
            if (obstacleSpawner.HasObstacleCollision(
                    player.transform.position
                ))
            {
                GameOver();
                return;
            }

            int collectedCoins =
                obstacleSpawner.CollectCoinsNearPlayer(
                    player.transform.position
                );

            if (scoreManager != null)
            {
                scoreManager.AddCoins(collectedCoins);
            }

            if (collectedCoins > 0 && soundEffectsManager != null)
            {
                soundEffectsManager.PlayCoinPickup();
            }
        }

        if (player != null &&
            obstacleSpawner != null &&
            obstacleSpawner.HasPassedAllRows(
                player.transform.position,
                distancePastLastRow
            ))
        {
            CompleteLevel();
            return;
        }

        if (useKeyboardInput)
        {
            HandleKeyboardInput();
        }

        HandleJoystickInput();
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null)
            return;

        // LEFT
        if (Keyboard.current.aKey.wasPressedThisFrame ||
            Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            MoveLeft();
        }

        // RIGHT
        if (Keyboard.current.dKey.wasPressedThisFrame ||
            Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            MoveRight();
        }
    }

    private void HandleJoystickInput()
    {
        float horizontalInput = 0f;

        foreach (Gamepad gamepad in Gamepad.all)
        {
            float input = gamepad.leftStick.ReadValue().x;
            if (Mathf.Abs(input) > Mathf.Abs(horizontalInput))
            {
                horizontalInput = input;
            }
        }

        foreach (Joystick joystick in Joystick.all)
        {
            float input = joystick.stick.ReadValue().x;
            if (Mathf.Abs(input) > Mathf.Abs(horizontalInput))
            {
                horizontalInput = input;
            }
        }

        if (Mathf.Abs(horizontalInput) <= joystickDeadzone)
        {
            joystickInputArmed = true;
            return;
        }

        if (!joystickInputArmed)
            return;

        if (horizontalInput <= -joystickPressThreshold)
        {
            MoveLeft();
            joystickInputArmed = false;
        }
        else if (horizontalInput >= joystickPressThreshold)
        {
            MoveRight();
            joystickInputArmed = false;
        }
    }

    private void PrepareGame()
    {
        if (player != null)
        {
            player.SetMovementEnabled(false);

            if (laneCenters.Length == 3 &&
                laneCenters[currentLane] != null)
            {
                player.SetLaneTarget(laneCenters[currentLane]);
            }
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetFollowing(false);
        }

        if (obstacleSpawner != null)
        {
            obstacleSpawner.SetSpawning(false);
        }

    }

    public void RetryGame()
    {
        if (currentState != GameState.GameOver &&
            currentState != GameState.Completed)
            return;

        if (soundEffectsManager != null)
        {
            soundEffectsManager.PlayRetry();
        }

        currentState = GameState.WaitingToStart;
        currentLane = Mathf.Clamp(startingLane, 0, 2);

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        if (obstacleSpawner != null)
        {
            obstacleSpawner.ClearSpawnedObstacles();
        }

        if (player != null)
        {
            player.ResetForRetry(
                initialPlayerPosition,
                initialPlayerRotation
            );

            if (laneCenters != null &&
                laneCenters.Length == 3 &&
                laneCenters[currentLane] != null)
            {
                player.SetLaneTarget(laneCenters[currentLane]);
            }
        }

        if (cameraFollow != null)
        {
            cameraFollow.ResetToStartPosition();
        }

        if (gameStart != null)
        {
            gameStart.ResetForRetry();
        }

        if (retryUI != null)
        {
            retryUI.SetActive(false);
        }

        if (finishUI != null)
        {
            finishUI.SetActive(false);
        }

        PrepareGame();
    }

    public void StartGame()
    {
        if (currentState != GameState.WaitingToStart)
            return;

        currentState = GameState.Playing;

        if (player != null)
        {
            player.SetMovementEnabled(true);
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetFollowing(true);
        }

        if (obstacleSpawner != null)
        {
            obstacleSpawner.SetSpawning(true);
        }

        if (scoreManager != null)
        {
            scoreManager.StartScoring();
        }

        if (soundEffectsManager != null)
        {
            soundEffectsManager.PlayGameStart();
        }
    }

    public void MoveLeft()
    {
        if (currentState != GameState.Playing)
            return;

        if (currentLane <= 0)
            return;

        currentLane--;

        if (soundEffectsManager != null)
        {
            soundEffectsManager.PlayLaneChange();
        }

        if (player != null &&
            laneCenters[currentLane] != null)
        {
            player.SetLaneTarget(
                laneCenters[currentLane]
            );
        }
    }

    public void MoveRight()
    {
        if (currentState != GameState.Playing)
            return;

        if (currentLane >= 2)
            return;

        currentLane++;

        if (soundEffectsManager != null)
        {
            soundEffectsManager.PlayLaneChange();
        }

        if (player != null &&
            laneCenters[currentLane] != null)
        {
            player.SetLaneTarget(
                laneCenters[currentLane]
            );
        }
    }

    public void GameOver()
    {
        if (currentState != GameState.Playing)
            return;

        currentState = GameState.GameOver;

        if (soundEffectsManager != null)
        {
            soundEffectsManager.PlayObstacleHit();
        }

        if (player != null)
        {
            player.SetMovementEnabled(false);
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetFollowing(false);
        }

        if (obstacleSpawner != null)
        {
            obstacleSpawner.SetSpawning(false);
        }

        if (scoreManager != null)
        {
            scoreManager.StopScoring();
        }

        if (retryUI != null)
        {
            retryUI.SetActive(true);
        }
    }

    private void CompleteLevel()
    {
        if (currentState != GameState.Playing)
            return;

        currentState = GameState.Completed;

        if (soundEffectsManager != null)
        {
            soundEffectsManager.PlayFinish();
        }

        if (scoreManager != null)
        {
            scoreManager.StopScoring();
        }

        if (player != null)
        {
            player.SetMovementEnabled(false);
            player.StartFinishDancing();
        }

        if (finishUI != null)
        {
            finishUI.SetActive(true);
        }
    }

    private void CreateFinishUIIfNeeded()
    {
        if (finishUI == null && finishCanvas != null)
        {
            finishUI = new GameObject(
                "FinishUI",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            RectTransform panelRect =
                finishUI.GetComponent<RectTransform>();

            panelRect.SetParent(finishCanvas.transform, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(520f, 180f);

            Image panelImage = finishUI.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.12f, 0.1f, 0.92f);

            GameObject labelObject = new GameObject(
                "FinishLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();

            labelRect.SetParent(panelRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 12f);
            labelRect.offsetMax = new Vector2(-12f, -12f);

            Text label = labelObject.GetComponent<Text>();
            label.text = "FINISH!";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 58;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 24;
            label.resizeTextMaxSize = 58;
        }

        if (finishUI != null)
        {
            CreateFinishRetryButtonIfNeeded();
            finishUI.SetActive(false);
        }
    }

    private void CreateFinishRetryButtonIfNeeded()
    {
        Button retryButton =
            finishUI.GetComponentInChildren<Button>(true);

        if (retryButton == null)
        {
            GameObject buttonObject = new GameObject(
                "FinishRetryButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();

            buttonRect.SetParent(finishUI.transform, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0.2f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.2f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(240f, 64f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.98f, 0.68f, 0.12f);

            retryButton = buttonObject.GetComponent<Button>();
            retryButton.targetGraphic = buttonImage;

            GameObject labelObject = new GameObject(
                "RetryLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();

            labelRect.SetParent(buttonRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.text = "RETRY";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.black;
            label.raycastTarget = false;
        }

        retryButton.onClick.RemoveListener(RetryGame);
        retryButton.onClick.AddListener(RetryGame);
    }

    private void CreateRetryUIIfNeeded()
    {
        if (retryUI == null && finishCanvas != null)
        {
            retryUI = new GameObject(
                "RetryUI",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            RectTransform panelRect =
                retryUI.GetComponent<RectTransform>();

            panelRect.SetParent(finishCanvas.transform, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(520f, 240f);

            retryUI.GetComponent<Image>().color =
                new Color(0.12f, 0.08f, 0.07f, 0.94f);

            GameObject titleObject = new GameObject(
                "GameOverLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

            RectTransform titleRect =
                titleObject.GetComponent<RectTransform>();

            titleRect.SetParent(panelRect, false);
            titleRect.anchorMin = new Vector2(0.05f, 0.48f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Text title = titleObject.GetComponent<Text>();
            title.text = "GAME OVER";
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 48;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;

            GameObject buttonObject = new GameObject(
                "RetryButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();

            buttonRect.SetParent(panelRect, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0.08f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.08f);
            buttonRect.anchoredPosition = new Vector2(0f, 54f);
            buttonRect.sizeDelta = new Vector2(260f, 76f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.98f, 0.68f, 0.12f);

            Button retryButton = buttonObject.GetComponent<Button>();
            retryButton.targetGraphic = buttonImage;
            retryButton.onClick.AddListener(RetryGame);

            GameObject buttonLabelObject = new GameObject(
                "RetryLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

            RectTransform buttonLabelRect =
                buttonLabelObject.GetComponent<RectTransform>();

            buttonLabelRect.SetParent(buttonRect, false);
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = Vector2.zero;
            buttonLabelRect.offsetMax = Vector2.zero;

            Text buttonLabel = buttonLabelObject.GetComponent<Text>();
            buttonLabel.text = "RETRY";
            buttonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonLabel.fontSize = 32;
            buttonLabel.fontStyle = FontStyle.Bold;
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            buttonLabel.color = Color.black;
            buttonLabel.raycastTarget = false;
        }

        if (retryUI != null)
        {
            Button retryButton =
                retryUI.GetComponentInChildren<Button>(true);

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(RetryGame);
                retryButton.onClick.AddListener(RetryGame);
            }

            retryUI.SetActive(false);
        }
    }

    private void ValidateSetup()
    {
        if (laneCenters == null ||
            laneCenters.Length != 3)
        {
            Debug.LogError(
                "GameManager: Lane Centers must contain exactly 3 objects: Left, Center, Right."
            );
        }

        if (player == null)
        {
            Debug.LogError(
                "GameManager: Player reference is missing."
            );
        }
    }
}