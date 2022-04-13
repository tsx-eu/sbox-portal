using System;
using System.Collections.Generic;
using Sandbox;

namespace PortalGame
{
	
	[Library("portal_generator")]
	[Hammer.Model( Model = "models/props_gameplay/portal_generator.vmdl" )]
	public partial class PortalGenerator : ModelEntity
	{

		[Net] private Portal Portal { get; set; }

		[Property( "target_destination", Title = "Target Portal")]
		[FGDType( "target_destination" )]
		public string TargetName { get; set; }

		public override void Spawn() {
			base.Spawn();
			SetModel( "models/props_gameplay/portal_generator.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		}

		[Input]
		public void Open() {
			PortalGenerator LinkedTo = FindByName( TargetName ) as PortalGenerator;

			if ( LinkedTo == null )
				return;
			if ( Portal != null )
				return;

			Portal = new Portal();
			Portal.Position = Position + Rotation.Left * 2;
			Portal.Rotation = Rotation.LookAt( Rotation.Left ) * Rotation.From( 0, 90, 0 );
			Portal.SetType( 0 );

			LinkedTo.Portal = new Portal();
			LinkedTo.Portal.Position = LinkedTo.Position;
			LinkedTo.Portal.Rotation = Rotation.LookAt( LinkedTo.Rotation.Left ) * Rotation.From( 0, -90, 0 );
			LinkedTo.Portal.SetType( 1 );

			Portal.Open( LinkedTo.Portal );
			LinkedTo.Portal.Open( Portal );
		}

		[Input]
		public void Close()
		{
			PortalGenerator LinkedTo = FindByName( TargetName ) as PortalGenerator;

			if ( Portal == null )
				return;

			Portal.Close();
			LinkedTo.Portal.Close();

			LinkedTo.Portal.Delete();
			LinkedTo.Portal = null;

			Portal.Delete();
			Portal = null;
		}

	}
}
