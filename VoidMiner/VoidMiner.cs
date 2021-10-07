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

namespace VoidMiner
{

	[BepInPlugin("klarkxy.dsp.VoidMiner", "虚空矿机", "0.1.5")]
	public class VoidMiner : BaseUnityPlugin
	{
		private static ManualLogSource s_logger;
		private static void LogInfo(string msg)
		{
#if DEBUG
			s_logger.LogInfo(msg);
#endif
		}

		public void Awake()
		{
			s_logger = Logger;
			try
			{
				Harmony.CreateAndPatchAll(typeof(VoidMiner));
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				LogInfo(e.ToString());
			}
		}
		private static ConfigEntry<uint> extraVeinCount;
		private static ConfigEntry<uint> extraOilCount;
		private static ConfigEntry<uint> extraWaterCount;
		private static ConfigEntry<uint> frameInterval;
		public void Start()
		{
			extraVeinCount = Config.Bind("config", "ExtraVeinCount", 6u, "每次虚空挖矿获得的矿物数量。The amount of minerals obtained in each void mining. ");
			extraOilCount = Config.Bind("config", "ExtraOilCount", 6u, "每次虚空炼油获得的石油数量。The amount of oil obtained in each void refining. ");
			extraWaterCount = Config.Bind("config", "ExtraWaterCount", 6u, "每次虚空抽水获得的水数量。The amount of water obtained for each void pumping. ");
			frameInterval = Config.Bind("config", "FrameInterval", 60u, "两次虚空挖矿的间隔。The interval between two void mining. ");
		}

		static long frame = 0;
		void Update()
		{
			frame++;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(MinerComponent), "InternalUpdate")]
		public static void InternalUpdate_Prefix(ref MinerComponent __instance, PlanetFactory factory, VeinData[] veinPool, float power, float miningRate, float miningSpeed, int[] productRegister)
		{
				if (power < 0.1f)
				return;

			if (__instance.productCount >= 45)
				return;

			if (__instance.type != EMinerType.Water && __instance.veinCount <= 0)
				return;

			// 没科技的情况下60帧扫一遍
			if (miningSpeed <= 0)
				return;

			int baseSpeed = (int)(frameInterval.Value / (miningSpeed));
			if (baseSpeed <= 0)
				baseSpeed = 1;

			// 分流，保证不会某一帧特别卡
			if (frame % baseSpeed != __instance.id % baseSpeed)
				return;

			if (factory == null || veinPool == null)
				return;

			// 抽水机直接＋就完事了，不用考虑扣东西
			if (__instance.type == EMinerType.Water)
			{
				LogInfo(String.Format("工厂:{0} 水机:{1} 类型:{2} 产物:{3}({4})", factory.index, __instance.id, __instance.type, factory.planet.waterItemId, __instance.productId));
				for (int i = 0; i < extraWaterCount.Value && __instance.productCount < 45; i++)
				{
					__instance.productCount++;

					lock (productRegister)
					{
						productRegister[factory.planet.waterItemId]++;
					}
				}
				return;
			}

			// 矿和石油走另外的逻辑
			int productId = __instance.productId;

			for (int i = 0; i < __instance.veinCount && productId == 0; i++)
				productId = veinPool[__instance.veins[i]].productId;

			LogInfo(String.Format("工厂:{0} 矿机:{1} 类型:{2} 产物:{3}({4})", factory.index, __instance.id, __instance.type, productId, __instance.productId));

			if (productId == 0)
				return;


			int count = 0;
			for (int i = 0; i < veinPool.Length && __instance.productCount < 45; i++)
			{
				ref VeinData vein = ref veinPool[i];

				if (vein.id == 0 || productId != vein.productId || vein.minerCount > 0)
					continue;

				switch (__instance.type)
				{
					case EMinerType.Vein:
						if (count > extraVeinCount.Value)
							return;
						break;
					case EMinerType.Oil:
						if (count > extraOilCount.Value)
							return;
						break;
				}

				lock (veinPool)
				{
					if (vein.id != 0 && vein.amount > 0)
					{
						count++;
						//__instance.time -= __instance.period;
						__instance.productCount++;

						lock (productRegister)
						{
							productRegister[productId]++;
						}

						if (miningRate > 0f)
						{
							if (__instance.type == EMinerType.Vein)
							{
								bool flag3 = true;
								if (miningRate < 0.99999f)
								{
									__instance.seed = (uint)((ulong)(__instance.seed % 2147483646U + 1U) * 48271UL % 2147483647UL) - 1U;
									flag3 = (__instance.seed / 2147483646.0 < (double)miningRate);
								}
								if (flag3)
								{
									vein.amount = vein.amount - 1;
									if (vein.amount < __instance.minimumVeinAmount)
									{
										__instance.minimumVeinAmount = vein.amount;
									}
									factory.planet.veinAmounts[(int)vein.type] -= 1L;
									PlanetData.VeinGroup[] veinGroups = factory.planet.veinGroups;
									short groupIndex = vein.groupIndex;
									veinGroups[(int)groupIndex].amount = veinGroups[(int)groupIndex].amount - 1L;
									factory.veinAnimPool[i].time = ((vein.amount >= 20000) ? 0f : (1f - (float)vein.amount * 5E-05f));
									if (vein.amount <= 0)
									{
										PlanetData.VeinGroup[] veinGroups2 = factory.planet.veinGroups;
										short groupIndex2 = vein.groupIndex;
										veinGroups2[(int)groupIndex2].count = veinGroups2[(int)groupIndex2].count - 1;
										factory.RemoveVeinWithComponents(i);
										factory.NotifyVeinExhausted();
										__instance.GetMinimumVeinAmount(factory, veinPool);
									}
								}
							}
							if (__instance.type == EMinerType.Oil && vein.amount > 2500)
							{
								__instance.seed = (uint)((ulong)(__instance.seed % 2147483646U + 1U) * 48271UL % 2147483647UL) - 1U;
								if (__instance.seed / 2147483646.0 < (double)miningRate)
								{
									vein.amount = vein.amount - 1;
									factory.planet.veinAmounts[(int)vein.type] -= 1L;
									PlanetData.VeinGroup[] veinGroups3 = factory.planet.veinGroups;
									short groupIndex3 = vein.groupIndex;
									veinGroups3[(int)groupIndex3].amount = veinGroups3[(int)groupIndex3].amount - 1L;
									factory.veinAnimPool[vein.id].time = ((vein.amount >= 25000) ? 0f : (1f - (float)vein.amount * VeinData.oilSpeedMultiplier));
								}
							}
						}
					}
				}
			}
		}
	}
}
