using UnityEngine;
public class mobaTest : Architecture<mobaTest>
{
    protected override void Init()
    {
        //Utility
        RegisterUtility<IFlowFieldUtil>(new FlowFieldUtil());
        RegisterUtility<IAStartPathUtil>(new AStartPathUtil());

        //System
        RegisterSystem<ITargetSystem>(new TargetSystem());
        RegisterSystem<IMoveSystem>(new MoveSystem());
        RegisterSystem<IPlayerStateManager>(new PlayerStateManager());
        RegisterSystem<IFindSystem>(new FindSystem());
        RegisterSystem<IEnemyMoveSystem>(new EnemyMoveSystem());
        //Model
        RegisterModel<IPlayerModel>(new PlayerModel());
        RegisterModel<IGridMapModel>(new GridMapModel());
        RegisterModel<IEnemyModel>(new EnemyModel());
    }
}
