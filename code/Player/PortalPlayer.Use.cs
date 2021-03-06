using Sandbox;

namespace PortalGame
{
	public interface IPlayerGrabable {
		public PortalPlayer GrabbedBy { get; set; }
	}

	public partial class PortalPlayer
	{
		public IPlayerGrabable GrabbedEntity { get; set; }
		public bool Grabbing { get { return GrabbedEntity != null; } }

		private const float MinGrabDistance = 32.0f;
		private const float MaxGrabDistance = 64.0f;
		private const float GrabVelocityFactor = 32.0f;

		public void ToggleGrab()
		{
			if ( !Grabbing )
				TryStartGrab();
			else
				StopGrab();
		}


		public void TryStartGrab()
		{
			if ( Grabbing )
				return;

			var tr = PortalTrace.Ray( EyePosition, EyePosition + EyeRotation.Forward * MaxGrabDistance )
				.Ignore( this, false )
				.HitLayer( CollisionLayer.Solid )
				.HitLayer( CollisionLayer.NPC_CLIP )
				.Run().result;

			if ( !tr.Hit || !tr.Entity.IsValid() || tr.Entity.IsWorld || tr.StartedSolid )
				return;

			var ent = tr.Entity as IPlayerGrabable;
			if( ent == null || ent.GrabbedBy != null )
				return;

			StartGrab( ent );
		}

		private void StartGrab( IPlayerGrabable grab )
		{
			var ent = grab as ModelEntity;
			ent.PhysicsBody.AutoSleep = false;
			ent.PhysicsBody.Sleeping = false;
			ent.PhysicsBody.GravityEnabled = false;
			ent.Position = ent.Position;

			GrabbedEntity = grab;
			GrabbedEntity.GrabbedBy = this;
		}
		public void StopGrab()
		{
			if ( !Grabbing )
				return;

			var ent = GrabbedEntity as ModelEntity;
			if ( ent.IsValid() ) {
				ent.PhysicsBody.AutoSleep = true;
				ent.PhysicsBody.GravityEnabled = true;

				GrabbedEntity.GrabbedBy = null;
			}

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

			var wantedDestination = EyePosition + EyeRotation.Forward * 64;

			var ray1 = PortalTrace.Ray( EyePosition, wantedDestination )
				.WorldAndEntities()
				.HitLayer( CollisionLayer.NPC_CLIP )
				.Ignore( this )
				.Ignore( GrabbedEntity as Entity )
				.Run( );

			var ray2 = PortalTrace.Ray( ent.Position, wantedDestination )
				.WorldAndEntities()
				.HitLayer( CollisionLayer.NPC_CLIP )
				.Ignore( this )
				.Ignore( GrabbedEntity as Entity )
				.Run( );


			if ( ray1.request.wentThroughtPortal || ray2.request.wentThroughtPortal )
			{
				ent.Position = ray1.result.EndPosition;
			}
			else
			{
				var direction = wantedDestination - ent.Position;
				float distance = direction.Length;

				if ( distance > MaxGrabDistance || distance < MinGrabDistance )
					ent.Velocity = direction * GrabVelocityFactor;
				if ( distance > MaxGrabDistance * 2.0f )
					StopGrab();
			}
		}
	}
}
