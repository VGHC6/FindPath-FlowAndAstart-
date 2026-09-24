//流场寻路工具类:以某个目标格为中心烘一张图,多个寻路物体查同一张图,需要先把地图烘培为格子图
//只烘静态地形(GridMapAsset 的 _walkable);格子被谁占用(occupant)不参与烘培,交给 System 层自己处理

using System.Collections.Generic;
using UnityEngine;

public class FlowFieldNode
{
    public int x;
    public int y;
    public bool isWalkable;//地形是否可走,烘培时定下,之后不变
    public int fcost { get; private set; }//从目标格走到这里的累计代价,int.MaxValue 表示不可达
    public Vector3 direction { get; private set; }//指向下一个格子的单位方向,到达目标格为零

    public FlowFieldNode(int x, int y, bool isWalkable)
    {
        this.x = x;
        this.y = y;
        this.isWalkable = isWalkable;
        ReSet();
    }

    public void SetFCost(int c) => fcost = c;
    public void SetDirection(Vector3 d) => direction = d;

    //每次重新烘培前清掉上一次的结果
    public void ReSet()
    {
        fcost = int.MaxValue;
        direction = Vector3.zero;
    }
}

public interface IFlowFieldUtil : IUtility
{
    //传入地图尺寸和全部节点,初始化内部节点表,下标要和 A* 一样是 x * ySize + y
    void Init(int xSize, int ySize, BaseNode[] allNodes);
    //以目标格为中心烘一次流场图,成功返回 true
    bool GenerateFlowField(int targetX, int targetY);
    //按格子坐标取移动方向,不可达或出界返回 Vector3.zero
    Vector3 GetDirection(int x, int y);
    //判断格子地形是否可走
    bool IsWalkable(int x, int y);
    int xSize { get; }
    int ySize { get; }
    int GetMapSize();
}

public class FlowFieldUtil : IFlowFieldUtil
{
    private int _xSize;//地图宽度
    private int _ySize;//地图高度
    public int xSize { get => _xSize; }
    public int ySize { get => _ySize; }
    public int GetMapSize() => _xSize * _ySize;

    private readonly Dictionary<int, FlowFieldNode> _nodes = new Dictionary<int, FlowFieldNode>();

    public IAchitecture GetArchitecture() => mobaTest.Interface;

    //和 A* 用同一套索引:x * ySize + y
    private int Index(int x, int y) => x * _ySize + y;

