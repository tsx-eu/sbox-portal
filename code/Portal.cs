using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace Sandbox
{
	public partial class PortalRendering : RenderEntity {
		public Portal source { get; set; }
		public Portal destination { get; set; }

		private Material material;
		private Texture viewTexture;
		private Texture depthTexture;

		private VertexBuffer quad;

		private Vector3 pos;
		private Rotation rot;
		private float fov;

		private void CreateViewTexture() {

			if ( viewTexture == null || viewTexture.Size.x != Screen.Width.FloorToInt() || viewTexture.Size.y != Screen.Height.FloorToInt() ) {
				viewTexture = Texture.CreateRenderTarget().WithSize( Screen.Width.FloorToInt(), Screen.Height.FloorToInt() ).WithDynamicUsage().Create();
				depthTexture = Texture.CreateRenderTarget().WithSize( Screen.Width.FloorToInt(), Screen.Height.FloorToInt() ).WithDepthFormat().Create();

				var m = Material.Load( "materials/portal.vmat" );
				m.OverrideTexture( "Color", viewTexture );
				material = m.CreateCopy();
			}
		}

		public void Update(Portal portal, Entity player) {
			CreateViewTexture();

			pos = GetPosition( portal, player );
			rot = GetRotation( portal, player );
			fov = GetFOV(portal, player);

			// FIXME: Moving Transform.position to portal position cause bug in the renderer.

			if ( quad == null ) {
				quad = new VertexBuffer();
				quad.Init( true );
				quad.AddCube( portal.Position, new Vector3( 128, 4, 128 ), portal.Rotation );
			}
		}

		public Vector3 GetPosition( Portal portal, Entity player ) {
			var relativePositionFromOrigin = portal.Position - player.EyePosition;
			var relativeRotation = relativePositionFromOrigin * portal.Rotation.Inverse * portal.linkedPortal.Rotation;
			var relativePositionToPortal = portal.linkedPortal.Position - relativeRotation;

			return relativePositionToPortal;
		}
		public Rotation GetRotation( Portal portal, Entity player ) {
			var rot = portal.Rotation.Inverse * portal.linkedPortal.Rotation * player.EyeRotation;
			return rot;
		}
		public float GetFOV( Portal portal, Entity player ) {
			var camera = (player as Pawn).CameraMode as GameCamera;
			float fov = camera.FieldOfView;
			var aspect = Screen.Width / Screen.Height;
			return MathF.Atan( MathF.Tan( fov.DegreeToRadian() * 0.5f ) * (aspect * 0.75f) ).RadianToDegree() * 2.0f;
		}

		[Event.Frame]
		public void OnPlayerTick()
		{
			EnableDrawing = true;
			RenderBounds = new BBox( Vector3.One * -99999, Vector3.One * 99999 );
		}

		public override void DoRender( SceneObject obj )
		{
			if ( EnableDrawing == false )
				return;
			if ( !source.IsVisible() )
				return;

			EnableDrawing = false;

			using ( Render.RenderTarget( viewTexture ) )
			{
				RenderAttributes attributes = new RenderAttributes();
				Render.Draw.DrawScene( viewTexture, depthTexture, obj.World, attributes, new Rect(0, 0, viewTexture.Width, viewTexture.Height), pos, rot, fov, 12.0f, 99999.0f );
			}

			quad.Draw( material );
		}
	}

	[Library("portal")]
	[Hammer.Solid]
	[Hammer.VisGroup( Hammer.VisGroup.Trigger )]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	public partial class Portal : ModelEntity
	{
		[Net, Property( "Target" ), FGDType( "target_destination" )]
		public string targetName { get; set; }

		[Net] public List<PortalTraveller> trackedTravellers { get; set; } = new List<PortalTraveller>();

		private PortalRendering render;
		public Portal linkedPortal { get; set; }

		private bool spawnCamera = true;
		private ModelEntity camera { get; set; }

		public override void Spawn()
		{
			base.Spawn();

			Transmit = TransmitType.Always;
			SetupPhysicsFromOBB( PhysicsMotionType.Keyframed, new Vector3( 128, 4, 128 ) / -2, new Vector3( 128, 4, 128 ) / 2 );
			CollisionGroup = CollisionGroup.Trigger;

			EnableSolidCollisions = false;
			EnableTouch = true;
			EnableTouchPersists = true;
		}
		public override void ClientSpawn()
		{
			base.ClientSpawn();

			if ( spawnCamera && (targetName == "orange" || targetName == "gray") )
			{
				camera = new ModelEntity();
				camera.Name = "debug-camera";
				camera.SetModel( "models/editor/camera.vmdl" );
				//camera.SetParent( this );
				camera.MoveType = MoveType.MOVETYPE_NOCLIP;
				camera.Scale = 1f;
			}
		}

		[Event.Tick]
		public void OnServerTick()
		{
			if ( !IsServer )
				return;

			if ( linkedPortal == null )
				linkedPortal = FindByName( targetName ) as Portal;


			if ( linkedPortal != null ) {

				foreach(var traveller in trackedTravellers ) {

					var offset = traveller.Entity.Position - Position;

					var newSide = Vector3.Forward.Dot( offset );
					var oldSide = Vector3.Forward.Dot( traveller.previousOffsetFromPortal );

					if( newSide != oldSide ) {
						traveller.Teleport( this, linkedPortal );
					}

					traveller.previousOffsetFromPortal = offset;
				}
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
				{
					render = new PortalRendering {
						source = this,
						destination = linkedPortal
					};
					render.SetParent( this );
					//SceneObject.RenderingEnabled = false;
					EnableDrawing = false;
				}
			}

			if ( linkedPortal != null ) {
				render.Update( this, Local.Pawn );
			}

			if( camera != null ) {
				camera.Position = render.GetPosition( this, Local.Pawn );
				camera.Rotation = render.GetRotation( this, Local.Pawn );
			}
		}


		public override void EndTouch( Entity other )
		{
			base.EndTouch( other );
			if ( other.IsWorld )
				return;

			Log.Info( other );
		}
		public override void StartTouch( Entity other )
		{
			base.StartTouch( other );

			if ( other.IsWorld )
				return;

			Log.Info( other );
		}
		public override void Touch( Entity other )
		{
			base.Touch( other );

			if ( other.IsWorld )
				return;

			Log.Info( other );
		}

		private void OnTriggerEnter( PortalTraveller traveller ) {
			if( !trackedTravellers.Contains(traveller) ) {
				traveller.EnterPortalThreshold( this );
				traveller.previousOffsetFromPortal = traveller.Entity.Position - Position;
				trackedTravellers.Add(traveller);
			}

		}
		private void OnTriggerExit( PortalTraveller traveller ) {
			if ( trackedTravellers.Contains( traveller ) ) {
				traveller.ExitPortalThreshold( this );
				trackedTravellers.Remove( traveller );
			}
		}

		public bool IsVisible( ) {
			return true;
			// TODO: BBox DOT

			// TODO: PVS check ? 
			// TODO: AABB check ?
			// TODO: Trace.Sweep ?

			var delta = Transform.Position - Local.Pawn.Position;
			var dot = Local.Pawn.EyeRotation.Forward.Dot( delta );

			if ( dot > 0 )
				return true;

			return false;
		}


	}
}
