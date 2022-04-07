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
		private static IDictionary<string, List<Material>> Materials = new Dictionary<string, List<Material>>();
		private Material Material {
			get {
				return Rand.FromList( Materials[Color + "_" + Type] );
			}
		}
		private Material MaterialEnd {
			get {
				return Rand.FromList( Materials[Color + "_" + Type + "_" + Ending] );
			}
		}
		private Material MaterialCorner {
			get
			{
				return Rand.FromList( Materials[Color + "_" + Type + "_" + CableEnding.Corner] );
			}
		}

		private static float CableSize = 16f;

		private List<ModelEntity> childs = new List<ModelEntity>();

		[Net] private string NetworkedPathNodesJSON { get; set; }

		public enum CableColor {
			Blue,
			Orange
		}
		public enum CableType {
			New,
			Old
		}
		public enum CableEnding {
			OnOff,
			Timer,
			Corner
		};

		[Property( "color")]
		public CableColor Color { get; set; } = CableColor.Blue;
		[Property( "type" )]
		public CableType Type { get; set; } = CableType.New;
		[Property( "ending" )]
		public CableEnding Ending { get; set; } = CableEnding.OnOff;

		private static void Init_Textures() {
			string key = "";

			key = CableColor.Blue + "_" + CableType.New;
			if ( Materials.ContainsKey( key ) == false ) {
					Materials[key] = new List<Material> {
					Material.Load( "materials/signage/indicator_blue_1.vmat" ),
					Material.Load( "materials/signage/indicator_blue_2.vmat" ),
					Material.Load( "materials/signage/indicator_blue_3.vmat" ),
					Material.Load( "materials/signage/indicator_blue_4.vmat" )
				};
			}

			key = CableColor.Blue + "_" + CableType.New + "_" + CableEnding.OnOff;
			if ( Materials.ContainsKey( key ) == false ) {
					Materials[key] = new List<Material> {
					Material.Load( "materials/portal_indicators/sign/indicator_unchecked.vmat" )
				};
			}

			key = CableColor.Blue + "_" + CableType.New + "_" + CableEnding.Corner;
			if ( Materials.ContainsKey( key ) == false )
			{
				Materials[key] = new List<Material> {
					Material.Load( "materials/signage/corner_blue.vmat" )
				};
			}


			key = CableColor.Orange + "_" + CableType.New;
			if ( Materials.ContainsKey( key ) == false ) {
				Materials[key] = new List<Material> {
					Material.Load( "materials/signage/indicator_orange_1.vmat" ),
					Material.Load( "materials/signage/indicator_orange_2.vmat" ),
					Material.Load( "materials/signage/indicator_orange_3.vmat" ),
					Material.Load( "materials/signage/indicator_orange_4.vmat" )
				};
			}

			key = CableColor.Orange + "_" + CableType.New + "_" + CableEnding.OnOff;
			if ( Materials.ContainsKey( key ) == false ) {
				Materials[key] = new List<Material> {
					Material.Load( "materials/portal_indicators/sign/indicator_checked.vmat" )
				};
			}

			key = CableColor.Orange + "_" + CableType.New + "_" + CableEnding.Corner;
			if ( Materials.ContainsKey( key ) == false ) {
				Materials[key] = new List<Material> {
					Material.Load( "materials/signage/corner_orange.vmat" )
				};
			}



		}

		public Cable() {
			Transmit = TransmitType.Always;

			Init_Textures();
		}

		public override void Spawn() {
			base.Spawn();
			NetworkedPathNodesJSON = pathNodesJSON;
		}

		
		public override void ClientSpawn() {
			base.ClientSpawn();
			pathNodesJSON = NetworkedPathNodesJSON;
			PathNodes = JsonSerializer.Deserialize<List<BasePathNode>>( pathNodesJSON, new JsonSerializerOptions { PropertyNameCaseInsensitive = true } );

			childs.ForEach( i => i.Delete() );
			childs.Clear();
			Rebuild();
		}

		private async void Rebuild() {
			// delay until all entities are spawned.
			await Task.Delay( 100 );


			for ( int i = 0; i < PathNodes.Count - 1; i++ ) {

				bool start = (i == 0);
				bool corner = !PathNodes[i].TangentIn.z.AlmostEqual( PathNodes[i].TangentOut.z );
				bool last = (i == PathNodes.Count - 2);

				var model = Model.Builder
					.AddMeshes( CreateMeshesh( PathNodes[i], PathNodes[i + 1], start, corner, last ) )
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

		private Mesh[] CreateMeshesh(BasePathNode src, BasePathNode dst, bool first, bool corner, bool last)
		{
			void CreateQuad( Vector3 position, Rotation rot, Material mat, ref bool skipedFirst, ref bool skippedCorner, ref List<Mesh> meshes ) {
				if ( !skippedCorner )
					skippedCorner = true;

				if ( !skipedFirst ) {
					skipedFirst = true;
					if( first || corner )
						return;
				}


				var vb = new VertexBuffer();
				vb.Init( true );

				Vector3 size = new Vector3( CableSize );

				var f = rot.Forward * 0.1f;
				var u = rot.Left * size.y * 0.5f;
				var v = rot.Up * size.x * 0.5f;

				vb.AddQuad( new Ray( position + f, f.Normal ), u, v );
				vb.AddQuad( new Ray( position - f, f.Normal ), u, -v );

				//

				var mesh = new Mesh( mat );
				mesh.CreateBuffers( vb );
				meshes.Add( mesh );
			}

			Vector3 start = src.Position;
			Vector3 end = dst.Position;
			Rotation rot = GetMeshDirection( src, dst ).EulerAngles.ToRotation();

			List<Mesh> meshes = new List<Mesh>();
			Vector3 delta = end - start;

			float incrx = Math.Sign( delta.x ) * CableSize;
			float incry = Math.Sign( delta.y ) * CableSize;
			float incrz = Math.Sign( delta.z ) * CableSize;

			float x = 0, y = 0, z = 0;
			bool skipedFirst = false, skippedCorner = false;

			skippedCorner = false;
			for ( x = 0; Math.Abs( x )  < Math.Abs( delta.x ); x += incrx ) {
				CreateQuad( new Vector3( start.x + x, start.y + y, start.z + z), rot, !skippedCorner ? MaterialCorner : Material, 
					ref skipedFirst, ref skippedCorner, ref meshes );
			}

			skippedCorner = false;
			for ( y = 0; Math.Abs( y ) < Math.Abs( delta.y ); y += incry ) {
				CreateQuad( new Vector3( start.x + x, start.y + y, start.z + z), rot, !skippedCorner ? MaterialCorner : Material, 
					ref skipedFirst, ref skippedCorner, ref meshes );
			}

			skippedCorner = false;
			for ( z = 0; Math.Abs( z ) < Math.Abs( delta.z ); z += incrz ) {
				CreateQuad( new Vector3( start.x + x, start.y + y, start.z + z), rot, !skippedCorner ? MaterialCorner : Material, 
					ref skipedFirst, ref skippedCorner, ref meshes );
			}


			if( last ) {
				CreateQuad( new Vector3( start.x + x, start.y + y, start.z + z ), rot, MaterialEnd,
					ref skipedFirst, ref skippedCorner, ref meshes );
			}
			return meshes.ToArray();			
		} 

		[Event.Tick]
		public void Tick()
		{
			//DrawPath( 0, true );
		}

		[ClientCmd("cmd_respawn")]
		public static void Respawn()
		{
			All.Where(i => i is Cable).ToList().ForEach( i => i.ClientSpawn() );
		}
	}
}
