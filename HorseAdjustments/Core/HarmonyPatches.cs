using HarmonyLib; // el diavolo nuevo
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HorseAdjustments
{
	public static class HarmonyPatches
	{
		private static IModHelper _helper;
		private static IMonitor _monitor;

		private const string ConstructorName = ".ctor";
		private const char PatchDelimiter = '_';

		#region Patch method prototypes

		private class Patch
		{
			public readonly Type TargetType;
			public readonly string TargetMethod;
			public readonly Type PatchType;
			public readonly string PatchMethod;
			public readonly Type[] TargetParams;

			public Patch(
				Type targetType, string targetMethod,
				Type patchType = null, string patchMethod = null,
				Type[] targetParams = null)
			{
				this.TargetType = targetType;
				this.TargetMethod = targetMethod;
				this.PatchType = patchType ?? typeof(HarmonyPatches);
				this.PatchMethod = patchMethod;
				this.TargetParams = targetParams;
			}
		}

		private static readonly Patch[] Patches = new Patch[]
		{
			new Patch(
				targetType: typeof(StardewValley.Farmer),
				targetMethod: "showRiding",
				patchMethod: nameof(HarmonyPatches.Farmer_ShowRiding_Prefix)),
		};

		#endregion

		#region Patch boilerplate

		public static void ApplyHarmonyPatches(IModHelper helper, IMonitor monitor, string id)
		{
			HarmonyPatches._helper = helper;
			HarmonyPatches._monitor = monitor;
			Harmony harmony = new Harmony(id: id);

			foreach (Patch patch in Patches)
			{
				HarmonyPatches._monitor.Log($"Applying Harmony patch {patch.TargetType}{PatchDelimiter}{patch.PatchMethod}", LogLevel.Trace);

				// Generate patch method
				string harmonyTypeName = patch.PatchMethod
					.Split(PatchDelimiter)
					.Last();
				HarmonyPatchType harmonyType = (HarmonyPatchType)Enum.Parse(
					enumType: typeof(HarmonyPatchType),
					value: harmonyTypeName);
				HarmonyMethod harmonyMethod = new HarmonyMethod(
					methodType: patch.PatchType,
					methodName: patch.PatchMethod);

				// Get original method
				System.Reflection.MethodBase original = (patch.TargetMethod == ConstructorName)
					? (System.Reflection.MethodBase)AccessTools.Constructor(
						type: patch.TargetType,
						parameters: patch.TargetParams)
					: AccessTools.Method(
						type: patch.TargetType,
						name: patch.TargetMethod,
						parameters: patch.TargetParams);

				// Apply patch to original
				harmony.Patch(
					original: original,
					prefix: harmonyType == HarmonyPatchType.Prefix ? harmonyMethod : null,
					postfix: harmonyType == HarmonyPatchType.Postfix ? harmonyMethod : null,
					transpiler: harmonyType == HarmonyPatchType.Transpiler ? harmonyMethod : null,
					finalizer: harmonyType == HarmonyPatchType.Finalizer ? harmonyMethod : null);
			}
		}

		private static void ErrorHandler(Exception e)
		{
			HarmonyPatches._monitor.Log($"Error in Harmony patch:{Environment.NewLine}{e}", LogLevel.Error);
		}

		#endregion

		#region Patch method definitions

		public static bool Farmer_ShowRiding_Prefix(ref Farmer __instance)
		{
			try
			{
				if (!ModEntry.ContentPacks.Any() || !__instance.isRidingHorse())
					return true;

				Data.ContentPack contentPack = ModEntry.ContentPacks.FirstOrDefault()
					?? ModEntry.DefaultContentPack;
				Data.Content content = contentPack.Content?.FirstOrDefault();
				Data.AnimationData animations = content?.Animations ?? ModEntry.DefaultContentPack.Content.First().Animations;

				__instance.xOffset = animations.FarmerOffset.X;
				__instance.yOffset = animations.FarmerOffset.Y;

				string[] directions = new[] { "u", "l", "d", "r" };
				string direction = directions[__instance.FacingDirection];
				bool isFacingRight = direction == "r";

				// Farmer frames
				{
					Data.FarmerFrame frame = animations.FarmerFrames
						.FirstOrDefault(pair => pair.Key.StartsWith(direction, StringComparison.InvariantCultureIgnoreCase))
						.Value;
					__instance.FarmerSprite.setCurrentSingleFrame(
						which: frame.Frame,
						interval: frame.Duration,
						secondaryArm: false,
						flip: isFacingRight);
					__instance.xOffset += frame.OffsetX;
					__instance.yOffset += frame.OffsetY;
				}

				// Horse frames
				if (__instance.isMoving())
				{
					int whichFrame = Math.Min(__instance.mount.Sprite.currentAnimationIndex, animations.HorseFrames.Count - 1);
					Data.HorseFrame frame = animations.HorseFrames[whichFrame];
					__instance.xOffset += frame.OffsetX;
					__instance.yOffset += frame.OffsetY;
				}

				return false;
			}
			catch (Exception e)
			{
				HarmonyPatches.ErrorHandler(e);
			}
			return true;
		}

		#endregion
	}
}