
namespace Sandbox
{
	public partial class PortalTraveller : EntityComponent
	{
		public Vector3 previousOffsetFromPortal { get; set; }

		public virtual void Teleport(Portal from, Portal to) {
			Entity.Position = from.GetPosition( Entity );

			if ( Host.IsClient )
				Log.Info( "client" );

			if ( Host.IsClient )
				Log.Info( "client" );

			(Entity as Pawn).CameraMode.Rotation = Rotation.Random;
			(Entity as Pawn).EyeRotation = Rotation.Random;
			
		}

		[ClientCmd( "test_camera" )]
		public static void Toast()
		{
			Local.Pawn.ResetInterpolation();
			(Local.Pawn as Player).Controller.Rotation = Rotation.LookAt( Vector3.Left );
			(Local.Pawn as Player).CameraMode.Rotation = Rotation.LookAt( Vector3.Left );
			(Local.Pawn as Player).Rotation = Rotation.LookAt( Vector3.Left );
			(Local.Pawn as Player).EyeRotation = Rotation.LookAt( Vector3.Left );
			Local.Pawn.Rotation = Rotation.LookAt(Vector3.Left);
			(Local.Pawn as Pawn).CameraMode = new GameCamera( Local.Pawn.Transform.Position , Rotation.Random );
			Log.Info( "changed" );
		}

		public virtual void EnterPortalThreshold(Portal portal) {

		}
		public virtual void ExitPortalThreshold(Portal portal) {

		}

	}
}
