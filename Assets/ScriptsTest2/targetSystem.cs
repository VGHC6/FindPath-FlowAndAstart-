//目标系统

using System.Collections.Generic;
using UnityEngine;
public interface ITargetSystem : ISystem
{
    void Tick(float deltaTime, Vector3 startPos, Vector3 targetPos, out Vector3 direction, out int x, out int y);
}


public class TargetSystem : AbstractSystem, ITargetSystem
{
    private IAStartPathUtil _startPathUtil;
    private IGridMapModel _gridMapModel;
    private IFlowFieldUtil _flowFieldUtil;
    //初值用 int.MinValue 而不是 0:格子(0,0)是地图角落的合法格,
    //默认 0 会让"首次目标就是(0,0)"这种情况被缓存守卫误判成"没变化"而跳过烘焙
    private int _oldTargetX = int.MinValue, _oldTargetY = int.MinValue;
    protected override void OnInit()
    {
        _startPathUtil = this.GetUtility<IAStartPathUtil>();
        _gridMapModel = this.GetModel<IGridMapModel>();
        _flowFieldUtil = this.GetUtility<IFlowFieldUtil>();
        _flowFieldUtil.Init(_gridMapModel.XSize, _gridMapModel.YSize, _gridMapModel.GetAllNodes());
        _startPathUtil.init(_gridMapModel.XSize, _gridMapModel.YSize, _gridMapModel.GetAllNodes());
    }

    //更新目标
    public void Tick(float deltaTime, Vector3 startPos, Vector3 targetPos, out Vector3 direction, out int x, out int y)
    {
        if (_startPathUtil == null || _gridMapModel == null)
        {
            Debug.LogError($"TargetSystem:没有初始化");
            direction = Vector3.zero;
            x = 0;
            y = 0;
            return;
        }

        BaseNode start = _gridMapModel.GetNode(startPos);
        BaseNode end = _gridMapModel.GetNode(targetPos);

        List<AStartNode> path = _startPathUtil.FindPath(start, end);
        if (path == null)
        {
            Debug.LogError($"没有找到路径");
            direction = Vector3.zero;
            x = 0;
            y = 0;
            return;
        }

        AStartNode first = path[0];
        direction = new Vector3(first.direction.x, 0, first.direction.y);
        x = first.node.x;
        y = first.node.y;

        RebuildFlowField(end.x, end.y);
    }

    //重新构建流场
    public void RebuildFlowField(int targetX, int targetY)
    {
        if (_oldTargetX == targetX && _oldTargetY == targetY) return;
        _oldTargetX = targetX;
        _oldTargetY = targetY;
        _flowFieldUtil.GenerateFlowField(targetX, targetY);
    }
}
