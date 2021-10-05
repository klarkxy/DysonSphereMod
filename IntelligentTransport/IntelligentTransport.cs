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
	[BepInPlugin("klarkxy.dsp.IntelligentTransport", "智能物流计划", "0.1.1")]
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
			needSortRemote = new HashSet<int>();
			needSortLocal = new HashSet<int>();
		}
		public void Start()
		{
			SortPreFrame = Config.Bind("config", "SortPreFrame", 30, "每帧最多进行的排序次数。\nThe maximum number of sorts per frame. ");
			SortLocal = Config.Bind("config", "SortLocal", true, "是否开启本地物流优化逻辑。\nWhether to enable local transport optimization logic.  ");
			SortRemote = Config.Bind("config", "SortRemote", true, "是否开启星际物流优化逻辑。\nWhether to enable remote transport optimization logic.  ");
		}
		public void Update()
		{
			updateThisFrame = SortPreFrame.Value;
		}
		private static ConfigEntry<int> SortPreFrame;
		private static ConfigEntry<bool> SortLocal;
		private static ConfigEntry<bool> SortRemote;
		private static int updateThisFrame;

		private static HashSet<int> needSortRemote;
		private static HashSet<int> needSortLocal;

		[HarmonyPostfix, HarmonyPatch(typeof(StationComponent), "RematchRemotePairs")]
		public static void RematchRemotePairs_Postfix(StationComponent __instance, StationComponent[] gStationPool, int gStationCursor, int keyStationGId, int shipCarries)
		{
			needSortRemote.Add(__instance.gid);
		}
		[HarmonyPostfix, HarmonyPatch(typeof(StationComponent), "RematchLocalPairs")]
		public static void RematchLocalPairs_Postfix(StationComponent __instance, StationComponent[] stationPool, int stationCursor, int keyStationId, int droneCarries)
		{
			needSortLocal.Add(__instance.gid);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "InternalTickRemote")]
		public static void InternalTickRemote_Prefix(ref StationComponent __instance, int timeGene, double dt, float shipSailSpeed, float shipWarpSpeed, int shipCarries, StationComponent[] gStationPool, AstroPose[] astroPoses, VectorLF3 relativePos, Quaternion relativeRot, bool starmap, int[] consumeRegister)
		{
			if (!SortRemote.Value)
				return;
			if (timeGene == __instance.gene)
			{
				// 先判断有没有可用的飞船，如果没有就不进行后续的排序
				if (updateThisFrame <= 0 || __instance.remotePairs == null || __instance.remotePairs.Length <= 0 || __instance.QueryIdleShip(0) < 0)
					return;
				if (needSortRemote.Contains(__instance.gid))
				{
					int gid = __instance.gid;
					int planetId = __instance.planetId;
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

						StationComponent stationComponent1 = gStationPool[other1], stationComponent2 = gStationPool[other2];
						// 计算到本站到两个星际站的距离
						double d1 = (astroPoses[planetId].uPos - astroPoses[stationComponent1.planetId].uPos).sqrMagnitude /* + (double)astroPoses[planetId].uRadius + (double)astroPoses[stationComponent1.planetId].uRadius*/;
						double d2 = (astroPoses[planetId].uPos - astroPoses[stationComponent2.planetId].uPos).sqrMagnitude /* + (double)astroPoses[planetId].uRadius + (double)astroPoses[stationComponent2.planetId].uRadius*/;
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
						str += string.Format("{0}.{1}->{2}.{3}: {4}\n", supply.gid, supply.id, demand.gid, demand.id,
							(astroPoses[supply.planetId].uPos - astroPoses[demand.planetId].uPos).sqrMagnitude/* + (double)astroPoses[supply.planetId].uRadius + (double)astroPoses[demand.planetId].uRadius*/);
					});
					LogInfo(string.Format("{0}.{1}[星际] 进行排序: \n{2}", __instance.gid, __instance.id, str));
#endif
					needSortRemote.Remove(__instance.gid);
					updateThisFrame--;
				}
			}
			// 把游标挪到0
			__instance.remotePairProcess = 0;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "InternalTickLocal")]
		public static void InternalTickLocal_Prefix(ref StationComponent __instance, int timeGene, float dt, float power, float droneSpeed, int droneCarries, StationComponent[] stationPool)
		{
			if (!SortLocal.Value)
				return;
			if (timeGene == __instance.gene % 20)
			{
				// 先判断有没有可用的飞船，如果没有就不进行后续的排序
				if (updateThisFrame <= 0 || __instance.localPairs == null || __instance.localPairs.Length <= 0 || __instance.idleDroneCount <= 0)
					return;
				if (needSortLocal.Contains(__instance.gid))
				{
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
						double x = supply.droneDock.x;
						double y = supply.droneDock.y;
						double z = supply.droneDock.z;
						double x1 = demand.droneDock.x;
						double y1 = demand.droneDock.y;
						double z1 = demand.droneDock.z;
						double d1 = (supply.droneDock - demand.droneDock).sqrMagnitude;
						str += string.Format("{0}.{1}->{2}.{3}: {4}\n", supply.gid, supply.id, demand.gid, demand.id, d1);
					});
					LogInfo(string.Format("{0}.{1}[本地] 进行排序: \n{2}", __instance.gid, __instance.id, str));
#endif
					needSortLocal.Remove(__instance.gid);
					updateThisFrame--;
				}
			}
			// 把游标挪到0
			__instance.localPairProcess = 0;
		}
	}
}
