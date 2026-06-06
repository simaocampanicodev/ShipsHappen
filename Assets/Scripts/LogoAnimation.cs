using UnityEngine;

public class LogoAnimation : MonoBehaviour
{
    [SerializeField] private float moveAmplitude = 0.5f;
    [SerializeField] private float moveFrequency = 1f;

    [SerializeField] private float scaleAmplitude = 0.05f;

    [SerializeField] private float rotationAmount = 5f;
    [SerializeField] private float rotationSpeed = 0.8f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private Quaternion startRotation;

    private void Start()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        startRotation = transform.localRotation;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * moveFrequency);

        transform.localPosition = startPosition + new Vector3(0f, wave * moveAmplitude, 0f);

        float scaleOffset = wave * scaleAmplitude;
        transform.localScale = startScale + new Vector3(scaleOffset, scaleOffset, scaleOffset);

        float tilt = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;
        transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, tilt);
    }
}