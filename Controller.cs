using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;



public interface IController
{
    public void UpdateState(KeyboardState kbstate, MouseState mstate, FrameEventArgs e);

    public Vector3 GetMovement(); 

    public Angles2D GetArmOrientation();

}

public class Controller : IController {

    private KeyboardState? _keyboardState;

    private FrameEventArgs? _frameEvent;

    private float _deltaTime;

    private Vector3 _movementInput = new Vector3();

    public float Speed {get; set;}

    public float ScaleMovement {get; set;}

    public float CameraDistance {get; set;}
    
    private int _hRes;

    private int _vRes;

    private MouseState ?_mouseState;
    private bool _firstMouse=true;
    private Vector2 _lastMouse=new Vector2();
    public float MouseSensitivity {get; set;}
    private Angles2D _armAngles;



    public Controller(int hRes, int vRes)
    {

            Speed=1.0f;
            ScaleMovement=1.0f;
            CameraDistance=3.0f;
            _hRes=hRes;
            _vRes=vRes;

            MouseSensitivity=1000000.0f;
            _armAngles=new Angles2D(0.0f,0.0f);


    }

    public void UpdateState(KeyboardState kbstate,MouseState mstate, FrameEventArgs e)
    {
        _keyboardState = kbstate;

        _frameEvent=e;
        _deltaTime=(float)e.Time;
        _movementInput=Vector3.Zero;

        // Keyboard
        if(_keyboardState.IsKeyDown(Keys.W))
            _movementInput.X=Speed*_deltaTime;

        if(_keyboardState.IsKeyDown(Keys.S))
            _movementInput.X= -Speed*_deltaTime;

        if(_keyboardState.IsKeyDown(Keys.A))
            _movementInput.Y=-Speed*_deltaTime;

        if(_keyboardState.IsKeyDown(Keys.D))
            _movementInput.Y=Speed*_deltaTime;

        if(_keyboardState.IsKeyDown(Keys.E))
            _movementInput.Z=Speed*_deltaTime;

        if(_keyboardState.IsKeyDown(Keys.Q))
            _movementInput.Z=-Speed*_deltaTime;

        _mouseState=mstate;

        if (_mouseState.IsButtonDown(MouseButton.Left)){
                float deltaX,deltaY;
                // Normalize to device
                float mx = (float)_mouseState.X/_hRes;
                float my = (float)_mouseState.Y/_vRes;

                if(_firstMouse){
                _lastMouse=new Vector2(mx,my);
                _firstMouse=false; 
                _armAngles=new Angles2D(0.0f,0.0f);
                }
                else{
                deltaX= mx-_lastMouse.X;
                deltaY= my-_lastMouse.Y;

                _lastMouse=new Vector2(mx,my);
                _armAngles=new Angles2D(deltaX,-deltaY)*_deltaTime*MouseSensitivity;
                }
                }
                else
                    _firstMouse=true;

        
    }

    public Vector3 GetMovement(){


            return _movementInput*ScaleMovement;

    }

    public Angles2D GetArmOrientation(){
        return _armAngles;
    }





}
