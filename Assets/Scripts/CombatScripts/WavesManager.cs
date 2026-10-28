using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class EnemyWave
{
    public CombatantData[] enemies;
}

public class WavesManager : MonoBehaviour
{
    public BattleManager battleManager;
    public GameObject banditPrefab;
    public Transform[] slots;
    public EnemyWave[] waves;
    public bool startOnPlay = true;
    public float dropHeight = 6f;
    public float dropTime = 0.5f;
    public float staggerTime = 0.15f;
    public float cleanupDelay = 1.5f;
    public TMP_Text waveText;

    int currentWave = -1;
    int spawnedCount;

    public int CurrentWave
    {
        get { return currentWave + 1; }
    }

    public int TotalWaves
    {
        get { return waves.Length; }
    }

    void OnEnable()
    {
        Combatant.AnyDied += OnDied;
    }

    void OnDisable()
    {
        Combatant.AnyDied -= OnDied;
    }

    void Start()
    {
        if (startOnPlay)
        {
            StartFirstWave();
        }
    }

    public void StartFirstWave()
    {
        List<Combatant> first = SpawnWave(0);
        battleManager.StartBattle(new List<Combatant>(battleManager.party), first);
        UpdateText();
    }

    public int EnemiesRemaining()
    {
        int total = 0;

        foreach (Combatant c in battleManager.enemies)
        {
            if (c != null && c.IsAlive)
            {
                total++;
            }
        }

        for (int i = currentWave + 1; i < waves.Length; i++)
        {
            total += Mathf.Min(waves[i].enemies.Length, slots.Length);
        }

        return total;
    }

    void OnDied(Combatant who)
    {
        if (who.isPlayerSide || !battleManager.enemies.Contains(who))
        {
            return;
        }

        UpdateText();

        foreach (Combatant c in battleManager.enemies)
        {
            if (c != null && c.IsAlive)
            {
                return;
            }
        }

        if (currentWave + 1 >= waves.Length)
        {
            return;
        }

        foreach (Combatant old in battleManager.enemies)
        {
            if (old != null)
            {
                Destroy(old.gameObject, cleanupDelay);
            }
        }

        List<Combatant> next = SpawnWave(currentWave + 1);
        battleManager.enemies.Clear();
        battleManager.enemies.AddRange(next);

        Debug.Log("Wave " + CurrentWave + " of " + TotalWaves);
        UpdateText();
    }

    List<Combatant> SpawnWave(int index)
    {
        currentWave = index;
        List<Combatant> result = new List<Combatant>();
        EnemyWave wave = waves[index];

        for (int i = 0; i < wave.enemies.Length && i < slots.Length; i++)
        {
            result.Add(SpawnEnemy(wave.enemies[i], slots[i], i));
        }

        return result;
    }

    Combatant SpawnEnemy(CombatantData data, Transform slot, int slotIndex)
    {
        GameObject go = Instantiate(banditPrefab, slot.position + Vector3.up * dropHeight, Quaternion.identity);
        spawnedCount++;
        go.name = data.displayName + " " + spawnedCount;
        go.transform.localScale = Vector3.one;

        banditActivator activator = go.GetComponent<banditActivator>();
        if (activator != null)
        {
            activator.enabled = false;
        }

        banditJump jump = go.GetComponent<banditJump>();
        if (jump != null)
        {
            jump.enabled = false;
        }

        Rigidbody2D body = go.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        Combatant c = go.GetComponent<Combatant>();
        if (c == null)
        {
            c = go.AddComponent<Combatant>();
        }
        c.data = data;
        c.isPlayerSide = false;
        c.Initialize();

        if (go.GetComponent<EnemyColorView>() == null)
        {
            go.AddComponent<EnemyColorView>();
        }

        StartCoroutine(DropIn(go.transform, slot.position, slotIndex * staggerTime));
        return c;
    }

    IEnumerator DropIn(Transform t, Vector3 target, float delay)
    {
        Vector3 start = t.position;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float elapsed = 0f;
        while (elapsed < dropTime)
        {
            if (t == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / dropTime);
            float eased = 1f - (1f - p) * (1f - p);
            t.position = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        if (t != null)
        {
            t.position = target;
        }
    }

    void UpdateText()
    {
        if (waveText != null)
        {
            waveText.text = "WAVE " + CurrentWave + "/" + TotalWaves + "   ENEMIES " + EnemiesRemaining();
        }
    }
}
