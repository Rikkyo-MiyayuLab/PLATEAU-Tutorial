using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Utils : MonoBehaviour {
    
    /// <summary>
    /// 避難者のランダムスポーン範囲を描画する
    /// </summary>
    public static void DrawWireCircle(Vector3 center, float radius, int segments = 36) {
        float angle = 0f;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0); // 初期点

        for (int i = 1; i <= segments; i++) {
            angle += angleStep;
            float rad = Mathf.Deg2Rad * angle;

            Vector3 newPoint = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);

            prevPoint = newPoint; // 次の線を描画するために現在の点を更新
        }
    }

}