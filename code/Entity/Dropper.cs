using System;
using Sandbox;

namespace Portal
{

	[Library( "portal_dropper" )]
	[Hammer.Model( Model = "models/props_backstage/item_dropper.vmdl" )]
	[Hammer.BoxSize(0.75f)]
	public partial class Dropper : AnimEntity
	{
		public override void Spawn()
		{
			base.Spawn();
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		}
	}
}
