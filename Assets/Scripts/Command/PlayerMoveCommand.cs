//角色移动命令
using UnityEngine;

public class PlayerMoveCommand : AbstractCommand
{
    public Vector3 TargetPosition;
    protected override void OnExcute()
    {
        this.GetModel<IPlayerModel>().SetTargetPosition(TargetPosition);
        this.GetSystem<IMoveSystem>().Move(TargetPosition);
        this.GetSystem<IPlayerStateManager>().ChangeState(PlayerState.Move);
    }
}
