using Godot;

public partial class CameraController : Camera3D
{
    [Export] public float MoveSpeed = 5.0f;
    [Export] public float LookSensitivity = 0.001f;
    [Export] public float RotateSpeed = 1.0f; // Speed for Q/E rotation

    private bool _isLeftMousePressed = false;
    private bool _isRightMousePressed = false;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Input(InputEvent @event)
    {
        // Handle mouse rotation with left mouse button
        if (@event is InputEventMouseMotion mouseMotion && _isLeftMousePressed)
        {
            RotateY(-mouseMotion.Relative.X * LookSensitivity);
            RotateX(mouseMotion.Relative.Y * LookSensitivity);

            Vector3 rotation = Rotation;
            rotation.X = Mathf.Clamp(rotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
            Rotation = rotation;
        }

        // Handle camera translation with right mouse button
        if (@event is InputEventMouseMotion mouseMotion2 && _isRightMousePressed)
        {
            Vector3 translation = new Vector3(-mouseMotion2.Relative.X, mouseMotion2.Relative.Y, 0) * LookSensitivity * MoveSpeed;
            Position += Transform.Basis * translation;
        }

        // Toggle left mouse button state
        if (@event is InputEventMouseButton leftMouseButton && leftMouseButton.ButtonIndex == MouseButton.Left)
        {
            _isLeftMousePressed = leftMouseButton.Pressed;
            Input.MouseMode = _isLeftMousePressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }

        // Toggle right mouse button state
        if (@event is InputEventMouseButton rightMouseButton && rightMouseButton.ButtonIndex == MouseButton.Right)
        {
            _isRightMousePressed = rightMouseButton.Pressed;
            Input.MouseMode = _isRightMousePressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }

        // Release mouse capture with Escape
        if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
        {
            _isLeftMousePressed = false;
            _isRightMousePressed = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _Process(double delta)
    {
        // Handle keyboard movement
        Vector3 direction = Vector3.Zero;

        if (Input.IsActionPressed("move_forward"))
            direction.Z -= 1;
        if (Input.IsActionPressed("move_backward"))
            direction.Z += 1;
        if (Input.IsActionPressed("move_left"))
            direction.X -= 1;
        if (Input.IsActionPressed("move_right"))
            direction.X += 1;

        if (direction != Vector3.Zero)
        {
            direction = direction.Normalized();
            Position += Transform.Basis * direction * MoveSpeed * (float)delta;
        }

        // Handle Q/E rotation
        if (Input.IsActionPressed("rotate_clockwise")) // Bind "Q" to "rotate_clockwise" in Input Map
            RotateZ(RotateSpeed * (float)delta);
        if (Input.IsActionPressed("rotate_counter_clockwise")) // Bind "E" to "rotate_counter_clockwise" in Input Map
            RotateZ(-RotateSpeed * (float)delta);
    }
}
