using Sandbox;

namespace Portal
{
	public partial class PortalPlayer : Player, IButtonTriggerable
	{

		private bool hasForcedRotationInput = false;
		private Rotation forceRotation;

		[ClientRpc]
		public void SetRotation( Rotation rotation ) {
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
			Animator = new PortalAnimator();
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

			if ( Host.IsServer ) {
				if ( Input.Pressed( InputButton.Use ) )
					TryStartGrab();
				UpdateGrab();
				if ( Input.Released( InputButton.Use ) )
					StopGrab();
			}
		}

	}
}
