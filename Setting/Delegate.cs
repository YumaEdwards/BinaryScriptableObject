#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;

namespace Yuma.Editor.Bso
{
    /// <summary>
    /// バイナリシリアライズファイルからフィールド値を読み込む時のイベント型<br />
    /// ※このイベントを使用して読み込み方法をカスタムする場合は書き込み時も同じくカスタマイズしてください。<br />
    /// ※カスタマイズする必要がない型であった場合は<see cref="BinaryScriptableObjectBase{ScriptableObjectT}.LoadFieldValue"/>を呼んでください。<br />
    /// </summary>
    /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
    /// <param name="pValue">読み込み対象のフィールドインスタンス(<paramref name="pReader"/>で読み込んだ内容をこの引数に設定します)</param>
    /// <param name="pValueType">読み込み対象フィールドの型情報</param>
    /// <returns>読み込みに成功したかどうか(trueで成功)</returns>
    internal delegate bool OnFieldValueRead( BinaryReader pReader, ref object pValue, Type pValueType );
    
    /// <summary>
    /// バイナリシリアライズファイルへフィールド値を書き込む時のイベント型<br />
    /// ※このイベントを使用して書き込み方法をカスタムする場合は読み込み時も同じくカスタマイズしてください。<br />
    /// ※カスタマイズする必要がない型であった場合は<see cref="BinaryScriptableObjectBase{ScriptableObjectT}.SaveFieldValue"/>を呼んでください。<br />
    /// </summary>
    /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
    /// <param name="pValue">書き込むフィールド値</param>
    /// <param name="pValueType">書き込むフィールドの型情報</param>
    internal delegate void OnFieldValueWrite( BinaryWriter pWriter, object pValue, Type pValueType );
}
#endif
