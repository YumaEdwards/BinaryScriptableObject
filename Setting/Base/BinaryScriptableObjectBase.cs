#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Yuma.Editor.Bso
{
    /// <summary>
    /// バイナリでの読み書きをサポートする<see cref="ScriptableObject"/>の基本クラス<br />
    /// ※DLL化して他のUnityプロジェクト環境と共有を行う際に<see cref="ScriptableObject"/>によるシリアライズを行ないたい場合に本クラスをベースクラスとして使用してください。
    /// </summary>
    /// <typeparam name="ScriptableObjectT">バイナリでの読み書きをサポートする<see cref="ScriptableObject"/>となるクラス</typeparam>
    internal class BinaryScriptableObjectBase<ScriptableObjectT> : BinaryScriptableObjectCore
        where ScriptableObjectT : BinaryScriptableObjectBase<ScriptableObjectT>
    {
        //-----------------------------------------------------------------------------
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓バイナリ読み込み機能関連
        
        /// <summary>
        /// <see cref="BinaryReader"/>を使用してバイナリシリアライズファイルのデフォルト読み込みを行なう関数
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pOverwriteFieldValueReadAction">フィールド読み込み時の読み込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでフィールド値の読み込み動作を行なうようになる)</param>
        protected override void DefaultLoadBinary(
            BinaryReader     pReader,
            OnFieldValueRead pOverwriteFieldValueReadAction = null )
        {
            if ( pReader == null ) { return; }
            
            object               fClassInstance  = this as ScriptableObjectT;
            BinaryFieldValueType fFieldValueType = (BinaryFieldValueType)pReader.ReadUInt16();
            
            if ( fFieldValueType == BinaryFieldValueType.NULL || fFieldValueType != BinaryFieldValueType.CLASS ) { return; }
            
            LoadClass(pReader, ref fClassInstance, pOverwriteFieldValueReadAction);
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓バイナリ書き込み機能関連
        
        /// <summary>
        /// <see cref="BinaryWriter"/>を使用してバイナリシリアライズファイルへデフォルト書き込みを行なう関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pOverwriteFieldValueWriteAction">フィールド書き込み時の書き込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでフィールド値の書き込み動作を行なうようになる)</param>
        protected override void DefaultSaveBinary(
            BinaryWriter      pWriter,
            OnFieldValueWrite pOverwriteFieldValueWriteAction = null )
        {
            if ( pWriter == null ) { return; }
            
            SaveClass(pWriter, this as ScriptableObjectT, pOverwriteFieldValueWriteAction);
        }
    }
}
#endif
