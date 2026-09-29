using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float forwardSpeed = 8f;
    [SerializeField] private float laneChangeSpeed = 12f;
    [SerializeField] private float maximumForwardSpeed = 20f;

    [Header("Lane Movement")]
    [SerializeField] private float laneReachDistance = 0.02f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [SerializeField] private string runParameter = "IsRunning";

    [SerializeField] private bool controlRunAnimation = true;

    [SerializeField] private string finishDanceTrigger = "FinishDance";

    private bool movementEnabled;
    private bool finishDancing;
    private Transform targetLane;

    private int runParameterHash;
    private bool hasRunParameter;
    private int finishDanceTriggerHash;
    private bool hasFinishDanceTrigger;

    private void Awake()
    {
        movementEnabled = false;

        if (animator != null &&
            !string.IsNullOrEmpty(runParameter))
        {
            runParameterHash =
                Animator.StringToHash(runParameter);

            hasRunParameter =
                HasAnimatorParameter(
                    runParameter
                );
        }

        if (animator != null &&
            !string.IsNullOrEmpty(finishDanceTrigger))
        {
            finishDanceTriggerHash =
                Animator.StringToHash(finishDanceTrigger);

            hasFinishDanceTrigger =
                HasAnimatorTriggerParameter(finishDanceTrigger);
        }
    }

    private void Update()
    {
        if (!movementEnabled)
        {
            SetRunningAnimation(false);
            return;
        }

        MoveForward();
        MoveTowardsLane();

        SetRunningAnimation(true);
    }

    private void MoveForward()
    {
        // Runner moves toward -Z.
        transform.position +=
            Vector3.back *
            forwardSpeed *
            Time.deltaTime;
    }

    private void MoveTowardsLane()
    {
        if (targetLane == null)
            return;

        Vector3 currentPosition =
            transform.position;

        float targetX =
            targetLane.position.x;

        currentPosition.x = Mathf.MoveTowards(
            currentPosition.x,
            targetX,
            laneChangeSpeed *
            Time.deltaTime
        );

        transform.position =
            currentPosition;

        if (Mathf.Abs(
                currentPosition.x - targetX)
            <= laneReachDistance)
        {
            currentPosition.x =
                targetX;

            transform.position =
                currentPosition;
        }
    }

    public void SetLaneTarget(
        Transform lane)
    {
        if (lane == null)
            return;

        targetLane = lane;
    }

    public void SetMovementEnabled(
        bool enabled)
    {
        movementEnabled = enabled;

        SetRunningAnimation(enabled);
    }

    public void StartFinishDancing()
    {
        if (finishDancing)
            return;

        movementEnabled = false;
        finishDancing = true;

        SetRunningAnimation(false);

        if (animator == null)
        {
            Debug.LogWarning(
                "CharacterMovement: Cannot play finish dance because no Animator is assigned."
            );

            return;
        }

        if (!hasFinishDanceTrigger)
        {
            Debug.LogError(
                $"CharacterMovement: Animator is missing the Trigger parameter '{finishDanceTrigger}'."
            );

            return;
        }

        animator.SetTrigger(finishDanceTriggerHash);
    }

    public void ResetForRetry(
        Vector3 position,
        Quaternion rotation)
    {
        movementEnabled = false;
        finishDancing = false;
        transform.SetPositionAndRotation(position, rotation);

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Play("Base Layer.Running", 0, 0f);
            animator.Update(0f);
        }

        SetRunningAnimation(false);
    }

    public void SetForwardSpeed(
        float speed)
    {
        forwardSpeed = Mathf.Clamp(
            speed,
            0f,
            maximumForwardSpeed
        );
    }

    public float GetForwardSpeed()
    {
        return forwardSpeed;
    }

    public bool IsMovementEnabled()
    {
        return movementEnabled;
    }

    private void SetRunningAnimation(
        bool running)
    {
        if (!controlRunAnimation)
            return;

        if (animator == null)
            return;

        if (!hasRunParameter)
            return;

        animator.SetBool(
            runParameterHash,
            running
        );
    }

    private bool HasAnimatorParameter(
        string parameterName)
    {
        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type ==
                AnimatorControllerParameterType.Bool)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnimatorTriggerParameter(
        string parameterName)
    {
        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type ==
                AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }

        return false;
    }
}