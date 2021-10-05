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

	[BepInPlugin("klarkxy.dsp.VoidMiner", "虚空矿机", "0.1.0")]
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
		private static ConfigEntry<int> extraVeinCount;
		private static ConfigEntry<int> extraOilCount;
		private static ConfigEntry<int> extraWaterCount;
		public void Start()
		{
			extraVeinCount = Config.Bind("config", "ExtraVeinCount", 10, "每个矿机额外开采的矿脉数量。");
			extraOilCount = Config.Bind("config", "ExtraOilCount", 3, "每个油井额外开采的石油数量。");
			extraWaterCount = Config.Bind("config", "ExtraWaterCount", 3, "每个抽水机额外开采的水数量。");
		}

		public static Dictionary<int, Dictionary<int, List<int>>> veinMap = null;
		[HarmonyPostfix, HarmonyPatch(typeof(PlanetFactory), "AddVeinData")]
		public static void AddVeinData_Postfix(PlanetFactory __instance, VeinData vein)
		{
			if (veinMap == null)
				veinMap = new Dictionary<int, Dictionary<int, List<int>>>();

			if (!veinMap.ContainsKey(__instance.index))
				veinMap.Add(__instance.index, new Dictionary<int, List<int>>());
			if (!veinMap[__instance.index].ContainsKey(vein.productId))
				veinMap[__instance.index].Add(vein.productId, new List<int>());
			LogInfo(String.Format("工厂:{0} 产物:{1} 加入矿脉:{2} 数量:{3}", __instance.index, vein.productId, vein.id, vein.amount));
			veinMap[__instance.index][vein.productId].Add(vein.id);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(PlanetFactory), "Import")]
		public static void Import_Postfix(PlanetFactory __instance, int _index, GameData _gameData)
		{
			if (veinMap == null)
				veinMap = new Dictionary<int, Dictionary<int, List<int>>>();

			if (!veinMap.ContainsKey(__instance.index))
				veinMap.Add(__instance.index, new Dictionary<int, List<int>>());

			for (int i = 1; i < __instance.veinCursor; i++)
			{
				VeinData vein = __instance.veinPool[i];
				if (vein.id == i)
				{
					if (!veinMap[__instance.index].ContainsKey(vein.productId))
						veinMap[__instance.index].Add(vein.productId, new List<int>());
					LogInfo(String.Format("工厂:{0} 产物:{1} 加入矿脉:{2} 数量:{3}", __instance.index, vein.productId, vein.id, vein.amount));
					veinMap[__instance.index][vein.productId].Add(vein.id);
				}
			}

		}

		[HarmonyPrefix, HarmonyPatch(typeof(MinerComponent), "InternalUpdate")]
		public static void InternalUpdate_Prefix(ref MinerComponent __instance, PlanetFactory factory, VeinData[] veinPool, float power, float miningRate, float miningSpeed, int[] productRegister)
		{
			if (power < 0.1f)
				return;

			if (__instance.productCount >= 50)
				return;

			if (__instance.veinCount <= 0)
				return;

			if (factory == null || veinPool == null)
				return;

			int productId = __instance.productId;

			if (productId == 0)
			{
				if (__instance.type == EMinerType.Water)
					productId = factory.planet.waterItemId;
				else for (int i = 0; i < __instance.veinCount && productId == 0; i++)
						productId = veinPool[__instance.veins[i]].productId;
			}

			//LogInfo(String.Format("工厂:{5} 矿机:{0} 产物:{1}({2}) 数量:{3} 覆盖矿脉:{4}", __instance.id, productId,
			//	__instance.productId, __instance.productCount, __instance.veinCount, factory.index));

			if (productId == 0)
				return;

			if (veinMap == null || !veinMap.ContainsKey(factory.index) || !veinMap[factory.index].ContainsKey(productId))
				return;


			// 挖矿
			List<int> veinList = veinMap[factory.index][productId];
			int count = 0;
			lock (veinList)
			{
				for (int i = veinList.Count - 1; i >= 0 && __instance.productCount < 40; i--)
				{
					ref VeinData vein = ref veinPool[veinList[i]];

					if (vein.id == 0)
					{
						veinList.RemoveAt(i);
						continue;
					}

					if (productId != vein.productId)
						continue;

					if (vein.minerCount > 0)
						continue;

					//LogInfo(String.Format("工厂:{0} 矿脉:{1} 产物:{2} 数量:{3}", factory.index, vein.id, vein.productId, vein.amount));

					if (__instance.time <= __instance.period)
					{
						count++;
						switch (__instance.type)
						{
							case EMinerType.Vein:
								__instance.time += (int)(power * (float)__instance.speed * miningSpeed);
								if (count > extraVeinCount.Value)
									return;
								break;
							case EMinerType.Oil:
								__instance.time += (int)(power * (float)__instance.speed * miningSpeed * (float)vein.amount * VeinData.oilSpeedMultiplier + 0.5f);
								if (count > extraOilCount.Value)
									return;
								break;
							case EMinerType.Water:
								__instance.time += (int)(power * (float)__instance.speed * miningSpeed);
								if (count > extraWaterCount.Value)
									return;
								break;
						}
					}

					//LogInfo(String.Format("计时器 {0}/{1}", __instance.time, __instance.period));

					if (__instance.time < __instance.period)
						continue;

					lock (veinPool)
					{
						if (vein.id != 0 && vein.amount > 0)
						{
							__instance.time -= __instance.period;
							__instance.productCount++;

							//LogInfo(String.Format("{0}从{1}/{4}获取{2} 数量:{3}", __instance.id, i, productId, __instance.productCount, veinPool.Length));

							int[] obj2 = productRegister;
							lock (obj2)
							{
								productRegister[productId]++;
								//factory.AddVeinTypeMiningFlagUnsafe(vein.type);
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
}
