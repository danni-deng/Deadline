using System.Collections;
using UnityEngine;

[System.Serializable]
public class CameraShot
{
    public Transform pose;
    public float fov = 35f;
    public float blendTime = 0.6f;
}

public class BattleCamera : MonoBehaviour
{
    public BattleManager battleManager;
    public Camera battleCamera;
    public CameraShot wideShot;
    public CameraShot playerTurnShot;
    public CameraShot playerActionShot;
    public CameraShot enemyActionShot;
    public CameraShot victoryShot;
    public float hitShakeTime = 0.25f;
    public float hitShakeAmount = 0.25f;
    public float punchAmount = 4f;
    public float punchRecovery = 30f;

    CameraShake cameraShake;
    Coroutine blendRoutine;
    float baseFov;
    float punch;

    void Awake()
    {
        if (battleCamera == null)
        {
            battleCamera = GetComponentInChildren<Camera>();
        }

        cameraShake = battleCamera.GetComponent<CameraShake>();
        baseFov = battleCamera.fieldOfView;
    }

    void OnEnable()
    {
        battleManager.StateChanged += OnStateChanged;
        battleManager.ActionPerformed += OnActionPerformed;
        Combatant.AnyDamaged += OnDamaged;
    }

    void OnDisable()
    {
        battleManager.StateChanged -= OnStateChanged;
        battleManager.ActionPerformed -= OnActionPerformed;
        Combatant.AnyDamaged -= OnDamaged;
    }

    void Start()
    {
        Snap(wideShot);
    }

    void LateUpdate()
    {
        if (!battleCamera.orthographic)
        {
            punch = Mathf.MoveTowards(punch, 0f, punchRecovery * Time.deltaTime);
            battleCamera.fieldOfView = baseFov - punch;
        }
    }

    void OnStateChanged(BattleState state)
    {
        if (state == BattleState.PlayerTurn)
        {
            MoveTo(playerTurnShot);
        }
        else if (state == BattleState.Won)
        {
            MoveTo(victoryShot);
        }
        else if (state == BattleState.Lost)
        {
            MoveTo(wideShot);
        }
    }

    void OnActionPerformed(Combatant user, Combatant target, BattleAction action)
    {
        if (user.isPlayerSide)
        {
            MoveTo(playerActionShot);
        }
        else
        {
            MoveTo(enemyActionShot);
        }
    }

    void OnDamaged(Combatant target, int amount)
    {
        punch = punchAmount;

        if (cameraShake != null)
        {
            cameraShake.shakeDuration = hitShakeTime;
            cameraShake.shakeAmount = hitShakeAmount;
            cameraShake.gunshotTime = true;
        }
    }

    void Snap(CameraShot shot)
    {
        if (shot == null || shot.pose == null)
        {
            return;
        }

        transform.SetPositionAndRotation(shot.pose.position, shot.pose.rotation);
        baseFov = shot.fov;
    }

    void MoveTo(CameraShot shot)
    {
        if (shot == null || shot.pose == null)
        {
            return;
        }

        if (blendRoutine != null)
        {
            StopCoroutine(blendRoutine);
        }
        blendRoutine = StartCoroutine(Blend(shot));
    }

    IEnumerator Blend(CameraShot shot)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startFov = baseFov;
        float duration = Mathf.Max(0.01f, shot.blendTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, shot.pose.position, t);
            transform.rotation = Quaternion.Slerp(startRot, shot.pose.rotation, t);
            baseFov = Mathf.Lerp(startFov, shot.fov, t);
            yield return null;
        }

        transform.SetPositionAndRotation(shot.pose.position, shot.pose.rotation);
        baseFov = shot.fov;
    }
}