//Simple camera shake script written by https://gist.github.com/ftvs

using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // Transform of the camera to shake. Grabs the gameObject's transform
    // if null.
    public Transform camTransform;

    // How long the object should shake for.
    public float shakeDuration = 0f;

    // Amplitude of the shake. A larger value shakes the camera harder.
    public float shakeAmount = 0.7f;
    public float decreaseFactor = 1.0f;

    public bool gunshotTime = false;

    float startingAmount = 0f;

    Vector3 originalPos;

    void Awake()
    {
        startingAmount = shakeAmount;
        if (camTransform == null)
        {
            camTransform = GetComponent(typeof(Transform)) as Transform;

        }
    }

    void OnEnable()
    {
        originalPos = camTransform.localPosition;
    }

    void Update()
    {
        if (shakeDuration > 0)
        {
            camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;

            if (gunshotTime)
            {
                shakeDuration -= Time.deltaTime * decreaseFactor;
            }
        }
        else
        {
            shakeDuration = 1f;
            gunshotTime = false;
            shakeAmount = startingAmount;
            camTransform.localPosition = originalPos;
        }
    }
}