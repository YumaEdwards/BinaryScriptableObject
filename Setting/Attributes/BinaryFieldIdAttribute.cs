#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace Yuma.Editor.Bso
{
    /// <summary>
    /// <see cref="BinaryScriptableObjectBase{ScriptableObjectT}"/>で読み書きを行う際に使用するバイナリ上のフィールドIDナンバリング用属性<br />
    /// ※ほかのフィールドと重複が無いようにIDを割り振ってください。<br />
    /// ※一度決めたIDは変更しないでください。変更すると読み込めなくなります。<br />
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class BinaryFieldIdAttribute : Attribute
    {
        public ushort id = 0;//フィールドのID
        //-----------------------------------------------------------------------------
        
        /// <summary>
        /// 属性初期化（コンストラクタ）
        /// </summary>
        /// <param name="pId">設定するフィールドのID</param>
        public BinaryFieldIdAttribute( ushort pId )
        {
            id = pId;
        }
    }
}
#endif
