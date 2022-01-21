using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace IntelligentTransport
{
	[BepInPlugin("klarkxy.dsp.IntelligentTransport", "智能物流计划", "1.0.0")]
	public class IntelligentTransport : BaseUnityPlugin
	{
		private static ManualLogSource s_logger;
		private static void LogInfo(string msg)
		{
			s_logger.LogInfo(msg);
		}

		public void Awake()
		{
			s_logger = Logger;
			try
			{
				Harmony.CreateAndPatchAll(typeof(IntelligentTransport));
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				LogInfo(e.ToString());
			}
		}
		public void Start()
		{
		}
		public void Update()
		{
		}

		public static GalaxyData galaxy;

		[HarmonyPostfix, HarmonyPatch(typeof(UniverseGen), "CreateGalaxy")]
		public static void CreateGalaxy_Postfix(GameDesc gameDesc, ref GalaxyData __result)
		{
			galaxy = __result;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(StationComponent), "RematchRemotePairs")]
		public static void RematchRemotePairs_Postfix(ref StationComponent __instance, StationComponent[] gStationPool,	int gStationCursor,	int keyStationGId,	int shipCarries)
		{
			if (__instance.remotePairs == null) return;

			int gid = __instance.gid;
			int planetId = __instance.planetId;
			StarData starData = galaxy.StarById(planetId / 100);
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

				// 获取另外两个星际站
				StationComponent stationComponent1 = gStationPool[other1];
				StationComponent stationComponent2 = gStationPool[other2];
				// 获取那两个星际站所在的恒星			
				StarData starData1 = galaxy.StarById(stationComponent1.planetId / 100);
				StarData starData2 = galaxy.StarById(stationComponent2.planetId / 100);
				// 计算距离
				double d1 = (starData.uPosition - starData1.uPosition).sqrMagnitude;
				double d2 = (starData.uPosition - starData2.uPosition).sqrMagnitude;
				if (d1 < d2)
					return -1;
				if (d1 > d2)
					return 1;
				return 0;
			});
#if (DEBUG)
			// 打印排序日志
			string str = "";
			Array.ForEach(__instance.remotePairs, (p) =>
			{
				if (p.supplyId <= 0 || p.demandId <= 0)
					return;

				StationComponent supply = gStationPool[p.supplyId], demand = gStationPool[p.demandId];
				double d1 = (supply.droneDock - demand.droneDock).sqrMagnitude;
				str += string.Format("{4}.{0}.{1}->{5}.{2}.{3}\n", supply.gid, supply.id, demand.gid, demand.id, supply.planetId, demand.planetId);
			});
			LogInfo(string.Format("{0}.{1}[远程] 进行排序: \n{2}", __instance.gid, __instance.id, str));
#endif
		}

		[HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "InternalTickRemote")]
		public static void InternalTickRemote_Prefix(ref StationComponent __instance, int timeGene, double dt, float shipSailSpeed, float shipWarpSpeed, int shipCarries, StationComponent[] gStationPool, AstroPose[] astroPoses, VectorLF3 relativePos, Quaternion relativeRot, bool starmap, int[] consumeRegister)
		{
			if (timeGene == __instance.gene)
			{
				// 把游标挪到0
				__instance.remotePairProcess = 0;
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(StationComponent), "RematchLocalPairs")]
		public static void RematchLocalPairs_Postfix(ref StationComponent __instance, StationComponent[] stationPool, int stationCursor, int keyStationId, int droneCarries)
		{
			if (__instance.localPairs == null) return;

			int id = __instance.id;
			Vector3 droneDock = __instance.droneDock;
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

				StationComponent stationComponent1 = stationPool[other1], stationComponent2 = stationPool[other2];
				// 计算到本站到两个行星站的距离
				double d1 = (droneDock - stationComponent1.droneDock).sqrMagnitude;
				double d2 = (droneDock - stationComponent2.droneDock).sqrMagnitude;
				if (d1 < d2)
					return -1;
				if (d1 > d2)
					return 1;
				return 0;
			});
#if (DEBUG)
			// 打印排序日志
			string str = "";
			Array.ForEach(__instance.localPairs, (p) =>
			{
				if (p.supplyId <= 0 || p.demandId <= 0)
					return;

				StationComponent supply = stationPool[p.supplyId], demand = stationPool[p.demandId];
				double d1 = (supply.droneDock - demand.droneDock).sqrMagnitude;
				str += string.Format("{0}.{1}->{2}.{3}\n", supply.gid, supply.id, demand.gid, demand.id);
			});
			LogInfo(string.Format("{0}.{1}[本地] 进行排序: \n{2}", __instance.gid, __instance.id, str));
#endif
		}
		[HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "InternalTickLocal")]
		public static void InternalTickLocal_Prefix(ref StationComponent __instance, int timeGene, float dt, float power, float droneSpeed, int droneCarries, StationComponent[] stationPool)
		{
			if (timeGene == __instance.gene % 20)
			{
				// 把游标挪到0
				__instance.localPairProcess = 0;
			}
		}
	}
}
