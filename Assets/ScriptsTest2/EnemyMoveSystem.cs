using UnityEngine;

//敌人移动系统:按流场方向推进敌人,并在换格的那一刻同步格子占用
//流场只烘静态地形,格子被谁占是动态的,所以"下一格能不能进"由这里判
public interface IEnemyMoveSystem : ISystem
{
    //推进一个敌人一帧。位置无效(敌人已被清掉)返回 false,调用方别拿 out 的值去赋值
    bool TryTick(int grid, float deltaTime, out Vector3 position,out int x,out int y);
}

public class EnemyMoveSystem : AbstractSystem, IEnemyMoveSystem
{
    //格子上敌方的占用编号。0 表示"没人占",所以编号不能取 0——格子索引 0 是(0,0)这个合法格子
    public const int EnemyOccupant = 1;

    private IEnemyModel _enemyModel;
    private IGridMapModel _gridMapModel;
    private IFlowFieldUtil _flowFieldUtil;

    protected override void OnInit()
    {
        _enemyModel = this.GetModel<IEnemyModel>();
        _gridMapModel = this.GetModel<IGridMapModel>();
        _flowFieldUtil = this.GetUtility<IFlowFieldUtil>();
    }

    public bool TryTick(int grid, float deltaTime, out Vector3 position, out int x, out int y)
    {
        position = Vector3.zero;
        EnemyData data = _enemyModel.GetEnemyData(grid);
        x = data.x;
        y = data.y;
        if (data == null) return false;
        position = data.pos;
        //流场方向:到目标格、不可达、或者地图没烘都返回零向量
        Vector3 direction = _flowFieldUtil.GetDirection(data.x, data.y);
        if (direction == Vector3.zero) return true;

        Vector3 next = data.pos + direction * data.speed * deltaTime;

        //还算在原格里:只更新连续位置,不动占用
        if (!_gridMapModel.WorldToGrid(next, out int cx, out int cy) || (cx == data.x && cy == data.y))
        {
            _enemyModel.SetEnemyPos(next, data.x, data.y, grid);
            position = next;
            return true;
        }

        //要换格了:先占新格,占到了再放旧格。顺序反了会有一帧两个格子都不占,别人就能插进来
        //新格被地形挡住或被别的敌人占了就原地停住(静态流场不做局部绕行)
        if (!_gridMapModel.TryClaim(cx, cy, EnemyOccupant)) return true;

        _gridMapModel.Release(data.x, data.y);
        _enemyModel.SetEnemyPos(next, cx, cy, grid);
        position = next;
        return true;
    }
}
