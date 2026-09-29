using UnityEngine;
using System;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Follow Offset")]
    [SerializeField] private Vector3 followOffset =
        new Vector3(0f, 5f, 8f);

    [Header("Start Camera")]
    [SerializeField] private Transform startCameraPoint;

    [Header("Runner Direction")]
    [Tooltip("Runner moves toward -Z.")]
    [SerializeField] private Vector3 runnerForwardDirection =
        Vector3.back;

    [Header("Follow Rotation")]
    [SerializeField] private Vector3 followRotationOffset;

    [Header("Camera Movement")]
    [Tooltip("If enabled, camera instantly matches player's lane.")]
    [SerializeField] private bool instantLaneFollow = true;

    private bool following;
    private bool transitioning;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Start()
    {
        following = false;
        transitioning = false;

        if (startCameraPoint != null &&
            startCameraPoint != transform)
        {
            SetStartPosition(startCameraPoint);
        }
    }

    private void LateUpdate()
    {
        if (!following)
            return;

        if (transitioning)
            return;

        if (player == null)
            return;

        FollowPlayer();
    }

    public void SetStartPosition(Transform startPoint)
    {
        if (startPoint == null)
            return;

        transform.position = startPoint.position;
        transform.rotation = startPoint.rotation;
    }

    public void ResetToStartPosition()
    {
        StopAllCoroutines();
        following = false;
        transitioning = false;

        if (startCameraPoint != null &&
            startCameraPoint != transform)
        {
            SetStartPosition(startCameraPoint);
        }
        else
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
    }

    public void SetFollowing(bool value)
    {
        following = value;
    }

    public void BeginFollowing(
        bool transitionToPlayer,
        float transitionDuration,
        Action onCameraReady)
    {
        if (player == null)
        {
            Debug.LogWarning(
                "CameraFollow: Player reference is missing."
            );

            onCameraReady?.Invoke();
            return;
        }

        if (!transitionToPlayer)
        {
            following = true;

            transform.position =
                player.position + followOffset;

            transform.rotation =
                GetForwardRotation();

            onCameraReady?.Invoke();

            return;
        }

        StartCoroutine(
            TransitionToPlayer(
                transitionDuration,
                onCameraReady
            )
        );
    }

    private IEnumerator TransitionToPlayer(
        float duration,
        Action onCameraReady)
    {
        transitioning = true;
        following = false;

        Vector3 startPosition =
            transform.position;

        Quaternion startRotation =
            transform.rotation;

        Vector3 targetPosition =
            player.position + followOffset;

        Quaternion targetRotation =
            GetForwardRotation();

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );

            yield return null;
        }

        transform.position =
            targetPosition;

        transform.rotation =
            targetRotation;

        transitioning = false;
        following = true;

        onCameraReady?.Invoke();
    }

    private void FollowPlayer()
    {
        Vector3 targetPosition =
            player.position + followOffset;

        if (instantLaneFollow)
        {
            // Camera stays exactly aligned with player.
            // No X-axis lag during lane switching.
            transform.position =
                targetPosition;
        }
        else
        {
            transform.position =
                Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    10f * Time.deltaTime
                );
        }

        // Camera rotation is completely independent
        // of player's lane.
        transform.rotation =
            GetForwardRotation();
    }

    private Quaternion GetForwardRotation()
    {
        Vector3 direction =
            runnerForwardDirection.normalized;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.back;
        }

        return Quaternion.LookRotation(
            direction,
            Vector3.up
        ) *
        Quaternion.Euler(
            followRotationOffset
        );
    }
}