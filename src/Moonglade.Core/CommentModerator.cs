using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edi.WordFilter;

namespace Moonglade.Core
{
    public interface ICommentModerator
    {
        public string ModerateContent(string input);

        public bool HasBadWord(params string[] input);
    }

    public class LocalWordFilterModerator : ICommentModerator
    {
        private readonly IMaskWordFilter _filter;

        public LocalWordFilterModerator(string badWords)
        {
            _filter = new MaskWordFilter(new StringWordSource(badWords));
        }

        public string ModerateContent(string input)
        {
            return _filter.FilterContent(input);
        }

        public bool HasBadWord(params string[] input)
        {
            return input.Any(s => _filter.ContainsAnyWord(s));
        }
    }
}
