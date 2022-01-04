using HarmonyLib; // el diavolo nuevo
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
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
				if (!__instance.isRidingHorse())
					return true;

				Data.AnimationData animations = ModEntry.ActiveContent.Animations;

				__instance.xOffset = animations.FarmerOffset.X;
				__instance.yOffset = animations.FarmerOffset.Y;

				// Farmer frames
				{
					Data.FarmerFrame frame = ModEntry.GetEntryForDirection(container: animations.FarmerFrames, direction: __instance.FacingDirection) as Data.FarmerFrame;
					__instance.FarmerSprite.setCurrentSingleFrame(
						which: frame.Frame,
						interval: frame.Duration,
						secondaryArm: false,
						flip: __instance.flip);
					__instance.xOffset += frame.OffsetX;
					__instance.yOffset += frame.OffsetY;
				}

				// Horse frames
				if (__instance.isMoving())
				{
					int whichFrame = Math.Min(__instance.mount.Sprite.currentAnimationIndex, animations.HorseFrames.Count - 1);
					Data.HorseFrame frame = animations.HorseFrames[whichFrame];
					__instance.xOffset += frame.Offset.Value.X;
					__instance.yOffset += frame.Offset.Value.Y;
				}

				return false;
			}
			catch (Exception e)
			{
				HarmonyPatches.ErrorHandler(e);
			}
			return true;
		}

		public static bool Horse_Draw_Prefix(
			ref Horse __instance,
			SpriteBatch b
		)
		{
			try
			{
				var drawPosition = __instance.getLocalPosition(Game1.viewport);
				var horseCentre = __instance.GetBoundingBox().Center;

				__instance.flip = __instance.FacingDirection == 3;
				__instance.Sprite.UpdateSourceRect();

				Data.DirectionEntry directionEntry = ModEntry.GetEntryForDirection(
					container: ModEntry.ActiveContent.Horse,
					direction: __instance.FacingDirection)
					as Data.DirectionEntry;

				// Draw horse sprite
				// Replaces 'base.draw(b)'
				__instance.Sprite.draw(
					b: b,
					screenPosition: drawPosition + __instance.Position,
					layerDepth: horseCentre.Y / 10000f);

				// Draw overlaid sprites
				if (__instance.FacingDirection == 2 && __instance.rider != null)
				{
					b.Draw(
						texture: ModEntry.DrawTextures.TryGetValue(directionEntry.Draw.SourceTexture, out Texture2D texture) && texture != null
							? texture
							: __instance.Sprite.Texture,
						position: drawPosition + new Vector2(48f, -24f - __instance.rider.yOffset),
						sourceRectangle: directionEntry.Draw.SourceRectangle.Value,
						color: Color.White,
						rotation: 0f,
						origin: Vector2.Zero,
						scale: Game1.pixelZoom,
						effects: SpriteEffects.None,
						layerDepth: (__instance.Position.Y + Game1.tileSize) / 10000f);
				}

				// Draw horse hats
				if (__instance.hat.Value == null || directionEntry.Hide.Value)
				{
					return false;
				}

				Data.HatEntry hatEntry = ModEntry.ActiveContent.Hats.Entries[__instance.hat.Value.which.Value];
				Vector2 hatOffset = Utility.PointToVector2(hatEntry.Offset.Value) * Game1.pixelZoom;
				/*if (__instance.shakeTimer > 0)
				{
					hatOffset += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
				}*/
				float horse_draw_layer = horseCentre.Y / 10000f;
				if (__instance.rider != null)
				{
					horse_draw_layer = (__instance.FacingDirection == 0)
						? ((__instance.position.Y + (Game1.tileSize / 2)) / 10000f)
						: ((__instance.FacingDirection != 2)
							? ((__instance.position.Y + Game1.tileSize - 1f) / 10000f)
							: ((__instance.position.Y + Game1.tileSize + ((__instance.rider != null) ? 1 : 1)) / 10000f));
				}

				directionEntry = ModEntry.GetEntryForDirection(
					container: hatEntry,
					direction: __instance.FacingDirection)
					as Data.DirectionEntry;

				if (directionEntry.Hide.Value)
					return false;

				bool isHatBouncing = false;
				horse_draw_layer += (1 / 10000f); // 1E-07f
				Vector2 hatPosition = Vector2.Zero;
				switch (__instance.Sprite.CurrentFrame)
				{
					case 0:
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
					case 6:
						hatPosition = new Vector2(30f, -42f);
						isHatBouncing = true;
						break;
					case 7:
					case 8:
						{
							hatPosition = new Vector2(70f, -74f);
						}
						break;
					case 9:
						{
							hatPosition = new Vector2(70f, -70f);
						}
						break;
					case 11:
					{
						hatPosition = new Vector2(66f, -74f);
					}
					break;
					case 10:
					case 12:
						{
							hatPosition = new Vector2(66f, -78f);
						}
						break;
					case 13:
						{
							hatPosition = new Vector2(70f, -78f);
						}
						break;
					case 21:
						{
							hatPosition = new Vector2(66f, -66f);
						}
						break;
					case 22:
						{
							hatPosition = new Vector2(70f, -54f);
						}
						break;
					case 23:
					case 24:
						{
							hatPosition = new Vector2(70f, -42f);
						}
						break;
					case 14:
					case 15:
					case 16:
					case 17:
					case 18:
					case 19:
					case 20:
					case 25:
						hatPosition = new Vector2(28f, -106f);
						isHatBouncing = true;
						break;
				}
				
				if (isHatBouncing)
				{
					hatPosition.Y -= __instance.rider?.yOffset ?? 0f;
				}
				else if (__instance.flip)
				{
					//  X  flipX   range  diffX   diffFlipX
					// 70   -18  |  88     -         -
					// 66   -14  |  80     4         4
					// 30   -42  |  72     36        28
					hatPosition.X -= Game1.tileSize;
					hatPosition.X -= (30 - hatPosition.X);
				}

				__instance.hat.Value.draw(
					spriteBatch: b,
					location: drawPosition + hatPosition + hatOffset,
					scaleSize: 1f + (1f / 3),
					transparency: 1f,
					layerDepth: horse_draw_layer,
					direction: 0);
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