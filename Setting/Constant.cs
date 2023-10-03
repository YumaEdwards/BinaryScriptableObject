#if UNITY_EDITOR
namespace Yuma.Editor.Bso
{
    /// <summary>
    /// バイナリシリアライズ時のフィールド値の型タイプ
    /// </summary>
    internal enum BinaryFieldValueType : ushort
    {
        NULL,
        BOOL,
        CHAR,
        SBYTE,
        BYTE,
        SHORT,
        USHORT,
        INT,
        UINT,
        LONG,
        ULONG,
        FLOAT,
        DOUBLE,
        STRING,
        UNITY_OBJECT,// UnityEngine.Object
        ARRAY,// 「int[]」系の通常配列。
        LIST,
        DICTIONARY,
        CLASS,// ユーザー定義クラス系。
        ENUM, // ユーザー定義列挙値系。
    }
    
    /// <summary>
    /// バイナリシリアライズ時の列挙値の型タイプ
    /// </summary>
    internal enum BinaryEnumValueType : byte
    {
        SBYTE,
        BYTE,
        SHORT,
        USHORT,
        INT,
        UINT,
        LONG,
        ULONG,
    }
}
#endif
