using UnityEngine;

public class CameraDrift : MonoBehaviour
{
    public BattleManager battleManager;
    public float driftX = 0.35f;
    public float driftY = 0.2f;
    public float speed = 0.3f;
    public float rotationSway = 0.6f;
    public float actionStrength = 0.3f;
    public float fadeSpeed = 1.5f;

    float phase;
    float strength = 1f;
    float seed;

    void Start()
    {
        seed = Random.value * 10f;
    }

    void LateUpdate()
    {
        float target = 1f;
        if (battleManager != null && battleManager.State == BattleState.Resolving)
        {
            target = actionStrength;
        }

        strength = Mathf.MoveTowards(strength, target, fadeSpeed * Time.deltaTime);
        phase += Time.deltaTime * speed * 2f * Mathf.PI;

        float x = Mathf.Sin(phase + seed) * driftX;
        float y = Mathf.Sin(phase * 1.3f + seed * 2f) * driftY;
        float tiltX = Mathf.Sin(phase * 0.8f + seed) * rotationSway;
        float tiltY = Mathf.Cos(phase * 0.6f + seed) * rotationSway;

        transform.localPosition = new Vector3(x, y, 0f) * strength;
        transform.localRotation = Quaternion.Euler(tiltX * strength, tiltY * strength, 0f);
    }
}
