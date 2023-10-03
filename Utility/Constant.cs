#if UNITY_EDITOR
namespace Yuma.Editor.Bso
{
    /// <summary>
    /// ファイル拡張子名
    /// </summary>
    internal static class FileExtName
    {
        public const string SCENE               = ".unity";     //シーンの拡張子
        public const string PREFAB              = ".prefab";    //プレハブの拡張子
        public const string SCRIPTABLE_OBJECT   = ".asset";     //ScriptableObjectの拡張子
        public const string ANIMATOR_CONTROLLER = ".controller";//AnimatorControllerの拡張子
        public const string ANIMATION_CLIP      = ".anim";      //AnimationClipの拡張子
        public const string MATERIAL            = ".mat";       //Materialの拡張子
        // ----- ここから下はGUIDを含まないタイプ -----
        public const string CS_SCRIPT = ".cs"; //C#ファイルの拡張子
        public const string PNG_IMAGE = ".png";//PNG画像の拡張子
        public const string JPG_IMAGE = ".jpg";//JPG画像の拡張子
        public const string BMP_IMAGE = ".bmp";//BMP画像の拡張子
        public const string PSD_IMAGE = ".psd";//PSD画像の拡張子
        public const string GIF_IMAGE = ".gif";//GIF画像の拡張子
        // ----- ここから下はUnityシステム系アセットタイプ -----
        public const string META = ".meta";//metaファイルの拡張子
        // ----- ここから下は独自形式ファイルタイプ -----
        public const string SETTING = ".setting";//バイナリ設定ファイルの拡張子
    }
}
#endif
