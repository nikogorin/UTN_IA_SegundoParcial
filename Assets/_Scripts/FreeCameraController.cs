using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float movementSpeed = 8f;
    [SerializeField, Min(1f)] private float sprintMultiplier = 3f;

    [Header("Rotation")]
    [SerializeField, Min(0f)] private float lookSensitivity = 0.1f;
    [SerializeField] private float minimumPitch = -85f;
    [SerializeField] private float maximumPitch = 85f;

    private InputAction _moveAction;
    private InputAction _verticalAction;
    private InputAction _sprintAction;
    private InputAction _lookAction;
    private InputAction _rotateAction;

    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        CreateInputActions();

        Vector3 initialRotation = transform.eulerAngles;

        _yaw = initialRotation.y;
        _pitch = NormalizeAngle(initialRotation.x);
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _verticalAction.Enable();
        _sprintAction.Enable();
        _lookAction.Enable();
        _rotateAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _verticalAction.Disable();
        _sprintAction.Disable();
        _lookAction.Disable();
        _rotateAction.Disable();

        UnlockCursor();
    }

    private void Update()
    {
        UpdateMovement();
        UpdateRotation();
    }

    private void CreateInputActions()
    {
        _moveAction = new InputAction(
            name: "Camera Movement",
            type: InputActionType.Value
        );

        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        _verticalAction = new InputAction(
            name: "Vertical Movement",
            type: InputActionType.Value
        );

        _verticalAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/q")
            .With("Positive", "<Keyboard>/e");

        _sprintAction = new InputAction(
            name: "Camera Sprint",
            type: InputActionType.Button,
            binding: "<Keyboard>/leftShift"
        );

        _lookAction = new InputAction(
            name: "Camera Look",
            type: InputActionType.Value,
            binding: "<Mouse>/delta"
        );

        _rotateAction = new InputAction(
            name: "Enable Camera Rotation",
            type: InputActionType.Button,
            binding: "<Mouse>/rightButton"
        );
    }

    private void UpdateMovement()
    {
        Vector2 movementInput = _moveAction.ReadValue<Vector2>();
        float verticalInput = _verticalAction.ReadValue<float>();

        Vector3 movement =
            transform.right * movementInput.x +
            transform.forward * movementInput.y +
            Vector3.up * verticalInput;

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        float currentSpeed = movementSpeed;

        if (_sprintAction.IsPressed())
            currentSpeed *= sprintMultiplier;

        transform.position +=
            movement * currentSpeed * Time.unscaledDeltaTime;
    }

    private void UpdateRotation()
    {
        if (!_rotateAction.IsPressed())
        {
            UnlockCursor();
            return;
        }

        LockCursor();

        Vector2 mouseDelta = _lookAction.ReadValue<Vector2>();

        _yaw += mouseDelta.x * lookSensitivity;
        _pitch -= mouseDelta.y * lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, minimumPitch, maximumPitch);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
