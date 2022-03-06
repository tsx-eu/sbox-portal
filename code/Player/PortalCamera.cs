using Sandbox;

namespace PortalGame
{
	public partial class PortalCamera : CameraMode
	{
		Vector3 lastPos;

		public PortalCamera() {

		}

		public PortalCamera(Vector3 pos, Rotation rot)
		{
			Position = pos;
			Rotation = rot;
		}

		public override void Build( ref CameraSetup camSetup )
		{
			base.Build( ref camSetup );
			camSetup.Position = Position;
			camSetup.Rotation = Rotation;
		}

		public override void Activated()
		{
			var pawn = Local.Pawn;
			if ( pawn == null ) return;

			Position = pawn.EyePosition;
			Rotation = pawn.EyeRotation;
			FieldOfView = 80.0f;
			ZNear = 1.0f;

			lastPos = Position;
		}

		public override void Update()
		{
			var pawn = Local.Pawn;
			if ( pawn == null ) return;

			var eyePos = pawn.EyePosition;
			if ( eyePos.Distance( lastPos ) < 300 ) // TODO: Tweak this, or add a way to invalidate lastpos when teleporting
			{
				Position = Vector3.Lerp( eyePos.WithZ( lastPos.z ), eyePos, 20.0f * Time.Delta );
			}
			else
			{
				Position = eyePos;
			}

			Rotation = pawn.EyeRotation;

			Viewer = pawn;
			lastPos = Position;
		}
	}
}
