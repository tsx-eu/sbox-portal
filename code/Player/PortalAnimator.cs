using System.Collections.Generic;
using Sandbox;

namespace Portal
{
	public class PortalAnimator : StandardPlayerAnimator
	{
		public Dictionary<string, object> Params = new Dictionary<string, object>();
		public Dictionary<string, object> Props = new Dictionary<string, object>();
		public Dictionary<string, object> Tags = new Dictionary<string, object>();

		public override void SetAnimParameter( string name, bool val ) {
			base.SetAnimParameter( name, val );
			Params[name] = val;
		}

		public override void SetAnimParameter( string name, int val ) {
			base.SetAnimParameter( name, val );
			Params[name] = val;
		}
		public override void SetAnimParameter( string name, float val ) {
			base.SetAnimParameter( name, val );
			Params[name] = val;
		}
		public override void SetAnimParameter( string name, Vector3 val ) {
			base.SetAnimParameter( name, val );
			Params[name] = val;
		}

		public override void SetProperty( string name, string val ) {
			base.SetProperty( name, val );
			Props[name] = val;
		}

	}
}
