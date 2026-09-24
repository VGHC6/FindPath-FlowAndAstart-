using UnityEngine;

//敌人视图:身上只挂一个 EnemyModel 的编号,每帧问系统要位置,自己不存任何寻路状态
public class EnemyView : MonoBehaviour, IController
{
    public IAchitecture GetArchitecture() => mobaTest.Interface;
    public void SetArchitecture(IAchitecture architecture) { }

    private IEnemyMoveSystem _enemyMoveSystem;
    private IGridMapModel _gridMapModel;

    //敌人编号,等于它在 EnemyModel 字典里的键,由 AddEnemy 分配。
    //它不是格坐标:敌人会走,走过的格子会被别人再用;-1 表示还没接上数据
    public int Id { get; private set; } = -1;

    void Awake()
    {
        _enemyMoveSystem = this.GetSystem<IEnemyMoveSystem>();
        _gridMapModel = this.GetModel<IGridMapModel>();
    }

    //创建敌人的流程调用一次,把视图和数据接起来
    public void Bind(int id)
    {
        Id = id;
    }

    void FixedUpdate()
    {
        if (Id < 0 || _enemyMoveSystem == null) return;
        if (_enemyMoveSystem.TryTick(Id, Time.deltaTime, out Vector3 position, out int x, out int y))
        {
            transform.position = _gridMapModel.GridToWorld(x, y);
        }
    }
}
