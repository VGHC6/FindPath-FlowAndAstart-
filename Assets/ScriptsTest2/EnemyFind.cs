using UnityEngine;
using UnityEngine.InputSystem;
//操纵:右键在地图上放敌人,放完立刻寻路到 Target
public class InitController : MonoBehaviour, IController
{
    public IAchitecture GetArchitecture() => mobaTest.Interface;
    public void SetArchitecture(IAchitecture architecture) { }
    private StarterAssets _starterAssets;
    private IGridMapModel _gridMapModel;
    private IFindSystem _enemyFindSystem;
    private IEnemyModel _enemyModel;
    //地图数据
    [SerializeField] private GridMapAsset _gridMapAsset;
    [SerializeField] private GameObject EnemyPrefab;
    //流场的终点:敌人的共同目标点,烘一次流场就在 Awake 里以它的格子为中心
    [SerializeField] private GameObject _targetEnemy;

    void Awake()
    {
        _starterAssets = new StarterAssets();
        _gridMapModel = this.GetModel<IGridMapModel>();
        _enemyFindSystem = this.GetSystem<IFindSystem>();
        _enemyModel = this.GetModel<IEnemyModel>();
        _gridMapModel.Apply(_gridMapAsset);
        _enemyFindSystem.RebuildGrid(_targetEnemy);
    }

    void OnEnable()
    {
        _starterAssets.Player.MouseRightDown.performed += OnCreateEnemy;
        _starterAssets.Player.MouseLeftDown.performed += SelectTargetEnemy;
        _starterAssets.Enable();
    }

    void OnDisable()
    {
        _starterAssets.Player.MouseRightDown.performed -= OnCreateEnemy;
        _starterAssets.Player.MouseLeftDown.performed -= SelectTargetEnemy;
    }


    /// <summary>
    /// 创建敌人
    /// </summary>
    /// <param name="context"></param>
    private void OnCreateEnemy(InputAction.CallbackContext context)
    {
        //得到点击位置
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        //判断位置是否有敌人
        if (!_gridMapModel.WorldToGrid(hit.point, out int x, out int y))
        {
            Debug.Log($"点击位置不是地图上的点");
            return;
        }

        //先占格再创建。占格挡住的是"同一个格子上重复点击",它挡不住撞键:
        //敌人一走开出生格就会被 Release(EnemyMoveSystem 换格时干的),
        //那个格子可以再放敌人,而格索引会被重复使用,所以键不能用格索引
        if (!_gridMapModel.TryClaim(x, y, EnemyMoveSystem.EnemyOccupant))
        {
            Debug.Log($"点击位置不可走或已被占用 ({x},{y})");
            return;
        }

        //创建敌人。位置统一用格子中心,数据和 Transform 必须是同一个值,
        //否则寻路按格算、显示按点击点放,一动就会错开
        Vector3 pos = _gridMapModel.GridToWorld(x, y);
        GameObject enemy = Instantiate(EnemyPrefab, pos, Quaternion.identity);
        if (GameObject.Find("Enemies") is GameObject parent) enemy.transform.SetParent(parent.transform);

        //先入模型拿到 id 再 Bind:视图要靠这个 id 每帧去问系统要位置,而 id 只有 AddEnemy 知道
        int id = _enemyModel.AddEnemy(new EnemyData(pos, x, y, 5));

        //把视图和模型接起来:视图自己每帧去问系统要位置
        if (!enemy.TryGetComponent(out EnemyView view)) view = enemy.AddComponent<EnemyView>();
        view.Bind(id);
    }




    /// <summary>
    /// 移动敌人到目标位置
    /// </summary>
    /// <param name="context"></param>
    private void SelectTargetEnemy(InputAction.CallbackContext context)
    {
        //得到点击位置
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        //_targetEnemy = hit.collider.gameObject;

    }

}
