using System;
using Sandbox;

namespace PortalGame
{
	public class PortalTrace
	{
		public struct TraceRequest {
			public Vector3 from;
			public Vector3 to;

			public Entity ignoreEntityOne;
			public bool ignoreHierarchyOne;
			public Entity ignoreEntityTwo;
			public bool ignoreHierarchyTwo;

			public CollisionLayer Mask;

			public bool Entities = false;
			public bool World = false;

			public bool wentThroughtPortal = false;
			public int ttl = 1;
		}

		public struct TraceResponse {
			public TraceRequest request;
			public TraceResult result;
		}

		private TraceRequest request;

		public static PortalTrace Ray( in Vector3 from, in Vector3 to ) {
			PortalTrace result = new PortalTrace();

			result.request = new TraceRequest {
				from = from,
				to = to,
			};

			return result;
		}

		public PortalTrace Ignore( in Entity ent, in bool hierarchy = true )
		{
			if ( ent == null )
			{

			}
			else if ( request.ignoreEntityOne == null )
			{
				request.ignoreEntityOne = ent;
				request.ignoreHierarchyOne = hierarchy;
			}
			else if ( request.ignoreEntityTwo == null )
			{
				request.ignoreEntityTwo = ent;
				request.ignoreHierarchyTwo = hierarchy;
			}
			else
			{
				throw new Exception( "Traces can only ignore two entities right now - need more? Tell me." );
			}

			return this;
		}

		public PortalTrace HitLayer( CollisionLayer layer, bool hit = true ) {

			if ( hit )
				request.Mask |= layer;
			else
				request.Mask &= ~layer;

			return this;
		}

		public PortalTrace WorldOnly() {
			request.Entities = false;
			request.World = true;
			return this;
		}

		public PortalTrace EntitiesOnly() {
			request.Entities = true;
			request.World = false;
			return this;
		}

		public PortalTrace WorldAndEntities() {
			request.Entities = true;
			request.World = true;
			return this;
		}


		public TraceResponse Run() {

			var ray = Trace.Ray( request.from, request.to )
				.HitLayer( request.Mask )
				.Ignore( request.ignoreEntityOne, request.ignoreHierarchyOne )
				.Ignore( request.ignoreEntityTwo, request.ignoreHierarchyTwo );

			if ( request.Entities && !request.World )
				ray = ray.EntitiesOnly();
			else if ( !request.Entities && request.World )
				ray = ray.WorldOnly();
			else if ( request.Entities && request.World )
				ray = ray.WorldAndEntities();

			var res = ray.Run();

			if ( res.Entity is Portal portal && request.ttl > 0 ) {

				request.from = portal.GetPosition( res.EndPosition );
				request.to = portal.GetPosition( request.to );

				if( request.ignoreEntityOne is PortalPlayer )
					request.ignoreEntityOne = portal.LinkedPortal;
				else if ( request.ignoreEntityTwo is PortalPlayer )
					request.ignoreEntityTwo = portal.LinkedPortal;

				DebugOverlay.Line( request.from, request.to, Color.Red, 0.1f, false);

				request.ttl--;
				request.wentThroughtPortal = true;
				return Run();
			}

			return new TraceResponse {
				request = request,
				result = res
			};
		}
	}
}
