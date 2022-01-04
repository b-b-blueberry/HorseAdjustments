using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HorseAdjustments.Data
{
	public class AnimationData
	{
		public Point FarmerOffset = new Point(-6, 0);
		public Data.AnimationFarmerEntry FarmerFrames = null;
		public List<Data.HorseFrame> HorseFrames = null;
	}
}
