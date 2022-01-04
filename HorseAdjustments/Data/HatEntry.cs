using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HorseAdjustments.Data
{
	public class HatEntry : Data.DirectionContainer
	{
		public List<int> Target = null;
		public Point? Offset = null;
		public bool? IsBouncy = null;
	}
}
