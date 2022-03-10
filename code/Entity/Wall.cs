using System;
using System.Collections.Generic;
using Sandbox;

namespace PortalGame
{

	public partial class FakeWall : ModelEntity {

		[Net, Change] private bool Solid { get; set;  }

		private const float width =  32f;
		private const float depth =   1f;
		private const float height = 50f;


		public FakeWall() : base() {
			Transmit = TransmitType.Always;
			Solid = false;
		}

		public void Toggle(bool status) {
			OnSolidChanged( Solid, status );
			Solid = status;
		}

		public void OnSolidChanged(bool old, bool newValue) {
			if( newValue ) {
				Model = CreateCollider();
				SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
				PhysicsEnabled = true;
				UsePhysicsCollision = true;
			}
			else {
				PhysicsEnabled = false;
				UsePhysicsCollision = false;
			}
		}

		private Model CreateCollider() {
			return Model.Builder
				.AddCollisionBox( new Vector3( width, depth,  1 ), new Vector3( 0, 0, -height ) )
				.AddCollisionBox( new Vector3( width, depth,  1 ), new Vector3( 0, 0,  height ) )
				.AddCollisionBox( new Vector3( 1, depth, height ), new Vector3( width, 0, 0 ) )
				.AddCollisionBox( new Vector3( 1, depth, height ), new Vector3(-width, 0, 0 ) )
				.Create();
		}
	}

	[Library( "portal_wall" )]
	[Hammer.Solid]
	public partial class Wall : BrushEntity {

		[Net] IDictionary<Portal, FakeWall> fakes { get; set; }

		public override void Spawn() {
			base.Spawn();
			SetInteractsExclude( CollisionLayer.CARRIED_OBJECT );
			Transmit = TransmitType.Always;
			fakes = new Dictionary<Portal, FakeWall>();
		}

		public void Open( Portal entrance ) {
			var fake = new FakeWall();
			fake.Position = entrance.Position;
			fake.Rotation = entrance.Rotation;
			fake.Parent = this;
			fake.Toggle( true );

			fakes[entrance] = fake;
		}
		public void Close( Portal entrance ) {

			fakes[entrance].Delete();
			fakes.Remove( entrance );
		}

		public void Carve( bool status, ModelEntity traveller) {
			if ( status ) {
				traveller.AddCollisionLayer( CollisionLayer.CARRIED_OBJECT );

				if ( traveller is PortalPlayer )
					EnableAllCollisions = false;
				else
					traveller.RemoveCollisionLayer( CollisionLayer.PhysicsProp );
			}
			else {
				traveller.RemoveCollisionLayer( CollisionLayer.CARRIED_OBJECT );
				
				if ( traveller is PortalPlayer )
					EnableAllCollisions = true; 
				else
					traveller.AddCollisionLayer( CollisionLayer.PhysicsProp );
			}
		}


	}
}
