using System.Collections;
using System.Collections.Generic;

namespace DataAccessProvider.Core.Abstractions
{
    [Obsolete("Migrate to the 1.4 client, command and result API before 2.0; see docs/migration.md.", DiagnosticId = "DAP001")]
    public abstract class BaseDataSourceParams<TValue> where TValue : class
    {
        private IEnumerable<TValue>? _value;
        public IEnumerable<TValue>? Value => _value;

        public void SetValue(List<TValue> value) => _value = value;
        public void SetValue(TValue value) => _value = new List<TValue> { value };

    }

    [Obsolete("Migrate to the 1.4 client, command and result API before 2.0; see docs/migration.md.", DiagnosticId = "DAP001")]

    public abstract class BaseDataSourceParams
    {
        private object? _value;
        public object? Value => _value;
        public void SetValue(object value) => _value = value;

    }

}
