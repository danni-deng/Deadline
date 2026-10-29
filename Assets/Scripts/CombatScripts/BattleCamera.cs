using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShotKind
{
    Wide,
    PlayerTurn,
    PlayerAction,
    EnemyAction,
    Victory
}

[System.Serializable]
public class FramedShot
{
    public float pitch = 4f;
    public float yaw = 0f;
    public float fov = 35f;
    public float padding = 2.5f;
    public float blendTime = 0.8f;
}

public class BattleCamera : MonoBehaviour
{
    public BattleManager battleManager;
    public Camera battleCamera;
    public FramedShot wideShot = new FramedShot();
    public FramedShot playerTurnShot = new FramedShot();
    public FramedShot playerActionShot = new FramedShot();
    public FramedShot enemyActionShot = new FramedShot();
    public FramedShot victoryShot = new FramedShot();
    public float subjectHeight = 2.5f;
    public float subjectDepth = 0.5f;
    public float minDistance = 6f;
    public Vector3 centerOffset = Vector3.zero;
    public float hitShakeTime = 0.25f;
    public float hitShakeAmount = 0.25f;
    public float punchAmount = 4f;
    public float punchRecovery = 30f;

    CameraShake cameraShake;
    Coroutine blendRoutine;
    float baseFov;
    float punch;

    void Reset()
    {
        wideShot = MakeShot(4f, 0f, 35f, 2.5f, 0.8f);
        playerTurnShot = MakeShot(4f, 8f, 33f, 1.5f, 0.7f);
        playerActionShot = MakeShot(3f, 4f, 28f, 0.6f, 0.3f);
        enemyActionShot = MakeShot(3f, -6f, 31f, 1.2f, 0.35f);
        victoryShot = MakeShot(2f, 0f, 38f, 2.5f, 1.2f);
    }

    FramedShot MakeShot(float pitch, float yaw, float fov, float padding, float blendTime)
    {
        FramedShot shot = new FramedShot();
        shot.pitch = pitch;
        shot.yaw = yaw;
        shot.fov = fov;
        shot.padding = padding;
        shot.blendTime = blendTime;
        return shot;
    }

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
        battleManager.TargetChanged += OnTargetChanged;
        battleManager.ActionPerformed += OnActionPerformed;
        Combatant.AnyDamaged += OnDamaged;
    }

    void OnDisable()
    {
        battleManager.StateChanged -= OnStateChanged;
        battleManager.TargetChanged -= OnTargetChanged;
        battleManager.ActionPerformed -= OnActionPerformed;
        Combatant.AnyDamaged -= OnDamaged;
    }

    void Start()
    {
        Vector3 position;
        Quaternion rotation;
        ComputePose(wideShot, ShotKind.Wide, null, null, out position, out rotation);
        transform.SetPositionAndRotation(position, rotation);
        baseFov = wideShot.fov;
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
            MoveTo(ShotKind.PlayerTurn, battleManager.ActiveCombatant, battleManager.SelectedTarget);
        }
        else if (state == BattleState.Won)
        {
            MoveTo(ShotKind.Victory, null, null);
        }
        else if (state == BattleState.Lost)
        {
            MoveTo(ShotKind.Wide, null, null);
        }
    }

    void OnTargetChanged(Combatant target)
    {
        if (battleManager.State == BattleState.PlayerTurn)
        {
            MoveTo(ShotKind.PlayerTurn, battleManager.ActiveCombatant, target);
        }
    }

    void OnActionPerformed(Combatant user, Combatant target, BattleAction action)
    {
        if (user.isPlayerSide)
        {
            MoveTo(ShotKind.PlayerAction, user, target);
        }
        else
        {
            MoveTo(ShotKind.EnemyAction, user, target);
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

    FramedShot GetShot(ShotKind kind)
    {
        switch (kind)
        {
            case ShotKind.PlayerTurn:
                return playerTurnShot;
            case ShotKind.PlayerAction:
                return playerActionShot;
            case ShotKind.EnemyAction:
                return enemyActionShot;
            case ShotKind.Victory:
                return victoryShot;
            default:
                return wideShot;
        }
    }

    void AddAlive(List<Vector3> points, List<Combatant> list)
    {
        foreach (Combatant c in list)
        {
            if (c != null && c.IsAlive)
            {
                points.Add(c.transform.position);
            }
        }
    }

    void ComputePose(FramedShot shot, ShotKind kind, Combatant user, Combatant target, out Vector3 position, out Quaternion rotation)
    {
        List<Vector3> points = new List<Vector3>();

        if (kind == ShotKind.Wide || kind == ShotKind.Victory)
        {
            AddAlive(points, battleManager.party);
            AddAlive(points, battleManager.enemies);
        }
        else
        {
            if (user != null)
            {
                points.Add(user.transform.position);
            }
            if (target != null)
            {
                points.Add(target.transform.position);
            }
        }

        if (points.Count == 0)
        {
            points.Add(transform.position + transform.forward * 10f);
        }

        Bounds bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 p in points)
        {
            bounds.Encapsulate(p + Vector3.down * subjectDepth);
            bounds.Encapsulate(p + Vector3.up * subjectHeight);
        }

        Vector3 center = bounds.center + centerOffset;
        rotation = Quaternion.Euler(shot.pitch, shot.yaw, 0f);

        float tanHalf = Mathf.Tan(shot.fov * 0.5f * Mathf.Deg2Rad);
        float distanceH = (bounds.extents.y + shot.padding) / tanHalf;
        float distanceW = (bounds.extents.x + shot.padding) / (tanHalf * battleCamera.aspect);
        float distance = Mathf.Max(minDistance, Mathf.Max(distanceH, distanceW) + bounds.extents.z);

        position = center - rotation * Vector3.forward * distance;
    }

    void MoveTo(ShotKind kind, Combatant user, Combatant target)
    {
        if (blendRoutine != null)
        {
            StopCoroutine(blendRoutine);
        }
        blendRoutine = StartCoroutine(Blend(kind, GetShot(kind), user, target));
    }

    IEnumerator Blend(ShotKind kind, FramedShot shot, Combatant user, Combatant target)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float startFov = baseFov;
        float duration = Mathf.Max(0.01f, shot.blendTime);
        float elapsed = 0f;

        Vector3 endPosition;
        Quaternion endRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ComputePose(shot, kind, user, target, out endPosition, out endRotation);

            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            baseFov = Mathf.Lerp(startFov, shot.fov, t);

            yield return null;
        }

        ComputePose(shot, kind, user, target, out endPosition, out endRotation);
        transform.SetPositionAndRotation(endPosition, endRotation);
        baseFov = shot.fov;
    }
}