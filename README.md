# BinaryScriptableObject
UnityのScriptableObjectをDLL化しても使えるようにするための汎用シリアライズライブラリ。  
ScriptableObjectにバイナリデータ形式での読み書き機能を加えたものなので、別の用途で使用することもできそうです。  
現状は`UnityEngine.Object`タイプの読み込みがエディタ専用になってしまっていますが、  
この辺りを調整すれば、アプリ内でも使用は可能となると思います。  

## ■使い方
使い方は簡単で、通常、`ScriptableObject`を継承する所を`BinaryScriptableObjectBase`に変更し、  
シリアライズ対象のメンバ変数に対して、`BinaryFieldIdAttribute`属性を追加するだけで、使用準備が完了します。  
```CSharp:SampleSetting.cs
using Yuma.Editor.Bso;

internal class SampleSetting : BinaryScriptableObjectBase<SampleSetting>
{
    [SerializeField,BinaryFieldId(0)]
    private int _intTest;
    [SerializeField,BinaryFieldId(1),Range(0, 1)]
    private float _sliderValue = 0.5f;
    [SerializeField,BinaryFieldId(2)]
    private string _testString;
    [SerializeField,BinaryFieldId(3)]
    private List<UnityEngine.Object> _testAssets = new List<UnityEngine.Object>();
}
```
`BinaryFieldIdAttribute`属性を追加する対象は必ず、シリアライズ可能な状態になっている必要があります。  
要するに、  

1. `public`であり、`NonSerializedAttribute`属性がついていない。
2. `private`の場合、`SerializeFieldAttribute`属性がついている。
3. 自身で作成したクラスの場合、クラス自体に`SerializableAttribute`属性がついており、  
   「1.」又は「2.」の条件を満たしている。

設定ファイルの準備が出来たら、あとは下記のコードで読み書きを行うだけです。
```CSharp:Sample.cs
SampleSetting setting = ScriptableObject.CreateInstance<SampleSetting>();

// ここに「SampleSetting」クラスに対しての設定を行うコードを書く。

setting.SaveBinary("Assets/Editor/SampleSetting.setting");//ファイルパスはサンプル。拡張子は「.setting」としてください。

// 読み込む場合は下記のようにする。
setting.LoadBinary();//一旦、パス付で保存または読み込みを行った場合はパスまたはGUID情報が内部に保存される為、パス無しの読み込みメソッドでよい。
```

本ライブラリは通常の`ScriptableObject`と併用が可能ですので、併用運用時のメソッドも用意しています。  

`EditorAssetUtility.LoadSettingAssetAtFileName<AssetObjectT>( string );`  
`EditorAssetUtility.LoadSettingAssetAtFilePath<AssetObjectT>( string );`  

上記メソッドを利用することで`ScriptableObject`が無い時とあるときでコードを変えずに読み変えることができます。  
このメソッドを使用した場合のサンプルは下記になります。  
```CSharp:Sample.cs
SampleSetting setting = EditorAssetUtility.LoadSettingAssetAtFilePath<SampleSetting>("Assets/Editor/SampleSetting.asset");

// ここに「SampleSetting」クラスに対しての設定を行うコードを書く。

EditorUtility.SetDirty(setting);
setting.SaveBinary();//LoadSettingAssetAtFilePath内でバイナリファイルの保存先パスが設定されているので、パス無しの書き込みメソッドでよい。
// ↑この処理を1回挟んだ後なら、元のScriptableObjectを消しても同じコードでバイナリファイル側が読み込まれます。
```

## ■サンプルプログラムについて
「■使い方」にて、簡単な説明を行いましたが、実際のプログラムを見た方が早いと思いますので、  
下記にサンプルプログラムを含む、UnityプロジェクトをUPしました。  
https://github.com/YumaEdwards/BSOSampleProject  

上記、レポジトリをクローンして、動作確認とプログラムの内容をご確認ください。  

## ■バイナリデータ仕様
下記に本ライブラリで取り扱うバイナリデータに関する、データ仕様を公開しています。  
自作の読み書きライブラリを作ったり、本ライブラリを改造する場合などにお使いください。  
https://docs.google.com/document/d/1DTKZdYGk1vbtR4gtiW4LWnM23JbYm-ihBg0WRjV8g4I/edit?usp=sharing

## ■ライセンスについて
ライセンスは「MIT License」となります。  
本ライブラリを使用したツールやゲームに関して、  
「著作権表示」と「MIT ライセンスの全文(またはリンク)」を含めていただければ、他は特に制限のないライセンスです。  
ライセンスの本文はGitHubの右上のライセンスリンクから飛ぶか、下記へアクセスください。  
https://github.com/YumaEdwards/BinaryScriptableObject/blob/develop/LICENSE
