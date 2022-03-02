using Sandbox;

namespace Portal
{
	public interface IPlayerGrabable {
		public PortalPlayer GrabbedBy { get; set; }
	}

	public partial class PortalPlayer
	{
		public IPlayerGrabable GrabbedEntity { get; set; }
		public bool Grabbing { get { return GrabbedEntity != null; } }
		private MoveType oldMoveType;

		private const float MinGrabDistance = 32.0f;
		private const float MaxGrabDistance = 64.0f;
		private const float GrabVelocityFactor = 32.0f;

		public void TryStartGrab()
		{
			if ( Grabbing )
				return;

			var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * MaxGrabDistance )
				.Ignore( this, false )
				.HitLayer( CollisionLayer.Solid )
				.Run();

			if ( !tr.Hit || !tr.Entity.IsValid() || tr.Entity.IsWorld || tr.StartedSolid )
				return;

			var ent = tr.Entity as IPlayerGrabable;
			if ( ent.GrabbedBy != null )
				return;

			StartGrab( ent );
		}

		private void StartGrab( IPlayerGrabable grab )
		{
			var ent = grab as ModelEntity;
			oldMoveType = ent.MoveType;
			ent.PhysicsBody.AutoSleep = false;
			ent.MoveType = MoveType.MOVETYPE_FLY;

			GrabbedEntity = grab;
			GrabbedEntity.GrabbedBy = this;
		}
		public void StopGrab()
		{
			if ( !Grabbing )
				return;

			var ent = GrabbedEntity as ModelEntity;
			ent.MoveType = oldMoveType;
			ent.PhysicsBody.AutoSleep = true;

			GrabbedEntity.GrabbedBy = null;
			GrabbedEntity = null;
		}


		private void UpdateGrab() {
			if ( !Grabbing )
				return;

			var ent = GrabbedEntity as Entity;
			if ( !ent.IsValid() ) {
				StopGrab();
				return;
			}

			var target = EyePosition + EyeRotation.Forward * MaxGrabDistance;
			var direction = target - ent.Position;
			float distance = direction.Length;

			if ( distance > MaxGrabDistance || distance < MinGrabDistance )
				ent.Velocity = direction * GrabVelocityFactor;
			if ( distance > MaxGrabDistance * 2.0f )
				StopGrab();
		}
	}
}
