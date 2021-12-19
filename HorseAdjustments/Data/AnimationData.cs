using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HorseAdjustments.Data
{
	public class AnimationData
	{
		public Point FarmerOffset = new Point(-6, 0);
		public Dictionary<string, Data.FarmerFrame> FarmerFrames = null;
		public List<Data.HorseFrame> HorseFrames = null;
	}
}
