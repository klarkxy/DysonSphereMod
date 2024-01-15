
using Mono.Cecil.Cil;
using System.Collections;
using System.IO;
using UnityEngine.Profiling.Memory.Experimental;

namespace IntelligentTransport
{
	internal class StationComponentPatch
	{
		public class StationComponentPatchData
		{
			// 行星ID
			public int planetID;
			// 站点ID - 对应StationComponent.id
			public int stationID;
			// 优先级
			public int priority;
			// 对应的物流站点
			private StationComponent stationComponent;
			// 初始化
			public StationComponentPatchData(StationComponent stationComponent = null)
			{
				this.stationComponent = stationComponent;
				if (stationComponent != null)
				{
					planetID = stationComponent.planetId;
					stationID = stationComponent.id;
					if (stationComponent.isCollector || stationComponent.isVeinCollector)
						priority = 0;   // 采集站的优先级设置为0，最低优先级
					else
						priority = 3;   // 其他站点的优先级设置为3，中等优先级
				}
				else
				{
					planetID = 0;
					stationID = 0;
					priority = 0;
				}
			}	
			// 读存档
			public void Import(BinaryReader r)
			{
				planetID = r.ReadInt32();
				stationID = r.ReadInt32();
				priority = r.ReadInt32();
				stationComponent = Util.GetStationComponent(planetID, stationID);
				if (stationComponent == null || stationComponent.id != stationID)// 物流站点不存在，删除该优先级
					stationComponentPatchArray.Remove(this);
			}
			// 写存档
			public void Export(BinaryWriter w)
			{
				// 先判断物流站点是否存在
				stationComponent = Util.GetStationComponent(planetID, stationID);
				if (stationComponent == null || stationComponent.id != stationID)// 物流站点不存在，删除该优先级
					return;
				w.Write(planetID);
				w.Write(stationID);
				w.Write(priority);
			}
		}

		private static ArrayList stationComponentPatchArray;
		// 是否已经完成初始化
		public static bool done { get; private set; } = false;
		
		private static StationComponentPatchData NewStationComponentPatchData(StationComponent stationComponent = null)
		{
			if (stationComponentPatchArray == null)
				stationComponentPatchArray = new ArrayList();
			StationComponentPatchData data = new StationComponentPatchData(stationComponent);
			stationComponentPatchArray.Add(data);
			return data;
		}

		// 读存档
		public static void Import(BinaryReader r)
		{
			stationComponentPatchArray = new ArrayList();
			int count = r.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				StationComponentPatchData data = NewStationComponentPatchData();
				data.Import(r);
			}
			done = true;
		}
		// 写存档
		public static void Export(BinaryWriter w)
		{
			if (stationComponentPatchArray == null)
				return;

			w.Write(stationComponentPatchArray.Count);
			foreach (StationComponentPatchData data in stationComponentPatchArray)
			{
				if (data != null)
					data.Export(w);
				else
				{
					w.Write(0);
					w.Write(0);
					w.Write(0);
				}
			}
		}
		// 从新存档中读取
		public static void IntoOtherSave()
		{
			Util.Log("未检测到智能物流系统数据，生成默认数据。");
			stationComponentPatchArray = new ArrayList();
			GameData gameData = GameMain.data;
			// 遍历所有存在的工厂
			for (int i=0; gameData != null && i < gameData.factoryCount; i++)
			{
				PlanetFactory factory = gameData.factories[i];
				// 遍历工厂中所有的塔
				for (int j = 1; factory != null && factory.transport != null && j < factory.transport.stationCursor; j++)
				{
					if (factory.transport.stationPool[j] != null && factory.transport.stationPool[j].id == j)
					{
						StationComponent stationComponent = factory.transport.stationPool[j];
						AddStationComponent(stationComponent);
					}
				}
			}
			done = true;
		}
		// 获取物流站点的优先级数据
		public static StationComponentPatchData GetStationComponentPatchData(int planetID, int stationID)
		{
			if (stationComponentPatchArray == null)
				return null;

			foreach (StationComponentPatchData data in stationComponentPatchArray)
			{
				if (data != null && data.planetID == planetID && data.stationID == stationID)
					return data;
			}
			return null;
		}
		// 获取物流站点的优先级数据
		public static StationComponentPatchData GetStationComponentPatchData(StationComponent stationComponent)
		{
			StationComponentPatchData data = GetStationComponentPatchData(stationComponent.planetId, stationComponent.id);
			// 如果没有，就新增一个
			if (data == null)
			{
				NewStationComponentPatchData(stationComponent);
			}
			return data;
		}
		// 获取物流站点的优先级
		public static int GetStationComponentPatchDataPriority(StationComponent stationComponent)
		{
			StationComponentPatchData data = GetStationComponentPatchData(stationComponent);
			if (data == null)
				return 0;
			return data.priority;
		}

		// 新增物流站点
		public static void AddStationComponent(StationComponent stationComponent)
		{
			StationComponentPatchData data = NewStationComponentPatchData(stationComponent);
			Util.Log("新增物流站点: " + data.planetID + "." + data.stationID);
		}
		// 删除物流站点
		public static void RemoveStationComponent(int planetID, int stationID)
		{
			if (stationComponentPatchArray == null)
				return;

			for (int i = 0; i < stationComponentPatchArray.Count; i++)
			{
				StationComponentPatchData data = (StationComponentPatchData)stationComponentPatchArray[i];
				if (data.planetID == planetID && data.stationID == stationID)
				{
					stationComponentPatchArray.RemoveAt(i);
					Util.Log("删除物流站点: " + data.planetID + "." + data.stationID);
					break;
				}
			}
		}
	}
}
