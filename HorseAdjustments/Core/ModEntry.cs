using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;

namespace HorseAdjustments
{
	public class ModEntry : Mod
	{
		private class IgnoreNullResolver : IValueResolver<object, object, object>
		{
			public object Resolve(object source, object dest, object sourceMember, ResolutionContext context)
			{
				return 
					//sourceMember != null ? source : dest
					source
					;
			}
		}

		public static Data.Content ActiveContent = null;
		public static Dictionary<string, Texture2D> DrawTextures = null;


		public override void Entry(IModHelper helper)
		{
			this.Helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
		}

		private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
		{
			if (this.LoadContentPacks())
			{
				//helper.Content.AssetEditors.Add(new AssetManager(helper));

				HarmonyPatches.ApplyHarmonyPatches(helper: this.Helper, monitor: this.Monitor, id: this.ModManifest.UniqueID);
			}
			else
			{
				this.Monitor.Log("No content packs were loaded. No changes will be made.",
					LogLevel.Trace);
			}
		}

		public bool LoadContentPacks()
		{
			// Clear content packs
			var contentPacks = new List<Data.ContentPack>();
			ModEntry.DrawTextures = new Dictionary<string, Texture2D>();

			// Add default content pack
			contentPacks.Add(this.Helper.Content.Load
				<Data.ContentPack>
				(AssetManager.LocalDefaultContentPackAssetPath + ".json"));

			// Add subsequent content packs
			contentPacks.AddRange(this.Helper.ContentPacks
				.GetOwned()
				.ToList()
				.Select(contentPack => contentPack.ReadJsonFile<Data.ContentPack>(Data.ContentPack.DataAssetName + ".json"))
				.ToList());

			// Populate active content
			Data.ContentPack activeContentPack = new Data.ContentPack();
			/*ModEntry.RecursiveFieldAction(
				outer: activeContentPack,
				action: (FieldInfo inner, object outer) =>
				{
					// Set default values
					inner.SetValue(obj: outer, value: default);
				});*/
			foreach (Data.ContentPack contentPack in contentPacks)
			{
				/*ModEntry.StartRecursiveFieldAction(
					outer: contentPack,
					action: (FieldInfo inner, object outer) =>
					{
						// Set assigned values from content packs
						if (inner.GetValue(obj: contentPack) is object value && value != null)
						{
							inner.SetValue(obj: outer, value: value);

							// Populate DrawTextures dictionary
							if (inner.FieldType.Equals(typeof(Data.DrawEntry))
								&& inner.GetValue(obj: outer).GetType().GetField(name: nameof(Data.DrawEntry.SourceTexture)).GetValue(obj: inner) is string sourceTexture
								&& !string.IsNullOrEmpty(sourceTexture)
								&& !ModEntry.DrawTextures.ContainsKey(sourceTexture))
							{
								try
								{
									ModEntry.DrawTextures[sourceTexture] = this.Helper.Content.Load<Texture2D>(sourceTexture);
								}
								catch (Exception e)
								{
									this.Monitor.Log($"Could not load {nameof(Data.DrawEntry.SourceTexture)} '{sourceTexture}'.{Environment.NewLine}{e}");
								}
							}
						}
					});*/

				var config = new MapperConfiguration(
					cfg =>
					{
						cfg.CreateMap<Data.ContentPack, Data.ContentPack>()
							//.ForAllMembers(options => options.Condition((src, dest, srcMember, destMember) => srcMember != null))
							//.ForAllMembers(options => options.MapFrom(new IgnoreNullResolver()))
							/*.ForAllPropertyMaps(
								pm => pm.TypeMap.SourceType == typeof(object),
								(pm, c) => c.MapFrom(new IgnoreNullResolver(), pm.SourceMember.Name))*/
							.ForAllMembers(options => options.Condition((src, dest, srcMem, destMem) => Nullable.GetUnderlyingType(srcMem.GetType()) == null || srcMem != null))
						;
					});

				/*
				var config = new MapperConfiguration(cfg =>
				{
				});
				*/
				var mapper = config.CreateMapper();
				activeContentPack = mapper.Map<Data.ContentPack>(contentPack);
			}

			return contentPacks.Count > 1;
		}

		/*internal static void StartRecursiveFieldAction(object outer, Action<FieldInfo, object> action)
		{
			ModEntry.RecursiveFieldAction(
				field: outer.GetType().GetFields().First(),
				outer: outer,
				action: action);
		}

		internal static void RecursiveFieldAction(FieldInfo field, object outer, Action<FieldInfo, object> action)
		{
			if (field == null)
				return;

			foreach (FieldInfo inner in outer.GetType().GetFields())
			{
				if (inner.GetValue(obj: outer) == null)
				{
					action(inner, outer);
					ModEntry.RecursiveFieldAction(field: inner, outer: field, action: action);
				}
			}
		}*/

		internal static object GetEntryForDirection(Data.DirectionContainer container, int direction)
		{
			string d = new[] { "u", "r", "d", "l" }[direction];
			return container.Directions
				.FirstOrDefault(pair => pair.Key.StartsWith(d, System.StringComparison.InvariantCultureIgnoreCase))
				.Value;
		}
	}
}
