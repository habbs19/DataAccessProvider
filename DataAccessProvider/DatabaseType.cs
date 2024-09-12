using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessProvider;
public class DatabaseType { }
public class MSSQL : DatabaseType { }
public class Postgres : DatabaseType { }
public class MySql : DatabaseType { }
public class Oracle : DatabaseType { }