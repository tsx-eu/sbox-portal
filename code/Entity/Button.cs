using System;
using System.Collections.Generic;
using Sandbox;

namespace PortalGame
{

	public interface IButtonTriggerable
	{
	}

	[Library( "portal_button" )]
	[Hammer.Model( Model = "models/props/button_base.vmdl" )]
	public partial class Button : AnimEntity {
		private ButtonTrigger trigger;

		protected Output OnPress { get; set; }
		protected Output OnRelease { get; set; }

		public override void Spawn() {
			base.Spawn();
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			SetAnimGraph( "animgraphs/portal_button.vanmgrph" );
			ResetAnimParameters();

			trigger = new ButtonTrigger();
			trigger.Parent = this;
			trigger.Bind();
		}

		public void Press() {
			SetMaterialGroup( "on" );
			SetAnimParameter( "enable", true );

			OnPress.Fire(this);
		}
		public void Release() {
			SetMaterialGroup( "default" );
			SetAnimParameter( "enable", false );

			OnRelease.Fire( this );
		}
	}

	partial class ButtonTrigger : BaseTrigger
	{
		private List<IButtonTriggerable> active = new List<IButtonTriggerable>();
		private Button button;
		private const float ratio = 0.6f;

		public void Bind() {
			button = Parent as Button;

			float x = button.CollisionBounds.Size.x;
			float y = button.CollisionBounds.Size.y;

			Model = Model.Builder
				.AddCollisionSphere(
					MathF.Sqrt( x * x + y * y ) / 2 * ratio,
					Transform.Position )
				.Create();

			Position = button.Position;

			CollisionGroup = CollisionGroup.Trigger;
			EnableSolidCollisions = false;
			EnableTouch = true;
			EnableTouchPersists = true;
		}

		public override void StartTouch( Entity toucher ) {
			base.StartTouch( toucher );
			if ( toucher.IsWorld || button == null )
				return;

			if ( toucher is IButtonTriggerable triggerable ) {
				if( active.Count == 0 )
					button.Press();

				if( !active.Contains(triggerable) )
					active.Add( triggerable );
			}
		}

		public override void EndTouch( Entity toucher ) {
			base.EndTouch( toucher );
			if ( toucher.IsWorld || button == null )
				return;

			if ( toucher is IButtonTriggerable triggerable ) {
				active.Remove( triggerable );

				if ( active.Count == 0 )
					button.Release();
			}
		}
	}
}
