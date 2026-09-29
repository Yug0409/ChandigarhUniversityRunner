using UnityEngine;

public class GameStart : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private SoundEffectsManager soundEffectsManager;
    [SerializeField] private GameObject startButtonUI;

    [Header("Start Settings")]
    [SerializeField] private bool transitionCameraToPlayer = true;

    [SerializeField] private float cameraTransitionDuration = 1.5f;

    private bool gameStarted;

    private void Start()
    {
        gameStarted = false;
    }

    public void PressPlay()
    {
        if (gameStarted)
            return;

        gameStarted = true;

        if (soundEffectsManager != null)
        {
            soundEffectsManager.PlayButtonClick();
        }

        if (startButtonUI != null)
        {
            startButtonUI.SetActive(false);
        }

        // Camera transition happens first.
        if (cameraFollow != null)
        {
            cameraFollow.BeginFollowing(
                transitionCameraToPlayer,
                cameraTransitionDuration,
                OnCameraReady
            );
        }
        else
        {
            // Safety fallback if no camera is assigned.
            OnCameraReady();
        }
    }

    public void ResetForRetry()
    {
        gameStarted = false;

        if (startButtonUI != null)
        {
            startButtonUI.SetActive(true);
        }
    }

    private void OnCameraReady()
    {
        if (gameManager != null)
        {
            gameManager.StartGame();
        }
    }
}