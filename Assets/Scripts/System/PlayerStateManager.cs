//玩家状态管理器
using System.Collections.Generic;
using UnityEngine;
public interface IPlayerStateManager : ISystem
{
    public Dictionary<PlayerState, IBasePlayerState> States { get; }
    public float CurrentAnimSpeed { get; }
    public void ChangeState(PlayerState state, int animatorBlendIndex = 0);
    public void Update();
}

public class PlayerStateManager : AbstractSystem, IPlayerStateManager
{
    private PlayerState _currentState = PlayerState.Idle;

    private const float MaxBlendSpeed = 6f;
    private const float SpeedBlendTime = 0.15f;
    private float _targetAnimSpeed;
    private float _currentAnimSpeed;
    public float CurrentAnimSpeed => _currentAnimSpeed;

    public Dictionary<PlayerState, IBasePlayerState> States { get; } = new();


    protected override void OnInit()
    {
        States.Add(PlayerState.Idle, new IdleState());
        States.Add(PlayerState.Move, new MoveState());
        States.Add(PlayerState.Attack, new AttackState());
        States[_currentState].Enter();
        _targetAnimSpeed = States[_currentState].blendInt;
        _currentAnimSpeed = _targetAnimSpeed;
    }



    public void ChangeState(PlayerState state, int animatorBlendIndex = 0)
    {
        if (_currentState == state)
        {
            return;
        }

        States[_currentState].Exit();
        _currentState = state;
        States[state].Enter();
        _targetAnimSpeed = States[state].blendInt;
        this.GetModel<IPlayerModel>().SetState(state);
        //发送状态改变事件
        this.SendEvent(new ChangeStateEvent { State = state });
    }

    public void Update()
    {
        States[_currentState].Update();
        UpdateAnimSpeed();
    }

    private void UpdateAnimSpeed()
    {
        float rate = MaxBlendSpeed / SpeedBlendTime;
        _currentAnimSpeed = Mathf.Lerp(_currentAnimSpeed, _targetAnimSpeed, rate * Time.deltaTime);
    }
}