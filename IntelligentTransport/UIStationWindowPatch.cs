
using UnityEngine;

namespace IntelligentTransport
{
	class UIStationWindowPatch
	{
		// ���ȼ���ť��
		public static UIButton[] groupPriorityButtons;
		// ��ǰ����վ
		public static StationComponent stationComponent;

		public static void AddButtonToStationWindow()
		{
			UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;

			// ���ȼ��ܹ���6�����ֱ��Ӧ0-5
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
					button.tips.tipTitle = "�������ȼ���" + i.ToString(); ;
					button.tips.tipText = "���ȼ�Խ�ߣ�Խ���ȴ��������վ������/��Ӧ��";
					button.tips.corner = 8;
					button.tips.offset = new Vector2(0f, 8f);

				}
				button.onClick += OnClick;
				groupPriorityButtons[i] = button;
			}
		}

		// ����¼�
		public static void OnClick(int obj)
		{
			// ��ȡ��ǰ����վ
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
			Util.Log("��������ȼ���ť: " + obj.ToString());
#endif
			// ��ȡ��ǰ����վ�����ȼ�
			StationComponentPatch.StationComponentPatchData stationComponentPatchData = StationComponentPatch.GetStationComponentPatchData(stationComponent);
			if (stationComponentPatchData == null)
				return;
			// ��ȡ��ǰ���ȼ�
			int priority = stationComponentPatchData.priority;
			// ��ȡ����İ�ť�����ȼ�
			int buttonPriority = obj;
			// �������İ�ť�����ȼ��͵�ǰ���ȼ���ͬ�������κβ���
			if (priority == buttonPriority)
				return;
			// �������ȼ�
			stationComponentPatchData.priority = buttonPriority;
			// �������ȼ���ť��
			UpdatePriorityButtons(buttonPriority);
			// ��������վ�����ȼ�
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

		// �������ȼ���ť��
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
			// ��ȡ��ǰ����վ
			UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
			if (stationWindow == null || stationWindow.transport == null)
				return;
			stationComponent = stationWindow.transport.stationPool[stationWindow.stationId];
			if (stationComponent == null)
				return;

			// ��ȡ��ǰ����վ�����ȼ�
			StationComponentPatch.StationComponentPatchData stationComponentPatchData = StationComponentPatch.GetStationComponentPatchData(stationComponent);
			if (stationComponentPatchData == null)
				return;
			// �������ȼ���ť��
			UpdatePriorityButtons(stationComponentPatchData.priority);
		}

		public static void OnOpen(UIStationWindow __instance)
		{
			// ��ȡ��ǰ����վ
			UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
			if (stationWindow == null || stationWindow.transport == null)
				return;
			stationComponent = stationWindow.transport.stationPool[stationWindow.stationId];
			if (stationComponent == null)
				return;

			// ��ȡ��ǰ����վ�����ȼ�
			StationComponentPatch.StationComponentPatchData stationComponentPatchData = StationComponentPatch.GetStationComponentPatchData(stationComponent);
			if (stationComponentPatchData == null)
				return;
			// �������ȼ���ť��
			UpdatePriorityButtons(stationComponentPatchData.priority);
		}
	}
}