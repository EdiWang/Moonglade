using Microsoft.AspNetCore.Http;
using System.Text;

namespace Moonglade.Web.Tests
{
    internal class MockHttpSession : ISession
    {
        readonly Dictionary<string, object> _sessionStorage = new();
        string ISession.Id => throw new NotImplementedException();
        bool ISession.IsAvailable => throw new NotImplementedException();
        IEnumerable<string> ISession.Keys => _sessionStorage.Keys;
        void ISession.Clear()
        {
            _sessionStorage.Clear();
        }
        Task ISession.CommitAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        Task ISession.LoadAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        void ISession.Remove(string key)
        {
            _sessionStorage.Remove(key);
        }
        void ISession.Set(string key, byte[] value)
        {
            _sessionStorage[key] = Encoding.UTF8.GetString(value);
        }
        bool ISession.TryGetValue(string key, out byte[] value)
        {
            if (_sessionStorage[key] != null)
            {
                value = Encoding.ASCII.GetBytes(_sessionStorage[key].ToString());
                return true;
            }
            value = null;
            return false;
        }
    }
}
