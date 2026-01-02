using System;
using System.Collections.Generic;

namespace mmria.common.metadata;

public sealed class FormMetadata
{
  public string FormName { get; set; }
  public string FileName { get; set; }
  public string FieldName { get; set; }
  public string Prompt { get; set; }
  public string Description { get; set; }
  public string Path { get; set; }
  public string DataType { get; set; }
  public List<ListValue> ListValues { get; set; }
}