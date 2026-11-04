using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunMode : MonoBehaviour
{
    public BattleManager battleManager;
    public gunshotController gunSound;
    public colorChanger colorIndicator;
    public TMP_Text shotsText;
    public Button shootButton;
    public Button swapButton;
    public KeyCode fireKey = KeyCode.Space;
    public KeyCode swapKey = KeyCode.LeftShift;
    public int shotDamage = 1;
    public float shotCooldown = 0.18f;
    public float shakeTime = 0.12f;
    public float shakeAmount = 0.12f;
    public string shootTrigger = "triggerFUCK";

    int selectedSlot;
    int shotsUsed;
    int shotsAllowed;
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
        get { return Mathf.Max(0, shotsAllowed - shotsUsed); }
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
        Combatant.AnyBulletsChanged += OnBulletsChanged;
    }

    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.TurnStarted -= OnTurnStarted;
        }
        Combatant.AnyBulletsChanged -= OnBulletsChanged;
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

        if (shootButton != null)
        {
            shootButton.onClick.AddListener(FireButton);
            AddHover(shootButton, ShootDescription);
        }
        if (swapButton != null)
        {
            swapButton.onClick.AddListener(SwapBullet);
            AddHover(swapButton, SwapDescription);
        }

        UpdateIndicator();
        UpdateText();
    }

    void AddHover(Button button, System.Func<string> provider)
    {
        HoverDescription hover = button.gameObject.GetComponent<HoverDescription>();
        if (hover == null)
        {
            hover = button.gameObject.AddComponent<HoverDescription>();
        }
        hover.getText = provider;
    }

    string DebuffName(BattleColor color)
    {
        if (color == BattleColor.Red)
        {
            return "BLEED";
        }
        if (color == BattleColor.Yellow)
        {
            return "STUN";
        }
        return "SLOW";
    }

    string EffectText(BattleColor color)
    {
        if (color == BattleColor.Red)
        {
            return "Bleed: the enemy takes damage at the start of its turn.";
        }
        if (color == BattleColor.Yellow)
        {
            return "Stun: " + Combatant.Rules.stunHitsRequired + " hits make the enemy skip a turn.";
        }
        return "Slow: the enemy acts less often.";
    }

    string ShootDescription()
    {
        return "SHOOT " + CurrentColor.ToString().ToUpper() + " - " + EffectText(CurrentColor) + " Free action, " + ShotsLeft + " shots left.";
    }

    string SwapDescription()
    {
        return "SWAP - switch between your two bullet colors.";
    }

    void OnTurnStarted(Combatant c)
    {
        selectedSlot = 0;
        shotsUsed = 0;
        shotsAllowed = 0;

        if (c != null && c.isPlayerSide && c.data != null)
        {
            shotsAllowed = c.data.shotsPerTurn + c.bonusShots;
            c.bonusShots = 0;
        }

        UpdateIndicator();
        UpdateText();
    }

    void OnBulletsChanged(Combatant c)
    {
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

    public void FireButton()
    {
        bool canShoot = battleManager.State == BattleState.PlayerTurn && !battleManager.IsTargetingAlly;
        if (canShoot && Time.time >= nextShotTime)
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
        if (shootButton != null)
        {
            shootButton.interactable = ShotsLeft > 0;
        }

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
        string first = (selectedSlot == 0 ? "> " : "   ") + a.ToString().ToUpper() + " (" + DebuffName(a) + ") " + user.BulletsOf(a);
        string second = (selectedSlot == 1 ? "> " : "   ") + b.ToString().ToUpper() + " (" + DebuffName(b) + ") " + user.BulletsOf(b);

        shotsText.text = "SHOTS " + ShotsLeft + "/" + shotsAllowed + "\n" + first + "\n" + second;
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
            Debug.Log(shooter.data.displayName + " has no shots left this turn");
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
        target.ApplyBullet(color);

        int damage = Mathf.Min(shotDamage, target.currentHP - 1);
        if (damage > 0)
        {
            target.TakeDamage(damage);
        }

        UpdateText();

        Debug.Log(shooter.data.displayName + " shot " + target.name + " with " + color + " (" + DebuffName(color) + ", shots left " + ShotsLeft + ")");
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
