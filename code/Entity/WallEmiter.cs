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

			var vb = new VertexBuffer();
			vb.Init( true );
			vb.AddCube( new Vector3( 8 + length/2, 0, 0 ), new Vector3(length, 64, 2), Rotation.Identity );

			var mesh = new Mesh( Material.Load( "materials/effects/test.vmat" ) );
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
