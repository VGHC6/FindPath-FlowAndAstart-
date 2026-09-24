//A*寻路工具类,单个寻路,使用IGridMapModel

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AStartNode
{
    public BaseNode node;//装载节点,存放是否可走和位置
    public int gCost { get; private set; }//当前节点到起始节点的路径成本
    public int hCost { get; private set; }//当前节点到目标节点的估计成本
    public int fCost => gCost + hCost;//当前节点的总成本,随g/h自动更新
    public AStartNode connect { get; private set; }//连接父节点,用于回溯路径使用
    //沿路径从本节点走往下一格的格子方向(单位步长,取值 -1/0/1)。
    //外面拿到路径后站在本节点上直接读它就知道往哪走,不用自己去减相邻两个节点的 x/y。
    //终点没有下一格,所以是 zero。
    public Vector2Int direction { get; private set; }
    public void SetG(int c) => gCost = c;
    public void SetH(int c) => hCost = c;
    public void SetConnect(AStartNode c) => connect = c;
    public void SetDirection(Vector2Int d) => direction = d;

    public AStartNode(BaseNode node)
    {
        this.node = node;
        ReSet();
    }

    public void ReSet()
    {
        gCost = int.MaxValue;
        hCost = 0;
        connect = null;
        //节点表是复用的,不清方向的话上一趟寻路的方向会留在这一趟没被访问到的节点上
        direction = Vector2Int.zero;
    }
}


public interface IAStartPathUtil : IUtility
{
    //传入地图尺寸和全部节点,初始化内部节点表
    int index(int x, int y);
    void init(int xSize, int ySize, BaseNode[] allNodes);
    public List<AStartNode> FindPath(BaseNode start, BaseNode end);
}

public class AStartPathUtil : IAStartPathUtil
{
    public IAchitecture GetArchitecture() => mobaTest.Interface;
    //节点列表
    private int _xSize;//地图宽度
    private int _ySize;//地图高度

    private readonly Dictionary<int, AStartNode> nodes = new Dictionary<int, AStartNode>();
    public int index(int x, int y) => x * _ySize + y;

    //初始化
    public void init(int xSize, int ySize, BaseNode[] allNodes)
    {
        if (allNodes == null || allNodes.Length < xSize * ySize)
        {
            Debug.LogError($"A*初始化失败:节点数组不完整");
            return;
        }
        _xSize = xSize;
        _ySize = ySize;
        nodes.Clear();
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                nodes[index(x, y)] = new AStartNode(allNodes[index(x, y)]);
            }
        }
    }


    /// <summary>
    /// 寻路
    /// </summary>
    /// <param name="start">起始节点</param>
    /// <param name="end">目标节点</param>
    /// <returns></returns>
    public List<AStartNode> FindPath(BaseNode start, BaseNode end)
    {
        if (start == null || end == null)
        {
            Debug.LogError($"起始节点或目标节点为空");
            return null;
        }
        if (!start.isWalkable || !end.isWalkable)
        {
            Debug.LogError($"起始节点或目标节点不可行走");
            return null;
        }
        if (nodes.Count == 0)
        {
            Debug.LogError($"A*未初始化");
            return null;
        }

        //每次寻路前重置全部节点的g/h/父节点,否则上一次的结果会污染这一次
        foreach (var n in nodes.Values) n.ReSet();

        var startNode = nodes[index(start.x, start.y)];
        var endNode = nodes[index(end.x, end.y)];
        startNode.SetG(0);
        startNode.SetH(CalculateCost(startNode, endNode));

        var searchNodes = new List<AStartNode>() { startNode };//待搜索节点
        var processedNodes = new List<AStartNode>();//已处理节点
        //找到f最小的节点为当前节点
        while (searchNodes.Any())
        {
            var currentNode = searchNodes[0];
            //找到f最小的，f相同取h小的
            foreach (var node in searchNodes)
            {
                if (node.fCost < currentNode.fCost || node.fCost == currentNode.fCost && node.hCost < currentNode.hCost)
                {
                    currentNode = node;
                }
            }
            //将当前节点添加到已处理节点中
            processedNodes.Add(currentNode);
            searchNodes.Remove(currentNode);

            //回溯
            if (currentNode.node == end)
            {
                return TracePath(startNode, currentNode);
            }
            foreach (var neighbor in GetNeighbouringNodes(currentNode))
            {
                if (!neighbor.node.isWalkable) continue;
                //起始节点本身被敌人占用,要能走出去;其他被占用的节点不可通行
                if (neighbor != startNode && neighbor != endNode && neighbor.node.occupant != 0) continue;
                if (processedNodes.Contains(neighbor)) continue;
                var inSearch = searchNodes.Contains(neighbor);
                //步长必须和启发函数用同一套度量,否则启发不可采纳
                var newGCost = currentNode.gCost + CalculateCost(currentNode, neighbor);
                if (!inSearch || newGCost < neighbor.gCost)
                {
                    neighbor.SetG(newGCost);
                    neighbor.SetH(CalculateCost(neighbor, endNode));
                    neighbor.SetConnect(currentNode);
                    if (!inSearch)
                    {
                        searchNodes.Add(neighbor);
                    }
                }
            }
        }
        Debug.LogError($"没有找到路径");
        return null;
    }


    //回溯路径
    private List<AStartNode> TracePath(AStartNode startNode, AStartNode endNode)
    {
        var path = new List<AStartNode>();
        for (var node = endNode; node != null; node = node.connect)
        {
            path.Add(node);
            if (node == startNode) break;
        }
        path.Reverse();
        //方向只在排好序以后算:这时 path[i+1] 才是真正的下一格。
        //如果在松弛阶段跟着 connect 一起设,拿到的是"从父格进到本格"的方向,
        //读的人还得再取 path[i+1] 才知道该往哪走,反而绕。
        for (int i = 0; i < path.Count - 1; i++)
        {
            path[i].SetDirection(new Vector2Int(
                path[i + 1].node.x - path[i].node.x,
                path[i + 1].node.y - path[i].node.y));
        }
        //最后一个节点保持 zero,表示已经到终点、没有下一格可走
        return path;
    }



    //得到当前节点的相邻节点
    private List<AStartNode> GetNeighbouringNodes(AStartNode node)
    {
        List<AStartNode> neighbours = new List<AStartNode>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                int x = node.node.x + i;
                int y = node.node.y + j;
                if (x < 0 || x >= _xSize || y < 0 || y >= _ySize) continue;
                //斜向移动要求两条正交边都能走,否则会从墙角穿过去
                if (i != 0 && j != 0
                    && (!IsWalkable(node.node.x + i, node.node.y) || !IsWalkable(node.node.x, node.node.y + j))) continue;
                neighbours.Add(nodes[index(x, y)]);
            }
        }
        return neighbours;
    }


    //只看地形是否可走,看占用
    private bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= _xSize || y < 0 || y >= _ySize) return false;
        return nodes[index(x, y)].node.isWalkable && nodes[index(x, y)].node.occupant == 0;
    }


    //计算
    private int CalculateCost(AStartNode from, AStartNode to)
    {
        int dx = Mathf.Abs(from.node.x - to.node.x);
        int dy = Mathf.Abs(from.node.y - to.node.y);
        int min = Mathf.Min(dx, dy);
        int max = Mathf.Max(dx, dy);
        return 14 * min + 10 * (max - min);
    }
}