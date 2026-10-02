using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [SerializeField] float sensX;
    [SerializeField] float sensY;

    [SerializeField] Transform orientation;

    float xRotation;
    float yRotation;

    float mouseX;
    float mouseY;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        RotatePlayer();

    }

    public void GetRotationInput(Vector2 input) 
    {
        mouseX = input.x * Time.deltaTime * sensX;
        mouseY = input.y * Time.deltaTime * sensY;
    }

    void RotatePlayer() 
    {
        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

}
