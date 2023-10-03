#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace Yuma.Editor.Bso
{
    internal static class EditorUtilityEx
    {
        //-----------------------------------------------------------------------------
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓型情報関連
        
        /// <summary>
        /// 指定した型情報に<typeparamref name="CheckT"/>が含まれているかどうか返す関数
        /// </summary>
        /// <param name="pThis">確認対象の型情報</param>
        /// <typeparam name="CheckT">確認したい型</typeparam>
        /// <returns>trueで<paramref name="pThis"/>に<typeparamref name="CheckT"/>が含まれている</returns>
        public static bool ContainsType<CheckT>( this Type pThis )
        {
            if ( pThis == typeof(CheckT) ) { return true; }
            
            Type fTargetType     = typeof(CheckT);
            Type fCurrentChkType = pThis.BaseType;
            
            if ( fCurrentChkType == null ) { return false; }
            if ( fCurrentChkType == fTargetType ) { return true; }

            while( true )
            {
                fCurrentChkType = fCurrentChkType.BaseType;
                if ( fCurrentChkType == null ) { break; }
                if ( fCurrentChkType == fTargetType ) { return true; }
            }
            
            return false;
        }
        
        
        //～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～～
        //↓数値関連
        
        /// <summary>
        /// <typeparamref name="ParseT"/>でキャスト可能かどうかを返す関数
        /// </summary>
        /// <param name="pThis">確認する対象</param>
        /// <typeparam name="ParseT">キャストチェックしたい型</typeparam>
        /// <returns><typeparamref name="ParseT"/>でキャスト可能かどうか(trueで可能)</returns>
        public static bool IsEnableParse<ParseT>( this object pThis )
        {
            try
            {
                ParseT fParseChk = (ParseT)pThis;
            }
            catch ( Exception ) { return false; }

            return true;
        }
    }
}
#endif
