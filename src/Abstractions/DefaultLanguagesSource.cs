using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class DefaultLanguagesSource : ILanguagesSource
    {
        private readonly string[] languages = new[] { "sk", "en" };

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < languages.Length; i++)
            {                 
                yield return languages[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
