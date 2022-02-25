using Sandbox;

namespace Sandbox
{
	partial class PortalPlayer : Player
	{

		private bool hasForcedRotationInput = false;
		private Rotation forceRotation;

		public void Teleport( Vector3 position, Rotation rotation ) {
			ResetInterpolation();
			Position = position;

			forceRotation = rotation;
			hasForcedRotationInput = true;
		}

		public override void BuildInput( InputBuilder input ) {
			base.BuildInput( input );

			if ( hasForcedRotationInput ) {
				input.ViewAngles = forceRotation.Angles();
				hasForcedRotationInput = false;
			}

		}

		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new WalkController();
			Animator = new StandardPlayerAnimator();
			CameraMode = new PortalCamera();

			Components.Add( new PortalTraveller() );

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			base.Respawn();
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );
			SimulateActiveChild( cl, ActiveChild );
		}
	}
}
