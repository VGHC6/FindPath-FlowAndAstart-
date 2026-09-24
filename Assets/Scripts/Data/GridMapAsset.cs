using UnityEngine;

[CreateAssetMenu(fileName = "GridMap", menuName = "地图/格子地图资产")]
public class GridMapAsset : ScriptableObject
{
    public int _xSize;//地图宽度
    public int _ySize;//地图高度
    public Vector3 _origin;//地图原点位置
    public float _cellSize;//格子大小
    public bool[] _walkable;//可走区域，true为可走，false为不可走


    public string _bakedFrom;//地图来源
    public string _bakedAt;//地图生成时间
    //方法
    //根据x,y坐标获取索引
    public int Index(int x, int y) => x * _ySize + y;
    //判断是否在地图范围内
    public bool inbounds(int x, int y) => x >= 0 && x < _xSize && y >= 0 && y < _ySize;
    //判断是否可走
    public bool isWalkable(int x, int y) => _walkable[Index(x, y)];
}
