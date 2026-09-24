//敌人寻路系统:同步格子数据给A*,并为敌人计算路径
using UnityEngine;
using System.Collections.Generic;

public interface IFindSystem : ISystem
{
    //把 GridMapModel 的最新格子数据同步进 A* 工具
    void RebuildGrid(GameObject targert);
    //为敌人寻路,成功则把路径交给敌人身上的 EnemyMove
    bool FindPathFor(GameObject enemy, Vector3 targetWorldPos, out int destX, out int destY);
    //使用FlowField寻路
    public Vector3 FindPathForFlowField(int x, int y);
}

public class FindSystem : AbstractSystem, IFindSystem
{
    private IGridMapModel _gridMapModel;
    private IAStartPathUtil _aStartPathUtil;
    private IFlowFieldUtil _flowFieldUtil;
    private IEnemyModel _enemyModel;
    protected override void OnInit()
    {
        _gridMapModel = this.GetModel<IGridMapModel>();
        _aStartPathUtil = this.GetUtility<IAStartPathUtil>();
        _flowFieldUtil = this.GetUtility<IFlowFieldUtil>();
        _enemyModel = this.GetModel<IEnemyModel>();
    }

    public void RebuildGrid(GameObject target)
    {
        if (_gridMapModel == null || _aStartPathUtil == null || _flowFieldUtil == null) return;
        int xs = _gridMapModel.XSize;
        int ys = _gridMapModel.YSize;
        if (xs <= 0 || ys <= 0) return;

        //按坐标取节点,下标要和 A* 里的 x * ySize + y 对齐
        BaseNode[] allNodes = new BaseNode[xs * ys];
        for (int x = 0; x < xs; x++)
        {
            for (int y = 0; y < ys; y++)
            {
                allNodes[x * ys + y] = _gridMapModel.GetNode(x, y);
            }
        }
        _aStartPathUtil.init(xs, ys, allNodes);
        _flowFieldUtil.Init(xs, ys, allNodes);

        //流场的起点是目标格。这里要过 WorldToGrid,直接拿 worldPos.x 当地格索引是错的:
        //地图原点在 _origin(-25,-25),而且 (int) 是朝零截断,负半轴会整片偏移
        if (target == null)
        {
            Debug.LogError($"RebuildGrid:没有指定目标,流场没有烘");
            return;
        }
        if (!_gridMapModel.WorldToGrid(target.transform.position, out int gx, out int gy))
        {
            Debug.LogError($"RebuildGrid:目标不在格子地图范围内");
            return;
        }
        _flowFieldUtil.GenerateFlowField(gx, gy);
    }

    //使用A*寻路
    public bool FindPathFor(GameObject enemy, Vector3 targetWorldPos, out int destX, out int destY)
    {
        if (!_gridMapModel.WorldToGrid(enemy.transform.position, out int sx, out int sy))
        {
            Debug.LogError($"敌人不在格子地图范围内");
            destX = sx;
            destY = sy;
            return false;
        }
        if (!_gridMapModel.WorldToGrid(targetWorldPos, out int ex, out int ey))
        {
            Debug.LogError($"目标点不在格子地图范围内");
            destX = sx;
            destY = sy;
            return false;
        }

        BaseNode start = _gridMapModel.GetNode(sx, sy);
        BaseNode end = _gridMapModel.GetNode(ex, ey);
        //如果节点被占
        if (!_gridMapModel.isWalkable(ex, ey) || _gridMapModel.isClaimed(ex, ey))
        {
            end = FindClosestPoint(sx, sy, ex, ey);
        }
        if (end == null) { Debug.LogError($"没有找到可走的点"); destX = sx; destY = sy; return false; }

        //开始寻路
        List<AStartNode> path = _aStartPathUtil.FindPath(start, end);
        //FindPath 失败会返回 null,直接走会 NRE,后面每一步都执行不到
        if (path == null)
        {
            Debug.LogError($"没有找到从({sx},{sy})到({end.x},{end.y})的路径");
            destX = sx;
            destY = sy;
            return false;
        }
        List<Vector3> worldPath = path.ConvertAll(n => _gridMapModel.GridToWorld(n.node.x, n.node.y));
        destX = end.x;
        destY = end.y;
        return true;
    }





    //使用流场寻路
    public Vector3 FindPathForFlowField(int x, int y)
    {
        //GetDirection 返回的是 Vector3 结构体,不可能为 null;取不到方向时它自己就返回 zero
        return _flowFieldUtil.GetDirection(x, y);
    }


    public Vector3 Tick(int grid, float dt)
    {
        EnemyData d = _enemyModel.GetEnemyData(grid);
        d.pos = _gridMapModel.GridToWorld(d.x, d.y);   // 或者按流场方向推进
        return d.pos;
    }






















    //找到目标节点最近的点
    private BaseNode FindClosestPoint(int sx, int sy, int ex, int ey)
    {
        BaseNode startNode = _gridMapModel.GetNode(sx, sy);
        int MaxR = Mathf.Max(_gridMapModel.XSize, _gridMapModel.YSize);

        //从近一圈往外扩
        for (int r = 0; r < MaxR; r++)
        {
            //所有节点
            List<BaseNode> nodes = new List<BaseNode>();
            //从第一圈开始,往里扩
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    //只取圈
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) != r) continue;
                    BaseNode node = _gridMapModel.GetNode(ex + dx, ey + dy);
                    if (node != null) nodes.Add(node);
                }
            }
            //再把一圈来排序
            nodes.Sort((a, b) => Dist2(a, ex, ey).CompareTo(Dist2(b, ex, ey)));

            //找到第一个可走的点
            foreach (var node in nodes)
            {
                if (_gridMapModel.isWalkable(node.x, node.y) && !_gridMapModel.isClaimed(node.x, node.y))
                {
                    return node;
                }
            }
        }
        return null;
    }

    //计算距离的平方
    private int Dist2(BaseNode n, int ex, int ey)
    {
        int dx = n.x - ex;
        int dy = n.y - ey;
        return dx * dx + dy * dy;
    }
}


