using System.Linq;
using Sandbox;

namespace PortalGame
{

	[Library( "portal_camera" )]
	[Hammer.Model( Model = "models/props/security_camera.vmdl" )]
	public partial class SecurityCamera : AnimEntity
	{
		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/props/security_camera.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

			SetAnimGraph( "animgraphs/security_camera.vanmgrph" );
			ResetAnimParameters();

		}

		[Event.Tick.Server]
		public void OnServerTick()
		{
			SetAnimParameter( "disabled", false );
			SetAnimParameter( "broken", false );

			EyeRotation = Rotation.Random;
			EyeLocalRotation = Rotation.Random;

			// TODO: Find nearest player
			var p = FindInSphere( Position, 1024 ).Where( i => i is PortalPlayer ).FirstOrDefault();
			if ( p != null ) {
				SetAnimParameter( "disabled", false );
				SetLookAt( "target", p.Position );
			}
			else
			{
				SetAnimParameter( "disabled", true );
			}
		}

		public virtual void SetLookAt( string name, Vector3 Position )
		{
			var localPos = (Position - EyePosition) * Rotation.Inverse;
			SetAnimParameter( name, localPos );
		}
	}
}
