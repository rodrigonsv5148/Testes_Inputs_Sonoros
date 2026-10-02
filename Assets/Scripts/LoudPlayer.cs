using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LoudPlayer : MonoBehaviour
{
    // Input System
    PlayerInput input;
    InputAction moveAction;
    InputAction rotateCamAction;
    public float moveSpeed = 3;


    [SerializeField] float loudnessSensibility = 100;
    [SerializeField] float threshold = 0.1f;
    float loudness;

    //[SerializeField] RevealManagerIndividual revealManager;
    [SerializeField] LoudDetection loudDetector;

    [SerializeField] float minRevealRadius = 0;
    [SerializeField] float maxRevealRadius = 4;

    [SerializeField] bool test = true;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        moveAction = input.actions.FindAction("Move");
        rotateCamAction = input.actions.FindAction("Look");
    }

    void Start()
    {
        Gizmos.color = Color.purple;
    }

    void Update()
    {
        //loudness = loudDetector.GetLoudnessFromMicrophone() * loudnessSensibility;
        
        if (loudness < threshold)
            loudness = 0;
        
        //revealManager.SetRadio(Mathf.LerpUnclamped(minRevealRadius, maxRevealRadius, loudness));

        MovePlayer();

    }

    private void OnDrawGizmos()
    {
        if (test) Gizmos.DrawWireSphere(transform.position, loudness);
    }

    public void OnAttack()
    {
        Debug.Log("OnAttack");
        loudness = maxRevealRadius;
        StartCoroutine(aasd());
    }

    void MovePlayer() 
    {
        Vector2 direction = moveAction.ReadValue<Vector2>();
        transform.position += new Vector3(direction.x, 0, direction.y) * moveSpeed * Time.deltaTime;
    }

    IEnumerator aasd() 
    {
        yield return new WaitForSeconds(.4f);
        loudness = 0;
    }

    public void OnInteract()
    {
        Debug.Log("OnInteract");
        //revealManager.SetPersistanceOn(1);
    }

    public void OnJump()
    {
        Debug.Log("OnJump");
        //revealManager.SetPersistanceOn(0);
    }
}
