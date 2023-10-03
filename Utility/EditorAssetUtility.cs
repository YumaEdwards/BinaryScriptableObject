#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Yuma.Editor.Bso
{
    internal static class EditorAssetUtility
    {
        //-----------------------------------------------------------------------------
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓基本機能関連
        
        /// <summary>
        /// ファイル名からアセットを読み込む関数<br />
        /// ※同名が複数あるとどれを読み込むかは分かりません。
        /// </summary>
        /// <param name="pFileName">読み込むアセットのファイル名(拡張子なし)</param>
        /// <param name="pExtensionName">読み込むアセットの拡張子(「.」から始まる拡張子)</param>
        /// <typeparam name="AssetObjectT">読み込むアセットの型</typeparam>
        /// <returns>読み込んだアセットのインスタンス(読み込めなかった場合はnull)</returns>
        public static AssetObjectT LoadAssetAtFileName<AssetObjectT>( string pFileName, string pExtensionName )
            where AssetObjectT : UnityEngine.Object
        {
            if ( string.IsNullOrEmpty(pFileName) || string.IsNullOrEmpty(pExtensionName) ) { return null; }
            
            string[] fResultGuids = AssetDatabase.FindAssets(pFileName);//検索結果のGUIDリスト
            string   fFilePath    = "";//ファイルパス
            int      fFileExtPos  = -1;//拡張子の位置
            string   fFileExt     = "";//拡張子
            
            foreach (string fFileGuid in fResultGuids)
            {
                fFilePath   = AssetDatabase.GUIDToAssetPath(fFileGuid);
                fFileExtPos = fFilePath.LastIndexOf(".");
                fFileExt    = (fFileExtPos >= 0)? fFilePath.Substring(fFileExtPos) : string.Empty;
                
                if ( fFileExt == pExtensionName )
                {
                    return AssetDatabase.LoadAssetAtPath<AssetObjectT>(fFilePath);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// ファイル名からアセットを読み込む関数(Typeクラス指定版)<br />
        /// ※同名が複数あるとどれを読み込むかは分かりません。
        /// </summary>
        /// <param name="pFileName">読み込むアセットのファイル名(拡張子なし)</param>
        /// <param name="pExtensionName">読み込むアセットの拡張子(「.」から始まる拡張子)</param>
        /// <param name="pAssetType"><see cref="UnityEngine.Object"/>を継承した型情報</param>
        /// <returns>読み込んだアセットのインスタンス(読み込めなかった場合はnull)</returns>
        public static UnityEngine.Object LoadAssetAtFileName( string pFileName, string pExtensionName, Type pAssetType )
        {
            if ( string.IsNullOrEmpty(pFileName) || string.IsNullOrEmpty(pExtensionName) ) { return null; }
            if ( pAssetType == null || !pAssetType.ContainsType<UnityEngine.Object>() ) { return null; }
            
            string[] fResultGuids = AssetDatabase.FindAssets(pFileName);//検索結果のGUIDリスト
            string   fFilePath    = "";                                 //ファイルパス
            int      fFileExtPos  = -1;                                 //拡張子の位置
            string   fFileExt     = "";                                 //拡張子
            
            foreach (string fFileGuid in fResultGuids)
            {
                fFilePath   = AssetDatabase.GUIDToAssetPath(fFileGuid);
                fFileExtPos = fFilePath.LastIndexOf(".");
                fFileExt    = (fFileExtPos >= 0)? fFilePath.Substring(fFileExtPos) : string.Empty;
                
                if ( fFileExt == pExtensionName )
                {
                    return AssetDatabase.LoadAssetAtPath(fFilePath, pAssetType);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// ファイル名から設定アセットを読み込む関数<br />
        /// ※同名が複数あるとどれを読み込むかは分かりません。
        /// </summary>
        /// <param name="pFileName">読み込む設定アセットのファイル名(拡張子なし)</param>
        /// <typeparam name="AssetObjectT">読み込む設定アセットの型</typeparam>
        /// <returns>読み込んだ設定アセットのインスタンス(読み込めなかった場合はnull)</returns>
        public static AssetObjectT LoadSettingAssetAtFileName<AssetObjectT>( string pFileName )
            where AssetObjectT : ScriptableObject
        {
            if ( string.IsNullOrEmpty(pFileName) ) { return null; }
            
            AssetObjectT fRetData = null;//返すデータ
            //-------------------------------------------------------------
            string[] fResultGuids = AssetDatabase.FindAssets(pFileName);//検索結果のGUIDリスト
            string   fFilePath    = "";//ファイルパス
            int      fFileExtPos  = -1;//拡張子の位置
            string   fFileExt     = "";//拡張子
            
            for ( int i = 0; i < fResultGuids.Length; i++ )
            {
                fFilePath   = AssetDatabase.GUIDToAssetPath(fResultGuids[i]);
                fFileExtPos = fFilePath.LastIndexOf(".");
                fFileExt    = (fFileExtPos >= 0)? fFilePath.Substring(fFileExtPos) : string.Empty;
                
                if ( fFileExt == FileExtName.SCRIPTABLE_OBJECT )
                {
                    fRetData = AssetDatabase.LoadAssetAtPath<AssetObjectT>(fFilePath);
                    if ( fRetData != null && fRetData is BinaryScriptableObjectCore )
                    {
                        BinaryScriptableObjectCore fBinaryScriptableObject = fRetData as BinaryScriptableObjectCore;
                        fBinaryScriptableObject.SetFileFromPath(fFilePath.Replace(fFileExt, FileExtName.SETTING));
                    }
                    break;
                }
                if ( fFileExt == FileExtName.SETTING )
                {
                    fRetData = ScriptableObject.CreateInstance<AssetObjectT>();
                    if ( fRetData is BinaryScriptableObjectCore )
                    {
                        BinaryScriptableObjectCore fBinaryScriptableObject = fRetData as BinaryScriptableObjectCore;
                        fBinaryScriptableObject.LoadBinary(fFilePath);
                        break;
                    }
                }
            }
            
            return fRetData;
        }
        
        /// <summary>
        /// ファイル名から設定アセットを読み込む関数(Typeクラス指定版)<br />
        /// ※同名が複数あるとどれを読み込むかは分かりません。
        /// </summary>
        /// <param name="pFileName">読み込む設定アセットのファイル名(拡張子なし)</param>
        /// <param name="pAssetType"><see cref="ScriptableObject"/>を継承した型情報</param>
        /// <returns>読み込んだ設定アセットのインスタンス(読み込めなかった場合はnull)</returns>
        public static ScriptableObject LoadSettingAssetAtFileName( string pFileName, Type pAssetType )
        {
            if ( string.IsNullOrEmpty(pFileName) || pAssetType == null || !pAssetType.ContainsType<ScriptableObject>() ) { return null; }
            
            ScriptableObject fRetData = null;//返すデータ
            //-------------------------------------------------------------
            string[] fResultGuids = AssetDatabase.FindAssets(pFileName);//検索結果のGUIDリスト
            string   fFilePath    = "";                                 //ファイルパス
            int      fFileExtPos  = -1;                                 //拡張子の位置
            string   fFileExt     = "";                                 //拡張子
            
            for ( int i = 0; i < fResultGuids.Length; i++ )
            {
                fFilePath   = AssetDatabase.GUIDToAssetPath(fResultGuids[i]);
                fFileExtPos = fFilePath.LastIndexOf(".");
                fFileExt    = (fFileExtPos >= 0)? fFilePath.Substring(fFileExtPos) : string.Empty;
                
                if ( fFileExt == FileExtName.SCRIPTABLE_OBJECT )
                {
                    fRetData = AssetDatabase.LoadAssetAtPath(fFilePath, pAssetType) as ScriptableObject;
                    if ( fRetData != null && fRetData is BinaryScriptableObjectCore )
                    {
                        BinaryScriptableObjectCore fBinaryScriptableObject = fRetData as BinaryScriptableObjectCore;
                        fBinaryScriptableObject.SetFileFromPath(fFilePath.Replace(fFileExt, FileExtName.SETTING));
                    }
                    break;
                }
                if ( fFileExt == FileExtName.SETTING )
                {
                    fRetData = ScriptableObject.CreateInstance(pAssetType);
                    if ( fRetData is BinaryScriptableObjectCore )
                    {
                        BinaryScriptableObjectCore fBinaryScriptableObject = fRetData as BinaryScriptableObjectCore;
                        fBinaryScriptableObject.LoadBinary(fFilePath);
                        break;
                    }
                }
            }
            
            return fRetData;
        }
        
        /// <summary>
        /// ファイルパスから設定アセットを読み込む関数<br />
        /// ※「ScriptableObject」か「バイナリシリアライズファイル」の拡張子を含めた形で指定します。
        /// </summary>
        /// <param name="pFilePath">読み込む設定アセットのファイルパス(拡張子付き)</param>
        /// <typeparam name="AssetObjectT">読み込む設定アセットの型</typeparam>
        /// <returns>読み込んだ設定アセットのインスタンス(読み込めなかった場合はnull)</returns>
        public static AssetObjectT LoadSettingAssetAtFilePath<AssetObjectT>( string pFilePath )
            where AssetObjectT : ScriptableObject
        {
            if ( string.IsNullOrEmpty(pFilePath) ) { return null; }
            if ( !pFilePath.EndsWith(FileExtName.SCRIPTABLE_OBJECT) && !pFilePath.EndsWith(FileExtName.SETTING) ) { return null; }
            
            AssetObjectT fRetData = null;//返すデータ
            //-------------------------------------------------------------
            int      fFileExtPos     = pFilePath.LastIndexOf(".");//拡張子の位置
            string   fFileExt        = (fFileExtPos >= 0)? pFilePath.Substring(fFileExtPos) : string.Empty;//拡張子
            string   fFilePath       = (string.IsNullOrEmpty(fFileExt))? $"{pFilePath}{FileExtName.SCRIPTABLE_OBJECT}" : pFilePath.Replace(fFileExt, FileExtName.SCRIPTABLE_OBJECT);//通常の設定アセットのファイルパス
            string   fBinaryFilePath = (string.IsNullOrEmpty(fFileExt))? $"{pFilePath}{FileExtName.SETTING}" : pFilePath.Replace(fFileExt, FileExtName.SETTING);//バイナリ形式の設定アセットのファイルパス
            
            if ( File.Exists(fFilePath) )
            {
                //↓通常の設定アセットのファイルがある場合
                fRetData = AssetDatabase.LoadAssetAtPath<AssetObjectT>(pFilePath);
                if ( fRetData != null && fRetData is BinaryScriptableObjectCore )
                {
                    BinaryScriptableObjectCore fBinaryScriptableObject = fRetData as BinaryScriptableObjectCore;
                    fBinaryScriptableObject.SetFileFromPath(pFilePath.Replace(FileExtName.SCRIPTABLE_OBJECT, FileExtName.SETTING));
                }
            }
            else if ( File.Exists(fBinaryFilePath) )
            {
                //↓バイナリ形式の設定アセットのファイルがある場合
                fRetData = ScriptableObject.CreateInstance<AssetObjectT>();
                if ( fRetData is BinaryScriptableObjectCore )
                {
                    BinaryScriptableObjectCore fBinaryScriptableObject = fRetData as BinaryScriptableObjectCore;
                    fBinaryScriptableObject.LoadBinary(fBinaryFilePath);
                }
                else { return null; }
            }
            
            return fRetData;
        }
        
        /// <summary>
        /// ファイルパスから設定アセットを読み込む関数(Typeクラス指定版)<br />
        /// ※「ScriptableObject」か「バイナリシリアライズファイル」の拡張子を含めた形で指定します。
        /// </summary>
        /// <param name="pFilePath">読み込む設定アセットのファイルパス(拡張子付き)</param>
        /// <param name="pAssetType"><see cref="ScriptableObject"/>を継承した型情報</param>
        /// <returns>読み込んだ設定アセットのインスタンス(読み込めなかった場合はnull)</returns>
        public static ScriptableObject LoadSettingAssetAtFilePath( string pFilePath, Type pAssetType )
        {
            if ( string.IsNullOrEmpty(pFilePath) || pAssetType == null || !pAssetType.ContainsType<ScriptableObject>() ) { return null; }
            if ( !pFilePath.EndsWith(FileExtName.SCRIPTABLE_OBJECT) && !pFilePath.EndsWith(FileExtName.SETTING) ) { return null; }
            
            ScriptableObject fRetData = null;//返すデータ
            //-------------------------------------------------------------
            int      fFileExtPos     = pFilePath.LastIndexOf(".");//拡張子の位置
            string   fFileExt        = (fFileExtPos >= 0)? pFilePath.Substring(fFileExtPos) : string.Empty;//拡張子
            string   fFilePath       = (string.IsNullOrEmpty(fFileExt))? $"{pFilePath}{FileExtName.SCRIPTABLE_OBJECT}" : pFilePath.Replace(fFileExt, FileExtName.SCRIPTABLE_OBJECT);//通常の設定アセットのファイルパス
            string   fBinaryFilePath = (string.IsNullOrEmpty(fFileExt))? $"{pFilePath}{FileExtName.SETTING}" : pFilePath.Replace(fFileExt, FileExtName.SETTING);//バイナリ形式の設定アセットのファイルパス
            
            if ( File.Exists(fFilePath) )
            {
                //↓通常の設定アセットのファイルがある場合
                fRetData = AssetDatabase.LoadAssetAtPath(pFilePath, pAssetType) as ScriptableObject;
                if ( fRetData != null && fRetData is BinaryScriptableObjectCore )
                {
                    BinaryScriptableObjectCore fBinaryScriptableObject = fRetData as BinaryScriptableObjectCore;
                    fBinaryScriptableObject.SetFileFromPath(pFilePath.Replace(FileExtName.SCRIPTABLE_OBJECT, FileExtName.SETTING));
                }
            }
            else if ( File.Exists(fBinaryFilePath) )
            {
                //↓バイナリ形式の設定アセットのファイルがある場合
                fRetData = ScriptableObject.CreateInstance(pAssetType);
                if ( fRetData is BinaryScriptableObjectCore )
                {
                    BinaryScriptableObjectCore fBinaryScriptableObject = fRetData as BinaryScriptableObjectCore;
                    fBinaryScriptableObject.LoadBinary(fBinaryFilePath);
                }
                else { return null; }
            }
            
            return fRetData;
        }
    }
}
#endif
