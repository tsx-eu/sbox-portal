
using Sandbox;

namespace Portal
{
	[Library( "portal_gun", Spawnable = true )]
	public partial class PortalGun : BaseWeapon
	{
		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";
		public PickupTrigger PickupTrigger { get; set; }
		[Net] public PortalGunProjectile projectile { get; set; }

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
			return Input.Pressed( InputButton.Attack1 ) && (projectile == null || !projectile.IsValid());
		}
		public override void AttackPrimary() {
			base.AttackPrimary();

			if ( Host.IsServer ) {
				projectile = new PortalGunProjectile();
				projectile.Fire( this );
			}
		}


		public override bool CanSecondaryAttack() {
			return Input.Pressed( InputButton.Attack2 );
		}
		public override void AttackSecondary() {
			base.AttackSecondary();

		}
	}

	public partial class PortalGunProjectile : ModelEntity
	{
		private const float Speed = 1024.0f;
		private const float LifeTime = 1.0f;

		public override void Spawn()
		{
			MoveType = MoveType.Physics;
			PhysicsEnabled = true;
			UsePhysicsCollision = true;
			SetupPhysicsFromSphere( PhysicsMotionType.Dynamic, Vector3.Zero, 2.0f );
			CollisionGroup = CollisionGroup.Weapon;
			RemoveCollisionLayer( CollisionLayer.Player );
			PhysicsBody.GravityEnabled = false;
		}
		public void Fire( BaseWeapon weapon ) {			
			Owner = weapon.Owner;
			Position = weapon.GetAttachment( "muzzle" ).Value.Position;
			Rotation = Owner.EyeRotation;
			Velocity = Rotation.Forward * Speed;

			DeleteAsync( LifeTime );
		}

		protected override void OnPhysicsCollision( CollisionEventData eventData )
		{
			base.OnPhysicsCollision( eventData );
			if ( eventData.Entity.IsWorld )
				Delete();
		}
	}
}
