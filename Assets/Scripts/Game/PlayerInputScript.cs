using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputScript : MonoBehaviour
{
    [SerializeField] PlayerCam cameraRotation;

    [SerializeField] PlayerMovement playerMoviment;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnLook(InputValue value) 
    {
        cameraRotation.GetRotationInput(value.Get<Vector2>());
    }
    public void OnMove(InputValue value)
    {
        playerMoviment.PlayerMovementInput(value.Get<Vector2>());
    }

    public void OnJump() 
    {
        playerMoviment.Jump();
        Debug.Log("pulou");
    }
}
