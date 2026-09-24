//这里是移动系统,计算角色的移动方向和移动位置
using UnityEngine;

public interface IMoveSystem : ISystem
{
    //移动角色
    void Move(Vector3 pos);
    //更新角色位置
    void UpdatePosition(Vector3 currentPosition, float deltaTime, out Vector3 step);
    //获取角色的旋转方向
    Vector3 GetRotate();
}

public class MoveSystem : AbstractSystem, IMoveSystem
{
    public Vector3 _Rotate;
    private Vector3 _targetPosition;
    private bool _hasTarget;
    protected override void OnInit()
    {
    }
    public void Move(Vector3 pos)
    {
        _targetPosition = pos;
        _hasTarget = true;
    }

    public void UpdatePosition(Vector3 currentPosition, float deltaTime, out Vector3 step)
    {
        if (!_hasTarget)
        {
            _Rotate = Vector3.zero;
            step = Vector3.zero;
            return;
        }

        step = Vector3.zero;
        Vector3 to = _targetPosition - currentPosition;
        to.y = 0f;
        float distinct = to.magnitude;//目标位置与当前位置的差值
        if (distinct <= 0.5f)
        {
            // Debug.Log("到达目标位置");
            _hasTarget = false;
            _targetPosition = currentPosition;
            _Rotate = Vector3.zero;
            this.GetSystem<IPlayerStateManager>().ChangeState(PlayerState.Idle);
            return;
        }
        Vector3 direction = to / distinct;
        _Rotate = direction.normalized;
        float maxSpped = this.GetModel<IPlayerModel>()._playerData.speed;
        float speed = Mathf.Min(maxSpped, distinct / 1.5f * maxSpped);
        step = direction * speed;
    }

    public Vector3 GetRotate()
    {
        return _Rotate;
    }


}
