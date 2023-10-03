#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Yuma.Editor.Bso
{
    /// <summary>
    /// Unityのアセットデータ全般の情報(主にパス周り)を管理するクラス<br />
    /// ※アセットの存在状態に応じてパスとGUIDを交互に切り替えて管理します。<br />
    /// ※アセットが存在したりしなかったりする可能性がある場合は本クラスでパスとGUID周りの管理を行なってください。<br />
    /// ※本クラスはファイルとディレクトリ両対応です。
    /// </summary>
    internal class AssetInfo
    {
        private string    _assetGuid = string.Empty;//アセットのGUID※アセットが存在する場合に使用。
        private string    _assetPath = string.Empty;//アセットパス※アセットが存在しない場合に使用。
        private AssetType _assetType = AssetType.UNKNOWN;//パスが表現するアセットのタイプ
        //-----------------------------------------------------------------------------
        public string    assetGuid { get => (!string.IsNullOrEmpty(_assetGuid))? _assetGuid : AssetDatabase.AssetPathToGUID(_assetPath); }//アセットのGUID
        public string    assetPath { get => (!string.IsNullOrEmpty(_assetPath))? _assetPath : AssetDatabase.GUIDToAssetPath(_assetGuid); }//アセットパス
        public AssetType assetType { get => _assetType; }//パスが表現するアセットのタイプ
        public string    assetName { get => FileSystemUtility.ToNamePath(assetPath, false); }//アセット名(拡張子含む)
        //-----------------------------------------------------------------------------
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓固有列挙体関連
        
        /// <summary>
        /// パスが表現するアセットのタイプ
        /// </summary>
        public enum AssetType
        {
            UNKNOWN,  //不明なタイプ
            FILE,     //ファイルタイプ
            DIRECTORY,//ディレクトリタイプ
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓システム関連
        
        /// <summary>
        /// 標準コンストラクタ
        /// </summary>
        public AssetInfo() {}
        
        /// <summary>
        /// GUIDから初期化するコンストラクタ
        /// </summary>
        /// <param name="pGuid">初期設定とするGUID文字列（ファイル又はディレクトリ）</param>
        public AssetInfo( string pGuid ) => SetPathFromGuid(pGuid);
        
        /// <summary>
        /// アセットパスから初期化するコンストラクタ
        /// </summary>
        /// <param name="pAssetPath">初期設定とするアセットパス文字列（ファイル又はディレクトリ）</param>
        /// <param name="pDefaultType">デフォルトのアセットタイプ<br />※<paramref name="pAssetPath"/>のアセットが存在しない場合にこの設定のアセットとして登録します。</param>
        public AssetInfo( string pAssetPath, AssetType pDefaultType = AssetType.FILE ) => SetPath(pAssetPath, pDefaultType);
        
        /// <summary>
        /// <see cref="FileInfo"/>から初期化するコンストラクタ
        /// </summary>
        /// <param name="pFile">初期設定に使用する<see cref="FileInfo"/></param>
        public AssetInfo( FileInfo pFile ) => SetPath(pFile);
        
        /// <summary>
        /// <see cref="pDirectory"/>から初期化するコンストラクタ
        /// </summary>
        /// <param name="pDirectory">初期設定に使用する<see cref="DirectoryInfo"/></param>
        public AssetInfo( DirectoryInfo pDirectory ) => SetPath(pDirectory);
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓基本機能関連
        
        /// <summary>
        /// メンバ変数の設定が不正な状態かどうかを返す関数
        /// </summary>
        /// <returns>trueで不正な状態</returns>
        private bool IsInvalidSetting()
        {
            if ( string.IsNullOrEmpty(_assetGuid) && string.IsNullOrEmpty(_assetPath) ) { return true; }
            
            return false;
        }
        
        /// <summary>
        /// クラス設定が空状態(初期状態)かどうかを返す関数
        /// </summary>
        /// <returns>trueで空状態</returns>
        public bool IsEmpty() => IsInvalidSetting();
        
        /// <summary>
        /// 管理モードの状態更新を行なう関数<br />
        /// ※登録しているアセットを新規で作成したり、削除した際に呼んでください。
        /// </summary>
        public void Refresh()
        {
            if ( IsInvalidSetting() ) { return; }
            
            string fAssetGuid = assetGuid;//アセットのGUID※アセットが存在しない場合はEmptyになると思われ。
            string fAssetPath = assetPath;//アセットパス※こっちは必ず正しいのが取れるはず。
            
            if ( string.IsNullOrEmpty(fAssetPath) ) { Reset(); return; }
            
            if ( File.Exists(fAssetPath) )
            {
                _assetGuid = fAssetGuid;
                _assetPath = string.Empty;
                _assetType = AssetType.FILE;
            }
            else if ( Directory.Exists(fAssetPath) )
            {
                _assetGuid = fAssetGuid;
                _assetPath = string.Empty;
                _assetType = AssetType.DIRECTORY;
            }
            else
            {
                _assetGuid = string.Empty;
                _assetPath = fAssetPath;
                //_assetType = AssetType.UNKNOWN;
            }
        }

        /// <summary>
        /// 登録されているアセットパスのファイルが存在するかどうかを返す関数
        /// </summary>
        /// <returns>trueで存在する</returns>
        public bool Exists()
        {
            if ( IsInvalidSetting() ) { return false; }
            
            switch (_assetType)
            {
                case AssetType.FILE:
                    return File.Exists(assetPath);
                case AssetType.DIRECTORY:
                    return Directory.Exists(assetPath);
            }
            
            return false;
        }
        
        /// <summary>
        /// ファイル又はディレクトリの作成を行なう関数<br />
        /// ※本関数でのファイル作成はプレーンなファイルの作成になります。
        /// </summary>
        public void Create()
        {
            if ( IsInvalidSetting() || Exists() ) { return; }
            if ( assetType == AssetType.UNKNOWN ) { return; }
            
            string fAssetPath = assetPath;//アセットパス※こっちは必ず正しいのが取れるはず。
            
            switch (_assetType)
            {
                case AssetType.FILE:
                    FileInfo fFile = new FileInfo(fAssetPath);
                    FileSystemUtility.SetupDirectory(fFile.Directory.FullName);
                    File.Create(fAssetPath).Close();
                    break;
                case AssetType.DIRECTORY:
                    DirectoryInfo fDirectory = new DirectoryInfo(fAssetPath);
                    FileSystemUtility.SetupDirectory(fDirectory.Parent.FullName);
                    Directory.CreateDirectory(fAssetPath);
                    break;
            }
            
            AssetDatabase.Refresh();
            Refresh();
        }
        
        /// <summary>
        /// アセットファイルの作成を行なう関数<br />
        /// ※ディレクトリはこの関数では作成できません。<see cref="Create"/>関数を使用してください。
        /// </summary>
        /// <param name="pAssetObject">作成時に保存する内容を含むアセットインスタンス</param>
        /// <typeparam name="AssetObjectT">作成対象のアセット系クラス</typeparam>
        public void CreateAsset<AssetObjectT>( AssetObjectT pAssetObject )
            where AssetObjectT : UnityEngine.Object
        {
            if ( pAssetObject == null ) { return; }
            if ( IsInvalidSetting() || Exists() || assetType != AssetType.FILE ) { return; }
            
            string   fAssetPath = assetPath;//アセットパス※こっちは必ず正しいのが取れるはず。
            FileInfo fAsset     = new FileInfo(fAssetPath);
            
            FileSystemUtility.SetupDirectory(fAsset.Directory.FullName);
            AssetDatabase.CreateAsset(pAssetObject, fAssetPath);
            
            Refresh();
        }

        /// <summary>
        /// アセットファイルを直接開く関数<br />
        /// ※ファイル以外は本関数を実行しても何も起こりません。<br />
        /// ※本関数ではアセットファイルを直接開きます。
        /// 　<see cref="UnityEngine.Object"/>型のクラスに紐づけて読み込みたい場合は<see cref="LoadAsset{AssetObjectT}"/>を使用してください。
        /// </summary>
        /// <param name="pIsAutoCreate">ファイルが存在しない場合に新規作成を行なうかどうか(trueで行なう)</param>
        /// <param name="pFileAccess">ファイルアクセスの種類</param>
        /// <returns>開いたファイルの操作用Stream</returns>
        public FileStream Open( bool pIsAutoCreate = false, FileAccess pFileAccess = FileAccess.ReadWrite )
        {
            if ( IsInvalidSetting() || assetType != AssetType.FILE ) { return null; }
            
            if ( !Exists() )
            {
                if ( !pIsAutoCreate ) { return null; }
                Create();
            }
            
            switch (pFileAccess)
            {
                case FileAccess.Read:
                    return File.OpenRead(assetPath);
                case FileAccess.Write:
                    return File.OpenWrite(assetPath);
            }
            
            return File.Open(assetPath, FileMode.Open, FileAccess.ReadWrite);
        }
        
        /// <summary>
        /// アセットを読み込む関数<br />
        /// ※<see cref="UnityEngine.Object"/>型のクラスに紐づけて読み込みたい場合に本関数を使用してください。<br />
        /// ※独自アセットなど、直接ファイルを読み込みたい場合は<see cref="Open"/>関数を使用してください。
        /// </summary>
        /// <typeparam name="AssetObjectT">読み込むアセットのクラス型</typeparam>
        /// <returns>読み込んだアセットクラスのインスタンス</returns>
        public AssetObjectT LoadAsset<AssetObjectT>()
            where AssetObjectT : UnityEngine.Object
        {
            if ( IsInvalidSetting() || !Exists() ) { return null; }
            if ( assetType == AssetType.UNKNOWN ) { return null; }
            
            return AssetDatabase.LoadAssetAtPath<AssetObjectT>(assetPath);
        }
        
        /// <summary>
        /// アセットを削除する関数
        /// </summary>
        public void Delete()
        {
            if ( IsInvalidSetting() || !Exists() ) { return; }
            if ( assetType == AssetType.UNKNOWN ) { return; }
            
            string fAssetPath     = assetPath;//アセットパス※こっちは必ず正しいのが取れるはず。
            string fAssetMetaPath = $"{fAssetPath}{FileExtName.META}";//アセットのmetaファイルパス
            
            switch (_assetType)
            {
                case AssetType.FILE:
                    File.Delete(fAssetPath);
                    break;
                case AssetType.DIRECTORY:
                    Directory.Delete(fAssetPath, true);
                    break;
            }
            
            if ( File.Exists(fAssetMetaPath) ) { File.Delete(fAssetMetaPath); }
            
            SetPath(fAssetPath, _assetType);
            
            AssetDatabase.Refresh();
            Refresh();
        }
        
        /// <summary>
        /// 文字列変換関数<br />
        /// ※ファイルパスを返します。
        /// </summary>
        /// <returns>変換した際の文字列</returns>
        public override string ToString() => assetPath;
        
        /// <summary>
        /// メンバ変数の状態を初期状態にリセットする関数
        /// </summary>
        public void Reset()
        {
            _assetGuid = _assetPath = string.Empty;
            _assetType = AssetType.UNKNOWN;
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓アクセサ関連
        
        /// <summary>
        /// アセットパスをGUIDから設定する関数
        /// </summary>
        /// <param name="pGuid">設定するGUID文字列（ファイル又はディレクトリ）</param>
        public void SetPathFromGuid( string pGuid )
        {
            if ( string.IsNullOrEmpty(pGuid) ) { return; }
            
            string fAssetPath = AssetDatabase.GUIDToAssetPath(pGuid);
            
            if ( string.IsNullOrEmpty(fAssetPath) ) { return; }
            
            _assetGuid = pGuid;
            _assetPath = string.Empty;
            _assetType = (AssetDatabase.IsValidFolder(fAssetPath))? AssetType.DIRECTORY : AssetType.FILE;
        }

        /// <summary>
        /// アセットパスを設定する関数<br />
        /// ※パスは「Assets」以下を示している必要があります。
        /// </summary>
        /// <param name="pAssetPath">設定するアセットパス文字列（ファイル又はディレクトリ）</param>
        /// <param name="pDefaultType">デフォルトのアセットタイプ<br />※<paramref name="pAssetPath"/>のアセットが存在しない場合にこの設定のアセットとして登録します。</param>
        public void SetPath( string pAssetPath, AssetType pDefaultType = AssetType.FILE )
        {
            if ( string.IsNullOrEmpty(pAssetPath) || pDefaultType == AssetType.UNKNOWN ) { return; }
            
            if ( File.Exists(pAssetPath) ) { SetPath(new FileInfo(pAssetPath)); return; }
            if ( Directory.Exists(pAssetPath) ) { SetPath(new DirectoryInfo(pAssetPath)); return; }
            
            FileInfo fAsset = new FileInfo(pAssetPath);
            
            if ( !fAsset.FullName.StartsWith(Directory.GetCurrentDirectory()) ) { return; }
            
            _assetGuid = string.Empty;
            _assetPath = fAsset.FullName.Substring(Directory.GetCurrentDirectory().Length + 1).Replace("\\","/");
            _assetType = pDefaultType;
        }
        
        /// <summary>
        /// アセットパスを<see cref="FileInfo"/>から設定する関数<br />
        /// ※パスは「Assets」以下を示している必要があります。
        /// </summary>
        /// <param name="pFile">設定に使用する<see cref="FileInfo"/></param>
        public void SetPath( FileInfo pFile )
        {
            if ( pFile == null || !pFile.FullName.StartsWith(Directory.GetCurrentDirectory()) ) { return; }
            
            string fAssetPath     = pFile.FullName.Substring(Directory.GetCurrentDirectory().Length + 1).Replace("\\","/");//「Assets」フォルダからの相対アセットパス
            string fAssetMetaPath = $"{fAssetPath}{FileExtName.META}";
            
            if ( File.Exists(fAssetPath) )
            {
                if ( !File.Exists(fAssetMetaPath) ) { AssetDatabase.Refresh(); }
                
                _assetGuid = AssetDatabase.AssetPathToGUID(fAssetPath);
                _assetPath = string.Empty;
                _assetType = AssetType.FILE;
            }
            else
            {
                _assetGuid = string.Empty;
                _assetPath = fAssetPath;
                _assetType = AssetType.FILE;
            }
        }
        
        /// <summary>
        /// アセットパスを<see cref="DirectoryInfo"/>から設定する関数<br />
        /// ※パスは「Assets」以下を示している必要があります。
        /// </summary>
        /// <param name="pDirectory">設定に使用する<see cref="DirectoryInfo"/></param>
        public void SetPath( DirectoryInfo pDirectory )
        {
            if ( pDirectory == null || !pDirectory.FullName.StartsWith(Directory.GetCurrentDirectory()) ) { return; }
            
            string fAssetPath     = pDirectory.FullName.Substring(Directory.GetCurrentDirectory().Length + 1).Replace("\\","/");//「Assets」フォルダからの相対アセットパス
            string fAssetMetaPath = $"{fAssetPath}{FileExtName.META}";

            if ( Directory.Exists(fAssetPath) )
            {
                if ( !File.Exists(fAssetMetaPath) ) { AssetDatabase.Refresh(); }
                
                _assetGuid = AssetDatabase.AssetPathToGUID(fAssetPath);
                _assetPath = string.Empty;
                _assetType = AssetType.DIRECTORY;
            }
            else
            {
                _assetGuid = string.Empty;
                _assetPath = fAssetPath;
                _assetType = AssetType.DIRECTORY;
            }
        }
        
        /// <summary>
        /// <see cref="FileInfo"/>を生成して返す関数<br />
        /// ※アセットのタイプがファイルでない場合はnullを返します。
        /// </summary>
        /// <returns>生成した<see cref="FileInfo"/>(ファイルでない場合はnull)</returns>
        public FileInfo GetFileInfo()
        {
            if ( IsInvalidSetting() || assetType != AssetType.FILE ) { return null; }
            
            return new FileInfo(assetPath);
        }
        
        /// <summary>
        /// <see cref="DirectoryInfo"/>を生成して返す関数<br />
        /// ※アセットのタイプがディレクトリでない場合はnullを返します。
        /// </summary>
        /// <returns>生成した<see cref="DirectoryInfo"/>(ディレクトリでない場合はnull)</returns>
        public DirectoryInfo GetDirectoryInfo()
        {
            if ( IsInvalidSetting() || assetType != AssetType.DIRECTORY ) { return null; }
            
            return new DirectoryInfo(assetPath);
        }
    }
}
#endif
