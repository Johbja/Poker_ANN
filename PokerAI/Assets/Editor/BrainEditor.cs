using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Brain))]
public class BrainEditor : Editor {
    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) {

        Brain brain = target as Brain;

        EditorUtility.SetDirty(brain);

        if(brain == null || brain.previewImage == null)
            return null;

        Texture2D texture = new Texture2D(width, height);
        EditorUtility.CopySerialized(brain.previewImage, texture);

        return texture;
    }
}
