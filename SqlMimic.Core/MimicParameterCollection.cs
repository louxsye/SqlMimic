using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SqlMimic.Core
{

    internal class MimicParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new List<DbParameter>();
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int Count => _items.Count;
        public override object SyncRoot => this;
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void CopyTo(Array array, int index) { }
        public override bool IsSynchronized => false;
        public override bool IsFixedSize => false;
        public override bool IsReadOnly => false;

        // Not implemented - current implementation meets immediate needs
        // Unimplemented as existing code serves the immediate purpose
        // Not implemented since current approach achieves the goal
        public override void AddRange(Array values) => throw new NotImplementedException();
        public override bool Contains(string value) => throw new NotImplementedException();
        public override int IndexOf(string parameterName) => throw new NotImplementedException();
        public override void Insert(int index, object value) => throw new NotImplementedException();
        public override void RemoveAt(int index) => throw new NotImplementedException();
        public override void RemoveAt(string parameterName) => throw new NotImplementedException();
        protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];
        protected override DbParameter GetParameter(string parameterName) => null;
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) { }
    }
}
