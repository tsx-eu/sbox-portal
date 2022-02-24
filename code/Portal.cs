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

		public void Update() {
			CreateViewTexture();

			pos = source.GetCameraPosition( Local.Pawn );
			rot = source.GetRotation( Local.Pawn );
			fov = GetFOV( Local.Pawn );

			// FIXME: Moving Transform.position to portal position cause bug in the renderer.

			if ( quad == null ) {
				quad = new VertexBuffer();
				quad.Init( true );
				quad.AddCube( source.Position, new Vector3( 128, 4, 128 ), source.Rotation );
			}
		}

		private float GetFOV( Entity player ) {
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

		public List<PortalTraveller> trackedTravellers { get; set; } = new List<PortalTraveller>();

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

		private void checkTraversal() {


			if ( linkedPortal == null )
				return;

			for ( int i = 0; i < trackedTravellers.Count; i++ )
			{
				var traveller = trackedTravellers[i];
				var transform = Transform.Rotation.Left;

				var offset = (traveller.Entity.Position - Position);

				var newSide = Math.Sign( Vector3.Dot( offset, transform ) );
				var oldSide = Math.Sign( Vector3.Dot( traveller.previousOffsetFromPortal, transform ) );

				if ( newSide != oldSide )
				{
					traveller.Teleport( this, linkedPortal );
					linkedPortal.OnTriggerEnter( traveller );
					this.trackedTravellers.RemoveAt( i );
					i--;
					continue;
				}
				else
				{
					traveller.previousOffsetFromPortal = offset;
				}
			}
		}

		[Event.Tick]
		public void OnServerTick()
		{
			if ( !IsServer )
				return;

			if ( linkedPortal == null )
				linkedPortal = FindByName( targetName ) as Portal;
			checkTraversal();
		}

		[Event.Frame]
		public void OnClientTick( ) {
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
					EnableDrawing = false;
				}
			}

			if ( linkedPortal != null ) {
				render.Update();
				checkTraversal();
			}

			if( camera != null ) {
				camera.Position = GetCameraPosition( Local.Pawn );
				camera.Rotation = GetRotation( Local.Pawn );
			}
		}

		public Vector3 GetPosition( Entity player )
		{
			var relativePositionFromOrigin = Position - player.Position;
			var relativeRotation = relativePositionFromOrigin * Rotation.Inverse * linkedPortal.Rotation;
			var relativePositionToPortal = linkedPortal.Position - relativeRotation;

			return relativePositionToPortal;
		}
		public Vector3 GetCameraPosition( Entity player )
		{
			var relativePositionFromOrigin = Position - player.EyePosition;
			var relativeRotation = relativePositionFromOrigin * Rotation.Inverse * linkedPortal.Rotation;
			var relativePositionToPortal = linkedPortal.Position - relativeRotation;

			return relativePositionToPortal;
		}
		public Rotation GetRotation( Entity player )
		{
			var rot = Rotation.Inverse * linkedPortal.Rotation * player.EyeRotation;
			return rot;
		}


		public override void StartTouch( Entity other )
		{
			base.StartTouch( other );

			if ( other.IsWorld )
				return;

			PortalTraveller traveller = other.Components.Get<PortalTraveller>();
			if ( traveller != null )
				OnTriggerEnter( traveller );
		}
		public override void EndTouch( Entity other )
		{
			base.EndTouch( other );
			if ( other.IsWorld )
				return;

			PortalTraveller traveller = other.Components.Get<PortalTraveller>();
			if ( traveller != null )
				OnTriggerExit( traveller );
		}


		public void OnTriggerEnter( PortalTraveller traveller ) {
			if( !trackedTravellers.Contains(traveller) ) {
				Log.Error( "entered in " + Name );
				traveller.EnterPortalThreshold( this );
				traveller.previousOffsetFromPortal = traveller.Entity.Position - Position;
				trackedTravellers.Add(traveller);
			}

		}
		public void OnTriggerExit( PortalTraveller traveller ) {
			if ( trackedTravellers.Contains( traveller ) ) {
				Log.Error( "left " + Name );
				traveller.ExitPortalThreshold( this );
				trackedTravellers.Remove( traveller );
			}
		}

		public bool IsVisible( ) {
			// TODO: PVS check ? 
			// TODO: AABB check ?
			// TODO: Trace.Sweep ?

			return true;
		}

	}
}
