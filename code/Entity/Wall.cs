using System;
using System.Collections.Generic;
using Sandbox;

namespace PortalGame
{
	
	[Library( "portal_wall" )]
	[Hammer.Solid]
	public partial class Wall : ModelEntity
	{

		public override void Spawn() {
			base.Spawn();
			EnableSolidCollisions = true;
		}

		public void Carve(Portal entrance, Portal exit) {
			EnableAllCollisions = false;
			EnableSolidCollisions = true;
			RemoveCollisionLayer( CollisionLayer.Player );
		}
		public void Reset() {
			EnableAllCollisions = true;
			EnableSolidCollisions = true;
			AddCollisionLayer( CollisionLayer.Player );
		}
	}
}
