using System;
using System.Collections.Generic;
using Sandbox;

namespace Portal
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
		private float nearClipPlane;

		private void CreateViewTexture() {

			if ( viewTexture == null || viewTexture.Size.x != Screen.Width.FloorToInt() || viewTexture.Size.y != Screen.Height.FloorToInt() ) {
				viewTexture = Texture.CreateRenderTarget().WithSize( Screen.Width.FloorToInt(), Screen.Height.FloorToInt() ).WithDynamicUsage().Create();
				depthTexture = Texture.CreateRenderTarget().WithSize( Screen.Width.FloorToInt(), Screen.Height.FloorToInt() ).WithDepthFormat().Create();

				var m = Material.Load( "materials/portal.vmat" );
				m.OverrideTexture( "Color", viewTexture );
				material = m.CreateCopy();

				quad = new VertexBuffer();
				quad.Init( true );
				foreach ( var v in source.Model.GetVertices() )
					quad.Add( v.Position );
				foreach ( var i in source.Model.GetIndices() )
					quad.AddRawIndex( (int)i );
			}
		}

		[Event.PreRender]
		public void Update() {
			CreateViewTexture();

			Position = source.Position;
			Rotation = source.Rotation;

			pos = source.GetCameraPosition( Local.Pawn );
			rot = source.GetRotation( Local.Pawn );
			fov = GetFOV( Local.Pawn );
			nearClipPlane = ((Local.Pawn as PortalPlayer).CameraMode as PortalCamera).ZNear;

			EnableDrawing = true;
			RenderBounds = new BBox( Vector3.One * -99999, Vector3.One * 99999 );

			// FIXME: Moving Transform.position to portal position cause bug in the renderer.
		}

		private float GetFOV( Entity player ) {
			var camera = (player as PortalPlayer).CameraMode as PortalCamera;
			float fov = camera.FieldOfView;
			var aspect = Screen.Width / Screen.Height;
			return MathF.Atan( MathF.Tan( fov.DegreeToRadian() * 0.5f ) * (aspect * 0.75f) ).RadianToDegree() * 2.0f;
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
				//float nearZ = SetClipPlane( attributes );
				float nearZ = 0.1f;
				Render.Draw.DrawScene( viewTexture, depthTexture, obj.World, attributes, new Rect(0, 0, viewTexture.Width, viewTexture.Height), pos, rot, fov, nearZ, 99999.0f );
			}

			//Vector3 localPosition;
			//float localWidth = getPortalLocalPosition( out localPosition );

			quad.Draw( material );
		}

		public float SetClipPlane( RenderAttributes attrributes )
		{
			Plane clipPlane = new Plane( destination.Transform.Position, destination.Transform.Rotation.Right );
			string k = "EnableClipPlane";
			bool value = true;
			attrributes.Set( in k, in value );

			k = "ClipPlane0";
			Vector4 value2 = new Vector4( in clipPlane.Normal, clipPlane.Distance );
			attrributes.Set( in k, in value2 );

			return (destination.Transform.Position - pos).Length;
		}
		protected static Vector3 GetCircleNormal( int i, int tessellation )
		{
			var angle = i * (float)Math.PI * 2 / tessellation;

			var dx = (float)Math.Cos( angle );
			var dy = (float)Math.Sin( angle );

			var v = new Vector3( dx, dy, 0 );
			return v.Normal;
		}

		private float getPortalLocalPosition(out Vector3 localPosition )
		{
			var aspect = Screen.Width / Screen.Height;

			float halfHeight = nearClipPlane * MathF.Tan( fov.DegreeToRadian() * 0.5f );
			float halfWidth = halfHeight * aspect;
			float dstToNearClipPlane = new Vector3( halfWidth, nearClipPlane, halfHeight ).Length;

			bool camFacingsameDirAsPortal = Vector3.Dot( source.Rotation.Left, source.Transform.Position - Local.Pawn.Transform.Position ) > 0;

			localPosition = source.Rotation.Left * dstToNearClipPlane * 1f * ( camFacingsameDirAsPortal ? 1 : -1);
			return dstToNearClipPlane;
		}
	}

	[Library("portal")]
	[Hammer.Model( Model = "models/vrportal/portalshape.vmdl" )]
	public partial class Portal : ModelEntity
	{
		[Net, Property( "Target" ), FGDType( "target_destination" )]
		public string targetName { get; set; }

		public List<PortalTraveller> trackedTravellers { get; set; } = new List<PortalTraveller>();

		private PortalRendering render;
		[Net]
		public Portal linkedPortal { get; set; }

		private bool spawnCamera = false;
		private ModelEntity camera { get; set; }

		public override void Spawn()
		{
			base.Spawn();

			Transmit = TransmitType.Always;
			SetModel( "models/vrportal/portalshape.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
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
				camera.MoveType = MoveType.MOVETYPE_NOCLIP;
				camera.Scale = 1f;
			}
		}

		[Event.Physics.PostStep]
		public void CheckTraversal() {
			if ( linkedPortal == null )
				return;

			for ( int i = 0; i < trackedTravellers.Count; i++ )
			{
				var traveller = trackedTravellers[i];
				var transform = Transform.Rotation.Left;

				var offset = (traveller.Entity.EyePosition - Position);

				var newSide = Math.Sign( Vector3.Dot( offset, transform ) );
				var oldSide = Math.Sign( Vector3.Dot( traveller.previousOffsetFromPortal, transform ) );

				if ( newSide != oldSide )
				{
					var pos = GetPosition( traveller.Entity );
					var rot = GetRotation( traveller.Entity );

					traveller.Teleport( pos, rot );
					linkedPortal.OnTriggerEnter( traveller );
					trackedTravellers.RemoveAt( i );
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
		}

		[Event.Frame]
		public void OnClientTick( ) {
			if ( !IsClient )
				return;

			if ( linkedPortal != null && render == null ) {
				render = new PortalRendering {
					source = this,
					destination = linkedPortal
				};
				render.SetParent( this );
				EnableDrawing = false;
			}

			if ( linkedPortal != null ) {
				render.Update();
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

		public void SetType( int type ) {
			SetMaterialGroup( type );
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
