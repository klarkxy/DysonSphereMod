
using UnityEngine;

namespace IntelligentTransport
{
	class UIStationWindowPatch
	{
		// 优先级按钮组
		public static UIButton[] groupPriorityButtons;
		// 当前物流站
		public static StationComponent stationComponent;

		public static void AddButtonToStationWindow()
		{
			UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;

			// 优先级总共有6个，分别对应0-5
			groupPriorityButtons = new UIButton[6];

			int fontsize = 12;
			float x = 0;
			float y = -60f;

			float buttonWidth = 20f;
			float buttonHeight = 20f;

			for (int i = 0; i < groupPriorityButtons.Length; i++)
			{
				UIButton button = Util.MakeSmallTextButton(i.ToString(), buttonWidth, buttonHeight, fontsize);
				if (button != null)
				{
					button.gameObject.name = "intelligent_transport_priority_button_" + i.ToString();
					button.data = i;
					RectTransform rect = Util.NormalizeRect(button.gameObject);

					rect.SetParent(stationWindow.windowTrans, false);
					rect.anchoredPosition = new Vector3(x + buttonWidth * i, y);
					button.tips.tipTitle = "设置优先级：" + i.ToString(); ;
					button.tips.tipText = "优先级越高，越优先处理该运输站的需求/供应。";
					button.tips.corner = 8;
					button.tips.offset = new Vector2(0f, 8f);

				}
				button.onClick += OnClick;
				groupPriorityButtons[i] = button;
			}
		}

		// 点击事件
		public static void OnClick(int obj)
		{
			// 获取当前物流站
			UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
			if (stationWindow == null)
				return;
			PlanetTransport transport = stationWindow.transport;
			if (transport == null)
				return;
			stationComponent = transport.stationPool[stationWindow.stationId];
			if (stationComponent == null)
				return;
#if DEBUG
			Util.Log("点击了优先级按钮: " + obj.ToString());
#endif
			// 获取当前物流站的优先级
			StationComponentPatch.StationComponentPatchData stationComponentPatchData = StationComponentPatch.GetStationComponentPatchData(stationComponent);
			if (stationComponentPatchData == null)
				return;
			// 获取当前优先级
			int priority = stationComponentPatchData.priority;
			// 获取点击的按钮的优先级
			int buttonPriority = obj;
			// 如果点击的按钮的优先级和当前优先级相同，则不做任何操作
			if (priority == buttonPriority)
				return;
			// 更新优先级
			stationComponentPatchData.priority = buttonPriority;
			// 更新优先级按钮组
			UpdatePriorityButtons(buttonPriority);
			// 更新物流站的优先级
			transport.RefreshStationTraffic(stationComponent.id);
			if (stationComponent.gid > 0)
			{
				GalacticTransport galacticTransport = GameMain.data.galacticTransport;
				if (galacticTransport != null)
				{
					galacticTransport.RefreshTraffic(stationComponent.gid);
				}
			}
		}

		// 更新优先级按钮组
		public static void UpdatePriorityButtons(int priority)
		{
			if (groupPriorityButtons == null)
				return;

			for (int i = 0; i < groupPriorityButtons.Length; i++)
			{
				UIButton button = groupPriorityButtons[i];
				bool old = button.highlighted;
				button.highlighted = (i == priority);
			}
		}

		public static void OnStationIdChange(UIStationWindow __instance)
		{
			// 获取当前物流站
			UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
			if (stationWindow == null || stationWindow.transport == null)
				return;
			stationComponent = stationWindow.transport.stationPool[stationWindow.stationId];
			if (stationComponent == null)
				return;

			// 获取当前物流站的优先级
			StationComponentPatch.StationComponentPatchData stationComponentPatchData = StationComponentPatch.GetStationComponentPatchData(stationComponent);
			if (stationComponentPatchData == null)
				return;
			// 更新优先级按钮组
			UpdatePriorityButtons(stationComponentPatchData.priority);
		}

		public static void OnOpen(UIStationWindow __instance)
		{
			// 获取当前物流站
			UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
			if (stationWindow == null || stationWindow.transport == null)
				return;
			stationComponent = stationWindow.transport.stationPool[stationWindow.stationId];
			if (stationComponent == null)
				return;

			// 获取当前物流站的优先级
			StationComponentPatch.StationComponentPatchData stationComponentPatchData = StationComponentPatch.GetStationComponentPatchData(stationComponent);
			if (stationComponentPatchData == null)
				return;
			// 更新优先级按钮组
			UpdatePriorityButtons(stationComponentPatchData.priority);
		}
	}
}