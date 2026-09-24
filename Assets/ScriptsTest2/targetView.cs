//目标视图
using UnityEngine;
using UnityEngine.InputSystem;

public class ITargetView : MonoBehaviour, IController
{
    public IAchitecture GetArchitecture() => mobaTest.Interface;
    public void SetArchitecture(IAchitecture architecture) { }
    private StarterAssets _starterAssets;
    private ITargetSystem _targetSystem;
    private IGridMapModel _gridMapModel;
    //位置
    private Vector3 _targetPos;
    private int _targetX, _targetY;
    //移动
    void Awake()
    {
        _starterAssets = new StarterAssets();
        _targetSystem = this.GetSystem<ITargetSystem>();
        _gridMapModel = this.GetModel<IGridMapModel>();
    }
    void OnEnable()
    {
        _starterAssets.Player.MouseLeftDown.performed += SelectTargetEnemy;
        _starterAssets.Enable();
    }

    void OnDisable()
    {
        _starterAssets.Player.MouseLeftDown.performed -= SelectTargetEnemy;
        _starterAssets.Disable();
    }

    private void SelectTargetEnemy(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //得到点击位置
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            _targetPos = hit.point;
            _gridMapModel.WorldToGrid(_targetPos, out _targetX, out _targetY);
        }
    }

    void FixedUpdate()
    {
        if (_targetPos == Vector3.zero) return;
        _targetSystem.Tick(Time.deltaTime, transform.position, _targetPos, out Vector3 direction, out int x, out int y);
        transform.position += direction * Time.deltaTime * 5;
    }
}

