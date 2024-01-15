using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace IntelligentTransport
{
	internal class Util
	{
		// 根据行星ID获取行星
		public static PlanetData GetPlanet(int planetID)
		{
			return GameMain.data.galaxy.PlanetById(planetID);
		}
		// 根据行星ID和塔ID获取塔
		public static StationComponent GetStationComponent(int planetID, int stationLocalID)
		{
			PlanetData planet = GetPlanet(planetID);
			if (planet == null || planet.factory == null)
				return null;
			PlanetTransport transport = planet.factory.transport;
			if (transport == null)
				return null;
			return transport.GetStationComponent(stationLocalID);
		}


		public static ManualLogSource logger;

		public static void Log(string msg)
		{
			if (logger != null)
			{
				logger.LogInfo(msg);
			}
		}

		// 生成文字按钮 - 从LSTM中复制过来的 
		public static UIButton MakeSmallTextButton(string label = "", float width = 10, float height = 10, int fontsize = 12)
		{
			UITurretWindow turretWindow = UIRoot.instance.uiGame.turretWindow;
			GameObject go = GameObject.Instantiate(turretWindow.groupSelectionBtns[0].gameObject);
			UIButton btn = go.GetComponent<UIButton>();
			Transform child = go.transform.Find("Text");
			GameObject.DestroyImmediate(child.GetComponent<Localizer>());
			Text txt = child.GetComponent<Text>();
			txt.text = label;
			txt.fontSize = (int)fontsize;
			btn.tips.tipText = "";
			btn.tips.tipTitle = "";

			if (width > 0 || height > 0)
			{
				RectTransform rect = (RectTransform)go.transform;
				if (width == 0)
				{
					width = rect.sizeDelta.x;
				}
				if (height == 0)
				{
					height = rect.sizeDelta.y;
				}
				rect.sizeDelta = new Vector2(width, height);
			}

			//go.transform.localScale = Vector3.one;

			return btn;
		}
		// 生成居中的矩形块 - 从LSTM中复制过来的
		public static RectTransform NormalizeRect(GameObject go, float width = 0, float height = 0)
		{
			RectTransform rect = (RectTransform)go.transform;
			rect.anchorMax = new Vector2(0.5f, 1f);
			rect.anchorMin = new Vector2(0.5f, 1f);
			rect.pivot = new Vector2(0.5f, 1f);
			if (width > 0 && height > 0)
			{
				rect.sizeDelta = new Vector2(width, height);
			}
			return rect;
		}
	}
}
