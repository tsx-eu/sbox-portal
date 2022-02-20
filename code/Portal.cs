using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace Sandbox
{
	struct PortalRendering {
		public Material material;
		public Texture viewTexture;
		public Texture depthTexture;

		public VertexBuffer quad;

		public Vector3 pos;
		public Angles ang;
		public float fov;

		private void CreateViewTexture() {

			if ( viewTexture == null || viewTexture.Size.x != Screen.Width.FloorToInt() || viewTexture.Size.y != Screen.Height.FloorToInt() ) {
				viewTexture = Texture.CreateRenderTarget().WithSize( Screen.Width.FloorToInt(), Screen.Height.FloorToInt() ).WithDynamicUsage().Create();
				depthTexture = Texture.CreateRenderTarget().WithSize( Screen.Width.FloorToInt(), Screen.Height.FloorToInt() ).WithDepthFormat().Create();

				var m = Material.Load( "materials/portal.vmat" );
				m.OverrideTexture( "g_tColor", viewTexture );
				material = m.CreateCopy();
			}
		}

		public void Update(Portal portal, Entity player) {
			CreateViewTexture();

			pos = GetPosition( portal, player );
			ang = GetRotation( portal, player ).Angles();
			fov = GetFOV(portal, player);

			if( quad == null ) {
				quad = new VertexBuffer();
				quad.Init( true );
				quad.AddCube( Vector3.Zero, new Vector3( 128, 4, 128 ), Rotation.Identity );
			}
		}

		public Vector3 GetPosition( Portal portal, Entity player ) {
			var relativePositionFromOrigin = portal.Position - player.EyePosition;
			var relativeRotation = relativePositionFromOrigin * portal.Rotation.Inverse * portal.linkedPortal.Rotation;
			var relativePositionToPortal = portal.linkedPortal.Position - relativeRotation;

			return relativePositionToPortal;
		}
		public Rotation GetRotation( Portal portal, Entity player ) {
			var rot = portal.Rotation.Inverse * portal.linkedPortal.Rotation * player.EyeLocalRotation;
			return rot;
		}
		public float GetFOV( Portal portal, Entity player ) {
			var camera = player.Camera as GameCamera;
			float fov = camera.FieldOfView;
			var aspect = Screen.Width / Screen.Height;
			return MathF.Atan( MathF.Tan( fov.DegreeToRadian() * 0.5f ) * (aspect * 0.75f) ).RadianToDegree() * 2.0f;
		}

	}

	[Library("portal")]
	[Hammer.Solid]
	[Hammer.AutoApplyMaterial("materials/portal.vmat")]
	public partial class Portal : RenderEntity
	{
		[Net, Property( "Target name" ), FGDType( "target_destination" )]
		public string targetName { get; set; }

		[Net] public ModelEntity camera { get; set; }

		private bool spawnCamera = true;
		private PortalRendering render;
		public Portal linkedPortal { get; set; }

		public override void Spawn() {
			Transmit = TransmitType.Always;

			if( spawnCamera && ( targetName == "orange" || targetName == "gray" ) ) {
				camera = new ModelEntity();
				camera.SetModel( "models/editor/camera.vmdl" );
				camera.Transmit = TransmitType.Always;
				camera.SetParent( this );
				camera.MoveType = MoveType.MOVETYPE_NOCLIP;
				camera.Scale = 0.5f;
				camera.GlowActive = false;
				camera.GlowColor = Color.Red;
			}
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
				camera.Position = render.GetPosition( this, player );
				camera.Rotation = render.GetRotation( this, player );
			}
		}

		[Event.Frame]
		public void OnPlayerTick(  )
		{
			if ( !IsClient )
				return;

			if ( linkedPortal == null ) {
				linkedPortal = FindByName( targetName ) as Portal;
				if ( linkedPortal != null )
					render = new PortalRendering();
			}

			if ( linkedPortal != null ) {
				render.Update( this, Local.Pawn );

				EnableDrawing = true;
				RenderBounds = new BBox( Vector3.One * -99999, Vector3.One * 99999 );
			}
		}

		public override void UpdateSceneObject( SceneObject obj )
		{
			base.UpdateSceneObject( obj );
		}
		public override void DoRender( SceneObject obj ) {
			if ( EnableDrawing == false )
				return;

			EnableDrawing = false;
			using ( Render.RenderTarget( render.viewTexture ) ) {
				Render.DrawScene( render.viewTexture, render.depthTexture, render.viewTexture.Size, obj.World, render.pos, render.ang, render.fov, default, default, 12.0f, 99999.0f );
			}
			
			render.quad.Draw( render.material );
		}
	}
}
