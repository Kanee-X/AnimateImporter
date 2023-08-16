using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static AnimateImporterUtils;

public class AnimateImporter : EditorWindow
{
    private static ObjectField SpriteSheetImageObjectField;
    private static ObjectField SpriteSheetDataObjectField;

    private static Toggle SpriteSheetOverwriteT2DSizeToggle;

    [MenuItem("Tools/Animate Importer")]
    public static void ShowWindow()
    {
        EditorWindow w = GetWindow<AnimateImporter>();
        w.titleContent = new GUIContent("Animate Importer");
        w.maxSize = new Vector2(420, 200);
        w.minSize = new Vector2(420, 200);

        VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/AnimateImporter/AnimateImporterUI.uxml");
        VisualElement ui = uiAsset.Instantiate();

        Button importButton = ui.Q<Button>("ImportSpritesButton");
        importButton.clicked += ImportButton_OnClick;

        SpriteSheetImageObjectField = ui.Q<ObjectField>("SpriteSheetImage");
        SpriteSheetDataObjectField = ui.Q<ObjectField>("SpriteSheetData");

        SpriteSheetOverwriteT2DSizeToggle = ui.Q<Toggle>("OverwriteMaxTexSize");

        w.rootVisualElement.Add(ui);
    }

    private static void ImportButton_OnClick()
    {
        Texture2D spriteSheetImage = (Texture2D)SpriteSheetImageObjectField.value;
        TextAsset spriteSheetData = (TextAsset)SpriteSheetDataObjectField.value;

        if(spriteSheetImage == null)
        {
            EditorUtility.DisplayDialog("Missing Requirement", "Missing SpriteSheet Image property.\n Please assign it in the Animate Importer window.", "Ok");
            return;
        }
        else if(spriteSheetData == null)
        {
            EditorUtility.DisplayDialog("Missing Requirement", "Missing SpriteSheet Data property.\n Please assign it in the Animate Importer window.", "Ok");
            return;
        }

        if (!SpriteSheetOverwriteT2DSizeToggle.value)
        {
            if (!EditorUtility.DisplayDialog("Disabled recommended setting.", "You have \"Overwrite Max Size\" disabled, unless you have manually set the Max Size correctly yourself, you should have this option ENABLED!\n\nIf the Max Size value is not set correctly, it will interfere with the import process in most circumstances, as some internal math will be using incorrect texture dimensions, and cause undesired output, such as by placing sprite rects in the wrong places.\n\nAre you sure you'd like to continue with these settings?", "Yes", "No"))
            {
                Debug.Log("[Animate Importer] SpriteSheet Max Size Overwrite Confirmation declined. Import operation cancelled.");
                return;
            }
        }

        if (!EditorUtility.DisplayDialog("Overwrite texture settings?", "This utility will overwrite some settings for the texture you provided.\n\nDo you still want to continue?", "Go ahead!", "I'm not ready yet!"))
        {
            Debug.Log("[Animate Importer] Asset overwrite denied. Import operation cancelled.");
            return;
        }

        if(AnimateImporterUtils.TryParseAnimateXML((TextAsset)SpriteSheetDataObjectField.value, out List<AnimateXMLItem>? ParsedAXML))
        {
            string texturePath = AssetDatabase.GetAssetPath(spriteSheetImage);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);

            EditorUtility.SetDirty(importer);

            bool importSettingsChanged = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                Debug.Log("[Animate Importer] Texture Type not marked as Sprite, marking as Sprite.");
                importer.textureType = TextureImporterType.Sprite;
                importSettingsChanged = true;
            }
            
            if(importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.Log("[Animate Importer] Sprite Import Mode not marked as Multiple, marking as Multiple.");
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importSettingsChanged = true;
            }

            if(SpriteSheetOverwriteT2DSizeToggle.value)
            {
                Debug.Log("[Animate Importer] SpriteSheet Texture2D Max Size override enabled, overwriting maxTextureSize to 16384.");
                importer.maxTextureSize = 16384;
                importSettingsChanged = true;
            }
            

            if (importSettingsChanged)
            {
                Debug.Log("[Animate Importer] Import settings changed, forcing asset re-import...");
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            }

            List<SpriteRect> spriteMetadata = new List<SpriteRect>();

            foreach (AnimateXMLItem item in ParsedAXML)
            {
                Vector2 fixedOffset = item.offset;

                float adjustedY = spriteSheetImage.height - (item.offset.y + item.size.y);
                fixedOffset.y = adjustedY;

                SpriteRect metaData = new SpriteRect
                {
                    name = item.name,

                    rect = new Rect(fixedOffset, item.size),
                    pivot = new Vector2(0.5f, 0.5f)
                };

                spriteMetadata.Add(metaData);
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(spriteSheetImage);
            dataProvider.InitSpriteEditorDataProvider();

            dataProvider.SetSpriteRects(spriteMetadata.ToArray());

            dataProvider.Apply();

            

            int recommendedMaxSize = FindClosestMaxSize(spriteSheetImage.width > spriteSheetImage.height ? spriteSheetImage.width : spriteSheetImage.height);
            if(importer.maxTextureSize != recommendedMaxSize)
            {
                Debug.Log("[Animate Importer] Detected incorrect max size, prompting user for confirmation.");
                if(!EditorUtility.DisplayDialog("Incorrect max size in use!", $"Your texture is currently using an incorrect Max Size value, Animate Importer can fix this for you by automatically setting it to the most optimal value.\n\nWould you like Animate Importer to fix this for you?\n\nCurrent Max Size: {importer.maxTextureSize}\nOptimal Max Size: {recommendedMaxSize}\nImage Dimensions: {spriteSheetImage.width}x{spriteSheetImage.height}", "Yes", "No"))
                {
                    Debug.Log("[Animate Importer] Declined automatic optimizer. Finishing up.");
                    return;
                }
                else
                {
                    importer.maxTextureSize = recommendedMaxSize;
                }
            }
            
            AssetImporter dataProviderImporter = dataProvider.targetObject as AssetImporter;
            dataProviderImporter.SaveAndReimport();

            EditorUtility.DisplayDialog("Import Finished!", "Successfully finished importing SpriteSheet!", "Ok");
        }
    }
}
