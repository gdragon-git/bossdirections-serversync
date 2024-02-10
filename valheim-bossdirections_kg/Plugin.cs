using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using ServerSync;

/*
 * [0.0.1]
 * First release.
 * 
 * [0.0.2]
 * Adding more logs to show which offererings were loaded.
 * Removing deer hide from the default offerings.
 * Removing drake thophies from the default offerings.
 * Adding a check for wood in the fireplace to avoid spamming debugs.
 * Showing what offerings are loaded when trying to burn anything.
 * 
 * [0.0.3]
 * Fixing missing LitJson reference.
 * 
 * [0.1.0]
 * Adding pinless config/mode (default: true).
*/

namespace BossDirections {
	[BepInPlugin("fiote.mods.bossdirections", "BossDirections", "0.1.0")]

	public class BossDirections : BaseUnityPlugin {
		// core stuff
		
		private static ServerSync.ConfigSync configSync = new ServerSync.ConfigSync("fiote.mods.bossdirections") { DisplayName = "BossDirections", CurrentVersion = "0.1.0", MinimumRequiredVersion = "0.1.0", IsLocked = true};
        
		ConfigEntry<T> configBind<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
		{
			ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

			SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
			syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

			return configEntry;
		}

		ConfigEntry<T> configBind<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => configBind(group, name, value, new ConfigDescription(description), synchronizedSetting);
		
		public static bool debug = true;
		public static CustomSyncedValue<List<Offering>> offerings = new(configSync, "fiote.mods.bossdirections.offerings", new List<Offering>());
		public static CustomSyncedValue<Config> config = new(configSync, "fiote.mods.bossdirections.config", new Config());
		
		private void Awake() {
			Bar();
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "fiote.mods.bossdirections");
			Debug("Awake");
			LoadConfig();
			LoadOfferings();
			Bar();
		}

		void LoadConfig() {
			Line();
			Debug("LoadConfig()");
			Config result = new Config();
			var data = Loaders.LoadFromFile<Config>("config.json");
			if (data == null) Loaders.LoadFromFile<Config>("config.yml");
			if (data != null) result = data;
			config.Value = result;
			Debug("Config loaded: pinless=" + config.Value.pinless);
			
		}

		void LoadOfferings() { 
			Line();
			Debug("LoadOfferings()");
			List<Offering> result = new List<Offering>();
			var data = Loaders.LoadFromFile<Offerings>("offerings.json");
			if (data == null) data = Loaders.LoadFromFile<Offerings>("offerings.yml");
			if (data != null) result = data.offerings;
			
			foreach (var offering in offerings.Value) {
				Debug("Offering loaded: " + offering.name);
			}

			if (offerings.Value.Count == 0) {
				Error("No offerings loaded. Are you sure you have BossDirections/offerings.json on your plugins folder?");
			}
			
			offerings.Value = result;
		}	

		#region LOGGING

		public static void Bar() {
			Debug("=============================================================");
		}

		public static void Line() {
			Debug("-------------------------------------------------------------");
		}

		public static void Debug(string message) {
			if (debug) Log(message);
		}

		public static void Log(string message) {
			UnityEngine.Debug.Log($"[BossDirections] {message}");
		}

		public static void Error(string message) {
			UnityEngine.Debug.LogError($"[BossDirections] {message}");
		}

		#endregion
	}
}

public class Config : ISerializableParameter {
	public bool pinless;
	public void Serialize(ref ZPackage pkg)
	{
		pkg.Write(pinless);
	}

	public void Deserialize(ref ZPackage pkg)
	{
		pinless = pkg.ReadBool();
	}
}

public class Offerings : ISerializableParameter{
	public List<Offering> offerings;
	public void Serialize(ref ZPackage pkg)
	{
		pkg.Write(offerings.Count);
		foreach (var offering in offerings) {
			pkg.Write(offering.location ?? "");
			pkg.Write(offering.name ?? "");
			pkg.Write(offering.addname);
			pkg.Write(offering.quotes.Count);
			foreach (var quote in offering.quotes) {
				pkg.Write(quote ?? "");
			}
			pkg.Write(offering.items.Count);
			foreach (var item in offering.items) {
				pkg.Write(item.Key ?? "");
				pkg.Write(item.Value);
			}
		}
	}

	public void Deserialize(ref ZPackage pkg)
	{
		offerings = new List<Offering>();
		int count = pkg.ReadInt();
		for (int i = 0; i < count; i++) {
			Offering offering = new Offering();
			offering.location = pkg.ReadString();
			offering.name = pkg.ReadString();
			offering.addname = pkg.ReadBool();
			int quotesCount = pkg.ReadInt();
			offering.quotes = new List<string>();
			for (int j = 0; j < quotesCount; j++) {
				offering.quotes.Add(pkg.ReadString());
			}
			int itemsCount = pkg.ReadInt();
			offering.items = new Dictionary<string, int>();
			for (int j = 0; j < itemsCount; j++) {
				offering.items.Add(pkg.ReadString(), pkg.ReadInt());
			}
			offerings.Add(offering);
		}
	}
}

public class Offering {
	public string location, name;
	public bool addname;
	public List<string> quotes;
	public Dictionary<string, int> items;
}