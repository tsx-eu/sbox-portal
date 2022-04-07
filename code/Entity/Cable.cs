using Sandbox;
using Sandbox.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Sandbox.Entity
{

	[Library( "portal_cable" )]
	[Hammer.Path( "portal_cable" )]
	public partial class Cable : GenericPathEntity
	{
		[Net] private string NetworkedPathNodesJSON { get; set; }

		private static IDictionary<string, List<Material>> Materials = new Dictionary<string, List<Material>>();
		private IList<ModelEntity> childs = new List<ModelEntity>();

		public enum CableDirection
		{
			Unkown,
			FloorX,
			FloorY,
			WallX,
			WallY,
			CeilingX,
			CeilingY,
		};

		public CableDirection Direction { get; set; } = CableDirection.Unkown;

		public Cable() {
			Transmit = TransmitType.Always;
		}

		public override void Spawn() {
			base.Spawn();
			NetworkedPathNodesJSON = pathNodesJSON;
		}

		private static void Initialize_Textures() {
			if ( Materials.ContainsKey( "blue" ) == false ) {
				var blues = new List<Material> {
					Material.Load( "materials/signage/indicator_blue_1.vmat" ),
					Material.Load( "materials/signage/indicator_blue_2.vmat" ),
					Material.Load( "materials/signage/indicator_blue_3.vmat" ),
					Material.Load( "materials/signage/indicator_blue_4.vmat" )
				};

				Materials["blue"] = blues;
			}

			if ( Materials.ContainsKey( "orange" ) == false ) {
				var blues = new List<Material> {
					Material.Load( "materials/signage/indicator_orange_1.vmat" ),
					Material.Load( "materials/signage/indicator_orange_2.vmat" ),
					Material.Load( "materials/signage/indicator_orange_3.vmat" ),
					Material.Load( "materials/signage/indicator_orange_4.vmat" )
				};

				Materials["orange"] = blues;
			}
		}

		public override void ClientSpawn() {
			base.ClientSpawn();

			Initialize_Textures();
			childs.ToList().ForEach( i => i.Delete() );
			rebuild();
		}

		private async void rebuild() {
			// delay until all entities are spawned.
			await Task.Delay( 100 );

			var nodes = JsonSerializer.Deserialize<List<BasePathNode>>( NetworkedPathNodesJSON, new JsonSerializerOptions { PropertyNameCaseInsensitive = true } );

			CableDirection dir = Direction;

			for ( int i = 0; i < nodes.Count - 1; i++ ) {

				var model = Model.Builder
					.AddMeshes( CreateMeshesh( nodes[i], nodes[i + 1], Materials["blue"], ref dir, i == 0, i == nodes.Count - 2 ) )
					.Create();

				var prop = new ModelEntity {
					PhysicsEnabled = false,
					Model = model,
					Transform = Transform,
					Transmit = TransmitType.Always
				};
				prop.Spawn();
				childs.Add( prop );

				await Task.Delay( 10 );
			}
		}

		private Vector3 GetMeshDirection( BasePathNode src, BasePathNode dst )
		{
			Vector3 mid = Transform.Position + (src.Position + dst.Position) / 2;

			// fwd is tangant
			Vector3 up = dst.TangentIn.EulerAngles.ToRotation().Up;
			Vector3 left = dst.TangentIn.EulerAngles.ToRotation().Left;

			//DebugOverlay.Line( mid +   up * 16, mid -   up * 16, Color.Green, 30, false );
			//DebugOverlay.Line( mid + left * 16, mid - left * 16, Color.Blue , 30, false );

			var up1 = Trace.Ray( mid + up * 8f, mid - up * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();
			var up2 = Trace.Ray( mid - up * 8f, mid + up * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();

			if ( up1.Hit && up1.Distance >= 4f )
				return up1.Normal;
			if ( up2.Hit && up2.Distance >= 4f )
				return up2.Normal;

			var left1 = Trace.Ray( mid + left * 8f, mid - left * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();
			var left2 = Trace.Ray( mid - left * 8f, mid + left * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();

			if ( left1.Hit && left1.Distance >= 4f )
				return left1.Normal;
			if ( left2.Hit && left2.Distance >= 4f )
				return left2.Normal;

			return Vector3.Up;
		}

		private Mesh[] CreateMeshesh(BasePathNode src, BasePathNode dst, List<Material> mats, ref CableDirection dir, bool first, bool last)
		{
			void CreateLine( Vector3 position, Rotation rot, Vector3 size, ref bool skipedFirst, ref bool skippedCorner, ref List<Mesh> meshes ) {
				if ( !skipedFirst ) {
					skipedFirst = true;
					skippedCorner = true;
					return;
				}
				if ( !skippedCorner ) {
					skippedCorner = true;
					return;
				}


				var vb = new VertexBuffer();
				vb.Init( true );


				var f = rot.Forward * 0.1f;

				var u = rot.Up * size.x * 0.5f;
				var v = rot.Left * size.y * 0.5f;

				vb.AddQuad( new Ray( position + f, f.Normal ), v, u );
				vb.AddQuad( new Ray( position - f, f.Normal ), u, v );

				var mesh = new Mesh( Rand.FromList( mats ) );
				mesh.CreateBuffers( vb );
				meshes.Add( mesh );
			}

			int size = 16;

			Vector3 start = src.Position;
			Vector3 end = dst.Position;

			Rotation rot = GetMeshDirection( src, dst ).EulerAngles.ToRotation();

			List<Mesh> meshes = new List<Mesh>();
			Vector3 delta = end - start;

			float incrx = Math.Sign( delta.x ) * size;
			float incry = Math.Sign( delta.y ) * size;
			float incrz = Math.Sign( delta.z ) * size;

			float x = 0, y = 0, z = 0;
			bool skipedFirst = false, skippedCorner = false;

			skippedCorner = false;
			for ( x = 0; Math.Abs( x )  < Math.Abs( delta.x ); x += incrx ) {
				CreateLine(new Vector3( start.x + x, start.y + y, start.z + z), rot, size, ref skipedFirst, ref skippedCorner, ref meshes );
			}

			skippedCorner = false;
			for ( y = 0; Math.Abs( y ) < Math.Abs( delta.y ); y += incry ) {
				CreateLine( new Vector3( start.x + x, start.y + y, start.z + z), rot, size, ref skipedFirst, ref skippedCorner, ref meshes );
			}

			skippedCorner = false;
			for ( z = 0; Math.Abs( z ) < Math.Abs( delta.z ); z += incrz ) {
				CreateLine( new Vector3( start.x + x, start.y + y, start.z + z), rot, size, ref skipedFirst, ref skippedCorner, ref meshes );
			}

			return meshes.ToArray();			
		} 

		[Event.Tick.Server]
		public void OnTick2()
		{
			DrawPath( 0, true );
		}

		[ClientCmd("cmd_respawn")]
		public static void Respawn()
		{
			All.Where(i => i is Cable).ToList().ForEach( i => i.ClientSpawn() );
		}
	}
}
