using System;
using System.Collections.Generic;

namespace mmria.services.backup;

public sealed class cBulkDocument
{
	public cBulkDocument ()
	{
		docs = new List<IDictionary<string, object>> ();
	}

	public List<IDictionary<string, object>> docs { get; set; }

}

