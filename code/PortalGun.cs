﻿
using Sandbox;

namespace Portal
{
	[Library( "portal_gun", Spawnable = true )]
	public partial class PortalGun : BaseWeapon
	{
		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

		public PickupTrigger PickupTrigger { get; protected set; }

		public override void Spawn() {
			base.Spawn();

			PickupTrigger = new PickupTrigger {
				Parent = this,
				Position = Position,
				EnableTouch = true,
				EnableSelfCollisions = false
			};

			PickupTrigger.PhysicsBody.AutoSleep = false;

			SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
		}
		public override void SimulateAnimator( PawnAnimator anim ) {
			anim.SetAnimParameter( "holdtype", 1 );
			anim.SetAnimParameter( "aim_body_weight", 1.0f );
			anim.SetAnimParameter( "holdtype_handedness", 0 );
		}

		public override void CreateViewModel() {
			Host.AssertClient();

			ViewModelEntity = new BaseViewModel {
				Position = Position,
				Owner = Owner,
				EnableViewmodelRendering = true
			};

			ViewModelEntity.SetModel( ViewModelPath );
		}

		public override bool CanPrimaryAttack() {
			return Input.Pressed( InputButton.Attack1 );
		}
		public override void AttackPrimary() {
			base.AttackPrimary();

			Log.Info( "primary" );
		}


		public override bool CanSecondaryAttack() {
			return Input.Pressed( InputButton.Attack2 );
		}
		public override void AttackSecondary() {
			base.AttackSecondary();

			Log.Info( "secondary" );
		}
	}
}