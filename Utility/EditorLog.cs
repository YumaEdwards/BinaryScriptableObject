#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Yuma.Editor.Bso
{
    /// <summary>
    /// エディタツールでログを表示する時用のクラス<br />
    /// ※UnityのDebugクラスに定義されているものをそのまま呼び出すと高負荷な処理になるようなので、基本はこちらを使うように。
    /// </summary>
    internal static class EditorLog
    {
        /// <summary>
        /// Unityコンソールにメッセージを表示する関数
        /// </summary>
        /// <param name="pMessage">ログに表示する文字列または文字列に変換できるオブジェクト</param>
        public static void Inf(object pMessage) => Debug.unityLogger.Log(LogType.Log, pMessage);
        
        /// <summary>
        /// Unityコンソールにメッセージを表示する関数
        /// </summary>
        /// <param name="pMessage">ログに表示する文字列または文字列に変換できるオブジェクト</param>
        /// <param name="pContext">メッセージが適用されるオブジェクト</param>
        public static void Inf(object pMessage, Object pContext) => Debug.unityLogger.Log(LogType.Log, pMessage, pContext);
        
        /// <summary>
        /// 警告メッセージをコンソールに表示する関数
        /// </summary>
        /// <param name="pMessage">ログに表示する文字列または文字列に変換できるオブジェクト</param>
        public static void Wng(object pMessage) => Debug.unityLogger.Log(LogType.Warning, pMessage);
        
        /// <summary>
        /// 警告メッセージをコンソールに表示する関数
        /// </summary>
        /// <param name="pMessage">ログに表示する文字列または文字列に変換できるオブジェクト</param>
        /// <param name="pContext">メッセージが適用されるオブジェクト</param>
        public static void Wng(object pMessage, Object pContext)
            => Debug.unityLogger.Log(LogType.Warning, pMessage, pContext);
        
        /// <summary>
        /// エラーメッセージをコンソールに表示する関数
        /// </summary>
        /// <param name="pMessage">ログに表示する文字列または文字列に変換できるオブジェクト</param>
        public static void Err( object pMessage ) => Debug.unityLogger.Log(LogType.Error, pMessage);
        
        /// <summary>
        /// エラーメッセージをコンソールに表示する関数
        /// </summary>
        /// <param name="pMessage">ログに表示する文字列または文字列に変換できるオブジェクト</param>
        /// <param name="pContext">メッセージが適用されるオブジェクト</param>
        public static void Err(object pMessage, Object pContext)
            => Debug.unityLogger.Log(LogType.Error, pMessage, pContext);
    }
}
#endif