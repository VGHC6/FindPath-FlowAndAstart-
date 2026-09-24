//这里是玩家数据
using UnityEngine;

public enum PlayerState
{
    Idle,
    Move,
    Attack,
}


public interface IPlayerModel : IModel
{
    public PlayerData _playerData { get; }
    public BindableProperty<Vector3> CurrentPosition { get; }
    public BindableProperty<Vector3> TargetPosition { get; }
    public PlayerState State { get; }
    public void SetTargetPosition(Vector3 targetPosition);
    public Vector3 GetTargetPosition();

    //设置状态
    public void SetState(PlayerState state);
}




public class PlayerModel : IPlayerModel
{
    public PlayerData _playerData { get; } = new PlayerData();
    public BindableProperty<Vector3> CurrentPosition { get; private set; } = new BindableProperty<Vector3>();
    public BindableProperty<Vector3> TargetPosition { get; private set; } = new BindableProperty<Vector3>();
    public PlayerState State { get; private set; } = PlayerState.Idle;

    public IAchitecture GetArchitecture() => mobaTest.Interface;
    public void Init()
    {
        State = PlayerState.Idle;
    }

    public void SetArchitecture(IAchitecture architecture) { }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        TargetPosition.Value = targetPosition;
    }

    public Vector3 GetTargetPosition()
    {
        return TargetPosition.Value;
    }

    //设置状态
    public void SetState(PlayerState state)
    {
        State = state;
    }
}