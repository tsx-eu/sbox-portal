using System;
using System.Collections.Generic;
using Sandbox.UI;

namespace PortalGame
{
	public class HUD : Sandbox.HudEntity<RootPanel>
	{
		public HUD()
		{
			if ( !IsClient )
				return;

			RootPanel.StyleSheet.Load( "/HUD.scss" );
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
