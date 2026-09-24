//角色控制器
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IController
{
    private Rigidbody _rigidbody;
    private StarterAssets _starterAssets;
    private IMoveSystem _moveSystem;
    private IPlayerStateManager _playerStateManagerSystem;
    private Animator _animator;
    public IAchitecture GetArchitecture() => mobaTest.Interface;

    public void SetArchitecture(IAchitecture architecture) { }
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _starterAssets = new StarterAssets();
        _moveSystem = this.GetSystem<IMoveSystem>();
        _playerStateManagerSystem = this.GetSystem<IPlayerStateManager>();
        _animator = GetComponent<Animator>();
    }
    void Start()
    {
        //注册输入事件
        _starterAssets.Player.MouseRightDown.performed += OnMouseDown;
        _starterAssets.Enable();
        _animator.SetFloat("MotionSpeed", 1f);
    }

    private void OnMouseDown(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            this.SendCommand(new PlayerMoveCommand { TargetPosition = hit.point });
        }
    }

    public void OnFootstep(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight > 0.5f)
        {
            //播放音效
        }
    }

    void Update()
    {
        _playerStateManagerSystem.Update();
        _animator.SetFloat("Speed", _playerStateManagerSystem.CurrentAnimSpeed);
    }

    void FixedUpdate()
    {
        //移动角色
        Vector3 velocity = _rigidbody.velocity;
        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        _moveSystem.UpdatePosition(transform.position, Time.deltaTime, out Vector3 step);
        Vector3 accel = Vector3.ClampMagnitude((step - planar) / Time.fixedDeltaTime, 150f);//限制加速度
        _rigidbody.AddForce(accel, ForceMode.Acceleration);

        //没有移动方向时不转向
        Vector3 rotate = _moveSystem.GetRotate();
        if (rotate.sqrMagnitude > 0.1f)
        {
            _rigidbody.MoveRotation(Quaternion.LookRotation(rotate));
        }
    }
}