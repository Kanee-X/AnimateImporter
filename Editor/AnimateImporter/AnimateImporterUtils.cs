using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class AnimateImporterUtils
{
    public struct AnimateXMLItem
    {
        // Sprite name
        public string name;

        // Sprite offset
        public Vector2 offset;

        // Sprite size
        public Vector2 size;
    }
    public static bool TryParseAnimateXML(this TextAsset text, out List<AnimateXMLItem>? parsed)
    {
        XmlDocument xmlDoc = new XmlDocument();
        
        try
        {
            xmlDoc.LoadXml(text.text);
        }
        catch(XmlException ex)
        {
            EditorUtility.DisplayDialog("Failed to parse XML data!", "An XmlException occurred whilst parsing the provided TextAsset.\n\n" + ex.Message, "Ok");
            
            parsed = null;
            return false;
        }

        XmlNodeList subTextureNodes = xmlDoc.SelectNodes("/TextureAtlas/SubTexture");
        List<AnimateXMLItem> list = new List<AnimateXMLItem>();

        for(int i = 0;  i < subTextureNodes.Count; i++)
        {
            EditorUtility.DisplayProgressBar("Importing Animate XML data", $"Parsing Animate XML... ({i + 1} of {subTextureNodes.Count})", i / subTextureNodes.Count);

            XmlNode node = subTextureNodes[i];

            AnimateXMLItem axmlItem = new AnimateXMLItem();

            XmlNode name = node.Attributes.GetNamedItem("name");

            if(name == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Null reference to name attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }

            string nameStr = name.Value;
            if(string.IsNullOrWhiteSpace(nameStr))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Malformed name attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }
            else
            {
                axmlItem.name = nameStr;
            }

            Vector2 offset = new Vector2();

            XmlNode offsetX = node.Attributes.GetNamedItem("x");
            XmlNode offsetY = node.Attributes.GetNamedItem("y");

            if (offsetX == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Null reference to offset x attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }
            else if (offsetY == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Null reference to offset y attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }

            if (int.TryParse(offsetX.Value, out int x))
            {
                offset.x = x;
            }
            else
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Malformed x offset attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }

            if (int.TryParse(offsetY.Value, out int y))
            {
                offset.y = y;
            }
            else
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Malformed x offset attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }

            axmlItem.offset = offset;
            Vector2 size = new Vector2();

            XmlNode sizeX = node.Attributes.GetNamedItem("width");
            XmlNode sizeY = node.Attributes.GetNamedItem("height");

            if(sizeX == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Null reference to width attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }
            else if(sizeY == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Null reference to height attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }

            if (int.TryParse(sizeX.Value, out int w))
            {
                size.x = w;
            }
            else
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Malformed width attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }

            if (int.TryParse(sizeY.Value, out int h))
            {
                size.y = h;
            }
            else
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Failed to parse XML data!", $"Malformed height attribute at node {i} detected.", "Ok");

                parsed = null;
                return false;
            }

            axmlItem.size = size;

            list.Add(axmlItem);
        }
        EditorUtility.ClearProgressBar();

        parsed = list;
        return true;
    }

    private static readonly int[] multiples = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
    public static int FindClosestMaxSize(int biggestDimension)
    {
        return multiples.Where(multiple => multiple >= biggestDimension).Min();
    }
}