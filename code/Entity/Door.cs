using System;
using System.Collections.Generic;
using Sandbox;

namespace Portal
{
	
	[Library( "portal_door" )]
	[Hammer.Model( Model = "models/props/portal_door_combined.vmdl" )]
	public partial class Door : AnimEntity
	{

		public override void Spawn() {
			base.Spawn();
			SetModel( "models/props/portal_door_combined.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			SetAnimGraph( "animgraphs/portal_door_combined.vanmgrph" );
			ResetAnimParameters();
		}

		[Input]
		public void Open() {
			SetAnimParameter( "enable", true );
		}

		[Input]
		public void Close() {
			SetAnimParameter( "enable", false );
		}

	}
}
