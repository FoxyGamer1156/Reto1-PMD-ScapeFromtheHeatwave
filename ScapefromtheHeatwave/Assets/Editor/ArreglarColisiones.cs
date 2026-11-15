using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools > Scape From the Heatwave > Arreglar colisiones del suelo
/// El suelo pasa a collider SÓLIDO (Polygons) y Marta a colisión continua + interpolación.
/// A velocidades altas el collider hueco (Outlines) deja que Marta se hunda en los tiles.
/// </summary>
public static class ArreglarColisiones
{
    [MenuItem("Tools/Scape From the Heatwave/Arreglar colisiones del suelo")]
    public static void Arreglar()
    {
        var mundo = GameObject.Find("Mundo");
        var marta = Object.FindFirstObjectByType<Controles>();
        if (!mundo || !marta)
        {
            EditorUtility.DisplayDialog("Escena equivocada", "Abre Nivel1 (necesita 'Mundo' y a Marta).", "OK");
            return;
        }

        // Suelo
        var rbSuelo = mundo.GetComponent<Rigidbody2D>();
        if (!rbSuelo) rbSuelo = Undo.AddComponent<Rigidbody2D>(mundo);
        Undo.RecordObject(rbSuelo, "Suelo estático");
        rbSuelo.bodyType = RigidbodyType2D.Static;

        var tc = mundo.GetComponent<TilemapCollider2D>();
        if (tc) { Undo.RecordObject(tc, "Tilemap collider"); tc.compositeOperation = Collider2D.CompositeOperation.Merge; }

        var cc = mundo.GetComponent<CompositeCollider2D>();
        if (!cc) cc = Undo.AddComponent<CompositeCollider2D>(mundo);
        Undo.RecordObject(cc, "Composite collider");
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
        cc.generationType = CompositeCollider2D.GenerationType.Synchronous;

        // Marta
        var rb = marta.GetComponent<Rigidbody2D>();
        Undo.RecordObject(rb, "Rigidbody de Marta");
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        PrefabUtility.RecordPrefabInstancePropertyModifications(rb);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Colisiones arregladas: suelo en Polygons, Marta en Continuous + Interpolate.");
    }
}
