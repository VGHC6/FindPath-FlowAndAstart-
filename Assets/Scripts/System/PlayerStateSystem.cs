//玩家状态系统
using System.Linq.Expressions;
using UnityEngine;
public interface IBasePlayerState
{
    public float blendInt { get; }
    public bool CanInterrupt { get; }
    public void Enter();
    public void Update();
    public void Exit();
}


public class IdleState : IBasePlayerState
{
    public float blendInt => 0f;
    public bool CanInterrupt => true;
    public void Enter()
    {
        // Debug.Log("IdleState Enter");
    }
    public void Update() { }
    public void Exit() { }
}

public class MoveState : IBasePlayerState
{
    public float blendInt => 6f;
    public bool CanInterrupt => true;
    public void Enter()
    {
        // Debug.Log("MoveState Enter");
    }
    public void Update() { }
    public void Exit() { }
}

public class AttackState : IBasePlayerState
{
    public float blendInt => 0f;
    public bool CanInterrupt => false;
    public void Enter()
    {
        Debug.Log("AttackState Enter");
    }
    public void Update() { }
    public void Exit() { }
}