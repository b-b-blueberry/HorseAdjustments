using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.IO;

namespace HorseAdjustments
{
	public class AssetManager : IAssetLoader, IAssetEditor
	{
		internal static readonly string LocalRootAssetPath
			= "assets";
		internal static readonly string LocalDefaultContentPackAssetPath
			= Path.Combine(AssetManager.LocalRootAssetPath, "content");

		private IModHelper _helper;


		public AssetManager(IModHelper helper)
		{
			this._helper = helper;
		}

		public bool CanLoad<T>(IAssetInfo asset)
		{
			return false;
		}

		public T Load<T>(IAssetInfo asset)
		{
			return default;
		}

		public bool CanEdit<T>(IAssetInfo asset)
		{
			return false;
		}

		public void Edit<T>(IAssetData asset)
		{
		}
	}
}
