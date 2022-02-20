using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.UI;

namespace Sandbox
{
	public class HUD : Sandbox.HudEntity<RootPanel>
	{
		public HUD()
		{
			if( IsClient )
				RootPanel.AddChild<RenderTargets>();
		}
	}

	public class RenderTargets : Panel
	{
		public static Queue<Action> Render = new();

		public override void DrawBackground( ref RenderState state ) {

			while( Render.Count > 0 )
			{
				var act = Render.Dequeue();
				act();
			}

		}
	}
}
