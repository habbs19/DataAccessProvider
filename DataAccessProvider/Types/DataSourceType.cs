using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessProvider.Types;

public abstract class DataSourceType { }


public class MSSQL : DataSourceType { }
public class Postgres : DataSourceType { }
public class MySql : DataSourceType { }
public class Oracle : DataSourceType { }
public class MongoDB : DataSourceType { }
public class JsonFile : DataSourceType { }
public class StaticCode : DataSourceType { }
