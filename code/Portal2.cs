using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace Sandbox2
{

	public partial class PortalRenderer : RenderEntity {

		class LinkedPortal {
			public bool ready;
			public bool drawn;
			public Portal entrance;
			public Portal exit;

			public Vector3 pos;
			public Angles ang;
			public float fov;

			public Material material;
			public Texture viewTexture;
			public Texture depthTexture;

			public VertexBuffer quad;

			public LinkedPortal( Portal aSource, Portal aTarget ) {
				entrance = aSource;
				exit = aTarget;

				pos = Vector3.Zero;
				ang = Angles.Zero;
				fov = 80.0f;

				material = null;
				viewTexture = null;
				depthTexture = null;

				ready = false;

				quad = new VertexBuffer();
				quad.Init( true );
				quad.AddCube( aSource.Transform.Position, new Vector3( 128, 4, 128 ), aTarget.Rotation );
			}
			private void CreateViewTexture() {

				if ( viewTexture == null || viewTexture.Size.x != Screen.Width.FloorToInt() || viewTexture.Size.y != Screen.Height.FloorToInt() ) {
					viewTexture = Texture.CreateRenderTarget().WithSize( Screen.Width.FloorToInt(), Screen.Height.FloorToInt() ).WithDynamicUsage().Create();
					depthTexture = Texture.CreateRenderTarget().WithSize( Screen.Width.FloorToInt(), Screen.Height.FloorToInt() ).WithDepthFormat().Create();

					var m = Material.Load( "materials/portal.vmat" );
					m.OverrideTexture( "g_tColor", viewTexture );
					material = m.CreateCopy();

					ready = true;
				}
			}

			public void UpdateCameraPosition() {

				CreateViewTexture();

				pos = entrance.GetPosition( Local.Pawn );
				ang = entrance.GetRotation( Local.Pawn ).Angles();
				fov = entrance.GetFOV( Local.Pawn );

				entrance.EnableDrawing = true;
				exit.EnableDrawing = true;
				drawn = false;
			}
		};
		List<LinkedPortal> portalList;

		public PortalRenderer() {
			portalList = new List<LinkedPortal>();
		}

		public override void ClientSpawn() {
		}

		public void addTarget(Portal aSource, Portal aTarget) {
			if ( aSource != null && aTarget != null ) {
				portalList.Add( new LinkedPortal(aSource, aTarget) );
			}
		}


		
		[Event.PreRender]
		public void think() {
			EnableDrawing = true;
			RenderBounds = new BBox( Vector3.One*-99999, Vector3.One*99999 );

			foreach ( var portal in portalList )
				portal.UpdateCameraPosition();
		}


		public override void UpdateSceneObject( SceneObject obj )
		{
			base.UpdateSceneObject( obj );
		}

		public override void DoRender( SceneObject obj ) {

			if ( EnableDrawing == false )
				return;
			EnableDrawing = false;

			foreach ( var portal in portalList ) {

				if ( portal.ready == false )
					continue;

				using ( Render.RenderTarget( portal.viewTexture ) ) {
					Render.DrawScene( portal.viewTexture, portal.depthTexture, portal.viewTexture.Size, obj.World, portal.pos, portal.ang, portal.fov, default, default, 12.0f, 99999.0f );
				}

				portal.quad.Draw( portal.material );
			}
		}
	}


	[Library("portal")]
	[Hammer.Solid]
	[Hammer.AutoApplyMaterial("materials/portal.vmat")]
	public partial class Portal : Entity
	{
		[Net, Property( "Target name" ), FGDType( "target_destination" )]
		public string targetName { get; set; }

		public Portal linkedPortal { get; set; }
		public static PortalRenderer renderer { get; set; }
		[Net] public ModelEntity camera { get; set; }


		public override void ClientSpawn() {
			if( renderer == null )
				renderer = new PortalRenderer();
			renderer.Transmit = TransmitType.Always;
		}

		public override void Spawn() {
			Transmit = TransmitType.Always;
			EnableDrawing = true;


/*
			if ( targetName == "orange" || targetName == "gray" ) {
				camera = new ModelEntity();
				camera.SetModel( "models/editor/camera.vmdl" );
				camera.Transmit = TransmitType.Always;
				camera.SetParent( this );
				camera.MoveType = MoveType.MOVETYPE_NOCLIP;
				camera.Scale = 0.5f;
				camera.GlowActive = false;
				camera.GlowColor = Color.Red;
			}
*/
		}



		[Event.Tick]
		public void OnServerTick()
		{
			if ( !IsServer )
				return;

			if( linkedPortal == null )
				linkedPortal = FindByName( targetName ) as Portal;

			if ( linkedPortal != null && camera != null ) {
				Entity player = Entity.All.Where( i => i is Player ).ToList().FirstOrDefault();
				camera.Position = GetPosition( player );
				camera.Rotation = GetRotation( player );
			}
		}

		public Vector3 GetPosition( Entity player )
		{
			var relativePositionFromOrigin = Transform.Position - player.EyePosition;
			var relativeRotation = relativePositionFromOrigin * Transform.Rotation.Inverse * linkedPortal.Rotation;
			var relativePositionToPortal = linkedPortal.Position - relativeRotation;

			return relativePositionToPortal;
		}
		public Rotation GetRotation( Entity player )
		{
			var rot = player.EyeLocalRotation;
			return rot;
		}
		public float GetFOV( Entity player )
		{
			var camera = player.Camera as GameCamera;
			float fov = camera.FieldOfView;
			var aspect = Screen.Width / Screen.Height;
			return MathF.Atan( MathF.Tan( fov.DegreeToRadian() * 0.5f ) * (aspect * 0.75f) ).RadianToDegree() * 2.0f;
		}


		[Event.Frame]
		public void OnPlayerTick(  )
		{
			if ( !IsClient )
				return;

			if ( linkedPortal == null )
			{
				linkedPortal = FindByName( targetName ) as Portal;
				if ( linkedPortal != null )
				{
					renderer.addTarget( this, linkedPortal );
					//SceneObject.RenderingEnabled = false;
					EnableDrawing = false;
				}
			}
		}
	}
}
