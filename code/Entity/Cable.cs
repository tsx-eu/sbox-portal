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

		[Net, Change( "Generate" )] private string NetworkedPathNodesJSON { get; set; }

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
		private enum CableFace {
			Unkown,
			X,
			Y,
			Z
		};

		[Property( "color"), Net, Change( "Generate" )]
		public CableColor Color { get; set; } = CableColor.Blue;
		[Property( "type" ), Net, Change( "Generate" )]
		public CableType Type { get; set; } = CableType.New;
		[Property( "ending" ), Net, Change( "Generate" )]
		public CableEnding Ending { get; set; } = CableEnding.OnOff;
		[Property( "facing" ), Net, Change( "Generate" )] 
		private CableFace WallDirection { get; set; } = CableFace.Z;


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

			Generate();
		}

		[Event.Entity.PostSpawn]
		private void Generate() {

			CableFace direction = WallDirection;

			childs.ForEach( i => i.Delete() );
			childs.Clear();
			for ( int i = 0; i < PathNodes.Count - 1; i++ ) {

				bool start = (i == 0);
				bool last = (i == PathNodes.Count - 2);

				var model = Model.Builder
					.AddMeshes( CreateMeshesh( PathNodes[i], PathNodes[i + 1], start, last, ref direction) )
					.Create();

				var prop = new ModelEntity {
					PhysicsEnabled = false,
					Model = model,
					Transform = Transform,
					Transmit = TransmitType.Always
				};
				prop.Spawn();
				childs.Add( prop );
			}
		}

		private Mesh[] CreateMeshesh(BasePathNode src, BasePathNode dst, bool first, bool last, ref CableFace direction)
		{
			Vector3 GetMeshDirection( BasePathNode src, BasePathNode dst, ref CableFace direction )
			{
				void ChangeDirection( ref CableFace direction )
				{
					if ( !src.TangentIn.x.AlmostEqual( 0 ) )
						direction = CableFace.X;
					if ( !src.TangentIn.y.AlmostEqual( 0 ) )
						direction = CableFace.Y;
					if ( !src.TangentIn.z.AlmostEqual( 0 ) )
						direction = CableFace.Z;
				}

				switch ( direction )
				{
					case CableFace.Unkown:
						{
							Vector3 mid = Transform.Position + (src.Position + dst.Position) / 2;

							var up1 = Trace.Ray( mid + Vector3.Up * 8f, mid - Vector3.Up * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();
							var up2 = Trace.Ray( mid - Vector3.Up * 8f, mid + Vector3.Up * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();

							if ( up1.Hit && up1.Distance >= 4f || up2.Hit && up2.Distance >= 4f )
							{
								direction = CableFace.Z;
								break;
							}

							var left1 = Trace.Ray( mid + Vector3.Left * 8f, mid - Vector3.Left * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();
							var left2 = Trace.Ray( mid - Vector3.Left * 8f, mid + Vector3.Left * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();

							if ( left1.Hit && left1.Distance >= 4f || left2.Hit && left2.Distance >= 4f )
							{
								direction = CableFace.Y;
								break;
							}

							var forward1 = Trace.Ray( mid + Vector3.Forward * 8f, mid - Vector3.Forward * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();
							var forward2 = Trace.Ray( mid - Vector3.Forward * 8f, mid + Vector3.Forward * 2f ).WorldAndEntities().Radius( 1.0f ).HitLayer( CollisionLayer.Solid ).Run();

							if ( forward1.Hit && forward1.Distance >= 4f || forward2.Hit && forward2.Distance >= 4f )
							{
								direction = CableFace.X;
								break;
							}

							break;
						}
					case CableFace.X: // nous sommes sur une face en direction X
						if ( !src.Position.x.AlmostEqual( dst.Position.x ) )
							ChangeDirection( ref direction );
						break;
					case CableFace.Y:
						if ( !src.Position.y.AlmostEqual( dst.Position.y ) )
							ChangeDirection( ref direction );
						break;
					case CableFace.Z:
						if ( !src.Position.z.AlmostEqual( dst.Position.z ) )
							ChangeDirection( ref direction );
						break;
				}

				switch ( direction )
				{
					case CableFace.X:
						return Vector3.Forward;
					case CableFace.Y:
						return Vector3.Left;
					case CableFace.Z:
						return Vector3.Up;
				}

				return Vector3.One;
			}
			void CreateQuad( Vector3 position, Rotation rot, Material mat, ref bool skipedFirst, ref bool skippedCorner, ref bool edge, ref List<Mesh> meshes ) {
				if ( !skippedCorner )
					skippedCorner = true;

				if ( !skipedFirst ) {
					skipedFirst = true;

					if( first || edge )
						return;
				}


				var vb = new VertexBuffer();
				vb.Init( true );

				var f = rot.Forward * 0.1f;
				var l = rot.Left * CableSize * 0.5f;
				var u = rot.Up * CableSize * 0.5f;
				var front = position + f;
				var back = position - f;

				vb.Default.Normal = f.Normal;
				vb.Default.Tangent = new Vector4( l.Normal, 1 );

				vb.Add( front + u + l, new Vector2(  0, -1 ) );
				vb.Add( front + u - l, new Vector2( -1, -1 ) );
				vb.Add( front - u - l, new Vector2( -1,  0 ) );
				vb.Add( front - u + l, new Vector2(  0,  0 ) );

				vb.AddTriangleIndex( 4, 3, 2 );
				vb.AddTriangleIndex( 2, 1, 4 );

				vb.Add( back + u - l, new Vector2(  0, -1 ) );
				vb.Add( back + u + l, new Vector2( -1, -1 ) );
				vb.Add( back - u + l, new Vector2( -1,  0 ) );
				vb.Add( back - u - l, new Vector2(  0,  0 ) );

				vb.AddTriangleIndex( 4, 3, 2 );
				vb.AddTriangleIndex( 2, 1, 4 );

				//

				var mesh = new Mesh( mat );
				mesh.CreateBuffers( vb );
				meshes.Add( mesh );
			}

			List<Mesh> meshes = new List<Mesh>();
			float x = 0, y = 0, z = 0;
			bool skipedFirst = false, skippedCorner = false, edge = false;
			Vector3 start = src.Position;
			Vector3 stop = dst.Position;
			Vector3 delta = stop - start;

			float incrx = Math.Sign( delta.x ) * CableSize;
			float incry = Math.Sign( delta.y ) * CableSize;
			float incrz = Math.Sign( delta.z ) * CableSize;

			CableFace oldFace = direction;
			Rotation rot = GetMeshDirection( src, dst, ref direction).EulerAngles.ToRotation();
			if ( oldFace != direction )
				edge = true;

			skippedCorner = false;
			for ( x = 0; Math.Abs( x )  < Math.Abs( delta.x ); x += incrx ) {
				CreateQuad( new Vector3( start.x + x, start.y + y, start.z + z), rot, !skippedCorner ? MaterialCorner : Material, 
					ref skipedFirst, ref skippedCorner, ref edge, ref meshes );
			}

			skippedCorner = false;
			for ( y = 0; Math.Abs( y ) < Math.Abs( delta.y ); y += incry ) {
				CreateQuad( new Vector3( start.x + x, start.y + y, start.z + z), rot, !skippedCorner ? MaterialCorner : Material, 
					ref skipedFirst, ref skippedCorner, ref edge, ref meshes );
			}

			skippedCorner = false;
			for ( z = 0; Math.Abs( z ) < Math.Abs( delta.z ); z += incrz ) {
				CreateQuad( new Vector3( start.x + x, start.y + y, start.z + z), rot, !skippedCorner ? MaterialCorner : Material, 
					ref skipedFirst, ref skippedCorner, ref edge, ref meshes );
			}


			if( last ) {
				CreateQuad( new Vector3( start.x + x, start.y + y, start.z + z ), rot, MaterialEnd,
					ref skipedFirst, ref skippedCorner, ref edge, ref meshes );
			}
			return meshes.ToArray();			
		} 

		[Input]
		public void Enable()
		{
			Color = CableColor.Orange;
		}

		[Input]
		public void Disable()
		{
			Color = CableColor.Blue;
		}
	}
}
