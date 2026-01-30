using System;

namespace mmria.services.backup;

public struct Header_Struct
{
	public string Authorization;
}
public struct Url_Struct
{
	public string url;
	public Header_Struct headers;
}

public struct Replicate_Struct
{
	public Url_Struct source;
	public Url_Struct target;

	public bool create_target;

	public bool continuous;

}
