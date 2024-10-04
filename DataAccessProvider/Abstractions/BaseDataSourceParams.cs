using System.Collections;
using System.Collections.Generic;

namespace DataAccessProvider.Abstractions
{
    public abstract class BaseDataSourceParams<T> where T : class
    {
        private IEnumerable<T>? _value;

        public IEnumerable<T>? Value => _value;

        public void SetValue(IEnumerable<T> value) => _value = value;
    }

    public abstract class BaseDataSourceParams 
    {
        private object? _value;

        public object? Value => _value;

        public void SetValue(object value) => _value = value;
    }

}
