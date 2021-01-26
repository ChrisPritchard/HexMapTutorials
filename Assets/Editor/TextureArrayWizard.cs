

using UnityEngine;
using UnityEditor;

public class TextureArrayWizard: ScriptableWizard 
{
    [MenuItem("Assets/Create/Texture Array")]
    private static void MenuEntryCall() => DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");

    public Texture2D[] Textures;

    private void OnWizardCreate() 
    {
        if(Textures.Length == 0)
            return;

        var path = EditorUtility.SaveFilePanelInProject("Save Texture Array", "Texture Array", "asset", "Save Texture Array");
        if(path.Length == 0)
            return;

        var texture = Textures[0];
        var array = new Texture2DArray(texture.width, texture.height, Textures.Length, texture.format, texture.mipmapCount > 1);
        array.anisoLevel = texture.anisoLevel;
        array.filterMode = texture.filterMode;
        array.wrapMode = texture.wrapMode;

        for(var i = 0; i < Textures.Length; i++)
            for(var m = 0; m < texture.mipmapCount; m++)
                Graphics.CopyTexture(Textures[i], 0, m, array, i, m);

        AssetDatabase.CreateAsset(array, path);
    }
}