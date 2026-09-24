using UnityEngine;

//显示网格地图
public class ShowGridGizoms : MonoBehaviour, IController
{
    private IGridMapModel _gridMapModel;
    public IAchitecture GetArchitecture() => mobaTest.Interface;
    public void SetArchitecture(IAchitecture architecture) { }


    private void OnDrawGizmosSelected()
    {
        if (_gridMapModel == null) _gridMapModel = this.GetModel<IGridMapModel>();
        int xs = _gridMapModel.XSize;
        int ys = _gridMapModel.YSize;
        float c = _gridMapModel.CellSize;
        if (xs <= 0 || ys <= 0 || c <= 0) return;

        Vector3 size = new Vector3(c, 0.02f, c);

        for (int x = 0; x < xs; x++)
        {
            for (int y = 0; y < ys; y++)
            {
                var node = _gridMapModel.GetNode(x, y);
                if (node == null) continue;

                if (node.occupant != 0)
                    Gizmos.color = new Color(0.2f, 0.4f, 1.0f, 0.60f);    // 蓝 = 被占用
                else if (node.isWalkable)
                    Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.30f);    // 绿 = 可走
                else
                    Gizmos.color = new Color(1.0f, 0.2f, 0.2f, 0.50f);    // 红 = 地形障碍

                //格心统一问模型要,别在这里另算一套 origin/cellSize,烘焙改了就会对不上
                Vector3 center = _gridMapModel.GridToWorld(x, y);
                center.y = 0.05f;
                Gizmos.DrawCube(center, size);
            }
        }
    }
}
