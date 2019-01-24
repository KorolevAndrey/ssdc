// Copyright (C) Sergey Grigorev
// Web site: http://ssdc.getdev.tk
// This addon SSDC, the deformable car for NeoAxis Engine.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace ProjectEntities
{
	public class EntityComponent_SSDC : Entity.Component
	{
		[Entity.FieldSerialize]
		MapCurve way;

        public EntityComponent_SSDC(Entity entity, object userData)
			: base( entity, userData )
		{
		}

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( way == entity )
				way = null;
		}

		public MapCurve Way
		{
			get { return way; }
			set
			{
				if( way != null )
					Owner.UnsubscribeToDeletionEvent( way );
				way = value;
				if( way != null )
					Owner.SubscribeToDeletionEvent( way );
			}
		}
	}
}
