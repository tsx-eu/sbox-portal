using System;
using System.Collections.Generic;
using Sandbox;

namespace Portal {

	[Library( "portal_button" )]
	[Hammer.Model( Model = "models/props/button_base.vmdl" )]
	public partial class Button : AnimEntity {
		private ButtonTrigger trigger;


		public override void Spawn() {
			base.Spawn();
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			SetAnimGraph( "animgraphs/portal_button.vanmgrph" );
			ResetAnimParameters();

			trigger = new ButtonTrigger();
			trigger.Parent = this;
			trigger.Bind();
		}

		public void OnPress() {
			SetMaterialGroup( "on" );
			SetAnimParameter( "enable", true );
		}
		public void OnRelease() {
			SetMaterialGroup( "default" );
			SetAnimParameter( "enable", false );
		}
	}

	public interface ButtonTriggerable {
	}

	partial class ButtonTrigger : BaseTrigger
	{
		private List<ButtonTriggerable> active = new List<ButtonTriggerable>();
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

			if ( toucher is ButtonTriggerable triggerable ) {
				if( active.Count == 0 )
					button.OnPress();

				if( !active.Contains(triggerable) )
					active.Add( triggerable );
			}
		}

		public override void EndTouch( Entity toucher ) {
			base.EndTouch( toucher );
			if ( toucher.IsWorld || button == null )
				return;

			if ( toucher is ButtonTriggerable triggerable ) {
				active.Remove( triggerable );

				if ( active.Count == 0 )
					button.OnRelease();
			}
		}
	}
}
