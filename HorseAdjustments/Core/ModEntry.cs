using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Collections.Generic;
using System.Linq;

namespace HorseAdjustments
{
	public class ModEntry : Mod
	{
		internal ITranslationHelper i18n => Helper.Translation;

		public static List<IContentPack> RawContentPacks = null;
		public static List<Data.ContentPack> ContentPacks = null;
		public static Data.ContentPack DefaultContentPack;

		public override void Entry(IModHelper helper)
		{
			//helper.Content.AssetEditors.Add(new AssetManager(helper));

			ModEntry.DefaultContentPack = this.Helper.Content.Load
				<Data.ContentPack>
				(AssetManager.LocalDefaultContentPackAssetPath + ".json");

			HarmonyPatches.ApplyHarmonyPatches(helper: this.Helper, monitor: this.Monitor, id: this.ModManifest.UniqueID);

			this.Helper.Events.GameLoop.GameLaunched += (sender, e) => ModEntry.GameLoop_GameLaunched(sender, e, helper: this.Helper);
		}

		private static void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e, IModHelper helper)
		{
			ModEntry.LoadContentPacks(helper: helper);
		}

		public static void LoadContentPacks(IModHelper helper)
		{
			ModEntry.RawContentPacks = helper.ContentPacks
				.GetOwned()
				.ToList();
			ModEntry.ContentPacks = ModEntry.RawContentPacks
				.Select(contentPack => contentPack.ReadJsonFile<Data.ContentPack>(Data.ContentPack.DataAssetName + ".json"))
				.ToList();
		}
	}
}
