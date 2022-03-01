using Sandbox;

namespace Portal
{

	[Library( "portal_cube" )]
	[Hammer.Model( Model = "models/props/metal_box.vmdl" )]
	public partial class Cube : Prop, ButtonTriggerable
	{

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/props/metal_box.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			PhysicsEnabled = true;
		}

	}
}
