using UnityEngine;

public class ScaleByMicrophoneLoud : MonoBehaviour
{
    //[SerializeField] private AudioSource source;
    [SerializeField] private Vector3 minScale;
    [SerializeField] private Vector3 maxScale;
    [SerializeField] private LoudDetection detector;

    [SerializeField] private float loudnessSensibility = 100f;
    [SerializeField] private float threshold = 0.1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (detector == null) return;

        float loudness = detector.GetLoudnessFromMicrophone() * loudnessSensibility;

        if (loudness < threshold)
            loudness = 0;

        transform.localScale = Vector3.Lerp(minScale, maxScale, loudness);
    }
}
