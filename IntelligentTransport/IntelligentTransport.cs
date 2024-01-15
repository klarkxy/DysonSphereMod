
using BepInEx;
using crecheng.DSPModSave;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IntelligentTransport
{
	[BepInPlugin("klarkxy.dsp.IntelligentTransport", "智能物流计划", "1.1.0")]
	[BepInDependency(DSPModSavePlugin.MODGUID)]
	public class IntelligentTransport : BaseUnityPlugin, IModCanSave
	{
		public const string MODID = "IntelligentTransport";
		public const string GUID = "klarkxy.dsp.plugin." + MODID;
		public const string NAME = "IntelligentTransport";


		// 读存档
		public void Import(BinaryReader r)
		{
			StationComponentPatch.Import(r);
		}
		// 写存档
		public void Export(BinaryWriter w)
		{
			StationComponentPatch.Export(w);
		}
		// 从新存档中读取
		public void IntoOtherSave()
		{
			StationComponentPatch.IntoOtherSave();
		}

		public void Awake()
		{
			Util.logger = Logger;
			try
			{
				Harmony.CreateAndPatchAll(typeof(IntelligentTransport));
			}
			catch (Exception e)
			{
				Util.Log(e.ToString());
			}
		}

		public void Start()
		{
		}

		public void Update()
		{
		}

		// 创建新的物流站后，添加到优先级列表中
		[HarmonyPostfix, HarmonyPatch(typeof(PlanetTransport), "NewStationComponent")]
		public static void PlanetTransport_NewStationComponent_Postfix(PlanetTransport __instance, ref StationComponent __result)
		{
			StationComponentPatch.AddStationComponent(__result);
		}

		// 删除物流站后，从优先级列表中删除
		[HarmonyPostfix, HarmonyPatch(typeof(PlanetTransport), "RemoveStationComponent")]
		public static void PlanetTransport_RemoveStationComponent_Postfix(PlanetTransport __instance, int id)
		{
			StationComponentPatch.RemoveStationComponent(__instance.planet.id, id);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(UIStationWindow), "_OnOpen")]
		public static void UIStationWindow__OnOpen_Postfix(UIStationWindow __instance)
		{
			UIStationWindowPatch.OnOpen(__instance);
		}

		// UI修改物流站ID，即切换到新的物流站的时候
		[HarmonyPostfix, HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
		public static void UIStationWindow_OnStationIdChange_Postfix(UIStationWindow __instance)
		{
			UIStationWindowPatch.OnStationIdChange(__instance);
		}

		private static bool _initialized = false;

		[HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnCreate")]
		public static void UIGame__OnCreate_Postfix()
		{
			if (!_initialized)
			{
				_initialized = true;
				UIStationWindowPatch.AddButtonToStationWindow();
			}
		}

		public static GalaxyData galaxy;

		[HarmonyPostfix, HarmonyPatch(typeof(UniverseGen), "CreateGalaxy")]
		public static void CreateGalaxy_Postfix(GameDesc gameDesc, ref GalaxyData __result)
		{
			galaxy = __result;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(StationComponent), "RematchRemotePairs")]
		public static void RematchRemotePairs_Postfix(ref StationComponent __instance, StationComponent[] gStationPool, int gStationCursor, int keyStationGId, int shipCarries)
		{
			if (__instance.remotePairs == null) return;
			if (!StationComponentPatch.done) return;

			int gid = __instance.gid;
			int planetId = __instance.planetId;
			StarData starData = galaxy.StarById(planetId / 100);
			Dictionary<int, int> oldvalue = new Dictionary<int, int>();   // 为了解决一个奇怪的问题，这里用一个变量来保存旧的值
			// 根据距离对所当前所有的星际飞船进行排序
			Array.Sort(__instance.remotePairs, (p1, p2) =>
			{
				// 获取到对端的星际站
				int other1 = p1.supplyId, other2 = p2.supplyId;
				if (other1 == gid)
					other1 = p1.demandId;
				if (other2 == gid)
					other2 = p2.demandId;
				if (other1 == 0) return 1;
				if (other2 == 0) return -1;
				if (other1 == other2) return 0; // 是自己，那就不用排序了【这种情况真的会发生吗？】

				// 为了解决莫名其妙出现的排序不同值问题
				if (oldvalue.ContainsKey(other1 * 10000 + other2))
					return oldvalue[other1 * 10000 + other2];
				else if (oldvalue.ContainsKey(other2 * 10000 + other1))
					return -oldvalue[other2 * 10000 + other1];

				// 获取另外两个星际站
				StationComponent stationComponent1 = gStationPool[other1];
				StationComponent stationComponent2 = gStationPool[other2];

				// 星际物流先考虑恒星之间的距离

				// 获取那两个星际站所在的恒星			
				StarData starData1 = galaxy.StarById(stationComponent1.planetId / 100);
				StarData starData2 = galaxy.StarById(stationComponent2.planetId / 100);
				// 计算距离
				int d1 = (int)((starData.uPosition - starData1.uPosition).sqrMagnitude);
				int d2 = (int)((starData.uPosition - starData2.uPosition).sqrMagnitude);

				if (d1 < d2)
					oldvalue.Add(other1 * 10000 + other2, -1);
				if (d1 > d2)
					oldvalue.Add(other1 * 10000 + other2, 1);
				if (oldvalue.ContainsKey(other1 * 10000 + other2))
					return oldvalue[other1 * 10000 + other2];

				// 距离相同，计算优先级，优先级越高越靠前
				int property1 = StationComponentPatch.GetStationComponentPatchDataPriority(stationComponent1);
				int property2 = StationComponentPatch.GetStationComponentPatchDataPriority(stationComponent2);

				if (property1 > property2)
					oldvalue.Add(other1 * 10000 + other2, -1);
				if (property1 < property2)
					oldvalue.Add(other1 * 10000 + other2, 1);
				if (oldvalue.ContainsKey(other1 * 10000 + other2))
					return oldvalue[other1 * 10000 + other2];

				// 都没法判断，根据ID，早的放前面
				if (other1 < other2)
					oldvalue.Add(other1 * 10000 + other2, -1);
				else if (other1 > other2)
					oldvalue.Add(other1 * 10000 + other2, 1);
				if (oldvalue.ContainsKey(other1 * 10000 + other2))
					return oldvalue[other1 * 10000 + other2];
				else
					return 0;
			});
			__instance.remotePairProcess = 0;

		}

		[HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "InternalTickRemote")]
		public static void InternalTickRemote_Prefix(ref StationComponent __instance, PlanetFactory factory, int timeGene, float shipSailSpeed, float shipWarpSpeed, int shipCarries, StationComponent[] gStationPool, AstroData[] astroPoses, ref VectorLF3 relativePos, ref Quaternion relativeRot, bool starmap, int[] consumeRegister)
		{
			if (timeGene == __instance.gene && __instance.remotePairCount > 0 && __instance.idleShipCount > 0 && __instance.energy > 6000000)
			{
				// 把游标挪到0
				__instance.remotePairProcess = 0;
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(StationComponent), "RematchLocalPairs")]
		public static void RematchLocalPairs_Postfix(ref StationComponent __instance, StationComponent[] stationPool, int stationCursor, int keyStationId, int droneCarries)
		{
			if (__instance.localPairs == null) return;
			if (!StationComponentPatch.done) return;

			int id = __instance.id;
			Vector3 droneDock = __instance.droneDock;
			Dictionary<int, int> oldvalue = new Dictionary<int, int>();   // 为了解决一个奇怪的问题，这里用一个变量来保存旧的值
			// 根据距离对所当前所有的本地飞船进行排序
			Array.Sort(__instance.localPairs, (p1, p2) =>
			{

				// 获取到对端的行星站
				int other1 = p1.supplyId, other2 = p2.supplyId;
				if (other1 == id)
					other1 = p1.demandId;
				if (other2 == id)
					other2 = p2.demandId;
				if (other1 == 0) return 1;
				if (other2 == 0) return -1;
				if (other1 == other2) return 0; // 是自己，那就不用排序了【这种情况真的会发生吗？】

				// 为了解决莫名其妙出现的排序不同值问题
				if (oldvalue.ContainsKey(other1 * 10000 + other2))
					return oldvalue[other1 * 10000 + other2];
				else if (oldvalue.ContainsKey(other2 * 10000 + other1))
					return -oldvalue[other2 * 10000 + other1];

				StationComponent stationComponent1 = stationPool[other1], stationComponent2 = stationPool[other2];
				// 行星内物流优先考虑优先级

				// 计算优先级，优先级越高越靠前
				int property1 = StationComponentPatch.GetStationComponentPatchDataPriority(stationComponent1);
				int property2 = StationComponentPatch.GetStationComponentPatchDataPriority(stationComponent2);

				if (property1 > property2)
					oldvalue.Add(other1 * 10000 + other2, -1);
				if (property1 < property2)
					oldvalue.Add(other1 * 10000 + other2, 1);
				if (oldvalue.ContainsKey(other1 * 10000 + other2))
					return oldvalue[other1 * 10000 + other2];

				// 计算到本站到两个行星站的距离
				int d1 = (int)((droneDock - stationComponent1.droneDock).sqrMagnitude);
				int d2 = (int)((droneDock - stationComponent2.droneDock).sqrMagnitude);

				if (d1 < d2)
					oldvalue.Add(other1 * 10000 + other2, -1);
				if (d1 > d2)
					oldvalue.Add(other1 * 10000 + other2, 1);
				if (oldvalue.ContainsKey(other1 * 10000 + other2))
					return oldvalue[other1 * 10000 + other2];
				// 都没法判断，根据ID，早的放前面

				if (other1 < other2)
					oldvalue.Add(other1 * 10000 + other2, -1);
				else if (other1 > other2)
					oldvalue.Add(other1 * 10000 + other2, 1);
				if (oldvalue.ContainsKey(other1 * 10000 + other2))
					return oldvalue[other1 * 10000 + other2];
				else
					return 0;
			});
			__instance.localPairProcess = 0;

		}
		[HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "InternalTickLocal")]
		public static void InternalTickLocal_Prefix(ref StationComponent __instance, PlanetFactory factory, int timeGene, float power, float droneSpeed, int droneCarries, StationComponent[] stationPool)
		{
			int num16 = __instance.workDroneCount + __instance.idleDroneCount;
			if (timeGene == __instance.gene % 20 || (num16 >= 75 && timeGene % 10 == __instance.gene % 10))
			{
				// 把游标挪到0
				__instance.localPairProcess = 0;
			}
		}
	}
}
