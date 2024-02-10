using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace BossDirections.Patches {

	[HarmonyPatch(typeof(Fireplace), "UseItem")]

	public class Fireplace_UseItem_Patch {
		[HarmonyPrefix]
		static bool Prefix(Fireplace __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result) {
			var iname = item.m_shared.m_name.ToLower();
			BossDirections.Log($"Fireplace_UseItem_Patch Prefix {iname} stack={item.m_stack}");

			var neededAmount = 0;
			var bossLocation = "";
			var bossName = "";
			var addname = false;
			var talks = new List<string>();

			BossDirections.offerings.Value.ForEach(offering => {
                foreach (var offitem in offering.items) {
					if ("$item_"+offitem.Key.ToLower() == iname) {
						neededAmount = offitem.Value;
						bossLocation = offering.location;
						bossName = offering.name;
						talks = offering.quotes;
						addname = offering.addname;
					}
				}
			});

			BossDirections.Log($"neededAmount={neededAmount} bossCode={bossLocation} bossName={bossName} talks={talks.Count}");
			

			if (bossLocation == "") {
				if (iname.Contains("wood")) return true;

				BossDirections.Log("No offering found for " + iname);
				var bossOfferings = string.Join("; ", BossDirections.offerings.Value.ConvertAll(offering => offering.name + " (" + string.Join(", ", offering.items.Keys)+")"));
				BossDirections.Log("Offerings loaded: " + bossOfferings);
				return true;
			}

			if (item.m_stack < neededAmount) {
				user.Message(MessageHud.MessageType.Center, "$msg_toofew " + iname);
				__result = true;
				return false;
			}

			var rnd = new System.Random();
			var randomIndex = rnd.Next(talks.Count);
			var bossTalk = talks[randomIndex];
			if (addname) bossTalk = $"[{bossName}]: " + bossTalk;

			user.GetInventory().RemoveItem(item.m_shared.m_name, neededAmount);
			user.Message(MessageHud.MessageType.Center, bossTalk);

			var point = __instance.gameObject.transform.position;

			if (BossDirections.config.Value.pinless) {
				BossDirections.Log("Discovering location in Pinless mode...");
				ZoneSystem.LocationInstance closest;
				ZoneSystem.instance.FindClosestLocation(bossLocation, point, out closest);
				Player.m_localPlayer.SetLookDir(closest.m_position - Player.m_localPlayer.transform.position, 3.5f);
			} else {
				BossDirections.Log("Discovering location in normal mode...");
				Game.instance.DiscoverClosestLocation(bossLocation, point, bossName, (int)Minimap.PinType.Boss, false, false);
			}

			BossDirections.Log("Offering done!");

			__result = true;
			return false;
		}
	}

}