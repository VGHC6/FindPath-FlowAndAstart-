using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
// 地图烘焙工具，把地图烘培成网格地图
public class BakeMapCreate : EditorWindow
{
    public static BakeMapCreate _window;
    [SerializeField] private GameObject _mapPrefab;//地图预制体
    [SerializeField] private float _cellSize = 1;//格子大小
    [SerializeField] private int _obstacleLayer;//不可走区域层掩码
    [SerializeField] private int _obstaclaHeight = 1;//可走区域高度，用于判断是否可走

    private List<GameObject> _childrens = new List<GameObject>();//地图子物体列表

    //地图需要的属性
    private int _xSize;//地图宽度
    private int _ySize;//地图高度
    private Vector3 _origin = Vector3.zero;//地图原点位置

    //文件存放路径
    private const string ModelDir = "Assets/Scripts/Model";
    private const string ModelPath = "Assets/Scripts/Model/GridMapModel.cs";
    //临时模板路径，用于生成地图模型
    private const string TemplatePath = "Assets/Editor/MapBaker/Templates/GridMapModel.template.txt";
    //地图资产目录
    private const string AssetDir = "Assets/Resource/GridMaps";


    [MenuItem("Tools/地图烘焙")]
    private static void AssetBakeMapCreate()
    {
        _window = GetWindow<BakeMapCreate>(false, "烘焙地图", false);
        _window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("地图烘培工具");
        _mapPrefab = EditorGUILayout.ObjectField(_mapPrefab, typeof(GameObject), true) as GameObject;
        _cellSize = EditorGUILayout.FloatField("网格大小 ", _cellSize);
        _obstacleLayer = EditorGUILayout.LayerField("不可走图层 ", _obstacleLayer);
        _obstaclaHeight = EditorGUILayout.IntField("可走区域高度", _obstaclaHeight);

        if (GUILayout.Button("烘焙地图"))
        {
            //得到地图大小
            GetMapSize();
            if (_xSize <= 0 || _ySize <= 0) { return; }
            //地图烘焙
            MapBake();
            //生成地图文件
            GenerateMapFile();
            EditorGUILayout.LabelField($"地图烘焙完成,{_xSize}*{_ySize}");
        }
    }

    //得到地图大小
    private void GetMapSize()
    {
        if (_mapPrefab == null)
        {
            Debug.LogError("请先选择地图预制体");
            return;
        }
        //得到地图子物体列表
        _childrens = _mapPrefab.GetComponentsInChildren<Transform>().Select(x => x.gameObject).ToList();

        Bounds maxBounds = new Bounds();
        bool isGetBounds = false;
        //找到最大的地图
        foreach (var child in _childrens)
        {
            //如果子物体没有碰撞组件，跳过
            if (!child.TryGetComponent(out Collider collider)) { continue; }
            if (!isGetBounds)
            {
                maxBounds = child.GetComponent<Collider>().bounds;
                isGetBounds = true;
            }
            else
            {
                if (child.GetComponent<Collider>().bounds.size.x > maxBounds.size.x && child.GetComponent<Collider>().bounds.size.z > maxBounds.size.z)
                {
                    maxBounds = child.GetComponent<Collider>().bounds;
                }
            }
        }
        //得到地图大小
        _xSize = Mathf.CeilToInt(maxBounds.size.x / _cellSize);
        _ySize = Mathf.CeilToInt(maxBounds.size.z / _cellSize);
        Debug.Log($"地图大小，宽度：{_xSize}，高度：{_ySize}");
        //得到地图原点位置  
        _origin = new Vector3(maxBounds.min.x, 0, maxBounds.min.z);
    }


    //生成地图文件
    private static void GenerateMapFile()
    {
        //生成地图模型
        if (!File.Exists(TemplatePath))
        {
            Debug.LogError($"模板文件不存在：{TemplatePath}");
            return;
        }
        //读取模板文件内容
        string content = File.ReadAllText(TemplatePath);
        content = content.Replace("\r\n", "\n").Replace("\n", "\r\n");

        //如果内容没变则不改
        if (File.Exists(ModelPath) && File.ReadAllText(ModelPath) == content)
        {
            Debug.Log($"地图模型文件未改变，无需生成");
            return;
        }

        //创建文件
        Directory.CreateDirectory(ModelDir);
        //创建并写入地图模型文件
        File.WriteAllText(ModelPath, content);
        AssetDatabase.Refresh();
        Debug.Log($"已生成：{ModelPath}");
    }

    //地图烘焙
    private void MapBake()
    {
        //创建格子数组
        int totalNodes = _xSize * _ySize;
        var walkables = new bool[totalNodes];
        for (int i = 0; i < totalNodes; i++)
        {
            walkables[i] = true;
        }

        int block = 0;
        var half = new Vector3(_cellSize * 0.45f, _obstaclaHeight * 0.5f, _cellSize * 0.45f);
        //遍历所有子物体
        for (int i = 0; i < _xSize; i++)
        {
            for (int j = 0; j < _ySize; j++)
            {
                Vector3 center = new Vector3(
                    _origin.x + (i + 0.5f) * _cellSize,
                    _origin.y + _obstaclaHeight,
                    _origin.z + (j + 0.5f) * _cellSize
                );
                if (!Physics.CheckBox(center, half, Quaternion.identity, 1 << _obstacleLayer)) continue;
                walkables[i * _ySize + j] = false;
                block++;
            }
        }
        WriteMapFile(walkables);
        Debug.Log($"烘焙完成：{_xSize}x{_ySize}，障碍 {block} 格，可行走 {totalNodes - block} 格");
    }

    //写入地图模型文件
    private void WriteMapFile(bool[] walkables)
    {
        Directory.CreateDirectory(AssetDir);
        //拼接地图模型文件路径
        string assestPath = $"{AssetDir}/{_mapPrefab.name}_GridMap.asset";
        //创建并写入地图模型文件
        var asset = AssetDatabase.LoadAssetAtPath<GridMapAsset>(assestPath);
        bool isExist = asset != null;
        if (!isExist)
        {
            asset = ScriptableObject.CreateInstance<GridMapAsset>();
        }
        asset._xSize = _xSize;
        asset._ySize = _ySize;
        asset._cellSize = _cellSize;
        asset._origin = _origin;
        asset._walkable = walkables;
        asset._bakedFrom = _mapPrefab.name;
        asset._bakedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (!isExist)
        {
            AssetDatabase.CreateAsset(asset, assestPath);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        AssetDatabase.Refresh();
        Debug.Log($"资产已写出：{assestPath}（{(isExist ? "新建" : "覆盖")}）");
    }
}