    //初始化,只对地形可走的格子建节点
    public void Init(int xSize, int ySize, BaseNode[] allNodes)
    {
        if (allNodes == null || allNodes.Length < xSize * ySize)
        {
            Debug.LogError($"流场初始化失败:节点数组不完整");
            return;
        }
        _xSize = xSize;
        _ySize = ySize;
        _nodes.Clear();
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                BaseNode cell = allNodes[Index(x, y)];
                //流场只认地形;占用是动态的,由 System 决定要不要绕开
                _nodes[Index(x, y)] = new FlowFieldNode(x, y, cell != null && cell.isWalkable);
            }
        }
    }

    /// <summary>
    /// 生成流场图:先以目标格为起点做一次最短路径积分,再按积分结果给每格选方向
    /// </summary>
    /// <param name="targetX">目标格 x</param>
    /// <param name="targetY">目标格 y</param>
    /// <returns>目标格合法并烘培完成返回 true</returns>
    public bool GenerateFlowField(int targetX, int targetY)
    {
        if (_nodes.Count == 0)
        {
            Debug.LogError($"流场未初始化");
            return false;
        }
        FlowFieldNode target = GetNode(targetX, targetY);
        if (target == null)
        {
            Debug.LogError($"目标格({targetX},{targetY})不在流场范围内");
            return false;
        }
        if (!target.isWalkable)
        {
            Debug.LogError($"目标格({targetX},{targetY})不可行走");
            return false;
        }

        //重烘前清掉上一次的积分结果,否则会污染这一次
        foreach (var node in _nodes.Values) node.ReSet();
        target.SetFCost(0);

        //Dijkstra:每次取累计代价最小的格子往外扩
        //单步代价只有 10/14,格子数也不大,所以列表里线性找最小即可,不必上优先队列
        var searchNodes = new List<FlowFieldNode>() { target };//待处理格
        var processedNodes = new List<FlowFieldNode>();//已定格的格子
        while (searchNodes.Count > 0)
        {
            FlowFieldNode currentNode = searchNodes[0];
            foreach (var node in searchNodes)
            {
                if (node.fcost < currentNode.fcost) currentNode = node;
            }
            searchNodes.Remove(currentNode);
            processedNodes.Add(currentNode);

            foreach (var neighbour in GetNeighbouringNodes(currentNode))
            {
                if (!neighbour.isWalkable || processedNodes.Contains(neighbour)) continue;
                //步长必须和启发/方向选择用同一套度量
                int newFCost = currentNode.fcost + CalculateCost(currentNode, neighbour);
                if (newFCost < neighbour.fcost)
                {
                    neighbour.SetFCost(newFCost);
                    if (!searchNodes.Contains(neighbour)) searchNodes.Add(neighbour);
                }
            }
        }

        BuildDirection();
        return true;
    }

    //按积分结果给每格选一个最省的邻格当方向
    private void BuildDirection()
    {
        foreach (var node in _nodes.Values)
        {
            node.SetDirection(Vector3.zero);
            //不可达的格子(fcost 还是 MaxValue)和被围死的格子没有方向
            if (!node.isWalkable || node.fcost == int.MaxValue) continue;

            FlowFieldNode best = null;
            foreach (var neighbour in GetNeighbouringNodes(node))
            {
                if (!neighbour.isWalkable || neighbour.fcost == int.MaxValue) continue;
                //只朝代价更小的邻格走,否则目标格自己会被赋上朝外的方向,站上去反而会走开
                if (neighbour.fcost >= node.fcost) continue;
                if (IsBetterNeighbour(neighbour, best, node)) best = neighbour;
            }
            if (best != null)
            {
                //方向是格与格之间的,和 cellSize 无关,直接取单位向量
                node.SetDirection(new Vector3(best.x - node.x, 0, best.y - node.y).normalized);
            }
        }
    }

    //邻格够不够好:代价小的优先;代价相同时正交优先,免得贴墙走时在斜向和正交之间来回抖
    private bool IsBetterNeighbour(FlowFieldNode candidate, FlowFieldNode current, FlowFieldNode from)
    {
        if (current == null) return true;
        if (candidate.fcost != current.fcost) return candidate.fcost < current.fcost;
        return IsDiagonal(current, from) && !IsDiagonal(candidate, from);
    }

    //得到目标格的方向,不可达返回零
    public Vector3 GetDirection(int x, int y)
    {
        FlowFieldNode node = GetNode(x, y);
        if (node == null) return Vector3.zero;

        //站在障碍上时借用可走邻格的方向,免得完全卡死
        if (!node.isWalkable)
        {
            foreach (var neighbour in GetNeighbouringNodes(node))
            {
                if (neighbour.isWalkable && neighbour.direction != Vector3.zero) return neighbour.direction;
            }
            return Vector3.zero;
        }
        return node.direction;
    }

    //判断格子地形是否可走
    public bool IsWalkable(int x, int y)
    {
        return GetNode(x, y)?.isWalkable ?? false;
    }

    //得到相邻的格子
    private List<FlowFieldNode> GetNeighbouringNodes(FlowFieldNode node)
    {
        List<FlowFieldNode> neighbours = new List<FlowFieldNode>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                FlowFieldNode neighbour = GetNode(node.x + i, node.y + j);
                if (neighbour == null) continue;
                //斜向移动要求两条正交边都能走,否则会从墙角穿过去
                if (i != 0 && j != 0
                    && (!IsWalkable(node.x + i, node.y) || !IsWalkable(node.x, node.y + j))) continue;
                neighbours.Add(neighbour);
            }
        }
        return neighbours;
    }

    //按坐标取格子,越界返回 null
    private FlowFieldNode GetNode(int x, int y)
    {
        if (x < 0 || x >= _xSize || y < 0 || y >= _ySize) return null;
        return _nodes.TryGetValue(Index(x, y), out var node) ? node : null;
    }

    //两个格子是不是斜向相邻
    private bool IsDiagonal(FlowFieldNode a, FlowFieldNode b)
    {
        return a.x != b.x && a.y != b.y;
    }

    //相邻两格的步长代价,和 A* 用同一套:正交 10,斜向 14
    private int CalculateCost(FlowFieldNode from, FlowFieldNode to)
    {
        int dx = Mathf.Abs(from.x - to.x);
        int dy = Mathf.Abs(from.y - to.y);
        int min = Mathf.Min(dx, dy);
        int max = Mathf.Max(dx, dy);
        return 14 * min + 10 * (max - min);
    }
}
