using UnityEngine;
namespace Core.MasterData 
{
    /// <summary>
    /// 1行のデータが必ずIDを持つことを保証する
    /// </summary>
    public interface IMasterData
    {
        public ulong Id { get; }
    }
}
