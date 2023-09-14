using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.RdfFulltextIndex
{
    class DefaultAnalyyzer : StopwordAnalyzerBase
    {
        public DefaultAnalyyzer(LuceneVersion version) : base(version)
        {
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new StandardFilter(m_matchVersion, source);
            result = new LowerCaseFilter(m_matchVersion, result);
            result = new ASCIIFoldingFilter(result);
            return new TokenStreamComponents(source, result);
        }
    }
}
