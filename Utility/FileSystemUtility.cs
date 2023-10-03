#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yuma.Editor.Bso
{
    internal static class FileSystemUtility
    {
        private static string _currentDirPath = "";//カレントディレクトリパス(「/」区切り)
        //-----------------------------------------------------------------------------
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓システム関連

        /// <summary>
        /// コンストラクタ(引数なし)
        /// </summary>
        static FileSystemUtility()
        {
            UpdateCurrentDirectory();
        }

        /// <summary>
        /// カレントディレクトリパスを更新する関数<br />
        /// ※カレントディレクトリを変更することは早々ないので、適宜更新する方式とする。
        /// </summary>
        public static void UpdateCurrentDirectory()
        {
            if ( !string.IsNullOrEmpty(_currentDirPath) && Directory.Exists(_currentDirPath) ){ return; }
            
            _currentDirPath = Directory.GetCurrentDirectory();
            _currentDirPath = _currentDirPath.Replace( "\\" , "/" );//「￥」を「／」に変換(Windows用)
            
            //Debug.Log($"[{typeof(FileSystemUtility)}.UpdateCurrentDirectory()]_Update current directory... [_currentDirPath:{_currentDirPath}]");
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓基本機能関連

        /// <summary>
        /// 相対パスに変換して返す関数
        /// </summary>
        /// <param name="pPath">変換したいパス(「/」区切りのパス指定推奨)</param>
        /// <returns>相対パスに変換した文字列</returns>
        public static string ToRelativePath( string pPath )
        {
            if ( string.IsNullOrEmpty(_currentDirPath) || !Directory.Exists(_currentDirPath) ) { UpdateCurrentDirectory(); }
            
            if ( pPath.Contains("\\") ) { pPath = pPath.Replace( "\\" , "/" ); }//※Windows対応。
            
            return (pPath.IndexOf(_currentDirPath) == -1)? pPath : pPath.Substring(_currentDirPath.Length + 1);
        }

        /// <summary>
        /// 指定したパスをファイル/ディレクトリ名に変換して返す関数
        /// </summary>
        /// <param name="pPath"                >ファイル/ディレクトリ名を取得したいパス</param>
        /// <param name="pIsUnifyPathDelimiter">パス区切り文字を統一する処理を行なうかどうか（trueで行なう）</param>
        /// <returns></returns>
        public static string ToNamePath( string pPath, bool pIsUnifyPathDelimiter = true )
        {
            if ( string.IsNullOrEmpty(pPath) ) { return string.Empty; }
            if ( pIsUnifyPathDelimiter && pPath.Contains("\\") ) { pPath = pPath.Replace( "\\" , "/" ); }//※Windows対応。
            
            int fLashDelimiterPos = pPath.LastIndexOf("/");//最後の区切り文字の位置
            
            return (fLashDelimiterPos >= 0)? pPath.Substring(fLashDelimiterPos + 1) : pPath;
        }
        
        /// <summary>
        /// 指定したディレクトリのセットアップを行なう関数<br />
        /// ※引数には準備を行いたいディレクトリへのフルパス又はAssetsから始まる相対パスを指定します。<br />
        /// 　それ以外のパスを指定すると、予期しない動作を起こすので注意してください。<br />
        /// </summary>
        /// <param name="pPath">初期化するディレクトリパス(フルパスorAssetsからの相対パス)</param>
        /// <returns>成功したかどうか(trueで成功)</returns>
        public static bool SetupDirectory( string pPath )
        {
            if ( string.IsNullOrEmpty(pPath) ) { return false; }//引数が無効な場合は何もしない。
            if ( Directory.Exists(pPath) ) { return true; }//既にフォルダが有る場合は何もしない。
            
            string   fAssetsPath      = ToRelativePath(pPath);//Assetsからの相対パス
            string[] fSplitOutputPath = fAssetsPath.Split('/');//フォルダ区切り文字で区切った出力先フォルダパス
            string   fLoopWorkPath    = "";//現在処理中のフォルダパス
            
            //Debug.Log($"[{typeof(FileSystemUtility)}.SetupDirectory()]_setup directories... [fAssetsPath:{fAssetsPath}]");
            
            for ( int i = 0 ; i < fSplitOutputPath.Length ; i++ ) {
                if ( fLoopWorkPath == "" ) {
                    if ( Directory.Exists(fSplitOutputPath[i]) ) { fLoopWorkPath = fSplitOutputPath[i]; continue; }
                    AssetDatabase.CreateFolder(fLoopWorkPath,fSplitOutputPath[i]);//フォルダを作成。
                    fLoopWorkPath = fSplitOutputPath[i];
                }else{
                    if ( Directory.Exists($"{fLoopWorkPath}/{fSplitOutputPath[i]}") ) { fLoopWorkPath = $"{fLoopWorkPath}/{fSplitOutputPath[i]}"; continue; }
                    AssetDatabase.CreateFolder(fLoopWorkPath,fSplitOutputPath[i]);//フォルダを作成。
                    fLoopWorkPath = $"{fLoopWorkPath}/{fSplitOutputPath[i]}";
                }
            }
            
            return true;
        }
        
    }
}
#endif
