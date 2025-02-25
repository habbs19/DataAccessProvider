﻿using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Core.DataSource.Params;

public interface IStaticCodeParams
{
  object Content { get; set; }
}
public class StaticCodeParams : BaseDataSourceParams, IStaticCodeParams
{
    public object Content { get; set; } = null!;
}

public class StaticCodeParams<TValue> : BaseDataSourceParams<TValue>, IStaticCodeParams where TValue : class
{
    public object Content { get; set; } = null!;
}
