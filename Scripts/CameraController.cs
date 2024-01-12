using Godot;

namespace ProceduralCityGenerator.Scripts;

public partial class CameraController : Camera3D
{
    private const float MouseSensitivity = 0.1f;
    private readonly Vector2 _draggingSensitivity = new(200f, 200f);
    private Vector2 _actualMousePosition;

    private CameraMovement _cameraMovement;
    private Vector3 _cameraSpeed = Vector3.One;
    private bool _dragging;
    private Vector2 _mouseMovement;
    private float _pitch;
    private Vector2 _startPoint;
    private float _yaw;

    public Vector2 centerScreen;

    public bool mouseCapture;

    public override void _Ready()
    {
        _startPoint = Vector2.Zero;
        _actualMousePosition = Vector2.Zero;
        Size = 4;
    }

    public override void _Process(double delta)
    {
        centerScreen = new Vector2(Position.X, Position.Z);

        switch (Projection)
        {
            case ProjectionType.Orthogonal:
                _ProcessOrthogonal();
                break;
            case ProjectionType.Perspective:
                _ProcessProjection(delta);
                break;
        }
    }

    private void _ProcessOrthogonal()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;

        if (!_dragging) return;

        var newPosition = new Vector3(
            Position.X + (_startPoint.X - _actualMousePosition.X) / _draggingSensitivity.X,
            Position.Y,
            Position.Z + (_startPoint.Y - _actualMousePosition.Y) / _draggingSensitivity.Y
        );
        Position = newPosition;
        _startPoint = _actualMousePosition;
    }

    private void _ProcessProjection(double delta)
    {
        Input.MouseMode = mouseCapture ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        _yaw = (_yaw - _mouseMovement.X * MouseSensitivity) % 360;
        _pitch = Mathf.Max(Mathf.Min(_pitch - _mouseMovement.Y * MouseSensitivity, 90), -90);
        Rotation = new Vector3(Mathf.DegToRad(_pitch), Mathf.DegToRad(_yaw), 0);
        if (_cameraMovement.forward)
            Position -= GlobalTransform.Basis.Z * _cameraSpeed * (float)delta;
        else if (_cameraMovement.backward) Position += GlobalTransform.Basis.Z * _cameraSpeed * (float)delta;
        if (_cameraMovement.left)
            Position -= GlobalTransform.Basis.X * _cameraSpeed * (float)delta;
        else if (_cameraMovement.right) Position += GlobalTransform.Basis.X * _cameraSpeed * (float)delta;
        if (_cameraMovement.up)
            Position += Vector3.Up * _cameraSpeed * (float)delta;
        else if (_cameraMovement.down) Position -= Vector3.Up * _cameraSpeed * (float)delta;
        _mouseMovement = Vector2.Zero;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionReleased("mouseMiddle"))
            _dragging = false;
        else if (@event.IsActionPressed("mouseMiddle")) _dragging = true;

        switch (Projection)
        {
            case ProjectionType.Orthogonal:
                _InputOrthogonal(@event);
                break;
            case ProjectionType.Perspective:
                _InputProjection(@event);
                break;
        }
    }

    private void _InputOrthogonal(InputEvent @event)
    {
        if (@event is InputEventMouseMotion eventMouseMotion)
            if (_dragging)
                _actualMousePosition = eventMouseMotion.Position;

        if (@event is InputEventMouseButton eventMouseButton)
        {
            if (@event.IsActionPressed("mouseMiddle"))
            {
                _startPoint = eventMouseButton.Position;
                _actualMousePosition = eventMouseButton.Position;
            }

            // Scroll up
            if (eventMouseButton.ButtonIndex is MouseButton.WheelUp)
            {
                if (Size - 1 <= 0)
                    Size = 1;
                else
                    Size -= 1;
            }
            // Scroll down
            else if (eventMouseButton.ButtonIndex is MouseButton.WheelDown)
            {
                Size += 1;
            }

            // var width = Size * (GetViewport().GetVisibleRect().Size.X / GetViewport().GetVisibleRect().Size.Y);
            // GD.Print("Position: ", Position.X - width / 2, ", ", Position.Z - Size / 2);
        }
    }

    private void _InputProjection(InputEvent @event)
    {
        if (!mouseCapture) return;

        if (@event is InputEventMouseMotion eventMouseMotion) _mouseMovement = eventMouseMotion.Relative;

        if (@event is InputEventKey inputEventKey)
        {
            if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.W)
            {
                _cameraMovement.forward = true;
            }
            else if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.S)
            {
                _cameraMovement.backward = true;
            }
            else if (inputEventKey.Keycode is Key.W or Key.S)
            {
                _cameraMovement.forward = false;
                _cameraMovement.backward = false;
            }

            if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.A)
            {
                _cameraMovement.left = true;
            }
            else if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.D)
            {
                _cameraMovement.right = true;
            }
            else if (inputEventKey.Keycode is Key.A or Key.D)
            {
                _cameraMovement.left = false;
                _cameraMovement.right = false;
            }

            if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.E)
            {
                _cameraMovement.up = true;
            }
            else if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.Q)
            {
                _cameraMovement.down = true;
            }
            else if (inputEventKey.Keycode is Key.Q or Key.E)
            {
                _cameraMovement.up = false;
                _cameraMovement.down = false;
            }

            if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.Shift)
                _cameraSpeed = Vector3.One * 4f;
            else if (!inputEventKey.IsPressed() && inputEventKey.Keycode == Key.Shift) _cameraSpeed = Vector3.One;
        }
    }

    private struct CameraMovement
    {
        public bool forward;
        public bool backward;
        public bool left;
        public bool right;
        public bool up;
        public bool down;
    }
}