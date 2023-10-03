#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Yuma.Editor.Bso
{
    /// <summary>
    /// バイナリでの読み書きをサポートする<see cref="ScriptableObject"/>のコアクラス<br />
    /// ※主にジェネリックパラメータの情報がいらない系の処理をまとめる用。
    /// </summary>
    internal abstract class BinaryScriptableObjectCore : ScriptableObject
    {
        private const BindingFlags ALL_FIELD_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;//全フィールド情報取得用フラグ
        //-----------------------------------------------------------------------------
        private AssetInfo _assetInfo = new AssetInfo();//このバイナリシリアライズ機能で使用するファイル情報
        //-----------------------------------------------------------------------------
        public string binaryFileGuid { get => _assetInfo.assetGuid; }//このバイナリシリアライズ機能で使用するファイルのGUID
        public string binaryFilePath { get => _assetInfo.assetPath; }//このバイナリシリアライズ機能で使用するファイルパス
        //-----------------------------------------------------------------------------
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓データクラス関連
        
        /// <summary>
        /// ファイルヘッダー情報クラス構造体
        /// </summary>
        private class FileHeader
        {
            public const string MAGIC_NO = "BSO";//正しいマジックナンバー定数値
            //------------------------------------------------------------
            public char[] magicNo      = new char[3];//マジックナンバー
            public byte   majorVersion = 0;//メジャーバージョン
            public byte   minorVersion = 0;//マイナーバージョン
            public byte   buildVersion = 0;//ビルドバージョン
            public int    dataStartPos = 0;//データ格納開始位置
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓基本機能関連
        
        /// <summary>
        /// 指定したオブジェクトがシリアライズ可能なクラスかどうかを返す関数
        /// </summary>
        /// <param name="pChkObject">確認する対象</param>
        /// <returns>trueでシリアライズ可能なクラス</returns>
        private bool IsSerializableClass( object pChkObject )
            => pChkObject != null && pChkObject.GetType().IsClass &&
               (pChkObject.GetType().IsSerializable || pChkObject is ScriptableObject);
        
        /// <summary>
        /// 基底クラスがもう存在しない又はシリアライズ不要なレベルのクラスかどうかを確認して返す関数
        /// </summary>
        /// <param name="pChkType">確認する型情報</param>
        /// <returns>trueで基底クラスが存在しない又はシリアライズ不要なクラスに行き着いた</returns>
        private bool IsEndOfBaseType( Type pChkType )
            => pChkType.BaseType == null || pChkType.BaseType == typeof(object) ||
               pChkType.BaseType.ToString().Contains("BinaryScriptableObjectBase") ||
               pChkType.BaseType == typeof(BinaryScriptableObjectCore);
        
        /// <summary>
        /// <see cref="FileHeader"/>インスタンスの作成を行なう関数
        /// </summary>
        /// <returns>[NotNull]作成した<see cref="FileHeader"/>インスタンス</returns>
        private FileHeader CreateFileHeader()
        {
            FileHeader fFileHeader = new FileHeader();
            
            for (int i = 0; i < FileHeader.MAGIC_NO.Length; i++)
            {
                fFileHeader.magicNo[i] = FileHeader.MAGIC_NO[i];
            }
            
            fFileHeader.majorVersion = 1;
            fFileHeader.minorVersion = 0;
            fFileHeader.buildVersion = 0;
            fFileHeader.dataStartPos = (sizeof(char) * 3) + (sizeof(byte) * 3) + sizeof(int);//仮設定。保存処理時に正式に設定推奨。
            
            return fFileHeader;
        }
        
        /// <summary>
        /// <paramref name="pCreateType"/>で指定したタイプのオブジェクトインスタンスを作成して返す関数<br />
        /// ※配列やそれ以外のものも対応しています。
        /// </summary>
        /// <param name="pCreateType">作成するオブジェクトインスタンスの型</param>
        /// <returns>作成したオブジェクトインスタンス</returns>
        private object CreateFieldInstance( Type pCreateType )
        {
            if ( pCreateType == null ) { return default; }

            if ( pCreateType.IsArray )
            {
                return Array.CreateInstance(pCreateType.GetElementType(), new int[pCreateType.GetArrayRank()]);
            }
            if ( pCreateType.ContainsType<string>()    ) { return ""; }
            if ( pCreateType.ContainsType<Material>()  ) { return new Material(Shader.Find("UI/Default")); }
            if ( pCreateType.ContainsType<Texture2D>() ) { return new Texture2D(1, 1); }
            if ( pCreateType.ContainsType<ScriptableObject>() )
            {
                return ScriptableObject.CreateInstance(pCreateType);
            }
            
            return Activator.CreateInstance(pCreateType);
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓バイナリ読み込み機能関連
        
        /// <summary>
        /// ファイルパスからバイナリシリアライズファイルの読み込みを行なう関数<br />
        /// ※引数<paramref name="pFilePath"/>で指定したファイルパスの設定と同時に読み込みます。
        /// 　以降は引数のない、<see cref="LoadBinary()"/>や<see cref="SaveBinary()"/>で読み書きが可能になります。
        /// </summary>
        /// <param name="pFilePath">読み込むファイルのパス</param>
        public void LoadBinary( string pFilePath )
        {
            if ( string.IsNullOrEmpty(pFilePath) || !File.Exists(pFilePath) ) { return; }
            
            _assetInfo.SetPath(pFilePath);
            LoadBinary();
        }
        
        /// <summary>
        /// ファイル情報からバイナリシリアライズファイルの読み込みを行なう関数<br />
        /// ※引数<paramref name="pFile"/>で指定したファイル情報の設定と同時に読み込みます。
        /// 　以降は引数のない、<see cref="LoadBinary()"/>や<see cref="SaveBinary()"/>で読み書きが可能になります。
        /// </summary>
        /// <param name="pFile">読み込むファイル情報(<see cref="FileInfo"/>)</param>
        public void LoadBinary( FileInfo pFile )
        {
            if ( pFile == null || !File.Exists(pFile.FullName) ) { return; }
            
            _assetInfo.SetPath(pFile);
            LoadBinary();
        }
        
        /// <summary>
        /// バイナリシリアライズファイルの読み込みを行なう関数
        /// </summary>
        public void LoadBinary()
        {
            if ( !_assetInfo.Exists() ) { return; }
            
            BinaryReader fReader     = new BinaryReader(_assetInfo.Open(false, FileAccess.Read), Encoding.UTF8);
            FileHeader   fFileHeader = new FileHeader();
            
            fReader.BaseStream.Position = 0;
            
            if ( LoadHeader(fReader, ref fFileHeader) )
            {
                fReader.BaseStream.Position = fFileHeader.dataStartPos;//データ本体情報読み取り開始位置に移動。※現状のデータ仕様では不要だが、将来用に実装。
                OnLoadBinary(fReader);
            }
            else { EditorLog.Err($"[{GetType().Name}.LoadBinary()]_ファイルヘッダーの読み込みに失敗しました。バイナリ設定ファイルが正しいデータ形式で保存されているかご確認ください。"); }
            
            fReader.Close();
        }
        
        /// <summary>
        /// <see cref="BinaryReader"/>を使用してバイナリシリアライズファイルのデフォルト読み込みを行なう関数
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pOverwriteFieldValueReadAction">フィールド読み込み時の読み込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでフィールド値の読み込み動作を行なうようになる)</param>
        protected abstract void DefaultLoadBinary(
            BinaryReader     pReader,
            OnFieldValueRead pOverwriteFieldValueReadAction = null );
        
        /// <summary>
        /// ファイルヘッダーを読み込む関数
        /// </summary>
        /// <param name="pReader"></param>
        /// <param name="pFileHeader"></param>
        /// <returns>読み込みに成功したかどうか(trueで成功)</returns>
        private bool LoadHeader( BinaryReader pReader, ref FileHeader pFileHeader )
        {
            if ( pReader == null || pFileHeader == null ) { return false; }

            for (int i = 0; i < FileHeader.MAGIC_NO.Length; i++)
            {
                pFileHeader.magicNo[i] = pReader.ReadChar();
                if ( pFileHeader.magicNo[i] != FileHeader.MAGIC_NO[i] ) { return false; }
            }

            pFileHeader.majorVersion = pReader.ReadByte();
            pFileHeader.minorVersion = pReader.ReadByte();
            pFileHeader.buildVersion = pReader.ReadByte();
            pFileHeader.dataStartPos = pReader.ReadInt32();
            
            //EditorLog.Inf($"[{GetType().Name}.LoadHeader()]_ファイルヘッダー読み込み完了。([Ver.{pFileHeader.majorVersion}.{pFileHeader.minorVersion}.{pFileHeader.buildVersion}][pFileHeader.dataStartPos:{pFileHeader.dataStartPos}][pReader.BaseStream.Position:{pReader.BaseStream.Position}])");
            
            return true;
        }
        
        /// <summary>
        /// 指定した<see cref="BinaryReader"/>からクラスのシリアライズフィールドへデータを読み込む関数
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pClassInstance">読み込むクラスのインスタンス</param>
        /// <param name="pOverwriteFieldValueReadAction">フィールド読み込み時の読み込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでフィールド値の読み込み動作を行なうようになる)</param>
        protected void LoadClass(
            BinaryReader     pReader,
            ref object       pClassInstance,
            OnFieldValueRead pOverwriteFieldValueReadAction = null )
            => LoadClass(pReader, ref pClassInstance, pClassInstance.GetType(), pOverwriteFieldValueReadAction);

        /// <summary>
        /// 指定した<see cref="BinaryReader"/>からクラスのシリアライズフィールドへデータを読み込む関数(規定クラスのシリアライズ向け/読み込み機能本体)
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pClassInstance">読み込むクラスのインスタンス</param>
        /// <param name="pClassType">読み込むクラスのタイプ※必ず、規定クラスか派生クラスである必要があります。</param>
        /// <param name="pOverwriteFieldValueReadAction">フィールド読み込み時の読み込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでフィールド値の読み込み動作を行なうようになる)</param>
        private void LoadClass(
            BinaryReader     pReader,
            ref object       pClassInstance,
            Type             pClassType,
            OnFieldValueRead pOverwriteFieldValueReadAction = null )
        {
            if ( pReader == null ) { return; }
            
            int fClassDataLength = pReader.ReadInt32();//本フィールド本体全体のバイト数
            
            if ( !IsSerializableClass(pClassInstance) ) { pReader.BaseStream.Seek(fClassDataLength, SeekOrigin.Current); return; }
            
            // 基底クラスの読み込み
            int          fBaseClassLength = pReader.ReadInt32();                              //基底クラスのフィールドデータバイト数
            byte[]       fBaseClassData   = null;                                             //基底クラス情報格納データ
            MemoryStream fBaseClassStream = new MemoryStream();                               //基底クラス読み込み用ストリーム
            BinaryReader fBaseClassReader = new BinaryReader(fBaseClassStream, Encoding.UTF8);//基底クラス読み込み用ストリームコントローラ

            if ( fBaseClassLength > 0 )
            {
                fBaseClassData = pReader.ReadBytes(fBaseClassLength);//基底クラス情報格納データを取得。
                fBaseClassStream.Write(fBaseClassData, 0, fBaseClassLength);
                fBaseClassStream.Position = 0;
                
                BinaryFieldValueType fFieldValueType = (BinaryFieldValueType)fBaseClassReader.ReadUInt16();//この基底クラスデータブロックの型タイプ※必ずクラスになっている必要あり。
                
                if ( fFieldValueType != BinaryFieldValueType.NULL && fFieldValueType == BinaryFieldValueType.CLASS && !IsEndOfBaseType(pClassType) )
                {
                    LoadClass(fBaseClassReader, ref pClassInstance, pClassType.BaseType, pOverwriteFieldValueReadAction);
                }
            }
            
            // クラス本体の読み込み
            ushort                 fFieldCount  = pReader.ReadUInt16();//シリアライズフィールドの数
            Dictionary<ushort,int> fClassIndex  = new Dictionary<ushort,int>();//クラスのフィールド目次テーブル
            int                    fFieldLength = 0;//全クラスフィールド情報格納データのバイト数
            byte[]                 fFieldData   = null;//全クラスフィールド情報格納データ
            MemoryStream           fFieldStream = new MemoryStream();//全クラスフィールド読み込み用ストリーム
            BinaryReader           fFieldReader = new BinaryReader(fFieldStream, Encoding.UTF8);//全クラスフィールド読み込み用ストリームコントローラ
            
            //EditorLog.Inf($"[{GetType().Name}.LoadClass()]_クラス「{pClassType.Name}」の読み込み開始...([fFieldCount:{fFieldCount}])");
            
            pReader.BaseStream.Seek(sizeof(int), SeekOrigin.Current);//FieldIdxLengthは現状使用しないのでスキップ。
            
            // ↓クラスのフィールド目次テーブルの作成
            for (int i = 0; i < fFieldCount; i++)
            {
                ushort fFieldId  = pReader.ReadUInt16();
                int    fPosition = pReader.ReadInt32();
                //-------------------------------------------------------------
                if ( fClassIndex.ContainsKey(fFieldId) ) { continue; }
                //EditorLog.Inf($"[{GetType().Name}.LoadClass()]_クラスIndex取得中...([fFieldId:{fFieldId}][fPosition:{fPosition}])");
                fClassIndex.Add(fFieldId, fPosition);
            }
            
            // ↓全クラスフィールド情報データを一旦読み込んで別ストリーム化する。
            fFieldLength = pReader.ReadInt32();//全クラスフィールド情報格納データのバイト数を取得。
            fFieldData   = pReader.ReadBytes(fFieldLength);//全クラスフィールド情報格納データを取得。
            fFieldStream.Write(fFieldData, 0, fFieldLength);
            
            // ↓クラスフィールドの読み込み
            foreach (var field in pClassType.GetFields(ALL_FIELD_FLAGS))
            {
                if ( field.FieldType.IsAbstract || field.FieldType.IsInterface ) { continue; }//実態がなさそうなフィールドは無視。
                
                SerializeField         fSerializeFieldAttribute = field.GetCustomAttribute<SerializeField>();
                NonSerializedAttribute fNonSerializedAttribute  = field.GetCustomAttribute<NonSerializedAttribute>();
                BinaryFieldIdAttribute fFieldIdAttribute        = field.GetCustomAttribute<BinaryFieldIdAttribute>();
                
                if ( field.IsPublic && (fNonSerializedAttribute != null || fFieldIdAttribute == null) ) { continue; }//パブリックだけどシリアライズしないor定義がない場合はスキップ。
                if ( !field.IsPublic && (fSerializeFieldAttribute == null || fFieldIdAttribute == null) ) { continue; }//プライベートでシリアライズ定義がないものはスキップ。
                if ( !fClassIndex.ContainsKey(fFieldIdAttribute.id) ) { continue; }
                
                object fFieldValue = CreateFieldInstance(field.FieldType);//設定対象のクラスフィールド値
                ushort fFieldId    = 0;//ファイルに記載されているフィールドID
                
                //EditorLog.Inf($"[{GetType().Name}.LoadClass()]_フィールド「{field.Name}」の読み込み開始...[FieldID:{fFieldIdAttribute.id}][fFieldValue:{fFieldValue}][fFieldId:{fFieldId}]");
                
                fFieldStream.Position = fClassIndex[fFieldIdAttribute.id];//処理する変数に該当する場所へバイナリの読み込み位置を移動する。
                
                fFieldId = fFieldReader.ReadUInt16();
                if ( fFieldId != fFieldIdAttribute.id ) {　EditorLog.Err($"[{GetType().Name}.LoadClass()]_ファイルに記録されているフィールドIDと設定対象のフィールドIDが異なります。データは読み込みません。[[fFieldId:{fFieldId}][fFieldIdAttribute.id:{fFieldIdAttribute.id}][FullName:{field.FieldType.FullName}]]"); continue; }//ファイルに記録されているIDと異なる場合はスキップする。
                
                if ( pOverwriteFieldValueReadAction == null )
                {
                    if ( !LoadFieldValue(fFieldReader, ref fFieldValue) ) { continue; }//読み込みに失敗したらスキップ。
                }
                else
                {
                    if ( !pOverwriteFieldValueReadAction.Invoke(fFieldReader, ref fFieldValue, field.FieldType) ) { continue; }//読み込みに失敗したらスキップ。
                }
                
                field.SetValue(pClassInstance, fFieldValue);
            }
            
            fFieldReader.Close();
            fFieldStream.Close();
            
            //EditorLog.Inf($"[{GetType().Name}.LoadClass()]_クラス「{pClassType.Name}」の読み込み終了。");
        }

        /// <summary>
        /// 指定した<paramref name="pReader"/>から値を読み込む関数<br />
        /// ※通常は<paramref name="pValue"/>にはフィールド値を指定します。
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pValue">読み込み対象となる、フィールドインスタンス(<paramref name="pReader"/>で読み込んだ内容をこの引数に設定します)</param>
        /// <param name="pOverwriteFieldValueReadAction">クラスフィールド読み込み時の読み込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでクラスフィールド値の読み込み動作を行なうようになる)※<paramref name="pValue"/>がクラスの場合のみ有効。</param>
        /// <returns>読み込みに成功したかどうか(trueで成功)</returns>
        protected bool LoadFieldValue(
            BinaryReader     pReader,
            ref object       pValue,
            OnFieldValueRead pOverwriteFieldValueReadAction = null )
        {
            if ( pReader == null || pValue == null ) { return false; }
            
            BinaryFieldValueType fFieldValueType = (BinaryFieldValueType)pReader.ReadUInt16();
            
            switch (fFieldValueType)
            {
                case BinaryFieldValueType.NULL:   { pValue = default; return true; }
                case BinaryFieldValueType.BOOL:   { if ( pValue is bool   ) { pValue = pReader.ReadBoolean(); /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.CHAR:   { if ( pValue is char   ) { pValue = pReader.ReadChar();    /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.SBYTE:  { if ( pValue is sbyte  ) { pValue = pReader.ReadSByte();   /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.BYTE:   { if ( pValue is byte   ) { pValue = pReader.ReadByte();    /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.SHORT:  { if ( pValue is short  ) { pValue = pReader.ReadInt16();   /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.USHORT: { if ( pValue is ushort ) { pValue = pReader.ReadUInt16();  /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.INT:    { if ( pValue is int    ) { pValue = pReader.ReadInt32();   /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.UINT:   { if ( pValue is uint   ) { pValue = pReader.ReadUInt32();  /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.LONG:   { if ( pValue is long   ) { pValue = pReader.ReadInt64();   /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.ULONG:  { if ( pValue is ulong  ) { pValue = pReader.ReadUInt64();  /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.FLOAT:  { if ( pValue is float  ) { pValue = pReader.ReadSingle();  /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.DOUBLE: { if ( pValue is double ) { pValue = pReader.ReadDouble();  /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                case BinaryFieldValueType.STRING: { if ( pValue is string ) { pValue = pReader.ReadString();  /*EditorLog.Inf($"[{GetType().Name}.LoadFieldValue()]_読み込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return true; } break; }
                //----- ここからクラス型 -----
                case BinaryFieldValueType.UNITY_OBJECT: { if ( pValue is UnityEngine.Object ) { return LoadUnityObject(pReader, ref pValue); } break; }
                case BinaryFieldValueType.ARRAY:        { if ( pValue.GetType().IsArray     ) { return LoadArray(pReader, ref pValue); } break; }
                case BinaryFieldValueType.LIST:         { if ( pValue is IList              ) { return LoadList(pReader, ref pValue); } break; }
                case BinaryFieldValueType.DICTIONARY:   { if ( pValue is IDictionary        ) { return LoadDictionary(pReader, ref pValue); } break; }
                case BinaryFieldValueType.CLASS:        { if ( IsSerializableClass(pValue)  ) { LoadClass(pReader, ref pValue, pOverwriteFieldValueReadAction); return true; } break; }
                case BinaryFieldValueType.ENUM:         { if ( pValue.GetType().IsEnum      ) { return LoadEnum(pReader, ref pValue); } break; }
            }
            
            EditorLog.Err($"[{GetType().Name}.LoadFieldValue()]_値を読み込めませんでした。([FullName:{pValue.GetType().FullName}][Type:{pValue.GetType()}][IsEnum:{pValue.GetType().IsEnum}][fFieldValueType:{fFieldValueType}])");
            
            pValue = default;
            
            return false;
        }
        
        /// <summary>
        /// 指定した<paramref name="pReader"/>から列挙値を読み込む関数
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pEnumObject">読み込み対象となる、列挙型のフィールドインスタンス(<paramref name="pReader"/>で読み込んだ内容をこの引数に設定します)</param>
        /// <returns>読み込みに成功したかどうか(trueで成功)</returns>
        private bool LoadEnum( BinaryReader pReader, ref object pEnumObject )
        {
            if ( pReader == null || pEnumObject == null ) { return false; }
            if ( !pEnumObject.GetType().IsEnum ) { return false; }

            BinaryEnumValueType fEnumValueType = (BinaryEnumValueType)pReader.ReadByte();

            switch (fEnumValueType)
            {
                case BinaryEnumValueType.SBYTE:  { if ( pEnumObject.IsEnableParse<sbyte>()  ) { pEnumObject = Enum.Parse(pEnumObject.GetType(), pReader.ReadSByte().ToString());  /*EditorLog.Inf($"[{GetType().Name}.LoadEnum()]_読み込みOK。[TypeName:{pEnumObject.GetType().Name}][pValue:{((sbyte) pEnumObject)}]");*/ return true; } break; }
                case BinaryEnumValueType.BYTE:   { if ( pEnumObject.IsEnableParse<byte>()   ) { pEnumObject = Enum.Parse(pEnumObject.GetType(), pReader.ReadByte().ToString());   /*EditorLog.Inf($"[{GetType().Name}.LoadEnum()]_読み込みOK。[TypeName:{pEnumObject.GetType().Name}][pValue:{((byte)  pEnumObject)}]");*/ return true; } break; }
                case BinaryEnumValueType.SHORT:  { if ( pEnumObject.IsEnableParse<short>()  ) { pEnumObject = Enum.Parse(pEnumObject.GetType(), pReader.ReadInt16().ToString());  /*EditorLog.Inf($"[{GetType().Name}.LoadEnum()]_読み込みOK。[TypeName:{pEnumObject.GetType().Name}][pValue:{((short) pEnumObject)}]");*/ return true; } break; }
                case BinaryEnumValueType.USHORT: { if ( pEnumObject.IsEnableParse<ushort>() ) { pEnumObject = Enum.Parse(pEnumObject.GetType(), pReader.ReadUInt16().ToString()); /*EditorLog.Inf($"[{GetType().Name}.LoadEnum()]_読み込みOK。[TypeName:{pEnumObject.GetType().Name}][pValue:{((ushort)pEnumObject)}]");*/ return true; } break; }
                case BinaryEnumValueType.INT:    { if ( pEnumObject.IsEnableParse<int>()    ) { pEnumObject = Enum.Parse(pEnumObject.GetType(), pReader.ReadInt32().ToString());  /*EditorLog.Inf($"[{GetType().Name}.LoadEnum()]_読み込みOK。[TypeName:{pEnumObject.GetType().Name}][pValue:{((int)   pEnumObject)}]");*/ return true; } break; }
                case BinaryEnumValueType.UINT:   { if ( pEnumObject.IsEnableParse<uint>()   ) { pEnumObject = Enum.Parse(pEnumObject.GetType(), pReader.ReadUInt32().ToString()); /*EditorLog.Inf($"[{GetType().Name}.LoadEnum()]_読み込みOK。[TypeName:{pEnumObject.GetType().Name}][pValue:{((uint)  pEnumObject)}]");*/ return true; } break; }
                case BinaryEnumValueType.LONG:   { if ( pEnumObject.IsEnableParse<long>()   ) { pEnumObject = Enum.Parse(pEnumObject.GetType(), pReader.ReadInt64().ToString());  /*EditorLog.Inf($"[{GetType().Name}.LoadEnum()]_読み込みOK。[TypeName:{pEnumObject.GetType().Name}][pValue:{((long)  pEnumObject)}]");*/ return true; } break; }
                case BinaryEnumValueType.ULONG:  { if ( pEnumObject.IsEnableParse<ulong>()  ) { pEnumObject = Enum.Parse(pEnumObject.GetType(), pReader.ReadUInt64().ToString()); /*EditorLog.Inf($"[{GetType().Name}.LoadEnum()]_読み込みOK。[TypeName:{pEnumObject.GetType().Name}][pValue:{((ulong) pEnumObject)}]");*/ return true; } break; }
            }
            
            EditorLog.Err($"[{GetType().Name}.LoadEnum()]_列挙値を読み込めませんでした。([FullName:{pEnumObject.GetType().FullName}][Type:{pEnumObject.GetType()}][IsEnum:{pEnumObject.GetType().IsEnum}])");
            
            pEnumObject = default;
            
            return false;
        }
        
        /// <summary>
        /// 指定した<paramref name="pReader"/>から配列を読み込む関数
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pArrayObject">読み込み対象となる、配列型のフィールドインスタンス(<paramref name="pReader"/>で読み込んだ内容をこの引数に設定します)</param>
        /// <returns>読み込みに成功したかどうか(trueで成功)</returns>
        private bool LoadArray( BinaryReader pReader, ref object pArrayObject )
        {
            if ( pReader == null || pArrayObject == null ) { return false; }
            if ( !pArrayObject.GetType().IsArray ) { return false; }
            if ( pArrayObject.GetType().GetArrayRank() > 1 ) { EditorLog.Err($"[{GetType().Name}.LoadArray()]_2次元以上の配列は非対応です。[ObjectType:{pArrayObject.GetType()}]"); return false; }
            
            Array  fArray        = null;//配列本体(処理用)
            Type   fItemType     = pArrayObject.GetType().GetElementType();//配列の項目型
            int    fArrayCount   = pReader.ReadInt32();//配列項目数
            object fArrayItem    = null;//配列項目[1個分]
            
            fArray = Array.CreateInstance(fItemType, fArrayCount);
            
            if ( fArrayCount <= 0 ) { return true; }//要素がなければ処理不要なのでここでフィールドの読み込みは終了。
            
            for (int i = 0; i < fArrayCount; i++)
            {
                fArrayItem = CreateFieldInstance(fItemType);
                LoadFieldValue(pReader, ref fArrayItem);
                fArray.SetValue(fArrayItem, i);
            }

            pArrayObject = fArray;
            
            //EditorLog.Inf($"[{GetType().Name}.LoadArray()]_読み込みOK。[TypeName:{pArrayObject.GetType().Name}][ArrayCount:{(pArrayObject as IList).Count}][ArrayRank:{pArrayObject.GetType().GetArrayRank()}][ItemType:{fItemType}]");
            
            return true;
        }
        
        /// <summary>
        /// 指定した<paramref name="pReader"/>から<see cref="UnityEngine.Object"/>を読み込む関数
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pUnityObject">読み込み対象となる、<see cref="UnityEngine.Object"/>型のフィールドインスタンス(<paramref name="pReader"/>で読み込んだ内容をこの引数に設定します)</param>
        /// <returns>読み込みに成功したかどうか(trueで成功)</returns>
        private bool LoadUnityObject( BinaryReader pReader, ref object pUnityObject )
        {
            if ( pReader == null || pUnityObject == null ) { return false; }
            if ( !(pUnityObject is UnityEngine.Object) ) { return false; }
            
            string fAssetGuid    = pReader.ReadString();
            string fAssetPath    = AssetDatabase.GUIDToAssetPath(fAssetGuid);
            int    fAssetExtPos  = -1;//設定アセットの拡張子の位置
            string fAssetExt     = "";//設定アセットの拡張子
            int    fAssetNamePos = -1;//設定アセットのファイル名の位置
            string fAssetName    = string.Empty;//設定アセットのファイル名(拡張子付)
            
            if ( string.IsNullOrEmpty(fAssetPath) )
            {
                fAssetPath = pReader.ReadString();
                if ( string.IsNullOrEmpty(fAssetPath) ) { return false; }
            }
            else { pReader.ReadString(); }//GUIDでパスを取得できていたなら「AssetPath」フィールドは読み飛ばす。
            
            fAssetExtPos  = fAssetPath.LastIndexOf(".");
            fAssetExt     = (fAssetExtPos >= 0)? fAssetPath.Substring(fAssetExtPos) : string.Empty;
            fAssetNamePos = fAssetPath.LastIndexOf("/");
            fAssetName    = (fAssetNamePos >= 0)? fAssetPath.Substring(fAssetNamePos + 1) : string.Empty;
            
            if ( string.IsNullOrEmpty(fAssetName) ) { return false; }
            
            if ( pUnityObject is ScriptableObject )
            {
                ScriptableObject fLoadResult = EditorAssetUtility.LoadSettingAssetAtFilePath(fAssetPath, pUnityObject.GetType());
                
                if ( fLoadResult == null ) { fLoadResult = EditorAssetUtility.LoadSettingAssetAtFileName((string.IsNullOrEmpty(fAssetExt))? fAssetName : fAssetName.Replace(fAssetExt,""), pUnityObject.GetType()); }
                if ( fLoadResult == null ) { return false; }
                
                pUnityObject = fLoadResult;
            }
            else
            {
                UnityEngine.Object fLoadResult = AssetDatabase.LoadAssetAtPath(fAssetPath, pUnityObject.GetType());
                
                if ( fLoadResult == null ) { fLoadResult = EditorAssetUtility.LoadAssetAtFileName((string.IsNullOrEmpty(fAssetExt))? fAssetName : fAssetName.Replace(fAssetExt,""), fAssetExt, pUnityObject.GetType()); }
                if ( fLoadResult == null ) { return false; }
                
                pUnityObject = fLoadResult;
            }
            
            //EditorLog.Inf($"[{GetType().Name}.LoadUnityObject()]_読み込みOK。[TypeName:{pUnityObject.GetType().Name}][AssetPath:{AssetDatabase.GetAssetPath(pUnityObject as UnityEngine.Object)}]");
            
            return true;
        }

        /// <summary>
        /// 指定した<paramref name="pReader"/>から<see cref="List{T}"/>を読み込む関数
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pListObject">読み込み対象となる、<see cref="List{T}"/>型のフィールドインスタンス(<paramref name="pReader"/>で読み込んだ内容をこの引数に設定します)</param>
        /// <returns>読み込みに成功したかどうか(trueで成功)</returns>
        private bool LoadList( BinaryReader pReader, ref object pListObject )
        {
            if ( pReader == null || pListObject == null ) { return false; }
            if ( !(pListObject is IList) ) { return false; }
            
            IList  fList      = pListObject as IList;//リスト本体
            Type   fItemType  = pListObject.GetType().GetGenericArguments()[0];//リストの項目型
            int    fListCount = pReader.ReadInt32();//リスト項目数
            object fListItem  = null;//リスト項目[1個分]
            
            fList.Clear();
            
            if ( fListCount <= 0 ) { return true; }//要素がなければ処理不要なのでここでフィールドの読み込みは終了。
            
            for (int i = 0; i < fListCount; i++)
            {
                fListItem = CreateFieldInstance(fItemType);
                LoadFieldValue(pReader, ref fListItem);
                fList.Add(fListItem);
            }
            
            //EditorLog.Inf($"[{GetType().Name}.LoadList()]_読み込みOK。[TypeName:{pListObject.GetType().Name}][ListCount:{(pListObject as IList).Count}]");
            
            return true;
        }

        /// <summary>
        /// 指定した<paramref name="pReader"/>から<see cref="Dictionary{TKey,TValue}"/>を読み込む関数
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        /// <param name="pDictionaryObject">>読み込み対象となる、<see cref="Dictionary{TKey,TValue}"/>型のフィールドインスタンス(<paramref name="pReader"/>で読み込んだ内容をこの引数に設定します)</param>
        /// <returns>読み込みに成功したかどうか(trueで成功)</returns>
        private bool LoadDictionary( BinaryReader pReader, ref object pDictionaryObject )
        {
            if ( pReader == null || pDictionaryObject == null ) { return false; }
            if ( !(pDictionaryObject is IDictionary) ) { return false; }
            
            IDictionary fDictionary       = pDictionaryObject as IDictionary;//辞書本体
            Type[]      fGenericArguments = pDictionaryObject.GetType().GetGenericArguments();//辞書のジェネリックパラメータリスト
            Type        fKeyType          = fGenericArguments[0];//辞書キー型
            Type        fItemType         = fGenericArguments[1];//辞書項目型
            int         fDictionaryCount  = pReader.ReadInt32();//辞書項目数
            object      fDictionaryKey    = null;//辞書キー名[1個分]
            object      fDictionaryItem   = null;//辞書項目[1個分]
            
            fDictionary.Clear();
            
            if ( fDictionaryCount <= 0 ) { return true; }//要素がなければ処理不要なのでここでフィールドの読み込みは終了。
            
            for (int i = 0; i < fDictionaryCount; i++)
            {
                fDictionaryKey  = CreateFieldInstance(fKeyType);
                fDictionaryItem = CreateFieldInstance(fItemType);
                LoadFieldValue(pReader, ref fDictionaryKey);
                LoadFieldValue(pReader, ref fDictionaryItem);
                fDictionary.Add(fDictionaryKey, fDictionaryItem);
            }
            
            //EditorLog.Inf($"[{GetType().Name}.LoadDictionary()]_読み込みOK。[TypeName:{pDictionaryObject.GetType().Name}][DictionaryCount:{(pDictionaryObject as IDictionary).Count}]");
            
            return true;
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓バイナリ書き込み機能関連
        
        /// <summary>
        /// ファイルパスが示すバイナリシリアライズファイルへ書き込みを行なう関数<br />
        /// ※引数<paramref name="pFilePath"/>で指定したファイルパスの設定と同時に書き込みます。
        /// 　以降は引数のない、<see cref="LoadBinary()"/>や<see cref="SaveBinary()"/>で読み書きが可能になります。
        /// </summary>
        /// <param name="pFilePath">書き込むファイルのパス</param>
        public void SaveBinary( string pFilePath )
        {
            if ( string.IsNullOrEmpty(pFilePath) ) { return; }
            
            _assetInfo.SetPath(pFilePath);
            SaveBinary();
        }
        
        /// <summary>
        /// ファイル情報が示すバイナリシリアライズファイルへ書き込みを行なう関数<br />
        /// ※引数<paramref name="pFile"/>で指定したファイル情報の設定と同時に書き込みます。
        /// 　以降は引数のない、<see cref="LoadBinary()"/>や<see cref="SaveBinary()"/>で読み書きが可能になります。
        /// </summary>
        /// <param name="pFile">書き込むファイル情報(<see cref="FileInfo"/>)</param>
        public void SaveBinary( FileInfo pFile )
        {
            if ( pFile == null ) { return; }
            
            _assetInfo.SetPath(pFile);
            SaveBinary();
        }
        
        /// <summary>
        /// バイナリシリアライズファイルへ書き込みを行なう関数
        /// </summary>
        public void SaveBinary()
        {
            if ( _assetInfo.IsEmpty() ) { return; }
            
            BinaryWriter fWriter     = new BinaryWriter(_assetInfo.Open(true, FileAccess.Write), Encoding.UTF8);
            FileHeader   fFileHeader = CreateFileHeader();
            
            fWriter.BaseStream.Position = 0;

            SaveHeader(fWriter, ref fFileHeader);
            OnSaveBinary(fWriter);
            
            fWriter.Close();
        }
        
        /// <summary>
        /// <see cref="BinaryWriter"/>を使用してバイナリシリアライズファイルへデフォルト書き込みを行なう関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pOverwriteFieldValueWriteAction">フィールド書き込み時の書き込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでフィールド値の書き込み動作を行なうようになる)</param>
        protected abstract void DefaultSaveBinary(
            BinaryWriter      pWriter,
            OnFieldValueWrite pOverwriteFieldValueWriteAction = null );

        /// <summary>
        /// ファイルヘッダーを書き込む関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pFileHeader">書き込む<see cref="FileHeader"/></param>
        private void SaveHeader( BinaryWriter pWriter, ref FileHeader pFileHeader )
        {
            if ( pWriter == null || pFileHeader == null ) { return; }
            
            for (int i = 0; i < FileHeader.MAGIC_NO.Length; i++)
            {
                if ( pFileHeader.magicNo[i] != FileHeader.MAGIC_NO[i] ) { return; }
            }
            
            pWriter.Write(pFileHeader.magicNo);
            pWriter.Write(pFileHeader.majorVersion);
            pWriter.Write(pFileHeader.minorVersion);
            pWriter.Write(pFileHeader.buildVersion);
            
            pFileHeader.dataStartPos = (int)(pWriter.BaseStream.Position + sizeof(int));
            pWriter.Write(pFileHeader.dataStartPos);
            
            //EditorLog.Inf($"[{GetType().Name}.SaveHeader()]_ファイルヘッダー書き込み完了。([pFileHeader.dataStartPos:{pFileHeader.dataStartPos}][pWriter.BaseStream.Position:{pWriter.BaseStream.Position}])");
        }
        
        /// <summary>
        /// 指定した<see cref="BinaryWriter"/>へクラスのシリアライズフィールドを書き込む関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pClassInstance">書き込むクラスのインスタンス</param>
        /// <param name="pOverwriteFieldValueWriteAction">フィールド書き込み時の書き込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでフィールド値の書き込み動作を行なうようになる)</param>
        protected void SaveClass(
            BinaryWriter      pWriter,
            object            pClassInstance,
            OnFieldValueWrite pOverwriteFieldValueWriteAction = null )
            => SaveClass(pWriter, pClassInstance, pClassInstance.GetType(), pOverwriteFieldValueWriteAction);

        /// <summary>
        /// 指定した<see cref="BinaryWriter"/>へクラスのシリアライズフィールドを書き込む関数(規定クラスのシリアライズ向け/書き込み機能本体)
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pClassInstance">書き込むクラスのインスタンス</param>
        /// <param name="pClassType">書き込むクラスのタイプ※必ず、規定クラスか派生クラスである必要があります。</param>
        /// <param name="pOverwriteFieldValueWriteAction">フィールド書き込み時の書き込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでフィールド値の書き込み動作を行なうようになる)</param>
        private void SaveClass(
            BinaryWriter      pWriter,
            object            pClassInstance,
            Type              pClassType,
            OnFieldValueWrite pOverwriteFieldValueWriteAction = null )
        {
            if ( pWriter == null ) { return; }
            if ( !IsSerializableClass(pClassInstance) || pClassType == null ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }
            
            ushort       fFieldCount      = 0;//定義されているシリアライズフィールドの数
            MemoryStream fBaseClassStream = new MemoryStream();//規定クラスのデータ保存用ストリーム
            BinaryWriter fBaseClassWriter = new BinaryWriter(fBaseClassStream, Encoding.UTF8);
            MemoryStream fFieldIdxStream  = new MemoryStream();//全フィールドの目次データ保存用ストリーム
            BinaryWriter fFieldIdxWriter  = new BinaryWriter(fFieldIdxStream, Encoding.UTF8);
            MemoryStream fFieldStream     = new MemoryStream();//全クラスフィールド保存用ストリーム
            BinaryWriter fFieldWriter     = new BinaryWriter(fFieldStream, Encoding.UTF8);
            
            //EditorLog.Inf($"[{GetType().Name}.SaveClass()]_クラス「{pClassType.Name}」の書き込み開始...");
            
            pWriter.Write((ushort)BinaryFieldValueType.CLASS);
            
            if ( !IsEndOfBaseType(pClassType) )
            {
                SaveClass(fBaseClassWriter, pClassInstance, pClassType.BaseType, pOverwriteFieldValueWriteAction);
            }
            
            foreach (var field in pClassType.GetFields(ALL_FIELD_FLAGS))
            {
                if ( field.FieldType.IsAbstract || field.FieldType.IsInterface ) { continue; }//実態がなさそうなフィールドは無視。
                
                SerializeField         fSerializeFieldAttribute = field.GetCustomAttribute<SerializeField>();
                NonSerializedAttribute fNonSerializedAttribute  = field.GetCustomAttribute<NonSerializedAttribute>();
                BinaryFieldIdAttribute fFieldIdAttribute        = field.GetCustomAttribute<BinaryFieldIdAttribute>();
                
                if ( field.IsPublic && (fNonSerializedAttribute != null || fFieldIdAttribute == null) ) { continue; }//パブリックだけどシリアライズしないor定義がない場合はスキップ。
                if ( !field.IsPublic && (fSerializeFieldAttribute == null || fFieldIdAttribute == null) ) { continue; }//プライベートでシリアライズ定義がないものはスキップ。
                
                // 目次フィールドの書き込み。
                fFieldIdxWriter.Write(fFieldIdAttribute.id);
                fFieldIdxWriter.Write((int)fFieldWriter.BaseStream.Position);
                
                //EditorLog.Inf($"[{GetType().Name}.SaveClass()]_フィールド「{field.Name}」の書き込み開始...[FieldID:{fFieldIdAttribute.id}][fFieldWriter-Position:{(int)fFieldWriter.BaseStream.Position}]");
                
                // フィールドの内容を書き込み。※一時保存ストリームへ書き込む。最後にまとめて引数のストリームに書き込む。
                fFieldWriter.Write(fFieldIdAttribute.id);
                if ( pOverwriteFieldValueWriteAction == null )
                {
                    SaveFieldValue(fFieldWriter, field.GetValue(pClassInstance));
                }
                else
                {
                    pOverwriteFieldValueWriteAction.Invoke(fFieldWriter, field.GetValue(pClassInstance), field.FieldType);
                }

                fFieldCount++;
            }

            fBaseClassStream.Position = 0;
            fFieldIdxStream.Position  = 0;
            fFieldStream.Position     = 0;
            
            byte[] fAllBaseClassDatas  = new byte[(int)fBaseClassStream.Length];//メモリに一時格納した基底クラスのフィールドデータ読み込み用バッファ
            int    fAllBaseClassLength = fBaseClassStream.Read(fAllBaseClassDatas, 0, (int)fBaseClassStream.Length);//メモリに一時格納した基底クラスのフィールドデータの長さ
            byte[] fAllFieldIdxDatas   = new byte[(int)fFieldIdxStream.Length];//メモリに一時格納したフィールド目次データ全部読み込み用バッファ
            int    fAllFieldIdxLength  = fFieldIdxStream.Read(fAllFieldIdxDatas, 0, (int)fFieldIdxStream.Length);//メモリに一時格納した全フィールド目次データの長さ
            byte[] fAllFieldDatas      = new byte[(int)fFieldStream.Length];//メモリに一時格納したフィールドデータ全部読み込み用バッファ
            int    fAllFieldLength     = fFieldStream.Read(fAllFieldDatas, 0, (int)fFieldStream.Length);//メモリに一時格納した全フィールドデータの長さ
            
            pWriter.Write(sizeof(ushort) + (sizeof(int) * 3) + fAllBaseClassLength + fAllFieldIdxLength + fAllFieldLength);//クラスフィールド本体全体のサイズ
            pWriter.Write(fAllBaseClassLength);//基底クラスのフィールドデータバイト数を記録しておく。読み込み時に別メモリで読み込めるように。
            pWriter.Write(fAllBaseClassDatas, 0, fAllBaseClassLength);//基底クラスのフィールドデータ全体を書き込む。
            pWriter.Write(fFieldCount);//シリアライズフィールドの数を書き込む。
            pWriter.Write(fAllFieldIdxLength);//フィールド目次データ部分の全体データバイト数を記録しておく。読み込み時に別メモリで読み込めるように。
            pWriter.Write(fAllFieldIdxDatas, 0, fAllFieldIdxLength);//フィールド目次データ全体を書き込む。
            pWriter.Write(fAllFieldLength);//クラスフィールド部分の全体データバイト数を記録しておく。読み込み時に別メモリで読み込めるように。
            pWriter.Write(fAllFieldDatas, 0, fAllFieldLength);//フィールド全体を書き込む。
            
            fBaseClassWriter.Close();
            fBaseClassStream.Close();
            fFieldIdxWriter.Close();
            fFieldIdxStream.Close();
            fFieldWriter.Close();
            fFieldStream.Close();
            
            //EditorLog.Inf($"[{GetType().Name}.SaveClass()]_クラス「{pClassType.Name}」の書き込み終了。");
        }

        /// <summary>
        /// 指定した値を<paramref name="pWriter"/>に書き込み関数<br />
        /// ※通常は<paramref name="pValue"/>にはフィールド値を指定します。
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pValue">書き込む値</param>
        /// <param name="pOverwriteFieldValueWriteAction">クラスフィールド書き込み時の書き込み方法カスタムイベント(通常は指定しなくてOK。指定すると、このイベントでクラスフィールド値の書き込み動作を行なうようになる)※<paramref name="pValue"/>がクラスの場合のみ有効。</param>
        protected void SaveFieldValue(
            BinaryWriter      pWriter,
            object            pValue,
            OnFieldValueWrite pOverwriteFieldValueWriteAction = null )
        {
            if ( pWriter == null ) { return; }
            if ( pValue == null ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }
            
            if ( pValue is bool   ) { pWriter.Write((ushort)BinaryFieldValueType.BOOL);   pWriter.Write((bool)pValue);   /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is char   ) { pWriter.Write((ushort)BinaryFieldValueType.CHAR);   pWriter.Write((char)pValue);   /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is sbyte  ) { pWriter.Write((ushort)BinaryFieldValueType.SBYTE);  pWriter.Write((sbyte)pValue);  /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is byte   ) { pWriter.Write((ushort)BinaryFieldValueType.BYTE);   pWriter.Write((byte)pValue);   /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is short  ) { pWriter.Write((ushort)BinaryFieldValueType.SHORT);  pWriter.Write((short)pValue);  /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is ushort ) { pWriter.Write((ushort)BinaryFieldValueType.USHORT); pWriter.Write((ushort)pValue); /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is int    ) { pWriter.Write((ushort)BinaryFieldValueType.INT);    pWriter.Write((int)pValue);    /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is uint   ) { pWriter.Write((ushort)BinaryFieldValueType.UINT);   pWriter.Write((uint)pValue);   /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is long   ) { pWriter.Write((ushort)BinaryFieldValueType.LONG);   pWriter.Write((long)pValue);   /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is ulong  ) { pWriter.Write((ushort)BinaryFieldValueType.ULONG);  pWriter.Write((ulong)pValue);  /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is float  ) { pWriter.Write((ushort)BinaryFieldValueType.FLOAT);  pWriter.Write((float)pValue);  /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is double ) { pWriter.Write((ushort)BinaryFieldValueType.DOUBLE); pWriter.Write((double)pValue); /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            if ( pValue is string ) { pWriter.Write((ushort)BinaryFieldValueType.STRING); pWriter.Write((string)pValue); /*EditorLog.Inf($"[{GetType().Name}.SaveFieldValue()]_書き込みOK。[TypeName:{pValue.GetType().Name}][pValue:{pValue}]");*/ return; }
            
            if ( pValue.GetType().IsEnum      ) { SaveEnum(pWriter, pValue); return; }
            if ( pValue.GetType().IsArray     ) { SaveArray(pWriter, pValue); return; }
            if ( pValue is UnityEngine.Object ) { SaveUnityObject(pWriter, pValue as UnityEngine.Object); return; }
            if ( pValue is IList              ) { SaveList(pWriter, pValue as IList); return; }
            if ( pValue is IDictionary        ) { SaveDictionary(pWriter, pValue as IDictionary); return; }
            
            if ( IsSerializableClass(pValue) ) { SaveClass(pWriter, pValue, pOverwriteFieldValueWriteAction); return; }
            
            pWriter.Write((ushort)BinaryFieldValueType.NULL);
            
            EditorLog.Err($"[{GetType().Name}.SaveFieldValue()]_値を保存できませんでした。([FullName:{pValue.GetType().FullName}][Type:{pValue.GetType()}][IsEnum:{pValue.GetType().IsEnum}])");
        }
        
        /// <summary>
        /// 指定した列挙値を<paramref name="pWriter"/>に書き込み関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pEnumObject">書き込む列挙値</param>
        private void SaveEnum( BinaryWriter pWriter, object pEnumObject )
        {
            if ( pWriter == null ) { return; }
            if ( pEnumObject == null || !pEnumObject.GetType().IsEnum ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }
            
            object fParseEnum = Enum.Parse(pEnumObject.GetType(), pEnumObject.ToString());
            
            pWriter.Write((ushort)BinaryFieldValueType.ENUM);
            
            if ( fParseEnum.IsEnableParse<sbyte>()  ) { pWriter.Write((byte)BinaryEnumValueType.SBYTE);  pWriter.Write((sbyte)fParseEnum);  /*EditorLog.Inf($"[{GetType().Name}.SaveEnum()]_書き込みOK。[TypeName:{pEnumObject.GetType().Name}][pEnumObject:{pEnumObject}][fParseEnum:{((sbyte) fParseEnum)}]");*/ return; }
            if ( fParseEnum.IsEnableParse<byte>()   ) { pWriter.Write((byte)BinaryEnumValueType.BYTE);   pWriter.Write((byte)fParseEnum);   /*EditorLog.Inf($"[{GetType().Name}.SaveEnum()]_書き込みOK。[TypeName:{pEnumObject.GetType().Name}][pEnumObject:{pEnumObject}][fParseEnum:{((byte)  fParseEnum)}]");*/ return; }
            if ( fParseEnum.IsEnableParse<short>()  ) { pWriter.Write((byte)BinaryEnumValueType.SHORT);  pWriter.Write((short)fParseEnum);  /*EditorLog.Inf($"[{GetType().Name}.SaveEnum()]_書き込みOK。[TypeName:{pEnumObject.GetType().Name}][pEnumObject:{pEnumObject}][fParseEnum:{((short) fParseEnum)}]");*/ return; }
            if ( fParseEnum.IsEnableParse<ushort>() ) { pWriter.Write((byte)BinaryEnumValueType.USHORT); pWriter.Write((ushort)fParseEnum); /*EditorLog.Inf($"[{GetType().Name}.SaveEnum()]_書き込みOK。[TypeName:{pEnumObject.GetType().Name}][pEnumObject:{pEnumObject}][fParseEnum:{((ushort)fParseEnum)}]");*/ return; }
            if ( fParseEnum.IsEnableParse<int>()    ) { pWriter.Write((byte)BinaryEnumValueType.INT);    pWriter.Write((int)fParseEnum);    /*EditorLog.Inf($"[{GetType().Name}.SaveEnum()]_書き込みOK。[TypeName:{pEnumObject.GetType().Name}][pEnumObject:{pEnumObject}][fParseEnum:{((int)   fParseEnum)}]");*/ return; }
            if ( fParseEnum.IsEnableParse<uint>()   ) { pWriter.Write((byte)BinaryEnumValueType.UINT);   pWriter.Write((uint)fParseEnum);   /*EditorLog.Inf($"[{GetType().Name}.SaveEnum()]_書き込みOK。[TypeName:{pEnumObject.GetType().Name}][pEnumObject:{pEnumObject}][fParseEnum:{((uint)  fParseEnum)}]");*/ return; }
            if ( fParseEnum.IsEnableParse<long>()   ) { pWriter.Write((byte)BinaryEnumValueType.LONG);   pWriter.Write((long)fParseEnum);   /*EditorLog.Inf($"[{GetType().Name}.SaveEnum()]_書き込みOK。[TypeName:{pEnumObject.GetType().Name}][pEnumObject:{pEnumObject}][fParseEnum:{((long)  fParseEnum)}]");*/ return; }
            if ( fParseEnum.IsEnableParse<ulong>()  ) { pWriter.Write((byte)BinaryEnumValueType.ULONG);  pWriter.Write((ulong)fParseEnum);  /*EditorLog.Inf($"[{GetType().Name}.SaveEnum()]_書き込みOK。[TypeName:{pEnumObject.GetType().Name}][pEnumObject:{pEnumObject}][fParseEnum:{((ulong) fParseEnum)}]");*/ return; }
            
            pWriter.Seek(-sizeof(ushort), SeekOrigin.Current);
            pWriter.Write((ushort)BinaryFieldValueType.NULL);
            
            EditorLog.Err($"[{GetType().Name}.SaveEnum()]_列挙値を保存できませんでした。([Type:{pEnumObject.GetType()}][ValueName:{pEnumObject}][Value:{fParseEnum}])");
        }
        
        /// <summary>
        /// 指定した配列を<paramref name="pWriter"/>に書き込み関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pArrayObject">書き込む配列</param>
        private void SaveArray( BinaryWriter pWriter, object pArrayObject )
        {
            if ( pWriter == null ) { return; }
            if ( pArrayObject == null || !pArrayObject.GetType().IsArray ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }

            IList fList = pArrayObject as IList;//参照しやすいようにIListに変換した配列。※なぜかIListに変換出来るっぽい。
            
            if ( fList == null ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }//通らないと思うけど、念のため。
            
            pWriter.Write((ushort)BinaryFieldValueType.ARRAY);
            pWriter.Write(fList.Count);// 読み込むときに必要になるので項目数を保存。
            
            if ( fList.Count <= 0 ) { return; }//要素がなければ処理不要なのでここでフィールドの保存は終了。
            
            foreach (object item in fList)
            {
                SaveFieldValue(pWriter, item);
            }
            
            //EditorLog.Inf($"[{GetType().Name}.SaveArray()]_書き込みOK。[TypeName:{pArrayObject.GetType().Name}][ArrayCount:{fList.Count}][ArrayRank:{pArrayObject.GetType().GetArrayRank()}][ItemType:{pArrayObject.GetType().GetElementType()}]");
        }
        
        /// <summary>
        /// 指定した<see cref="UnityEngine.Object"/>を<paramref name="pWriter"/>に書き込み関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pObject">書き込む<see cref="UnityEngine.Object"/></param>
        private void SaveUnityObject( BinaryWriter pWriter, UnityEngine.Object pObject )
        {
            if ( pWriter == null ) { return; }
            if ( pObject == null ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }
            
            string fAssetPath = AssetDatabase.GetAssetPath(pObject);
            
            if ( string.IsNullOrEmpty(fAssetPath) ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }
            
            pWriter.Write((ushort)BinaryFieldValueType.UNITY_OBJECT);
            pWriter.Write(AssetDatabase.AssetPathToGUID(fAssetPath));
            pWriter.Write(fAssetPath);
            
            if ( pObject is BinaryScriptableObjectCore )
            {
                int                        fAssetExtPos            = fAssetPath.LastIndexOf(".");//アセットの拡張子の位置
                string                     fAssetExt               = (fAssetExtPos >= 0)? fAssetPath.Substring(fAssetExtPos) : string.Empty;//アセットの拡張子
                string                     fBinaryFilePath         = (string.IsNullOrEmpty(fAssetExt))? $"{fAssetPath}{FileExtName.SETTING}" : fAssetPath.Replace(fAssetExt, FileExtName.SETTING);//バイナリ形式の設定アセットのファイルパス
                BinaryScriptableObjectCore fBinaryScriptableObject = pObject as BinaryScriptableObjectCore;
                
                if ( fBinaryScriptableObject._assetInfo.IsEmpty() ) { fBinaryScriptableObject.SetFileFromPath(fBinaryFilePath); }
                
                //EditorLog.Inf($"[{GetType().Name}.SaveUnityObject()]_セットされていた、{nameof(BinaryScriptableObjectCore)}の書き込み開始。[TypeName:{pObject.GetType().Name}][AssetPath:{fAssetPath}][GUID:{AssetDatabase.AssetPathToGUID(fAssetPath)}]");
                fBinaryScriptableObject.SaveBinary();
                //EditorLog.Inf($"[{GetType().Name}.SaveUnityObject()]_セットされていた、{nameof(BinaryScriptableObjectCore)}の書き込み終了。[TypeName:{pObject.GetType().Name}][AssetPath:{fAssetPath}][GUID:{AssetDatabase.AssetPathToGUID(fAssetPath)}]");
            }
            
            //EditorLog.Inf($"[{GetType().Name}.SaveUnityObject()]_書き込みOK。[TypeName:{pObject.GetType().Name}][AssetPath:{fAssetPath}][GUID:{AssetDatabase.AssetPathToGUID(fAssetPath)}]");
        }
        
        /// <summary>
        /// 指定した<see cref="IList"/>を<paramref name="pWriter"/>に書き込み関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pList">書き込む<see cref="List{T}"/></param>
        private void SaveList( BinaryWriter pWriter, IList pList )
        {
            if ( pWriter == null ) { return; }
            if ( pList == null ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }
            
            pWriter.Write((ushort)BinaryFieldValueType.LIST);
            pWriter.Write(pList.Count);// 読み込むときに必要になるので項目数を保存。
            
            if ( pList.Count <= 0 ) { return; }//要素がなければ処理不要なのでここでフィールドの保存は終了。
            
            foreach (object item in pList)
            {
                SaveFieldValue(pWriter, item);
            }
            
            //EditorLog.Inf($"[{GetType().Name}.SaveList()]_書き込みOK。[TypeName:{pList.GetType().Name}][ListCount:{pList.Count}]");
        }
        
        /// <summary>
        /// 指定した<see cref="IDictionary"/>を<paramref name="pWriter"/>に書き込み関数
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        /// <param name="pDictionary">書き込む<see cref="Dictionary{TKey,TValue}"/></param>
        private void SaveDictionary( BinaryWriter pWriter, IDictionary pDictionary )
        {
            if ( pWriter == null ) { return; }
            if ( pDictionary == null ) { pWriter.Write((ushort)BinaryFieldValueType.NULL); return; }
            
            pWriter.Write((ushort)BinaryFieldValueType.DICTIONARY);
            pWriter.Write(pDictionary.Count);// 読み込むときに必要になるので項目数を保存。
            
            if ( pDictionary.Count <= 0 ) { return; }//要素がなければ処理不要なのでここでフィールドの保存は終了。
            
            foreach (DictionaryEntry item in pDictionary)
            {
                SaveFieldValue(pWriter, item.Key);
                SaveFieldValue(pWriter, item.Value);
            }
            
            //EditorLog.Inf($"[{GetType().Name}.SaveDictionary()]_書き込みOK。[TypeName:{pDictionary.GetType().Name}][DictionaryCount:{pDictionary.Count}]");
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓派生先実装用イベント関連
        
        /// <summary>
        /// バイナリシリアライズファイルから読み込むイベント<br />
        /// ※デフォルト動作で良い場合はオーバーライド実装不要です。<br />
        /// ※デフォルト動作ではシリアライズ定義済み且つ、<see cref="BinaryFieldIdAttribute"/>が定義されている物のみを読み込みます。
        /// </summary>
        /// <param name="pReader">読み込みに使用する<see cref="BinaryReader"/></param>
        protected virtual void OnLoadBinary( BinaryReader pReader ) => DefaultLoadBinary(pReader);
        
        /// <summary>
        /// バイナリシリアライズファイルへの書き込みイベント<br />
        /// ※デフォルト動作で良い場合はオーバーライド実装不要です。<br />
        /// ※デフォルト動作ではシリアライズ定義済み且つ、<see cref="BinaryFieldIdAttribute"/>が定義されている物のみを保存します。
        /// </summary>
        /// <param name="pWriter">書き込む対象の<see cref="BinaryWriter"/></param>
        protected virtual void OnSaveBinary( BinaryWriter pWriter ) => DefaultSaveBinary(pWriter);
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓アクセサ関連

        /// <summary>
        /// このバイナリシリアライズ機能で使用するファイルをGUIDから設定する関数
        /// </summary>
        /// <param name="pGuid">設定するGUID文字列</param>
        public void SetFileFromGuid( string pGuid )
        {
            string fAssetPath = AssetDatabase.GUIDToAssetPath(pGuid);
            
            if ( string.IsNullOrEmpty(fAssetPath) || AssetDatabase.IsValidFolder(fAssetPath) ) { return; }
            
            _assetInfo.SetPathFromGuid(pGuid);
        }
        
        /// <summary>
        /// このバイナリシリアライズ機能で使用するファイルをファイルパスから設定する関数<br />
        /// ※パスは「Assets」以下を示している必要があります。
        /// </summary>
        /// <param name="pFilePath">設定するアセットファイルパス文字列</param>
        public void SetFileFromPath( string pFilePath ) => _assetInfo.SetPath(pFilePath);
        
        /// <summary>
        /// このバイナリシリアライズ機能で使用するファイルを<see cref="FileInfo"/>から設定する関数<br />
        /// ※パスは「Assets」以下を示している必要があります。
        /// </summary>
        /// <param name="pFile">設定に使用する<see cref="FileInfo"/></param>
        public void SetFile( FileInfo pFile ) => _assetInfo.SetPath(pFile);
    }
}
#endif