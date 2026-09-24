using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyData
{
    public Vector3 pos;//敌人位置
    public int x, y;//敌人在网格中的位置
    public float speed;//敌人速度

    public EnemyData(Vector3 pos, int x, int y, float speed)
    {
        this.pos = pos;
        this.x = x;
        this.y = y;
        this.speed = speed;
    }
}

public interface IEnemyModel : IModel
{
    //得到敌人数量
    int Count { get; }
    //添加敌人,返回分配给它的 id(字典的键)。
    //id 必须和格坐标无关:敌人会走,走了出生格就还给别人,而格索引会被重复分配
    int AddEnemy(EnemyData enemyData);
    //得到敌人数据
    EnemyData GetEnemyData(int grid);
    //设置敌人位置
    void SetEnemyPos(Vector3 pos, int x, int y, int grid);
    //清除敌人 
    void ClearEnemy(int grid);
}

public class EnemyModel : IEnemyModel
{
    Dictionary<int, EnemyData> _enemies = new Dictionary<int, EnemyData>();//敌人字典
    //id 的自增源。不能拿 count 当 id:ClearEnemy 会让 count 回退,
    //回退之后发出的新 id 会和还活着的敌人撞号,那正是这次的崩溃
    int _nextId = 0;
    int count = 0;//敌人数量
    public int Count => count;
    public IAchitecture GetArchitecture() => mobaTest.Interface;

    public void Init()
    {

    }

    public void SetArchitecture(IAchitecture architecture) { }

    /// <summary>
    /// 添加敌人,返回分配给它的 id
    /// </summary>
    /// <param name="enemyData"></param>
    public int AddEnemy(EnemyData enemyData)
    {
        int id = _nextId++;
        _enemies.Add(id, enemyData);
        count++;
        return id;
    }

    //取不到返回 null,别用 _enemies[grid]——键不存在会抛 KeyNotFoundException
    public EnemyData GetEnemyData(int grid)
    {
        return _enemies.TryGetValue(grid, out var enemyData) ? enemyData : null;
    }

    public void SetEnemyPos(Vector3 pos, int x, int y, int grid)
    {
        _enemies[grid].pos = pos;
        _enemies[grid].x = x;
        _enemies[grid].y = y;
    }

    //清除敌人 
    public void ClearEnemy(int grid)
    {
        _enemies.Remove(grid);
        count--;
    }

 
}