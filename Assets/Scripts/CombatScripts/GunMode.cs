using TMPro;
using UnityEngine;

public class GunMode : MonoBehaviour
{
    public BattleManager battleManager;
    public gunshotController gunSound;
    public colorChanger colorIndicator;
    public TMP_Text shotsText;
    public KeyCode fireKey = KeyCode.Space;
    public KeyCode swapKey = KeyCode.LeftShift;
    public int shotDamage = 2;
    public float matchDamageMultiplier = 2f;
    public float shotCooldown = 0.4f;
    public float shakeTime = 0.15f;
    public float shakeAmount = 0.15f;
    public string shootTrigger = "triggerFUCK";

    int selectedSlot;
    int shotsUsed;
    float nextShotTime;
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
        get
        {
            Combatant user = battleManager.ActiveCombatant;
            if (user == null || user.data == null)
            {
                return 0;
            }
            return Mathf.Max(0, user.data.shotsPerTurn - shotsUsed);
        }
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
        }
    }

    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.TurnStarted -= OnTurnStarted;
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

        UpdateIndicator();
        UpdateText();
    }

    void OnTurnStarted(Combatant c)
    {
        selectedSlot = 0;
        shotsUsed = 0;
        UpdateIndicator();
        UpdateText();
    }

    void Update()
    {
        bool canShoot = battleManager.State == BattleState.PlayerTurn && !battleManager.IsTargetingAlly;
        if (!canShoot)
        {
            return;
        }

        if (Input.GetKeyDown(swapKey))
        {
            SwapBullet();
        }
        if (Input.GetKeyDown(fireKey) && Time.time >= nextShotTime)
        {
            Fire();
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
        UpdateText();

        if (colorIndicator != null)
        {
            colorIndicator.PlaySwitchSound();
        }
    }

    void UpdateIndicator()
    {
        if (colorIndicator != null && battleManager.ActiveCombatant != null)
        {
            colorIndicator.SetColor((int)CurrentColor);
        }
    }

    void UpdateText()
    {
        if (shotsText == null)
        {
            return;
        }

        Combatant user = battleManager.ActiveCombatant;
        if (user == null || user.data == null || !user.isPlayerSide)
        {
            shotsText.text = "";
            return;
        }

        BattleColor a = user.data.bulletColorA;
        BattleColor b = user.data.bulletColorB;
        string first = (selectedSlot == 0 ? "> " : "   ") + a.ToString().ToUpper() + " " + user.BulletsOf(a);
        string second = (selectedSlot == 1 ? "> " : "   ") + b.ToString().ToUpper() + " " + user.BulletsOf(b);

        shotsText.text = "SHOTS " + ShotsLeft + "/" + user.data.shotsPerTurn + "\n" + first + "\n" + second;
    }

    void Fire()
    {
        Combatant shooter = battleManager.ActiveCombatant;
        Combatant target = battleManager.SelectedTarget;

        if (shooter == null || shooter.data == null || target == null || !target.IsAlive)
        {
            return;
        }

        if (ShotsLeft <= 0)
        {
            Debug.Log(shooter.data.displayName + " has no shots left this turn (max " + shooter.data.shotsPerTurn + ")");
            return;
        }

        BattleColor color = CurrentColor;

        if (!shooter.UseBullet(color))
        {
            Debug.Log(shooter.data.displayName + " is out of " + color + " bullets");
            return;
        }

        shotsUsed++;
        nextShotTime = Time.time + shotCooldown;

        PlayShotEffects(shooter);

        bool matched = target.ApplyShot(color, shooter);

        int damage = shotDamage;
        if (matched)
        {
            damage = Mathf.RoundToInt(shotDamage * matchDamageMultiplier);
        }
        damage = Mathf.Min(damage, target.currentHP - 1);

        if (damage > 0)
        {
            target.TakeDamage(damage);
        }

        UpdateText();

        Debug.Log(shooter.data.displayName + " shot " + target.name + " with " + color + " (inking " + target.colorProgress[(int)color] + "/" + target.shotsToColor + ", " + color + " bullets left " + shooter.BulletsOf(color) + ", shots left this turn " + ShotsLeft + ")");
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
            audioSource.clip = clips[Random.Range(0, clips.Length)];
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
