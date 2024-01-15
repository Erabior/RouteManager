using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace RouteManager.v2.UI
{
    public class testInterface : MonoBehaviour
    {
        GameObject mainUIPanel;

        public testInterface()
        {

            //Create Canvas to Contain RRE Elements
            GameObject parentObject = GameObject.Find("Erabior.Dispatcher");
            Canvas canvas = parentObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parentObject.AddComponent<CanvasScaler>();
            GraphicRaycaster temp = parentObject.AddComponent<GraphicRaycaster>();
            //temp.blockingMask = LayerMask.NameToLayer("GameUI");


            //Create Panel to draw to.
            mainUIPanel = new GameObject("UI Panel");
            mainUIPanel.AddComponent<CanvasRenderer>();

            //Give Panel some color for testing
            Image i = mainUIPanel.AddComponent<Image>();
            i.color = new Color(0, 0, 0, .5f);
            i.rectTransform.sizeDelta = new Vector2(960, 540);

            //Add Panel to Canvas
            mainUIPanel.transform.SetParent(canvas.transform, false);

            //Add button for testing
            var buttonObject = new GameObject("Button");
            var image = buttonObject.AddComponent<Image>();
            image.transform.SetParent(mainUIPanel.transform);
            image.rectTransform.sizeDelta = new Vector2(180, 50);
            image.rectTransform.anchoredPosition = Vector3.zero;
            image.color = new Color(1, 1, 1);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => Console.Log("Button Was Clicked!"));

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform);
            var text = textObject.AddComponent<Text>();
            text.rectTransform.anchoredPosition = new Vector2(.5f, .5f);
            text.text = "Hello World!";
            text.font = Resources.FindObjectsOfTypeAll<Font>()[0];
            text.fontSize = 20;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
        }

        public void togglePanel()
        {
            if (mainUIPanel.activeSelf)
            {
                mainUIPanel.SetActive(false);
            }
            else
            {
                mainUIPanel.SetActive(true);
            }
        }
    }
}
