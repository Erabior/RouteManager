using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace RouteManager.v2.UI
{
    public class CoreInterface : MonoBehaviour
    {

        public CoreInterface(GameObject interfaceRootGameObject)
        {
            GameObject myText;
            Canvas myCanvas;
            Text text;
            RectTransform rectTransform;

            // Canvas
            interfaceRootGameObject.name = "TestCanvas";
            interfaceRootGameObject.AddComponent<Canvas>();

            myCanvas = interfaceRootGameObject.GetComponent<Canvas>();
            myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            interfaceRootGameObject.AddComponent<CanvasScaler>();
            interfaceRootGameObject.AddComponent<GraphicRaycaster>();

            // Text
            myText = new GameObject();
            myText.transform.parent = interfaceRootGameObject.transform;
            myText.name = "wibble";

            text = myText.AddComponent<Text>();
            text.font = (Font)Resources.Load("MyFont");
            text.text = "wobble";
            text.fontSize = 100;

            // Text position
            rectTransform = text.GetComponent<RectTransform>();
            rectTransform.localPosition = new Vector3(0, 0, 0);
            rectTransform.sizeDelta = new Vector2(400, 200);
        }
    }
}
