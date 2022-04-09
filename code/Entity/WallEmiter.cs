using System.Linq;
using Sandbox;

namespace PortalGame
{

	[Library( "portal_wall_emitter" )]
	[Hammer.Model( Model = "models/props/sign_frame01/wall_emitter.vmdl" )]
	public partial class WallEmiter : Prop
	{
		public enum EmitionType
		{
			Disabled,
			Cleaner,
			Bridge
		}

		[Property( "emition" )]
		public EmitionType Emition { get; set; } = EmitionType.Cleaner;

		[Net]
		public WallEmition EmitedEntity { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/props/sign_frame01/wall_emitter.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			PhysicsEnabled = false;

			EmitedEntity?.Delete();
			EmitedEntity = new WallEmition();
			EmitedEntity.Transform = Transform;
			EmitedEntity.Parent = this;
			EmitedEntity.Spawn();
		}

		[Input]
		public void Enable()
		{
		}

		[Input]
		public void Disable()
		{

		}

		[ServerCmd("cmd_respawn")]
		public static void Cmd_Spawn()
		{
			All.Where( i => i is WallEmiter ).ToList().ForEach( i => i.Spawn() );
		}
	}

	public partial class WallEmition : Prop
	{

		public override void Spawn() {
			base.ClientSpawn();

			Generate();
		}
		public override void ClientSpawn() {
			base.ClientSpawn();

			Generate();
		}
		
		[Event.Entity.PostSpawn]
		public void Generate()
		{
			Vector3 start = Transform.Position + Transform.Rotation.Forward * 8;

			var ray = Trace.Ray( start, start + Transform.Rotation.Forward * 65000 )
				.WorldAndEntities()
				.Radius( 2 )
				.Ignore( this )
				.Ignore( Parent ) 
				.HitLayer( CollisionLayer.Solid )
				.HitLayer( CollisionLayer.NPC_CLIP )
				.WithoutTags( "traveller" )
				.Run();

			Vector3 end = ray.EndPosition;

			float length = (end - start).Length;

			var f = Vector3.Forward * length * 0.5f;
			var l = Vector3.Up * 2 * 0.5f;
			var u = Vector3.Left * 64 * 0.5f;

			var o = f + Vector3.Forward * 8;

			var vb = new VertexBuffer();
			vb.Init( true );

			vb.Default.Normal = f.Normal;
			vb.Default.Tangent = new Vector4( l.Normal, 1 );

			vb.Add( o + u - f, new Vector2( 0, 1 ) );
			vb.Add( o + u + f, new Vector2( length / 64f, 1 ) );
			vb.Add( o - u + f, new Vector2( length / 64f, 0 ) );
			vb.Add( o - u - f, new Vector2( 0, 0 ) );

			vb.AddTriangleIndex( 4, 3, 2 );
			vb.AddTriangleIndex( 2, 1, 4 );

			vb.Add( o + u + f, new Vector2( 0, 1 ) );
			vb.Add( o + u - f, new Vector2( -length / 64f, 1 ) );
			vb.Add( o - u - f, new Vector2( -length / 64f, 0 ) );
			vb.Add( o - u + f, new Vector2( 0, 0 ) );

			vb.AddTriangleIndex( 4, 3, 2 );
			vb.AddTriangleIndex( 2, 1, 4 );



			var mesh = new Mesh( Material.Load( "materials/effects/test2.vmat" ) );
			mesh.CreateBuffers( vb );

			var model = Model.Builder
				.AddMesh( mesh )
				.Create();

			Model = model;
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			Log.Info( "generated" );
		}
	}
}
