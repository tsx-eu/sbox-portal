using System;
using System.Collections.Generic;
using Sandbox;

namespace PortalGame
{
	
	[Library( "portal_wall" )]
	[Hammer.Solid]
	public partial class Wall : ModelEntity {

		public override void Spawn()
		{
			base.Spawn();
			Transmit = TransmitType.Always;
		}

		public void Carve(Portal entrance) {
			EnableAllCollisions = false;
			EnableSolidCollisions = true;
			RemoveCollisionLayer( CollisionLayer.Player );

			Log.Info( "carved" + this);
		}
		public void Reset() {
			EnableAllCollisions = true;
			EnableSolidCollisions = true;
			AddCollisionLayer( CollisionLayer.Player );

			Log.Info( "reset" );
		}
	}
}
