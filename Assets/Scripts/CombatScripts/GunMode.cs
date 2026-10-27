using UnityEngine;

public class GunMode : MonoBehaviour
{
    public BattleManager battleManager;
    public colorChanger colorSource;
    public KeyCode fireKey = KeyCode.Space;
    public int shotDamage = 2;
    public float matchDamageMultiplier = 2f;
    public float shotCooldown = 0.4f;
    public bool restrictToCharacterColors = true;

    float nextShotTime;

    void Update()
    {
        if (battleManager.State != BattleState.PlayerTurn)
        {
            return;
        }

        if (Input.GetKeyDown(fireKey) && Time.time >= nextShotTime)
        {
            Fire();
        }
    }

    void Fire()
    {
        Combatant shooter = battleManager.ActiveCombatant;
        Combatant target = battleManager.SelectedTarget;

        if (shooter == null || target == null || !target.IsAlive)
        {
            return;
        }

        BattleColor color = BattleColorUtil.FromString(colorSource.getColor());

        if (restrictToCharacterColors && color != shooter.data.bulletColorA && color != shooter.data.bulletColorB)
        {
            Debug.Log(shooter.data.displayName + " has no " + color + " bullets");
            return;
        }

        if (!shooter.UseBullet())
        {
            Debug.Log(shooter.data.displayName + " is out of bullets");
            return;
        }

        nextShotTime = Time.time + shotCooldown;

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

        Debug.Log(shooter.data.displayName + " shot " + target.name + " with " + color + " (progress " + target.colorProgress[(int)color] + "/" + target.shotsToColor + ", bullets left " + shooter.bulletsLeft + ")");
    }
}
