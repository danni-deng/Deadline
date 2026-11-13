using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GunMode : MonoBehaviour
{
    public BattleManager battleManager;
    public gunshotController gunSound;
    public colorChanger colorIndicator;
    public Button swapButton;
    public KeyCode swapKey = KeyCode.LeftShift;
    public int buildupPerAttack = 3;
    public bool useAmmo = true;
    public int maxBurst = 6;
    public float burstSpacing = 0.12f;
    public float shakeTime = 0.12f;
    public float shakeAmount = 0.12f;
    public string shootTrigger = "triggerFUCK";

    public event Action SlotChanged;

    int selectedSlot;
    CameraShake cameraShake;
    AudioSource audioSource;

    public BattleColor CurrentColor
    {
        get
        {
            Combatant user = battleManager.ActiveCombatant;
            if (user == null || user.data == null)
            {
                return BattleColor.Red;
            }
            return selectedSlot == 0 ? user.data.bulletColorA : user.data.bulletColorB;
        }
    }

    public int ShotsLeft
    {
        get { return 0; }
    }

    public void FireButton()
    {
    }

    public int BaseBuildup(Combatant c)
    {
        if (c != null && c.data != null && c.data.buildupPerAttack > 0)
        {
            return c.data.buildupPerAttack;
        }
        return buildupPerAttack;
    }

    public int PendingBuildup(Combatant c)
    {
        int bonus = c != null ? c.bonusShots : 0;
        return Mathf.Max(0, BaseBuildup(c) + bonus);
    }

    void Awake()
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            cameraShake = cam.GetComponent<CameraShake>();
        }
    }

    void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.TurnStarted += OnTurnStarted;
            battleManager.ActionPerformed += OnActionPerformed;
            battleManager.AttackResolved += OnAttackResolved;
        }
    }

    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.TurnStarted -= OnTurnStarted;
            battleManager.ActionPerformed -= OnActionPerformed;
            battleManager.AttackResolved -= OnAttackResolved;
        }
    }

    void Start()
    {
        if (gunSound != null)
        {
            audioSource = gunSound.GetComponent<AudioSource>();
            gunSound.enabled = false;
        }

        foreach (Combatant c in battleManager.party)
        {
            if (c == null)
            {
                continue;
            }

            playerController old = c.GetComponentInChildren<playerController>();
            if (old != null)
            {
                old.enabled = false;
            }
        }

        if (swapButton != null)
        {
            swapButton.onClick.AddListener(SwapBullet);
        }

        UpdateIndicator();
    }

    void OnTurnStarted(Combatant c)
    {
        selectedSlot = 0;
        UpdateIndicator();

        if (SlotChanged != null)
        {
            SlotChanged();
        }
    }

    void OnActionPerformed(Combatant user, Combatant target, BattleAction action)
    {
        if (action != BattleAction.Attack || user == null || !user.isPlayerSide)
        {
            return;
        }

        int count = Mathf.Clamp(PendingBuildup(user), 1, Mathf.Max(1, maxBurst));
        StartCoroutine(Burst(user, count));
    }

    IEnumerator Burst(Combatant shooter, int count)
    {
        for (int i = 0; i < count; i++)
        {
            PlayShotEffects(shooter);
            yield return new WaitForSeconds(burstSpacing);
        }
    }

    void OnAttackResolved(Combatant user, Combatant target)
    {
        if (user == null || user.data == null || target == null)
        {
            return;
        }

        int amount = PendingBuildup(user);
        user.bonusShots = 0;

        BattleColor color = CurrentColor;
        int applied = 0;

        for (int i = 0; i < amount; i++)
        {
            if (!target.IsAlive || target.IsResistant)
            {
                break;
            }

            if (useAmmo && !user.UseBullet(color))
            {
                Debug.Log(user.data.displayName + " is out of " + color + " bullets");
                break;
            }

            target.ApplyBullet(color);
            applied++;
        }

        Debug.Log(user.data.displayName + " added " + applied + " " + color + " buildup to " + target.name);
    }

    void Update()
    {
        if (battleManager.State != BattleState.PlayerTurn || battleManager.IsTargetingAlly)
        {
            return;
        }

        if (Input.GetKeyDown(swapKey))
        {
            SwapBullet();
        }
    }

    public void SwapBullet()
    {
        if (battleManager.State != BattleState.PlayerTurn || battleManager.IsTargetingAlly)
        {
            return;
        }

        selectedSlot = 1 - selectedSlot;
        UpdateIndicator();

        if (colorIndicator != null)
        {
            colorIndicator.PlaySwitchSound();
        }

        if (SlotChanged != null)
        {
            SlotChanged();
        }
    }

    void UpdateIndicator()
    {
        if (colorIndicator != null && battleManager.ActiveCombatant != null)
        {
            colorIndicator.SetColor((int)CurrentColor);
        }
    }

    void PlayShotEffects(Combatant shooter)
    {
        Animator animator = shooter.GetComponentInChildren<Animator>();
        if (animator != null && HasTrigger(animator, shootTrigger))
        {
            animator.SetTrigger(shootTrigger);
        }

        if (cameraShake != null)
        {
            cameraShake.shakeDuration = shakeTime;
            cameraShake.shakeAmount = shakeAmount;
            cameraShake.gunshotTime = true;
        }

        if (gunSound != null && audioSource != null)
        {
            AudioClip[] clips = new AudioClip[] { gunSound.gun1, gunSound.gun2, gunSound.gun3, gunSound.gun4 };
            audioSource.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            audioSource.Play();
        }
    }

    bool HasTrigger(Animator animator, string triggerName)
    {
        if (animator.runtimeAnimatorController == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == triggerName && p.type == AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }
        return false;
    }
}